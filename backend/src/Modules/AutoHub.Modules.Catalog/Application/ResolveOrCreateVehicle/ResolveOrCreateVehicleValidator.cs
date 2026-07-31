using FluentValidation;

namespace AutoHub.Modules.Catalog.Application.ResolveOrCreateVehicle;

/// <summary>
/// Shape checks only. Whether a code exists is the taxonomy registry's answer, not a rule here.
/// </summary>
/// <remarks>
/// The split matters: a validator that carried a list of legal categories would be a hardcoded
/// taxonomy, and the whole point of the registry is that adding Construction Equipment is data
/// entry. So this refuses a request that could not possibly be processed, and everything that
/// depends on registry content is refused by the registry itself, with the registry's error code.
/// </remarks>
public sealed class ResolveOrCreateVehicleValidator : AbstractValidator<ResolveOrCreateVehicleCommand>
{
    /// <summary>
    /// A submission carries at most one of each identifier type, so more than a handful is either a
    /// client bug or somebody probing how much work one request can cause.
    /// </summary>
    private const int MaxIdentifiers = 8;

    public ResolveOrCreateVehicleValidator()
    {
        RuleFor(command => command.CategoryCode)
            .NotEmpty()
            .WithErrorCode("catalog.vehicle.category_required");

        RuleFor(command => command.Identifiers)
            .NotEmpty()
            .WithErrorCode("catalog.vehicle.identifier_required")
            .Must(identifiers => identifiers.Count <= MaxIdentifiers)
            .WithErrorCode("catalog.vehicle.too_many_identifiers");

        RuleForEach(command => command.Identifiers)
            .ChildRules(identifier => identifier
                .RuleFor(i => i.Value)
                .NotEmpty()
                .WithErrorCode("catalog.identifier.required"));

        // The lower bound is not decoration: a "1962" typed into a year box on a 2019 car changes an
        // age-based valuation by an order of magnitude, and nothing downstream would question it.
        RuleFor(command => command.ManufactureYear)
            .InclusiveBetween(1900, 2100)
            .When(command => command.ManufactureYear.HasValue)
            .WithErrorCode("catalog.vehicle.manufacture_year_invalid");

        RuleFor(command => command.ManufactureMonth)
            .InclusiveBetween(1, 12)
            .When(command => command.ManufactureMonth.HasValue)
            .WithErrorCode("catalog.vehicle.manufacture_month_invalid");
    }
}
