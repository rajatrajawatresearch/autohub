using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Auction.Domain;

/// <summary>
/// A sale event: a set of lots that open and close together, under one set of terms.
/// </summary>
/// <remarks>
/// The auction owns the terms; the lot owns the bidding. That split is what keeps the hot path
/// short — a bid touches one lot row and one Redis hash, and never loads the event, its other four
/// hundred lots, or the profile the terms came from.
/// <para>
/// <see cref="Settings"/> is a copy taken from the type profile at creation, not a reference to it.
/// An operator improving the default anti-snipe window must not change the rules of an auction
/// dealers are already bidding in — that is the same immutability argument a published workflow
/// definition makes, and it matters more here because money is on the table in real time.
/// </para>
/// </remarks>
public sealed class Auction : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<AuctionLot> _lots = [];

    private Auction(Guid id, string code, string title, string typeCode, AuctionSettings settings)
        : base(id)
    {
        Code = code;
        Title = title;
        TypeCode = typeCode;
        Settings = settings;
    }

    private Auction() { }

    /// <summary>Operator-facing short code, e.g. <c>PUNE-W34</c>. Printed on the catalogue.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>The profile this auction's terms were taken from. Reporting only; nothing branches on it.</summary>
    public string TypeCode { get; private set; } = string.Empty;

    public AuctionSettings Settings { get; private set; } = null!;

    public string? CityCode { get; private set; }

    public string? StateCode { get; private set; }

    /// <summary>The pricing registry scope that prices a win, e.g. <c>auction.win</c>.</summary>
    public string WinPricingScope { get; private set; } = string.Empty;

    public DateTimeOffset PreviewOpensAt { get; private set; }

    public DateTimeOffset OpensAt { get; private set; }

    /// <summary>When the last lot is scheduled to close, before any anti-snipe extension.</summary>
    public DateTimeOffset ClosesAt { get; private set; }

    /// <summary>
    /// Restricts bidding to invited dealers.
    /// </summary>
    /// <remarks>
    /// Enforced as a permission check against the auction's scope rather than as a list compared in
    /// code, so "who may bid in the West region wholesale auction" is a role assignment an operations
    /// manager makes.
    /// </remarks>
    public bool IsInviteOnly { get; private set; }

    public IReadOnlyList<AuctionLot> Lots => _lots;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<Auction> Schedule(AuctionSpecification specification)
    {
        if (string.IsNullOrWhiteSpace(specification.Code))
        {
            return Error.Validation("auction.code_required", "An auction needs a code.");
        }

        if (specification.ClosesAt <= specification.OpensAt)
        {
            return Error.Validation(
                "auction.window_inverted",
                "An auction must close after it opens.");
        }

        if (specification.PreviewOpensAt > specification.OpensAt)
        {
            return Error.Validation(
                "auction.preview_after_open",
                "Preview cannot start after bidding does.");
        }

        return new Auction(
            UuidV7.New(),
            specification.Code,
            specification.Title,
            specification.TypeCode,
            specification.Settings)
        {
            CityCode = specification.CityCode,
            StateCode = specification.StateCode,
            WinPricingScope = specification.WinPricingScope,
            PreviewOpensAt = specification.PreviewOpensAt,
            OpensAt = specification.OpensAt,
            ClosesAt = specification.ClosesAt,
            IsInviteOnly = specification.IsInviteOnly,
        };
    }

    /// <summary>
    /// Adds a lot, stamping it with this auction's terms.
    /// </summary>
    /// <remarks>
    /// The terms are copied down rather than read up, so the bid path never joins to the auction. At
    /// four hundred lots and several thousand bids a minute, that join is the difference between a
    /// bid costing one row read and costing three.
    /// </remarks>
    public Result<AuctionLot> AddLot(LotSpecification specification)
    {
        if (specification.ClosesAt <= specification.OpensAt)
        {
            return Error.Validation("auction.lot.window_inverted", "A lot must close after it opens.");
        }

        if (Settings.ReserveRequired && specification.ReservePrice is null)
        {
            return Error.Rule(
                "auction.lot.reserve_required",
                "This auction's terms require every lot to carry a reserve.");
        }

        if (specification.ReservePrice is { } reserve && reserve < specification.StartPrice)
        {
            return Error.Validation(
                "auction.lot.reserve_below_start",
                "A reserve below the opening price is a reserve that is always met.");
        }

        if (!Settings.BuyNowAllowed && specification.BuyNowPrice is not null)
        {
            return Error.Rule(
                "auction.lot.buy_now_not_allowed",
                "This auction's terms do not offer buy now.");
        }

        var nextNumber = _lots.Count == 0 ? 1 : _lots.Max(l => l.LotNumber) + 1;

        var lot = AuctionLot.Create(this, nextNumber, specification);
        _lots.Add(lot);

        return lot;
    }
}

public sealed record AuctionSpecification
{
    public required string Code { get; init; }

    public required string Title { get; init; }

    public required string TypeCode { get; init; }

    public required AuctionSettings Settings { get; init; }

    public required string WinPricingScope { get; init; }

    public required DateTimeOffset OpensAt { get; init; }

    public required DateTimeOffset ClosesAt { get; init; }

    public DateTimeOffset PreviewOpensAt { get; init; }

    public string? CityCode { get; init; }

    public string? StateCode { get; init; }

    public bool IsInviteOnly { get; init; }
}
