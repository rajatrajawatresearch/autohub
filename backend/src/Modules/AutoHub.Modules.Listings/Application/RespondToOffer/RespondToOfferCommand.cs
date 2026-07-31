using AutoHub.Modules.Listings.Application.MakeOffer;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Listings.Application.RespondToOffer;

/// <summary>
/// Answers a negotiation — accept, decline, withdraw, or counter with a number.
/// </summary>
/// <remarks>
/// One command for all four because they are one thing: a workflow transition on the offer, fired by
/// one of the two parties. Four commands would mean four handlers repeating the same identity check
/// and the same engine call, and a fifth outcome — "accept subject to inspection" — would be a fifth
/// handler rather than a transition an operator adds.
/// <para>
/// A counter is the only one that also changes the aggregate, and it does so by appending a turn
/// before the transition fires, so the guard evaluating "is the counter within the allowed spread?"
/// sees the number it is judging.
/// </para>
/// </remarks>
public sealed record RespondToOfferCommand : IRequest<Result<OfferDetail>>
{
    public required Guid OfferId { get; init; }

    /// <summary>The party answering. Must be one of the two on the negotiation.</summary>
    public required Guid PartyId { get; init; }

    /// <summary>One of the codes the offer's workflow snapshot offered this caller.</summary>
    public required string ActionCode { get; init; }

    /// <summary>Present only on a counter. Its absence is what makes this a plain response.</summary>
    public long? CounterAmountMinorUnits { get; init; }

    public string? Message { get; init; }

    /// <summary>Required by transitions the registry marks as needing one — a decline, typically.</summary>
    public string? Reason { get; init; }

    public string? IdempotencyKey { get; init; }
}
