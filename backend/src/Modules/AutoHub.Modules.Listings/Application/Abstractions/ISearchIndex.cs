using AutoHub.Modules.Listings.Contracts;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Listings.Application.Abstractions;

/// <summary>
/// How a search result set is ordered. The contract's <c>&amp;sort=</c> parameter.
/// </summary>
/// <remarks>
/// A closed set because each value is a different query plan and a different index, not because the
/// list is business vocabulary. An eighth sort is an index and a code path; it is not a row.
/// </remarks>
public enum ListingSort
{
    /// <summary>Text relevance, falling back to recency when there is no query text.</summary>
    Relevance = 1,

    PriceAscending = 2,

    PriceDescending = 3,

    /// <summary>Most recently published first. The default for a browse with no query.</summary>
    Newest = 4,

    /// <summary>Lowest odometer reading first, from the indexed <c>kms_driven</c> attribute.</summary>
    KilometresAscending = 5,
}

/// <summary>
/// One attribute filter from the contract's <c>attr.*</c> grammar.
/// </summary>
/// <remarks>
/// The grammar is deliberately not a fixed field list, because the facets are generated from the
/// taxonomy registry and a new attribute must become a filter without a deploy. Two shapes cover
/// everything the registry can produce:
/// <list type="bullet">
/// <item><c>attr.fuel_type=petrol,cng</c> — one or more option values, OR-ed within the attribute
/// and AND-ed across attributes, which is what every faceted search on the internet does and what
/// users expect without being told.</item>
/// <item><c>attr.kms_driven=0..40000</c> — an inclusive range over an ordered attribute.</item>
/// </list>
/// Both may be absent, which means "this attribute is not filtered", and both may be present only
/// for an attribute whose data type supports them — the registry decides which, not this record.
/// </remarks>
public sealed record AttributeFilter(IReadOnlyList<string> AnyOf, decimal? Min, decimal? Max)
{
    public static AttributeFilter OneOf(params string[] values) => new(values, null, null);

    public static AttributeFilter Between(decimal? min, decimal? max) => new([], min, max);

    public bool IsEmpty => AnyOf.Count == 0 && Min is null && Max is null;
}

/// <summary>An inclusive amount range in minor units. Both ends optional.</summary>
public sealed record MoneyRange(long? MinMinorUnits, long? MaxMinorUnits);

/// <summary>Everything §9 of the contract lets a caller ask for.</summary>
public sealed record ListingSearchRequest
{
    public string? Text { get; init; }

    public string? CityCode { get; init; }

    public ListingIntent? Intent { get; init; }

    public string? CategoryCode { get; init; }

    public IReadOnlyDictionary<string, AttributeFilter> Attributes { get; init; } =
        new Dictionary<string, AttributeFilter>(StringComparer.Ordinal);

    public MoneyRange? Price { get; init; }

    public ListingSort Sort { get; init; } = ListingSort.Relevance;

    /// <summary>Opaque continuation token. Its encoding belongs to the index, not to the caller.</summary>
    public string? Cursor { get; init; }

    public int Limit { get; init; } = 24;
}

/// <summary>
/// One hit, reduced to what the index can answer without leaving its own store.
/// </summary>
/// <remarks>
/// Deliberately primitives and ids. The contract's result item carries a title, a registration, a
/// spec strip and badges — all of which are joins against the catalog and the taxonomy registry, and
/// all of which the <i>handler</i> performs after the index has answered. Pushing them into the
/// index would mean every taxonomy edit reindexes the corpus, and a make renamed on Tuesday would
/// leave a million documents wrong until the reindex finished.
/// </remarks>
public sealed record ListingHit
{
    public required Guid ListingId { get; init; }

    public required Guid VehicleId { get; init; }

    public required ListingIntent Intent { get; init; }

    public required long PriceMinorUnits { get; init; }

    public required string Currency { get; init; }

    public required string CityCode { get; init; }

    public required int QualityScore { get; init; }

    public required int MediaCount { get; init; }

    public string? CoverObjectKey { get; init; }

    public double? CoverAspectRatio { get; init; }
}

/// <summary>One option of a facet, with the count of hits it would leave.</summary>
public sealed record FacetOption(string Value, long Count);

/// <summary>
/// A facet the index computed over the current result set.
/// </summary>
/// <remarks>
/// The index returns codes and counts only. Labels and the rendering style come from the taxonomy
/// registry at response time, so a facet's label can be translated into Marathi without touching a
/// single indexed document.
/// </remarks>
public sealed record ListingFacet(string Code, IReadOnlyList<FacetOption> Options, decimal? Min, decimal? Max);

public sealed record ListingSearchResult(
    IReadOnlyList<ListingHit> Items,
    IReadOnlyList<ListingFacet> Facets,
    string? NextCursor,
    long TotalEstimate);

/// <summary>
/// The seam between the marketplace and whatever is answering its searches.
/// </summary>
/// <remarks>
/// <b>Why an interface rather than just writing the SQL.</b> The ADR picks OpenSearch for faceted
/// search over dynamic attributes, and OpenSearch is not running today. Waiting for it would block
/// the entire buyer side of the product; writing directly against OpenSearch and stubbing it would
/// mean the thing that actually works is the stub. So the marketplace is built against this
/// interface, PostgreSQL implements it well enough for launch volumes — a partial index per
/// attribute type over the EAV table answers both the equality and the range grammar — and the
/// OpenSearch adapter drops in behind the same interface when the cluster exists.
/// <para>
/// The two implementations are not expected to be identical. PostgreSQL will not do typo tolerance
/// or relevance scoring across a description field, and that is an acceptable launch position: a
/// marketplace with exact-match search sells cars, and a marketplace with no search does not.
/// </para>
/// <para>
/// <b>Indexing is by id, not by document.</b> The caller says "this listing changed"; the index
/// reads whatever it needs. A signature that took a document would put the projection logic in every
/// caller, and the caller that forgot a field would produce hits missing a facet nobody could
/// explain.
/// </para>
/// </remarks>
public interface ISearchIndex
{
    Task<Result<ListingSearchResult>> SearchAsync(
        ListingSearchRequest request,
        CancellationToken cancellationToken);

    /// <summary>Adds or refreshes a listing. Idempotent — the outbox will deliver twice eventually.</summary>
    Task<Result> IndexAsync(Guid listingId, CancellationToken cancellationToken);

    /// <summary>Removes a listing from the index without deleting it from the database.</summary>
    Task<Result> RemoveAsync(Guid listingId, CancellationToken cancellationToken);
}
