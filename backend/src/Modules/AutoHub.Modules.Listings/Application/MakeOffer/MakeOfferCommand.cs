using AutoHub.Modules.Listings.Domain.Offers;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Listings.Application.MakeOffer;

/// <summary>Opens a C2C negotiation on a listing.</summary>
public sealed record MakeOfferCommand : IRequest<Result<OfferDetail>>
{
    public required Guid ListingId { get; init; }

    /// <summary>The buying party. A party, not a user — a dealer's staff bid on the dealer's behalf.</summary>
    public required Guid BuyerPartyId { get; init; }

    public required long AmountMinorUnits { get; init; }

    public string CurrencyCode { get; init; } = Currency.Inr.Code;

    /// <summary>The note that travels with the offer, and stays on the record with it.</summary>
    public string? Message { get; init; }
}

/// <summary>One move in the chain, as the two parties see it.</summary>
public sealed record OfferTurnView(
    int Sequence,
    OfferSide Side,
    Guid PartyId,
    long AmountMinorUnits,
    string Currency,
    string? Message,
    DateTimeOffset PlacedAt);

/// <summary>
/// A negotiation with its whole history and its current lifecycle state.
/// </summary>
/// <remarks>
/// There is no status field here either. Whether the offer is open, accepted, declined or expired is
/// <see cref="Workflow"/>, resolved for this caller — and <c>Workflow.DueAt</c> is the "expires in
/// four hours" the UI counts down, so the deadline has exactly one definition.
/// </remarks>
public sealed record OfferDetail
{
    public required Guid OfferId { get; init; }

    public required Guid ListingId { get; init; }

    public required Guid BuyerPartyId { get; init; }

    public required Guid SellerPartyId { get; init; }

    /// <summary>The amount currently on the table — what an acceptance would agree to.</summary>
    public required long CurrentAmountMinorUnits { get; init; }

    public required string Currency { get; init; }

    public required IReadOnlyList<OfferTurnView> Turns { get; init; }

    public required WorkflowSnapshot Workflow { get; init; }
}
