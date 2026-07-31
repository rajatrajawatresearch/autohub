using System.Text.Json;
using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Persistence.Outbox;

/// <summary>
/// A domain event, durably queued in the same transaction as the state change that raised it.
/// </summary>
/// <remarks>
/// This row is the reason "the deal was created but nobody was told" is not a state this system can
/// reach. Writing the aggregate and publishing the event are two different systems, and any ordering
/// of two systems without a shared transaction has a window: publish-then-commit can announce a deal
/// that was rolled back, and commit-then-publish can lose the announcement to a process that dies in
/// between. Writing the event as a <i>row</i>, in the aggregate's own transaction, collapses both
/// failures — either both are committed or neither is — and delivery becomes a separate, retryable
/// concern owned by <see cref="OutboxProcessor"/>.
/// <para>
/// The guarantee is at-least-once, not exactly-once, and it cannot be anything else: a handler that
/// succeeds and then loses the acknowledgement will be retried. That is why every domain event
/// handler in this codebase must be idempotent, and it is not a detail to be discovered later.
/// </para>
/// </remarks>
public sealed class OutboxMessage
{
    private OutboxMessage(
        Guid id,
        DateTimeOffset occurredAt,
        string eventType,
        Guid aggregateId,
        string aggregateType,
        JsonNode payload)
    {
        Id = id;
        OccurredAt = occurredAt;
        EventType = eventType;
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        Payload = payload;
        NextAttemptAt = occurredAt;
    }

    private OutboxMessage() { }

    /// <summary>
    /// UUID v7, so the table's primary key is also its insertion order and the claim query's
    /// tie-breaker costs nothing.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// When the domain said it happened — the event's own timestamp, not the row's insert time.
    /// </summary>
    /// <remarks>
    /// The distinction is load-bearing for ordering. Two events raised inside one transaction are
    /// inserted at the same instant but happened in a definite order, and a consumer replaying them
    /// by insert time can see a lot closing before the bid that closed it.
    /// </remarks>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Versioned wire name, e.g. <c>auction.lot_won.v1</c>. Never edited once shipped.</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// The aggregate the event is about, and the partition key delivery is ordered by.
    /// </summary>
    /// <remarks>
    /// Ordering is guaranteed <b>per aggregate</b> and nowhere else, which is the only promise a
    /// horizontally scalable processor can keep. Two events about one lot are delivered in order;
    /// an event about a lot and an event about a loan are not ordered relative to each other, and
    /// nothing should assume they are.
    /// </remarks>
    public Guid AggregateId { get; private set; }

    /// <summary>
    /// The CLR type name of the aggregate that raised it. Diagnostic, not functional — it turns
    /// "which module produced this?" from an archaeology exercise into a column.
    /// </summary>
    public string AggregateType { get; private set; } = string.Empty;

    /// <summary>The serialised event, as <c>jsonb</c>.</summary>
    public JsonNode Payload { get; private set; } = new JsonObject();

    /// <summary>How many delivery attempts have been made. Drives backoff and the poison limit.</summary>
    public int Attempts { get; private set; }

    /// <summary>
    /// When the next attempt becomes eligible. Set to <see cref="OccurredAt"/> on insert so a fresh
    /// message is claimed immediately, and pushed out exponentially on each failure.
    /// </summary>
    public DateTimeOffset NextAttemptAt { get; private set; }

    /// <summary>Null until delivered. The claim query's filter, and a partial index's predicate.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>
    /// The last failure, truncated. Kept on the row rather than only in logs because the row is what
    /// an operator looks at when a message is stuck, and correlating it to a log line an hour later
    /// is a step nobody takes at 3am.
    /// </summary>
    public string? Error { get; private set; }

    public bool IsProcessed => ProcessedAt.HasValue;

    /// <summary>
    /// Captures a raised domain event.
    /// </summary>
    /// <remarks>
    /// Serialised against the event's runtime type rather than <see cref="IDomainEvent"/>, because
    /// serialising through the interface would emit four properties and drop every field the event
    /// actually carries — a silent, total data loss that looks like a working outbox.
    /// </remarks>
    public static OutboxMessage From(IDomainEvent domainEvent, string aggregateType, JsonSerializerOptions options)
    {
        var payload = JsonSerializer.SerializeToNode(domainEvent, domainEvent.GetType(), options)
            ?? new JsonObject();

        return new OutboxMessage(
            UuidV7.New(domainEvent.OccurredAt),
            domainEvent.OccurredAt,
            domainEvent.EventType,
            domainEvent.AggregateId,
            aggregateType,
            payload);
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedAt = now;
        Error = null;
    }

    /// <summary>
    /// Records a failed attempt and schedules the retry.
    /// </summary>
    /// <remarks>
    /// Exponential, because the overwhelmingly common cause of a failed delivery is a dependency
    /// that is down, and retrying a down dependency every second is how a partial outage becomes a
    /// total one. Capped, because an unbounded backoff means a message raised during a ten-minute
    /// incident is next attempted some time next week.
    /// </remarks>
    public void MarkFailed(DateTimeOffset now, string error, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        Attempts++;
        Error = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;

        var exponent = Math.Min(Attempts, MaxBackoffExponent);
        var delayTicks = Math.Min(baseDelay.Ticks * (1L << exponent), maxDelay.Ticks);

        NextAttemptAt = now.AddTicks(delayTicks);
    }

    /// <summary>
    /// Pushes a message out without counting an attempt, because it was never attempted.
    /// </summary>
    /// <remarks>
    /// Used for the messages queued behind one that just failed for the same aggregate. Counting an
    /// attempt against them would poison a perfectly deliverable message for the sin of arriving
    /// after a broken one; leaving them eligible would have the processor reclaim the same rows
    /// every poll interval for as long as the head stays stuck.
    /// </remarks>
    public void MarkDeferred(DateTimeOffset now, TimeSpan delay) => NextAttemptAt = now.Add(delay);

    /// <summary>Whether this message has exhausted its attempts and must stop being retried.</summary>
    public bool IsPoisoned(int maxAttempts) => Attempts >= maxAttempts;

    private const int MaxErrorLength = 2000;

    // 2^12 × the base delay is roughly an hour at a five-second base — far enough out that a long
    // outage is not being hammered, near enough that recovery is measured in minutes, not days.
    private const int MaxBackoffExponent = 12;
}
