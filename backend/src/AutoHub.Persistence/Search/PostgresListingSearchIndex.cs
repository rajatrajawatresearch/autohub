using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Search;

/// <summary>
/// Faceted listing search on PostgreSQL — the implementation that actually runs today.
/// </summary>
/// <remarks>
/// <b>Why PostgreSQL is enough to launch with.</b> ADR 0006 chooses OpenSearch, and it is the right
/// long-term answer for typo tolerance, relevance scoring and geo queries at scale. None of those is
/// what stops a marketplace launching; having no search at all is. The queries the contract's §9
/// grammar actually needs are an equality filter, a range filter and a facet count, and PostgreSQL
/// answers all three with indexes that already exist:
/// <list type="bullet">
/// <item>Equality on an attribute uses <c>(attribute_definition_id, value_text)</c>, a partial B-tree
/// on the EAV table.</item>
/// <item>A range uses <c>(attribute_definition_id, value_number)</c>, which is the composite the
/// EAV design argues for at length and the one a GIN index over JSONB cannot answer at all.</item>
/// <item>Facet counts are a grouped aggregate over the same rows, restricted to the filtered set by
/// a subquery rather than by materialising ids into the application.</item>
/// </list>
/// <para>
/// <b>What it deliberately does not do.</b> No stemming, no fuzzy matching, no relevance ranking
/// across a description. <see cref="ListingSort.Relevance"/> falls back to recency, which is honest:
/// pretending to rank by relevance with a trigram similarity nobody tuned would be worse than
/// admitting the ordering is chronological.
/// </para>
/// <para>
/// <b>Membership is decided by the registry, not by this class.</b> A listing is searchable because a
/// workflow effect indexed it, and that effect is wired to whichever transition an operator decided
/// means "on the market". Nothing here reads a state or compares a status.
/// </para>
/// </remarks>
public sealed class PostgresListingSearchIndex : ISearchIndex
{
    private readonly AutoHubDbContext _context;
    private readonly IClock _clock;

    public PostgresListingSearchIndex(AutoHubDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result<ListingSearchResult>> SearchAsync(
        ListingSearchRequest request,
        CancellationToken cancellationToken)
    {
        var documents = _context.ListingSearchDocuments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.CityCode))
        {
            documents = documents.Where(d => d.CityCode == request.CityCode);
        }

        if (request.Intent is { } intent)
        {
            documents = documents.Where(d => d.Intent == intent);
        }

        if (request.Price?.MinMinorUnits is { } floor)
        {
            documents = documents.Where(d => d.PriceMinorUnits >= floor);
        }

        if (request.Price?.MaxMinorUnits is { } ceiling)
        {
            documents = documents.Where(d => d.PriceMinorUnits <= ceiling);
        }

        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            // Trigram containment. `ILIKE '%term%'` is what the GIN trigram index accelerates, and it
            // is what makes "swif" find "Swift" — which a stemmer would not.
            var term = request.Text.Trim();
            documents = documents.Where(d => EF.Functions.ILike(d.SearchText, $"%{term}%"));
        }

        var attributeIds = await ResolveAttributeIdsAsync(request.Attributes.Keys, cancellationToken);

        foreach (var (code, filter) in request.Attributes)
        {
            if (filter.IsEmpty || !attributeIds.TryGetValue(code, out var definitionId))
            {
                // An unknown attribute code is dropped rather than returning nothing. A stale
                // bookmark carrying a facet that has since been retired should show the city's
                // listings, not an empty page that reads as "there are no cars in Pune".
                continue;
            }

            // Captured into locals: the closure is translated into a subquery per filter, and
            // capturing the loop variables directly would bind every filter to the last one.
            var id = definitionId;
            var anyOf = filter.AnyOf;
            var min = filter.Min;
            var max = filter.Max;

            // Matches an attribute on either the listing or the car it is selling. The facts a buyer
            // filters by live on the vehicle, so a subquery restricted to the listing found nothing
            // and every filter returned an empty page — the same mistake the facet counts made.
            // Listing first in the `||` so an override, where one exists, decides the match.
            documents = documents.Where(d => _context.EntityAttributeValues.Any(value =>
                ((value.EntityType == ListingEntityTypes.Listing && value.EntityId == d.ListingId)
                    || (value.EntityType == CatalogEntityTypes.Vehicle && value.EntityId == d.VehicleId))
                && value.AttributeDefinitionId == id
                && (anyOf.Count == 0 || (value.ValueText != null && anyOf.Contains(value.ValueText)))
                && (min == null || value.ValueNumber >= min)
                && (max == null || value.ValueNumber <= max)));
        }

        var facets = await BuildFacetsAsync(documents, cancellationToken);

        documents = SearchCursor.Apply(documents, request.Sort, SearchCursor.Parse(request.Cursor));

        var ordered = Order(documents, request.Sort);

        // One extra row tells us whether a next page exists, which costs a row instead of a COUNT
        // over the whole filtered set.
        var page = await ordered.Take(request.Limit + 1).ToListAsync(cancellationToken);

        var hasMore = page.Count > request.Limit;
        var items = hasMore ? page[..request.Limit] : page;

        return new ListingSearchResult(
            [.. items.Select(ToHit)],
            facets,
            hasMore ? SearchCursor.Format(items[^1], request.Sort) : null,
            hasMore ? items.Count + 1L : items.Count);
    }

    /// <summary>
    /// Adds or refreshes a listing's document.
    /// </summary>
    /// <remarks>
    /// Idempotent by upsert, which it has to be: the outbox delivers at least once and a retried
    /// transition fires the effect again. The odometer reading is copied out of the EAV rows because
    /// the contract publishes <c>sort=kms_asc</c>, and a sort through the EAV join would materialise
    /// every matching row before it could order them.
    /// </remarks>
    public async Task<Result> IndexAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await _context.Listings
            .Include(l => l.Media)
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken);

        if (listing is null)
        {
            return Error.NotFound("listing.not_found", "No listing with that id.")
                .With("listingId", listingId);
        }

        var document = await _context.ListingSearchDocuments
            .FirstOrDefaultAsync(d => d.ListingId == listingId, cancellationToken);

        if (document is null)
        {
            document = ListingSearchDocument.For(listingId);
            await _context.ListingSearchDocuments.AddAsync(document, cancellationToken);
        }

        var registration = await _context.VehicleIdentities
            .Where(identity => identity.VehicleId == listing.VehicleId && identity.ValidTo == null)
            .Select(identity => identity.Value)
            .FirstOrDefaultAsync(cancellationToken);

        var kilometres = await _context.EntityAttributeValues
            .Where(value =>
                value.EntityType == ListingEntityTypes.Listing
                && value.EntityId == listingId
                && value.ValueNumber != null)
            .Join(
                _context.AttributeDefinitions.Where(d => d.Code == KilometresAttributeCode),
                value => value.AttributeDefinitionId,
                definition => definition.Id,
                (value, _) => value.ValueNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var cover = listing.Cover;

        document.Populate(
            listing.VehicleId,
            listing.CategoryId,
            listing.Intent,
            listing.PriceMinorUnits,
            listing.PriceCurrencyCode,
            listing.CityCode,
            listing.QualityScore,
            listing.Media.Count,
            cover?.ObjectKey,
            cover?.AspectRatio,
            BuildSearchText(listing.Title, listing.Description, registration),
            kilometres is null ? null : (long)kilometres.Value,
            _clock.UtcNow);

        return Result.Success();
    }

    public async Task<Result> RemoveAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var document = await _context.ListingSearchDocuments
            .FirstOrDefaultAsync(d => d.ListingId == listingId, cancellationToken);

        if (document is not null)
        {
            _context.ListingSearchDocuments.Remove(document);
        }

        // Removing something already absent is success, not a failure: the effect is retried by the
        // engine and re-delivered by the outbox, and either would otherwise abort a transition that
        // has nothing left to do.
        return Result.Success();
    }

    /// <summary>
    /// The attribute code the odometer reading is promoted from.
    /// </summary>
    /// <remarks>
    /// The single taxonomy code this layer knows by name, and it is here because the contract
    /// publishes <c>sort=kms_asc</c> — a sort key has to be a column, and a column has to be filled
    /// from a named source. Every other attribute stays anonymous and is filtered through the EAV
    /// join, which is what keeps a new attribute from needing a schema change.
    /// </remarks>
    private const string KilometresAttributeCode = "kms_driven";

    private static string BuildSearchText(string? title, string? description, string? registration)
        => string.Join(' ', new[] { title, description, registration }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    private async Task<Dictionary<string, Guid>> ResolveAttributeIdsAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken)
    {
        var wanted = codes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (wanted.Length == 0)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var definitions = await _context.AttributeDefinitions
            .AsNoTracking()
            .Where(definition => wanted.Contains(definition.Code))
            .Select(definition => new { definition.Code, definition.Id })
            .ToListAsync(cancellationToken);

        return definitions.ToDictionary(d => d.Code, d => d.Id, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Counts every option and measures every numeric range over the whole filtered set.
    /// </summary>
    /// <remarks>
    /// <b>Over the filtered set, not the page.</b> A facet that counted the twenty-four rows on
    /// screen would say "Petrol (12)" on a corpus of four hundred petrol cars, which is worse than
    /// no count at all. The restriction is a subquery over the already-filtered document query, so
    /// the ids never leave the database.
    /// <para>
    /// Two queries rather than one per attribute: options group by <c>value_text</c>, ranges
    /// aggregate <c>value_number</c>, and a category with fifteen facets would otherwise be fifteen
    /// round trips per search.
    /// </para>
    /// </remarks>
    private async Task<List<ListingFacet>> BuildFacetsAsync(
        IQueryable<ListingSearchDocument> documents,
        CancellationToken cancellationToken)
    {
        var listingIds = documents.Select(d => d.ListingId);
        var vehicleIds = documents.Select(d => d.VehicleId);

        // Facet over the vehicles as well as the listings.
        //
        // Everything a buyer filters by — fuel, kilometres, owners, transmission, battery health —
        // is a fact about the car, and the car is where it is stored. Counting only the listing's
        // own attributes returned an empty facet set on a marketplace with seventeen vehicles in it,
        // and an empty facet set is not a cosmetic failure: the rail vanishes, and with it the only
        // demonstration that filters come from the taxonomy registry rather than from code.
        var values = _context.EntityAttributeValues
            .AsNoTracking()
            .Where(value =>
                (value.EntityType == ListingEntityTypes.Listing && listingIds.Contains(value.EntityId))
                || (value.EntityType == CatalogEntityTypes.Vehicle && vehicleIds.Contains(value.EntityId)));

        var optionCounts = await values
            .Where(value => value.ValueText != null)
            .GroupBy(value => new { value.AttributeDefinitionId, value.ValueText })
            .Select(group => new
            {
                group.Key.AttributeDefinitionId,
                group.Key.ValueText,
                Count = group.LongCount(),
            })
            .ToListAsync(cancellationToken);

        var ranges = await values
            .Where(value => value.ValueNumber != null)
            .GroupBy(value => value.AttributeDefinitionId)
            .Select(group => new
            {
                AttributeDefinitionId = group.Key,
                Min = group.Min(value => value.ValueNumber),
                Max = group.Max(value => value.ValueNumber),
            })
            .ToListAsync(cancellationToken);

        var definitionIds = optionCounts.Select(o => o.AttributeDefinitionId)
            .Concat(ranges.Select(r => r.AttributeDefinitionId))
            .Distinct()
            .ToArray();

        if (definitionIds.Length == 0)
        {
            return [];
        }

        var codes = await _context.AttributeDefinitions
            .AsNoTracking()
            .Where(definition => definitionIds.Contains(definition.Id))
            .Select(definition => new { definition.Id, definition.Code })
            .ToListAsync(cancellationToken);

        var codesById = codes.ToDictionary(c => c.Id, c => c.Code);
        var facets = new List<ListingFacet>(definitionIds.Length);

        foreach (var id in definitionIds)
        {
            if (!codesById.TryGetValue(id, out var code))
            {
                continue;
            }

            var options = optionCounts
                .Where(o => o.AttributeDefinitionId == id)
                .OrderByDescending(o => o.Count)
                .Select(o => new AutoHub.Modules.Listings.Application.Abstractions.FacetOption(o.ValueText!, o.Count))
                .ToList();

            var range = ranges.Find(r => r.AttributeDefinitionId == id);

            facets.Add(new ListingFacet(code, options, range?.Min, range?.Max));
        }

        return facets;
    }

    /// <summary>
    /// Applies the sort, with the listing id as the tie-break every ordering needs.
    /// </summary>
    /// <remarks>
    /// The tie-break is not cosmetic: cursor pagination over a non-unique sort key skips and repeats
    /// rows whenever two listings share a price, and the user sees the same car twice on page three.
    /// The id is UUID v7, so it is also the recency ordering — which is why
    /// <see cref="ListingSort.Newest"/> and <see cref="ListingSort.Relevance"/> need no second column.
    /// </remarks>
    private static IQueryable<ListingSearchDocument> Order(
        IQueryable<ListingSearchDocument> documents,
        ListingSort sort) => sort switch
        {
            ListingSort.PriceAscending =>
                documents.OrderBy(d => d.PriceMinorUnits).ThenBy(d => d.ListingId),

            ListingSort.PriceDescending =>
                documents.OrderByDescending(d => d.PriceMinorUnits).ThenBy(d => d.ListingId),

            ListingSort.KilometresAscending =>
                documents.OrderBy(d => d.KilometresDriven ?? long.MaxValue).ThenBy(d => d.ListingId),

            // Newest and relevance are the same ordering here, and saying so is more honest than
            // inventing a similarity score nobody tuned. When OpenSearch lands, relevance diverges.
            _ => documents.OrderByDescending(d => d.ListingId),
        };

    private static ListingHit ToHit(ListingSearchDocument document) => new()
    {
        ListingId = document.ListingId,
        VehicleId = document.VehicleId,
        Intent = document.Intent,
        PriceMinorUnits = document.PriceMinorUnits,
        Currency = document.Currency,
        CityCode = document.CityCode,
        QualityScore = document.QualityScore,
        MediaCount = document.MediaCount,
        CoverObjectKey = document.CoverObjectKey,
        CoverAspectRatio = document.CoverAspectRatio,
    };
}
