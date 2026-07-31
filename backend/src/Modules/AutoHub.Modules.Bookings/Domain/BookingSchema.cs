namespace AutoHub.Modules.Bookings.Domain;

/// <summary>
/// The locking strategy that prevents double booking, stated explicitly.
/// </summary>
/// <remarks>
/// <b>The race.</b> Two customers open the same inspector's Tuesday, both see 11:00 free, both
/// submit. Each request reads the calendar, finds no clash, and inserts. Both succeed, and an
/// inspector who can only be in one place has two appointments. This is the booking equivalent of the
/// auction's two-dealers-one-millisecond problem, and it has the same shape: read, decide, write,
/// with a gap in the middle.
/// <para>
/// <b>Why not <c>SERIALIZABLE</c>.</b> It would work, and it would abort a large share of concurrent
/// bookings across the whole platform to protect a conflict that only exists between two bookings for
/// the same resource on the same day. The retries would fall on unrelated traffic.
/// </para>
/// <para>
/// <b>Why not a lock on the resource row.</b> <c>SELECT … FOR UPDATE</c> on the resource serialises
/// every booking for that resource across every date. A dealer with one busy demo car would find that
/// booking next Tuesday waits behind booking this Friday.
/// </para>
/// <para>
/// <b>The strategy: a transaction-scoped advisory lock keyed on resource and service date, plus a
/// database constraint behind it.</b>
/// </para>
/// <list type="number">
/// <item><b>Take <c>pg_advisory_xact_lock</c> on a hash of (resource id, service date).</b> Held
/// until the transaction ends, released automatically on commit or rollback — no lock to leak if the
/// process dies. Two bookings for different resources never contend; two for the same resource on
/// different days never contend; two for the same resource on the same day serialise, which is
/// exactly and only the conflict that exists. See <see cref="AcquireDayLockSql"/>.</item>
/// <item><b>Inside the lock, count overlapping bookings and compare to capacity.</b> Correct for any
/// capacity, because the lock makes read-then-write atomic for that resource-day.</item>
/// <item><b>An exclusion constraint as the backstop for capacity-1 resources.</b> See
/// <see cref="CreateExclusionConstraint"/>. It catches anything that reaches the table without taking
/// the lock — a repair script, a future service, a code path somebody adds and forgets to guard.
/// The advisory lock protects against concurrency; the constraint protects against everybody who
/// does not know about the advisory lock.</item>
/// </list>
/// <para>
/// <b>Why the constraint only covers capacity-1.</b> An exclusion constraint is pairwise: it refuses
/// a row that conflicts with any single existing row. "At most four overlapping" is a cardinality
/// rule over a set, and no pairwise constraint can express it. So the exclusive case — an inspector,
/// a bay, one specific vehicle, which is the overwhelming majority — gets a hard database guarantee,
/// and the capacity case relies on the lock, which is sound as long as every writer takes it. That
/// asymmetry is stated here rather than discovered later.
/// </para>
/// </remarks>
public static class BookingSchema
{
    public const string Schema = "booking";

    /// <summary>
    /// Locks one resource's day for the rest of the transaction.
    /// </summary>
    /// <remarks>
    /// Two <c>int4</c> keys rather than one <c>int8</c> so the resource and the date occupy separate
    /// halves of the advisory lock space and cannot collide across dates by hash accident.
    /// <c>hashtextextended</c> with a fixed seed is stable across sessions and versions, which
    /// <c>hashtext</c> historically was not.
    /// </remarks>
    public const string AcquireDayLockSql =
        "SELECT pg_advisory_xact_lock(hashtextextended(@resourceKey, 0), hashtextextended(@dayKey, 0))";

    /// <summary>
    /// Refuses two overlapping bookings on a resource that can only take one.
    /// </summary>
    /// <remarks>
    /// The range is built from <c>occupied_from</c> and <c>occupied_to</c> — the travel-buffered
    /// window — rather than from the appointment times, because the conflict a calendar has to
    /// prevent is "the inspector cannot get there", not "the appointments overlap".
    /// <para>
    /// The <c>WHERE</c> clause is what makes this usable: it applies only to exclusive resources and
    /// only to live bookings, so a cancelled 11:00 does not block a rebooked 11:00, and a
    /// four-capacity test-drive centre is not accidentally constrained to one.
    /// </para>
    /// <para>
    /// <c>gist</c> with <c>btree_gist</c> because the constraint mixes an equality operator on a uuid
    /// with an overlap operator on a range, and only that combination indexes both.
    /// </para>
    /// </remarks>
    public const string CreateExclusionConstraint = """
        CREATE EXTENSION IF NOT EXISTS btree_gist;

        ALTER TABLE booking.bookings
            DROP CONSTRAINT IF EXISTS ex_bookings_no_overlap;

        ALTER TABLE booking.bookings
            ADD CONSTRAINT ex_bookings_no_overlap
            EXCLUDE USING gist (
                bookable_resource_id WITH =,
                tstzrange(occupied_from, occupied_to, '[)') WITH &&
            )
            WHERE (is_exclusive AND cancelled_at IS NULL AND deleted_at IS NULL);
        """;

    public const string DropExclusionConstraint = """
        ALTER TABLE booking.bookings DROP CONSTRAINT IF EXISTS ex_bookings_no_overlap;
        """;

    /// <summary>Row-level checks the EF configuration attaches to <c>booking.bookings</c>.</summary>
    public static class Checks
    {
        public const string WindowName = "ck_bookings_window";

        public const string Window = "ends_at > starts_at";

        public const string OccupiedName = "ck_bookings_occupied_window";

        /// <summary>The buffered range must contain the appointment, or the calendar is lying.</summary>
        public const string Occupied = "occupied_from <= starts_at AND occupied_to >= ends_at";

        public const string OtpAttemptsName = "ck_bookings_otp_attempts";

        public const string OtpAttempts = "otp_attempts >= 0";
    }
}

/// <summary>
/// Takes the resource-day advisory lock.
/// </summary>
/// <remarks>
/// An interface because the lock is a PostgreSQL mechanism and this project does not reference EF
/// Core, and because a handler's behaviour under contention is worth testing without a database. The
/// implementation runs <see cref="BookingSchema.AcquireDayLockSql"/> on the ambient transaction, so
/// the caller must already be in one — a lock taken on its own connection is released immediately and
/// protects nothing.
/// </remarks>
public interface IResourceDayLock
{
    Task AcquireAsync(Guid resourceId, DateOnly serviceDate, CancellationToken cancellationToken);
}
