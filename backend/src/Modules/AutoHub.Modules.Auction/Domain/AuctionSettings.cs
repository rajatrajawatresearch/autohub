using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Modules.Auction.Domain;

/// <summary>
/// How one kind of auction behaves — configuration, not a subclass.
/// </summary>
/// <remarks>
/// <b>Why there is no <c>TimedAuction : Auction</c>.</b> The brief lists six types — open, hidden,
/// timed, instant, reserve, buy-now — and they are not six things. They are five independent
/// switches, and real auctions combine them freely: a timed sealed-bid tender with a reserve, an
/// open live auction with buy-now, an instant-award B2B liquidation with a hidden floor. Six
/// subclasses cannot express a combination, so the seventh requirement forces either a rewrite or a
/// class per combination.
/// <para>
/// As flags and numbers on a row, "hidden bidding, no proxy, 90-second anti-snipe, three extensions
/// maximum, Pune only" is a profile an operations user creates on a Tuesday afternoon. Nothing in
/// this module branches on <see cref="TypeCode"/>; every behavioural decision reads one of these
/// fields.
/// </para>
/// </remarks>
public sealed record AuctionSettings
{
    /// <summary>Whether other bidders can see the standing price. False makes it a sealed-bid tender.</summary>
    public required bool BidsVisible { get; init; }

    /// <summary>Whether the standing bidder is identified. False still shows a stable mask.</summary>
    public required bool BidderIdentityVisible { get; init; }

    /// <summary>Whether a bidder may leave a ceiling and let the platform bid up to it for them.</summary>
    public required bool ProxyBiddingAllowed { get; init; }

    public required bool BuyNowAllowed { get; init; }

    /// <summary>Whether a lot must carry a reserve. Reserve is still hidden when it is required.</summary>
    public required bool ReserveRequired { get; init; }

    /// <summary>
    /// A bid landing within this many seconds of the close extends the lot.
    /// </summary>
    /// <remarks>
    /// Zero disables anti-snipe, which is right for an instant-award sale and wrong for everything
    /// else. Getting the number from a profile rather than a constant is what lets a category with
    /// slow bidders — commercial vehicles, where a buyer phones their manager — run a five-minute
    /// window while cars run ninety seconds.
    /// </remarks>
    public required int AntiSnipeWindowSeconds { get; init; }

    /// <summary>How far the close moves when a bid lands inside the window.</summary>
    public required int AntiSnipeExtensionSeconds { get; init; }

    /// <summary>
    /// A cap on extensions, because an unbounded auction is an operational problem.
    /// </summary>
    /// <remarks>
    /// Two determined dealers will extend a lot for hours. The cap ends it, and the number is data
    /// because the right answer differs between a ₹40,000 scooter and a ₹40,00,000 excavator.
    /// </remarks>
    public required int MaxExtensions { get; init; }

    /// <summary>
    /// The pricing registry scope that supplies the minimum bid step.
    /// </summary>
    /// <remarks>
    /// A bid increment ladder — "₹1,000 up to ₹1 lakh, ₹2,500 to ₹5 lakh, ₹5,000 above" — is exactly
    /// a slab table, which the pricing registry already evaluates, explains and versions. Reusing it
    /// means the ladder is editable, auditable and previewable by the same people who change
    /// commission, instead of being a constant in a bidding service.
    /// </remarks>
    public required string BidIncrementScope { get; init; }

    /// <summary>How long a winner has to pay before the lot is forfeited. Drives a workflow timer.</summary>
    public required int PaymentWindowMinutes { get; init; }

    /// <summary>
    /// Whether the first bid at or above the floor wins immediately.
    /// </summary>
    /// <remarks>
    /// The "instant" type, expressed without a subclass: the lot closes on the first qualifying bid
    /// rather than at a time. Combined with <see cref="BidsVisible"/> false, it is a sealed instant
    /// sale; combined with a reserve, it is a "meet the price and it is yours" listing.
    /// </remarks>
    public required bool AwardsInstantly { get; init; }
}

/// <summary>
/// A named, operator-editable auction behaviour profile. The registry row behind an auction's type.
/// </summary>
/// <remarks>
/// Versioned by copy rather than by reference: an auction takes a snapshot of the profile when it is
/// created and runs on that snapshot forever. Editing the profile mid-auction would change the terms
/// dealers bid under, which is the one thing an auction platform cannot do — the same reason a
/// published workflow definition is immutable.
/// </remarks>
public sealed class AuctionTypeProfile : Entity, IAuditable
{
    private AuctionTypeProfile(Guid id, string typeCode, string name, AuctionSettings settings)
        : base(id)
    {
        TypeCode = typeCode;
        Name = name;
        Settings = settings;
    }

    private AuctionTypeProfile() { }

    /// <summary>Taxonomy code, e.g. <c>timed_reserve</c>, <c>sealed_tender</c>, <c>instant_b2b</c>.</summary>
    public string TypeCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public AuctionSettings Settings { get; private set; } = null!;

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static AuctionTypeProfile Define(
        string typeCode,
        string name,
        AuctionSettings settings,
        string? description = null)
        => new(UuidV7.New(), typeCode, name, settings) { Description = description };
}

/// <summary>Loads auction type profiles. Implemented in persistence, read through the registry cache.</summary>
public interface IAuctionTypeProfileStore
{
    Task<AuctionTypeProfile?> GetAsync(string typeCode, CancellationToken cancellationToken);
}
