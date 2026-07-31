using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Api.Application.Platform;

/// <summary>
/// The workflow document of §3 of the contract.
/// </summary>
/// <remarks>
/// A DTO rather than the aggregate, for two reasons the aggregate cannot serve. The wire names
/// differ — <c>from</c> and <c>to</c>, not <c>fromStateCode</c> — and <c>category</c> has to be the
/// registry's published spelling rather than an enum ordinal, because the design system maps it to a
/// <c>StatusPill</c> tone. The second reason matters more: guards and effects are deliberately
/// <b>not</b> in this response. A guard is a rule an operator authored, and shipping it to a client
/// tells an attacker exactly which conditions to satisfy.
/// </remarks>
public sealed record WorkflowDefinitionResponse(
    string Key,
    int Version,
    string EntityType,
    IReadOnlyList<WorkflowStateResponse> States,
    IReadOnlyList<WorkflowTransitionResponse> Transitions);

public sealed record WorkflowStateResponse(
    string Code,
    string Name,
    string Category,
    bool IsInitial,
    bool IsTerminal,
    int? SlaMinutes);

public sealed record WorkflowTransitionResponse(
    string ActionCode,
    string Label,
    string From,
    string To,
    string? RequiredPermission,
    bool RequiresReason);

internal sealed record GetWorkflowDefinitionQuery(string Key) : IRequest<Result<WorkflowDefinitionResponse>>;

internal sealed class GetWorkflowDefinitionHandler
    : IRequestHandler<GetWorkflowDefinitionQuery, Result<WorkflowDefinitionResponse>>
{
    private readonly IWorkflowStore _store;

    public GetWorkflowDefinitionHandler(IWorkflowStore store) => _store = store;

    public async Task<Result<WorkflowDefinitionResponse>> Handle(
        GetWorkflowDefinitionQuery query,
        CancellationToken cancellationToken)
    {
        var definition = await _store.GetPublishedDefinitionAsync(query.Key, cancellationToken);

        if (definition is null)
        {
            return Error.NotFound("workflow.not_published", $"No published workflow named '{query.Key}'.")
                .With("workflowKey", query.Key);
        }

        return Project(definition);
    }

    /// <summary>Shared with the admin read side so both describe a definition the same way.</summary>
    public static WorkflowDefinitionResponse Project(WorkflowDefinition definition)
        => new(
            definition.Key,
            definition.Version,
            definition.EntityType,
            definition.States
                .Select(state => new WorkflowStateResponse(
                    state.Code,
                    state.Name,
                    state.Category.ToWireName(),
                    state.IsInitial,
                    state.IsTerminal,
                    state.SlaMinutes))
                .ToList(),
            definition.Transitions
                .Select(transition => new WorkflowTransitionResponse(
                    transition.ActionCode,
                    transition.Label,
                    transition.FromStateCode,
                    transition.ToStateCode,
                    transition.RequiredPermission,
                    transition.RequiresReason))
                .ToList());
}
