namespace AutoHub.Modules.Auction.Domain;

public interface IAuctionRepository
{
    Task<Auction?> GetAsync(Guid auctionId, CancellationToken cancellationToken);

    Task AddAsync(Auction auction, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Loads and saves lots and their bids.
/// </summary>
/// <remarks>
/// <see cref="GetAsync"/> deliberately does not load bids. The bid path reads a lot to record an
/// outcome, and a lot with nine hundred bids attached is nine hundred rows materialised to append one
/// — on the hottest path in the product, several times a second, per lot.
/// </remarks>
public interface IAuctionLotRepository
{
    Task<AuctionLot?> GetAsync(Guid lotId, CancellationToken cancellationToken);

    /// <summary>Lots whose close has passed and whose workflow has not yet been closed out.</summary>
    Task<IReadOnlyList<AuctionLot>> GetDueForCloseAsync(
        DateTimeOffset asOf,
        int batchSize,
        CancellationToken cancellationToken);

    Task AddBidAsync(Bid bid, CancellationToken cancellationToken);

    /// <summary>
    /// The bid that won, by sequence.
    /// </summary>
    /// <remarks>
    /// Ordered by the gate's sequence rather than by amount or by timestamp. Amount ties on an exact
    /// proxy match, and timestamps tie at millisecond resolution; the sequence is the only field that
    /// is guaranteed unique and totally ordered, which is why the gate assigns it.
    /// </remarks>
    Task<Bid?> GetWinningBidAsync(Guid lotId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Bid>> GetBidsAsync(Guid lotId, long sinceSequence, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
