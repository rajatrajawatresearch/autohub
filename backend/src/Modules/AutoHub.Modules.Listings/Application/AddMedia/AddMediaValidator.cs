using FluentValidation;

namespace AutoHub.Modules.Listings.Application.AddMedia;

/// <summary>
/// Structural checks. How many items are allowed is the form registry's answer, resolved at runtime.
/// </summary>
public sealed class AddMediaValidator : AbstractValidator<AddMediaCommand>
{
    public AddMediaValidator()
    {
        RuleFor(command => command.ListingId)
            .NotEmpty()
            .WithErrorCode("listing.id_required");

        RuleFor(command => command.ObjectKey)
            .NotEmpty()
            .WithErrorCode("listing.media.object_key_required");

        RuleFor(command => command.MediaTypeCode)
            .NotEmpty()
            .WithErrorCode("listing.media.type_required");

        // Zero and negative dimensions are a client that failed to read the image and sent its
        // defaults. Letting them through produces an aspect ratio of zero and a gallery that
        // reserves no space, which is the layout shift the field exists to prevent.
        RuleFor(command => command.Width)
            .GreaterThan(0)
            .When(command => command.Width.HasValue)
            .WithErrorCode("listing.media.width_invalid");

        RuleFor(command => command.Height)
            .GreaterThan(0)
            .When(command => command.Height.HasValue)
            .WithErrorCode("listing.media.height_invalid");
    }
}
