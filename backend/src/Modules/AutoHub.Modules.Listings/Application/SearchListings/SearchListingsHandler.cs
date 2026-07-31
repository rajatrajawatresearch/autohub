using System.Globalization;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.Modules.Listings.Application.Validation;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Vehicles;
using FluentValidation;
using MediatR;

namespace AutoHub.Modules.Listings.Application.SearchListings;

/// <summary>
/// Asks the index which listings match, then renders them.
/// </summary>
/// <remarks>
/// <b>The split between the two halves is the whole design.</b> The index answers only what it can
/// answer from its own store — ids, prices, cities, facet counts — and the handler joins the result
/// to the catalog and the taxonomy registry to produce titles, plates and spec strips. Pushing those
/// joins into the index would mean every taxonomy edit reindexes the corpus, and a make renamed on a
/// Tuesday leaves a million documents wrong until the reindex finishes.
/// <para>
/// Every join here is <b>batched</b>. Twenty-four results produce one call for the vehicles, one for
/// the attribute values and one for the taxonomy names — not seventy-two. The alternative reads
/// perfectly well and takes two seconds.
/// </para>
/// </remarks>
public sealed class SearchListingsHandler : IRequestHandler<SearchListingsQuery, Result<ListingSearchResponse>>
{
    private readonly ISearchIndex _index;
    private readonly IVehicleDirectory _vehicles;
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly ITaxonomyRegistry _registry;
    private readonly IValidator<SearchListingsQuery> _validator;

    public SearchListingsHandler(
        ISearchIndex index,
        IVehicleDirectory vehicles,
        IVehicleTaxonomy taxonomy,
        ITaxonomyRegistry registry,
        IValidator<SearchListingsQuery> validator)
    {
        _index = index;
        _vehicles = vehicles;
        _taxonomy = taxonomy;
        _registry = registry;
        _validator = validator;
    }

    public async Task<Result<ListingSearchResponse>> Handle(
        SearchListingsQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return RequestValidation.ToError(validation);
        }

        ListingIntent? intent = null;
        if (!string.IsNullOrWhiteSpace(query.Intent))
        {
            var parsed = ListingIntentWireNames.Parse(query.Intent);
            if (parsed.IsFailure)
            {
                return Result.Failure<ListingSearchResponse>(parsed.Error);
            }

            intent = parsed.Value;
        }

        var result = await _index.SearchAsync(
            new ListingSearchRequest
            {
                Text = query.Text,
                CityCode = query.CityCode,
                Intent = intent,
                CategoryCode = query.CategoryCode,
                Attributes = query.Attributes,
                Price = query.PriceMinMinorUnits is null && query.PriceMaxMinorUnits is null
                    ? null
                    : new MoneyRange(query.PriceMinMinorUnits, query.PriceMaxMinorUnits),
                Sort = query.Sort,
                Cursor = query.Cursor,
                Limit = query.Limit,
            },
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<ListingSearchResponse>(result.Error);
        }

        var items = await RenderAsync(result.Value.Items, cancellationToken);
        var facets = await LabelFacetsAsync(result.Value.Facets, query.CategoryCode, cancellationToken);

        return new ListingSearchResponse(items, facets, result.Value.NextCursor, result.Value.TotalEstimate);
    }

    private async Task<IReadOnlyList<ListingResultItem>> RenderAsync(
        IReadOnlyList<ListingHit> hits,
        CancellationToken cancellationToken)
    {
        if (hits.Count == 0)
        {
            return [];
        }

        var vehicles = await _vehicles.GetManyAsync([.. hits.Select(h => h.VehicleId)], cancellationToken);
        var byVehicle = vehicles.IsSuccess ? vehicles.Value : new Dictionary<Guid, VehicleSummary>();

        var values = await _registry.ResolveAttributeValuesAsync(
            ListingEntityTypes.Listing, [.. hits.Select(h => h.ListingId)], cancellationToken);

        var byListing = values.IsSuccess
            ? values.Value.ByEntity
            : new Dictionary<Guid, IReadOnlyList<ResolvedAttribute>>();

        var names = await DescribeVehiclesAsync(byVehicle.Values, cancellationToken);

        var items = new List<ListingResultItem>(hits.Count);

        foreach (var hit in hits)
        {
            byVehicle.TryGetValue(hit.VehicleId, out var vehicle);
            var attributes = byListing.GetValueOrDefault(hit.ListingId) ?? [];

            items.Add(new ListingResultItem
            {
                ListingId = hit.ListingId,
                Intent = hit.Intent.ToWireName(),
                Registration = ToRegistrationView(vehicle),
                Title = ComposeTitle(vehicle, names),
                PriceMinorUnits = hit.PriceMinorUnits,
                Currency = hit.Currency,
                CityCode = hit.CityCode,
                SpecStrip = BuildSpecStrip(attributes),
                Media = new ResultMedia(hit.CoverObjectKey, hit.CoverAspectRatio, hit.MediaCount),
                QualityScore = hit.QualityScore,
            });
        }

        return items;
    }

    /// <summary>
    /// Resolves every make, model and variant name the page needs in one call.
    /// </summary>
    /// <remarks>
    /// Deduplicated first: a results page full of Swifts asks for the same three ids twenty-four
    /// times, and the taxonomy cache would answer all of them, but building the list is free and
    /// the intent reads better.
    /// </remarks>
    private async Task<IReadOnlyDictionary<Guid, TaxonomyRef>> DescribeVehiclesAsync(
        IEnumerable<VehicleSummary> vehicles,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid>();

        foreach (var vehicle in vehicles)
        {
            AddIfPresent(ids, vehicle.MakeId);
            AddIfPresent(ids, vehicle.ModelId);
            AddIfPresent(ids, vehicle.VariantId);
        }

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, TaxonomyRef>();
        }

        var described = await _taxonomy.DescribeAsync(ids, cancellationToken);
        return described.IsSuccess ? described.Value : new Dictionary<Guid, TaxonomyRef>();
    }

    /// <summary>
    /// Attaches labels and a rendering style to the counts the index returned.
    /// </summary>
    /// <remarks>
    /// Both come from the taxonomy registry, which is what lets a facet be renamed or translated
    /// without touching a single indexed document. Without a category the labels cannot be resolved,
    /// so the code stands in for the label — a working filter rail with technical labels is a better
    /// answer than no facets, and every real search arrives with a category anyway.
    /// </remarks>
    private async Task<IReadOnlyList<SearchFacetView>> LabelFacetsAsync(
        IReadOnlyList<ListingFacet> facets,
        string? categoryCode,
        CancellationToken cancellationToken)
    {
        if (facets.Count == 0)
        {
            return [];
        }

        var definitions = new Dictionary<string, AttributeView>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            var attributes = await _registry.GetAttributesForCategoryAsync(categoryCode, cancellationToken);

            if (attributes.IsSuccess)
            {
                foreach (var attribute in attributes.Value.Attributes)
                {
                    definitions[attribute.Code] = attribute;
                }
            }
        }

        var views = new List<SearchFacetView>(facets.Count);

        foreach (var facet in facets)
        {
            definitions.TryGetValue(facet.Code, out var definition);

            var options = facet.Options
                .Select(option => new SearchFacetOptionView(
                    option.Value,
                    LabelOf(definition, option.Value),
                    option.Count))
                .ToList();

            views.Add(new SearchFacetView(
                facet.Code,
                definition?.Label ?? facet.Code,
                definition?.FacetStyle ?? FacetStyle.List.ToWireName(),
                options,
                facet.Min,
                facet.Max));
        }

        return views;
    }

    private static string LabelOf(AttributeView? definition, string value)
        => definition?.Options?.FirstOrDefault(o => string.Equals(o.Value, value, StringComparison.Ordinal))?.Label
           ?? value;

    private static RegistrationView? ToRegistrationView(VehicleSummary? vehicle)
    {
        if (vehicle?.Registration is not { } canonical)
        {
            return null;
        }

        // Falls back to the canonical form when the stored value will not re-parse, which can only
        // happen for a row written around the domain. A plate rendered ungrouped is worse than one
        // rendered grouped and better than no plate at all.
        var display = vehicle.RegistrationDisplay
            ?? (RegistrationNumber.TryParse(canonical) is { IsSuccess: true } parsed
                ? parsed.Value.ToPlateDisplay()
                : canonical);

        return new RegistrationView(canonical, display);
    }

    private static string ComposeTitle(VehicleSummary? vehicle, IReadOnlyDictionary<Guid, TaxonomyRef> names)
    {
        if (vehicle is null)
        {
            return string.Empty;
        }

        var parts = new List<string>(4);

        if (vehicle.ManufactureYear is { } year)
        {
            parts.Add(year.ToString(CultureInfo.InvariantCulture));
        }

        AddName(parts, names, vehicle.MakeId);
        AddName(parts, names, vehicle.ModelId);
        AddName(parts, names, vehicle.VariantId);

        return parts.Count > 0
            ? string.Join(' ', parts)
            : vehicle.RegistrationDisplay ?? vehicle.Registration ?? string.Empty;
    }

    private static List<SpecStripItem> BuildSpecStrip(IReadOnlyList<ResolvedAttribute> attributes)
    {
        var items = new List<SpecStripItem>(attributes.Count);

        foreach (var attribute in attributes)
        {
            var value = attribute.DisplayValue ?? Stringify(attribute.Value);

            if (!string.IsNullOrWhiteSpace(value))
            {
                items.Add(new SpecStripItem(attribute.Code, attribute.Label, value, attribute.Unit));
            }
        }

        return items;
    }

    private static string Stringify(TypedAttributeValue value) => value.DataType switch
    {
        AttributeDataType.String or AttributeDataType.Enum => value.Text ?? string.Empty,
        AttributeDataType.Number or AttributeDataType.Integer =>
            value.Number?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        AttributeDataType.Boolean => value.Flag?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        AttributeDataType.Date => value.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
        AttributeDataType.Money => value.Amount?.MinorUnits.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        AttributeDataType.MultiEnum => string.Join(", ", value.Choices),
        _ => string.Empty,
    };

    private static void AddIfPresent(HashSet<Guid> ids, Guid? id)
    {
        if (id is { } value)
        {
            ids.Add(value);
        }
    }

    private static void AddName(List<string> parts, IReadOnlyDictionary<Guid, TaxonomyRef> names, Guid? id)
    {
        if (id is { } value && names.TryGetValue(value, out var reference) && !string.IsNullOrWhiteSpace(reference.Name))
        {
            parts.Add(reference.Name);
        }
    }
}
