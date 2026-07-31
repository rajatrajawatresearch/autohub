using FluentValidation;

namespace AutoHub.Modules.Listings.Application.PublishListing;

/// <summary>
/// Checks that an action was named. Whether it is <i>allowed</i> is the workflow engine's answer.
/// </summary>
/// <remarks>
/// There is deliberately no list of valid action codes here. A validator that carried one would be
/// a copy of the workflow definition that goes stale the moment an operator publishes a new version,
/// and the failure would be a perfectly valid transition rejected with a message nobody can explain.
/// </remarks>
public sealed class PublishListingValidator : AbstractValidator<PublishListingCommand>
{
    public PublishListingValidator()
    {
        RuleFor(command => command.ListingId)
            .NotEmpty()
            .WithErrorCode("listing.id_required");

        RuleFor(command => command.ActionCode)
            .NotEmpty()
            .WithErrorCode("listing.action_required");
    }
}
