namespace AutoHub.SharedKernel.Time;

/// <summary>
/// The current instant, injected rather than read from a static.
/// </summary>
/// <remarks>
/// Two reasons, both load-bearing. First, an auction that auto-extends when a bid lands inside the
/// last thirty seconds cannot be tested against a wall clock. Second, "now" must come from one
/// authority: if two API nodes disagree by 400ms, two bidders disagree about who was first, and the
/// loser is right to be angry. Every deadline in this system — bid windows, payment windows, rental
/// late-fee accrual, policy expiry — is evaluated against this.
/// </remarks>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly TodayInIst { get; }
}

public sealed class SystemClock : IClock
{
    /// <summary>
    /// India has one time zone and no daylight saving, so IST is a fixed +05:30 offset. Resolving a
    /// tz database id here would add a lookup and a deployment dependency for no benefit.
    /// </summary>
    public static readonly TimeSpan IstOffset = TimeSpan.FromMinutes(330);

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly TodayInIst => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(IstOffset).Date);
}
