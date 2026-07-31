using FluentValidation;

namespace AutoHub.Modules.Listings.Application.CreateListing;

/// <summary>
/// Refuses only what cannot be processed. Every field rule lives in the form schema registry.
/// </summary>
/// <remarks>
/// The split is deliberate and is the point of the whole module: a validator here that knew a sell
/// form needs six photos would be a hardcoded form schema, and changing it would be a release. So
/// this checks that the request is structurally answerable — there is an intent, a seller, a city, a
/// form to validate against — and everything a business person could plausibly change is refused by
/// a registry instead, with the registry's own error code.
/// </remarks>
public sealed class CreateListingValidator : AbstractValidator<CreateListingCommand>
{
    public CreateListingValidator()
    {
        RuleFor(command => command.Intent)
            .NotEmpty()
            .WithErrorCode("listing.intent_required");

        RuleFor(command => command.CategoryCode)
            .NotEmpty()
            .WithErrorCode("listing.category_required");

        RuleFor(command => command.FormKey)
            .NotEmpty()
            .WithErrorCode("listing.form_key_required");

        RuleFor(command => command.SellerPartyId)
            .NotEmpty()
            .WithErrorCode("listing.seller_required");

        RuleFor(command => command.CityCode)
            .NotEmpty()
            .WithErrorCode("listing.city_required");

        RuleFor(command => command.VehicleIdentifiers)
            .NotEmpty()
            .WithErrorCode("listing.vehicle_identifier_required");

        // Zero is a real listing — "price on application" — and negative is a client bug that would
        // otherwise flow straight into a commission calculation.
        RuleFor(command => command.PriceMinorUnits)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("listing.price_negative");

        RuleFor(command => command.Payload)
            .NotNull()
            .WithErrorCode("listing.payload_required");
    }
}
