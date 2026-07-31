using System.Text.Json.Nodes;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Modules.Listings.Domain.Offers;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Listings.Application.MakeOffer;

/// <summary>
/// Assembles the one shape every offer use case returns.
/// </summary>
/// <remarks>
/// Shared by opening an offer and responding to one, so the two cannot disagree about the amount on
/// the table — which in a negotiation is the only number that matters and the one a dispute turns on.
/// </remarks>
public sealed class OfferDescriber
{
    private readonly IWorkflowEngine _workflow;

    public OfferDescriber(IWorkflowEngine workflow) => _workflow = workflow;

    public async Task<Result<OfferDetail>> DescribeAsync(
        ListingOffer offer,
        WorkflowActor actor,
        CancellationToken cancellationToken)
    {
        var snapshot = await _workflow.GetSnapshotAsync(
            ListingWorkflows.Offer,
            offer.Id,
            GuardContext(offer, actor),
            actor,
            cancellationToken);

        if (snapshot.IsFailure)
        {
            return Result.Failure<OfferDetail>(snapshot.Error);
        }

        var current = offer.CurrentAmount;

        return new OfferDetail
        {
            OfferId = offer.Id,
            ListingId = offer.ListingId,
            BuyerPartyId = offer.BuyerPartyId,
            SellerPartyId = offer.SellerPartyId,
            CurrentAmountMinorUnits = current?.MinorUnits ?? 0,
            Currency = offer.CurrencyCode,
            Turns = [.. offer.Turns
                .OrderBy(t => t.Sequence)
                .Select(t => new OfferTurnView(
                    t.Sequence, t.Side, t.PartyId, t.AmountMinorUnits, t.CurrencyCode, t.Message, t.PlacedAt))],
            Workflow = snapshot.Value,
        };
    }

    /// <summary>
    /// The facts a negotiation guard may evaluate against.
    /// </summary>
    /// <remarks>
    /// <c>awaitingSide</c> is the one that earns its place: "only the side whose turn it is may
    /// counter" is a rule every marketplace has and one that would otherwise be an <c>if</c> in this
    /// handler. Expressed as a fact, the workflow can also encode "the seller may accept at any
    /// time, even out of turn", which is a real and reasonable thing for a category manager to want.
    /// </remarks>
    private static JsonObject GuardContext(ListingOffer offer, WorkflowActor actor) => new()
    {
        ["listingId"] = offer.ListingId.ToString(),
        ["buyerPartyId"] = offer.BuyerPartyId.ToString(),
        ["sellerPartyId"] = offer.SellerPartyId.ToString(),
        ["currentAmountMinorUnits"] = offer.CurrentAmount?.MinorUnits ?? 0,
        ["currency"] = offer.CurrencyCode,
        ["turnCount"] = offer.Turns.Count,
        ["awaitingSide"] = offer.Latest is { } latest
            ? (latest.Side == OfferSide.Buyer ? nameof(OfferSide.Seller) : nameof(OfferSide.Buyer))
            : nameof(OfferSide.Seller),
        ["actorId"] = actor.UserId?.ToString(),
    };
}
