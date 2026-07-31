using AutoHub.Modules.Bookings.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Modules.Bookings.Application;

/// <summary>
/// Books a slot, under the resource-day lock that makes the check-then-insert safe.
/// </summary>
/// <remarks>
/// <b>The locking strategy is stated in full in <see cref="BookingSchema"/>.</b> In short: take a
/// transaction-scoped advisory lock on (resource, service date); inside it, count overlapping live
/// bookings and compare to capacity; insert. Two requests for the same resource on the same day
/// serialise, requests for anything else do not contend at all, and the exclusion constraint behind
/// the table refuses an overlap on a capacity-1 resource even if a future writer forgets the lock.
/// <para>
/// The lock is taken <b>before</b> the overlap count and released by the transaction, not by this
/// code. Counting first and then locking would leave the same gap the lock exists to close, and
/// releasing by hand would leak the lock whenever the handler returned early — which it does, on
/// four separate paths below.
/// </para>
/// </remarks>
public sealed record RequestBookingCommand : IRequest<Result<BookingView>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid ResourceId { get; init; }

    public required BookingSpecification Booking { get; init; }
}

public sealed record BookingView(
    Guid Id,
    string PurposeCode,
    Guid BookableResourceId,
    Guid CustomerId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string LocationKindCode,
    DateTimeOffset? VerifiedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt,
    long? PenaltyMinorUnits,
    string? PenaltyCurrency,
    WorkflowSnapshot? Workflow)
{
    public static BookingView From(Booking booking, WorkflowSnapshot? workflow)
        => new(
            booking.Id,
            booking.PurposeCode,
            booking.BookableResourceId,
            booking.CustomerId,
            booking.StartsAt,
            booking.EndsAt,
            booking.LocationKindCode,
            booking.VerifiedAt,
            booking.CompletedAt,
            booking.CancelledAt,
            booking.PenaltyAmount?.MinorUnits,
            booking.PenaltyAmount?.Currency.Code,
            workflow);
}

public sealed class RequestBookingHandler : IRequestHandler<RequestBookingCommand, Result<BookingView>>
{
    private readonly IBookableResourceRepository _resources;
    private readonly IBookingRepository _bookings;
    private readonly IResourceDayLock _lock;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;

    public RequestBookingHandler(
        IBookableResourceRepository resources,
        IBookingRepository bookings,
        IResourceDayLock resourceDayLock,
        IWorkflowEngine workflow,
        IClock clock)
    {
        _resources = resources;
        _bookings = bookings;
        _lock = resourceDayLock;
        _workflow = workflow;
        _clock = clock;
    }

    public async Task<Result<BookingView>> Handle(
        RequestBookingCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(BookingPermissions.Request))
        {
            return Error.Forbidden("booking.permission_denied", "You cannot make this booking.")
                .With("requiredPermission", BookingPermissions.Request);
        }

        var resource = await _resources.GetAsync(request.ResourceId, cancellationToken);
        if (resource is null)
        {
            return Error.NotFound("booking.resource_not_found", "No bookable resource with that id.")
                .With("resourceId", request.ResourceId);
        }

        if (!resource.IsActive)
        {
            return Error.Rule("booking.resource_inactive", "That resource is not taking bookings.");
        }

        var now = _clock.UtcNow;

        var created = Booking.Request(request.Booking, resource, now);
        if (created.IsFailure)
        {
            return Result.Failure<BookingView>(created.Error);
        }

        var booking = created.Value;

        // Taken before anything is read, and held by the transaction until it commits or rolls back.
        // The service date is the IST calendar day the appointment starts on, which is what an
        // operator means by "the inspector's Tuesday" and what makes the lock granular enough.
        var serviceDate = DateOnly.FromDateTime(
            booking.StartsAt.ToOffset(SystemClock.IstOffset).Date);

        await _lock.AcquireAsync(resource.Id, serviceDate, cancellationToken);

        var overlapping = await _bookings.GetOverlappingAsync(
            resource.Id, booking.OccupiedFrom, booking.OccupiedTo, cancellationToken);

        if (overlapping.Count >= resource.Capacity)
        {
            // Reached only when somebody took the last slot between the availability list being
            // rendered and this request arriving. Returning the capacity lets the client say
            // "just taken" rather than "something went wrong".
            return Error.Conflict(
                "booking.slot_unavailable",
                "That slot has just been taken.")
                .With("capacity", resource.Capacity)
                .With("booked", overlapping.Count);
        }

        await _bookings.AddAsync(booking, cancellationToken);
        await _bookings.SaveChangesAsync(cancellationToken);

        var snapshot = await _workflow.StartAsync(
            BookingWorkflow.Key,
            booking.Id,
            BookingContext.Build(booking, resource),
            request.Actor.UserId,
            cancellationToken);

        return snapshot.IsSuccess
            ? BookingView.From(booking, snapshot.Value)
            : Result.Failure<BookingView>(snapshot.Error);
    }
}
