using FluentValidation;

namespace AutoHub.Modules.Listings.Application.RespondToOffer;

public sealed class RespondToOfferValidator : AbstractValidator<RespondToOfferCommand>
{
    public RespondToOfferValidator()
    {
        RuleFor(command => command.OfferId)
            .NotEmpty()
            .WithErrorCode("listing.offer.id_required");

        RuleFor(command => command.PartyId)
            .NotEmpty()
            .WithErrorCode("listing.offer.party_required");

        RuleFor(command => command.ActionCode)
            .NotEmpty()
            .WithErrorCode("listing.action_required");

        // Zero is not "no counter" — absence is. A zero counter is a client that sent an empty field
        // as a number, and accepting it would put a free car on the table.
        RuleFor(command => command.CounterAmountMinorUnits)
            .GreaterThan(0)
            .When(command => command.CounterAmountMinorUnits.HasValue)
            .WithErrorCode("listing.offer.amount_invalid");
    }
}
