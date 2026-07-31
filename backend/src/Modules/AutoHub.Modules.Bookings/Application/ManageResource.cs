using System.Globalization;
using AutoHub.Modules.Bookings.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Bookings.Application;

/// <summary>
/// Creates a bookable resource with its weekly calendar.
/// </summary>
/// <remarks>
/// The calendar is set at creation rather than through a second call, because a resource with no
/// windows is a resource that silently shows no availability — and the report that says "no slots"
/// looks identical whether the calendar is empty or genuinely full. Requiring at least one window
/// makes the mistake impossible rather than merely detectable.
/// </remarks>
public sealed record CreateResourceCommand : IRequest<Result<Guid>>
{
    public required WorkflowActor Actor { get; init; }

    public required ResourceSpecification Resource { get; init; }

    /// <summary>Weekly opening hours, in IST wall-clock time. At least one is required.</summary>
    public required IReadOnlyList<ResourceWindowRequest> Windows { get; init; }

    /// <summary>Holidays, leave and one-off hours. Optional; edited far more often than the windows.</summary>
    public IReadOnlyList<ResourceExceptionRequest> Exceptions { get; init; } = [];
}

public sealed record ResourceWindowRequest(DayOfWeek DayOfWeek, TimeOnly OpensAt, TimeOnly ClosesAt);

public sealed record ResourceExceptionRequest(
    DateOnly Date,
    bool IsClosed,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    string? Reason);

public sealed class CreateResourceHandler : IRequestHandler<CreateResourceCommand, Result<Guid>>
{
    private readonly IBookableResourceRepository _resources;

    public CreateResourceHandler(IBookableResourceRepository resources) => _resources = resources;

    public async Task<Result<Guid>> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(BookingPermissions.ResourceManage))
        {
            return Error.Forbidden("booking.permission_denied", "You cannot manage bookable resources.")
                .With("requiredPermission", BookingPermissions.ResourceManage);
        }

        if (request.Windows.Count == 0)
        {
            return Error.Validation(
                "booking.resource.no_windows",
                "A resource with no opening hours can never be booked.");
        }

        foreach (var window in request.Windows)
        {
            if (window.ClosesAt <= window.OpensAt)
            {
                return Error.Validation(
                    "booking.resource.window_inverted",
                    "An opening window must close after it opens.")
                    .With("dayOfWeek", window.DayOfWeek.ToString());
            }
        }

        foreach (var exception in request.Exceptions)
        {
            // An open exception without hours would be read by WindowsOn as "open from null to null",
            // which is a null reference at the one moment a resource is meant to be extra available.
            if (!exception.IsClosed && (exception.OpensAt is null || exception.ClosesAt is null))
            {
                return Error.Validation(
                    "booking.resource.exception_incomplete",
                    "An exception that is not a closure must state its hours.")
                    .With("date", exception.Date.ToString("O", CultureInfo.InvariantCulture));
            }
        }

        var created = BookableResource.Create(request.Resource);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        var resource = created.Value;

        foreach (var window in request.Windows)
        {
            resource.AddWindow(new CalendarWindow(window.DayOfWeek, window.OpensAt, window.ClosesAt));
        }

        foreach (var exception in request.Exceptions)
        {
            resource.AddException(new CalendarException(
                exception.Date, exception.IsClosed, exception.OpensAt, exception.ClosesAt, exception.Reason));
        }

        await _resources.AddAsync(resource, cancellationToken);
        await _resources.SaveChangesAsync(cancellationToken);

        return resource.Id;
    }
}
