using System.Text.Json.Nodes;
using AutoHub.Modules.Deals.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Modules.Deals.Application;

/// <summary>
/// Closes a deal and hands the money movement to the ledger.
/// </summary>
/// <remarks>
/// <b>This handler moves no money and computes no split.</b> It fires the workflow's settle
/// transition, whose effects — configured in the registry, implemented in the payments module — post
/// the double-entry journal. The deal then records that it settled and under which journal key.
/// <para>
/// The ordering is the important part. The journal is posted by an effect, and effects run
/// <i>before</i> the engine commits the state, so a posting that will not balance aborts the
/// transition and leaves the deal exactly where it was. The alternative — mark settled, then post —
/// produces the one state this system must never reach: a deal that says it is settled with no
/// entries behind it, which is a number on a dashboard that no money supports.
/// </para>
/// <para>
/// Settlement is idempotent all the way down. The journal's idempotency key is
/// <c>Deal.SettlementKey(dealId)</c>, computed from the deal id alone, so a retried effect, a
/// redelivered outbox message and an operator re-running a stuck batch all land on the journal that
/// already exists.
/// </para>
/// </remarks>
public sealed record SettleDealCommand : IRequest<Result<DealView>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid DealId { get; init; }

    /// <summary>Optional note for the audit trail — "settled manually after gateway reconciliation".</summary>
    public string? Note { get; init; }
}

public sealed class SettleDealHandler : IRequestHandler<SettleDealCommand, Result<DealView>>
{
    private readonly IDealRepository _deals;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;

    public SettleDealHandler(IDealRepository deals, IWorkflowEngine workflow, IClock clock)
    {
        _deals = deals;
        _workflow = workflow;
        _clock = clock;
    }

    public async Task<Result<DealView>> Handle(SettleDealCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(DealPermissions.Settle))
        {
            return DealErrors.PermissionDenied(DealPermissions.Settle);
        }

        var deal = await _deals.GetAsync(request.DealId, cancellationToken);
        if (deal is null)
        {
            return DealErrors.NotFound(request.DealId);
        }

        if (deal.SettledAt is not null)
        {
            var already = await _workflow.GetSnapshotAsync(
                DealWorkflow.Key, deal.Id, deal.ToPricingContext(), request.Actor, cancellationToken);

            return DealView.From(deal, already.IsSuccess ? already.Value : null);
        }

        // The context the settlement effect reads: which journal key to post under, what the lines
        // are, and who is owed what. Assembled here rather than looked up by the effect so the
        // posting is a pure function of facts that were true at the moment the transition fired —
        // and so the payments module never has to reference this one to read a deal's rows.
        var context = deal.ToPricingContext();
        context["settlement"] = BuildSettlementContext(deal, request.Note);

        var snapshot = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = DealWorkflow.Key,
                EntityId = deal.Id,
                ActionCode = DealWorkflow.Settle,
                Actor = request.Actor,
                Reason = request.Note,
                Context = context,
            },
            cancellationToken);

        if (snapshot.IsFailure)
        {
            return Result.Failure<DealView>(snapshot.Error);
        }

        var settled = deal.MarkSettled(_clock.UtcNow);
        if (settled.IsFailure)
        {
            return Result.Failure<DealView>(settled.Error);
        }

        await _deals.SaveChangesAsync(cancellationToken);

        return DealView.From(deal, snapshot.Value);
    }

    /// <summary>
    /// The settlement block the ledger's posting effect reads.
    /// </summary>
    /// <remarks>
    /// Every priced line travels, including the principal, because the posting templates map line
    /// codes to accounts and a line that does not travel is a line that silently never reaches the
    /// accounts. Parties travel with their roles so an owner-scoped account — one dealer's wallet
    /// rather than the control account — can be resolved without the payments module knowing what a
    /// deal party is.
    /// </remarks>
    private static JsonObject BuildSettlementContext(Deal deal, string? note)
    {
        var lines = new JsonArray();
        foreach (var line in deal.Lines.OrderBy(l => l.SortOrder))
        {
            lines.Add(new JsonObject
            {
                ["code"] = line.Code,
                ["amountMinorUnits"] = line.Amount.MinorUnits,
            });
        }

        var parties = new JsonArray();
        foreach (var party in deal.Parties)
        {
            parties.Add(new JsonObject
            {
                ["role"] = party.RoleCode,
                ["partyId"] = party.PartyId?.ToString(),
                ["partyType"] = party.PartyType,
                ["isPayee"] = party.IsPayee,
                ["isPayer"] = party.IsPayer,
            });
        }

        return new JsonObject
        {
            ["journalKey"] = Deal.SettlementKey(deal.Id),
            ["payableMinorUnits"] = deal.Payable.MinorUnits,
            ["currency"] = deal.Payable.Currency.Code,
            ["note"] = note,
            ["lines"] = lines,
            ["parties"] = parties,
        };
    }
}
