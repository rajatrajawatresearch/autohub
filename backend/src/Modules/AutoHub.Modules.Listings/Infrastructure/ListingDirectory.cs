using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Listings.Infrastructure;

/// <summary>
/// The listings contract, served in-process today and over HTTP after extraction.
/// </summary>
/// <remarks>
/// Projections only. Nothing here lets another module change a listing, because a listing changes by
/// moving through its workflow and a caller that could write around that would be writing around the
/// audit trail, the guards and the permissions all at once.
/// </remarks>
internal sealed class ListingDirectory : IListingDirectory
{
    private readonly IListingRepository _listings;

    public ListingDirectory(IListingRepository listings) => _listings = listings;

    public async Task<Result<ListingSummary>> GetAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await _listings.GetAsync(listingId, cancellationToken);

        return listing is null
            ? Error.NotFound("listing.not_found", "No listing with that id.").With("listingId", listingId)
            : listing.ToSummary();
    }

    public async Task<Result<IReadOnlyDictionary<Guid, ListingSummary>>> GetManyAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken)
    {
        if (listingIds.Count == 0)
        {
            return new Dictionary<Guid, ListingSummary>();
        }

        var listings = await _listings.GetManyAsync(listingIds, cancellationToken);

        // Missing ids are absent rather than fatal, for the same reason the catalog's batch read is:
        // one soft-deleted listing behind a settlement run must not fail the whole run.
        return listings.ToDictionary(l => l.Id, l => l.ToSummary());
    }
}
