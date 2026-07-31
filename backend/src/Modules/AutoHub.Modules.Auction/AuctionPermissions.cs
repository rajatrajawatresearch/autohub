using AutoHub.Platform.Access;

namespace AutoHub.Modules.Auction;

/// <summary>The permissions the auction module checks. Which roles hold them, and where, is data.</summary>
/// <remarks>
/// <see cref="BidPlace"/> is the one to look at. It is scoped in the RBAC registry, so "may bid in
/// the West region wholesale auction" is an assignment with a scope rather than an invitee list
/// compared in code — which is what makes "Regional Auction Moderator — West" a role somebody
/// creates in the admin panel.
/// </remarks>
public static class AuctionPermissions
{
    public const string AuctionSchedule = "auction.schedule";

    public const string LotManage = "auction.lot.manage";

    public const string LotClose = "auction.lot.close";

    public const string BidPlace = "auction.bid.place";

    /// <summary>Withdrawing a bid. Rare, always manual, always audited — it changes who won.</summary>
    public const string BidRetract = "auction.bid.retract";

    /// <summary>Seeing the reserve. Held by the auction team and the consignor, never by bidders.</summary>
    public const string ReserveView = "auction.reserve.view";

    public const string LotRelist = "auction.lot.relist";
}

/// <summary>Declares <see cref="AuctionPermissions"/> to the RBAC catalog.</summary>
public sealed class AuctionPermissionContributor : IPermissionContributor
{
    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        PermissionDefinition.Declare(
            AuctionPermissions.AuctionSchedule, "Schedule an auction",
            description: "Create a sale event and set its terms."),

        PermissionDefinition.Declare(
            AuctionPermissions.LotManage, "Manage lots",
            description: "Add, edit and withdraw lots before bidding opens."),

        PermissionDefinition.Declare(
            AuctionPermissions.LotClose, "Close a lot", isDangerous: true,
            description: "End bidding and decide the winner. Normally the scheduler's job."),

        PermissionDefinition.Declare(
            AuctionPermissions.BidPlace, "Place a bid",
            description: "Bid in an auction. Scoped so invite-only sales are a role assignment."),

        PermissionDefinition.Declare(
            AuctionPermissions.BidRetract, "Retract a bid", isDangerous: true,
            description: "Withdraw a placed bid. Changes who is winning, so always audited."),

        PermissionDefinition.Declare(
            AuctionPermissions.ReserveView, "View reserves",
            description: "See the hidden reserve on a lot."),

        PermissionDefinition.Declare(
            AuctionPermissions.LotRelist, "Relist a lot",
            description: "Put an unsold or forfeited lot into a later auction."),
    ];
}
