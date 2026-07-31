using System.Globalization;
using AutoHub.Modules.Valuation.Application.Abstractions;
using AutoHub.Modules.Valuation.Domain.Models;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Valuation.Infrastructure;

/// <summary>
/// A rule-based hedonic depreciation model that works today, on coefficients that live in the registry.
/// </summary>
/// <remarks>
/// <b>Why a hedonic model and not an ML one at launch.</b> An ML price model needs realised sales —
/// eighteen months of them, at volume, per segment — and on day one there are none. A marketplace
/// whose homepage hero is "get your car's price" cannot wait for that, and a model trained on
/// nothing is worse than a model built from published depreciation behaviour that a pricing analyst
/// can read, argue with and correct.
/// <para>
/// <b>Why every number is a registry row.</b> Every coefficient here — the age curve, the kilometre
/// curve, the owner penalty, the city multiplier, the fuel adjustment, the condition grade, the
/// channel spreads, the confidence weights — is a number a pricing team revises against realised
/// sales, monthly, forever. Compiled in, each revision is a release, and the release is scheduled
/// behind whatever else is in the sprint. As rows they are a form, and the model's <i>shape</i>
/// stays code where it can be tested.
/// </para>
/// <para>
/// <b>The arithmetic is a multiplicative chain rendered as an additive ledger.</b> Each factor
/// multiplies the running value, and the difference it made is recorded as a signed delta, so the
/// baseline plus every delta sums exactly to the market price. That is what makes a quote
/// explainable: a seller who sees "₹8.20L new, less ₹2.10L for four years, less ₹38,000 for 62,000
/// km" argues about a line, which is a conversation. A seller who sees one number leaves.
/// </para>
/// <para>
/// Order matters and is fixed here, because multiplication is commutative but the ledger is not: the
/// deltas a seller reads depend on the order, and "age first, then usage, then ownership, then
/// market" is the order a person reasons about a used car in.
/// </para>
/// </remarks>
public sealed class HedonicValuationModel : IValuationModel
{
    /// <summary>
    /// The provider code this model is routed to under the <c>valuation.predict</c> capability.
    /// </summary>
    /// <remarks>
    /// A technical identifier, in the same category as a cache key: it names an implementation the
    /// container holds, and a typo in the matching registry row shows up as an empty provider chain
    /// rather than a wrong price.
    /// </remarks>
    public const string Key = "hedonic";

    private readonly IValuationModelStore _store;
    private readonly ILogger<HedonicValuationModel> _logger;

    public HedonicValuationModel(IValuationModelStore store, ILogger<HedonicValuationModel> logger)
    {
        _store = store;
        _logger = logger;
    }

    public string ModelKey => Key;

    public async Task<Result<ValuationPrediction>> PredictAsync(
        ValuationInputs inputs,
        CancellationToken cancellationToken)
    {
        var definitions = await _store.GetPublishedAsync(cancellationToken);
        var context = inputs.ToJson();

        // Highest priority wins, ties broken by key so the choice is deterministic across nodes —
        // two API servers pricing the same car differently is the worst possible bug here.
        var definition = definitions
            .Where(d => d.Matches(context))
            .OrderByDescending(d => d.Priority)
            .ThenBy(d => d.Key, StringComparer.Ordinal)
            .FirstOrDefault();

        if (definition is null)
        {
            _logger.LogError(
                "No published valuation model matches category {Category}",
                inputs.CategoryCode);

            return Error.Unexpected(
                "valuation.model.not_configured",
                "No valuation model is configured for this kind of vehicle.")
                .With("categoryCode", inputs.CategoryCode);
        }

        var currency = inputs.ReferencePrice?.Currency ?? Currency.Inr;

        var baseline = ResolveBaseline(definition, inputs, currency);
        if (baseline.IsFailure)
        {
            return Result.Failure<ValuationPrediction>(baseline.Error);
        }

        var ageMonths = AgeInMonths(inputs);

        var retentionCurve = definition.Curve(ValuationCurves.AgeRetention);
        if (retentionCurve is null)
        {
            return Result.Failure<ValuationPrediction>(
                ValuationErrors.MissingCurve(ValuationCurves.AgeRetention));
        }

        var ledger = new List<AdjustmentLine>();
        var running = baseline.Value;

        // 1. Age. The largest single move, and the one a seller expects to see first.
        var retention = ageMonths is { } months ? retentionCurve.ValueAt(months) : null;
        running = Apply(running, ledger, ValuationCurves.AgeRetention, null, "Age", retention);

        // 2. Usage. Multiplicative on top of age, because a high-mileage three-year-old and a
        //    high-mileage ten-year-old are not penalised by the same rupee amount.
        var kilometreMultiplier = inputs.KilometresDriven is { } kilometres
            ? definition.Curve(ValuationCurves.KilometrePenalty)?.ValueAt(kilometres)
            : null;

        running = Apply(running, ledger, ValuationCurves.KilometrePenalty, null, "Kilometres", kilometreMultiplier);

        // 3. Ownership, condition, and the market the car sits in.
        running = ApplyFactor(
            running, ledger, definition, ValuationFactors.OwnerCount,
            OwnerKey(inputs), ValuationFactors.OverflowMatchValue);
        running = ApplyFactor(running, ledger, definition, ValuationFactors.ConditionGrade, inputs.ConditionCode);
        running = ApplyFactor(running, ledger, definition, ValuationFactors.FuelType, inputs.FuelCode);
        running = ApplyFactor(running, ledger, definition, ValuationFactors.Transmission, inputs.TransmissionCode);
        running = ApplyFactor(running, ledger, definition, ValuationFactors.City, inputs.CityCode);

        var market = running;

        var confidence = ScoreConfidence(definition, inputs, ageMonths, retentionCurve);

        var bandWidth = definition.Curve(ValuationCurves.ConfidenceBandWidth)?.ValueAt(confidence);
        if (bandWidth is null)
        {
            return Result.Failure<ValuationPrediction>(
                ValuationErrors.MissingCurve(ValuationCurves.ConfidenceBandWidth));
        }

        var channels = BuildChannels(definition, market, retentionCurve, retention, ageMonths);
        var forecast = BuildForecast(definition, market, retentionCurve, retention, ageMonths, bandWidth.Value);

        var band = definition.BandFor(confidence)
            ?? definition.ConfidenceBands.OrderBy(b => b.MinimumScore).FirstOrDefault();

        return new ValuationPrediction
        {
            ModelKey = Key,
            ModelVersion = definition.ModelVersion,
            Currency = currency,
            Channels = channels,
            Adjustments = ledger,
            BaselineMinorUnits = baseline.Value.MinorUnits,
            LowMinorUnits = market.MultiplyByRate(1m - bandWidth.Value).MinorUnits,
            HighMinorUnits = market.MultiplyByRate(1m + bandWidth.Value).MinorUnits,
            Confidence = confidence,
            ConfidenceBand = band?.Code ?? string.Empty,
            Forecast = forecast,
            ValidityDays = definition.ValidityDays,
            Explanation = RenderExplanation(definition, inputs, ledger),
        };
    }

    /// <summary>
    /// The amount the model starts from.
    /// </summary>
    /// <remarks>
    /// Resolved most specific first — variant, then model, then make, then category — so a baseline
    /// exists for a car whose exact trim nobody recorded, at a cost in confidence rather than a
    /// refusal. The alternative is that every vehicle without a variant is unpriceable, and on real
    /// Indian RC data that is most of them.
    /// <para>
    /// An explicit reference price from the caller wins over all of it: somebody who knows the
    /// ex-showroom figure should not be pushed onto a segment median that is worse.
    /// </para>
    /// </remarks>
    private static Result<Money> ResolveBaseline(
        ValuationModelDefinition definition,
        ValuationInputs inputs,
        Currency currency)
    {
        if (inputs.ReferencePrice is { } supplied)
        {
            return supplied;
        }

        string?[] candidates =
        [
            Qualify("variant", inputs.VariantCode),
            Qualify("model", inputs.ModelCode),
            Qualify("make", inputs.MakeCode),
            Qualify("category", inputs.CategoryCode),
        ];

        foreach (var candidate in candidates)
        {
            if (definition.Factor(ValuationFactors.Baseline, candidate)?.AmountMinorUnits is { } amount)
            {
                return Money.FromMinorUnits(amount, currency);
            }
        }

        return ValuationErrors.BaselineUnknown(inputs.CategoryCode);
    }

    private static string? Qualify(string prefix, string? code)
        => string.IsNullOrWhiteSpace(code) ? null : $"{prefix}:{code}";

    /// <summary>
    /// Applies one multiplier and records the rupee difference it made.
    /// </summary>
    /// <remarks>
    /// A null multiplier means the dimension does not apply — no city premium is authored for this
    /// city, no odometer reading was supplied. Nothing is recorded, because a ledger line reading
    /// "City: ₹0" invites the reader to wonder what went wrong.
    /// <para>
    /// Rounding goes through <c>Money.MultiplyByRate</c>, so it is banker's rounding at every step,
    /// the same as every fee the platform charges. Rounding differently here would make a valuation
    /// and a commission on the same amount disagree by a rupee, which is a reconciliation ticket.
    /// </para>
    /// </remarks>
    private static Money Apply(
        Money running,
        List<AdjustmentLine> ledger,
        string factorCode,
        string? matchValue,
        string label,
        decimal? multiplier)
    {
        if (multiplier is not { } rate || rate == 1m)
        {
            return running;
        }

        var delta = running.MultiplyByRate(rate - 1m);
        ledger.Add(new AdjustmentLine(factorCode, matchValue, label, rate, delta.MinorUnits));

        return running + delta;
    }

    /// <summary>
    /// Looks a factor up, optionally falling back to a catch-all row.
    /// </summary>
    /// <remarks>
    /// The fallback exists for open-ended dimensions. Owner count is the one that matters: an
    /// operator authors rows for one, two and three owners and a <c>max</c> row for everything
    /// beyond, rather than a row per integer to infinity. It is an explicit second lookup rather
    /// than inference — the engine never guesses that "5" should be treated like "3", because the
    /// day a category wants a different shape that guess would be wrong and invisible.
    /// </remarks>
    private static Money ApplyFactor(
        Money running,
        List<AdjustmentLine> ledger,
        ValuationModelDefinition definition,
        string factorCode,
        string? matchValue,
        string? fallbackMatchValue = null)
    {
        if (matchValue is null)
        {
            return running;
        }

        var factor = definition.Factor(factorCode, matchValue)
            ?? (fallbackMatchValue is null ? null : definition.Factor(factorCode, fallbackMatchValue));

        return factor?.Multiplier is null
            ? running
            : Apply(running, ledger, factorCode, matchValue, factor.Label ?? factorCode, factor.Multiplier);
    }

    /// <summary>The owner count as an exact match value. Beyond the authored rows, <c>max</c> applies.</summary>
    private static string? OwnerKey(ValuationInputs inputs)
        => inputs.OwnerSerial is { } owners ? owners.ToString(CultureInfo.InvariantCulture) : null;

    /// <summary>
    /// Turns the anchor into the seven prices, and refuses to publish them out of order.
    /// </summary>
    /// <remarks>
    /// <b>The ordering invariant is not decoration.</b> A quick-sale price above a trade-in price, or
    /// a trade-in above the market value, tells a dealer the product is a toy — and it is the exact
    /// symptom of a spread row edited without thinking about its neighbours. The engine sorts by
    /// authored spread and clamps any amount that would break monotonicity.
    /// <para>
    /// It clamps by spread order rather than by channel name, which is what keeps this method from
    /// knowing that <c>quick_sale</c> or <c>dealer</c> exist. An eighth channel inserted between two
    /// existing ones is ordered correctly the moment its spread is authored.
    /// </para>
    /// </remarks>
    private List<ChannelAmount> BuildChannels(
        ValuationModelDefinition definition,
        Money market,
        ValuationCurve retentionCurve,
        decimal? retentionToday,
        int? ageMonths)
    {
        var priced = new List<(ValuationChannelSpread Spread, Money Amount)>();

        foreach (var channel in definition.Channels)
        {
            var amount = channel.IsForecast
                ? Project(market, retentionCurve, retentionToday, ageMonths, definition.ResaleHorizonMonths)
                  .MultiplyByRate(1m + channel.Spread)
                : market.MultiplyByRate(1m + channel.Spread);

            priced.Add((channel, amount));
        }

        // Forecast channels answer a different question — tomorrow's price — so they are outside the
        // monotonic chain. Including them would clamp an expected resale up to today's trade-in,
        // which is precisely the number a seller is trying to compare against.
        var chain = priced
            .Where(p => !p.Spread.IsForecast)
            .OrderBy(p => p.Spread.Spread)
            .ToList();

        var clamped = 0;
        for (var i = 1; i < chain.Count; i++)
        {
            if (chain[i].Amount >= chain[i - 1].Amount)
            {
                continue;
            }

            chain[i] = (chain[i].Spread, chain[i - 1].Amount);
            clamped++;
        }

        if (clamped > 0)
        {
            // A violation is a configuration bug, not a business case. Serving a clamped set keeps
            // the quote coherent; the log is what gets the spread rows fixed.
            _logger.LogError(
                "Valuation model {Model} produced {Count} channel amounts out of order; clamped to preserve ordering",
                definition.ModelVersion, clamped);
        }

        var byChannel = chain.ToDictionary(p => p.Spread.Id, p => p.Amount);

        return [.. priced
            .Select(p => new ChannelAmount(
                p.Spread.ChannelCode,
                p.Spread.Label,
                byChannel.TryGetValue(p.Spread.Id, out var adjusted) ? adjusted.MinorUnits : p.Amount.MinorUnits,
                p.Spread.IsPrimary,
                p.Spread.SortOrder))
            .OrderBy(c => c.SortOrder)];
    }

    /// <summary>
    /// The market value re-evaluated at a future age, using the same retention curve.
    /// </summary>
    /// <remarks>
    /// Ratio of retentions rather than a separate forecast curve, so a change to the depreciation
    /// shape moves today's price and tomorrow's together. Two curves would eventually disagree, and
    /// the disagreement would show as a car worth more in six months than it is now.
    /// </remarks>
    private static Money Project(
        Money market,
        ValuationCurve retentionCurve,
        decimal? retentionToday,
        int? ageMonths,
        int horizonMonths)
    {
        if (ageMonths is not { } age || retentionToday is not { } today || today <= 0m)
        {
            return market;
        }

        var future = retentionCurve.ValueAt(age + horizonMonths);

        return future is null ? market : market.MultiplyByRate(future.Value / today);
    }

    /// <summary>
    /// The seventh output: what the car is worth every month from here to the model's horizon.
    /// </summary>
    /// <remarks>
    /// Every point carries a band, because a value three years out without one is the most
    /// confidently wrong chart the product could ship. The band widens only if an operator has
    /// authored a <c>forecast_band_width</c> curve — the engine will not invent a decay rate it has
    /// no evidence for.
    /// </remarks>
    private static List<ForecastPoint> BuildForecast(
        ValuationModelDefinition definition,
        Money market,
        ValuationCurve retentionCurve,
        decimal? retentionToday,
        int? ageMonths,
        decimal todayBandWidth)
    {
        var points = new List<ForecastPoint>();

        if (ageMonths is not { } age || retentionToday is not { } today || today <= 0m)
        {
            return points;
        }

        var widthCurve = definition.Curve(ValuationCurves.ForecastBandWidth);

        for (var month = 1; month <= definition.ForecastMonths; month++)
        {
            var future = retentionCurve.ValueAt(age + month);

            if (future is null)
            {
                continue;
            }

            var value = market.MultiplyByRate(future.Value / today);
            var width = widthCurve?.ValueAt(month) ?? todayBandWidth;

            points.Add(new ForecastPoint(
                month,
                value.MinorUnits,
                value.MultiplyByRate(1m - width).MinorUnits,
                value.MultiplyByRate(1m + width).MinorUnits));
        }

        return points;
    }

    /// <summary>
    /// A real confidence score: a weighted sum of signals, renormalised over the ones we could compute.
    /// </summary>
    /// <remarks>
    /// <b>Renormalising rather than scoring a missing signal as zero</b> is the difference between a
    /// score that means something and a decoration. A model that has not authored a
    /// <c>variant_resolution</c> curve should not report every quote as less certain; it should
    /// report certainty over the evidence it actually weighed. Scoring the gap as zero would make
    /// adding a signal look like the model got worse.
    /// </remarks>
    private static decimal ScoreConfidence(
        ValuationModelDefinition definition,
        ValuationInputs inputs,
        int? ageMonths,
        ValuationCurve retentionCurve)
    {
        var signals = new List<(string Code, decimal Value)>
        {
            (ConfidenceInputs.FeatureCompleteness, Completeness(definition, inputs)),
            (ConfidenceInputs.OdometerPresent, inputs.KilometresDriven.HasValue ? 1m : 0m),
            (ConfidenceInputs.ConditionPresent, string.IsNullOrWhiteSpace(inputs.ConditionCode) ? 0m : 1m),
            (ConfidenceInputs.AgeSupport, AgeSupport(retentionCurve, ageMonths)),
        };

        if (definition.Curve(ValuationCurves.VariantResolution) is { } resolution)
        {
            signals.Add((ConfidenceInputs.VariantResolution, resolution.ValueAt(ResolutionLevel(inputs)) ?? 0m));
        }

        var weighted = 0m;
        var totalWeight = 0m;

        foreach (var (code, value) in signals)
        {
            var weight = definition.WeightOf(code);

            if (weight <= 0m)
            {
                continue;
            }

            weighted += weight * Math.Clamp(value, 0m, 1m);
            totalWeight += weight;
        }

        return totalWeight <= 0m ? 0m : Math.Clamp(weighted / totalWeight, 0m, 1m);
    }

    /// <summary>How much of what the model asked for actually arrived.</summary>
    private static decimal Completeness(ValuationModelDefinition definition, ValuationInputs inputs)
    {
        var required = definition.RequiredInputs();

        if (required.Count == 0)
        {
            return 1m;
        }

        var present = required.Count(code => IsPresent(inputs, code));
        return (decimal)present / required.Count;
    }

    /// <summary>
    /// Whether one named input arrived.
    /// </summary>
    /// <remarks>
    /// The named properties are checked by name and everything else falls through to the attribute
    /// bag, which is what lets an operator add <c>service_history</c> to the required list and have
    /// it count without a code change.
    /// </remarks>
    private static bool IsPresent(ValuationInputs inputs, string code) => code switch
    {
        "kilometres_driven" => inputs.KilometresDriven.HasValue,
        "owner_serial" => inputs.OwnerSerial.HasValue,
        "city" => !string.IsNullOrWhiteSpace(inputs.CityCode),
        "fuel_type" => !string.IsNullOrWhiteSpace(inputs.FuelCode),
        "transmission" => !string.IsNullOrWhiteSpace(inputs.TransmissionCode),
        "condition_grade" => !string.IsNullOrWhiteSpace(inputs.ConditionCode),
        "manufacture_year" => inputs.ManufactureYear.HasValue,
        "variant" => !string.IsNullOrWhiteSpace(inputs.VariantCode),
        _ => inputs.Attributes.TryGetPropertyValue(code, out var value) && value is not null,
    };

    /// <summary>
    /// How precisely the vehicle was pinned down, as an ordinal the registry's curve interprets.
    /// </summary>
    /// <remarks>
    /// An ordinal rather than a score, so the <i>value</i> of knowing the exact trim is a curve an
    /// analyst authors — it is worth far more on a ₹40 lakh SUV with six trims than on a scooter with
    /// one.
    /// </remarks>
    private static int ResolutionLevel(ValuationInputs inputs)
    {
        if (!string.IsNullOrWhiteSpace(inputs.VariantCode))
        {
            return 4;
        }

        if (!string.IsNullOrWhiteSpace(inputs.ModelCode))
        {
            return 3;
        }

        return string.IsNullOrWhiteSpace(inputs.MakeCode) ? 1 : 2;
    }

    /// <summary>
    /// One if the age falls inside the curve's authored domain, decaying linearly outside it.
    /// </summary>
    /// <remarks>
    /// A twenty-two-year-old truck priced from a curve authored to fifteen years is an extrapolation
    /// wearing a point estimate. The curve itself clamps rather than extrapolating, so the number
    /// stays sane; this is what tells the seller the number is a guess.
    /// </remarks>
    private static decimal AgeSupport(ValuationCurve retentionCurve, int? ageMonths)
    {
        if (ageMonths is not { } age || retentionCurve.Domain is not { } domain)
        {
            return 0m;
        }

        if (age >= domain.Min && age <= domain.Max)
        {
            return 1m;
        }

        var span = domain.Max - domain.Min;
        if (span <= 0m)
        {
            return 0m;
        }

        var distance = age < domain.Min ? domain.Min - age : age - domain.Max;
        return Math.Clamp(1m - (distance / span), 0m, 1m);
    }

    /// <summary>Age in whole months, preferring the build date over the registration date.</summary>
    /// <remarks>
    /// A 2019-built car registered in January 2020 is priced by the market as a 2019 car, and the
    /// registration date would make it look a year younger — which on a four-year-old hatchback is
    /// tens of thousands of rupees.
    /// </remarks>
    private static int? AgeInMonths(ValuationInputs inputs)
    {
        DateOnly? built = inputs.ManufactureYear is { } year
            ? new DateOnly(year, Math.Clamp(inputs.ManufactureMonth ?? 1, 1, 12), 1)
            : inputs.FirstRegisteredOn;

        if (built is not { } from)
        {
            return null;
        }

        var months = ((inputs.AsOf.Year - from.Year) * 12) + inputs.AsOf.Month - from.Month;
        return Math.Max(months, 0);
    }

    /// <summary>
    /// Fills the model's authored explanation template.
    /// </summary>
    /// <remarks>
    /// The sentence is registry text, exactly like a pricing rule's explanation, because marketing
    /// will rewrite it several times and a translator will need it. The engine only substitutes the
    /// numbers it computed.
    /// </remarks>
    private static string? RenderExplanation(
        ValuationModelDefinition definition,
        ValuationInputs inputs,
        List<AdjustmentLine> ledger)
    {
        if (string.IsNullOrWhiteSpace(definition.ExplanationTemplate))
        {
            return null;
        }

        return definition.ExplanationTemplate
            .Replace("{adjustmentCount}", ledger.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{city}", inputs.CityCode ?? string.Empty, StringComparison.Ordinal)
            .Replace("{modelVersion}", definition.ModelVersion, StringComparison.Ordinal);
    }
}
