using AutoHub.SharedKernel.Domain;

namespace AutoHub.Modules.Auction.Domain;

/// <summary>
/// A bid was accepted and the standing price moved.
/// </summary>
/// <remarks>
/// Carries <see cref="Sequence"/>, which is the gate's monotonic counter and the same number the
/// socket broadcasts. A client reconnecting with <c>?sinceSeq=1042</c> is asking for every event
/// after this one, so the number has to be on the event that produced it rather than assigned later
/// by whatever published it.
/// </remarks>
public sealed record BidPlaced(
    Guid LotId,
    DateTimeOffset At,
    long Sequence,
    long StandingAmountMinorUnits,
    string Currency,
    Guid StandingBidderId,
    bool IsReserveMet) : DomainEvent(LotId, At)
{
    public override string EventType => "auction.bid_placed.v1";
}

public sealed record LotExtended(
    Guid LotId,
    DateTimeOffset At,
    DateTimeOffset EndsAt,
    int ExtensionCount) : DomainEvent(LotId, At)
{
    public override string EventType => "auction.lot_extended.v1";
}

/// <summary>
/// A lot closed with a winner.
/// </summary>
/// <remarks>
/// The event the deals module reacts to. It carries the payment deadline so the winner's window is a
/// fact of the sale rather than something recomputed later from a duration that may since have been
/// edited in the profile.
/// </remarks>
public sealed record LotSold(
    Guid LotId,
    DateTimeOffset At,
    Guid WinnerId,
    long AmountMinorUnits,
    string Currency,
    DateTimeOffset PaymentDueAt) : DomainEvent(LotId, At)
{
    public override string EventType => "auction.lot_sold.v1";
}

public sealed record LotUnsold(
    Guid LotId,
    DateTimeOffset At,
    string ReasonCode,
    long? HighBidMinorUnits) : DomainEvent(LotId, At)
{
    public override string EventType => "auction.lot_unsold.v1";
}

/// <summary>
/// A winner did not pay inside their window.
/// </summary>
/// <remarks>
/// Consumed by trust — a dealer who forfeits twice is a dealer whose bidding limit needs looking at —
/// and by payments, which forfeits the deposit under whichever pricing rule the registry says
/// applies.
/// </remarks>
public sealed record LotForfeited(
    Guid LotId,
    DateTimeOffset At,
    Guid ForfeitedWinnerId,
    long AmountMinorUnits,
    string ReasonCode) : DomainEvent(LotId, At)
{
    public override string EventType => "auction.lot_forfeited.v1";
}
