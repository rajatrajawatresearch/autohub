using System.Collections.Concurrent;
using System.Globalization;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Integrations.Search;

/// <summary>
/// A working search index held in process memory.
/// </summary>
/// <remarks>
/// This exists so the API boots and serves without OpenSearch, which is the state of every developer
/// laptop and every CI run. It is deliberately <b>working</b> rather than a no-op that returns
/// nothing: an adapter that silently answers "no results" is indistinguishable from a bug, and the
/// first person to hit it spends an afternoon proving the indexer is fine.
/// <para>
/// What it does not do is what OpenSearch is for — analysers, typo tolerance, relevance scoring,
/// geo distance, and a working set larger than one node's heap. It matches exactly, counts facets
/// exactly, and paginates by offset. That is enough to develop against and nowhere near enough to
/// ship, which is why <see cref="IsAvailableAsync"/> is honest about being a fallback.
/// </para>
/// </remarks>
public sealed class InMemorySearchIndex : ISearchIndex
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SearchDocument>> _indexes
        = new(StringComparer.Ordinal);

    public Task<Result> IndexAsync(SearchDocument document, CancellationToken cancellationToken)
    {
        _indexes.GetOrAdd(document.Index, _ => new ConcurrentDictionary<string, SearchDocument>(StringComparer.Ordinal))
            [document.Id] = document;

        return Task.FromResult(Result.Success());
    }

    public Task<Result> RemoveAsync(string index, string id, CancellationToken cancellationToken)
    {
        if (_indexes.TryGetValue(index, out var documents))
        {
            documents.TryRemove(id, out _);
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result<SearchResults>> QueryAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        if (!_indexes.TryGetValue(query.Index, out var documents))
        {
            return Task.FromResult(Result.Success(new SearchResults([], null, 0, [])));
        }

        var matches = documents.Values.Where(document => Matches(document, query)).ToList();

        var offset = ParseCursor(query.Cursor);
        var page = matches.Skip(offset).Take(query.Limit).ToList();
        var nextOffset = offset + page.Count;

        var facets = query.AttributeFilters.Keys
            .Select(code => new SearchFacet(code, CountValues(matches, code)))
            .ToList();

        return Task.FromResult(Result.Success(new SearchResults(
            page,
            nextOffset < matches.Count ? nextOffset.ToString(CultureInfo.InvariantCulture) : null,
            matches.Count,
            facets)));
    }

    /// <summary>Always true — the fallback is the thing that is available when nothing else is.</summary>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    private static bool Matches(SearchDocument document, SearchQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Text)
            && !document.Fields.Values.Any(value =>
                value?.ToString()?.Contains(query.Text, StringComparison.OrdinalIgnoreCase) == true))
        {
            return false;
        }

        // Values within an attribute are OR-ed, attributes are AND-ed. That is the contract's
        // grammar, and getting it backwards produces a filter rail where picking two fuel types
        // returns nothing — which reads to a user as a broken site rather than as a bug.
        foreach (var (code, wanted) in query.AttributeFilters)
        {
            if (!document.Fields.TryGetValue(code, out var actual))
            {
                return false;
            }

            var text = actual?.ToString();

            if (text is null || !wanted.Contains(text, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static List<SearchFacetValue> CountValues(IEnumerable<SearchDocument> documents, string code)
        => documents
            .Select(document => document.Fields.GetValueOrDefault(code)?.ToString())
            .Where(value => !string.IsNullOrEmpty(value))
            .GroupBy(value => value!, StringComparer.Ordinal)
            .Select(group => new SearchFacetValue(group.Key, group.LongCount()))
            .OrderByDescending(value => value.Count)
            .ThenBy(value => value.Value, StringComparer.Ordinal)
            .ToList();

    private static int ParseCursor(string? cursor)
        => int.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var offset) ? offset : 0;
}
