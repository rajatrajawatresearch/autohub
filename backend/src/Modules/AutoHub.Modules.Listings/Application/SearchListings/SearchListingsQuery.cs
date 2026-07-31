using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Listings.Application.SearchListings;

/// <summary>
/// The buyer-facing search — §9 of the contract, in one query.
/// </summary>
/// <remarks>
/// The attribute grammar is a dictionary rather than named properties because the facets are
/// generated from the taxonomy registry: <c>attr.boom_length=0..30</c> has to work the afternoon
/// somebody adds Construction Equipment, and a typed query object would make that a release.
/// </remarks>
public sealed record SearchListingsQuery : IRequest<Result<ListingSearchResponse>>
{
    public string? Text { get; init; }

    public string? CityCode { get; init; }

    /// <summary>Wire name — <c>sale</c>, <c>auction</c>, … Null searches every intent.</summary>
    public string? Intent { get; init; }

    /// <summary>
    /// Category code. Also decides which facet labels can be resolved.
    /// </summary>
    /// <remarks>
    /// Facet <i>counts</i> come from the index and need no category; facet <i>labels</i> and
    /// rendering styles come from the taxonomy registry, which resolves attributes per category.
    /// A search with no category still returns working facets, keyed by code — which is enough for
    /// the filter rail to function and is why the parameter is optional rather than required.
    /// </remarks>
    public string? CategoryCode { get; init; }

    public IReadOnlyDictionary<string, AttributeFilter> Attributes { get; init; } =
        new Dictionary<string, AttributeFilter>(StringComparer.Ordinal);

    public long? PriceMinMinorUnits { get; init; }

    public long? PriceMaxMinorUnits { get; init; }

    public ListingSort Sort { get; init; } = ListingSort.Relevance;

    public string? Cursor { get; init; }

    public int Limit { get; init; } = 24;
}

/// <summary>The plate, in both the form that is indexed and the form that is read.</summary>
public sealed record RegistrationView(string Canonical, string Display);

/// <summary>What the card shows about a listing's media, without the card knowing the CDN.</summary>
public sealed record ResultMedia(string? CoverObjectKey, double? AspectRatio, int Count);

/// <summary>One search result, in the shape §9 of the contract publishes.</summary>
public sealed record ListingResultItem
{
    public required Guid ListingId { get; init; }

    public required string Intent { get; init; }

    public RegistrationView? Registration { get; init; }

    public required string Title { get; init; }

    public required long PriceMinorUnits { get; init; }

    public required string Currency { get; init; }

    public required string CityCode { get; init; }

    /// <summary>Order and content come from the taxonomy registry, not from this code.</summary>
    public required IReadOnlyList<SpecStripItem> SpecStrip { get; init; }

    public required ResultMedia Media { get; init; }

    public required int QualityScore { get; init; }

    /// <summary>
    /// Trust markers — <c>inspected</c>, <c>verified_rc</c>.
    /// </summary>
    /// <remarks>
    /// Present in the contract from day one and empty until the inspection and trust modules ship
    /// their projections. Publishing the field now and filling it later means no client change when
    /// it starts arriving; inventing values here would mean a badge that says "inspected" about a car
    /// nobody has inspected, which is the one lie a marketplace cannot recover from.
    /// </remarks>
    public IReadOnlyList<string> Badges { get; init; } = [];
}

/// <summary>A facet with its label and rendering style resolved from the taxonomy registry.</summary>
/// <remarks>
/// <see cref="Style"/> is the registry's <c>facetStyle</c> — <c>chips</c>, <c>list</c>,
/// <c>range</c>, <c>toggle</c> — and the design system maps it to a component. The server never
/// sends a component name and never sends a colour.
/// </remarks>
public sealed record SearchFacetView(
    string Code,
    string Label,
    string Style,
    IReadOnlyList<SearchFacetOptionView> Options,
    decimal? Min,
    decimal? Max);

public sealed record SearchFacetOptionView(string Value, string Label, long Count);

public sealed record ListingSearchResponse(
    IReadOnlyList<ListingResultItem> Items,
    IReadOnlyList<SearchFacetView> Facets,
    string? NextCursor,
    long TotalEstimate);
