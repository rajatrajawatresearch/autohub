using AutoHub.SharedKernel.Time;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// A clock the demo seeder drives by hand.
/// </summary>
/// <remarks>
/// Demo data is only convincing if it has a past. Every listing created at the same instant produces
/// a marketplace where nothing was listed yesterday, no lot has been running for six minutes, and
/// every workflow history row shares one timestamp — which is exactly what a screenshot of fake data
/// looks like.
/// <para>
/// The workflow engine takes its "now" from <see cref="IClock"/>, so handing it one the seeder can
/// wind forward is what gives the seeded history real gaps: seventeen hours in review, three days
/// live, ninety seconds between bids. Nothing else in the process sees this clock — the auditing
/// interceptor keeps the system one, because <c>created_at</c> is a record of when the row was
/// actually written and backdating it would be forging an audit trail rather than staging a demo.
/// </para>
/// </remarks>
internal sealed class SeedClock : IClock
{
    public SeedClock(DateTimeOffset start) => UtcNow = start;

    public DateTimeOffset UtcNow { get; private set; }

    public DateOnly TodayInIst
        => DateOnly.FromDateTime(UtcNow.ToOffset(SystemClock.IstOffset).Date);

    public void MoveTo(DateTimeOffset instant) => UtcNow = instant;

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
