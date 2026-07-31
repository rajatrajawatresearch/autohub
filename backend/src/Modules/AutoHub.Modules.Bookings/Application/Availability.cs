using AutoHub.Modules.Bookings.Domain;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Modules.Bookings.Application;

/// <summary>
/// Lists the slots a resource can actually take on a date.
/// </summary>
/// <remarks>
/// <b>Availability and booking must agree, so they share the overlap query.</b> A slot list built
/// from one definition of "busy" and a double-booking guard built from another is a UI that offers
/// slots the server then refuses — which users read as the site being broken, and which is
/// impossible to reproduce because it depends on who else was booking at the time.
/// <para>
/// The list is a snapshot, not a reservation. Between rendering it and submitting one, somebody else
/// may take the slot; that is what the lock in <see cref="RequestBookingHandler"/> is for. Holding a
/// slot while a user thinks about it would need an expiry, a release path and a lock the user can
/// walk away from, which is a reservation system — worth building when it is asked for, and not
/// worth pretending to have.
/// </para>
/// </remarks>
public sealed record GetAvailabilityQuery : IRequest<Result<AvailabilityView>>
{
    public required Guid ResourceId { get; init; }

    public required DateOnly Date { get; init; }

    /// <summary>How long the appointment needs. A test drive is not an inspection.</summary>
    public required int DurationMinutes { get; init; }

    /// <summary>Whether the appointment is at the customer, which brings the travel buffer into play.</summary>
    public string LocationKindCode { get; init; } = LocationKinds.AtResource;
}

public sealed record AvailabilityView(
    Guid ResourceId,
    DateOnly Date,
    int SlotMinutes,
    IReadOnlyList<AvailableSlot> Slots);

public sealed record AvailableSlot(DateTimeOffset StartsAt, DateTimeOffset EndsAt, int RemainingCapacity);

public sealed class GetAvailabilityHandler : IRequestHandler<GetAvailabilityQuery, Result<AvailabilityView>>
{
    private readonly IBookableResourceRepository _resources;
    private readonly IBookingRepository _bookings;
    private readonly IClock _clock;

    public GetAvailabilityHandler(
        IBookableResourceRepository resources,
        IBookingRepository bookings,
        IClock clock)
    {
        _resources = resources;
        _bookings = bookings;
        _clock = clock;
    }

    public async Task<Result<AvailabilityView>> Handle(
        GetAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DurationMinutes <= 0)
        {
            return Error.Validation("booking.duration_invalid", "An appointment needs a positive duration.");
        }

        var resource = await _resources.GetAsync(request.ResourceId, cancellationToken);
        if (resource is null)
        {
            return Error.NotFound("booking.resource_not_found", "No bookable resource with that id.")
                .With("resourceId", request.ResourceId);
        }

        var windows = resource.WindowsOn(request.Date);
        if (windows.Count == 0)
        {
            return new AvailabilityView(resource.Id, request.Date, resource.SlotMinutes, []);
        }

        var now = _clock.UtcNow;
        var earliest = now.AddMinutes(resource.MinimumNoticeMinutes);
        var latest = now.AddDays(resource.BookingHorizonDays);

        var buffer = string.Equals(request.LocationKindCode, LocationKinds.AtCustomer, StringComparison.Ordinal)
            ? resource.TravelBufferMinutes
            : 0;

        // One query for the whole day rather than one per candidate slot. A day has up to forty-eight
        // slots on a half-hour grid, and forty-eight round trips to render one date picker is how a
        // calendar becomes the slowest screen in the product.
        var dayStart = ToInstant(request.Date, TimeOnly.MinValue).AddMinutes(-buffer);
        var dayEnd = ToInstant(request.Date.AddDays(1), TimeOnly.MinValue).AddMinutes(buffer);

        var existing = await _bookings.GetOverlappingAsync(resource.Id, dayStart, dayEnd, cancellationToken);

        var slots = new List<AvailableSlot>();

        foreach (var window in windows)
        {
            var cursor = ToInstant(request.Date, window.OpensAt);
            var closes = ToInstant(request.Date, window.ClosesAt);

            while (cursor.AddMinutes(request.DurationMinutes) <= closes)
            {
                var startsAt = cursor;
                var endsAt = cursor.AddMinutes(request.DurationMinutes);
                cursor = cursor.AddMinutes(resource.SlotMinutes);

                if (startsAt < earliest || startsAt > latest)
                {
                    continue;
                }

                var occupiedFrom = startsAt.AddMinutes(-buffer);
                var occupiedTo = endsAt.AddMinutes(buffer);

                var taken = existing.Count(b => b.OccupiedFrom < occupiedTo && b.OccupiedTo > occupiedFrom);
                var remaining = resource.Capacity - taken;

                if (remaining > 0)
                {
                    slots.Add(new AvailableSlot(startsAt, endsAt, remaining));
                }
            }
        }

        return new AvailabilityView(resource.Id, request.Date, resource.SlotMinutes, slots);
    }

    /// <summary>
    /// Combines a date and a wall-clock opening time into an instant.
    /// </summary>
    /// <remarks>
    /// Opening hours are IST wall-clock, and India has one fixed offset with no daylight saving, so
    /// this is a constant offset rather than a time-zone conversion. <c>SystemClock.IstOffset</c> is
    /// the single place that fact is stated; duplicating <c>+05:30</c> here would be a second place
    /// for it to be wrong.
    /// </remarks>
    private static DateTimeOffset ToInstant(DateOnly date, TimeOnly time)
        => new(date.ToDateTime(time), SystemClock.IstOffset);
}
