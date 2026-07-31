using AutoHub.Modules.Listings.Contracts;
using AutoHub.SharedKernel.Domain;

namespace AutoHub.Modules.Listings.Domain.Listings;

/// <summary>
/// A listing exists.
/// </summary>
/// <remarks>
/// Named for the fact rather than for the state it lands in. "Drafted" would have read naturally and
/// would have baked the assumption that the initial state is <c>draft</c> into an event name three
/// clients and an outbox consumer depend on — and the initial state is whatever the published
/// workflow says it is, which operations may change without telling this module.
/// </remarks>
public sealed record ListingCreated(
    Guid ListingId,
    Guid VehicleId,
    ListingIntent Intent,
    Guid SellerPartyId,
    DateTimeOffset OccurredOn)
    : DomainEvent(ListingId, OccurredOn)
{
    public override string EventType => "listings.listing_created.v1";
}

/// <summary>
/// The asking price moved.
/// </summary>
/// <remarks>
/// Carries the previous amount as well as the new one, because every consumer needs the delta and
/// only this event has it: notifications sends "price dropped" and not "price changed", analytics
/// tracks reduction depth against days-to-sale, and neither can reconstruct the old value from a
/// row that has already been overwritten.
/// </remarks>
public sealed record ListingRepriced(
    Guid ListingId,
    long PreviousMinorUnits,
    long PriceMinorUnits,
    string Currency,
    DateTimeOffset OccurredOn)
    : DomainEvent(ListingId, OccurredOn)
{
    public override string EventType => "listings.listing_repriced.v1";
}

/// <summary>Media landed on a listing, so the quality score and the search projection are stale.</summary>
public sealed record ListingMediaAttached(Guid ListingId, Guid MediaId, DateTimeOffset OccurredOn)
    : DomainEvent(ListingId, OccurredOn)
{
    public override string EventType => "listings.listing_media_attached.v1";
}

/// <summary>
/// The listing entered a state its workflow marks as publicly visible.
/// </summary>
/// <remarks>
/// Raised by the publish use case after the workflow transition succeeds, never by the transition
/// itself — the workflow engine's own effects are registry-configured and this is a domain fact the
/// module owns. It is what the search indexer subscribes to; the alternative, indexing on every
/// save, would put unreviewed drafts in front of buyers.
/// </remarks>
public sealed record ListingPublished(
    Guid ListingId,
    Guid VehicleId,
    ListingIntent Intent,
    string CityCode,
    DateTimeOffset OccurredOn)
    : DomainEvent(ListingId, OccurredOn)
{
    public override string EventType => "listings.listing_published.v1";
}
