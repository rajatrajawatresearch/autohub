using AutoHub.Platform.Access;

namespace AutoHub.Modules.Deals;

/// <summary>The permissions the deals module checks. Which roles hold them is data.</summary>
public static class DealPermissions
{
    public const string Open = "deal.open";

    public const string View = "deal.view";

    public const string Quote = "deal.quote";

    public const string Accept = "deal.accept";

    public const string Cancel = "deal.cancel";

    /// <summary>
    /// Moving money. Held by the settlement service and by finance operations, never by a customer.
    /// </summary>
    public const string Settle = "deal.settle";

    public const string PartyManage = "deal.party.manage";
}

/// <summary>Declares <see cref="DealPermissions"/> to the RBAC catalog.</summary>
public sealed class DealPermissionContributor : IPermissionContributor
{
    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        PermissionDefinition.Declare(
            DealPermissions.Open, "Open a deal",
            description: "Create a settlement record for a sale, auction win, rental, lease or attachment."),

        PermissionDefinition.Declare(
            DealPermissions.View, "View a deal",
            description: "Read a deal, its parties, its priced lines and its lifecycle."),

        PermissionDefinition.Declare(
            DealPermissions.Quote, "Price a deal",
            description: "Ask the pricing registry for this deal's fees and record the quote."),

        PermissionDefinition.Declare(
            DealPermissions.Accept, "Accept a deal",
            description: "Record a party's acceptance and move the deal forward."),

        PermissionDefinition.Declare(
            DealPermissions.Cancel, "Cancel a deal",
            description: "Cancel a deal that has not yet settled. Always requires a reason."),

        PermissionDefinition.Declare(
            DealPermissions.Settle, "Settle a deal", isDangerous: true,
            description: "Close a deal and trigger the ledger posting that moves the money."),

        PermissionDefinition.Declare(
            DealPermissions.PartyManage, "Manage deal parties",
            description: "Attach or amend the counterparties on a deal."),
    ];
}
