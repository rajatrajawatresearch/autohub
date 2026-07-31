using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Workflow;

/// <summary>
/// The only way an entity's state changes.
/// </summary>
/// <remarks>
/// No module sets a status directly. Every lifecycle move goes through <see cref="FireAsync"/>,
/// which means the guard was evaluated, the permission was checked, the effects ran, and the audit
/// row was written — every time, for every entity, without any module having to remember to.
/// </remarks>
public interface IWorkflowEngine
{
    /// <summary>Begins a workflow for a newly created entity, placing it in the initial state.</summary>
    Task<Result<WorkflowSnapshot>> StartAsync(
        string workflowKey,
        Guid entityId,
        JsonNode? context,
        Guid? actorId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The entity's current state plus the actions this actor may fire right now.
    /// </summary>
    /// <remarks>
    /// Guards and permissions are evaluated here, server-side, so the client renders exactly the
    /// buttons that will work. A client that decides for itself which actions to show is a client
    /// that shows "Approve" to a customer and gets a 403 for its trouble.
    /// </remarks>
    Task<Result<WorkflowSnapshot>> GetSnapshotAsync(
        string workflowKey,
        Guid entityId,
        JsonNode? context,
        WorkflowActor actor,
        CancellationToken cancellationToken);

    /// <summary>Attempts a transition.</summary>
    Task<Result<WorkflowSnapshot>> FireAsync(
        WorkflowFireRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Runs transitions whose timer has come due — expired payment windows, unsold lots relisting,
    /// unconfirmed bookings releasing their slot. Called by the scheduler, never by a request.
    /// </summary>
    Task<Result<int>> RunDueTransitionsAsync(int batchSize, CancellationToken cancellationToken);
}

public sealed record WorkflowActor(Guid? UserId, IReadOnlySet<string> Permissions)
{
    /// <summary>The scheduler firing an automatic transition. Holds every permission by definition.</summary>
    public static WorkflowActor System { get; } = new(null, new HashSet<string>(StringComparer.Ordinal));

    public bool IsSystem => UserId is null;

    public bool Holds(string? permission)
        => permission is null || IsSystem || Permissions.Contains(permission);
}

public sealed record WorkflowFireRequest
{
    public required string WorkflowKey { get; init; }

    public required Guid EntityId { get; init; }

    public required string ActionCode { get; init; }

    public required WorkflowActor Actor { get; init; }

    /// <summary>
    /// The facts the guard is evaluated against — the entity's own fields, the actor, the deal
    /// amount, the city. Retained verbatim on the history row so a decision stays explainable after
    /// the rules that made it have changed.
    /// </summary>
    public JsonNode? Context { get; init; }

    public string? Reason { get; init; }

    /// <summary>
    /// Deduplicates retries. A dealer double-tapping "Accept offer" on a flaky connection must not
    /// produce two transitions, two commission postings, and two notifications.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}

public sealed record WorkflowSnapshot
{
    public required string WorkflowKey { get; init; }

    public required int DefinitionVersion { get; init; }

    public required WorkflowStateView CurrentState { get; init; }

    public required DateTimeOffset EnteredAt { get; init; }

    public DateTimeOffset? DueAt { get; init; }

    public bool IsComplete { get; init; }

    public required IReadOnlyList<AvailableAction> AvailableActions { get; init; }

    public required IReadOnlyList<WorkflowHistoryView> History { get; init; }
}

public sealed record WorkflowStateView(string Code, string Name, StateCategory Category, bool IsTerminal);

public sealed record AvailableAction(string ActionCode, string Label, bool RequiresReason);

public sealed record WorkflowHistoryView(
    string? FromStateCode,
    string ToStateCode,
    string? ActionCode,
    DateTimeOffset OccurredAt,
    Guid? ActorId,
    string? Reason,
    TimeSpan? TimeInPreviousState);

/// <summary>
/// Reads and writes workflow definitions and instances.
/// </summary>
/// <remarks>
/// Defined here and implemented in the persistence layer so <c>AutoHub.Platform</c> never
/// references EF Core. The engine is therefore unit-testable against an in-memory store, which
/// matters because the auction timer logic is exactly the kind of thing that needs a hundred cheap
/// tests rather than three expensive ones.
/// </remarks>
public interface IWorkflowStore
{
    Task<WorkflowDefinition?> GetPublishedDefinitionAsync(string key, CancellationToken cancellationToken);

    Task<WorkflowDefinition?> GetDefinitionVersionAsync(string key, int version, CancellationToken cancellationToken);

    Task<WorkflowInstance?> GetInstanceAsync(string workflowKey, Guid entityId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowInstance>> GetDueInstancesAsync(
        DateTimeOffset asOf, int batchSize, CancellationToken cancellationToken);

    Task AddInstanceAsync(WorkflowInstance instance, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
