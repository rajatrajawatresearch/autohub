using AutoHub.Modules.Valuation.Domain.Models;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// The coefficients the hedonic valuation model runs on, as rows.
/// </summary>
/// <remarks>
/// Without this the module is a working engine with nothing to run — <c>POST /valuation/quotes</c>
/// answers <c>valuation.model.not_configured</c> for every vehicle on the platform, which is the
/// homepage hero returning an error on a freshly migrated database. The engine deliberately refuses
/// to invent a depreciation curve, so somebody has to author one, and this is that authoring in its
/// starting state.
/// <para>
/// <b>Three definitions rather than one, because three segments depreciate differently and no single
/// curve is honest about all of them.</b> A passenger car keeps roughly 78% of its value through the
/// first year and flattens after five. A two-wheeler falls faster and further — the resale market is
/// thinner and a five-year-old commuter is worth about a third of new. A commercial vehicle
/// depreciates on <i>use</i> more than on age, so its curve is shallower and its kilometre penalty is
/// far steeper. Selection is by JsonLogic on the category, exactly as pricing rules select, so a
/// fourth segment is an insert.
/// </para>
/// <para>
/// <b>The numbers are published-market shapes, not measurements, and they are staging values.</b>
/// They are approximately right for the Indian used market in 2026 and they are the first thing a
/// pricing analyst will overwrite once realised sales exist. That is the entire argument for them
/// being rows: the correction is a form, not a release. Every one of them is authored where an
/// analyst will look for it rather than distributed through a service.
/// </para>
/// <para>
/// <b>Baselines are ex-showroom-shaped amounts in paise</b>, authored per category and per model.
/// The engine resolves most specific first — variant, model, make, then category — so a car whose
/// exact trim nobody recorded still prices, at a cost in confidence rather than a refusal, which on
/// real RC data is the majority case.
/// </para>
/// </remarks>
internal sealed class ValuationModelSeeder
{
    private const int SeededVersion = 1;

    /// <summary>
    /// The inputs each model declares it wants, feeding the completeness confidence signal.
    /// </summary>
    /// <remarks>
    /// Declared rather than inferred from what the engine reads, because the two are different
    /// questions: the engine can read a transmission code, and whether a missing one should cost a
    /// hatchback confidence is a judgement about the market. A commercial vehicle asks for the
    /// odometer and not the transmission; a two-wheeler asks for neither transmission nor condition
    /// grade, because nobody inspects a scooter.
    /// </remarks>
    private const string CarInputs =
        "manufacture_year,kilometres_driven,owner_serial,city,fuel_type,transmission,condition_grade,variant";

    private const string TwoWheelerInputs = "manufacture_year,kilometres_driven,owner_serial,city";

    private const string CommercialInputs =
        "manufacture_year,kilometres_driven,owner_serial,city,fuel_type,condition_grade";

    private readonly AutoHubDbContext _context;
    private readonly IClock _clock;

    public ValuationModelSeeder(AutoHubDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.ValuationModels
            .Select(model => model.Key)
            .ToListAsync(cancellationToken);

        var known = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var build in Models)
        {
            var model = build();

            if (known.Contains(model.Key))
            {
                continue;
            }

            // Published through the aggregate rather than by setting a flag, so the seeder is subject
            // to the same validation an admin publish is: exactly one primary channel, an age curve
            // with something to interpolate between, confidence weights that sum to one. A model that
            // would have priced every car at zero fails here, at boot, naming the problem.
            var published = model.Publish(_clock.UtcNow);

            if (published.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seeded valuation model '{model.Key}' cannot be published — {published.Error}");
            }

            _context.ValuationModels.Add(model);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SeedStepResult("valuation_models", created, known.Count);
    }

    private static readonly Func<ValuationModelDefinition>[] Models =
    [
        BuildPassengerCar,
        BuildTwoWheeler,
        BuildCommercial,
    ];

    // ── Passenger cars ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The default model: hatchbacks, sedans, SUVs, MUVs, luxury cars and electric cars.
    /// </summary>
    /// <remarks>
    /// Priority 10 — the lowest of the three — because its condition is the broadest. Two-wheelers
    /// and commercial vehicles override it by being more specific and saying so, which is the same
    /// authored-specificity rule the pricing registry uses rather than an inferred one.
    /// </remarks>
    private static ValuationModelDefinition BuildPassengerCar()
    {
        var model = ValuationModelDefinition.Draft("hedonic-car", SeededVersion, "Passenger car depreciation");

        model.Configure(
            conditionJson: """
                {"in":[{"var":"categoryCode"},["car","hatchback","sedan","suv","muv","luxury","ev","taxi"]]}
                """,
            priority: 10,
            validityDays: 7,
            forecastMonths: 36,
            resaleHorizonMonths: 12,
            explanationTemplate:
                "Based on {adjustmentCount} adjustments to the segment baseline for this car, "
                + "priced for {city}. Model {modelVersion}.",
            requiredInputsCsv: CarInputs);

        // Retention against age in months. The shape is the point: a cliff through year one, a
        // gentler slope to year five, then a long flat tail. An exponential cannot do all three.
        model.AddCurve(
            ValuationCurves.AgeRetention,
            (0, 1.00m), (3, 0.90m), (6, 0.86m), (12, 0.78m), (24, 0.68m), (36, 0.59m),
            (48, 0.52m), (60, 0.45m), (84, 0.34m), (120, 0.22m), (180, 0.11m));

        // Usage, against the odometer. Centred at 1.0 near the Indian average of about 10,000 km a
        // year, so a car driven normally is neither rewarded nor punished for it.
        model.AddCurve(
            ValuationCurves.KilometrePenalty,
            (0, 1.08m), (20_000, 1.04m), (40_000, 1.00m), (60_000, 0.96m),
            (80_000, 0.92m), (120_000, 0.85m), (200_000, 0.74m));

        AddSharedCurves(model, lowConfidenceWidth: 0.16m, highConfidenceWidth: 0.055m);

        model.AddFactor(ValuationFactors.OwnerCount, "1", 1.00m, label: "First owner");
        model.AddFactor(ValuationFactors.OwnerCount, "2", 0.955m, label: "Second owner");
        model.AddFactor(ValuationFactors.OwnerCount, "3", 0.92m, label: "Third owner");
        model.AddFactor(ValuationFactors.OwnerCount, ValuationFactors.OverflowMatchValue, 0.88m,
            label: "Fourth owner or later");

        model.AddFactor(ValuationFactors.ConditionGrade, "excellent", 1.06m, label: "Excellent condition");
        model.AddFactor(ValuationFactors.ConditionGrade, "good", 1.00m, label: "Good condition");
        model.AddFactor(ValuationFactors.ConditionGrade, "fair", 0.93m, label: "Fair condition");
        model.AddFactor(ValuationFactors.ConditionGrade, "poor", 0.84m, label: "Needs work");

        // Diesel carries a discount because the NCR ten-year rule and its imitators cap the resale
        // life of a diesel car in the biggest markets. CNG carries a premium for the opposite reason.
        model.AddFactor(ValuationFactors.FuelType, "petrol", 1.00m, label: "Petrol");
        model.AddFactor(ValuationFactors.FuelType, "diesel", 0.97m, label: "Diesel");
        model.AddFactor(ValuationFactors.FuelType, "cng", 1.02m, label: "CNG");
        model.AddFactor(ValuationFactors.FuelType, "ev", 0.94m, label: "Electric");
        model.AddFactor(ValuationFactors.FuelType, "hybrid", 1.01m, label: "Hybrid");

        model.AddFactor(ValuationFactors.Transmission, "manual", 1.00m, label: "Manual");
        model.AddFactor(ValuationFactors.Transmission, "automatic", 1.03m, label: "Automatic");
        model.AddFactor(ValuationFactors.Transmission, "amt", 1.01m, label: "AMT");
        model.AddFactor(ValuationFactors.Transmission, "cvt", 1.02m, label: "CVT");
        model.AddFactor(ValuationFactors.Transmission, "dct", 1.03m, label: "DCT");

        AddCityFactors(model);
        AddCarBaselines(model);

        return model;
    }

    /// <summary>
    /// Segment and model baselines, in paise.
    /// </summary>
    /// <remarks>
    /// Both levels are authored because they answer different requests. A model-level row prices the
    /// Swift a seller actually typed in; the category row prices the car whose model nobody recorded,
    /// which is what the homepage hero gets from a plate alone. Without the category row that request
    /// is refused, and refusing the anonymous path is refusing the top of the funnel.
    /// </remarks>
    private static void AddCarBaselines(ValuationModelDefinition model)
    {
        Baseline(model, "category:hatchback", 7_20_000_00);
        Baseline(model, "category:sedan", 12_50_000_00);
        Baseline(model, "category:suv", 15_80_000_00);
        Baseline(model, "category:muv", 16_40_000_00);
        Baseline(model, "category:luxury", 52_00_000_00);
        Baseline(model, "category:ev", 16_90_000_00);
        Baseline(model, "category:car", 9_60_000_00);
        Baseline(model, "category:taxi", 9_20_000_00);

        Baseline(model, "model:swift", 8_20_000_00);
        Baseline(model, "model:baleno", 8_90_000_00);
        Baseline(model, "model:wagon-r", 6_40_000_00);
        Baseline(model, "model:i20", 9_10_000_00);
        Baseline(model, "model:altroz", 8_60_000_00);
        Baseline(model, "model:glanza", 8_40_000_00);

        Baseline(model, "model:dzire", 8_80_000_00);
        Baseline(model, "model:amaze", 9_20_000_00);
        Baseline(model, "model:city", 14_60_000_00);
        Baseline(model, "model:verna", 14_90_000_00);

        Baseline(model, "model:nexon", 12_20_000_00);
        Baseline(model, "model:punch", 9_40_000_00);
        Baseline(model, "model:brezza", 12_80_000_00);
        Baseline(model, "model:venue", 12_10_000_00);
        Baseline(model, "model:sonet", 12_00_000_00);
        Baseline(model, "model:magnite", 9_80_000_00);
        Baseline(model, "model:kiger", 9_90_000_00);
        Baseline(model, "model:xuv300", 11_60_000_00);
        Baseline(model, "model:creta", 17_20_000_00);
        Baseline(model, "model:seltos", 17_40_000_00);
        Baseline(model, "model:taigun", 16_90_000_00);
        Baseline(model, "model:kushaq", 16_40_000_00);
        Baseline(model, "model:hector", 18_60_000_00);
        Baseline(model, "model:harrier", 21_40_000_00);
        Baseline(model, "model:thar", 16_80_000_00);
        Baseline(model, "model:scorpio-n", 19_60_000_00);
        Baseline(model, "model:xuv700", 24_80_000_00);
        Baseline(model, "model:fortuner", 44_50_000_00);

        Baseline(model, "model:ertiga", 12_90_000_00);
        Baseline(model, "model:carens", 13_40_000_00);
        Baseline(model, "model:innova-crysta", 22_60_000_00);

        Baseline(model, "model:nexon-ev", 17_40_000_00);
        Baseline(model, "model:tiago-ev", 11_20_000_00);
        Baseline(model, "model:e6", 29_60_000_00);

        Baseline(model, "model:3-series", 52_00_000_00);
        Baseline(model, "model:a4", 51_50_000_00);
        Baseline(model, "model:c-class", 61_00_000_00);
    }

    // ── Two-wheelers ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bikes and scooters, which depreciate faster and further than cars.
    /// </summary>
    /// <remarks>
    /// The separate model exists because reusing the car curve would overstate a five-year-old
    /// commuter by roughly a third. The resale market is thinner, the buyer is more price-sensitive,
    /// and a new equivalent is cheap enough that a used one has to be much cheaper to be worth the
    /// risk — none of which the car curve's long flat tail expresses.
    /// </remarks>
    private static ValuationModelDefinition BuildTwoWheeler()
    {
        var model = ValuationModelDefinition.Draft("hedonic-two-wheeler", SeededVersion, "Two-wheeler depreciation");

        model.Configure(
            conditionJson: """
                {"in":[{"var":"categoryCode"},["bike","scooter","commuter","cruiser","sports"]]}
                """,
            priority: 20,
            validityDays: 7,
            forecastMonths: 24,
            resaleHorizonMonths: 12,
            explanationTemplate:
                "Based on {adjustmentCount} adjustments to the segment baseline for this two-wheeler. "
                + "Model {modelVersion}.",
            requiredInputsCsv: TwoWheelerInputs);

        model.AddCurve(
            ValuationCurves.AgeRetention,
            (0, 1.00m), (6, 0.82m), (12, 0.72m), (24, 0.60m),
            (36, 0.50m), (60, 0.34m), (96, 0.20m), (144, 0.10m));

        // A two-wheeler's expected annual distance is far lower, so the curve is centred lower and
        // falls away sooner. Applying the car curve here would treat 30,000 km as barely used.
        model.AddCurve(
            ValuationCurves.KilometrePenalty,
            (0, 1.06m), (10_000, 1.02m), (20_000, 1.00m),
            (35_000, 0.94m), (50_000, 0.88m), (80_000, 0.78m));

        AddSharedCurves(model, lowConfidenceWidth: 0.20m, highConfidenceWidth: 0.08m);

        model.AddFactor(ValuationFactors.OwnerCount, "1", 1.00m, label: "First owner");
        model.AddFactor(ValuationFactors.OwnerCount, "2", 0.94m, label: "Second owner");
        model.AddFactor(ValuationFactors.OwnerCount, ValuationFactors.OverflowMatchValue, 0.87m,
            label: "Third owner or later");

        model.AddFactor(ValuationFactors.ConditionGrade, "excellent", 1.07m, label: "Excellent condition");
        model.AddFactor(ValuationFactors.ConditionGrade, "good", 1.00m, label: "Good condition");
        model.AddFactor(ValuationFactors.ConditionGrade, "fair", 0.91m, label: "Fair condition");
        model.AddFactor(ValuationFactors.ConditionGrade, "poor", 0.80m, label: "Needs work");

        model.AddFactor(ValuationFactors.FuelType, "petrol", 1.00m, label: "Petrol");
        model.AddFactor(ValuationFactors.FuelType, "ev", 0.90m, label: "Electric");

        AddCityFactors(model);

        Baseline(model, "category:bike", 1_15_000_00);
        Baseline(model, "category:scooter", 88_000_00);
        Baseline(model, "category:commuter", 82_000_00);
        Baseline(model, "category:cruiser", 2_10_000_00);
        Baseline(model, "category:sports", 1_45_000_00);

        Baseline(model, "model:splendor-plus", 78_000_00);
        Baseline(model, "model:pulsar-150", 1_18_000_00);
        Baseline(model, "model:classic-350", 2_05_000_00);
        Baseline(model, "model:activa-6g", 82_000_00);
        Baseline(model, "model:jupiter", 79_000_00);
        Baseline(model, "model:ather-450x", 1_52_000_00);
        Baseline(model, "model:ola-s1-pro", 1_35_000_00);

        return model;
    }

    // ── Commercial and construction ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Trucks, buses, pickups, tempos and construction equipment.
    /// </summary>
    /// <remarks>
    /// The segment where usage outweighs age, and the reason its curves are the mirror of the car's:
    /// a shallow age curve and a steep kilometre curve. A goods vehicle is a working asset whose
    /// value tracks the life left in its driveline, so a six-year-old truck with 90,000 km is worth
    /// considerably more than a four-year-old one with 400,000 — an ordering the car model gets
    /// backwards.
    /// <para>
    /// Construction equipment sits here rather than in its own model because it shares that
    /// property. Its baseline is authored separately; its shape is not different enough to justify a
    /// fourth definition until somebody has data saying otherwise.
    /// </para>
    /// </remarks>
    private static ValuationModelDefinition BuildCommercial()
    {
        var model = ValuationModelDefinition.Draft("hedonic-commercial", SeededVersion, "Commercial vehicle depreciation");

        model.Configure(
            conditionJson: """
                {"in":[{"var":"categoryCode"},["commercial","truck","bus","pickup","tempo","construction"]]}
                """,
            priority: 20,
            validityDays: 5,
            forecastMonths: 36,
            resaleHorizonMonths: 12,
            explanationTemplate:
                "Based on {adjustmentCount} adjustments to the segment baseline for this vehicle, "
                + "weighted towards distance covered. Model {modelVersion}.",
            requiredInputsCsv: CommercialInputs);

        model.AddCurve(
            ValuationCurves.AgeRetention,
            (0, 1.00m), (12, 0.84m), (24, 0.74m), (36, 0.66m),
            (60, 0.52m), (96, 0.36m), (144, 0.21m), (216, 0.10m));

        // Steep, and authored out to half a million kilometres, because that is the working life of
        // a goods vehicle and the range real requests arrive in.
        model.AddCurve(
            ValuationCurves.KilometrePenalty,
            (0, 1.12m), (50_000, 1.04m), (100_000, 1.00m),
            (200_000, 0.88m), (300_000, 0.77m), (500_000, 0.58m));

        AddSharedCurves(model, lowConfidenceWidth: 0.22m, highConfidenceWidth: 0.09m);

        model.AddFactor(ValuationFactors.OwnerCount, "1", 1.00m, label: "First owner");
        model.AddFactor(ValuationFactors.OwnerCount, "2", 0.96m, label: "Second owner");
        model.AddFactor(ValuationFactors.OwnerCount, ValuationFactors.OverflowMatchValue, 0.91m,
            label: "Third owner or later");

        model.AddFactor(ValuationFactors.ConditionGrade, "excellent", 1.05m, label: "Excellent condition");
        model.AddFactor(ValuationFactors.ConditionGrade, "good", 1.00m, label: "Good condition");
        model.AddFactor(ValuationFactors.ConditionGrade, "fair", 0.92m, label: "Fair condition");
        model.AddFactor(ValuationFactors.ConditionGrade, "poor", 0.81m, label: "Needs work");

        // Diesel is the working default in this segment rather than the penalised choice it is in a
        // car, which is why the same fuel code carries a different multiplier in a different model —
        // and why fuel adjustments are per-model rows and not one global table.
        model.AddFactor(ValuationFactors.FuelType, "diesel", 1.00m, label: "Diesel");
        model.AddFactor(ValuationFactors.FuelType, "cng", 0.97m, label: "CNG");
        model.AddFactor(ValuationFactors.FuelType, "ev", 0.92m, label: "Electric");
        model.AddFactor(ValuationFactors.FuelType, "petrol", 0.95m, label: "Petrol");

        AddCityFactors(model);

        Baseline(model, "category:commercial", 14_50_000_00);
        Baseline(model, "category:truck", 24_80_000_00);
        Baseline(model, "category:bus", 21_40_000_00);
        Baseline(model, "category:pickup", 9_80_000_00);
        Baseline(model, "category:tempo", 6_20_000_00);
        Baseline(model, "category:construction", 34_50_000_00);

        Baseline(model, "model:dost", 8_60_000_00);
        Baseline(model, "model:ace-gold", 5_80_000_00);
        Baseline(model, "model:bolero-pickup", 9_40_000_00);
        Baseline(model, "model:pro-2049", 19_80_000_00);
        Baseline(model, "model:boss-1115", 26_40_000_00);
        Baseline(model, "model:traveller-26", 21_60_000_00);
        Baseline(model, "model:3dx-backhoe", 34_50_000_00);

        return model;
    }

    // ── Shared authoring ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The parts every model needs and none of them should differ on without a reason.
    /// </summary>
    /// <remarks>
    /// The band-width curve is the one that is not optional: without it the engine refuses to
    /// publish, because a quote with a point and no range claims a precision no depreciation model
    /// has. The two ends are parameters rather than constants here because the honest width does
    /// differ by segment — a commercial vehicle's price is genuinely less certain than a hatchback's.
    /// <para>
    /// The channel spreads are the PRD's structural differentiator: the same car is worth different
    /// amounts through different channels, and the gap between them is the information a seller came
    /// for. <c>market</c> is the anchor and everything else is signed relative to it.
    /// </para>
    /// </remarks>
    private static void AddSharedCurves(
        ValuationModelDefinition model,
        decimal lowConfidenceWidth,
        decimal highConfidenceWidth)
    {
        // Half-width of the published range against the confidence score. Falling rather than rising:
        // the less we know, the wider the honest band.
        model.AddCurve(
            ValuationCurves.ConfidenceBandWidth,
            (0.0m, lowConfidenceWidth),
            (0.5m, (lowConfidenceWidth + highConfidenceWidth) / 2m),
            (0.8m, highConfidenceWidth * 1.35m),
            (1.0m, highConfidenceWidth));

        // Uncertainty grows with the horizon. Authored, because the rate at which a used-vehicle
        // forecast decays is a measured quantity and the engine will not guess one.
        model.AddCurve(
            ValuationCurves.ForecastBandWidth,
            (1, highConfidenceWidth * 1.2m),
            (12, highConfidenceWidth * 1.9m),
            (24, highConfidenceWidth * 2.6m),
            (36, highConfidenceWidth * 3.2m));

        // How much knowing the exact trim is worth, against the resolution ordinal the engine
        // computes: 1 category only, 2 make, 3 model, 4 variant.
        model.AddCurve(
            ValuationCurves.VariantResolution,
            (1, 0.25m), (2, 0.50m), (3, 0.80m), (4, 1.00m));

        // Must sum to 1, and the aggregate refuses to publish otherwise — a score outside its own
        // scale reads as "low" for every quote, which is worse than no score.
        model.AddConfidenceWeight(ConfidenceInputs.FeatureCompleteness, 0.30m);
        model.AddConfidenceWeight(ConfidenceInputs.VariantResolution, 0.25m);
        model.AddConfidenceWeight(ConfidenceInputs.AgeSupport, 0.20m);
        model.AddConfidenceWeight(ConfidenceInputs.OdometerPresent, 0.15m);
        model.AddConfidenceWeight(ConfidenceInputs.ConditionPresent, 0.10m);

        model.AddConfidenceBand("low", 0.00m, 1);
        model.AddConfidenceBand("medium", 0.55m, 2);
        model.AddConfidenceBand("high", 0.78m, 3);

        model.AddChannel("market", "Market value", 0.00m, isPrimary: true, sortOrder: 1);
        model.AddChannel("dealer", "Dealer offer", -0.07m, sortOrder: 2);
        model.AddChannel("auction", "Auction estimate", -0.09m, sortOrder: 3);
        model.AddChannel("trade", "Trade value", -0.13m, sortOrder: 4);
        model.AddChannel("quick_sale", "Quick sale", -0.18m, sortOrder: 5);
        model.AddChannel("insurance_idv", "Insured value", -0.05m, sortOrder: 6);
        model.AddChannel("expected_resale", "Expected resale in a year", 0.00m, sortOrder: 7, isForecast: true);
    }

    /// <summary>
    /// City multipliers, for the cities the geography registry serves.
    /// </summary>
    /// <remarks>
    /// Shared across all three models because the effect is a property of the market rather than of
    /// the vehicle: registration cost, buyer density and how far a car travels to be sold move every
    /// segment in the same direction. Where that stops being true the row is per-model already, so
    /// splitting it is an edit rather than a redesign.
    /// </remarks>
    private static void AddCityFactors(ValuationModelDefinition model)
    {
        model.AddFactor(ValuationFactors.City, "pune", 1.02m, label: "Pune market");
        model.AddFactor(ValuationFactors.City, "mumbai", 1.04m, label: "Mumbai market");
        model.AddFactor(ValuationFactors.City, "bengaluru", 1.03m, label: "Bengaluru market");
        model.AddFactor(ValuationFactors.City, "delhi", 0.98m, label: "Delhi market");
        model.AddFactor(ValuationFactors.City, "hyderabad", 1.01m, label: "Hyderabad market");
        model.AddFactor(ValuationFactors.City, "chennai", 1.00m, label: "Chennai market");
        model.AddFactor(ValuationFactors.City, "kolkata", 0.97m, label: "Kolkata market");
        model.AddFactor(ValuationFactors.City, "ahmedabad", 0.99m, label: "Ahmedabad market");
        model.AddFactor(ValuationFactors.City, "jaipur", 0.98m, label: "Jaipur market");
        model.AddFactor(ValuationFactors.City, "nagpur", 0.99m, label: "Nagpur market");
    }

    /// <summary>An amount factor rather than a multiplier — the price the chain starts from.</summary>
    private static void Baseline(ValuationModelDefinition model, string matchValue, long amountMinorUnits)
        => model.AddFactor(
            ValuationFactors.Baseline,
            matchValue,
            multiplier: null,
            amountMinorUnits: amountMinorUnits,
            label: "Baseline");
}
