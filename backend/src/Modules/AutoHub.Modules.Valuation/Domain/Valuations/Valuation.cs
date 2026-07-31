using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Valuation.Domain.Valuations;

/// <summary>One of the seven prices, as stored.</summary>
public sealed class ValuationChannelAmount : Entity
{
    internal ValuationChannelAmount(
        Guid valuationId,
        string channelCode,
        string label,
        long amountMinorUnits,
        bool isPrimary,
        int sortOrder)
        : base(UuidV7.New())
    {
        ValuationId = valuationId;
        ChannelCode = channelCode;
        Label = label;
        AmountMinorUnits = amountMinorUnits;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    private ValuationChannelAmount() { }

    public Guid ValuationId { get; private set; }

    public string ChannelCode { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public long AmountMinorUnits { get; private set; }

    public bool IsPrimary { get; private set; }

    public int SortOrder { get; private set; }
}

/// <summary>
/// One signed line of the explanation ledger.
/// </summary>
/// <remarks>
/// The baseline plus every delta sums exactly to the market price, which is the property that makes
/// a quote defensible. A seller who is told "₹5.49 lakh" and nothing else does not believe it; a
/// seller who is told "₹8.20 lakh new, less ₹2.10 lakh for four years, less ₹38,000 for 62,000 km,
/// less ₹23,000 for a second owner" believes it and argues about the parts they disagree with —
/// which is a conversation, not a bounce.
/// </remarks>
public sealed class ValuationAdjustment : Entity
{
    internal ValuationAdjustment(
        Guid valuationId,
        string factorCode,
        string? matchValue,
        string label,
        decimal? multiplier,
        long deltaMinorUnits,
        int sortOrder)
        : base(UuidV7.New())
    {
        ValuationId = valuationId;
        FactorCode = factorCode;
        MatchValue = matchValue;
        Label = label;
        Multiplier = multiplier;
        DeltaMinorUnits = deltaMinorUnits;
        SortOrder = sortOrder;
    }

    private ValuationAdjustment() { }

    public Guid ValuationId { get; private set; }

    /// <summary>Which dimension moved the number — <c>age_retention</c>, <c>owner_count</c>, <c>city</c>.</summary>
    public string FactorCode { get; private set; } = string.Empty;

    /// <summary>The value that matched — <c>2</c>, <c>pune</c>, <c>diesel</c>. Null for curve factors.</summary>
    public string? MatchValue { get; private set; }

    public string Label { get; private set; } = string.Empty;

    /// <summary>The ratio applied, for an operator reconciling a quote against the registry.</summary>
    public decimal? Multiplier { get; private set; }

    /// <summary>Signed, in minor units. Negative for anything that takes value off.</summary>
    public long DeltaMinorUnits { get; private set; }

    public int SortOrder { get; private set; }
}

/// <summary>
/// A price for a vehicle, at a moment, from a named model — immutable once written.
/// </summary>
/// <remarks>
/// <b>Immutability is the whole design.</b> A valuation is an opinion the platform published, and the
/// instant-offer product turns one of its channels into a binding commitment. Both mean the same
/// thing: any quote must be reproducible years later, byte for byte, from its stored inputs and its
/// model version. A row that can be updated cannot make that promise, and the first time a seller
/// disputes a number the only honest answer would be "we do not know what we told you".
/// <para>
/// So there is no <c>Reprice</c> method and no setter after construction. A new opinion is a new
/// row, which also gives the price history that the analytics module needs and that a re-quote
/// against a changed model would otherwise destroy.
/// </para>
/// <para>
/// <b>Nothing here is a status.</b> Whether a quote has expired is arithmetic against
/// <see cref="ExpiresAt"/>; whether an offer built on it was accepted is the deal's workflow. A
/// status column would be a third answer to a question two things already answer.
/// </para>
/// </remarks>
public sealed class Valuation : AggregateRoot, IAuditable
{
    private readonly List<ValuationChannelAmount> _channels = [];
    private readonly List<ValuationAdjustment> _adjustments = [];

    private Valuation(Guid id, string modelKey, string modelVersion, string currencyCode) : base(id)
    {
        ModelKey = modelKey;
        ModelVersion = modelVersion;
        CurrencyCode = currencyCode;
    }

    private Valuation() { }

    /// <summary>The canonical vehicle, when one could be resolved. Null for a speculative quote.</summary>
    public Guid? VehicleId { get; private set; }

    /// <summary>Canonical registration as submitted, kept even when no vehicle resolved.</summary>
    /// <remarks>
    /// Without it, an anonymous homepage quote for a plate we could not identify leaves no trace, and
    /// "how many people asked about cars we cannot price?" — the single most useful coverage metric
    /// this module produces — is unanswerable.
    /// </remarks>
    public string? RegistrationCanonical { get; private set; }

    public Guid? CategoryId { get; private set; }

    /// <summary>Model key alone, for grouping quotes by model in analytics.</summary>
    public string ModelKey { get; private set; } = string.Empty;

    /// <summary>Key and version, e.g. <c>hedonic-car@3</c>. What the API publishes.</summary>
    public string ModelVersion { get; private set; } = string.Empty;

    public string CurrencyCode { get; private set; } = string.Empty;

    /// <summary>The anchor — the primary channel's amount, in minor units.</summary>
    public long PointMinorUnits { get; private set; }

    public long LowMinorUnits { get; private set; }

    public long HighMinorUnits { get; private set; }

    /// <summary>Zero to one. A real score, computed from weighted signals, not a decoration.</summary>
    public decimal Confidence { get; private set; }

    /// <summary>Band code — <c>high</c>, <c>medium</c>, <c>low</c>. Localised at the edge.</summary>
    public string ConfidenceBandCode { get; private set; } = string.Empty;

    /// <summary>
    /// The exact inputs the model saw, verbatim.
    /// </summary>
    /// <remarks>
    /// This column is what makes reproducibility real rather than aspirational. Re-deriving the
    /// inputs from today's vehicle row would give a different answer the moment anything about the
    /// vehicle changed — which for a car being sold is immediately.
    /// </remarks>
    public string InputsJson { get; private set; } = "{}";

    /// <summary>The monthly depreciation forecast, as an authored JSON array of points.</summary>
    /// <remarks>
    /// JSON rather than rows because it is a chart series, read whole and never queried into. Thirty-six
    /// rows per quote, across millions of quotes, would be a table whose only access pattern is
    /// "give me all of them for this id".
    /// </remarks>
    public string? ForecastJson { get; private set; }

    /// <summary>The sentence shown under the number, rendered from the model's template.</summary>
    public string? Explanation { get; private set; }

    /// <summary>The date the quote speaks for. A date, not an instant — the market moves in days.</summary>
    public DateOnly AsOf { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Whether this run was a shadow comparison rather than the answer that was served.
    /// </summary>
    /// <remarks>
    /// Shadow runs are stored, never returned. Promoting a new model is a routing change, and the
    /// evidence for making it is weeks of shadow quotes sitting next to the served ones — which only
    /// exists if they were persisted at the time.
    /// </remarks>
    public bool IsShadow { get; private set; }

    public IReadOnlyList<ValuationChannelAmount> Channels => _channels;

    public IReadOnlyList<ValuationAdjustment> Adjustments => _adjustments;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public Money Point => Money.FromMinorUnits(PointMinorUnits, Currency.FromCode(CurrencyCode));

    public bool HasExpired(DateTimeOffset at) => at >= ExpiresAt;

    /// <summary>
    /// Records a completed model run.
    /// </summary>
    /// <remarks>
    /// Everything arrives at once because a half-written valuation is not a valuation — a row with a
    /// point estimate and no channels would be served to a seller as a single number, which is
    /// exactly the product the PRD says we are not building.
    /// </remarks>
    public static Result<Valuation> Record(
        Guid? vehicleId,
        string? registrationCanonical,
        Guid? categoryId,
        string modelKey,
        string modelVersion,
        Currency currency,
        IReadOnlyList<(string ChannelCode, string Label, long AmountMinorUnits, bool IsPrimary, int SortOrder)> channels,
        IReadOnlyList<(string FactorCode, string? MatchValue, string Label, decimal? Multiplier, long DeltaMinorUnits)> adjustments,
        long lowMinorUnits,
        long highMinorUnits,
        decimal confidence,
        string confidenceBandCode,
        JsonObject inputs,
        JsonArray? forecast,
        string? explanation,
        DateOnly asOf,
        DateTimeOffset expiresAt,
        bool isShadow = false)
    {
        var primary = channels.FirstOrDefault(c => c.IsPrimary);

        if (primary.ChannelCode is null)
        {
            return Error.Unexpected(
                "valuation.no_primary_channel",
                "The model produced no primary channel, so there is no price to publish.");
        }

        if (lowMinorUnits > highMinorUnits)
        {
            return Error.Unexpected(
                "valuation.range_inverted",
                "The model produced a range whose floor is above its ceiling.")
                .With("low", lowMinorUnits)
                .With("high", highMinorUnits);
        }

        var valuation = new Valuation(UuidV7.New(), modelKey, modelVersion, currency.Code)
        {
            VehicleId = vehicleId,
            RegistrationCanonical = registrationCanonical,
            CategoryId = categoryId,
            PointMinorUnits = primary.AmountMinorUnits,
            LowMinorUnits = lowMinorUnits,
            HighMinorUnits = highMinorUnits,
            // Clamped rather than rejected. A confidence outside its own scale is a model bug worth
            // an alert, not a reason to fail a seller's request for a price.
            Confidence = Math.Clamp(confidence, 0m, 1m),
            ConfidenceBandCode = confidenceBandCode,
            InputsJson = inputs.ToJsonString(),
            ForecastJson = forecast?.ToJsonString(),
            Explanation = explanation,
            AsOf = asOf,
            ExpiresAt = expiresAt,
            IsShadow = isShadow,
        };

        foreach (var (code, label, amount, isPrimary, sortOrder) in channels)
        {
            valuation._channels.Add(
                new ValuationChannelAmount(valuation.Id, code, label, amount, isPrimary, sortOrder));
        }

        var order = 0;
        foreach (var (factorCode, matchValue, label, multiplier, delta) in adjustments)
        {
            valuation._adjustments.Add(
                new ValuationAdjustment(valuation.Id, factorCode, matchValue, label, multiplier, delta, order++));
        }

        if (!isShadow)
        {
            valuation.Raise(new ValuationProduced(
                valuation.Id, vehicleId, primary.AmountMinorUnits, currency.Code, modelVersion,
                expiresAt, DateTime.SpecifyKind(asOf.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)));
        }

        return valuation;
    }

    public JsonObject Inputs()
        => JsonNode.Parse(InputsJson) as JsonObject ?? [];

    public JsonArray Forecast()
        => string.IsNullOrWhiteSpace(ForecastJson) ? [] : JsonNode.Parse(ForecastJson) as JsonArray ?? [];

    /// <summary>
    /// One serializer configuration for the forecast, shared by whoever writes and reads it.
    /// </summary>
    /// <remarks>
    /// Shared for the same reason the form registry shares one: two near-identical configurations
    /// eventually differ in a casing flag, and a chart that silently deserialises to zeros is a
    /// depreciation curve that says a car becomes worthless immediately.
    /// </remarks>
    public static JsonSerializerOptions ForecastJsonOptions { get; } = new(JsonSerializerDefaults.Web);

    /// <summary>The <c>asOf</c> date in the invariant form the API publishes.</summary>
    public string AsOfIso => AsOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
