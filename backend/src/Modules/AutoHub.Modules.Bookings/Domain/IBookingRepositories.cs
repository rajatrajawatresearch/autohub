namespace AutoHub.Modules.Bookings.Domain;

public interface IBookableResourceRepository
{
    Task<BookableResource?> GetAsync(Guid resourceId, CancellationToken cancellationToken);

    Task AddAsync(BookableResource resource, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IBookingRepository
{
    Task<Booking?> GetAsync(Guid bookingId, CancellationToken cancellationToken);

    /// <summary>
    /// Live bookings whose buffered window overlaps a range on one resource.
    /// </summary>
    /// <remarks>
    /// The one query both the availability list and the double-booking check run, so the two can
    /// never disagree about what counts as a clash. Implementations must compare
    /// <c>occupied_from</c>/<c>occupied_to</c> rather than the appointment times, and must exclude
    /// cancelled rows — a cancelled 11:00 has to stop blocking 11:00.
    /// </remarks>
    Task<IReadOnlyList<Booking>> GetOverlappingAsync(
        Guid resourceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    Task AddAsync(Booking booking, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
