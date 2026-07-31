using FluentValidation;

namespace AutoHub.Modules.Catalog.Application.SearchVehicles;

/// <summary>
/// Bounds the page. The quality floor's "no unbounded queries reach production", enforced.
/// </summary>
/// <remarks>
/// The ceiling is a technical limit rather than a business one: a hundred rows is what one screen
/// and one JSON response can carry without the serialiser becoming the slowest part of the request.
/// A caller that wants everything pages for it, which is also what makes the cursor useful.
/// </remarks>
public sealed class SearchVehiclesValidator : AbstractValidator<SearchVehiclesQuery>
{
    private const int MaxLimit = 100;

    public SearchVehiclesValidator()
    {
        RuleFor(query => query.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithErrorCode("catalog.search.limit_out_of_range");

        RuleFor(query => query.ManufactureYearTo)
            .GreaterThanOrEqualTo(query => query.ManufactureYearFrom!.Value)
            .When(query => query.ManufactureYearFrom.HasValue && query.ManufactureYearTo.HasValue)
            .WithErrorCode("catalog.search.year_range_inverted");
    }
}
