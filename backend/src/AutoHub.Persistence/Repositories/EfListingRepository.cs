using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Modules.Listings.Domain.Offers;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Repositories;

public sealed class EfListingRepository : IListingRepository
{
    private readonly AutoHubDbContext _context;

    public EfListingRepository(AutoHubDbContext context) => _context = context;

    public async Task<Listing?> GetAsync(Guid listingId, CancellationToken cancellationToken)
        => await _context.Listings
            .Include(listing => listing.Media)
            .FirstOrDefaultAsync(listing => listing.Id == listingId, cancellationToken);

    public async Task<IReadOnlyList<Listing>> GetManyAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken)
    {
        if (listingIds.Count == 0)
        {
            return [];
        }

        var ids = listingIds.ToArray();

        return await _context.Listings
            .Include(listing => listing.Media)
            .Where(listing => ids.Contains(listing.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Listing listing, CancellationToken cancellationToken)
        => await _context.Listings.AddAsync(listing, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// The offer repository, and the one query that shows how "open" is answered without a status.
/// </summary>
/// <remarks>
/// <see cref="FindOpenAsync"/> joins the offer to its workflow instance and takes the ones the engine
/// has not completed. That is the same row the engine itself reads, so the answer cannot drift — as
/// it would if the offer carried its own <c>is_open</c> flag that a failed transition left stale.
/// </remarks>
public sealed class EfListingOfferRepository : IListingOfferRepository
{
    private readonly AutoHubDbContext _context;

    public EfListingOfferRepository(AutoHubDbContext context) => _context = context;

    public async Task<ListingOffer?> GetAsync(Guid offerId, CancellationToken cancellationToken)
        => await _context.ListingOffers
            .Include(offer => offer.Turns)
            .FirstOrDefaultAsync(offer => offer.Id == offerId, cancellationToken);

    public async Task<ListingOffer?> FindOpenAsync(
        Guid listingId,
        Guid buyerPartyId,
        CancellationToken cancellationToken)
        => await _context.ListingOffers
            .Include(offer => offer.Turns)
            .Where(offer => offer.ListingId == listingId && offer.BuyerPartyId == buyerPartyId)
            .Where(offer => _context.WorkflowInstances.Any(instance =>
                instance.WorkflowKey == ListingWorkflows.Offer
                && instance.EntityId == offer.Id
                && instance.CompletedAt == null))
            .OrderByDescending(offer => offer.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(ListingOffer offer, CancellationToken cancellationToken)
        => await _context.ListingOffers.AddAsync(offer, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}
