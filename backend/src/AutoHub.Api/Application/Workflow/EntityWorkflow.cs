using System.Text.Json.Nodes;
using System.Xml;
using AutoHub.Api.Application.Behaviours;
using AutoHub.Platform.Access;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Security;
using FluentValidation;
using MediatR;

namespace AutoHub.Api.Application.Workflow;

/// <summary>
/// The live-state document of §3 of the contract.
/// </summary>
/// <remarks>
/// <c>timeInPreviousState</c> is an ISO-8601 duration string (<c>PT17H10M</c>), not a
/// <c>TimeSpan</c>. System.Text.Json serialises a TimeSpan as <c>17:10:00</c>, which no ISO-8601
/// duration parser accepts and which the published contract does not describe — so the conversion
/// is explicit and happens exactly here.
/// </remarks>
public sealed record WorkflowSnapshotResponse(
    string WorkflowKey,
    WorkflowStateSnapshot CurrentState,
    DateTimeOffset EnteredAt,
    DateTimeOffset? DueAt,
    IReadOnlyList<AvailableActionResponse> AvailableActions,
    IReadOnlyList<WorkflowHistoryResponse> History);

public sealed record WorkflowStateSnapshot(string Code, string Name, string Category, bool IsTerminal);

public sealed record AvailableActionResponse(string ActionCode, string Label, bool RequiresReason);

public sealed record WorkflowHistoryResponse(
    string? From,
    string To,
    string? ActionCode,
    DateTimeOffset OccurredAt,
    string? Actor,
    string? Reason,
    string? TimeInPreviousState);

/// <summary><c>GET /{module}/{resource}/{id}/workflow</c>.</summary>
internal sealed record GetEntityWorkflowQuery(string Module, string Resource, Guid Id)
    : IRequest<Result<WorkflowSnapshotResponse>>;

/// <summary>
/// <c>POST /{module}/{resource}/{id}/workflow/actions</c>.
/// </summary>
/// <remarks>
/// Transactional, because a transition runs effects — posting a commission, opening an escrow — and
/// a transition that half-happened is the state this codebase spends most of its design budget
/// avoiding.
/// </remarks>
internal sealed record FireWorkflowActionCommand(
    string Module,
    string Resource,
    Guid Id,
    string ActionCode,
    string? Reason,
    JsonObject? Payload)
    : IRequest<Result<WorkflowSnapshotResponse>>, ITransactionalRequest;

internal sealed class FireWorkflowActionValidator : AbstractValidator<FireWorkflowActionCommand>
{
    public FireWorkflowActionValidator()
        => RuleFor(command => command.ActionCode)
            .NotEmpty().WithErrorCode("workflow.action_required")
            .MaximumLength(64).WithErrorCode("workflow.action_invalid");
}

/// <summary>
/// Serves both workflow endpoints, because they answer with the same document.
/// </summary>
/// <remarks>
/// One handler for the read and the write is not a shortcut — it is what guarantees the POST
/// response is byte-identical in shape to the GET, which is what lets the client replace its state
/// rather than reconcile it. Two handlers would eventually diverge in one field and the client would
/// silently lose it after every action.
/// </remarks>
internal sealed class EntityWorkflowHandler
    : IRequestHandler<GetEntityWorkflowQuery, Result<WorkflowSnapshotResponse>>,
      IRequestHandler<FireWorkflowActionCommand, Result<WorkflowSnapshotResponse>>
{
    private readonly WorkflowRouteResolver _routes;
    private readonly IWorkflowEngine _engine;
    private readonly IPermissionResolver _permissions;
    private readonly ICurrentUser _currentUser;

    public EntityWorkflowHandler(
        WorkflowRouteResolver routes,
        IWorkflowEngine engine,
        IPermissionResolver permissions,
        ICurrentUser currentUser)
    {
        _routes = routes;
        _engine = engine;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkflowSnapshotResponse>> Handle(
        GetEntityWorkflowQuery query,
        CancellationToken cancellationToken)
    {
        var key = await _routes.ResolveAsync(query.Module, query.Resource, cancellationToken);
        if (key.IsFailure)
        {
            return Result.Failure<WorkflowSnapshotResponse>(key.Error);
        }

        var actor = await ResolveActorAsync(cancellationToken);

        var snapshot = await _engine.GetSnapshotAsync(key.Value, query.Id, null, actor, cancellationToken);

        return snapshot.Map(Project);
    }

    public async Task<Result<WorkflowSnapshotResponse>> Handle(
        FireWorkflowActionCommand command,
        CancellationToken cancellationToken)
    {
        var key = await _routes.ResolveAsync(command.Module, command.Resource, cancellationToken);
        if (key.IsFailure)
        {
            return Result.Failure<WorkflowSnapshotResponse>(key.Error);
        }

        var actor = await ResolveActorAsync(cancellationToken);

        var snapshot = await _engine.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = key.Value,
                EntityId = command.Id,
                ActionCode = command.ActionCode,
                Actor = actor,
                Reason = command.Reason,
                Context = command.Payload,
            },
            cancellationToken);

        return snapshot.Map(Project);
    }

    /// <summary>
    /// Builds the engine's actor from the resolved permission set.
    /// </summary>
    /// <remarks>
    /// An anonymous caller gets an actor with no user id and no permissions — <b>not</b>
    /// <see cref="WorkflowActor.System"/>, which holds every permission by definition. Passing the
    /// system actor here would let an unauthenticated request fire every transition on the platform,
    /// which is the single most expensive mistake available in this file.
    /// </remarks>
    private async Task<WorkflowActor> ResolveActorAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return new WorkflowActor(Guid.Empty, new HashSet<string>(StringComparer.Ordinal));
        }

        var permissions = await _permissions.ResolveAsync(userId, cancellationToken);
        return new WorkflowActor(userId, permissions.PermissionCodes);
    }

    private static WorkflowSnapshotResponse Project(WorkflowSnapshot snapshot)
        => new(
            snapshot.WorkflowKey,
            new WorkflowStateSnapshot(
                snapshot.CurrentState.Code,
                snapshot.CurrentState.Name,
                snapshot.CurrentState.Category.ToWireName(),
                snapshot.CurrentState.IsTerminal),
            snapshot.EnteredAt,
            snapshot.DueAt,
            snapshot.AvailableActions
                .Select(action => new AvailableActionResponse(
                    action.ActionCode, action.Label, action.RequiresReason))
                .ToList(),
            snapshot.History
                .Select(entry => new WorkflowHistoryResponse(
                    entry.FromStateCode,
                    entry.ToStateCode,
                    entry.ActionCode,
                    entry.OccurredAt,

                    // The actor is an id today and a resolved display name once the identity module
                    // exposes a batch name lookup. Sending the id rather than nothing keeps the
                    // history renderable and keeps the field's type stable across that change.
                    entry.ActorId?.ToString(),
                    entry.Reason,
                    entry.TimeInPreviousState is { } elapsed ? XmlConvert.ToString(elapsed) : null))
                .ToList());
}
