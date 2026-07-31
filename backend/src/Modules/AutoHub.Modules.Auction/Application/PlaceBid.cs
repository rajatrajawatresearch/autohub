using AutoHub.Modules.Auction.Contracts;
using AutoHub.Modules.Auction.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Auction.Application;

/// <summary>
/// Places a bid.
/// </summary>
/// <remarks>
/// <b>Order of operations, and why it is this order.</b>
/// <list type="number">
/// <item><b>Permission, then the lot.</b> Cheap checks before any I/O the caller was never entitled
/// to cause.</item>
/// <item><b>Resolve the increment from the pricing registry.</b> Before the gate, because the gate's
/// critical section must be as short as possible and a registry read is a network call. The registry
/// is cached, so this is a memory read in the ordinary case.</item>
/// <item><b>The gate decides.</b> One atomic step, described in <see cref="IBidGate"/>. Nothing this
/// handler does before or after can change that decision, and it does not re-derive it.</item>
/// <item><b>Persist the bid, accepted or not.</b> Including rejections: "I bid and it did not go
/// through" is the most common dispute in an auction, and the only answer is the row.</item>
/// <item><b>Journal, then broadcast.</b> In that order, because a client that missed the broadcast
/// recovers by replaying the journal, and a client that missed the journal entry can never recover
/// it.</item>
/// </list>
/// <para>
/// The bid arrives over HTTP with an <c>Idempotency-Key</c> and never over the socket. See
/// <see cref="AuctionRealtimeEvent"/> for why that is not a preference.
/// </para>
/// </remarks>
public sealed record PlaceBidCommand : IRequest<Result<PlaceBidResult>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid LotId { get; init; }

    public required Guid BidderId { get; init; }

    public required string BidderType { get; init; }

    public required long AmountMinorUnits { get; init; }

    /// <summary>The bidder's ceiling for proxy bidding. Null bids exactly the amount offered.</summary>
    public long? MaximumAmountMinorUnits { get; init; }

    /// <summary>The caller's <c>Idempotency-Key</c> header. Mandatory; a bid without one is refused.</summary>
    public required string IdempotencyKey { get; init; }

    public string? DeviceId { get; init; }

    public string? IpAddress { get; init; }
}

public sealed record PlaceBidResult(
    BidOutcome Outcome,
    long StandingAmountMinorUnits,
    string Currency,
    bool YouAreStanding,
    long Sequence,
    DateTimeOffset ClosesAt,
    bool ReserveMet);

public sealed class PlaceBidHandler : IRequestHandler<PlaceBidCommand, Result<PlaceBidResult>>
{
    private readonly IAuctionLotRepository _lots;
    private readonly IBidGate _gate;
    private readonly IBidIncrementPolicy _increments;
    private readonly IAuctionEventJournal _journal;
    private readonly IAuctionBroadcaster _broadcaster;
    private readonly IClock _clock;
    private readonly ILogger<PlaceBidHandler> _logger;

    public PlaceBidHandler(
        IAuctionLotRepository lots,
        IBidGate gate,
        IBidIncrementPolicy increments,
        IAuctionEventJournal journal,
        IAuctionBroadcaster broadcaster,
        IClock clock,
        ILogger<PlaceBidHandler> logger)
    {
        _lots = lots;
        _gate = gate;
        _increments = increments;
        _journal = journal;
        _broadcaster = broadcaster;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<PlaceBidResult>> Handle(
        PlaceBidCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(AuctionPermissions.BidPlace))
        {
            return Error.Forbidden("auction.permission_denied", "You are not approved to bid in this auction.")
                .With("requiredPermission", AuctionPermissions.BidPlace);
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return Error.Validation(
                "auction.bid.idempotency_key_required",
                "A bid must carry an Idempotency-Key.");
        }

        var lot = await _lots.GetAsync(request.LotId, cancellationToken);
        if (lot is null)
        {
            return Error.NotFound("auction.lot.not_found", "No lot with that id.")
                .With("lotId", request.LotId);
        }

        var currency = lot.StartPrice.Currency;
        var amount = Money.FromMinorUnits(request.AmountMinorUnits, currency);

        if (!amount.IsPositive)
        {
            return Error.Validation("auction.bid.amount_not_positive", "A bid must be for a positive amount.");
        }

        Money? maximum = request.MaximumAmountMinorUnits is { } max
            ? Money.FromMinorUnits(max, currency)
            : null;

        if (maximum is not null && !lot.Settings.ProxyBiddingAllowed)
        {
            return Error.Rule(
                "auction.bid.proxy_not_allowed",
                "This auction does not accept maximum bids.");
        }

        if (maximum is { } ceiling && ceiling < amount)
        {
            return Error.Validation(
                "auction.bid.maximum_below_amount",
                "A maximum below the bid itself would be ignored. Raise it or leave it out.");
        }

        // Resolved before the gate so the critical section stays short. The standing price decides
        // the band, and on the first bid the band is decided by the opening price.
        var increment = await _increments.MinimumIncrementAsync(
            lot, lot.HighBidAmount ?? lot.StartPrice, cancellationToken);

        if (increment.IsFailure)
        {
            return Result.Failure<PlaceBidResult>(increment.Error);
        }

        var attempt = new BidAttempt
        {
            LotId = lot.Id,
            BidderId = request.BidderId,
            Amount = amount,
            MaximumAmount = maximum,
            MinimumIncrement = increment.Value,
            IdempotencyKey = request.IdempotencyKey,

            // Server time. A client-supplied instant would let a dealer with a skewed clock, or a
            // modified client, claim a bid landed before one that genuinely arrived first — and the
            // platform would have nothing to disprove it with.
            PlacedAt = _clock.UtcNow,
        };

        var admission = await _gate.AdmitAsync(attempt, cancellationToken);
        if (admission.IsFailure)
        {
            // The gate could not decide, so nothing was decided. The bid is refused and the dealer is
            // told to retry — never accepted on the assumption that it would probably have won.
            return Result.Failure<PlaceBidResult>(admission.Error);
        }

        var decision = admission.Value;

        if (decision.IsReplay)
        {
            // The same request, already decided. Returning the original decision is what the
            // Idempotency-Key contract promises, and it is what stops a retry over a flaky
            // connection from bidding a dealer up against themselves.
            return ToResult(lot, decision, request.BidderId);
        }

        var bid = Bid.Record(attempt, decision, request.BidderType, request.DeviceId, request.IpAddress);
        await _lots.AddBidAsync(bid, cancellationToken);

        if (decision.Outcome is BidOutcome.Accepted or BidOutcome.Outbid)
        {
            lot.ApplyAcceptedBid(decision);
        }

        await _lots.SaveChangesAsync(cancellationToken);

        if (decision.Outcome is BidOutcome.Accepted or BidOutcome.Outbid)
        {
            await PublishAsync(lot, decision, cancellationToken);
        }

        return ToResult(lot, decision, request.BidderId);
    }

    /// <summary>
    /// Journals first, broadcasts second.
    /// </summary>
    /// <remarks>
    /// A broadcast that fails is recovered by a client replaying from the journal. A journal write
    /// that fails leaves a hole in the sequence that no reconnect can fill, and the client sees the
    /// gap and has no way to close it. So the durable write goes first, and a failed broadcast is a
    /// warning rather than an error — the bid is already accepted, and refusing it now would mean
    /// undoing a decision the gate has already made.
    /// </remarks>
    private async Task PublishAsync(AuctionLot lot, BidAdmission decision, CancellationToken cancellationToken)
    {
        var placed = AuctionRealtimeEvent.BidPlaced(
            lot.Id,
            decision.Sequence,
            lot.Settings.BidsVisible ? decision.StandingAmount.MinorUnits : 0,
            decision.StandingBidderId,
            decision.PlacedAt,
            lot.Settings.BidderIdentityVisible,
            lot.IsReserveMet);

        await _journal.AppendAsync(placed, cancellationToken);

        try
        {
            await _broadcaster.PublishAsync(placed, cancellationToken);

            if (decision.ExtendedTo is { } endsAt)
            {
                var extended = AuctionRealtimeEvent.LotExtended(
                    lot.Id, decision.Sequence, endsAt, decision.PlacedAt);

                await _journal.AppendAsync(extended, cancellationToken);
                await _broadcaster.PublishAsync(extended, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Broadcast failed for lot {LotId}; clients will replay", lot.Id);
        }
    }

    private static PlaceBidResult ToResult(AuctionLot lot, BidAdmission decision, Guid bidderId)
        => new(
            decision.Outcome,
            decision.StandingAmount.MinorUnits,
            decision.StandingAmount.Currency.Code,
            decision.BidderIsStanding(bidderId),
            decision.Sequence,
            decision.ExtendedTo ?? lot.ClosesAt,
            lot.IsReserveMet);
}
