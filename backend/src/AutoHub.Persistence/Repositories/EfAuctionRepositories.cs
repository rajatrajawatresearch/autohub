using AutoHub.Modules.Auction.Domain;
using AutoHub.Platform.Workflow;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Repositories;

public sealed class EfAuctionRepository : IAuctionRepository
{
    private readonly AutoHubDbContext _context;

    public EfAuctionRepository(AutoHubDbContext context) => _context = context;

    public async Task<Modules.Auction.Domain.Auction?> GetAsync(
        Guid auctionId,
        CancellationToken cancellationToken)
        => await _context.Auctions
            .Include(auction => auction.Lots)
            .FirstOrDefaultAsync(auction => auction.Id == auctionId, cancellationToken);

    public async Task AddAsync(Modules.Auction.Domain.Auction auction, CancellationToken cancellationToken)
        => await _context.Auctions.AddAsync(auction, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// Lots and bids, kept apart on the hot path.
/// </summary>
/// <remarks>
/// <see cref="GetAsync"/> loads no bids, matching the interface's contract and for the reason it
/// states: a lot with nine hundred bids attached is nine hundred rows materialised to append one, on
/// the hottest path in the product, several times a second, per lot. Bids are read through
/// <see cref="GetBidsAsync"/> when somebody actually wants them.
/// </remarks>
public sealed class EfAuctionLotRepository : IAuctionLotRepository
{
    private readonly AutoHubDbContext _context;

    public EfAuctionLotRepository(AutoHubDbContext context) => _context = context;

    public async Task<AuctionLot?> GetAsync(Guid lotId, CancellationToken cancellationToken)
        => await _context.AuctionLots
            .FirstOrDefaultAsync(lot => lot.Id == lotId, cancellationToken);

    /// <summary>
    /// Lots whose close has passed and whose workflow has not been closed out.
    /// </summary>
    /// <remarks>
    /// "Not yet closed" is answered by the workflow instance rather than by a status column, the same
    /// way the listings module answers "open offer": the engine's row is the one source of truth, so
    /// a sweeper and the engine cannot disagree about what is still running.
    /// <para>
    /// Ordered by <c>closes_at</c> so the batch clears the most overdue lots first. A sale that ran
    /// long leaves hundreds due at once, and a dealer whose lot closed ten minutes ago is angrier
    /// than one whose closed ten seconds ago.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<AuctionLot>> GetDueForCloseAsync(
        DateTimeOffset asOf,
        int batchSize,
        CancellationToken cancellationToken)
        => await _context.AuctionLots
            .Where(lot => lot.ClosesAt <= asOf && lot.ClosedAt == null)
            .Where(lot => _context.WorkflowInstances.Any(instance =>
                instance.WorkflowKey == AuctionWorkflow.LotKey
                && instance.EntityId == lot.Id
                && instance.CompletedAt == null))
            .OrderBy(lot => lot.ClosesAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task AddBidAsync(Bid bid, CancellationToken cancellationToken)
        => await _context.Bids.AddAsync(bid, cancellationToken);

    /// <summary>
    /// The bid that won, by the gate's sequence.
    /// </summary>
    /// <remarks>
    /// Ordered by sequence rather than by amount or timestamp, exactly as the interface requires:
    /// amount ties on an exact proxy match and timestamps tie at millisecond resolution, so the
    /// sequence is the only field guaranteed unique and totally ordered. Refused attempts are
    /// excluded — a bid the gate turned down never stood at any price.
    /// </remarks>
    public async Task<Bid?> GetWinningBidAsync(Guid lotId, CancellationToken cancellationToken)
        => await _context.Bids
            .Where(bid => bid.AuctionLotId == lotId && bid.Outcome == BidOutcome.Accepted)
            .OrderByDescending(bid => bid.Sequence)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Bid>> GetBidsAsync(
        Guid lotId,
        long sinceSequence,
        CancellationToken cancellationToken)
        => await _context.Bids
            .Where(bid => bid.AuctionLotId == lotId && bid.Sequence > sinceSequence)
            .OrderBy(bid => bid.Sequence)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// Serves auction type profiles from the database.
/// </summary>
/// <remarks>
/// Read on the creation path only — an auction copies the profile once and never reads it again —
/// so this is deliberately not behind the registry cache. Caching a row that is read once per
/// auction and edited by operations would trade a rare query for a stale profile.
/// </remarks>
public sealed class EfAuctionTypeProfileStore : IAuctionTypeProfileStore
{
    private readonly AutoHubDbContext _context;

    public EfAuctionTypeProfileStore(AutoHubDbContext context) => _context = context;

    public async Task<AuctionTypeProfile?> GetAsync(string typeCode, CancellationToken cancellationToken)
        => await _context.AuctionTypeProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                profile => profile.TypeCode == typeCode && profile.IsActive,
                cancellationToken);
}
