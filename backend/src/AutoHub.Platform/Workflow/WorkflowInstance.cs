using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Platform.Workflow;

/// <summary>
/// Where one entity currently sits in its workflow, and how it got there.
/// </summary>
/// <remarks>
/// The instance holds the state, not the entity. A listing row has no <c>status</c> column — asking
/// "what state is this listing in?" is a question for the workflow engine.
/// <para>
/// That indirection buys the thing the whole architecture is for: adding a state to the listing
/// lifecycle touches no table, no migration, and no query. It also means the audit trail is not
/// optional — there is nowhere to change a state except through a transition that records itself.
/// </para>
/// </remarks>
public sealed class WorkflowInstance : AggregateRoot
{
    private readonly List<WorkflowHistoryEntry> _history = [];

    private WorkflowInstance(
        Guid id,
        string workflowKey,
        int definitionVersion,
        string entityType,
        Guid entityId,
        string currentStateCode,
        DateTimeOffset enteredAt)
        : base(id)
    {
        WorkflowKey = workflowKey;
        DefinitionVersion = definitionVersion;
        EntityType = entityType;
        EntityId = entityId;
        CurrentStateCode = currentStateCode;
        StateEnteredAt = enteredAt;
        StartedAt = enteredAt;
    }

    private WorkflowInstance() { }

    public string WorkflowKey { get; private set; } = string.Empty;

    /// <summary>
    /// Pinned at start. In-flight entities keep running against the definition they began on, so
    /// publishing v4 of the listing workflow cannot strand a listing that started on v3.
    /// </summary>
    public int DefinitionVersion { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string CurrentStateCode { get; private set; } = string.Empty;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset StateEnteredAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsComplete => CompletedAt.HasValue;

    /// <summary>
    /// When an automatic transition on the current state is due. Denormalised from the definition so
    /// the timer sweeper can find due instances with one indexed range scan instead of re-evaluating
    /// every workflow definition against every open instance.
    /// </summary>
    public DateTimeOffset? DueAt { get; private set; }

    public IReadOnlyList<WorkflowHistoryEntry> History => _history;

    public static WorkflowInstance Start(
        WorkflowDefinition definition,
        WorkflowState initialState,
        Guid entityId,
        DateTimeOffset now,
        Guid? actorId)
    {
        var instance = new WorkflowInstance(
            UuidV7.New(),
            definition.Key,
            definition.Version,
            definition.EntityType,
            entityId,
            initialState.Code,
            now);

        instance._history.Add(WorkflowHistoryEntry.Started(instance.Id, initialState.Code, now, actorId));
        return instance;
    }

    internal void ApplyTransition(
        WorkflowTransition transition,
        WorkflowState destination,
        DateTimeOffset now,
        Guid? actorId,
        string? reason,
        string? contextJson)
    {
        _history.Add(WorkflowHistoryEntry.Transitioned(
            Id,
            CurrentStateCode,
            destination.Code,
            transition.ActionCode,
            now,
            actorId,
            reason,
            contextJson,
            now - StateEnteredAt));

        CurrentStateCode = destination.Code;
        StateEnteredAt = now;
        DueAt = null;

        if (destination.IsTerminal)
        {
            CompletedAt = now;
        }
    }

    internal void ScheduleAutomaticTransition(DateTimeOffset dueAt) => DueAt = dueAt;
}

/// <summary>One immutable line in an entity's lifecycle audit trail.</summary>
public sealed class WorkflowHistoryEntry
{
    private WorkflowHistoryEntry() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid WorkflowInstanceId { get; private set; }

    public string? FromStateCode { get; private set; }

    public string ToStateCode { get; private set; } = string.Empty;

    /// <summary>Null on the first entry, where nothing was fired — the instance simply began.</summary>
    public string? ActionCode { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Null when a timer fired the transition rather than a person.</summary>
    public Guid? ActorId { get; private set; }

    public string? Reason { get; private set; }

    /// <summary>
    /// The context the guard was evaluated against, retained verbatim. Without it, "why was this
    /// transition allowed six months ago?" is unanswerable once the rules have moved on.
    /// </summary>
    public string? ContextJson { get; private set; }

    /// <summary>Time spent in the previous state. Makes SLA reporting a query, not a batch job.</summary>
    public TimeSpan? TimeInPreviousState { get; private set; }

    internal static WorkflowHistoryEntry Started(
        Guid instanceId, string initialState, DateTimeOffset now, Guid? actorId)
        => new()
        {
            WorkflowInstanceId = instanceId,
            FromStateCode = null,
            ToStateCode = initialState,
            ActionCode = null,
            OccurredAt = now,
            ActorId = actorId,
        };

    internal static WorkflowHistoryEntry Transitioned(
        Guid instanceId,
        string from,
        string to,
        string action,
        DateTimeOffset now,
        Guid? actorId,
        string? reason,
        string? contextJson,
        TimeSpan timeInPreviousState)
        => new()
        {
            WorkflowInstanceId = instanceId,
            FromStateCode = from,
            ToStateCode = to,
            ActionCode = action,
            OccurredAt = now,
            ActorId = actorId,
            Reason = reason,
            ContextJson = contextJson,
            TimeInPreviousState = timeInPreviousState,
        };
}
