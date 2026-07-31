using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Platform.Auditing;

/// <summary>
/// One immutable answer to "who changed what, from what, to what, when, and from where".
/// </summary>
/// <remarks>
/// The quality floor demands this row for every admin and money action, and registry publishing is
/// the most consequential admin action in the product: whoever publishes a pricing rule set changes
/// what every dealer in India is charged, and whoever publishes a workflow can remove the approval
/// step before a payout.
/// <para>
/// It is deliberately <b>not</b> the same mechanism as <see cref="IAuditable"/>. Those four columns
/// answer "who last touched this row" and are overwritten by the next write; this table answers
/// "what did the change actually do", keeps the before and after, and is append-only. A system with
/// only the former cannot answer the question an investigation asks.
/// </para>
/// <para>
/// <see cref="Before"/> and <see cref="After"/> are JSON rather than typed columns because the
/// registries they describe are themselves schemaless by design — the whole point of the taxonomy
/// registry is that nobody knows in advance which attributes a category will have. Typed audit
/// columns would need a migration every time a registry gained a field, which is exactly the
/// deployment this architecture exists to remove.
/// </para>
/// </remarks>
public sealed class AuditEntry : Entity
{
    private AuditEntry(
        Guid id,
        DateTimeOffset occurredAt,
        string action,
        string entityType,
        string? entityKey,
        Guid? entityId)
        : base(id)
    {
        OccurredAt = occurredAt;
        Action = action;
        EntityType = entityType;
        EntityKey = entityKey;
        EntityId = entityId;
    }

    private AuditEntry() { }

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// What was done, named with the permission that authorised it — <c>registry.pricing.publish</c>.
    /// </summary>
    /// <remarks>
    /// Reusing the permission code rather than inventing a parallel verb vocabulary means an access
    /// review can join "who may publish pricing" to "who has published pricing" without a lookup
    /// table that the two halves would eventually disagree about.
    /// </remarks>
    public string Action { get; private set; } = string.Empty;

    /// <summary>The thing acted on, e.g. <c>platform.pricing_rule_set</c>.</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>
    /// The natural key an operator would recognise — <c>auction.win</c>, <c>sell_vehicle</c>.
    /// </summary>
    /// <remarks>
    /// Stored alongside the id because the id is meaningless in a support conversation, and because
    /// registry rows are recreated: a rolled-back rule set has a new id and the same scope, and the
    /// audit trail has to read as one story rather than as two unrelated objects.
    /// </remarks>
    public string? EntityKey { get; private set; }

    public Guid? EntityId { get; private set; }

    /// <summary>Null when a scheduler or a migration acted. Distinguishable from "unknown".</summary>
    public Guid? ActorId { get; private set; }

    /// <summary>The "from where" the quality floor asks for. Nullable — a background sweep has none.</summary>
    public string? ActorIp { get; private set; }

    /// <summary>Correlates the row with the request's logs and traces. This is how you find the why.</summary>
    public string? TraceId { get; private set; }

    /// <summary>One human sentence, for the admin activity feed. Never parsed.</summary>
    public string? Summary { get; private set; }

    public JsonNode? Before { get; private set; }

    public JsonNode? After { get; private set; }

    public static AuditEntry Record(
        DateTimeOffset occurredAt,
        string action,
        string entityType,
        string? entityKey = null,
        Guid? entityId = null,
        Guid? actorId = null,
        string? actorIp = null,
        string? traceId = null,
        string? summary = null,
        JsonNode? before = null,
        JsonNode? after = null)
        => new(UuidV7.New(), occurredAt, action, entityType, entityKey, entityId)
        {
            ActorId = actorId,
            ActorIp = actorIp,
            TraceId = traceId,
            Summary = summary,
            Before = before,
            After = after,
        };
}

/// <summary>
/// Appends to the audit trail.
/// </summary>
/// <remarks>
/// Declared here and implemented in the persistence layer, like every other registry store, so
/// <c>AutoHub.Platform</c> stays free of EF Core.
/// <para>
/// <b>The write joins the caller's transaction rather than opening its own.</b> An audit row that
/// commits independently can survive a rolled-back publish — a trail claiming a change that never
/// happened is worse than no trail, because it is believed.
/// </para>
/// </remarks>
public interface IAuditTrail
{
    void Record(AuditEntry entry);

    /// <summary>Most recent first. Powers the admin activity feed; always bounded.</summary>
    Task<IReadOnlyList<AuditEntry>> RecentAsync(
        string? action,
        int limit,
        CancellationToken cancellationToken);
}
