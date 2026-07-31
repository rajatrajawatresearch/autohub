using System.Globalization;
using AutoHub.Modules.Bookings.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AutoHub.Persistence.Repositories;

public sealed class EfBookableResourceRepository : IBookableResourceRepository
{
    private readonly AutoHubDbContext _context;

    public EfBookableResourceRepository(AutoHubDbContext context) => _context = context;

    /// <summary>
    /// Loads a resource with its whole calendar.
    /// </summary>
    /// <remarks>
    /// Both collections every time, because <c>WindowsOn</c> consults the exceptions before the
    /// windows and a resource loaded without its exceptions is one that is bookable on Diwali.
    /// </remarks>
    public async Task<BookableResource?> GetAsync(Guid resourceId, CancellationToken cancellationToken)
        => await _context.BookableResources
            .Include(resource => resource.Windows)
            .Include(resource => resource.Exceptions)
            .FirstOrDefaultAsync(resource => resource.Id == resourceId, cancellationToken);

    public async Task AddAsync(BookableResource resource, CancellationToken cancellationToken)
        => await _context.BookableResources.AddAsync(resource, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

public sealed class EfBookingRepository : IBookingRepository
{
    private readonly AutoHubDbContext _context;

    public EfBookingRepository(AutoHubDbContext context) => _context = context;

    public async Task<Booking?> GetAsync(Guid bookingId, CancellationToken cancellationToken)
        => await _context.Bookings
            .FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);

    /// <summary>
    /// Live bookings whose buffered window overlaps a range on one resource.
    /// </summary>
    /// <remarks>
    /// Compares <c>occupied_from</c>/<c>occupied_to</c> rather than the appointment times, exactly as
    /// the interface requires: the conflict a calendar prevents is "the inspector cannot get there",
    /// not "the appointments overlap". Cancelled rows are excluded so a cancelled 11:00 stops
    /// blocking 11:00; soft-deleted rows are excluded by the model-wide query filter.
    /// <para>
    /// The comparison is half-open — <c>from &lt; to</c> on both sides rather than <c>&lt;=</c> — so a
    /// booking that ends exactly when the next begins does not count as a clash. Back-to-back slots
    /// are the normal case on a 30-minute grid, and the closed comparison would have made every
    /// calendar half as available as it is.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<Booking>> GetOverlappingAsync(
        Guid resourceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
        => await _context.Bookings
            .Where(booking => booking.BookableResourceId == resourceId)
            .Where(booking => booking.CancelledAt == null)
            .Where(booking => booking.OccupiedFrom < to && booking.OccupiedTo > from)
            .OrderBy(booking => booking.OccupiedFrom)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
        => await _context.Bookings.AddAsync(booking, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// Takes the transaction-scoped advisory lock that makes check-then-insert safe.
/// </summary>
/// <remarks>
/// The strategy is stated in full in <see cref="BookingSchema"/>. What this class adds is the one
/// operational requirement that strategy has: the lock must run on the <b>ambient transaction</b>.
/// <c>pg_advisory_xact_lock</c> is released when the transaction ends, so a lock taken on a
/// connection with no transaction open is released the instant the statement returns and protects
/// nothing at all — the handler would run its overlap count completely unguarded and the failure
/// would only ever show up under concurrency, in production, as a double-booked inspector.
/// <para>
/// So this refuses to run outside one rather than silently doing nothing. The API's transaction
/// behaviour opens a transaction around every command, which is what makes the requirement satisfied
/// in practice; the guard is here for the caller that is added later and does not know.
/// </para>
/// <para>
/// The keys are passed as text and hashed by PostgreSQL rather than hashed in C#, so the lock space
/// is stable across processes and .NET versions — <c>string.GetHashCode</c> is randomised per process
/// and two API nodes would take different locks for the same resource-day.
/// </para>
/// </remarks>
public sealed class PostgresResourceDayLock : IResourceDayLock
{
    private readonly AutoHubDbContext _context;

    public PostgresResourceDayLock(AutoHubDbContext context) => _context = context;

    public async Task AcquireAsync(Guid resourceId, DateOnly serviceDate, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The resource-day lock must be taken inside a transaction. Taken outside one it is "
                + "released immediately and guards nothing, which would leave the overlap check "
                + "exposed to exactly the race it exists to close.");
        }

        var resourceKey = new NpgsqlParameter("resourceKey", resourceId.ToString("D", CultureInfo.InvariantCulture));
        var dayKey = new NpgsqlParameter("dayKey", serviceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        await _context.Database.ExecuteSqlRawAsync(
            BookingSchema.AcquireDayLockSql,
            [resourceKey, dayKey],
            cancellationToken);
    }
}
