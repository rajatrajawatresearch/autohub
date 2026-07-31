using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Notifications;

/// <summary>
/// The rule that turns a domain event into messages: who hears about it, on what, saying what.
/// </summary>
/// <remarks>
/// Modules raise events and know nothing about notifications. The auction module raises
/// <c>auction.bid_placed.v1</c>; whether that becomes a push to the outbid dealer, a WhatsApp to the
/// seller, both, or nothing at all is a row here. That separation is what makes "change the WhatsApp
/// bid-outbid message in Marathi" a config change instead of a release.
/// <para>
/// One domain event maps to <b>many</b> of these — one per audience. <c>auction.bid_placed.v1</c>
/// produces "you were outbid" to the previous high bidder and "new bid received" to the seller, and
/// those are separate rows with separate templates, separate channels, and separate throttles.
/// Trying to express both in one row is where notification systems become unreadable.
/// </para>
/// </remarks>
public sealed class NotificationEvent : AggregateRoot, IAuditable
{
    private NotificationEvent(
        Guid id,
        string code,
        string eventType,
        string categoryCode,
        string templateKey,
        NotificationAudience audience)
        : base(id)
    {
        Code = code;
        EventType = eventType;
        CategoryCode = categoryCode;
        TemplateKey = templateKey;
        Audience = audience;
    }

    private NotificationEvent() { }

    /// <summary>
    /// Identifies this rule, e.g. <c>auction.bid_placed.notify_previous_bidder</c>. Appears on every
    /// dispatch so "why did I get this?" is answerable from a log line.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Matches <see cref="IDomainEvent.EventType"/> exactly, version suffix included —
    /// <c>auction.lot_won.v1</c>.
    /// </summary>
    /// <remarks>
    /// Binding to the versioned name rather than a stripped one is deliberate. When
    /// <c>lot_won.v2</c> ships alongside v1 with a different payload, the v1 rule keeps firing for
    /// v1 events with v1's variables, and someone decides consciously whether the v2 rule says the
    /// same thing. A loose match would silently feed v2 payloads to a template expecting v1 fields.
    /// </remarks>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// The preference bucket a user actually toggles: <c>auction</c>, <c>deals</c>, <c>marketing</c>.
    /// </summary>
    /// <remarks>
    /// Users do not manage two hundred switches. They manage six categories, and every rule declares
    /// which one it belongs to — which also means adding a rule inherits the user's existing choice
    /// rather than defaulting them back into something they had already turned off.
    /// </remarks>
    public string CategoryCode { get; private set; } = string.Empty;

    public NotificationAudience Audience { get; private set; } = NotificationAudience.Actor();

    /// <summary>Channels this rule may use, subject to the recipient's preferences.</summary>
    public string ChannelsCsv { get; private set; } = string.Empty;

    public string TemplateKey { get; private set; } = string.Empty;

    /// <summary>
    /// JsonLogic over the event payload. Null means every occurrence notifies.
    /// </summary>
    /// <remarks>
    /// This is how "only tell me about bids over ₹5 lakh" exists without a code path. It is also how
    /// the same event serves different segments: one rule with <c>{"==":[{"var":"party.tier"},
    /// "gold"}]}</c> and a warmer template, one without.
    /// </remarks>
    public string? ConditionJson { get; private set; }

    public NotificationUrgency Urgency { get; private set; } = NotificationUrgency.Informational;

    /// <summary>
    /// Suppresses an identical message to the same recipient inside this window.
    /// </summary>
    /// <remarks>
    /// The concrete failure: an auction lot in its final minute takes forty bids, and the losing
    /// dealer's phone buzzes forty times. Dedupe collapses that to one message per window per
    /// <see cref="DedupeKeyTemplate"/>, which is why the key is per-lot rather than global — being
    /// outbid on a different lot is genuinely new information.
    /// </remarks>
    public int? DedupeWindowSeconds { get; private set; }

    /// <summary>
    /// Placeholder expression building the dedupe identity, e.g. <c>{lotId}</c>. Null dedupes on the
    /// event code and recipient alone.
    /// </summary>
    public string? DedupeKeyTemplate { get; private set; }

    /// <summary>
    /// Hard ceiling on messages from this rule per recipient per window, regardless of dedupe key.
    /// </summary>
    /// <remarks>
    /// Dedupe stops repetition; throttling stops volume. A dealer watching thirty lots would
    /// otherwise receive thirty distinct, individually reasonable messages in the closing minutes,
    /// and would turn the channel off entirely — which costs us every future notification, not just
    /// the noisy ones.
    /// </remarks>
    public int? ThrottleMaxPerWindow { get; private set; }

    public int? ThrottleWindowSeconds { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static NotificationEvent Draft(
        string code,
        string eventType,
        string categoryCode,
        string templateKey,
        NotificationAudience audience,
        IEnumerable<NotificationChannel> channels)
        => new(UuidV7.New(), code, eventType, categoryCode, templateKey, audience)
        {
            ChannelsCsv = string.Join(',', channels.Select(c => c.ToString())),
        };

    public IReadOnlyList<NotificationChannel> Channels()
    {
        if (string.IsNullOrWhiteSpace(ChannelsCsv))
        {
            return [];
        }

        var channels = new List<NotificationChannel>();

        foreach (var token in ChannelsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<NotificationChannel>(token, ignoreCase: true, out var channel))
            {
                channels.Add(channel);
            }
        }

        return channels;
    }

    public void SetCondition(string? conditionJson) => ConditionJson = conditionJson;

    public void SetUrgency(NotificationUrgency urgency) => Urgency = urgency;

    public void SetDedupe(TimeSpan window, string? keyTemplate)
    {
        DedupeWindowSeconds = (int)window.TotalSeconds;
        DedupeKeyTemplate = keyTemplate;
    }

    public void SetThrottle(int maxPerWindow, TimeSpan window)
    {
        ThrottleMaxPerWindow = maxPerWindow;
        ThrottleWindowSeconds = (int)window.TotalSeconds;
    }

    public Result Activate()
    {
        if (Channels().Count == 0)
        {
            return Error.Validation(
                "notification.event.no_channels",
                "A notification rule that reaches no channel would never send anything.");
        }

        IsActive = true;
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;
}

/// <summary>
/// Who a rule notifies, expressed as a descriptor the owning module resolves into people.
/// </summary>
/// <remarks>
/// The registry cannot know that the "seller" of an auction lot is the listing's owner and not the
/// dealer who consigned it — that is the auction module's model. So the registry stores the
/// <i>intent</i> and asks an <see cref="INotificationAudienceResolver"/> to turn it into user ids.
/// <para>
/// Note what is absent: there is no <c>Role</c> audience. The prime directive bans role checks, and
/// a notification aimed at "everyone who can moderate listings in Pune" is a permission-and-scope
/// question with exactly one correct answer source — the RBAC registry — not a role name copied into
/// a second place where it will drift.
/// </para>
/// </remarks>
public sealed record NotificationAudience
{
    private NotificationAudience(AudienceKind kind, string? selector)
    {
        Kind = kind;
        Selector = selector;
    }

    public AudienceKind Kind { get; }

    /// <summary>
    /// Interpreted per <see cref="Kind"/>: a permission code, a segment code, or a comma-separated
    /// list of user ids. Null where the kind is self-describing.
    /// </summary>
    public string? Selector { get; }

    /// <summary>The person who caused the event. "Your listing was approved."</summary>
    public static NotificationAudience Actor() => new(AudienceKind.Actor, null);

    /// <summary>The owner of the aggregate the event is about. "Your car received an offer."</summary>
    public static NotificationAudience AggregateOwner() => new(AudienceKind.AggregateOwner, null);

    /// <summary>Everyone with an active interest — watchers, bidders, participants in a chat.</summary>
    public static NotificationAudience Participants() => new(AudienceKind.Participants, null);

    /// <summary>Everyone holding a permission, within the scope the event carries.</summary>
    public static NotificationAudience PermissionHolders(string permissionCode)
        => new(AudienceKind.PermissionHolders, permissionCode);

    /// <summary>A saved audience from the analytics module — "gold dealers in Maharashtra".</summary>
    public static NotificationAudience Segment(string segmentCode) => new(AudienceKind.Segment, segmentCode);

    public static NotificationAudience ExplicitUsers(IEnumerable<Guid> userIds)
        => new(AudienceKind.ExplicitUsers, string.Join(',', userIds));
}

public enum AudienceKind
{
    Actor = 1,
    AggregateOwner = 2,
    Participants = 3,
    PermissionHolders = 4,
    Segment = 5,
    ExplicitUsers = 6,
}

/// <summary>
/// How much a message is allowed to interrupt.
/// </summary>
/// <remarks>
/// This is not a priority queue hint — it decides whether quiet hours and DND apply, which in India
/// is also a regulatory question. TRAI's DND framework exempts <see cref="Transactional"/> traffic
/// (an OTP, a payment confirmation) precisely because withholding it breaks something the user
/// asked for; <see cref="Promotional"/> traffic is not exempt and honouring the preference is
/// mandatory rather than polite.
/// </remarks>
public enum NotificationUrgency
{
    /// <summary>Marketing. Quiet hours, DND, and category preferences all apply.</summary>
    Promotional = 1,

    /// <summary>Something happened that the user will want to know about, but not right now.</summary>
    Informational = 2,

    /// <summary>The user is mid-flow and waiting: an OTP, a payment status, a bid confirmation.</summary>
    Transactional = 3,

    /// <summary>
    /// Time-critical and financially consequential — an auction closing, a payment window expiring,
    /// a lender's offer lapsing. Delivered through quiet hours because a silenced one costs money.
    /// </summary>
    Critical = 4,
}
