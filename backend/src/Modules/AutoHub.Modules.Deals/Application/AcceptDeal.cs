using AutoHub.Modules.Deals.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Modules.Deals.Application;

/// <summary>
/// Records one party's acceptance, and moves the deal forward once everyone required has accepted.
/// </summary>
/// <remarks>
/// Acceptance is per party and the transition is fired once, which is the shape a two-sided deal
/// actually has: a C2C sale needs the buyer and the seller, an auction win needs nobody because the
/// bid was already binding, and a lease needs the lessee plus a credit approval that arrives as a
/// third party's acceptance. Whether a given party's acceptance is required is a flag on the party,
/// so the same handler serves all three without knowing which intent it is serving.
/// </remarks>
public sealed record AcceptDealCommand : IRequest<Result<DealView>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid DealId { get; init; }

    /// <summary>
    /// Which party is accepting. Defaults to the actor, which is the only honest default.
    /// </summary>
    /// <remarks>
    /// Settable so an operations user can record an acceptance taken over the phone, which is a real
    /// and frequent path in Indian used-car sales. That is why accepting on someone else's behalf
    /// needs <c>deal.party.manage</c> as well — the audit row must be able to say who actually
    /// clicked.
    /// </remarks>
    public Guid? OnBehalfOfPartyId { get; init; }
}

public sealed class AcceptDealHandler : IRequestHandler<AcceptDealCommand, Result<DealView>>
{
    private readonly IDealRepository _deals;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;

    public AcceptDealHandler(IDealRepository deals, IWorkflowEngine workflow, IClock clock)
    {
        _deals = deals;
        _workflow = workflow;
        _clock = clock;
    }

    public async Task<Result<DealView>> Handle(AcceptDealCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(DealPermissions.Accept))
        {
            return DealErrors.PermissionDenied(DealPermissions.Accept);
        }

        if (request.OnBehalfOfPartyId is not null
            && request.OnBehalfOfPartyId != request.Actor.UserId
            && !request.Actor.Holds(DealPermissions.PartyManage))
        {
            return DealErrors.PermissionDenied(DealPermissions.PartyManage);
        }

        var partyId = request.OnBehalfOfPartyId ?? request.Actor.UserId;
        if (partyId is null)
        {
            return Error.Unauthorized(
                "deal.accept.actor_required",
                "Accepting a deal requires a signed-in party.");
        }

        var deal = await _deals.GetAsync(request.DealId, cancellationToken);
        if (deal is null)
        {
            return DealErrors.NotFound(request.DealId);
        }

        var now = _clock.UtcNow;

        var everyoneAccepted = deal.RecordPartyAcceptance(partyId.Value, now);
        if (everyoneAccepted.IsFailure)
        {
            return Result.Failure<DealView>(everyoneAccepted.Error);
        }

        if (!everyoneAccepted.Value)
        {
            // Still waiting on somebody. The deal is saved so the acceptance is not lost, but no
            // transition fires — the workflow moves when the last signature lands, not the first.
            await _deals.SaveChangesAsync(cancellationToken);

            var pending = await _workflow.GetSnapshotAsync(
                DealWorkflow.Key, deal.Id, deal.ToPricingContext(), request.Actor, cancellationToken);

            return DealView.From(deal, pending.IsSuccess ? pending.Value : null);
        }

        var accepted = deal.MarkAccepted(now);
        if (accepted.IsFailure)
        {
            return Result.Failure<DealView>(accepted.Error);
        }

        // The engine runs the transition's effects before it commits the state, so a guard that
        // refuses — a legal hold, an unverified KYC, an expired quote — leaves the deal exactly
        // where it was and the acceptance timestamps unsaved along with it.
        var snapshot = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = DealWorkflow.Key,
                EntityId = deal.Id,
                ActionCode = DealWorkflow.Accept,
                Actor = request.Actor,
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
