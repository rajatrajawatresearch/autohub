namespace AutoHub.Modules.Bookings.Domain;

/// <summary>
/// The workflow registry key a booking's lifecycle is defined under, and the verbs this module fires.
/// </summary>
/// <remarks>
/// One workflow key for all eight purposes, and that is the point of the module. If test drives ever
/// need a different lifecycle from inspections — a confirmation step, an approval, a reminder gate —
/// that is a second published definition under a second key, chosen per purpose in configuration,
/// and not a second booking table.
/// <para>
/// <see cref="Release"/> is the timer: an unconfirmed booking gives its slot back after a period the
/// registry sets, wired as a transition with <c>automaticAfterMinutes</c>. Written as a background
/// job with a hardcoded window, "hold home visits for two hours instead of thirty minutes" is a
/// deploy.
/// </para>
/// </remarks>
public static class BookingWorkflow
{
    public const string Key = "booking";

    public const string EntityType = "booking.booking";

    public const string Confirm = "confirm";

    public const string Reschedule = "reschedule";

    public const string Start = "start";

    public const string Complete = "complete";

    public const string Cancel = "cancel";

    public const string NoShow = "no_show";

    /// <summary>Fires on a timer when a booking is never confirmed, returning the slot to the calendar.</summary>
    public const string Release = "release";
}

/// <summary>
/// The pricing registry scopes a booking is charged under.
/// </summary>
/// <remarks>
/// Technical identifiers naming the moments this module asks the registry about. Whether either
/// carries a fee at all, how much, in which city and for which customer tier, is entirely rules.
/// </remarks>
public static class BookingPricingScopes
{
    public const string Cancellation = "booking.cancellation";

    public const string NoShow = "booking.no_show";
}
