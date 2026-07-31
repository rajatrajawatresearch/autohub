using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Workflow;

/// <summary>Everything an effect is allowed to know about the transition that triggered it.</summary>
public sealed record WorkflowEffectContext(
    string WorkflowKey,
    Guid EntityId,
    string FromStateCode,
    string ToStateCode,
    WorkflowTransition Transition,
    JsonNode EvaluationContext,
    WorkflowActor Actor,
    string? Reason,
    DateTimeOffset OccurredAt);

/// <summary>
/// A capability a workflow transition may invoke.
/// </summary>
/// <remarks>
/// Handlers are the vocabulary; the workflow registry writes the sentences. A developer adds
/// <c>ledger.post_commission</c> once, with tests; an operator then wires it to whichever
/// transitions should post commission, and rewires it later without a deploy.
/// <para>
/// Handlers must be idempotent. The engine can retry a transition after a transient failure, and
/// the scheduler can pick up the same due instance twice if a node dies mid-sweep.
/// </para>
/// </remarks>
public interface IWorkflowEffectHandler
{
    /// <summary>Stable key referenced by <see cref="WorkflowEffect.EffectCode"/>.</summary>
    string EffectCode { get; }

    Task<Result> ExecuteAsync(
        WorkflowEffectContext context,
        JsonNode? arguments,
        CancellationToken cancellationToken);
}

public interface IWorkflowEffectDispatcher
{
    Task<Result> DispatchAsync(WorkflowEffectContext context, CancellationToken cancellationToken);

    /// <summary>Every registered code. Used to validate a definition before it can be published.</summary>
    IReadOnlySet<string> KnownEffectCodes { get; }
}

public sealed class WorkflowEffectDispatcher : IWorkflowEffectDispatcher
{
    private readonly Dictionary<string, IWorkflowEffectHandler> _handlers;
    private readonly ILogger<WorkflowEffectDispatcher> _logger;

    public WorkflowEffectDispatcher(
        IEnumerable<IWorkflowEffectHandler> handlers,
        ILogger<WorkflowEffectDispatcher> logger)
    {
        _handlers = handlers.ToDictionary(h => h.EffectCode, StringComparer.Ordinal);
        _logger = logger;
        KnownEffectCodes = _handlers.Keys.ToHashSet(StringComparer.Ordinal);
    }

    public IReadOnlySet<string> KnownEffectCodes { get; }

    public async Task<Result> DispatchAsync(WorkflowEffectContext context, CancellationToken cancellationToken)
    {
        foreach (var effect in context.Transition.Effects.OrderBy(e => e.SortOrder))
        {
            if (!_handlers.TryGetValue(effect.EffectCode, out var handler))
            {
                // Publish-time validation should have caught this. Reaching it means a handler was
                // removed from the code while a published workflow still referenced it — which is a
                // deployment mistake, and the transition must not proceed as if nothing was missing.
                _logger.LogError(
                    "Workflow {WorkflowKey} action {Action} references unregistered effect {EffectCode}",
                    context.WorkflowKey, context.Transition.ActionCode, effect.EffectCode);

                return Error.Unexpected(
                    "workflow.effect_unregistered",
                    "This action is misconfigured and has been blocked.")
                    .With("effectCode", effect.EffectCode);
            }

            Result outcome;
            try
            {
                outcome = await handler.ExecuteAsync(context, effect.Arguments(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow effect {EffectCode} threw", effect.EffectCode);
                outcome = Error.Unexpected("workflow.effect_failed", $"Effect '{effect.EffectCode}' failed.");
            }

            if (outcome.IsFailure)
            {
                if (!effect.ContinueOnFailure)
                {
                    return outcome;
                }

                // Declared non-critical by the workflow author. A dead APNs connection must never
                // block a sale; a failed commission posting must always block one.
                _logger.LogWarning(
                    "Non-critical workflow effect {EffectCode} failed ({Code}); continuing",
                    effect.EffectCode, outcome.Error.Code);
            }
        }

        return Result.Success();
    }
}
