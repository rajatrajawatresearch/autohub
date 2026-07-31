using AutoHub.Modules.Deals.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Modules.Deals.Application;

/// <summary>
/// Cancels a deal that has not settled.
/// </summary>
/// <remarks>
/// A settled deal is never cancelled — it is reversed with a refund, which is a new journal and not
/// an erasure of the old one. That distinction is enforced in the aggregate rather than here,
/// because it is an invariant and not a policy of this code path.
/// <para>
/// The reason is mandatory and reaches the workflow history verbatim. Cancellations are the single
/// most disputed event in a marketplace — "the dealer cancelled after I paid the token" is a support
/// case with money attached — and a cancellation with no recorded reason is one the platform cannot
/// adjudicate months later.
/// </para>
/// </remarks>
public sealed record CancelDealCommand : IRequest<Result<DealView>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid DealId { get; init; }

    public required string Reason { get; init; }
}

public sealed class CancelDealHandler : IRequestHandler<CancelDealCommand, Result<DealView>>
{
    private readonly IDealRepository _deals;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;

    public CancelDealHandler(IDealRepository deals, IWorkflowEngine workflow, IClock clock)
    {
        _deals = deals;
        _workflow = workflow;
        _clock = clock;
    }

    public async Task<Result<DealView>> Handle(CancelDealCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(DealPermissions.Cancel))
        {
            return DealErrors.PermissionDenied(DealPermissions.Cancel);
        }

        var deal = await _deals.GetAsync(request.DealId, cancellationToken);
        if (deal is null)
        {
            return DealErrors.NotFound(request.DealId);
        }

        var cancelled = deal.MarkCancelled(request.Reason, _clock.UtcNow);
        if (cancelled.IsFailure)
        {
            return Result.Failure<DealView>(cancelled.Error);
        }

        var snapshot = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = DealWorkflow.Key,
                EntityId = deal.Id,
                ActionCode = DealWorkflow.Cancel,
                Actor = request.Actor,
                Reason = request.Reason,
                Context = deal.ToPricingContext(),
            },
            cancellationToken);

        if (snapshot.IsFailure)
        {
            return Result.Failure<DealView>(snapshot.Error);
        }

        await _deals.SaveChangesAsync(cancellationToken);

        return DealView.From(deal, snapshot.Value);
    }
}
