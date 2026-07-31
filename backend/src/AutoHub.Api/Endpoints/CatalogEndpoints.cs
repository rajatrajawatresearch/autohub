using System.Globalization;
using System.Text.Json.Serialization;
using AutoHub.Api.Http;
using AutoHub.Integrations.Storage;
using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.Modules.Listings.Application.SearchListings;
using MediatR;

namespace AutoHub.Api.Endpoints;

/// <summary>
/// §9 of the contract — <c>GET /catalog/search</c>, the buyer side of the marketplace.
/// </summary>
/// <remarks>
/// Anonymous, for the same reason the valuation hero is: a buyer browses before they sign in, and a
/// search behind a token is a search engine that never indexes us.
/// <para>
/// <b>The query is read from <see cref="HttpRequest.Query"/> rather than bound to parameters.</b>
/// That is not laziness — it is the contract. Facets are generated from the taxonomy registry, so
/// <c>attr.boom_length=0..30</c> has to work the afternoon somebody adds Construction Equipment, and
/// a bound parameter list would make every new attribute a release. The grammar is closed even
/// though the field set is open, which is what keeps parsing it safe.
/// </para>
/// </remarks>
internal static class CatalogEndpoints
{
    /// <summary>The <c>attr.</c> prefix of the open filter grammar.</summary>
    private const string AttributePrefix = "attr.";

    /// <summary>Separates the ends of a range token — <c>0..40000</c>, <c>..40000</c>, <c>300000..</c>.</summary>
    private const string RangeSeparator = "..";

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/catalog").WithTags("Catalog").AllowAnonymous();

        catalog.MapGet("/search", async (
                HttpContext context,
                ISender sender,
                IObjectStore objects,
                CancellationToken ct) =>
            {
                var result = await sender.Send(ReadQuery(context.Request), ct);

                if (result.IsFailure)
                {
                    return ApiProblem.Result(result.Error, context);
                }

                return Results.Ok(await SearchResponse.FromAsync(result.Value, objects, ct));
            })
            .WithName("SearchCatalog")
            .WithSummary("Faceted vehicle search. Facets and filters come from the taxonomy registry.");

        return app;
    }

    /// <summary>
    /// Translates the wire grammar into the module's query.
    /// </summary>
    /// <remarks>
    /// Every unparseable value is dropped rather than rejected. A filter the caller cannot express —
    /// a range with letters in it, a limit of "many" — is almost always a hand-edited URL or a stale
    /// bookmark, and answering it with a results page is a better outcome than a 422 the buyer cannot
    /// act on. The bounds that actually matter are enforced by <c>SearchListingsValidator</c> against
    /// the values that did parse, so an out-of-range limit still fails loudly.
    /// </remarks>
    private static SearchListingsQuery ReadQuery(HttpRequest request)
    {
        var query = request.Query;
        var attributes = new Dictionary<string, AttributeFilter>(StringComparer.Ordinal);
        string? categoryCode = null;

        foreach (var pair in query)
        {
            if (!pair.Key.StartsWith(AttributePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var code = pair.Key[AttributePrefix.Length..];
            var raw = pair.Value.ToString();

            if (code.Length == 0 || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            // The category is not an attribute — it is the thing that decides which attributes
            // exist. It arrives through the same grammar because the filter rail has no other way to
            // send it, and lifting it here is what stops it reaching the index as a filter on a
            // column that does not exist, which would return an empty page with no explanation.
            if (string.Equals(code, "category", StringComparison.Ordinal))
            {
                categoryCode = raw;
                continue;
            }

            var filter = ParseFilter(raw);

            if (!filter.IsEmpty)
            {
                attributes[code] = filter;
            }
        }

        var price = ParseRange(query["price"].ToString());

        return new SearchListingsQuery
        {
            Text = Trimmed(query["q"]),
            // §12: X-Active-City scopes search. An explicit ?city= wins, because a shared link
            // carries the city it was searched in and must not be silently rewritten to the
            // recipient's.
            CityCode = Trimmed(query["city"]) ?? Trimmed(request.Headers["X-Active-City"]),
            Intent = Trimmed(query["intent"]),
            CategoryCode = Trimmed(query["category"]) ?? categoryCode,
            Attributes = attributes,
            PriceMinMinorUnits = ToMinorUnits(price.Min),
            PriceMaxMinorUnits = ToMinorUnits(price.Max),
            Sort = ParseSort(query["sort"].ToString()),
            Cursor = Trimmed(query["cursor"]),
            Limit = int.TryParse(query["limit"].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var limit)
                ? limit
                : DefaultLimit,
        };
    }

    /// <summary>Matches the client's default page size so an omitted limit and the UI agree.</summary>
    private const int DefaultLimit = 24;

    /// <summary>
    /// One <c>attr.{code}</c> value: a range if it carries the separator, otherwise a value list.
    /// </summary>
    /// <remarks>
    /// The two shapes are distinguished by syntax rather than by asking the registry what data type
    /// the attribute has. That keeps parsing a pure function of the request — no registry read on the
    /// hot path, and no request that fails differently depending on cache state — and the index
    /// rejects a range against an unordered attribute anyway.
    /// </remarks>
    private static AttributeFilter ParseFilter(string raw)
    {
        if (raw.Contains(RangeSeparator, StringComparison.Ordinal))
        {
            var range = ParseRange(raw);
            return AttributeFilter.Between(range.Min, range.Max);
        }

        var values = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        return AttributeFilter.OneOf(values);
    }

    /// <summary>Parses <c>min..max</c>, either end optional. An open end stays open.</summary>
    private static (decimal? Min, decimal? Max) ParseRange(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, null);
        }

        var separator = raw.IndexOf(RangeSeparator, StringComparison.Ordinal);

        if (separator < 0)
        {
            return (null, null);
        }

        // AsSpan before slicing: the range indexer on a string allocates a substring for each end,
        // and this runs on every search request that carries a range filter.
        return (
            ParseDecimal(raw.AsSpan()[..separator]),
            ParseDecimal(raw.AsSpan()[(separator + RangeSeparator.Length)..]));
    }

    private static decimal? ParseDecimal(ReadOnlySpan<char> value)
        => decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    /// <summary>
    /// Price travels in minor units, like every other amount on the wire.
    /// </summary>
    /// <remarks>
    /// The range grammar is shared with the attribute filters, which are decimal because an attribute
    /// can be a decimal. Money is not: truncating here rather than rounding means a
    /// <c>price=..600000</c> ceiling never quietly includes a listing a paisa above it.
    /// </remarks>
    private static long? ToMinorUnits(decimal? amount)
        => amount is { } value ? (long)decimal.Truncate(value) : null;

    /// <summary>
    /// Maps the contract's sort tokens onto the module's ordering.
    /// </summary>
    /// <remarks>
    /// A closed set on both sides, and deliberately so — each token is a different index and a
    /// different query plan, not a business constant. An unknown token falls back to relevance rather
    /// than failing: a stale bookmark should show results, not an error.
    /// </remarks>
    private static ListingSort ParseSort(string? sort) => sort switch
    {
        "price_asc" => ListingSort.PriceAscending,
        "price_desc" => ListingSort.PriceDescending,
        "newest" => ListingSort.Newest,
        "kms_asc" => ListingSort.KilometresAscending,
        _ => ListingSort.Relevance,
    };

    private static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>The §9 response body.</summary>
public sealed record SearchResponse
{
    public required IReadOnlyList<SearchItemResponse> Items { get; init; }

    public required IReadOnlyList<SearchFacetResponse> Facets { get; init; }

    public string? NextCursor { get; init; }

    /// <summary>An estimate by contract — never a <c>COUNT(*)</c> on a hot table.</summary>
    public required long TotalEstimate { get; init; }

    /// <summary>
    /// Renders the module's result, resolving cover object keys into URLs.
    /// </summary>
    /// <remarks>
    /// The keys are resolved <b>deduplicated and in one pass</b> rather than per card. A results page
    /// is twenty-four cards and a page of one model is often the same handful of keys; signing each
    /// one once is the difference between a search that costs one round trip per distinct image and
    /// one that costs twenty-four.
    /// <para>
    /// URL minting lives here and not in the module for the reason argued on
    /// <c>ListingMediaView.ObjectKey</c>: the CDN host and the signing policy are edge concerns that
    /// change without the media changing. When listing covers move behind a public CDN prefix — they
    /// are public objects, unlike inspection photos — this is the only method that changes.
    /// </para>
    /// </remarks>
    public static async Task<SearchResponse> FromAsync(
        ListingSearchResponse response,
        IObjectStore objects,
        CancellationToken cancellationToken)
    {
        var urls = await ResolveCoversAsync(response.Items, objects, cancellationToken);

        return new SearchResponse
        {
            Items = [.. response.Items.Select(item => SearchItemResponse.From(item, urls))],
            Facets = [.. response.Facets.Select(SearchFacetResponse.From)],
            NextCursor = response.NextCursor,
            TotalEstimate = response.TotalEstimate,
        };
    }

    private static async Task<Dictionary<string, string>> ResolveCoversAsync(
        IReadOnlyList<ListingResultItem> items,
        IObjectStore objects,
        CancellationToken cancellationToken)
    {
        var urls = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            if (item.Media.CoverObjectKey is not { Length: > 0 } key || urls.ContainsKey(key))
            {
                continue;
            }

            var url = await objects.CreateReadUrlAsync(key, CoverLifetime, cancellationToken);

            // A store that cannot sign is a card with no photo, not a search with no results. The
            // listing's price, title and plate are all still worth showing.
            if (url.IsSuccess)
            {
                urls[key] = url.Value.ToString();
            }
        }

        return urls;
    }

    /// <summary>
    /// How long a signed cover URL stays valid.
    /// </summary>
    /// <remarks>
    /// Longer than a page view and far shorter than the listing, so a URL copied out of devtools
    /// stops working before it is worth sharing. Technical rather than commercial — it is a property
    /// of the signing scheme, not a number anybody would want to change without a deploy.
    /// </remarks>
    private static readonly TimeSpan CoverLifetime = TimeSpan.FromHours(1);
}

/// <summary>One card, in the shape §9 publishes.</summary>
/// <remarks>
/// Named for the wire rather than for the module — <c>priceMinor</c> and <c>city</c> here against
/// <c>PriceMinorUnits</c> and <c>CityCode</c> there. The wire is the seam the frontend was built
/// against; translating at the edge is what lets either side be renamed without the other noticing.
/// </remarks>
public sealed record SearchItemResponse
{
    public required Guid ListingId { get; init; }

    public required string Intent { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PlateResponse? Registration { get; init; }

    public required string Title { get; init; }

    public required long PriceMinor { get; init; }

    public required string Currency { get; init; }

    public required string City { get; init; }

    public required IReadOnlyList<SpecItemResponse> SpecStrip { get; init; }

    public required MediaResponse Media { get; init; }

    /// <summary>Trust markers as codes. Labels resolve through the i18n registry, not here.</summary>
    public required IReadOnlyList<string> Badges { get; init; }

    public static SearchItemResponse From(ListingResultItem item, IReadOnlyDictionary<string, string> coverUrls)
    {
        string? coverUrl = null;

        if (item.Media.CoverObjectKey is { Length: > 0 } key)
        {
            coverUrls.TryGetValue(key, out coverUrl);
        }

        return new SearchItemResponse
        {
            ListingId = item.ListingId,
            Intent = item.Intent,
            Registration = item.Registration is { } plate
                ? new PlateResponse(plate.Canonical, plate.Display)
                : null,
            Title = item.Title,
            PriceMinor = item.PriceMinorUnits,
            Currency = item.Currency,
            City = item.CityCode,
            SpecStrip = [.. item.SpecStrip.Select(SpecItemResponse.From)],
            Media = new MediaResponse(coverUrl, item.Media.AspectRatio, item.Media.Count),
            Badges = item.Badges,
        };
    }
}

/// <summary>
/// One spec row. The datum, never a formatted string.
/// </summary>
/// <remarks>
/// <c>38400</c>, not "38,400" — Indian digit grouping is locale-dependent and the server does not
/// know the viewer's locale. <c>unit</c> travels separately so the client renders "38,400 km" or
/// "38.4k km" as the space allows.
/// </remarks>
public sealed record SpecItemResponse(string Code, string Label, string Value, string? Unit)
{
    public static SpecItemResponse From(SpecStripItem item)
        => new(item.Code, item.Label, item.Value, item.Unit);
}

/// <summary>The card's media block. <c>coverUrl</c> is null when the listing has no photos yet.</summary>
public sealed record MediaResponse(string? CoverUrl, double? AspectRatio, int Count);

/// <summary>
/// A facet, shaped by its rendering style.
/// </summary>
/// <remarks>
/// <c>options</c> and <c>min</c>/<c>max</c> are mutually exclusive and the unused side is omitted
/// rather than sent as null, because the client parses facets as a union discriminated on
/// <c>style</c> — a range facet carrying <c>"options": null</c> fails to match its own branch.
/// <para>
/// The style itself comes from the taxonomy registry's <c>facetStyle</c>. The server sends a style,
/// never a component name and never a colour.
/// </para>
/// </remarks>
public sealed record SearchFacetResponse
{
    public required string Code { get; init; }

    public required string Label { get; init; }

    public required string Style { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FacetOptionResponse>? Options { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Min { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Max { get; init; }

    /// <summary>The registry's wire name for a continuous facet.</summary>
    private const string RangeStyle = "range";

    public static SearchFacetResponse From(SearchFacetView facet)
    {
        var isRange = string.Equals(facet.Style, RangeStyle, StringComparison.Ordinal);

        return new SearchFacetResponse
        {
            Code = facet.Code,
            Label = facet.Label,
            Style = facet.Style,
            Options = isRange
                ? null
                : [.. facet.Options.Select(option => new FacetOptionResponse(option.Value, option.Label, option.Count))],
            Min = isRange ? facet.Min : null,
            Max = isRange ? facet.Max : null,
        };
    }
}

/// <summary>One facet option and the number of hits selecting it would leave.</summary>
public sealed record FacetOptionResponse(string Value, string Label, long Count);
