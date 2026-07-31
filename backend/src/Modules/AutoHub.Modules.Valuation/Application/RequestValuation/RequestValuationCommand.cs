using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Valuation.Application.RequestValuation;

/// <summary>
/// The homepage hero's request — §10 of the API contract, plus what the sell flow already knows.
/// </summary>
/// <remarks>
/// <b>Everything is optional except the intent to be priced.</b> The contract's example carries a
/// registration number, an odometer reading, a city and a condition, and that is the anonymous hero
/// path: no auth, no account, no phone number, because asking for one before showing a number is
/// exactly why users bounce.
/// <para>
/// The classification fields exist for the callers that already have them — the sell flow after the
/// seller has picked a variant, a dealer's bulk upload, an auction lot import. A caller with a
/// category can have a vehicle created; a caller with only a plate we have never seen gets
/// <c>valuation.insufficient_inputs</c> rather than a fabricated number, which is the PRD's stated
/// position on coverage: widen the band, never invent the point.
/// </para>
/// </remarks>
public sealed record RequestValuationCommand : IRequest<Result<ValuationQuote>>
{
    /// <summary>As typed on the plate. Canonicalised before anything is looked up.</summary>
    public string? RegistrationNumber { get; init; }

    /// <summary>Odometer reading. Absence costs confidence rather than failing the request.</summary>
    public long? KilometresDriven { get; init; }

    /// <summary>Geography registry city code. Scopes the market multiplier.</summary>
    public string? City { get; init; }

    /// <summary>Taxonomy option value — <c>excellent</c>, <c>good</c>, <c>fair</c>.</summary>
    public string? Condition { get; init; }

    /// <summary>
    /// Category code. Supplying it lets an unknown plate become a canonical vehicle.
    /// </summary>
    /// <remarks>
    /// Deliberately the switch between "look up" and "look up or create". Without it an anonymous
    /// browse cannot fill the catalog with cars nobody is selling; with it, the sell flow's first
    /// step creates the vehicle it is about to list.
    /// </remarks>
    public string? CategoryCode { get; init; }

    public string? MakeCode { get; init; }

    public string? ModelCode { get; init; }

    public string? VariantCode { get; init; }

    public int? ManufactureYear { get; init; }

    public int? ManufactureMonth { get; init; }

    /// <summary>Owner number from the RC. The first-to-second step costs far more than later ones.</summary>
    public int? OwnerSerial { get; init; }

    /// <summary>A known ex-showroom or reference price, when the caller has a better one than the registry.</summary>
    public long? ReferencePriceMinorUnits { get; init; }

    /// <summary>
    /// The caller's <c>Idempotency-Key</c>, used as the model-routing affinity key.
    /// </summary>
    /// <remarks>
    /// Without it a seller refreshing a slow page could land on either side of a canary split and see
    /// two different prices for the same car, which destroys the trust the number exists to build.
    /// </remarks>
    public string? IdempotencyKey { get; init; }
}

/// <summary>The vehicle block of the §10 response.</summary>
/// <remarks>
/// <c>source</c> is the provenance code — <c>vahan</c>, <c>rc_ocr</c>, <c>seller</c> — and it is on
/// the contract because it changes how much the number should be trusted. A price built on a
/// seller's own description of their car is a different claim from one built on the RTO's record.
/// </remarks>
public sealed record QuotedVehicle(
    Guid? VehicleId,
    string Title,
    string? RegistrationDisplay,
    int? Year,
    string? Fuel,
    string? Source);

/// <summary>One of the seven prices, as §10 publishes it.</summary>
public sealed record QuotedValuation(string Code, string Label, long AmountMinor, bool IsPrimary);

/// <summary>The published range. Always present — a point estimate alone overstates the precision.</summary>
public sealed record QuotedRange(long LowMinor, long HighMinor);

/// <summary>
/// The confidence block.
/// </summary>
/// <remarks>
/// <c>basedOn</c> is the model's authored explanation rather than a compiled sentence, so marketing
/// can rewrite it and a translator can key off it — the same argument <c>PricingRule.Explanation</c>
/// makes about a fee a dealer will query.
/// </remarks>
public sealed record QuotedConfidence(decimal Score, string Band, string? BasedOn);

/// <summary>One month of the depreciation forecast — the seventh output, as a series.</summary>
public sealed record QuotedForecastPoint(int Month, long ValueMinor, long LowMinor, long HighMinor);

/// <summary>The §10 response, field for field.</summary>
/// <remarks>
/// <c>amountMinor</c> everywhere, never a formatted string: the client renders "₹5.49L" because only
/// the client knows the viewer's locale and the space available. The server sending "₹5.49L" would
/// be a server deciding that a Tamil user reads lakhs.
/// </remarks>
public sealed record ValuationQuote
{
    public required Guid QuoteId { get; init; }

    public required QuotedVehicle Vehicle { get; init; }

    public required string Currency { get; init; }

    public required IReadOnlyList<QuotedValuation> Valuations { get; init; }

    public required QuotedRange Range { get; init; }

    public required QuotedConfidence Confidence { get; init; }

    /// <summary>Monthly depreciation to the model's horizon. Empty when the age is unknown.</summary>
    public required IReadOnlyList<QuotedForecastPoint> Forecast { get; init; }

    /// <summary><c>yyyy-MM-dd</c>. The market moves in days, so the quote speaks for a day.</summary>
    public required string AsOf { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Key and version, e.g. <c>hedonic-car@3</c>. Reproduces the quote with its inputs.</summary>
    public required string ModelVersion { get; init; }
}
