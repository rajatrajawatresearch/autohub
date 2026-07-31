using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Valuation.Application.Abstractions;

/// <summary>
/// Everything a model is allowed to see about the vehicle it is pricing.
/// </summary>
/// <remarks>
/// One record for every implementation, and deliberately flat. A model that took the
/// <c>Vehicle</c> aggregate would be a model that could not be run against a hypothetical car — and
/// "what would this be worth with 20,000 fewer kilometres?" is a question the product asks on every
/// valuation page.
/// <para>
/// <see cref="Attributes"/> carries everything the taxonomy registry knows that this record does not
/// name, which is what lets a new attribute become a pricing input without changing this type. The
/// named properties are the ones every model in every category needs; the bag is the long tail.
/// </para>
/// </remarks>
public sealed record ValuationInputs
{
    public Guid? VehicleId { get; init; }

    public Guid? CategoryId { get; init; }

    public required string CategoryCode { get; init; }

    public string? MakeCode { get; init; }

    public string? ModelCode { get; init; }

    public string? VariantCode { get; init; }

    public int? ManufactureYear { get; init; }

    public int? ManufactureMonth { get; init; }

    public DateOnly? FirstRegisteredOn { get; init; }

    /// <summary>Odometer reading. Null is a real state and costs confidence rather than failing.</summary>
    public long? KilometresDriven { get; init; }

    /// <summary>Ordinal owner number from the RC. The first-to-second step is worth far more than later ones.</summary>
    public int? OwnerSerial { get; init; }

    public string? CityCode { get; init; }

    public string? FuelCode { get; init; }

    public string? TransmissionCode { get; init; }

    /// <summary>Taxonomy option value — <c>excellent</c>, <c>good</c>, <c>fair</c>.</summary>
    public string? ConditionCode { get; init; }

    /// <summary>
    /// A known reference price, when the caller has one — an ex-showroom figure or a prior quote.
    /// </summary>
    /// <remarks>
    /// Overrides the registry's baseline lookup. It exists because the taxonomy does not yet carry
    /// launch prices for every variant, and a caller who knows the number should not be forced into
    /// a segment median that is worse.
    /// </remarks>
    public Money? ReferencePrice { get; init; }

    public required DateOnly AsOf { get; init; }

    public JsonObject Attributes { get; init; } = [];

    /// <summary>
    /// The inputs as a document, for the model's applicability condition and for reproducibility.
    /// </summary>
    /// <remarks>
    /// The same document is stored on the valuation, which is what makes a quote re-runnable years
    /// later. Building it here rather than at the two call sites means the stored copy is provably
    /// the one the condition was evaluated against.
    /// </remarks>
    public JsonObject ToJson()
    {
        var json = new JsonObject
        {
            ["categoryCode"] = CategoryCode,
            ["makeCode"] = MakeCode,
            ["modelCode"] = ModelCode,
            ["variantCode"] = VariantCode,
            ["manufactureYear"] = ManufactureYear,
            ["manufactureMonth"] = ManufactureMonth,
            ["firstRegisteredOn"] = FirstRegisteredOn?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ["kilometresDriven"] = KilometresDriven,
            ["ownerSerial"] = OwnerSerial,
            ["cityCode"] = CityCode,
            ["fuelCode"] = FuelCode,
            ["transmissionCode"] = TransmissionCode,
            ["conditionCode"] = ConditionCode,
            ["referencePriceMinorUnits"] = ReferencePrice?.MinorUnits,
            ["asOf"] = AsOf.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ["attributes"] = Attributes.DeepClone(),
        };

        return json;
    }
}

/// <summary>One line of the explanation ledger a model produces.</summary>
public sealed record AdjustmentLine(
    string FactorCode,
    string? MatchValue,
    string Label,
    decimal? Multiplier,
    long DeltaMinorUnits);

/// <summary>One of the seven prices a model produces.</summary>
public sealed record ChannelAmount(string Code, string Label, long AmountMinorUnits, bool IsPrimary, int SortOrder);

/// <summary>One month of the depreciation forecast.</summary>
/// <remarks>
/// Carries a band as well as a point, and the band widens with the horizon: a value 36 months out is
/// a guess dressed as a number unless the uncertainty travels with it. Publishing a bare line would
/// be the most confidently wrong chart in the product.
/// </remarks>
public sealed record ForecastPoint(int Month, long ValueMinorUnits, long LowMinorUnits, long HighMinorUnits);

/// <summary>
/// What a model returns: the seven prices, the ledger that explains them, and how sure it is.
/// </summary>
public sealed record ValuationPrediction
{
    public required string ModelKey { get; init; }

    /// <summary>Key and version, e.g. <c>hedonic-car@3</c>. Reproduces the quote when paired with the inputs.</summary>
    public required string ModelVersion { get; init; }

    public required Currency Currency { get; init; }

    /// <summary>The seven prices. Exactly one is primary and is the anchor.</summary>
    public required IReadOnlyList<ChannelAmount> Channels { get; init; }

    /// <summary>Baseline plus every delta sums to the primary channel's amount. Checked, not assumed.</summary>
    public required IReadOnlyList<AdjustmentLine> Adjustments { get; init; }

    public required long BaselineMinorUnits { get; init; }

    public required long LowMinorUnits { get; init; }

    public required long HighMinorUnits { get; init; }

    public required decimal Confidence { get; init; }

    public required string ConfidenceBand { get; init; }

    /// <summary>Monthly, to the model's forecast horizon. The seventh output.</summary>
    public required IReadOnlyList<ForecastPoint> Forecast { get; init; }

    public required int ValidityDays { get; init; }

    public string? Explanation { get; init; }
}

/// <summary>
/// One way of pricing a vehicle.
/// </summary>
/// <remarks>
/// <b>Why an abstraction with two implementations rather than one good model.</b> The rule-based
/// model works today and needs no data; the ML model needs eighteen months of realised sales that do
/// not exist yet. Building only the first means rewriting every caller when the second arrives;
/// building only the second means no valuation at launch, which is the feature the homepage hero is
/// built around.
/// <para>
/// So both live behind this interface and <b>which one runs is a routing decision in the integration
/// registry</b>, not a branch in code. Promoting the ML model is a route priority change; running it
/// in shadow beside the rule-based one is a config flag; rolling back is the same change in reverse,
/// with no deployment in either direction. That is the same mechanism that swaps a payment gateway,
/// used for the same reason: the decision belongs to whoever is watching the numbers, not to
/// whoever is next to cut a release.
/// </para>
/// <para>
/// <see cref="ModelKey"/> matches the integration registry's provider code, which is what lets a
/// route point at an implementation without a lookup table that could disagree with the container.
/// </para>
/// </remarks>
public interface IValuationModel
{
    /// <summary>Matches <c>IntegrationProvider.Code</c> for the <c>valuation.predict</c> capability.</summary>
    string ModelKey { get; }

    Task<Result<ValuationPrediction>> PredictAsync(ValuationInputs inputs, CancellationToken cancellationToken);
}
