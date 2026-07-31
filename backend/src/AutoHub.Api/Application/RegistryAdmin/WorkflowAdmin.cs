using System.Text.Json.Nodes;
using AutoHub.Api.Application.Behaviours;
using AutoHub.Api.Application.Platform;
using AutoHub.Persistence;
using AutoHub.Platform.Access;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Api.Application.RegistryAdmin;

/// <summary>A whole workflow version, authored as one document.</summary>
/// <remarks>
/// One document rather than a state-at-a-time API, because a workflow is only meaningful whole:
/// <c>Validate</c> checks reachability, dead ends and dangling effect codes, and none of those
/// questions can be answered about a half-built definition. Partial authoring would mean either
/// deferring every check to publish — where the operator has lost the context of what they were
/// editing — or letting a definition sit in an unvalidatable state.
/// </remarks>
public sealed record WorkflowVersionBody(
    string EntityType,
    string Name,
    string? Description,
    IReadOnlyList<WorkflowStateBody> States,
    IReadOnlyList<WorkflowTransitionBody> Transitions);

public sealed record WorkflowStateBody(
    string Code,
    string Name,
    string Category,
    bool IsInitial,
    bool IsTerminal,
    int? SlaMinutes);

public sealed record WorkflowTransitionBody(
    string ActionCode,
    string Label,
    string From,
    string To,
    string? RequiredPermission,
    JsonNode? Guard,
    bool RequiresReason,
    int? AutomaticAfterMinutes,
    IReadOnlyList<WorkflowEffectBody>? Effects);

public sealed record WorkflowEffectBody(string EffectCode, JsonNode? Arguments, bool ContinueOnFailure);

internal sealed record CreateWorkflowVersionCommand(string Key, WorkflowVersionBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record PublishWorkflowVersionCommand(string Key, int Version)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

/// <summary>
/// Republishes an earlier version's content as a new version.
/// </summary>
/// <remarks>
/// <b>Rollback is forward, never backward.</b> A published definition is immutable and in-flight
/// entities are pinned to the version they started on, so "unpublish v4" would strand every entity
/// currently running on it in states that no longer resolve. Copying v2's content into v5 and
/// publishing that leaves every running instance untouched, starts new ones on the restored rules,
/// and keeps the audit trail a straight line rather than a story about a version that was published
/// and then was not.
/// </remarks>
internal sealed record RollbackWorkflowCommand(string Key, int ToVersion)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed class CreateWorkflowVersionValidator : AbstractValidator<CreateWorkflowVersionCommand>
{
    public CreateWorkflowVersionValidator()
    {
        RuleFor(command => command.Key).NotEmpty().WithErrorCode("workflow.key_required");
        RuleFor(command => command.Body.EntityType).NotEmpty().WithErrorCode("workflow.entity_type_required");
        RuleFor(command => command.Body.States).NotEmpty().WithErrorCode("workflow.states_required");
    }
}

internal sealed class WorkflowAdminHandler
    : IRequestHandler<CreateWorkflowVersionCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<PublishWorkflowVersionCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<RollbackWorkflowCommand, Result<RegistryWriteResponse>>
{
    private const string Entity = "platform.workflow_definition";

    private readonly AutoHubDbContext _context;
    private readonly IWorkflowEffectDispatcher _effects;
    private readonly RegistryPublisher _publisher;
    private readonly IClock _clock;

    public WorkflowAdminHandler(
        AutoHubDbContext context,
        IWorkflowEffectDispatcher effects,
        RegistryPublisher publisher,
        IClock clock)
    {
        _context = context;
        _effects = effects;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        CreateWorkflowVersionCommand command,
        CancellationToken cancellationToken)
    {
        var nextVersion = await NextVersionAsync(command.Key, cancellationToken);

        var built = Build(command.Key, nextVersion, command.Body);
        if (built.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(built.Error);
        }

        _context.WorkflowDefinitions.Add(built.Value);

        // A draft is invisible to the engine, so nothing is invalidated — but it is audited, because
        // "who wrote this rule" and "who published it" are different questions.
        _publisher.Record(
            PlatformPermissions.WorkflowPublish,
            Entity,
            command.Key,
            built.Value.Id,
            $"Drafted workflow '{command.Key}' v{nextVersion}.",
            after: Describe(built.Value));

        return new RegistryWriteResponse(RegistryNames.Workflow, nextVersion, command.Key, built.Value.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        PublishWorkflowVersionCommand command,
        CancellationToken cancellationToken)
    {
        var definition = await LoadAsync(command.Key, command.Version, cancellationToken);

        if (definition is null)
        {
            return Error.NotFound(
                "workflow.version_not_found",
                $"Workflow '{command.Key}' has no version {command.Version}.");
        }

        var published = definition.Publish(_effects.KnownEffectCodes, _clock.UtcNow);
        if (published.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(published.Error);
        }

        var version = await _publisher.PublishAsync(
            RegistryNames.Workflow,
            PlatformPermissions.WorkflowPublish,
            Entity,
            command.Key,
            definition.Id,
            $"Published workflow '{command.Key}' v{definition.Version}.",
            before: null,
            after: Describe(definition),
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Workflow, version, command.Key, definition.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        RollbackWorkflowCommand command,
        CancellationToken cancellationToken)
    {
        var source = await LoadAsync(command.Key, command.ToVersion, cancellationToken);

        if (source is null)
        {
            return Error.NotFound(
                "workflow.version_not_found",
                $"Workflow '{command.Key}' has no version {command.ToVersion}.");
        }

        var nextVersion = await NextVersionAsync(command.Key, cancellationToken);
        var clone = WorkflowDefinition.Draft(source.Key, nextVersion, source.EntityType, source.Name);

        foreach (var state in source.States)
        {
            var added = clone.AddState(new WorkflowState(
                state.Code, state.Name, state.Category, state.IsInitial, state.IsTerminal, state.SlaMinutes));

            if (added.IsFailure)
            {
                return Result.Failure<RegistryWriteResponse>(added.Error);
            }
        }

        foreach (var transition in source.Transitions)
        {
            var copy = new WorkflowTransition(
                transition.ActionCode,
                transition.Label,
                transition.FromStateCode,
                transition.ToStateCode,
                transition.RequiredPermission,
                transition.GuardJson,
                transition.RequiresReason,
                transition.AutomaticAfterMinutes);

            foreach (var effect in transition.Effects)
            {
                copy.AddEffect(new WorkflowEffect(
                    effect.EffectCode, effect.Arguments(), effect.ContinueOnFailure));
            }

            var added = clone.AddTransition(copy);
            if (added.IsFailure)
            {
                return Result.Failure<RegistryWriteResponse>(added.Error);
            }
        }

        var published = clone.Publish(_effects.KnownEffectCodes, _clock.UtcNow);
        if (published.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(published.Error);
        }

        _context.WorkflowDefinitions.Add(clone);

        var version = await _publisher.PublishAsync(
            RegistryNames.Workflow,
            PlatformPermissions.WorkflowPublish,
            Entity,
            command.Key,
            clone.Id,
            $"Rolled workflow '{command.Key}' back to v{command.ToVersion}, republished as v{nextVersion}.",
            before: Describe(source),
            after: Describe(clone),
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Workflow, version, command.Key, clone.Id);
    }

    private static Result<WorkflowDefinition> Build(string key, int version, WorkflowVersionBody body)
    {
        var definition = WorkflowDefinition.Draft(key, version, body.EntityType, body.Name);

        foreach (var state in body.States)
        {
            var category = WorkflowWireNames.ParseCategory(state.Category);
            if (category.IsFailure)
            {
                return Result.Failure<WorkflowDefinition>(category.Error);
            }

            var added = definition.AddState(new WorkflowState(
                state.Code, state.Name, category.Value, state.IsInitial, state.IsTerminal, state.SlaMinutes));

            if (added.IsFailure)
            {
                return Result.Failure<WorkflowDefinition>(added.Error);
            }
        }

        foreach (var transition in body.Transitions)
        {
            var built = new WorkflowTransition(
                transition.ActionCode,
                transition.Label,
                transition.From,
                transition.To,
                transition.RequiredPermission,
                transition.Guard?.ToJsonString(),
                transition.RequiresReason,
                transition.AutomaticAfterMinutes);

            foreach (var effect in transition.Effects ?? [])
            {
                built.AddEffect(new WorkflowEffect(
                    effect.EffectCode, effect.Arguments, effect.ContinueOnFailure));
            }

            var added = definition.AddTransition(built);
            if (added.IsFailure)
            {
                return Result.Failure<WorkflowDefinition>(added.Error);
            }
        }

        return definition;
    }

    private static JsonObject Describe(WorkflowDefinition definition)
        => new JsonObject
        {
            ["key"] = definition.Key,
            ["version"] = definition.Version,
            ["entityType"] = definition.EntityType,
            ["states"] = definition.States.Count,
            ["transitions"] = definition.Transitions.Count,
            ["isPublished"] = definition.IsPublished,
        };

    private async Task<int> NextVersionAsync(string key, CancellationToken cancellationToken)
    {
        var highest = await _context.WorkflowDefinitions
            .Where(definition => definition.Key == key)
            .MaxAsync(definition => (int?)definition.Version, cancellationToken);

        return (highest ?? 0) + 1;
    }

    private async Task<WorkflowDefinition?> LoadAsync(
        string key,
        int version,
        CancellationToken cancellationToken)
        => await _context.WorkflowDefinitions
            .Include(nameof(WorkflowDefinition.States))
            .Include($"{nameof(WorkflowDefinition.Transitions)}.{nameof(WorkflowTransition.Effects)}")
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                definition => definition.Key == key && definition.Version == version,
                cancellationToken);
}

/// <summary>Reads a definition version for the admin console, published or not.</summary>
internal sealed record GetWorkflowVersionQuery(string Key, int Version)
    : IRequest<Result<WorkflowDefinitionResponse>>;

internal sealed class GetWorkflowVersionHandler
    : IRequestHandler<GetWorkflowVersionQuery, Result<WorkflowDefinitionResponse>>
{
    private readonly IWorkflowStore _store;

    public GetWorkflowVersionHandler(IWorkflowStore store) => _store = store;

    public async Task<Result<WorkflowDefinitionResponse>> Handle(
        GetWorkflowVersionQuery query,
        CancellationToken cancellationToken)
    {
        var definition = await _store.GetDefinitionVersionAsync(query.Key, query.Version, cancellationToken);

        return definition is null
            ? Error.NotFound("workflow.version_not_found", $"No '{query.Key}' v{query.Version}.")
            : GetWorkflowDefinitionHandler.Project(definition);
    }
}
