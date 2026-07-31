using System.Text.Json.Nodes;
using AutoHub.Platform.Expressions;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Workflow;

/// <summary>
/// Executes workflow definitions. The one place in the system where an entity's state changes.
/// </summary>
public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowStore _store;
    private readonly IWorkflowEffectDispatcher _effects;
    private readonly IClock _clock;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        IWorkflowStore store,
        IWorkflowEffectDispatcher effects,
        IClock clock,
        ILogger<WorkflowEngine> logger)
    {
        _store = store;
        _effects = effects;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<WorkflowSnapshot>> StartAsync(
        string workflowKey,
        Guid entityId,
        JsonNode? context,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var existing = await _store.GetInstanceAsync(workflowKey, entityId, cancellationToken);
        if (existing is not null)
        {
            // Idempotent by design: a retried create must not start a second lifecycle for the
            // same entity, which would give it two histories and two due timers.
            return BuildSnapshot(
                existing,
                await RequireDefinitionAsync(workflowKey, existing.DefinitionVersion, cancellationToken),
                WorkflowActor.System,
                context);
        }

        var definition = await _store.GetPublishedDefinitionAsync(workflowKey, cancellationToken);
        if (definition is null)
        {
            return WorkflowNotPublished(workflowKey);
        }

        var initial = definition.InitialState;
        if (initial is null)
        {
            return Error.Unexpected(
                "workflow.no_initial_state",
                $"Workflow '{workflowKey}' v{definition.Version} has no initial state.");
        }

        var now = _clock.UtcNow;
        var instance = WorkflowInstance.Start(definition, initial, entityId, now, actorId);

        ScheduleNextAutomaticTransition(instance, definition, now);

        await _store.AddInstanceAsync(instance, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Workflow {WorkflowKey} v{Version} started for {EntityId} in state {State}",
            workflowKey, definition.Version, entityId, initial.Code);

        return BuildSnapshot(instance, definition, WorkflowActor.System, context);
    }

    public async Task<Result<WorkflowSnapshot>> GetSnapshotAsync(
        string workflowKey,
        Guid entityId,
        JsonNode? context,
        WorkflowActor actor,
        CancellationToken cancellationToken)
    {
        var instance = await _store.GetInstanceAsync(workflowKey, entityId, cancellationToken);
        if (instance is null)
        {
            return InstanceNotFound(workflowKey, entityId);
        }

        var definition = await RequireDefinitionAsync(workflowKey, instance.DefinitionVersion, cancellationToken);
        if (definition is null)
        {
            return DefinitionVersionMissing(workflowKey, instance.DefinitionVersion);
        }

        return BuildSnapshot(instance, definition, actor, context);
    }

    public async Task<Result<WorkflowSnapshot>> FireAsync(
        WorkflowFireRequest request,
        CancellationToken cancellationToken)
    {
        var instance = await _store.GetInstanceAsync(request.WorkflowKey, request.EntityId, cancellationToken);
        if (instance is null)
        {
            return InstanceNotFound(request.WorkflowKey, request.EntityId);
        }

        var definition = await RequireDefinitionAsync(
            request.WorkflowKey, instance.DefinitionVersion, cancellationToken);
        if (definition is null)
        {
            return DefinitionVersionMissing(request.WorkflowKey, instance.DefinitionVersion);
        }

        if (instance.IsComplete)
        {
            return Error.Conflict(
                "workflow.already_complete",
                "This item has finished its lifecycle and cannot be changed.")
                .With("currentState", instance.CurrentStateCode);
        }

        var transition = definition.FindTransition(instance.CurrentStateCode, request.ActionCode);
        if (transition is null)
        {
            // The state moved underneath the caller — usually a stale tab, occasionally two
            // moderators on the same listing. Return the current state so the UI can refresh
            // rather than guess.
            return Error.Conflict(
                "workflow.transition_not_allowed",
                $"'{request.ActionCode}' is not available from the current state.")
                .With("currentState", instance.CurrentStateCode)
                .With("actionCode", request.ActionCode);
        }

        if (!request.Actor.Holds(transition.RequiredPermission))
        {
            return Error.Forbidden(
                "workflow.permission_denied",
                "You do not have permission to do this.")
                .With("requiredPermission", transition.RequiredPermission);
        }

        if (transition.RequiresReason && string.IsNullOrWhiteSpace(request.Reason))
        {
            return Error.Validation(
                "workflow.reason_required",
                "A reason is required for this action.");
        }

        var context = BuildEvaluationContext(instance, request.Context, request.Actor);

        var guard = ConditionExpression.FromJson(transition.GuardJson);
        if (guard.IsFailure)
        {
            // A guard that will not parse is a configuration fault, not a user fault. Refusing the
            // transition is the safe direction — the alternative is applying a rule nobody authored.
            _logger.LogError(
                "Workflow {WorkflowKey} transition {Action} has an unparseable guard",
                request.WorkflowKey, request.ActionCode);

            return Error.Unexpected(
                "workflow.guard_invalid",
                "This action is misconfigured and has been blocked.");
        }

        if (!guard.Value.Evaluate(context))
        {
            return Error.Rule(
                "workflow.guard_failed",
                "The conditions for this action are not met.")
                .With("actionCode", request.ActionCode);
        }

        var destination = definition.FindState(transition.ToStateCode);
        if (destination is null)
        {
            return Error.Unexpected(
                "workflow.destination_missing",
                $"Transition '{transition.ActionCode}' points at unknown state '{transition.ToStateCode}'.");
        }

        var now = _clock.UtcNow;

        // Effects run before the state is committed so an aborting effect leaves the entity where
        // it was. A failed commission posting must not produce a "sold" listing with no money moved.
        var effectOutcome = await _effects.DispatchAsync(
            new WorkflowEffectContext(
                request.WorkflowKey,
                request.EntityId,
                instance.CurrentStateCode,
                destination.Code,
                transition,
                context,
                request.Actor,
                request.Reason,
                now),
            cancellationToken);

        if (effectOutcome.IsFailure)
        {
            _logger.LogError(
                "Workflow {WorkflowKey} action {Action} on {EntityId} aborted by effect: {Code}",
                request.WorkflowKey, request.ActionCode, request.EntityId, effectOutcome.Error.Code);

            return Result.Failure<WorkflowSnapshot>(effectOutcome.Error);
        }

        instance.ApplyTransition(
            transition,
            destination,
            now,
            request.Actor.UserId,
            request.Reason,
            context.ToJsonString());

        ScheduleNextAutomaticTransition(instance, definition, now);

        await _store.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Workflow {WorkflowKey} {EntityId}: {From} --{Action}--> {To}",
            request.WorkflowKey, request.EntityId, transition.FromStateCode,
            transition.ActionCode, destination.Code);

        return BuildSnapshot(instance, definition, request.Actor, request.Context);
    }

    public async Task<Result<int>> RunDueTransitionsAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var due = await _store.GetDueInstancesAsync(now, batchSize, cancellationToken);
        var fired = 0;

        foreach (var instance in due)
        {
            var definition = await RequireDefinitionAsync(
                instance.WorkflowKey, instance.DefinitionVersion, cancellationToken);

            var transition = definition?
                .TransitionsFrom(instance.CurrentStateCode)
                .FirstOrDefault(t => t.AutomaticAfterMinutes.HasValue);

            if (definition is null || transition is null)
            {
                // The definition version was withdrawn, or the automatic transition was removed in a
                // later version this instance is not on. Clear the timer so the sweeper stops
                // picking the row up on every pass.
                instance.ScheduleAutomaticTransition(default);
                continue;
            }

            var result = await FireAsync(
                new WorkflowFireRequest
                {
                    WorkflowKey = instance.WorkflowKey,
                    EntityId = instance.EntityId,
                    ActionCode = transition.ActionCode,
                    Actor = WorkflowActor.System,
                    Reason = "Automatic transition after timeout.",
                },
                cancellationToken);

            if (result.IsSuccess)
            {
                fired++;
            }
            else
            {
                // One stuck instance must not stop the sweep. A guard that keeps failing is a
                // configuration problem for the alerting to surface, not a reason to stall every
                // other expiring payment window on the platform.
                _logger.LogWarning(
                    "Automatic transition {Action} on {WorkflowKey}/{EntityId} failed: {Code}",
                    transition.ActionCode, instance.WorkflowKey, instance.EntityId, result.Error.Code);
            }
        }

        await _store.SaveChangesAsync(cancellationToken);
        return fired;
    }

    private async Task<WorkflowDefinition?> RequireDefinitionAsync(
        string key, int version, CancellationToken cancellationToken)
        => await _store.GetDefinitionVersionAsync(key, version, cancellationToken);

    private static void ScheduleNextAutomaticTransition(
        WorkflowInstance instance, WorkflowDefinition definition, DateTimeOffset now)
    {
        var automatic = definition
            .TransitionsFrom(instance.CurrentStateCode)
            .FirstOrDefault(t => t.AutomaticAfterMinutes.HasValue);

        if (automatic?.AutomaticAfterMinutes is { } minutes)
        {
            instance.ScheduleAutomaticTransition(now.AddMinutes(minutes));
        }
    }

    /// <summary>
    /// Assembles what a guard is allowed to see: the caller's facts plus the workflow's own.
    /// </summary>
    /// <remarks>
    /// Workflow facts are namespaced under <c>_workflow</c> so a caller cannot shadow them.
    /// Without that, a payload carrying <c>currentState</c> could talk a guard into approving a
    /// transition out of a state the entity is not in.
    /// </remarks>
    private JsonObject BuildEvaluationContext(WorkflowInstance instance, JsonNode? caller, WorkflowActor actor)
    {
        var context = caller?.DeepClone() as JsonObject ?? [];

        context["_workflow"] = new JsonObject
        {
            ["currentState"] = instance.CurrentStateCode,
            ["enteredAt"] = instance.StateEnteredAt.ToString("O"),
            ["minutesInState"] = (int)(_clock.UtcNow - instance.StateEnteredAt).TotalMinutes,
            ["isSystem"] = actor.IsSystem,
            ["actorId"] = actor.UserId?.ToString(),
        };

        return context;
    }

    private static WorkflowSnapshot BuildSnapshot(
        WorkflowInstance instance,
        WorkflowDefinition? definition,
        WorkflowActor actor,
        JsonNode? context)
    {
        var state = definition?.FindState(instance.CurrentStateCode);

        var actions = new List<AvailableAction>();
        if (definition is not null && !instance.IsComplete)
        {
            foreach (var transition in definition.TransitionsFrom(instance.CurrentStateCode))
            {
                if (transition.AutomaticAfterMinutes.HasValue && !actor.IsSystem)
                {
                    continue; // timer-driven; never offered as a button
                }

                if (!actor.Holds(transition.RequiredPermission))
                {
                    continue;
                }

                var guard = ConditionExpression.FromJson(transition.GuardJson);
                if (guard.IsFailure || !guard.Value.Evaluate(context?.DeepClone() ?? new JsonObject()))
                {
                    continue;
                }

                actions.Add(new AvailableAction(transition.ActionCode, transition.Label, transition.RequiresReason));
            }
        }

        return new WorkflowSnapshot
        {
            WorkflowKey = instance.WorkflowKey,
            DefinitionVersion = instance.DefinitionVersion,
            CurrentState = new WorkflowStateView(
                instance.CurrentStateCode,
                state?.Name ?? instance.CurrentStateCode,
                state?.Category ?? StateCategory.InProgress,
                state?.IsTerminal ?? false),
            EnteredAt = instance.StateEnteredAt,
            DueAt = instance.DueAt,
            IsComplete = instance.IsComplete,
            AvailableActions = actions,
            History = instance.History
                .OrderBy(h => h.OccurredAt)
                .Select(h => new WorkflowHistoryView(
                    h.FromStateCode, h.ToStateCode, h.ActionCode, h.OccurredAt,
                    h.ActorId, h.Reason, h.TimeInPreviousState))
                .ToList(),
        };
    }

    private static Error WorkflowNotPublished(string key)
        => Error.NotFound("workflow.not_published", $"No published workflow named '{key}'.");

    private static Error InstanceNotFound(string key, Guid entityId)
        => Error.NotFound("workflow.instance_not_found", $"No '{key}' workflow running for that item.")
            .With("entityId", entityId);

    private static Error DefinitionVersionMissing(string key, int version)
        => Error.Unexpected(
            "workflow.definition_version_missing",
            $"Workflow '{key}' v{version} is referenced by a running instance but no longer exists.");
}
