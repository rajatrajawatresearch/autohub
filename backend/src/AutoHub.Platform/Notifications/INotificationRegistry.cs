using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Notifications;

/// <summary>
/// Turns one domain event into the exact list of messages that should be sent. Registry 7.
/// </summary>
/// <remarks>
/// <b>Resolution only.</b> This registry decides who, on what channel, with which template and which
/// variables — and stops. Actually calling FCM, a DLT-registered SMS gateway or the WhatsApp
/// Business API is the Notifications module's job, over the integration registry, with its own
/// retries and its own delivery ledger.
/// <para>
/// The split is what makes both halves testable. Deciding "a gold dealer in Pune outbid at 11pm gets
/// an in-app row now and a WhatsApp at 7am" is pure logic over registry data, with no network in it;
/// delivering that decision is all network and no logic. Fused together, neither can be tested
/// without the other.
/// </para>
/// </remarks>
public interface INotificationRegistry
{
    /// <summary>
    /// Resolves an event that has already been raised.
    /// </summary>
    /// <remarks>
    /// Takes <see cref="IDomainEvent"/> directly because that is what the outbox dispatcher holds,
    /// and requiring every caller to unpack it into a request would be twenty chances to pass the
    /// wrong aggregate id.
    /// </remarks>
    Task<Result<IReadOnlyList<NotificationDispatch>>> ResolveAsync(
        IDomainEvent domainEvent,
        JsonNode? payload,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves from an explicit request. Used by the admin "preview this rule" screen and by
    /// scheduled campaigns, neither of which has a domain event to hand.
    /// </summary>
    Task<Result<IReadOnlyList<NotificationDispatch>>> ResolveAsync(
        NotificationResolutionRequest request,
        CancellationToken cancellationToken);
}

public sealed record NotificationResolutionRequest
{
    /// <summary>Versioned event name, matching <see cref="IDomainEvent.EventType"/>.</summary>
    public required string EventType { get; init; }

    public required Guid AggregateId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// The facts, serving three jobs at once: the condition is evaluated against it, the audience
    /// resolver reads its ids out of it, and its leaf values become the template variables.
    /// </summary>
    /// <remarks>
    /// One payload rather than three parameters because they must agree. A condition that matched on
    /// <c>amountMinor</c> and a template that then rendered a different amount would be a defect
    /// nobody could reproduce.
    /// </remarks>
    public JsonNode? Payload { get; init; }

    /// <summary>Who caused it. Null for system-originated events like a timer firing.</summary>
    public Guid? ActorId { get; init; }

    /// <summary>
    /// Bypasses dedupe and throttling. Set only by the admin preview screen, so that testing a rule
    /// twice in a minute actually shows something twice.
    /// </summary>
    public bool IsPreview { get; init; }
}

/// <summary>One message, resolved and ready for the Notifications module to deliver.</summary>
public sealed record NotificationDispatch
{
    /// <summary>The rule that produced this. Answers "why did I receive this?" from a log line.</summary>
    public required string EventCode { get; init; }

    public required NotificationRecipient Recipient { get; init; }

    public required NotificationChannel Channel { get; init; }

    public required string TemplateKey { get; init; }

    /// <summary>The locale the template was actually found in, after fallback.</summary>
    public required string Locale { get; init; }

    public string? Subject { get; init; }

    public required string Body { get; init; }

    /// <summary>
    /// The DLT / WhatsApp template id the gateway must be given. Null for channels that accept free
    /// text.
    /// </summary>
    public string? ProviderTemplateId { get; init; }

    public string? SenderId { get; init; }

    /// <summary>
    /// The variables, carried alongside the rendered body rather than instead of it.
    /// </summary>
    /// <remarks>
    /// Indian SMS and WhatsApp gateways do not accept a finished string — they take the registered
    /// template id plus an ordered set of variable values, and assemble the message themselves. So
    /// the dispatcher needs the variables. Push and email need the rendered text. Sending both is
    /// cheaper than making every adapter re-render and risk the two disagreeing.
    /// </remarks>
    public required IReadOnlyDictionary<string, string?> Variables { get; init; }

    /// <summary>
    /// Identity for suppression. The delivery side also stores it, so a redelivered outbox message
    /// cannot produce a second notification.
    /// </summary>
    public required string DedupeKey { get; init; }

    public required NotificationUrgency Urgency { get; init; }

    /// <summary>
    /// Set when quiet hours held the message. Null means send now.
    /// </summary>
    public DateTimeOffset? DeferUntil { get; init; }
}

/// <summary>
/// A person to notify, with the language they read in.
/// </summary>
/// <remarks>
/// Carries no phone number, device token or email address. Those are contact details that change,
/// need consent tracking, and belong to the Identity module — resolving them here would put PII into
/// a cache-heavy registry path and into every debug log that prints a dispatch.
/// </remarks>
public sealed record NotificationRecipient(Guid UserId, string? Locale)
{
    /// <summary>Marks a recipient the resolver knows has no usable address on a channel.</summary>
    public IReadOnlySet<NotificationChannel> UnreachableChannels { get; init; } =
        new HashSet<NotificationChannel>();
}

/// <summary>Loads notification registry rows. Implemented in persistence, read through the cache.</summary>
public interface INotificationRegistryStore
{
    Task<IReadOnlyList<NotificationEvent>> GetActiveEventsAsync(
        string eventType,
        CancellationToken cancellationToken);

    Task<NotificationTemplate?> GetTemplateAsync(
        string key,
        NotificationChannel channel,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every subscription row for these users in this category, plus their user-level defaults.
    /// </summary>
    /// <remarks>
    /// Batched by design. An auction closing notifies a hundred watchers, and resolving preferences
    /// one user at a time would put a hundred queries behind a single domain event.
    /// </remarks>
    Task<IReadOnlyList<NotificationSubscription>> GetSubscriptionsAsync(
        IReadOnlyCollection<Guid> userIds,
        string categoryCode,
        CancellationToken cancellationToken);
}

/// <summary>
/// Turns an audience descriptor into people. Implemented per module.
/// </summary>
/// <remarks>
/// The Platform cannot know who "participants" means for an auction lot versus a chat thread, and it
/// must not learn — that knowledge is exactly what module boundaries exist to keep out. Modules
/// register a resolver; the registry composes them.
/// </remarks>
public interface INotificationAudienceResolver
{
    /// <summary>The event type prefix this resolver serves, e.g. <c>auction</c>. Null serves all.</summary>
    string? EventTypePrefix { get; }

    Task<IReadOnlyList<NotificationRecipient>> ResolveAsync(
        NotificationAudience audience,
        NotificationResolutionRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Records what has been sent recently, so dedupe and throttling have something to consult.
/// </summary>
/// <remarks>
/// Backed by Redis in production: the counters are short-lived, high-churn, and worthless after
/// their window, which is the exact shape Postgres is worst at and Redis is best at.
/// </remarks>
public interface INotificationSuppressionStore
{
    /// <summary>
    /// Atomically records an attempt and reports whether it should be suppressed.
    /// </summary>
    /// <remarks>
    /// One call rather than check-then-record, because two API nodes handling the same outbox batch
    /// would otherwise both check, both see nothing, and both send.
    /// </remarks>
    Task<bool> TryReserveAsync(
        string dedupeKey,
        TimeSpan window,
        CancellationToken cancellationToken);

    /// <summary>Count of sends for a rule to a recipient inside a rolling window.</summary>
    Task<int> CountAndIncrementAsync(
        string throttleKey,
        TimeSpan window,
        CancellationToken cancellationToken);
}
