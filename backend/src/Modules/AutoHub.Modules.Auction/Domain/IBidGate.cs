using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Auction.Domain;

/// <summary>
/// The critical section of the whole platform: deciding, atomically, whether a bid wins.
/// </summary>
/// <remarks>
/// <b>The race this closes.</b> Two dealers tap "bid" on lot 41 within the same millisecond. Both
/// requests land on different API nodes. Both read a standing bid of ₹5,15,000. Both compute that
/// ₹5,20,000 clears the increment. Both write. Whichever write lands second overwrites the first,
/// and the dealer whose bid was erased was told it was accepted. There is no transaction isolation
/// level in PostgreSQL that fixes this cheaply enough for the bid path — <c>SERIALIZABLE</c> turns
/// every concurrent bid into a retry storm, and <c>SELECT … FOR UPDATE</c> serialises every bidder on
/// a hot lot behind one row lock held across a network round trip.
/// <para>
/// The fix is to move read-validate-write to where it can be one step. Redis executes a Lua script
/// single-threaded and to completion, so the whole decision — read the standing bid, apply the
/// registry's increment, resolve proxy ceilings, assign the next sequence number, extend the close if
/// the bid landed inside the anti-snipe window, record the outcome under the idempotency key —
/// happens with no other bid interleaved. The second dealer's script sees the first dealer's result,
/// which is the entire point.
/// </para>
/// <para>
/// <b>When Redis is unavailable the bid is rejected.</b> Never accepted optimistically, never queued,
/// never validated against PostgreSQL as a fallback. The failure being avoided is asymmetric: a
/// rejected bid costs one dealer one retry and a visible error, while an optimistically accepted bid
/// costs two dealers the same lot, a support case, and a sale that has to be unwound after the
/// vehicle has been collected. A "degraded mode" that accepts bids without the atomic check is not a
/// degraded mode; it is a different, broken auction.
/// </para>
/// <para>
/// <b>Redis is not the system of record.</b> It holds the contended state for the duration of a lot's
/// life and nothing more; every accepted bid is written to PostgreSQL immediately afterwards. If the
/// gate's state is missing — an eviction, a restart, a failover to an empty replica — the gate
/// refuses rather than starting from zero, and the lot is rehydrated from the database by an operator
/// or by the rehydrate path. Starting from zero would let a ₹8 lakh lot be won for the opening price.
/// </para>
/// </remarks>
public interface IBidGate
{
    /// <summary>
    /// Opens a lot for bidding, seeding the gate with its terms.
    /// </summary>
    /// <remarks>
    /// Also the rehydrate path: called with the current standing bid to restore a lot whose gate
    /// state was lost, which is why it takes the standing price rather than assuming zero.
    /// </remarks>
    Task<Result> OpenAsync(BidGateState state, CancellationToken cancellationToken);

    /// <summary>Runs the atomic admission decision for one bid.</summary>
    Task<Result<BidAdmission>> AdmitAsync(BidAttempt attempt, CancellationToken cancellationToken);

    /// <summary>Reads the standing state without deciding anything. For the lot page and for replay.</summary>
    Task<Result<BidGateSnapshot>> PeekAsync(Guid lotId, CancellationToken cancellationToken);

    /// <summary>Stops the gate accepting bids. Called when the lot closes, before settlement runs.</summary>
    Task<Result> CloseAsync(Guid lotId, CancellationToken cancellationToken);
}

/// <summary>The terms and the standing position a lot's gate is seeded with.</summary>
public sealed record BidGateState
{
    public required Guid LotId { get; init; }

    public required Money StartPrice { get; init; }

    public required DateTimeOffset ClosesAt { get; init; }

    public required int AntiSnipeWindowSeconds { get; init; }

    public required int AntiSnipeExtensionSeconds { get; init; }

    public required int MaxExtensions { get; init; }

    /// <summary>The standing bid when rehydrating. Zero for a fresh lot.</summary>
    public Money? StandingAmount { get; init; }

    public Guid? StandingBidderId { get; init; }

    /// <summary>The standing bidder's proxy ceiling, which must survive a rehydrate or they lose it.</summary>
    public Money? StandingMaximum { get; init; }

    public long Sequence { get; init; }

    public int ExtensionCount { get; init; }
}

public sealed record BidAttempt
{
    public required Guid LotId { get; init; }

    public required Guid BidderId { get; init; }

    /// <summary>What the bidder is offering now.</summary>
    public required Money Amount { get; init; }

    /// <summary>
    /// The ceiling the platform may bid up to on their behalf. Null means bid exactly
    /// <see cref="Amount"/>.
    /// </summary>
    public Money? MaximumAmount { get; init; }

    /// <summary>The minimum step, resolved from the pricing registry before the gate is called.</summary>
    public required Money MinimumIncrement { get; init; }

    /// <summary>
    /// The caller's <c>Idempotency-Key</c>.
    /// </summary>
    /// <remarks>
    /// Bids are the reason the header is mandatory on every write. A dealer on a train tunnels
    /// through a dead spot, the request times out after the gate has already accepted, the client
    /// retries — and without the key the retry is a second bid against the dealer's own standing
    /// one, which raises the price they are paying against themselves.
    /// </remarks>
    public required string IdempotencyKey { get; init; }

    /// <summary>Server time. See <see cref="AuctionLot"/> for why the client's is not accepted.</summary>
    public required DateTimeOffset PlacedAt { get; init; }
}

/// <summary>The gate's decision, and the state it left behind.</summary>
public sealed record BidAdmission
{
    public required BidOutcome Outcome { get; init; }

    /// <summary>The price the lot now stands at — not necessarily the amount that was offered.</summary>
    public required Money StandingAmount { get; init; }

    public required Guid StandingBidderId { get; init; }

    /// <summary>The sequence number this decision was assigned. Total order across all bidders.</summary>
    public required long Sequence { get; init; }

    public required DateTimeOffset PlacedAt { get; init; }

    /// <summary>Set when anti-snipe moved the close. Null when it did not.</summary>
    public DateTimeOffset? ExtendedTo { get; init; }

    public int ExtensionCount { get; init; }

    /// <summary>True when this response is a replay of an earlier identical request.</summary>
    public bool IsReplay { get; init; }

    /// <summary>Whether the bidder who submitted holds the standing bid after this decision.</summary>
    public bool BidderIsStanding(Guid bidderId) => StandingBidderId == bidderId;
}

/// <summary>The standing position, read without deciding.</summary>
public sealed record BidGateSnapshot(
    Money? StandingAmount,
    Guid? StandingBidderId,
    long Sequence,
    DateTimeOffset ClosesAt,
    int ExtensionCount,
    bool IsOpen);

/// <summary>
/// What the gate decided.
/// </summary>
/// <remarks>
/// Not a workflow state and not a status: it is the outcome of one admission decision, the same kind
/// of thing as <c>PaymentCollectStatus</c>. What the <i>lot</i> does in response — whether an
/// instant-award auction closes on this bid, whether an outbid notification goes out — is registry
/// configuration.
/// </remarks>
public enum BidOutcome
{
    /// <summary>The bidder now holds the standing bid.</summary>
    Accepted = 1,

    /// <summary>
    /// Valid, but beaten by a proxy ceiling that was already in place.
    /// </summary>
    /// <remarks>
    /// The standing price rises and the bidder does not hold it. Distinct from
    /// <see cref="TooLow"/> because the bidder did nothing wrong and the correct advice is "bid
    /// higher", not "read the increment".
    /// </remarks>
    Outbid = 2,

    /// <summary>Below the opening price, or below the standing bid plus the registry's increment.</summary>
    TooLow = 3,

    /// <summary>The lot had already closed, including after an extension had run out.</summary>
    Closed = 4,

    /// <summary>An identical request was already decided. The original decision is returned.</summary>
    Replay = 5,
}
