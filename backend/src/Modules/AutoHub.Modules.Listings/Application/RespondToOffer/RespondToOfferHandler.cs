using System.Text.Json.Nodes;
using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Application.MakeOffer;
using AutoHub.Modules.Listings.Application.Validation;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Modules.Listings.Infrastructure;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;

namespace AutoHub.Modules.Listings.Application.RespondToOffer;

public sealed class RespondToOfferHandler : IRequestHandler<RespondToOfferCommand, Result<OfferDetail>>
{
    private readonly IListingOfferRepository _offers;
    private readonly IWorkflowEngine _workflow;
    private readonly OfferDescriber _describer;
    private readonly WorkflowActorFactory _actors;
    private readonly IValidator<RespondToOfferCommand> _validator;
    private readonly IClock _clock;

    public RespondToOfferHandler(
        IListingOfferRepository offers,
        IWorkflowEngine workflow,
        OfferDescriber describer,
        WorkflowActorFactory actors,
        IValidator<RespondToOfferCommand> validator,
        IClock clock)
    {
        _offers = offers;
        _workflow = workflow;
        _describer = describer;
        _actors = actors;
        _validator = validator;
        _clock = clock;
    }

    public async Task<Result<OfferDetail>> Handle(
        RespondToOfferCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return RequestValidation.ToError(validation);
        }

        var offer = await _offers.GetAsync(command.OfferId, cancellationToken);
        if (offer is null)
        {
            return Error.NotFound("listing.offer.not_found", "No offer with that id.")
                .With("offerId", command.OfferId);
        }

        // Party membership is checked here rather than by a workflow guard because it is not a
        // business rule anybody would tune — a stranger is never a participant in someone else's
        // negotiation, in any city, under any campaign. Permissions and spreads are the guard's job;
        // "you are not in this conversation" is the aggregate's.
        if (offer.SideOf(command.PartyId) is not { } side)
        {
            return Error.Forbidden(
                "listing.offer.not_a_participant",
                "You are not part of this negotiation.")
                .With("offerId", offer.Id);
        }

        var now = _clock.UtcNow;

        if (command.CounterAmountMinorUnits is { } counterAmount)
        {
            var countered = offer.Counter(
                side,
                command.PartyId,
                Money.FromMinorUnits(counterAmount, Currency.FromCode(offer.CurrencyCode)),
                command.Message,
                now);

            if (countered.IsFailure)
            {
                return Result.Failure<OfferDetail>(countered.Error);
            }
        }

        var actor = await _actors.ForCurrentUserAsync(cancellationToken);

        var fired = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = ListingWorkflows.Offer,
                EntityId = offer.Id,
                ActionCode = command.ActionCode,
                Actor = actor,
                Context = new JsonObject
                {
                    ["respondingSide"] = side.ToString(),
                    ["respondingPartyId"] = command.PartyId.ToString(),
                    ["hasCounter"] = command.CounterAmountMinorUnits.HasValue,
                    ["currentAmountMinorUnits"] = offer.CurrentAmount?.MinorUnits ?? 0,
                    ["turnCount"] = offer.Turns.Count,
                },
                Reason = command.Reason,
                IdempotencyKey = command.IdempotencyKey,
            },
            cancellationToken);

        if (fired.IsFailure)
        {
            // The counter turn is discarded along with the transition, because nothing has been
            // saved: a rejected transition must not leave a counter on the record that the other
            // party can see and the workflow does not know about.
            return Result.Failure<OfferDetail>(fired.Error);
        }

        await _offers.SaveChangesAsync(cancellationToken);

        return await _describer.DescribeAsync(offer, actor, cancellationToken);
    }
}
