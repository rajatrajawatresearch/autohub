using AutoHub.Modules.Deals.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Modules.Deals.Application;

/// <summary>
/// Adds a counterparty to an open deal.
/// </summary>
/// <remarks>
/// Separate from opening because parties arrive late as often as they arrive with the deal: a broker
/// is introduced after the buyer and seller have met, a lender joins once the loan is sanctioned, and
/// an insurer joins at delivery. Requiring the full cast up front would force every intent module to
/// know facts it does not have yet.
/// </remarks>
public sealed record AttachPartyCommand : IRequest<Result<DealView>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid DealId { get; init; }

    public required DealPartySpecification Party { get; init; }
}

public sealed class AttachPartyHandler : IRequestHandler<AttachPartyCommand, Result<DealView>>
{
    private readonly IDealRepository _deals;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;

    public AttachPartyHandler(IDealRepository deals, IWorkflowEngine workflow, IClock clock)
    {
        _deals = deals;
        _workflow = workflow;
        _clock = clock;
    }

    public async Task<Result<DealView>> Handle(AttachPartyCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(DealPermissions.PartyManage))
        {
            return DealErrors.PermissionDenied(DealPermissions.PartyManage);
        }

        var deal = await _deals.GetAsync(request.DealId, cancellationToken);
        if (deal is null)
        {
            return DealErrors.NotFound(request.DealId);
        }

        var attached = deal.AttachParty(request.Party, _clock.UtcNow);
        if (attached.IsFailure)
        {
            return Result.Failure<DealView>(attached.Error);
        }

        await _deals.SaveChangesAsync(cancellationToken);

        var snapshot = await _workflow.GetSnapshotAsync(
            DealWorkflow.Key, deal.Id, deal.ToPricingContext(), request.Actor, cancellationToken);

        return DealView.From(deal, snapshot.IsSuccess ? snapshot.Value : null);
    }
}
