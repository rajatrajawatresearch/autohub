using FluentValidation;

namespace AutoHub.Modules.Catalog.Application.UpdateVehicleAttributes;

/// <summary>
/// Refuses only what cannot be processed at all. Every value rule lives in the taxonomy registry.
/// </summary>
/// <remarks>
/// The bound on key count is a denial-of-service guard, not a business rule: the taxonomy ignores
/// keys a category does not define, so a payload with fifty thousand junk keys would otherwise be
/// fifty thousand dictionary probes per request, all of them discarded.
/// </remarks>
public sealed class UpdateVehicleAttributesValidator : AbstractValidator<UpdateVehicleAttributesCommand>
{
    private const int MaxSubmittedKeys = 512;

    public UpdateVehicleAttributesValidator()
    {
        RuleFor(command => command.VehicleId)
            .NotEmpty()
            .WithErrorCode("catalog.vehicle.id_required");

        RuleFor(command => command.Attributes)
            .NotNull()
            .WithErrorCode("catalog.vehicle.attributes_required")
            .Must(attributes => attributes.Count <= MaxSubmittedKeys)
            .WithErrorCode("catalog.vehicle.too_many_attributes");
    }
}
