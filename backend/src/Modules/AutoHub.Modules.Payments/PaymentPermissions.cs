using AutoHub.Platform.Access;

namespace AutoHub.Modules.Payments;

/// <summary>The permissions the payments module checks. Which roles hold them is data.</summary>
public static class PaymentPermissions
{
    public const string LedgerView = "payment.ledger.view";

    /// <summary>Posting a journal by hand. The most dangerous capability in the product.</summary>
    public const string LedgerPost = "payment.ledger.post";

    public const string WalletView = "payment.wallet.view";

    public const string WalletFreeze = "payment.wallet.freeze";

    public const string RefundIssue = "payment.refund.issue";

    public const string PayoutApprove = "payment.payout.approve";

    public const string PayoutInstruct = "payment.payout.instruct";

    public const string ReconciliationRun = "payment.reconciliation.run";
}

/// <summary>Declares <see cref="PaymentPermissions"/> to the RBAC catalog.</summary>
public sealed class PaymentPermissionContributor : IPermissionContributor
{
    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        PermissionDefinition.Declare(
            PaymentPermissions.LedgerView, "View the ledger",
            description: "Read journals, entries and account balances."),

        PermissionDefinition.Declare(
            PaymentPermissions.LedgerPost, "Post a manual journal", isDangerous: true,
            description: "Write a balanced journal by hand. Every use is audited and alerted on."),

        PermissionDefinition.Declare(
            PaymentPermissions.WalletView, "View wallets",
            description: "Read dealer and customer balances."),

        PermissionDefinition.Declare(
            PaymentPermissions.WalletFreeze, "Freeze a wallet", isDangerous: true,
            description: "Block payouts from a wallet during a fraud review."),

        PermissionDefinition.Declare(
            PaymentPermissions.RefundIssue, "Issue a refund", isDangerous: true,
            description: "Return money to a payer and post the reversing journal."),

        PermissionDefinition.Declare(
            PaymentPermissions.PayoutApprove, "Approve a payout batch", isDangerous: true,
            description: "Authorise a batch of payouts before it reaches the bank."),

        PermissionDefinition.Declare(
            PaymentPermissions.PayoutInstruct, "Instruct a payout batch", isDangerous: true,
            description: "Send an approved batch to the bank."),

        PermissionDefinition.Declare(
            PaymentPermissions.ReconciliationRun, "Run reconciliation",
            description: "Compare a provider's settlement file against the ledger."),
    ];
}
