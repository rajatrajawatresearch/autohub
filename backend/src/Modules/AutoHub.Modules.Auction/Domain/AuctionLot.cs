using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Auction.Domain;

/// <summary>
/// One vehicle in an auction, and the unit every bid contends on.
/// </summary>
/// <remarks>
/// A lot is its own aggregate root even though it belongs to an auction, and that is a deliberate
/// consistency decision rather than an oversight. Two dealers bidding on lot 41 must not serialise
/// against two dealers bidding on lot 42; making the auction the aggregate would put four hundred
/// lots behind one row lock and turn a busy sale into a queue.
/// <para>
/// <b>There is no status column here either.</b> Open, closed, sold, unsold, awaiting payment,
/// forfeited, relisted — all of that is the workflow registry's, keyed on this lot's id. What lives
/// here are the facts a bid needs: the standing price, who holds it, when the lot closes after
/// however many extensions, and how many extensions have been spent.
/// </para>
/// <para>
/// <b>Every time on this row is server time.</b> <c>IClock</c> supplies it, never the client. A bid
/// that carried its own timestamp would let a dealer with a skewed laptop, or a modified client,
/// claim to have bid three seconds ago and beat somebody who genuinely bid first — and the platform
/// would have no way to prove otherwise. Ordering is decided by the sequence number the bid gate
/// hands out, which is generated inside Redis under the same atomic step that validates the bid, so
/// two bids in the same millisecond still receive a total order that both dealers can be shown.
/// </para>
/// </remarks>
public sealed class AuctionLot : AggregateRoot, IAuditable, ISoftDeletable
{
    private AuctionLot(Guid id, Guid auctionId, int lotNumber, LotSpecification specification, AuctionSettings settings)
        : base(id)
    {
        AuctionId = auctionId;
        LotNumber = lotNumber;
        VehicleId = specification.VehicleId;
        ListingId = specification.ListingId;
        SellerId = specification.SellerId;
        SellerType = specification.SellerType;
        StartPrice = specification.StartPrice;
        ReservePrice = specification.ReservePrice;
        BuyNowPrice = specification.BuyNowPrice;
        OpensAt = specification.OpensAt;
        ClosesAt = specification.ClosesAt;
        ScheduledCloseAt = specification.ClosesAt;
        Settings = settings;
        WinPricingScope = specification.WinPricingScope;
        CityCode = specification.CityCode;
        StateCode = specification.StateCode;
    }

    private AuctionLot() { }

    public Guid AuctionId { get; private set; }

    /// <summary>Position in the catalogue. What an auctioneer and a dealer both say out loud.</summary>
    public int LotNumber { get; private set; }

    public Guid VehicleId { get; private set; }

    public Guid? ListingId { get; private set; }

    public Guid SellerId { get; private set; }

    public string SellerType { get; private set; } = string.Empty;

    /// <summary>The floor. The first bid must meet it exactly; there is no increment below it.</summary>
    public Money StartPrice { get; private set; }

    /// <summary>
    /// The price below which the seller is not obliged to sell. Never shown to bidders.
    /// </summary>
    /// <remarks>
    /// Hidden rather than absent: bidders are told whether the reserve is met, never what it is.
    /// Publishing it turns the auction into a fixed-price sale at the reserve, because nobody bids
    /// past a number they can see.
    /// </remarks>
    public Money? ReservePrice { get; private set; }

    public Money? BuyNowPrice { get; private set; }

    public DateTimeOffset OpensAt { get; private set; }

    /// <summary>The close as it stands now, including every anti-snipe extension applied so far.</summary>
    public DateTimeOffset ClosesAt { get; private set; }

    /// <summary>The close the catalogue advertised. Kept so "why did it run late?" is answerable.</summary>
    public DateTimeOffset ScheduledCloseAt { get; private set; }

    public int ExtensionCount { get; private set; }

    /// <summary>The terms, copied from the auction so the bid path needs no join.</summary>
    public AuctionSettings Settings { get; private set; } = null!;

    public string WinPricingScope { get; private set; } = string.Empty;

    public string? CityCode { get; private set; }

    public string? StateCode { get; private set; }

    /// <summary>The standing price. Null until the first bid.</summary>
    public Money? HighBidAmount { get; private set; }

    public Guid? HighBidderId { get; private set; }

    /// <summary>
    /// Monotonic count of accepted bids, assigned by the gate.
    /// </summary>
    /// <remarks>
    /// The tie-break, and the replay cursor for the socket. Two bids that arrive in the same
    /// millisecond receive different sequence numbers because the gate assigns them inside one
    /// atomic step, so "who was first" has an answer that does not depend on either client's clock
    /// or on which API node handled the request.
    /// </remarks>
    public long BidSequence { get; private set; }

    public int BidCount { get; private set; }

    public Guid? WinningBidId { get; private set; }

    public Guid? WinnerId { get; private set; }

    public Money? WinningAmount { get; private set; }

    /// <summary>The deal opened for the winner. Null until the lot closes sold.</summary>
    public Guid? DealId { get; private set; }

    /// <summary>When the winner's payment window expires. Drives the forfeiture timer.</summary>
    public DateTimeOffset? PaymentDueAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>How many times this vehicle has been through an auction. Relist count, for reporting.</summary>
    public int RelistCount { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    /// <summary>Whether the standing price has met the seller's floor. The only reserve fact bidders see.</summary>
    public bool IsReserveMet
        => ReservePrice is not { } reserve || (HighBidAmount is { } high && high >= reserve);

    internal static AuctionLot Create(Auction auction, int lotNumber, LotSpecification specification)
        => new(UuidV7.New(), auction.Id, lotNumber, specification, auction.Settings);

    /// <summary>
    /// Records an outcome the gate has already decided.
    /// </summary>
    /// <remarks>
    /// The aggregate does not re-validate the bid, and must not. The gate's decision was made inside
    /// a single atomic step against shared state; re-deciding it here, against a row read a
    /// millisecond later, is how the two disagree — and when they disagree, the version that told the
    /// dealer they were winning is the one they will quote back.
    /// </remarks>
    public void ApplyAcceptedBid(BidAdmission admission)
    {
        HighBidAmount = admission.StandingAmount;
        HighBidderId = admission.StandingBidderId;
        BidSequence = admission.Sequence;
        BidCount++;

        if (admission.ExtendedTo is { } extendedTo)
        {
            ClosesAt = extendedTo;
            ExtensionCount = admission.ExtensionCount;

            Raise(new LotExtended(Id, admission.PlacedAt, extendedTo, ExtensionCount));
        }

        Raise(new BidPlaced(
            Id,
            admission.PlacedAt,
            admission.Sequence,
            admission.StandingAmount.MinorUnits,
            admission.StandingAmount.Currency.Code,
            admission.StandingBidderId,
            IsReserveMet));
    }

    /// <summary>
    /// Closes the lot as sold, opening the winner's payment window.
    /// </summary>
    /// <remarks>
    /// The window is set from the terms rather than from a constant, and it is stored rather than
    /// computed on read, so that extending one winner's window by hand — which operations does, for
    /// a dealer whose bank is down — is a field update and not a special case in a query.
    /// </remarks>
    public Result Award(Guid winningBidId, Guid winnerId, Money amount, DateTimeOffset now)
    {
        if (WinningBidId is not null)
        {
            return Result.Success();
        }

        if (ReservePrice is { } reserve && amount < reserve)
        {
            return Error.Rule(
                "auction.lot.reserve_not_met",
                "This lot cannot be awarded below its reserve.")
                .With("lotId", Id);
        }

        WinningBidId = winningBidId;
        WinnerId = winnerId;
        WinningAmount = amount;
        ClosedAt = now;
        PaymentDueAt = now.AddMinutes(Settings.PaymentWindowMinutes);

        Raise(new LotSold(Id, now, winnerId, amount.MinorUnits, amount.Currency.Code, PaymentDueAt.Value));

        return Result.Success();
    }

    public Result CloseUnsold(DateTimeOffset now, string reasonCode)
    {
        if (ClosedAt is not null)
        {
            return Result.Success();
        }

        ClosedAt = now;
        Raise(new LotUnsold(Id, now, reasonCode, HighBidAmount?.MinorUnits));

        return Result.Success();
    }

    /// <summary>Records the deal the win settles through. Set once, by the closing handler.</summary>
    public void AttachDeal(Guid dealId) => DealId ??= dealId;

    /// <summary>
    /// Forfeits a win whose payment window expired, and prepares the lot to be relisted.
    /// </summary>
    /// <remarks>
    /// Clearing the winner but keeping <see cref="RelistCount"/> is what lets the same lot go round
    /// again while staying identifiable — a vehicle on its third pass is a vehicle whose reserve is
    /// wrong, and that is a report the auction team runs weekly.
    /// </remarks>
    public Result Forfeit(DateTimeOffset now, string reasonCode)
    {
        if (WinningBidId is null)
        {
            return Error.Rule("auction.lot.no_win_to_forfeit", "This lot has no win to forfeit.");
        }

        var forfeitedWinner = WinnerId;
        var forfeitedAmount = WinningAmount;

        WinningBidId = null;
        WinnerId = null;
        WinningAmount = null;
        DealId = null;
        PaymentDueAt = null;
        RelistCount++;

        Raise(new LotForfeited(
            Id, now, forfeitedWinner ?? Guid.Empty, forfeitedAmount?.MinorUnits ?? 0, reasonCode));

        return Result.Success();
    }

    /// <summary>Reopens a forfeited or unsold lot in a later auction, at a new window.</summary>
    public Result Relist(DateTimeOffset opensAt, DateTimeOffset closesAt, Money? startPrice, Money? reservePrice)
    {
        if (closesAt <= opensAt)
        {
            return Error.Validation("auction.lot.window_inverted", "A lot must close after it opens.");
        }

        OpensAt = opensAt;
        ClosesAt = closesAt;
        ScheduledCloseAt = closesAt;
        ExtensionCount = 0;
        HighBidAmount = null;
        HighBidderId = null;
        BidSequence = 0;
        BidCount = 0;
        ClosedAt = null;

        if (startPrice is { } start)
        {
            StartPrice = start;
        }

        if (reservePrice is { } reserve)
        {
            ReservePrice = reserve;
        }

        return Result.Success();
    }
}

public sealed record LotSpecification
{
    public required Guid VehicleId { get; init; }

    public required Guid SellerId { get; init; }

    public required string SellerType { get; init; }

    public required Money StartPrice { get; init; }

    public required DateTimeOffset OpensAt { get; init; }

    public required DateTimeOffset ClosesAt { get; init; }

    public required string WinPricingScope { get; init; }

    public Guid? ListingId { get; init; }

    public Money? ReservePrice { get; init; }

    public Money? BuyNowPrice { get; init; }

    public string? CityCode { get; init; }

    public string? StateCode { get; init; }
}
