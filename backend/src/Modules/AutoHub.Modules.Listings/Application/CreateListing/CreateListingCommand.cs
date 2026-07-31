using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Listings.Application.CreateListing;

/// <summary>
/// Creates a listing from a sell-flow submission.
/// </summary>
/// <remarks>
/// <b>The payload is not typed, and that is the whole design.</b> Which fields a seller fills in is
/// the form schema registry's answer and changes per category, per city and per campaign without a
/// deploy; a typed command would make every one of those changes a backend release, a mobile release
/// and a coordinated rollout. So the payload arrives as a JSON document, the form registry validates
/// it against the version the client rendered, and the taxonomy registry turns the parts of it that
/// are attributes into typed values.
/// <para>
/// The handful of properties that <i>are</i> typed here are the ones the domain model needs to exist
/// at all — a listing without a vehicle, a seller, a price or a city is not a listing — and none of
/// them is ever going to become configurable.
/// </para>
/// </remarks>
public sealed record CreateListingCommand : IRequest<Result<ListingDetail>>
{
    /// <summary>Wire name of the intent: <c>sale</c>, <c>auction</c>, <c>rental</c>, …</summary>
    public required string Intent { get; init; }

    public required string CategoryCode { get; init; }

    /// <summary>Form registry key the seller filled in, e.g. <c>sell_vehicle</c>.</summary>
    public required string FormKey { get; init; }

    /// <summary>The contract's <c>?context=category:suv</c>, selecting the form variant.</summary>
    public string? FormContext { get; init; }

    /// <summary>BCP-47 tag from <c>Accept-Language</c>, so error messages resolve in the seller's language.</summary>
    public string? Locale { get; init; }

    /// <summary>
    /// The whole sell-flow submission, exactly as the renderer produced it.
    /// </summary>
    /// <remarks>
    /// Validated twice, against two different things, and both are needed. The form registry checks
    /// what the seller was <i>asked</i> — required fields, visibility conditions, per-field patterns.
    /// The taxonomy registry checks what the domain will <i>accept</i> — that <c>kms_driven</c> is a
    /// whole number inside the category's bounds. A CSV import that never passes a form still has to
    /// clear the second check.
    /// </remarks>
    public required JsonObject Payload { get; init; }

    /// <summary>How the caller knows the vehicle. Resolved through the catalog's deduplication.</summary>
    public required IReadOnlyList<VehicleIdentifierInput> VehicleIdentifiers { get; init; }

    public required Guid SellerPartyId { get; init; }

    public required string CityCode { get; init; }

    public required long PriceMinorUnits { get; init; }

    /// <summary>
    /// ISO 4217 code. Defaults to the platform's declared base currency rather than a literal.
    /// </summary>
    /// <remarks>
    /// Stated rather than assumed, so the day a second currency exists this is the one line that has
    /// to change — the same reasoning <c>AttributeValueValidator</c> applies to money attributes.
    /// </remarks>
    public string PriceCurrencyCode { get; init; } = Currency.Inr.Code;

    /// <summary>Rate period for rental, lease and attachment — <c>per_day</c>, <c>per_month</c>.</summary>
    public string? PricePeriodCode { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }
}
