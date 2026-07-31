namespace AutoHub.SharedKernel.Domain;

/// <summary>
/// Something that happened, stated in the past tense, that other modules may care about.
/// </summary>
/// <remarks>
/// Every event is written to the outbox inside the same transaction as the state change that
/// produced it, so "the deal was created but nobody was told" is not a state this system can reach.
/// <para>
/// <see cref="EventType"/> is the wire contract and is versioned in its name
/// (<c>listings.listing_published.v1</c>). A breaking payload change means a new version alongside
/// the old, never an edit — a partner bank's webhook consumer is not redeployed on our schedule.
/// </para>
/// </remarks>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }

    /// <summary>Stable versioned name, e.g. <c>auction.lot_won.v1</c>.</summary>
    string EventType { get; }

    /// <summary>
    /// The aggregate this event is about. Used as the outbox partition key so events for one
    /// vehicle, one auction lot, or one deal are always delivered in the order they happened.
    /// </summary>
    Guid AggregateId { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent(Guid aggregateId, DateTimeOffset occurredAt)
    {
        AggregateId = aggregateId;
        OccurredAt = occurredAt;
    }

    public Guid EventId { get; init; } = Ids.UuidV7.New();

    public DateTimeOffset OccurredAt { get; init; }

    public Guid AggregateId { get; init; }

    public abstract string EventType { get; }
}
