using AutoHub.Modules.Bookings.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Modules.Bookings.Application;

/// <summary>
/// Moves a booking to a new slot.
/// </summary>
/// <remarks>
/// Takes the lock for the <b>new</b> day and checks capacity there, exactly as a fresh booking does.
/// The old slot needs no lock: vacating one can never over-fill anything, and the row's own move
/// releases it.
/// <para>
/// A move within the same day still takes the lock, and the overlap count still excludes this
/// booking — otherwise a booking would collide with the slot it is currently occupying and every
/// same-day reschedule on a capacity-1 resource would be refused.
/// </para>
/// </remarks>
public sealed record RescheduleBookingCommand : IRequest<Result<BookingView>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid BookingId { get; init; }

    public required DateTimeOffset StartsAt { get; init; }

    public required DateTimeOffset EndsAt { get; init; }

    public string? Reason { get; init; }
}

public sealed class RescheduleBookingHandler : IRequestHandler<RescheduleBookingCommand, Result<BookingView>>
{
    private readonly IBookingRepository _bookings;
    private readonly IBookableResourceRepository _resources;
    private readonly IResourceDayLock _lock;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;

    public RescheduleBookingHandler(
        IBookingRepository bookings,
        IBookableResourceRepository resources,
        IResourceDayLock resourceDayLock,
        IWorkflowEngine workflow,
        IClock clock)
    {
        _bookings = bookings;
        _resources = resources;
        _lock = resourceDayLock;
        _workflow = workflow;
        _clock = clock;
    }

    public async Task<Result<BookingView>> Handle(
        RescheduleBookingCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(BookingPermissions.Reschedule))
        {
            return Error.Forbidden("booking.permission_denied", "You cannot move this booking.")
                .With("requiredPermission", BookingPermissions.Reschedule);
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

        var serviceDate = DateOnly.FromDateTime(request.StartsAt.ToOffset(SystemClock.IstOffset).Date);
        await _lock.AcquireAsync(resource.Id, serviceDate, cancellationToken);

        var now = _clock.UtcNow;

        var moved = booking.Reschedule(request.StartsAt, request.EndsAt, resource, now);
        if (moved.IsFailure)
        {
            return Result.Failure<BookingView>(moved.Error);
        }

        var overlapping = await _bookings.GetOverlappingAsync(
            resource.Id, booking.OccupiedFrom, booking.OccupiedTo, cancellationToken);

        // Excluding itself, or a same-day move on a capacity-1 resource would always collide with the
        // slot it is leaving.
        var others = overlapping.Count(b => b.Id != booking.Id);

        if (others >= resource.Capacity)
        {
            return Error.Conflict(
                "booking.slot_unavailable",
                "That slot has just been taken.")
                .With("capacity", resource.Capacity)
                .With("booked", others);
        }

        var snapshot = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = BookingWorkflow.Key,
                EntityId = booking.Id,
                ActionCode = BookingWorkflow.Reschedule,
                Actor = request.Actor,
                Reason = request.Reason,
                Context = BookingContext.Build(booking, resource, now),
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
