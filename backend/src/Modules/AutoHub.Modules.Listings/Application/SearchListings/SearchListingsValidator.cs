using FluentValidation;

namespace AutoHub.Modules.Listings.Application.SearchListings;

/// <summary>
/// Bounds the page and the filter set. "No unbounded queries reach production", enforced.
/// </summary>
/// <remarks>
/// Both ceilings are technical rather than commercial. A hundred results is what one response can
/// serialise without the serialiser becoming the slowest part of the request; thirty-two attribute
/// filters is far more than any real filter rail offers and stops a crafted query from asking the
/// index for thirty-two intersections.
/// </remarks>
public sealed class SearchListingsValidator : AbstractValidator<SearchListingsQuery>
{
    private const int MaxLimit = 100;

    private const int MaxAttributeFilters = 32;

    public SearchListingsValidator()
    {
        RuleFor(query => query.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithErrorCode("listing.search.limit_out_of_range");

        RuleFor(query => query.Attributes)
            .Must(attributes => attributes.Count <= MaxAttributeFilters)
            .WithErrorCode("listing.search.too_many_filters");

        RuleFor(query => query.PriceMaxMinorUnits)
            .GreaterThanOrEqualTo(query => query.PriceMinMinorUnits!.Value)
            .When(query => query.PriceMinMinorUnits.HasValue && query.PriceMaxMinorUnits.HasValue)
            .WithErrorCode("listing.search.price_range_inverted");
    }
}
