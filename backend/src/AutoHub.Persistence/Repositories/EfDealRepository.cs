using AutoHub.Modules.Deals.Domain;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Repositories;

/// <summary>
/// Loads and saves deals, always with their parties and lines.
/// </summary>
/// <remarks>
/// Both collections are included on every read rather than left to lazy loading, because every
/// caller needs them: quoting rebuilds the lines, acceptance walks the parties, settlement allocates
/// across both. A deal has single-digit rows of each, so the join is cheaper than the round trip a
/// second query would cost — and lazy loading is off model-wide, so a missed <c>Include</c> would be
/// an empty collection that reads as "this deal has no parties" rather than as a bug.
/// </remarks>
public sealed class EfDealRepository : IDealRepository
{
    private readonly AutoHubDbContext _context;

    public EfDealRepository(AutoHubDbContext context) => _context = context;

    public async Task<Deal?> GetAsync(Guid dealId, CancellationToken cancellationToken)
        => await _context.Deals
            .Include(deal => deal.Parties)
            .Include(deal => deal.Lines)
            .FirstOrDefaultAsync(deal => deal.Id == dealId, cancellationToken);

    /// <summary>
    /// The idempotency guard every module that opens a deal on an event runs first.
    /// </summary>
    /// <remarks>
    /// Matches the partial unique index behind it, so a caller that races this check still collides
    /// in the database rather than opening a second deal for one auction lot and paying the seller
    /// twice.
    /// </remarks>
    public async Task<Deal?> FindBySourceAsync(
        string sourceType,
        Guid sourceId,
        CancellationToken cancellationToken)
        => await _context.Deals
            .Include(deal => deal.Parties)
            .Include(deal => deal.Lines)
            .FirstOrDefaultAsync(
                deal => deal.SourceType == sourceType && deal.SourceId == sourceId,
                cancellationToken);

    public async Task AddAsync(Deal deal, CancellationToken cancellationToken)
        => await _context.Deals.AddAsync(deal, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}
