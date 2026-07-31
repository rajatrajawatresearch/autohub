using System.Text.Json.Nodes;
using AutoHub.Modules.Deals.Domain;
using AutoHub.Platform.Pricing;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Modules.Deals.Application;

/// <summary>
/// Asks the pricing registry what this deal costs, and records the answer.
/// </summary>
/// <remarks>
/// <b>This handler contains no arithmetic, and that is the point.</b> There is no percentage here, no
/// slab, no GST rate, no "if the dealer is gold" — the deal assembles the facts, the engine applies
/// whichever rule an operator authored, and the deal writes down what came back along with the rule
/// that produced it. Changing an auction commission to 1.5% in Pune only is a row; it is not this
/// file.
/// <para>
/// <see cref="QuoteFeesCommand.AdditionalFacts"/> exists because some facts are not the deal's to
/// know. A dealer's loyalty tier lives in identity, a vehicle's category lives in catalog, and a
/// campaign code lives in marketing. The caller that has them passes them; a deal that fetched them
/// itself would be a deal that depends on three other modules.
/// </para>
/// </remarks>
public sealed record QuoteFeesCommand : IRequest<Result<DealView>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid DealId { get; init; }

    /// <summary>Facts the rules may match on that the deal does not carry — party tier, vehicle, campaign.</summary>
    public JsonObject? AdditionalFacts { get; init; }

    /// <summary>Lets an operator preview what a pending rule change will do. Null means now.</summary>
    public DateTimeOffset? AsOf { get; init; }

    /// <summary>
    /// When true the quote is returned without being written to the deal.
    /// </summary>
    /// <remarks>
    /// The sell flow and the auction lot page both show "you will receive ₹x" before anyone has
    /// committed to anything. Recording those previews would leave every abandoned browse as a
    /// quoted deal, and the difference between a quote and a decision is exactly what the workflow
    /// transition marks.
    /// </remarks>
    public bool PreviewOnly { get; init; }
}

public sealed class QuoteFeesHandler : IRequestHandler<QuoteFeesCommand, Result<DealView>>
{
    private readonly IDealRepository _deals;
    private readonly IPricingEngine _pricing;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;

    public QuoteFeesHandler(
        IDealRepository deals,
        IPricingEngine pricing,
        IWorkflowEngine workflow,
        IClock clock)
    {
        _deals = deals;
        _pricing = pricing;
        _workflow = workflow;
        _clock = clock;
    }

    public async Task<Result<DealView>> Handle(QuoteFeesCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(DealPermissions.Quote))
        {
            return DealErrors.PermissionDenied(DealPermissions.Quote);
        }

        var deal = await _deals.GetAsync(request.DealId, cancellationToken);
        if (deal is null)
        {
            return DealErrors.NotFound(request.DealId);
        }

        var quote = await _pricing.QuoteAsync(
            new PriceQuoteRequest
            {
                Scope = deal.PricingScope,
                BaseAmount = deal.BaseAmount,
                Context = deal.ToPricingContext(request.AdditionalFacts),
                AsOf = request.AsOf,
            },
            cancellationToken);

        if (quote.IsFailure)
        {
            return Result.Failure<DealView>(quote.Error);
        }

        if (request.PreviewOnly)
        {
            return DealView.From(deal, null);
        }

        var applied = deal.ApplyQuote(quote.Value, _clock.UtcNow);
        if (applied.IsFailure)
        {
            return Result.Failure<DealView>(applied.Error);
        }

        await _deals.SaveChangesAsync(cancellationToken);

        // Fired after the quote is on the deal, so a guard on the transition can read the lines it
        // is guarding. A workflow whose definition has no 'quote' action from the current state
        // simply refuses, and the quote stands on its own — pricing a deal is not a lifecycle move
        // in every configuration, and this module does not get to assume it is.
        var snapshot = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = DealWorkflow.Key,
                EntityId = deal.Id,
                ActionCode = DealWorkflow.Quote,
                Actor = request.Actor,
                Context = deal.ToPricingContext(request.AdditionalFacts),
            },
            cancellationToken);

        return DealView.From(deal, snapshot.IsSuccess ? snapshot.Value : null);
    }
}
