using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Time;

namespace AutoHub.Platform.Notifications;

/// <summary>
/// One user's answer to "what may we send you, where, and when".
/// </summary>
/// <remarks>
/// A row with a null <see cref="CategoryCode"/> is the user's default and applies to every category
/// they have not overridden. That shape exists so quiet hours and DND — which are decisions about a
/// <i>person</i>, not about a topic — are stated once, while channel choices can still be made per
/// topic. Duplicating quiet hours onto every category row is how a user ends up muted for auctions
/// and woken up by deals.
/// <para>
/// Absence of a row is not absence of consent. It means "not yet decided", and the resolution path
/// applies the platform default rather than treating silence as a no — a new user who has never
/// opened settings still needs their OTP.
/// </para>
/// </remarks>
public sealed class NotificationSubscription : AggregateRoot, IAuditable
{
    private readonly List<ChannelPreference> _channels = [];

    private NotificationSubscription(Guid id, Guid userId, string? categoryCode) : base(id)
    {
        UserId = userId;
        CategoryCode = categoryCode;
    }

    private NotificationSubscription() { }

    public Guid UserId { get; private set; }

    /// <summary>Null is the user-level default that every category inherits.</summary>
    public string? CategoryCode { get; private set; }

    public IReadOnlyList<ChannelPreference> Channels => _channels;

    /// <summary>Null means "any hour is fine".</summary>
    public QuietHours? QuietHours { get; private set; }

    /// <summary>
    /// A blanket "do not interrupt me" that outranks every individual channel choice.
    /// </summary>
    /// <remarks>
    /// Separate from turning each channel off because it is a different intent with a different
    /// lifetime — a dealer switching DND on for a fortnight's leave expects to switch it off and get
    /// their previous settings back, not to rebuild them.
    /// </remarks>
    public bool DoNotDisturb { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static NotificationSubscription ForUser(Guid userId, string? categoryCode = null)
        => new(UuidV7.New(), userId, categoryCode);

    public void SetChannel(NotificationChannel channel, bool isEnabled)
    {
        var existing = _channels.Find(c => c.Channel == channel);

        if (existing is null)
        {
            _channels.Add(new ChannelPreference(channel, isEnabled));
            return;
        }

        existing.Set(isEnabled);
    }

    public void SetQuietHours(QuietHours? quietHours) => QuietHours = quietHours;

    public void SetDoNotDisturb(bool enabled) => DoNotDisturb = enabled;

    /// <summary>
    /// Whether this subscription permits a channel. Null means "no opinion" so the caller can fall
    /// through to the user-level default rather than reading silence as a refusal.
    /// </summary>
    public bool? Allows(NotificationChannel channel)
        => _channels.Find(c => c.Channel == channel)?.IsEnabled;
}

public sealed class ChannelPreference
{
    public ChannelPreference(NotificationChannel channel, bool isEnabled)
    {
        Channel = channel;
        IsEnabled = isEnabled;
    }

    private ChannelPreference() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid NotificationSubscriptionId { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public bool IsEnabled { get; private set; }

    internal void Set(bool isEnabled) => IsEnabled = isEnabled;
}

/// <summary>
/// A daily window during which intrusive channels stay silent, expressed in IST.
/// </summary>
/// <remarks>
/// Stored as minutes past midnight rather than a <c>TimeOnly</c> pair so the wrap across midnight —
/// which is the only interesting case, since quiet hours are almost always 22:00 to 07:00 — is
/// arithmetic rather than a special case somebody forgets.
/// <para>
/// IST, not UTC, and not the device's zone. India has one time zone; a user in Kochi and a user in
/// Guwahati both mean "ten at night" and neither should have their preference reinterpreted because
/// a phone was left on airport time.
/// </para>
/// </remarks>
public sealed record QuietHours
{
    public QuietHours(int startMinuteIst, int endMinuteIst)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startMinuteIst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startMinuteIst, MinutesPerDay);
        ArgumentOutOfRangeException.ThrowIfNegative(endMinuteIst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(endMinuteIst, MinutesPerDay);

        StartMinuteIst = startMinuteIst;
        EndMinuteIst = endMinuteIst;
    }

    private const int MinutesPerDay = 24 * 60;

    public int StartMinuteIst { get; }

    public int EndMinuteIst { get; }

    public static QuietHours Between(int startHourIst, int endHourIst)
        => new(startHourIst * 60, endHourIst * 60);

    public bool Contains(DateTimeOffset instant)
    {
        var minute = MinuteOfDayIst(instant);

        // A window that ends before it starts spans midnight: 22:00–07:00 is "at or after 22:00, or
        // before 07:00", not an empty range.
        return StartMinuteIst <= EndMinuteIst
            ? minute >= StartMinuteIst && minute < EndMinuteIst
            : minute >= StartMinuteIst || minute < EndMinuteIst;
    }

    /// <summary>
    /// When the window next opens, so a held message can be scheduled rather than dropped.
    /// </summary>
    /// <remarks>
    /// Deferring beats discarding for anything worth sending at all: a dealer who is told at 07:00
    /// that their payout cleared overnight is informed, whereas one who is never told assumes it
    /// failed and opens a ticket.
    /// </remarks>
    public DateTimeOffset NextOpening(DateTimeOffset instant)
    {
        if (!Contains(instant))
        {
            return instant;
        }

        var ist = instant.ToOffset(SystemClock.IstOffset);
        var minute = MinuteOfDayIst(instant);
        var minutesUntilOpen = minute < EndMinuteIst
            ? EndMinuteIst - minute
            : MinutesPerDay - minute + EndMinuteIst;

        return ist.AddMinutes(minutesUntilOpen);
    }

    private static int MinuteOfDayIst(DateTimeOffset instant)
    {
        var ist = instant.ToOffset(SystemClock.IstOffset);
        return (ist.Hour * 60) + ist.Minute;
    }
}
