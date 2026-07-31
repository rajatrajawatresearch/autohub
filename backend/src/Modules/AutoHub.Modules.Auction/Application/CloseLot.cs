using AutoHub.Modules.Auction.Contracts;
using AutoHub.Modules.Auction.Domain;
using AutoHub.Modules.Deals.Contracts;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Auction.Application;

/// <summary>
/// Closes a lot and settles the outcome.
/// </summary>
/// <remarks>
/// <b>The sequence a win goes through, and where each step lives.</b>
/// <code>
/// gate closed        this handler        no more bids can be admitted, full stop
/// winner decided     this handler        by gate sequence, never by amount or timestamp
/// deal opened        deals module        via IDealGateway, idempotent on the lot id
/// win priced         pricing registry    commission and GST, through the deal
/// payment window     workflow registry   a transition with automaticAfterMinutes set
/// forfeiture         workflow scheduler  fires that transition when the window expires
/// relist             workflow registry   a transition out of forfeited, back to scheduled
/// </code>
/// Not one of those durations, states or percentages is in this file. What is here is the order they
/// happen in, which is code because it is a sequence of operations rather than a policy.
/// <para>
/// <b>Closing the gate comes first and is not optional.</b> If it fails, the handler stops. Awarding
/// a lot whose gate is still admitting bids would let a bid land after the winner was decided, and
/// the platform would have told two dealers they won the same vehicle.
/// </para>
/// <para>
/// <b>Everything after that is idempotent.</b> The scheduler can pick the same due lot up twice, a
/// node can die between opening the deal and recording its id, and an operator can close a lot by
/// hand that the timer was about to close. Each step checks what it already did, and the deal gateway
/// deduplicates on the lot id.
/// </para>
/// </remarks>
public sealed record CloseLotCommand : IRequest<Result<CloseLotResult>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid LotId { get; init; }
}

public sealed record CloseLotResult(
    Guid LotId,
    string OutcomeCode,
    Guid? WinnerId,
    long? WinningAmountMinorUnits,
    Guid? DealId,
    DateTimeOffset? PaymentDueAt);

public sealed class CloseLotHandler : IRequestHandler<CloseLotCommand, Result<CloseLotResult>>
{
    private readonly IAuctionLotRepository _lots;
    private readonly IBidGate _gate;
    private readonly IDealGateway _deals;
    private readonly IWorkflowEngine _workflow;
    private readonly IAuctionEventJournal _journal;
    private readonly IAuctionBroadcaster _broadcaster;
    private readonly IClock _clock;
    private readonly ILogger<CloseLotHandler> _logger;

    public CloseLotHandler(
        IAuctionLotRepository lots,
        IBidGate gate,
        IDealGateway deals,
        IWorkflowEngine workflow,
        IAuctionEventJournal journal,
        IAuctionBroadcaster broadcaster,
        IClock clock,
        ILogger<CloseLotHandler> logger)
    {
        _lots = lots;
        _gate = gate;
        _deals = deals;
        _workflow = workflow;
        _journal = journal;
        _broadcaster = broadcaster;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Outcome codes on the <c>lot.closed</c> socket event, per §11 of the contract.</summary>
    private const string SoldOutcome = "sold";

    private const string UnsoldOutcome = "unsold";

    /// <summary>Reason recorded when a lot closes without meeting its reserve.</summary>
    private const string ReserveNotMetReason = "reserve_not_met";

    private const string NoBidsReason = "no_bids";

    /// <summary>The source type a deal is opened against. Half of the deal's idempotency key.</summary>
    private const string LotSourceType = "auction.lot";

    public async Task<Result<CloseLotResult>> Handle(
        CloseLotCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(AuctionPermissions.LotClose))
        {
            return Error.Forbidden("auction.permission_denied", "You cannot close a lot.")
                .With("requiredPermission", AuctionPermissions.LotClose);
        }

        var lot = await _lots.GetAsync(request.LotId, cancellationToken);
        if (lot is null)
        {
            return Error.NotFound("auction.lot.not_found", "No lot with that id.")
                .With("lotId", request.LotId);
        }

        // First, and if it fails nothing else runs. See the remarks on this class.
        var closed = await _gate.CloseAsync(lot.Id, cancellationToken);
        if (closed.IsFailure)
        {
            return Result.Failure<CloseLotResult>(closed.Error);
        }

        var now = _clock.UtcNow;
        var winning = await _lots.GetWinningBidAsync(lot.Id, cancellationToken);

        if (winning is null || lot.HighBidAmount is null)
        {
            return await PassInAsync(lot, request.Actor, NoBidsReason, now, cancellationToken);
        }

        if (!lot.IsReserveMet)
        {
            // The seller is not obliged to sell. The high bid stands as an offer the auction team can
            // take to them, which is exactly what a pass-in is in a physical saleroom.
            return await PassInAsync(lot, request.Actor, ReserveNotMetReason, now, cancellationToken);
        }

        var awarded = lot.Award(winning.Id, winning.BidderId, lot.HighBidAmount.Value, now);
        if (awarded.IsFailure)
        {
            return Result.Failure<CloseLotResult>(awarded.Error);
        }

        var deal = await _deals.OpenAsync(
            new OpenDealRequest
            {
                IntentCode = AuctionIntentCode,
                PricingScope = lot.WinPricingScope,
                BaseAmountMinorUnits = lot.HighBidAmount.Value.MinorUnits,
                CurrencyCode = lot.HighBidAmount.Value.Currency.Code,
                SourceType = LotSourceType,
                SourceId = lot.Id,
                ListingId = lot.ListingId,
                VehicleId = lot.VehicleId,
                CityCode = lot.CityCode,
                StateCode = lot.StateCode,
                Parties =
                [
                    new DealPartyRequest
                    {
                        RoleCode = SellerRole,
                        PartyId = lot.SellerId,
                        PartyType = lot.SellerType,
                        IsPayee = true,
                        SettlementShareRatio = 1,
                    },
                    new DealPartyRequest
                    {
                        RoleCode = BuyerRole,
                        PartyId = winning.BidderId,
                        PartyType = winning.BidderType,
                        IsPayer = true,
                    },
                ],
            },
            cancellationToken);

        if (deal.IsFailure)
        {
            // The lot stays awarded and unsettled rather than silently sold for nothing. An alert
            // fires, operations retries, and the idempotency on the lot id means the retry opens the
            // same deal instead of a second one.
            _logger.LogError(
                "Lot {LotId} was won but its deal could not be opened: {Code}",
                lot.Id, deal.Error.Code);

            return Result.Failure<CloseLotResult>(deal.Error);
        }

        lot.AttachDeal(deal.Value.DealId);

        var transition = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = AuctionWorkflow.LotKey,
                EntityId = lot.Id,
                ActionCode = AuctionWorkflow.Award,
                Actor = request.Actor,
                Context = BuildContext(lot, deal.Value.DealId),
            },
            cancellationToken);

        if (transition.IsFailure)
        {
            return Result.Failure<CloseLotResult>(transition.Error);
        }

        await _lots.SaveChangesAsync(cancellationToken);

        await AnnounceAsync(lot, SoldOutcome, winning.BidderId, now, cancellationToken);

        return new CloseLotResult(
            lot.Id,
            SoldOutcome,
            winning.BidderId,
            lot.WinningAmount?.MinorUnits,
            lot.DealId,
            lot.PaymentDueAt);
    }

    private async Task<Result<CloseLotResult>> PassInAsync(
        AuctionLot lot,
        WorkflowActor actor,
        string reasonCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var unsold = lot.CloseUnsold(now, reasonCode);
        if (unsold.IsFailure)
        {
            return Result.Failure<CloseLotResult>(unsold.Error);
        }

        var transition = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = AuctionWorkflow.LotKey,
                EntityId = lot.Id,
                ActionCode = AuctionWorkflow.PassIn,
                Actor = actor,
                Reason = reasonCode,
                Context = BuildContext(lot, null),
            },
            cancellationToken);

        if (transition.IsFailure)
        {
            return Result.Failure<CloseLotResult>(transition.Error);
        }

        await _lots.SaveChangesAsync(cancellationToken);

        await AnnounceAsync(lot, UnsoldOutcome, null, now, cancellationToken);

        return new CloseLotResult(lot.Id, UnsoldOutcome, null, null, null, null);
    }

    private async Task AnnounceAsync(
        AuctionLot lot,
        string outcomeCode,
        Guid? winnerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // The close takes the next sequence number after the last bid, so a client replaying from
        // sinceSeq receives the close in the same ordered stream as the bids that led to it.
        var closedEvent = AuctionRealtimeEvent.LotClosed(
            lot.Id, lot.BidSequence + 1, outcomeCode, winnerId, now);

        await _journal.AppendAsync(closedEvent, cancellationToken);

        try
        {
            await _broadcaster.PublishAsync(closedEvent, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Close broadcast failed for lot {LotId}; clients will replay", lot.Id);
        }
    }

    private static System.Text.Json.Nodes.JsonObject BuildContext(AuctionLot lot, Guid? dealId)
        => new()
        {
            ["lot"] = new System.Text.Json.Nodes.JsonObject
            {
                ["reserveMet"] = lot.IsReserveMet,
                ["bidCount"] = lot.BidCount,
                ["relistCount"] = lot.RelistCount,
                ["paymentWindowMinutes"] = lot.Settings.PaymentWindowMinutes,
                ["dealId"] = dealId?.ToString(),
            },
            ["location"] = new System.Text.Json.Nodes.JsonObject
            {
                ["city"] = lot.CityCode,
                ["state"] = lot.StateCode,
            },
        };

    /// <summary>The intent this module opens deals under. A taxonomy code, named once.</summary>
    private const string AuctionIntentCode = "auction";

    private const string SellerRole = "seller";

    private const string BuyerRole = "buyer";
}
