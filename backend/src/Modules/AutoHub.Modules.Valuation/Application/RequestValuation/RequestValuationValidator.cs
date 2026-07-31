using FluentValidation;

namespace AutoHub.Modules.Valuation.Application.RequestValuation;

/// <summary>
/// Refuses a request that names nothing to price. Every coefficient rule lives in the registry.
/// </summary>
/// <remarks>
/// The bounds here are sanity limits rather than business ones, and each catches a specific real
/// mistake. An odometer of nine million is a client that sent metres; a year of 3021 is a date picker
/// with a two-digit field; a twentieth owner is a typo. Every one of them would otherwise flow
/// straight into an age or usage curve and out the other side as a plausible-looking price.
/// </remarks>
public sealed class RequestValuationValidator : AbstractValidator<RequestValuationCommand>
{
    private const long MaxKilometres = 3_000_000;

    private const int MaxOwners = 20;

    public RequestValuationValidator()
    {
        RuleFor(command => command)
            .Must(command =>
                !string.IsNullOrWhiteSpace(command.RegistrationNumber)
                || !string.IsNullOrWhiteSpace(command.CategoryCode))
            .WithName(nameof(RequestValuationCommand.RegistrationNumber))
            .WithErrorCode("valuation.identity_required");

        RuleFor(command => command.KilometresDriven)
            .InclusiveBetween(0, MaxKilometres)
            .When(command => command.KilometresDriven.HasValue)
            .WithErrorCode("valuation.kilometres_out_of_range");

        RuleFor(command => command.ManufactureYear)
            .InclusiveBetween(1900, 2100)
            .When(command => command.ManufactureYear.HasValue)
            .WithErrorCode("valuation.manufacture_year_invalid");

        RuleFor(command => command.ManufactureMonth)
            .InclusiveBetween(1, 12)
            .When(command => command.ManufactureMonth.HasValue)
            .WithErrorCode("valuation.manufacture_month_invalid");

        RuleFor(command => command.OwnerSerial)
            .InclusiveBetween(1, MaxOwners)
            .When(command => command.OwnerSerial.HasValue)
            .WithErrorCode("valuation.owner_serial_invalid");

        RuleFor(command => command.ReferencePriceMinorUnits)
            .GreaterThan(0)
            .When(command => command.ReferencePriceMinorUnits.HasValue)
            .WithErrorCode("valuation.reference_price_invalid");
    }
}
