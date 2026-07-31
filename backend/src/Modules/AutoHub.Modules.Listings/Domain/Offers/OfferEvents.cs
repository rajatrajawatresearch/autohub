using AutoHub.SharedKernel.Domain;

namespace AutoHub.Modules.Listings.Domain.Offers;

/// <summary>A buyer opened a negotiation on a listing.</summary>
/// <remarks>
/// Notifications turns this into the seller's push; analytics counts it as the strongest demand
/// signal a listing produces, well ahead of a view or a saved search.
/// </remarks>
public sealed record OfferPlaced(
    Guid OfferId,
    Guid ListingId,
    Guid BuyerPartyId,
    long AmountMinorUnits,
    string Currency,
    DateTimeOffset OccurredOn)
    : DomainEvent(OfferId, OccurredOn)
{
    public override string EventType => "listings.offer_placed.v1";
}

/// <summary>One side answered with a different number.</summary>
public sealed record OfferCountered(
    Guid OfferId,
    Guid ListingId,
    OfferSide Side,
    long AmountMinorUnits,
    string Currency,
    DateTimeOffset OccurredOn)
    : DomainEvent(OfferId, OccurredOn)
{
    public override string EventType => "listings.offer_countered.v1";
}
