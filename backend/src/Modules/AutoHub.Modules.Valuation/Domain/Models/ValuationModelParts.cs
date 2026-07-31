using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Valuation.Domain.Models;

/// <summary>
/// One point on an authored curve — an age in months, a kilometre reading, a confidence score.
/// </summary>
/// <remarks>
/// <see cref="X"/> is deliberately untyped in meaning: the curve's code says what it is. That is what
/// lets one table hold the age-retention curve, the kilometre-penalty curve and the confidence-band
/// curve without three schemas and three admin screens.
/// </remarks>
public sealed class CurvePoint
{
    internal CurvePoint(Guid curveId, decimal x, decimal y)
    {
        CurveId = curveId;
        X = x;
        Y = y;
    }

    private CurvePoint() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid CurveId { get; private set; }

    public decimal X { get; private set; }

    /// <summary>The value at <see cref="X"/>. For a retention curve, a fraction of the baseline.</summary>
    public decimal Y { get; private set; }
}

/// <summary>
/// A piecewise-linear function stored as data.
/// </summary>
/// <remarks>
/// <b>Why a curve rather than a formula.</b> The obvious depreciation model is
/// <c>value = base × (1 − rate)^years</c>, which is one number in code and wrong in a specific,
/// expensive way: an Indian used car loses far more in its first twelve months than an exponential
/// admits, flattens between years three and seven, and falls off again at the ten-year diesel and
/// fifteen-year petrol re-registration cliffs. No single exponent fits that shape, and every attempt
/// to make one fit is a pricing team arguing with a compiled constant.
/// <para>
/// Authored points with linear interpolation between them fit any shape a pricing analyst can draw,
/// and changing the shape is editing rows. The interpolation itself is arithmetic, which the prime
/// directive explicitly allows; the <i>shape</i> is data, which it explicitly demands.
/// </para>
/// <para>
/// <b>Outside the authored domain the curve clamps rather than extrapolates.</b> A linear
/// extrapolation past the last point eventually produces a negative value, and a 22-year-old truck
/// priced at minus three lakh is a number that reaches a customer before it reaches a test.
/// </para>
/// </remarks>
public sealed class ValuationCurve
{
    private readonly List<CurvePoint> _points = [];

    internal ValuationCurve(Guid modelDefinitionId, string code)
    {
        ModelDefinitionId = modelDefinitionId;
        Code = code;
    }

    private ValuationCurve() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid ModelDefinitionId { get; private set; }

    /// <summary>What the curve is for — <c>age_retention</c>, <c>km_penalty</c>, <c>confidence_band</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public IReadOnlyList<CurvePoint> Points => _points;

    internal void AddPoint(decimal x, decimal y) => _points.Add(new CurvePoint(Id, x, y));

    /// <summary>
    /// The value at <paramref name="x"/>, interpolated between the two nearest authored points.
    /// </summary>
    /// <remarks>
    /// Returns null for a curve with no points, so the caller can decide whether a missing curve is
    /// a fatal misconfiguration or a factor that simply does not apply. Silently returning 1.0 would
    /// make a missing age curve look like a car that never depreciates.
    /// </remarks>
    public decimal? ValueAt(decimal x)
    {
        if (_points.Count == 0)
        {
            return null;
        }

        var ordered = _points.OrderBy(p => p.X).ToList();

        if (x <= ordered[0].X)
        {
            return ordered[0].Y;
        }

        if (x >= ordered[^1].X)
        {
            return ordered[^1].Y;
        }

        for (var i = 1; i < ordered.Count; i++)
        {
            if (x > ordered[i].X)
            {
                continue;
            }

            var left = ordered[i - 1];
            var right = ordered[i];
            var span = right.X - left.X;

            // Two points authored at the same x would divide by zero. Taking the left value is the
            // conservative reading of a duplicate row and keeps a data-entry slip out of the maths.
            return span == 0m ? left.Y : left.Y + ((x - left.X) * (right.Y - left.Y) / span);
        }

        return ordered[^1].Y;
    }

    /// <summary>The authored domain, used to judge how far a request sits outside the training range.</summary>
    public (decimal Min, decimal Max)? Domain
        => _points.Count == 0 ? null : (_points.Min(p => p.X), _points.Max(p => p.X));
}

/// <summary>
/// A named multiplier or amount, selected by matching a value.
/// </summary>
/// <remarks>
/// This is the lookup table behind "second owner costs 4%", "Pune trades 3% above the national
/// number", "diesel carries a discount in NCR". Each is a row, keyed by
/// <see cref="FactorCode"/> and <see cref="MatchValue"/>, and changing one is a form rather than a
/// release — which matters because these are exactly the numbers a pricing team revises monthly
/// against realised sale data.
/// <para>
/// <see cref="AmountMinorUnits"/> exists so the same table can hold the baseline price a model starts
/// from. A baseline is an amount and a penalty is a ratio; forcing both into one column would mean
/// one of them lies about its units, and the units of money are the one thing this codebase refuses
/// to leave ambiguous.
/// </para>
/// </remarks>
public sealed class ValuationFactor
{
    internal ValuationFactor(
        Guid modelDefinitionId,
        string factorCode,
        string matchValue,
        decimal? multiplier,
        long? amountMinorUnits)
    {
        ModelDefinitionId = modelDefinitionId;
        FactorCode = factorCode;
        MatchValue = matchValue;
        Multiplier = multiplier;
        AmountMinorUnits = amountMinorUnits;
    }

    private ValuationFactor() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid ModelDefinitionId { get; private set; }

    /// <summary>Which dimension — <c>city</c>, <c>owner_count</c>, <c>fuel_type</c>, <c>baseline</c>.</summary>
    public string FactorCode { get; private set; } = string.Empty;

    /// <summary>The value it matches — <c>pune</c>, <c>2</c>, <c>diesel</c>, <c>model:swift</c>.</summary>
    public string MatchValue { get; private set; } = string.Empty;

    /// <summary>Fraction of the running value: 0.96 takes 4% off. Null for an amount factor.</summary>
    public decimal? Multiplier { get; private set; }

    /// <summary>Minor units, for factors that supply an amount rather than a ratio.</summary>
    public long? AmountMinorUnits { get; private set; }

    /// <summary>Default English label for the adjustment line. Localised at the edge by code.</summary>
    public string? Label { get; private set; }

    internal void Describe(string? label) => Label = label;
}

/// <summary>
/// One of the seven prices, expressed as a spread from the market anchor.
/// </summary>
/// <remarks>
/// The PRD's structural differentiator is that the same car is worth different amounts through
/// different channels, and that the spread between them is the information a seller is actually
/// trying to buy. Modelling each channel as a row means adding an eighth — a bank's repossession
/// reserve, say — is an insert plus a label, not a new field on a response three clients parse.
/// <para>
/// <see cref="Spread"/> is signed and relative: <c>+0.11</c> for a dealer's retail price,
/// <c>-0.14</c> for a trade-in. Exactly one row per model carries <see cref="IsPrimary"/>, and that
/// row is the anchor every other one is computed from.
/// </para>
/// </remarks>
public sealed class ValuationChannelSpread
{
    internal ValuationChannelSpread(
        Guid modelDefinitionId,
        string channelCode,
        string label,
        decimal spread,
        bool isPrimary,
        int sortOrder)
    {
        ModelDefinitionId = modelDefinitionId;
        ChannelCode = channelCode;
        Label = label;
        Spread = spread;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    private ValuationChannelSpread() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid ModelDefinitionId { get; private set; }

    /// <summary>Machine code — <c>market</c>, <c>trade</c>, <c>dealer</c>, <c>auction</c>, …</summary>
    public string ChannelCode { get; private set; } = string.Empty;

    /// <summary>Default English label. The user-facing string comes from the i18n registry.</summary>
    public string Label { get; private set; } = string.Empty;

    public decimal Spread { get; private set; }

    /// <summary>The anchor. Exactly one per model, checked before the model can be published.</summary>
    public bool IsPrimary { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>
    /// Whether this channel is answered by projecting the retention curve forward.
    /// </summary>
    /// <remarks>
    /// True for <c>expected_resale</c>, which is not a spread on today's price but tomorrow's price:
    /// the market value re-evaluated at <c>age + horizon</c>. Flagging it here rather than matching on
    /// the channel code is what keeps the engine from knowing any channel by name.
    /// </remarks>
    public bool IsForecast { get; private set; }

    internal void MarkForecast() => IsForecast = true;
}

/// <summary>One input to the confidence score, and how much it counts.</summary>
/// <remarks>
/// Weights are rows because the score is a claim about how much a seller should trust the number,
/// and the right weighting is something only realised-sale data can settle — which arrives months
/// after launch. A weight in code is a weight that stays at its launch-day guess.
/// </remarks>
public sealed class ConfidenceWeight
{
    internal ConfidenceWeight(Guid modelDefinitionId, string inputCode, decimal weight)
    {
        ModelDefinitionId = modelDefinitionId;
        InputCode = inputCode;
        Weight = weight;
    }

    private ConfidenceWeight() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid ModelDefinitionId { get; private set; }

    /// <summary>Which signal — <c>feature_completeness</c>, <c>variant_resolution</c>, <c>age_support</c>.</summary>
    public string InputCode { get; private set; } = string.Empty;

    public decimal Weight { get; private set; }
}

/// <summary>
/// A band label and the score at which it starts.
/// </summary>
/// <remarks>
/// The client renders "high", "medium" or "low" next to the number, and the thresholds between them
/// are a product decision that will be argued about. A row makes the argument a form; a constant
/// makes it a release.
/// </remarks>
public sealed class ConfidenceBand
{
    internal ConfidenceBand(Guid modelDefinitionId, string code, decimal minimumScore, int sortOrder)
    {
        ModelDefinitionId = modelDefinitionId;
        Code = code;
        MinimumScore = minimumScore;
        SortOrder = sortOrder;
    }

    private ConfidenceBand() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid ModelDefinitionId { get; private set; }

    /// <summary>Machine code — <c>high</c>, <c>medium</c>, <c>low</c>. Never shown; localised by key.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Inclusive lower bound. The highest matching band wins.</summary>
    public decimal MinimumScore { get; private set; }

    public int SortOrder { get; private set; }
}

/// <summary>The codes the engine looks for. Technical keys, not business vocabulary.</summary>
/// <remarks>
/// These name the <i>slots</i> the engine reads, exactly as <c>WorkflowEffect.EffectCode</c> names
/// the handlers a workflow may invoke. What goes in each slot — how steep the age curve is, which
/// cities carry a premium, how much a second owner costs — is entirely data. What the engine knows
/// how to do with a slot is code, because each one is a branch with a test behind it.
/// </remarks>
public static class ValuationCurves
{
    /// <summary>Fraction of the baseline retained at an age in months.</summary>
    public const string AgeRetention = "age_retention";

    /// <summary>Multiplier for a kilometre reading, relative to expected use.</summary>
    public const string KilometrePenalty = "km_penalty";

    /// <summary>Half-width of the published range, as a fraction, against the confidence score.</summary>
    public const string ConfidenceBandWidth = "confidence_band_width";

    /// <summary>
    /// Half-width of a forecast point's band, as a fraction, against months into the future.
    /// </summary>
    /// <remarks>
    /// Optional. Without it the forecast carries the same band at month 36 as at month 1, which is a
    /// conservative understatement of uncertainty rather than an invented widening coefficient — the
    /// rate at which a used-car forecast decays is exactly the kind of number that has to be measured
    /// rather than guessed, so the engine refuses to guess it.
    /// </remarks>
    public const string ForecastBandWidth = "forecast_band_width";

    /// <summary>Confidence contribution of how precisely the vehicle was identified.</summary>
    public const string VariantResolution = "variant_resolution";
}

/// <summary>The factor dimensions the engine applies, in the order it applies them.</summary>
public static class ValuationFactors
{
    /// <summary>Starting amount. Matched most specific first: variant, then model, then category.</summary>
    public const string Baseline = "baseline";

    public const string OwnerCount = "owner_count";

    public const string City = "city";

    public const string FuelType = "fuel_type";

    public const string ConditionGrade = "condition_grade";

    public const string Transmission = "transmission";

    /// <summary>
    /// The catch-all match value for an open-ended dimension.
    /// </summary>
    /// <remarks>
    /// Owner count is the case it exists for: an operator authors rows for one, two and three owners
    /// and this one for everything beyond, instead of a row per integer. The engine tries the exact
    /// value first and this one second — it never infers that five should behave like three.
    /// </remarks>
    public const string OverflowMatchValue = "max";
}

/// <summary>Confidence input codes the engine can compute today.</summary>
public static class ConfidenceInputs
{
    /// <summary>Fraction of the model's declared required inputs that arrived.</summary>
    public const string FeatureCompleteness = "feature_completeness";

    /// <summary>How precisely the vehicle was pinned down — variant beats model beats category.</summary>
    public const string VariantResolution = "variant_resolution";

    /// <summary>Whether the age falls inside the authored domain of the retention curve.</summary>
    public const string AgeSupport = "age_support";

    /// <summary>Whether an odometer reading was supplied at all.</summary>
    public const string OdometerPresent = "odometer_present";

    /// <summary>Whether a condition grade was supplied — an inspection outranks a seller's opinion.</summary>
    public const string ConditionPresent = "condition_present";
}

/// <summary>Guards a curve or factor lookup that the model cannot proceed without.</summary>
internal static class ValuationErrors
{
    public static Error MissingCurve(string code)
        => Error.Unexpected(
            "valuation.model.curve_missing",
            $"The valuation model has no '{code}' curve, so it cannot produce a number.")
            .With("curve", code);

    public static Error BaselineUnknown(string categoryCode)
        => Error.Validation(
            "valuation.insufficient_inputs",
            "There is not enough information about this vehicle to price it.")
            .With("reason", "baseline_unknown")
            .With("categoryCode", categoryCode);
}
