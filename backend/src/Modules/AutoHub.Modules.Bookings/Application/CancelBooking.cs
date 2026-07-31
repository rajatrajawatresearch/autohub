using AutoHub.Modules.Bookings.Domain;
using AutoHub.Platform.Pricing;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Bookings.Application;

/// <summary>
/// Cancels a booking, charging whatever the pricing registry says a cancellation costs.
/// </summary>
/// <remarks>
/// <b>There is no free-cancellation window in this file.</b> "Free more than 24 hours ahead, ₹300
/// inside that, always free for a first offence, never in a city launched under three months ago" is
/// a rule set, evaluated against a context that carries <c>hoursUntilStart</c>. The handler asks and
/// records; it does not know whether a fee applies today.
/// <para>
/// A missing rule set is treated as "nothing to charge" rather than as a failure, and that is the one
/// place this module's error handling differs from the ledger's. A cancellation that cannot proceed
/// because a fee could not be priced leaves the slot blocked and the customer stuck, which is a worse
/// outcome than an uncharged fee — and the uncharged fee is visible in a report, where the blocked
/// slot is not.
/// </para>
/// </remarks>
public sealed record CancelBookingCommand : IRequest<Result<BookingView>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid BookingId { get; init; }

    public required string Reason { get; init; }

    /// <summary>True when nobody turned up. Prices under a different scope and scores differently.</summary>
    public bool WasNoShow { get; init; }
}

public sealed class CancelBookingHandler : IRequestHandler<CancelBookingCommand, Result<BookingView>>
{
    private readonly IBookingRepository _bookings;
    private readonly IBookableResourceRepository _resources;
    private readonly IPricingEngine _pricing;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;
    private readonly ILogger<CancelBookingHandler> _logger;

    public CancelBookingHandler(
        IBookingRepository bookings,
        IBookableResourceRepository resources,
        IPricingEngine pricing,
        IWorkflowEngine workflow,
        IClock clock,
        ILogger<CancelBookingHandler> logger)
    {
        _bookings = bookings;
        _resources = resources;
        _pricing = pricing;
        _workflow = workflow;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<BookingView>> Handle(
        CancelBookingCommand request,
        CancellationToken cancellationToken)
    {
        var permission = request.WasNoShow ? BookingPermissions.MarkNoShow : BookingPermissions.Cancel;

        if (!request.Actor.Holds(permission))
        {
            return Error.Forbidden("booking.permission_denied", "You cannot do this.")
                .With("requiredPermission", permission);
        }

        var booking = await _bookings.GetAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Error.NotFound("booking.not_found", "No booking with that id.")
                .With("bookingId", request.BookingId);
        }

        var resource = await _resources.GetAsync(booking.BookableResourceId, cancellationToken);
        if (resource is null)
        {
            return Error.NotFound("booking.resource_not_found", "This booking's resource no longer exists.");
        }

        var now = _clock.UtcNow;
        var context = BookingContext.Build(booking, resource, now);

        var scope = request.WasNoShow ? BookingPricingScopes.NoShow : BookingPricingScopes.Cancellation;

        // Priced against zero: a cancellation fee has no principal to be a percentage of, so the rule
        // set emits flat or slab components. Passing the appointment's own value instead would let a
        // fee scale with a vehicle's price, which nobody has asked for and which a rule can express
        // later by reading it out of the context.
        var quote = await _pricing.QuoteAsync(
            new PriceQuoteRequest
            {
                Scope = scope,
                BaseAmount = Money.ZeroInr,
                Context = context,
            },
            cancellationToken);

        Money? penalty = null;
        string? ruleCode = null;

        if (quote.IsSuccess)
        {
            penalty = quote.Value.TotalFees.IsPositive ? quote.Value.TotalFees : null;
            ruleCode = quote.Value.AppliedRuleCode;
        }
        else
        {
            _logger.LogWarning(
                "No cancellation pricing for scope {Scope}; booking {BookingId} cancelled without a fee: {Code}",
                scope, booking.Id, quote.Error.Code);
        }

        var cancelled = request.WasNoShow
            ? booking.MarkNoShow(penalty, ruleCode, now)
            : booking.Cancel(request.Reason, penalty, ruleCode, now);

        if (cancelled.IsFailure)
        {
            return Result.Failure<BookingView>(cancelled.Error);
        }

        var snapshot = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = BookingWorkflow.Key,
                EntityId = booking.Id,
                ActionCode = request.WasNoShow ? BookingWorkflow.NoShow : BookingWorkflow.Cancel,
                Actor = request.Actor,
                Reason = request.Reason,
                Context = context,
            },
            cancellationToken);

        if (snapshot.IsFailure)
        {
            return Result.Failure<BookingView>(snapshot.Error);
        }

        await _bookings.SaveChangesAsync(cancellationToken);

        return BookingView.From(booking, snapshot.Value);
    }
}
