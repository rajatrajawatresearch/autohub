using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;

namespace AutoHub.Modules.Auction.Domain;

/// <summary>
/// The durable record of one admission decision.
/// </summary>
/// <remarks>
/// Written for every attempt the gate decided, including the ones it refused. A rejected bid is the
/// evidence that answers "I bid and it did not go through" — and that question arrives, from a dealer
/// who is certain, within an hour of every busy sale.
/// <para>
/// <see cref="Sequence"/> comes from the gate, not from a database sequence, and it is the total
/// order two bidders can be shown. <see cref="PlacedAt"/> is the server's instant for the same
/// reason: a bid's position is decided by when the platform admitted it, never by when a client
/// claims it was sent.
/// </para>
/// <para>
/// <see cref="MaximumAmount"/> is a secret. It is the bidder's ceiling, and publishing it — even to
/// the seller — would tell the room exactly how far the competition can go, which is the whole value
/// of proxy bidding destroyed in one API response.
/// </para>
/// </remarks>
public sealed class Bid : Entity, IAuditable
{
    private Bid(Guid id, Guid lotId, Guid bidderId, Money amount, string idempotencyKey)
        : base(id)
    {
        AuctionLotId = lotId;
        BidderId = bidderId;
        Amount = amount;
        IdempotencyKey = idempotencyKey;
    }

    private Bid() { }

    public Guid AuctionLotId { get; private set; }

    public Guid BidderId { get; private set; }

    public string BidderType { get; private set; } = string.Empty;

    /// <summary>What was offered.</summary>
    public Money Amount { get; private set; }

    /// <summary>The bidder's ceiling. Never leaves the server.</summary>
    public Money? MaximumAmount { get; private set; }

    /// <summary>The price the lot stood at after this decision.</summary>
    public Money StandingAmountAfter { get; private set; }

    /// <summary>The gate's total order. Zero on a bid the gate refused before assigning one.</summary>
    public long Sequence { get; private set; }

    public BidOutcome Outcome { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>Server time, from <c>IClock</c>. Never a client's.</summary>
    public DateTimeOffset PlacedAt { get; private set; }

    /// <summary>The increment that applied at this price, from the pricing registry. Kept for the audit trail.</summary>
    public long AppliedIncrementMinorUnits { get; private set; }

    /// <summary>Feeds fraud scoring: two "different" dealers bidding from one device is a ring.</summary>
    public string? DeviceId { get; private set; }

    public string? IpAddress { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static Bid Record(BidAttempt attempt, BidAdmission admission, string bidderType, string? deviceId, string? ipAddress)
        => new(UuidV7.New(attempt.PlacedAt), attempt.LotId, attempt.BidderId, attempt.Amount, attempt.IdempotencyKey)
        {
            BidderType = bidderType,
            MaximumAmount = attempt.MaximumAmount,
            StandingAmountAfter = admission.StandingAmount,
            Sequence = admission.Sequence,
            Outcome = admission.Outcome,
            PlacedAt = attempt.PlacedAt,
            AppliedIncrementMinorUnits = attempt.MinimumIncrement.MinorUnits,
            DeviceId = deviceId,
            IpAddress = ipAddress,
        };
}
