using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Application.Validation;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Modules.Listings.Domain.Offers;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Security;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;
using AutoHub.Modules.Listings.Infrastructure;

namespace AutoHub.Modules.Listings.Application.MakeOffer;

public sealed class MakeOfferHandler : IRequestHandler<MakeOfferCommand, Result<OfferDetail>>
{
    private readonly IListingRepository _listings;
    private readonly IListingOfferRepository _offers;
    private readonly IWorkflowEngine _workflow;
    private readonly OfferDescriber _describer;
    private readonly WorkflowActorFactory _actors;
    private readonly IValidator<MakeOfferCommand> _validator;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public MakeOfferHandler(
        IListingRepository listings,
        IListingOfferRepository offers,
        IWorkflowEngine workflow,
        OfferDescriber describer,
        WorkflowActorFactory actors,
        IValidator<MakeOfferCommand> validator,
        ICurrentUser currentUser,
        IClock clock)
    {
        _listings = listings;
        _offers = offers;
        _workflow = workflow;
        _describer = describer;
        _actors = actors;
        _validator = validator;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<OfferDetail>> Handle(
        MakeOfferCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return RequestValidation.ToError(validation);
        }

        var listing = await _listings.GetAsync(command.ListingId, cancellationToken);
        if (listing is null)
        {
            return Error.NotFound("listing.not_found", "No listing with that id.")
                .With("listingId", command.ListingId);
        }

        // Returning the existing negotiation rather than refusing: a buyer who taps "Make offer"
        // again is trying to continue, not to start over, and two open chains on one listing means
        // two prices a seller can accept and a second deal on a car that has already been sold.
        var open = await _offers.FindOpenAsync(listing.Id, command.BuyerPartyId, cancellationToken);
        if (open is not null)
        {
            var existingActor = await _actors.ForCurrentUserAsync(cancellationToken);
            return await _describer.DescribeAsync(open, existingActor, cancellationToken);
        }

        Money amount;
        try
        {
            amount = Money.FromMinorUnits(command.AmountMinorUnits, Currency.FromCode(command.CurrencyCode));
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Validation(
                "listing.offer.currency_unsupported",
                $"'{command.CurrencyCode}' is not a currency this platform accepts.")
                .With("currency", command.CurrencyCode);
        }

        var opened = ListingOffer.Open(
            listing.Id,
            command.BuyerPartyId,
            listing.SellerPartyId,
            amount,
            command.Message,
            _clock.UtcNow);

        if (opened.IsFailure)
        {
            return Result.Failure<OfferDetail>(opened.Error);
        }

        var offer = opened.Value;
        await _offers.AddAsync(offer, cancellationToken);

        // Started before the save so the offer and its lifecycle commit together. An offer visible
        // for the moment between the two would have no state and no expiry, and the seller's app
        // would render a negotiation nobody can answer.
        var started = await _workflow.StartAsync(
            ListingWorkflows.Offer,
            offer.Id,
            null,
            _currentUser.UserId,
            cancellationToken);

        if (started.IsFailure)
        {
            return Result.Failure<OfferDetail>(started.Error);
        }

        await _offers.SaveChangesAsync(cancellationToken);

        var actor = await _actors.ForCurrentUserAsync(cancellationToken);
        return await _describer.DescribeAsync(offer, actor, cancellationToken);
    }
}
