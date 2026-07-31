using AutoHub.Platform.Expressions;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Valuation.Domain.Models;

/// <summary>
/// Every coefficient the rule-based valuation model runs on, as data.
/// </summary>
/// <remarks>
/// <b>This is the registry the prime directive demands for pricing intelligence.</b> A hedonic
/// depreciation model is nothing but coefficients — an age curve, a kilometre curve, an owner
/// penalty, a city multiplier, a fuel adjustment, a condition grade, and the spreads between the
/// seven channels. Written as constants they would be twenty-odd numbers scattered across a service,
/// and every one of them is something a pricing analyst revises monthly against realised sale data.
/// Written here they are rows, versioned, published and audited like every other registry in the
/// platform.
/// <para>
/// <b>Published definitions are immutable and versioned</b>, for the same reason workflow
/// definitions are: a quote has to be reproducible years later from its inputs and its model
/// version, because a seller who was quoted ₹5.49L and sold for ₹5.10L will eventually ask why, and
/// "the coefficients have changed since" is not an answer anybody accepts. Editing a live model
/// would make every historical quote unreproducible in one <c>UPDATE</c>.
/// </para>
/// <para>
/// <b>Several definitions coexist.</b> Each carries a JsonLogic condition over the valuation's
/// inputs, so commercial vehicles can depreciate on a different curve from hatchbacks and Kerala can
/// price differently from Delhi — with the most specific matching definition winning by priority.
/// This is the same selection mechanism the pricing registry uses, deliberately, because an operator
/// who has learned one should not have to learn two.
/// </para>
/// </remarks>
public sealed class ValuationModelDefinition : AggregateRoot, IAuditable
{
    private readonly List<ValuationCurve> _curves = [];
    private readonly List<ValuationFactor> _factors = [];
    private readonly List<ValuationChannelSpread> _channels = [];
    private readonly List<ConfidenceWeight> _confidenceWeights = [];
    private readonly List<ConfidenceBand> _confidenceBands = [];

    private ValuationModelDefinition(Guid id, string key, int version, string name) : base(id)
    {
        Key = key;
        Version = version;
        Name = name;
    }

    private ValuationModelDefinition() { }

    /// <summary>
    /// Stable identifier, e.g. <c>hedonic-car</c>. Combined with <see cref="Version"/> it is the
    /// <c>modelVersion</c> the API publishes and a quote is reproduced from.
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    public int Version { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>JsonLogic over the valuation inputs. Null means it applies to everything.</summary>
    public string? ConditionJson { get; private set; }

    /// <summary>Higher wins. Specificity is authored, not inferred — see the pricing registry.</summary>
    public int Priority { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>
    /// How long a quote stays honest, in days.
    /// </summary>
    /// <remarks>
    /// A business duration, so it is a column rather than a constant: it shortens in a volatile
    /// market and lengthens in a flat one, and the instant-offer product commits real money against
    /// it.
    /// </remarks>
    public int ValidityDays { get; private set; } = 7;

    /// <summary>Months the depreciation forecast projects forward, at monthly granularity.</summary>
    public int ForecastMonths { get; private set; } = 36;

    /// <summary>
    /// The horizon, in months, the <c>expected_resale</c> channel answers for.
    /// </summary>
    /// <remarks>
    /// One number rather than the full set, because the channel publishes a single amount. The whole
    /// set of horizons is readable off <see cref="ForecastMonths"/> of monthly points, which is the
    /// same data at finer resolution and avoids a second list to keep in step.
    /// </remarks>
    public int ResaleHorizonMonths { get; private set; } = 12;

    /// <summary>
    /// The sentence shown under the number, with <c>{placeholders}</c> the engine fills.
    /// </summary>
    /// <remarks>
    /// Registry text rather than a compiled string, exactly like <c>PricingRule.Explanation</c>: a
    /// seller who cannot see why they were quoted a number does not trust it, and the wording of that
    /// explanation is something marketing will rewrite several times before launch.
    /// </remarks>
    public string? ExplanationTemplate { get; private set; }

    /// <summary>Input codes this model needs, comma-separated. Feeds the completeness confidence signal.</summary>
    public string? RequiredInputsCsv { get; private set; }

    public IReadOnlyList<ValuationCurve> Curves => _curves;

    public IReadOnlyList<ValuationFactor> Factors => _factors;

    public IReadOnlyList<ValuationChannelSpread> Channels => _channels;

    public IReadOnlyList<ConfidenceWeight> ConfidenceWeights => _confidenceWeights;

    public IReadOnlyList<ConfidenceBand> ConfidenceBands => _confidenceBands;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    /// <summary>What the API publishes as <c>modelVersion</c>, e.g. <c>hedonic-car@3</c>.</summary>
    public string ModelVersion => $"{Key}@{Version}";

    public static ValuationModelDefinition Draft(string key, int version, string name)
        => new(UuidV7.New(), key, version, name);

    public ValuationCurve? Curve(string code)
        => _curves.Find(c => string.Equals(c.Code, code, StringComparison.Ordinal));

    /// <summary>
    /// The factor for one value of one dimension, or null when none is authored.
    /// </summary>
    /// <remarks>
    /// Null rather than a neutral 1.0, so the caller can tell "no adjustment applies" from "this
    /// dimension was not configured". Only one of those is worth an alert.
    /// </remarks>
    public ValuationFactor? Factor(string factorCode, string? matchValue)
        => matchValue is null
            ? null
            : _factors.Find(f =>
                string.Equals(f.FactorCode, factorCode, StringComparison.Ordinal)
                && string.Equals(f.MatchValue, matchValue, StringComparison.OrdinalIgnoreCase));

    public ValuationChannelSpread? PrimaryChannel => _channels.Find(c => c.IsPrimary);

    public IReadOnlyList<string> RequiredInputs()
        => string.IsNullOrWhiteSpace(RequiredInputsCsv)
            ? []
            : RequiredInputsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>The band a score falls into. Highest matching threshold wins.</summary>
    public ConfidenceBand? BandFor(decimal score)
        => _confidenceBands
            .Where(b => score >= b.MinimumScore)
            .OrderByDescending(b => b.MinimumScore)
            .FirstOrDefault();

    public decimal WeightOf(string inputCode)
        => _confidenceWeights.Find(w => string.Equals(w.InputCode, inputCode, StringComparison.Ordinal))?.Weight ?? 0m;

    /// <summary>
    /// Adds a curve with its points, in one call.
    /// </summary>
    /// <remarks>
    /// The points travel with the curve rather than being appended afterwards because a curve with no
    /// points is not a partially-built curve, it is a broken one: <c>ValueAt</c> returns null for it
    /// and the engine reads that as "this dimension does not apply", so an age curve left empty is a
    /// car that never depreciates. Taking both at once removes the window in which that state exists.
    /// <para>
    /// This is also why <c>ValuationCurve.AddPoint</c> stays internal. Every other child of this
    /// aggregate — factors, channels, weights, bands — is added through the root, because the root is
    /// the consistency boundary and <see cref="Validate"/> is the thing that gets to judge the result.
    /// </para>
    /// </remarks>
    public ValuationCurve AddCurve(string code, params (decimal X, decimal Y)[] points)
    {
        var curve = new ValuationCurve(Id, code);

        foreach (var (x, y) in points)
        {
            curve.AddPoint(x, y);
        }

        _curves.Add(curve);
        return curve;
    }

    public void AddFactor(
        string factorCode,
        string matchValue,
        decimal? multiplier = null,
        long? amountMinorUnits = null,
        string? label = null)
    {
        var factor = new ValuationFactor(Id, factorCode, matchValue, multiplier, amountMinorUnits);
        factor.Describe(label);
        _factors.Add(factor);
    }

    public void AddChannel(
        string channelCode,
        string label,
        decimal spread,
        bool isPrimary = false,
        int sortOrder = 0,
        bool isForecast = false)
    {
        var channel = new ValuationChannelSpread(Id, channelCode, label, spread, isPrimary, sortOrder);

        if (isForecast)
        {
            channel.MarkForecast();
        }

        _channels.Add(channel);
    }

    public void AddConfidenceWeight(string inputCode, decimal weight)
        => _confidenceWeights.Add(new ConfidenceWeight(Id, inputCode, weight));

    public void AddConfidenceBand(string code, decimal minimumScore, int sortOrder = 0)
        => _confidenceBands.Add(new ConfidenceBand(Id, code, minimumScore, sortOrder));

    public void Configure(
        string? conditionJson,
        int priority,
        int validityDays,
        int forecastMonths,
        int resaleHorizonMonths,
        string? explanationTemplate,
        string? requiredInputsCsv)
    {
        ConditionJson = conditionJson;
        Priority = priority;
        ValidityDays = validityDays;
        ForecastMonths = forecastMonths;
        ResaleHorizonMonths = resaleHorizonMonths;
        ExplanationTemplate = explanationTemplate;
        RequiredInputsCsv = requiredInputsCsv;
    }

    /// <summary>Whether this definition applies to a given set of valuation inputs.</summary>
    /// <remarks>
    /// An unparseable condition does not match. The safe direction for a valuation is "this model
    /// does not apply"; the alternative is pricing every car in the country under a rule nobody
    /// authored, which is the same argument the pricing engine makes about fees.
    /// </remarks>
    public bool Matches(System.Text.Json.Nodes.JsonNode context)
    {
        var condition = ConditionExpression.FromJson(ConditionJson);
        return condition.IsSuccess && condition.Value.Evaluate(context);
    }

    /// <summary>
    /// Proves the model can actually produce a number before anything depends on it.
    /// </summary>
    /// <remarks>
    /// Every check is a failure a customer would otherwise see. No primary channel means no anchor
    /// and therefore no price; two primaries means the anchor depends on row order; a missing age
    /// curve means a car that never depreciates; confidence weights that do not sum to one produce a
    /// score outside its own scale, which the band lookup then reads as "low" for every quote.
    /// </remarks>
    public Result<IReadOnlyList<string>> Validate()
    {
        var problems = new List<string>();

        var primaries = _channels.Count(c => c.IsPrimary);
        if (primaries != 1)
        {
            problems.Add($"Expected exactly one primary channel, found {primaries}.");
        }

        if (Curve(ValuationCurves.AgeRetention) is not { } age || age.Points.Count < 2)
        {
            problems.Add("The age retention curve needs at least two points to interpolate between.");
        }

        if (Curve(ValuationCurves.ConfidenceBandWidth) is not { } band || band.Points.Count == 0)
        {
            // Without it every quote would publish a point estimate with no range, which is the one
            // presentation the PRD explicitly rules out — a single number implies a precision this
            // model does not have.
            problems.Add("No confidence band-width curve: quotes would publish a point with no range.");
        }

        if (_confidenceBands.Count == 0)
        {
            problems.Add("No confidence bands: every quote would publish a score with no label.");
        }

        var weightTotal = _confidenceWeights.Sum(w => w.Weight);
        if (_confidenceWeights.Count > 0 && Math.Abs(weightTotal - 1m) > 0.001m)
        {
            problems.Add($"Confidence weights sum to {weightTotal}, not 1.");
        }

        if (!_factors.Exists(f => string.Equals(f.FactorCode, ValuationFactors.Baseline, StringComparison.Ordinal)))
        {
            problems.Add("No baseline factor: the model has no amount to start from.");
        }

        if (ConditionExpression.FromJson(ConditionJson).IsFailure)
        {
            problems.Add("The applicability condition will not parse.");
        }

        return problems.Count == 0
            ? Result.Success<IReadOnlyList<string>>([])
            : Error.Validation("valuation.model.invalid", "This valuation model cannot be published.")
                .With("problems", problems);
    }

    public Result Publish(DateTimeOffset now)
    {
        if (IsPublished)
        {
            return Error.Conflict("valuation.model.already_published", "This version is already published.");
        }

        var validation = Validate();
        if (validation.IsFailure)
        {
            return Result.Failure(validation.Error);
        }

        IsPublished = true;
        PublishedAt = now;
        return Result.Success();
    }
}
