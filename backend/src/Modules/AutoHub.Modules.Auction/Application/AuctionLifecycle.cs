using AutoHub.Modules.Auction.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Auction.Application;

/// <summary>
/// Schedules a sale event, taking its terms from the type profile registry.
/// </summary>
/// <remarks>
/// The profile is read once and copied onto the auction. From that point the auction runs on its own
/// snapshot, so an operator improving the default anti-snipe window does not change the rules of a
/// sale dealers are already bidding in.
/// </remarks>
public sealed record ScheduleAuctionCommand : IRequest<Result<Guid>>
{
    public required WorkflowActor Actor { get; init; }

    public required string Code { get; init; }

    public required string Title { get; init; }

    /// <summary>The profile whose behaviour this auction adopts, e.g. <c>timed_reserve</c>.</summary>
    public required string TypeCode { get; init; }

    public required string WinPricingScope { get; init; }

    public required DateTimeOffset OpensAt { get; init; }

    public required DateTimeOffset ClosesAt { get; init; }

    public DateTimeOffset? PreviewOpensAt { get; init; }

    public string? CityCode { get; init; }

    public string? StateCode { get; init; }

    public bool IsInviteOnly { get; init; }
}

public sealed class ScheduleAuctionHandler : IRequestHandler<ScheduleAuctionCommand, Result<Guid>>
{
    private readonly IAuctionRepository _auctions;
    private readonly IAuctionTypeProfileStore _profiles;

    public ScheduleAuctionHandler(IAuctionRepository auctions, IAuctionTypeProfileStore profiles)
    {
        _auctions = auctions;
        _profiles = profiles;
    }

    public async Task<Result<Guid>> Handle(ScheduleAuctionCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(AuctionPermissions.AuctionSchedule))
        {
            return Error.Forbidden("auction.permission_denied", "You cannot schedule an auction.")
                .With("requiredPermission", AuctionPermissions.AuctionSchedule);
        }

        var profile = await _profiles.GetAsync(request.TypeCode, cancellationToken);
        if (profile is null)
        {
            // Refusing beats defaulting, for the same reason a missing pricing rule refuses: an
            // auction run under terms nobody authored is an auction whose outcome nobody can defend.
            return Error.NotFound(
                "auction.type_profile_not_found",
                "No auction type profile with that code.")
                .With("typeCode", request.TypeCode);
        }

        // Qualified because this module's own namespace is also called Auction, and an unqualified
        // reference binds to the namespace rather than to the aggregate.
        var scheduled = Domain.Auction.Schedule(new AuctionSpecification
        {
            Code = request.Code,
            Title = request.Title,
            TypeCode = profile.TypeCode,
            Settings = profile.Settings,
            WinPricingScope = request.WinPricingScope,
            OpensAt = request.OpensAt,
            ClosesAt = request.ClosesAt,
            PreviewOpensAt = request.PreviewOpensAt ?? request.OpensAt,
            CityCode = request.CityCode,
            StateCode = request.StateCode,
            IsInviteOnly = request.IsInviteOnly,
        });

        if (scheduled.IsFailure)
        {
            return Result.Failure<Guid>(scheduled.Error);
        }

        await _auctions.AddAsync(scheduled.Value, cancellationToken);
        await _auctions.SaveChangesAsync(cancellationToken);

        return scheduled.Value.Id;
    }
}

/// <summary>
/// Adds a lot to a scheduled auction and seeds its bid gate.
/// </summary>
/// <remarks>
/// Seeding is what makes the gate refuse a bid on a lot it has never heard of. The gate treats a
/// missing hash as "closed", so a lot that was never opened cannot be bid on at any price — which is
/// the safe direction, and the reason opening is an explicit step rather than something the first bid
/// does lazily.
/// </remarks>
public sealed record OpenLotCommand : IRequest<Result<Guid>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid AuctionId { get; init; }

    public required LotSpecification Lot { get; init; }
}

public sealed class OpenLotHandler : IRequestHandler<OpenLotCommand, Result<Guid>>
{
    private readonly IAuctionRepository _auctions;
    private readonly IBidGate _gate;
    private readonly IWorkflowEngine _workflow;

    public OpenLotHandler(IAuctionRepository auctions, IBidGate gate, IWorkflowEngine workflow)
    {
        _auctions = auctions;
        _gate = gate;
        _workflow = workflow;
    }

    public async Task<Result<Guid>> Handle(OpenLotCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(AuctionPermissions.LotManage))
        {
            return Error.Forbidden("auction.permission_denied", "You cannot add lots to this auction.")
                .With("requiredPermission", AuctionPermissions.LotManage);
        }

        var auction = await _auctions.GetAsync(request.AuctionId, cancellationToken);
        if (auction is null)
        {
            return Error.NotFound("auction.not_found", "No auction with that id.")
                .With("auctionId", request.AuctionId);
        }

        var added = auction.AddLot(request.Lot);
        if (added.IsFailure)
        {
            return Result.Failure<Guid>(added.Error);
        }

        var lot = added.Value;

        await _auctions.SaveChangesAsync(cancellationToken);

        // Seeded after the row is committed. A gate open on a lot that failed to persist would accept
        // bids against a lot nobody can find; a lot persisted with no gate simply cannot be bid on
        // until it is seeded, which an operator can retry.
        var seeded = await _gate.OpenAsync(
            new BidGateState
            {
                LotId = lot.Id,
                StartPrice = lot.StartPrice,
                ClosesAt = lot.ClosesAt,
                AntiSnipeWindowSeconds = lot.Settings.AntiSnipeWindowSeconds,
                AntiSnipeExtensionSeconds = lot.Settings.AntiSnipeExtensionSeconds,
                MaxExtensions = lot.Settings.MaxExtensions,
            },
            cancellationToken);

        if (seeded.IsFailure)
        {
            return Result.Failure<Guid>(seeded.Error);
        }

        var started = await _workflow.StartAsync(
            AuctionWorkflow.LotKey,
            lot.Id,
            null,
            request.Actor.UserId,
            cancellationToken);

        return started.IsSuccess ? lot.Id : Result.Failure<Guid>(started.Error);
    }
}

/// <summary>
/// Rehydrates a lot's gate from the database.
/// </summary>
/// <remarks>
/// The recovery path for the one failure the gate refuses to guess about: its state is gone — an
/// eviction, a restart, a failover to an empty replica — and it is answering <c>closed</c> to every
/// bid. Restoring the standing bid and the winner's proxy ceiling from the rows that were written
/// alongside every accepted bid is what turns that from an incident into an operation.
/// <para>
/// The standing bidder's ceiling has to be restored too. Without it the incumbent silently loses the
/// protection they paid for, and the next challenger takes the lot for one increment.
/// </para>
/// </remarks>
public sealed record RehydrateLotGateCommand : IRequest<Result>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid LotId { get; init; }
}

public sealed class RehydrateLotGateHandler : IRequestHandler<RehydrateLotGateCommand, Result>
{
    private readonly IAuctionLotRepository _lots;
    private readonly IBidGate _gate;
    private readonly ILogger<RehydrateLotGateHandler> _logger;

    public RehydrateLotGateHandler(
        IAuctionLotRepository lots,
        IBidGate gate,
        ILogger<RehydrateLotGateHandler> logger)
    {
        _lots = lots;
        _gate = gate;
        _logger = logger;
    }

    public async Task<Result> Handle(RehydrateLotGateCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(AuctionPermissions.LotManage))
        {
            return Error.Forbidden("auction.permission_denied", "You cannot rehydrate this lot.")
                .With("requiredPermission", AuctionPermissions.LotManage);
        }

        var lot = await _lots.GetAsync(request.LotId, cancellationToken);
        if (lot is null)
        {
            return Error.NotFound("auction.lot.not_found", "No lot with that id.")
                .With("lotId", request.LotId);
        }

        var winning = await _lots.GetWinningBidAsync(lot.Id, cancellationToken);

        _logger.LogWarning(
            "Rehydrating bid gate for lot {LotId} at {StandingMinor} seq {Sequence}",
            lot.Id, lot.HighBidAmount?.MinorUnits ?? 0, lot.BidSequence);

        return await _gate.OpenAsync(
            new BidGateState
            {
                LotId = lot.Id,
                StartPrice = lot.StartPrice,
                ClosesAt = lot.ClosesAt,
                AntiSnipeWindowSeconds = lot.Settings.AntiSnipeWindowSeconds,
                AntiSnipeExtensionSeconds = lot.Settings.AntiSnipeExtensionSeconds,
                MaxExtensions = lot.Settings.MaxExtensions,
                StandingAmount = lot.HighBidAmount,
                StandingBidderId = lot.HighBidderId,
                StandingMaximum = winning?.MaximumAmount ?? lot.HighBidAmount,
                Sequence = lot.BidSequence,
                ExtensionCount = lot.ExtensionCount,
            },
            cancellationToken);
    }
}

/// <summary>
/// Closes every lot whose time is up. Driven by the scheduler, never by a request.
/// </summary>
/// <remarks>
/// One failing lot must not stop the sweep. A lot whose deal could not be opened is an alert and a
/// retry; the other three hundred lots closing in the same minute have nothing to do with it, and a
/// sweep that aborts on the first problem leaves them open past their advertised end.
/// </remarks>
public sealed record CloseDueLotsCommand : IRequest<Result<int>>
{
    public int BatchSize { get; init; } = 100;
}

public sealed class CloseDueLotsHandler : IRequestHandler<CloseDueLotsCommand, Result<int>>
{
    private readonly IAuctionLotRepository _lots;
    private readonly ISender _sender;
    private readonly IClock _clock;
    private readonly ILogger<CloseDueLotsHandler> _logger;

    public CloseDueLotsHandler(
        IAuctionLotRepository lots,
        ISender sender,
        IClock clock,
        ILogger<CloseDueLotsHandler> logger)
    {
        _lots = lots;
        _sender = sender;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(CloseDueLotsCommand request, CancellationToken cancellationToken)
    {
        var due = await _lots.GetDueForCloseAsync(_clock.UtcNow, request.BatchSize, cancellationToken);
        var closed = 0;

        foreach (var lot in due)
        {
            var outcome = await _sender.Send(
                new CloseLotCommand { Actor = WorkflowActor.System, LotId = lot.Id },
                cancellationToken);

            if (outcome.IsSuccess)
            {
                closed++;
            }
            else
            {
                _logger.LogError(
                    "Lot {LotId} was due to close and could not be: {Code}",
                    lot.Id, outcome.Error.Code);
            }
        }

        return closed;
    }
}

/// <summary>
/// Forfeits a win whose payment window expired.
/// </summary>
/// <remarks>
/// Fired by the workflow scheduler from a transition with <c>automaticAfterMinutes</c> set, which is
/// why the actor is the system and there is no permission check on the timer path. The window's
/// length came from the lot's own settings when it was awarded, so extending the default afterwards
/// never shortens or lengthens a window a dealer is already inside.
/// <para>
/// The forfeited deposit and any penalty are the pricing registry's business, charged through the
/// deal that the win opened. This handler frees the vehicle; it does not decide what the failure
/// costs.
/// </para>
/// </remarks>
public sealed record ForfeitLotCommand : IRequest<Result>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid LotId { get; init; }

    public required string ReasonCode { get; init; }
}

public sealed class ForfeitLotHandler : IRequestHandler<ForfeitLotCommand, Result>
{
    private readonly IAuctionLotRepository _lots;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;

    public ForfeitLotHandler(IAuctionLotRepository lots, IWorkflowEngine workflow, IClock clock)
    {
        _lots = lots;
        _workflow = workflow;
        _clock = clock;
    }

    public async Task<Result> Handle(ForfeitLotCommand request, CancellationToken cancellationToken)
    {
        var lot = await _lots.GetAsync(request.LotId, cancellationToken);
        if (lot is null)
        {
            return Error.NotFound("auction.lot.not_found", "No lot with that id.")
                .With("lotId", request.LotId);
        }

        var forfeited = lot.Forfeit(_clock.UtcNow, request.ReasonCode);
        if (forfeited.IsFailure)
        {
            return forfeited;
        }

        var snapshot = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = AuctionWorkflow.LotKey,
                EntityId = lot.Id,
                ActionCode = AuctionWorkflow.Forfeit,
                Actor = request.Actor,
                Reason = request.ReasonCode,
            },
            cancellationToken);

        if (snapshot.IsFailure)
        {
            return Result.Failure(snapshot.Error);
        }

        await _lots.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>Puts a forfeited or unsold lot into a later auction.</summary>
public sealed record RelistLotCommand : IRequest<Result>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid LotId { get; init; }

    public required DateTimeOffset OpensAt { get; init; }

    public required DateTimeOffset ClosesAt { get; init; }

    /// <summary>A lower opening price. Null keeps the existing one.</summary>
    public long? StartPriceMinorUnits { get; init; }

    /// <summary>A lower reserve — the usual reason a lot that passed in sells the second time.</summary>
    public long? ReservePriceMinorUnits { get; init; }
}

public sealed class RelistLotHandler : IRequestHandler<RelistLotCommand, Result>
{
    private readonly IAuctionLotRepository _lots;
    private readonly IBidGate _gate;
    private readonly IWorkflowEngine _workflow;

    public RelistLotHandler(IAuctionLotRepository lots, IBidGate gate, IWorkflowEngine workflow)
    {
        _lots = lots;
        _gate = gate;
        _workflow = workflow;
    }

    public async Task<Result> Handle(RelistLotCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(AuctionPermissions.LotRelist))
        {
            return Error.Forbidden("auction.permission_denied", "You cannot relist this lot.")
                .With("requiredPermission", AuctionPermissions.LotRelist);
        }

        var lot = await _lots.GetAsync(request.LotId, cancellationToken);
        if (lot is null)
        {
            return Error.NotFound("auction.lot.not_found", "No lot with that id.")
                .With("lotId", request.LotId);
        }

        var currency = lot.StartPrice.Currency;

        Money? startPrice = request.StartPriceMinorUnits is { } start
            ? Money.FromMinorUnits(start, currency)
            : null;

        Money? reservePrice = request.ReservePriceMinorUnits is { } reserve
            ? Money.FromMinorUnits(reserve, currency)
            : null;

        var relisted = lot.Relist(request.OpensAt, request.ClosesAt, startPrice, reservePrice);
        if (relisted.IsFailure)
        {
            return relisted;
        }

        var snapshot = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = AuctionWorkflow.LotKey,
                EntityId = lot.Id,
                ActionCode = AuctionWorkflow.Relist,
                Actor = request.Actor,
            },
            cancellationToken);

        if (snapshot.IsFailure)
        {
            return Result.Failure(snapshot.Error);
        }

        await _lots.SaveChangesAsync(cancellationToken);

        // Re-seeded with a cleared standing bid, because the relisted lot is a fresh contest. Seeding
        // it with the forfeited winner's price would open the second sale at a number nobody has
        // actually offered.
        return await _gate.OpenAsync(
            new BidGateState
            {
                LotId = lot.Id,
                StartPrice = lot.StartPrice,
                ClosesAt = lot.ClosesAt,
                AntiSnipeWindowSeconds = lot.Settings.AntiSnipeWindowSeconds,
                AntiSnipeExtensionSeconds = lot.Settings.AntiSnipeExtensionSeconds,
                MaxExtensions = lot.Settings.MaxExtensions,
            },
            cancellationToken);
    }
}
