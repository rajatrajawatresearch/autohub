using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Listings.Infrastructure;

/// <summary>
/// The OpenSearch adapter, deliberately unimplemented and deliberately present.
/// </summary>
/// <remarks>
/// <b>Why this file exists at all.</b> ADR 0006 picks OpenSearch for faceted search over dynamic
/// attributes, and no cluster is running. Two ways of handling that are worse than this one: writing
/// the module against an OpenSearch client and mocking it means the thing that works in production
/// has never been run; waiting for the cluster means the entire buyer side of the product is blocked
/// on infrastructure. So the marketplace is built against <see cref="ISearchIndex"/>, PostgreSQL
/// implements it for launch volumes, and this class marks the seam the cluster drops into.
/// <para>
/// <b>Why it fails loudly rather than silently.</b> A no-op that returned an empty result set would
/// look like a working search with no inventory, which is the single most expensive failure a
/// marketplace can have — nobody reports it, and every buyer concludes there are no cars. Returning
/// an upstream error means a misconfiguration is an alert on the first request rather than a
/// conversion cliff nobody notices for a week.
/// </para>
/// <para>
/// It is not registered by default. <c>AddListingsModule</c> wires the PostgreSQL implementation the
/// persistence layer supplies; this one is chosen explicitly, which means turning OpenSearch on is a
/// deliberate act rather than a package reference nobody remembers adding.
/// </para>
/// </remarks>
public sealed class OpenSearchListingIndex : ISearchIndex
{
    private readonly ILogger<OpenSearchListingIndex> _logger;

    public OpenSearchListingIndex(ILogger<OpenSearchListingIndex> logger) => _logger = logger;

    public Task<Result<ListingSearchResult>> SearchAsync(
        ListingSearchRequest request,
        CancellationToken cancellationToken)
        => Task.FromResult(Result.Failure<ListingSearchResult>(NotConfigured()));

    public Task<Result> IndexAsync(Guid listingId, CancellationToken cancellationToken)
    {
        // Indexing failures are recoverable by a reindex sweep, so this one logs and succeeds rather
        // than blocking the transition that triggered it. A search cluster that is not there must
        // not stop a seller from publishing.
        _logger.LogWarning(
            "OpenSearch adapter is a stub; listing {ListingId} was not indexed",
            listingId);

        return Task.FromResult(Result.Success());
    }

    public Task<Result> RemoveAsync(Guid listingId, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "OpenSearch adapter is a stub; listing {ListingId} was not removed from the index",
            listingId);

        return Task.FromResult(Result.Success());
    }

    private static Error NotConfigured()
        => Error.Upstream(
            "listing.search.index_unavailable",
            "The OpenSearch adapter is not implemented in this build.");
}
