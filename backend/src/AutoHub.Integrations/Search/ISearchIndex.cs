using AutoHub.SharedKernel.Results;

namespace AutoHub.Integrations.Search;

/// <summary>
/// The search engine, behind an interface the product owns.
/// </summary>
/// <remarks>
/// OpenSearch is the intended implementation, and it is not running in every environment this code
/// has to boot in — a laptop, a CI job, the first ten minutes of an incident. The interface exists
/// so that fact is a registration choice rather than a startup crash.
/// <para>
/// <b>The document is a bag, not a type.</b> Facets are generated from the taxonomy registry, so the
/// set of indexed fields is unknown at compile time — a category that gains <c>boom_length</c> gains
/// a facet without a deploy, and a typed document would make that a schema change in three places.
/// </para>
/// </remarks>
public interface ISearchIndex
{
    /// <summary>Adds or replaces one document. Idempotent by id, so a replay is free.</summary>
    Task<Result> IndexAsync(SearchDocument document, CancellationToken cancellationToken);

    Task<Result> RemoveAsync(string index, string id, CancellationToken cancellationToken);

    Task<Result<SearchResults>> QueryAsync(SearchQuery query, CancellationToken cancellationToken);

    /// <summary>Whether the backing engine is reachable. Feeds the readiness probe, never a request.</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}

/// <summary>One indexable record. <see cref="Fields"/> keys are taxonomy attribute codes.</summary>
public sealed record SearchDocument(
    string Index,
    string Id,
    IReadOnlyDictionary<string, object?> Fields);

/// <summary>
/// A parsed search request.
/// </summary>
/// <remarks>
/// <see cref="AttributeFilters"/> carries the contract's <c>attr.fuel_type=petrol,cng</c> grammar:
/// values within one attribute are OR-ed, attributes are AND-ed. Modelled as data rather than as
/// method parameters because the attribute set is registry-driven and cannot be a fixed field list.
/// </remarks>
public sealed record SearchQuery
{
    public required string Index { get; init; }

    public string? Text { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> AttributeFilters { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>(0, StringComparer.Ordinal);

    public string? Sort { get; init; }

    public string? Cursor { get; init; }

    /// <summary>Bounded by the caller. Every list endpoint is paginated; there is no "all".</summary>
    public int Limit { get; init; } = 24;
}

public sealed record SearchResults(
    IReadOnlyList<SearchDocument> Items,
    string? NextCursor,
    long TotalEstimate,
    IReadOnlyList<SearchFacet> Facets);

public sealed record SearchFacet(string Code, IReadOnlyList<SearchFacetValue> Values);

public sealed record SearchFacetValue(string Value, long Count);
