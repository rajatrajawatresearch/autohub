using FluentValidation;

namespace AutoHub.Modules.Listings.Application.MakeOffer;

/// <summary>
/// Structural checks. Whether the amount is acceptable is a workflow guard, not a rule here.
/// </summary>
/// <remarks>
/// "An offer must be within 30% of the asking price" is exactly the kind of rule a category manager
/// wants to tune per city and per intent, so it belongs in the <c>listing_offer</c> workflow's guard
/// where it can be changed without a release. What this refuses is only what has no meaning at all.
/// </remarks>
public sealed class MakeOfferValidator : AbstractValidator<MakeOfferCommand>
{
    public MakeOfferValidator()
    {
        RuleFor(command => command.ListingId)
            .NotEmpty()
            .WithErrorCode("listing.id_required");

        RuleFor(command => command.BuyerPartyId)
            .NotEmpty()
            .WithErrorCode("listing.offer.buyer_required");

        RuleFor(command => command.AmountMinorUnits)
            .GreaterThan(0)
            .WithErrorCode("listing.offer.amount_invalid");
    }
}
