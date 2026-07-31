namespace AutoHub.SharedKernel.Domain;

/// <summary>Anything with an identity that outlives its field values.</summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id) => Id = id;

    /// <summary>Required by EF Core's materialiser. Never call this yourself.</summary>
    protected Entity() { }

    public Guid Id { get; protected set; }

    public bool Equals(Entity? other)
        => other is not null
           && other.GetType() == GetType()
           && other.Id == Id
           && Id != Guid.Empty;

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}

/// <summary>
/// The consistency boundary. A transaction saves exactly one aggregate; anything else that must
/// change reacts to a domain event afterwards.
/// </summary>
/// <remarks>
/// This is the rule that keeps twenty modules from turning into one tangled transaction. When an
/// auction lot is won, the auction module saves the lot and raises <c>LotWon</c>; the deals module
/// creates the deal, the payments module opens the escrow, and notifications tells the dealer —
/// each in its own transaction, each retryable, none of them able to deadlock the bid path.
/// </remarks>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }

    /// <summary>
    /// Optimistic concurrency token, mapped to a PostgreSQL <c>xmin</c> system column.
    /// Two admins editing the same commission rule is a conflict we surface, not one we silently lose.
    /// </summary>
    public uint RowVersion { get; protected set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Called by the persistence layer once events have been copied into the outbox.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
