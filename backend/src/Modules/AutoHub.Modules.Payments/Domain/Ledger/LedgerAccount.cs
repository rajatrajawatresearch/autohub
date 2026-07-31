using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Payments.Domain.Ledger;

/// <summary>
/// One account in the chart of accounts.
/// </summary>
/// <remarks>
/// Accounts are rows, not code. A finance team that wants to split <c>platform_revenue</c> into
/// auction, listing and referral revenue should be able to, and every posting rule that references
/// them is data too — see <see cref="PostingTemplate"/>.
/// <para>
/// <b>Owner-scoped accounts.</b> <c>dealer_wallet</c> is a control account: the platform owes money
/// to eight thousand dealers, and "how much do we owe dealer X" must be a query, not a scan. So a
/// wallet account exists per owner, sharing the parent's <see cref="Code"/> and carrying an
/// <see cref="OwnerId"/>. The control account's balance is the sum of its children, which is the
/// check reconciliation runs.
/// </para>
/// </remarks>
public sealed class LedgerAccount : AggregateRoot, IAuditable
{
    private LedgerAccount(Guid id, string code, string name, LedgerAccountType type, Currency currency)
        : base(id)
    {
        Code = code;
        Name = name;
        Type = type;
        Currency = currency;
    }

    private LedgerAccount() { }

    /// <summary>e.g. <c>escrow_liability</c>. Shared by every owner-scoped instance of the account.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public LedgerAccountType Type { get; private set; }

    /// <summary>
    /// An account holds exactly one currency, always.
    /// </summary>
    /// <remarks>
    /// A multi-currency account is a mixed-unit sum waiting to be taken. When the export marketplace
    /// arrives, AED escrow is a second account, not a second column.
    /// </remarks>
    public Currency Currency { get; private set; }

    /// <summary>Which module owns <see cref="OwnerId"/>, e.g. <c>identity.dealer</c>. Null on a control account.</summary>
    public string? OwnerType { get; private set; }

    public Guid? OwnerId { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Whether this account may go the wrong way for its type.
    /// </summary>
    /// <remarks>
    /// A dealer wallet must never go negative — that would be the platform lending money it never
    /// agreed to lend, discovered at payout time. <c>gateway_clearing</c>, by contrast, legitimately
    /// swings both ways between a capture and its settlement file. The flag is per account because
    /// which of the two an account is is a finance decision, not a code one.
    /// </remarks>
    public bool AllowsContraBalance { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static LedgerAccount Control(
        string code,
        string name,
        LedgerAccountType type,
        Currency currency,
        bool allowsContraBalance = false)
        => new(UuidV7.New(), code, name, type, currency) { AllowsContraBalance = allowsContraBalance };

    public static LedgerAccount ForOwner(
        LedgerAccount control,
        string ownerType,
        Guid ownerId,
        string name)
        => new(UuidV7.New(), control.Code, name, control.Type, control.Currency)
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            AllowsContraBalance = control.AllowsContraBalance,
        };

    /// <summary>
    /// The direction that increases this account, from the accounting equation.
    /// </summary>
    /// <remarks>
    /// Assets and expenses increase on the debit side; liabilities, income and equity on the credit
    /// side. Stated once here so no posting rule has to remember it and no report can disagree with
    /// the ledger about which way a number points.
    /// </remarks>
    public LedgerDirection NormalBalance => Type switch
    {
        LedgerAccountType.Asset or LedgerAccountType.Expense => LedgerDirection.Debit,
        _ => LedgerDirection.Credit,
    };

    public Result EnsurePostable()
        => IsActive
            ? Result.Success()
            : Error.Rule("ledger.account_inactive", "That ledger account has been closed.")
                .With("accountCode", Code);
}

/// <summary>
/// The five account classes of double-entry bookkeeping.
/// </summary>
/// <remarks>
/// This is an enum rather than registry data because it is not configuration — it is the definition
/// of the technique. Nobody will ever want a sixth class, and the direction that increases each one
/// is not a business preference. What <i>is</i> configuration is which accounts exist and what they
/// are called, and those are rows.
/// </remarks>
public enum LedgerAccountType
{
    Asset = 1,
    Liability = 2,
    Income = 3,
    Expense = 4,
    Equity = 5,
}

public enum LedgerDirection
{
    Debit = 1,
    Credit = 2,
}

/// <summary>
/// The account codes that a line of code in this module names.
/// </summary>
/// <remarks>
/// The same boundary <c>IntegrationCapabilities</c> draws: the rows are the truth and carry the
/// name, the description and the active flag an operator edits; these constants are the compile-time
/// half, and they exist so that a typo in a posting rule is a build error rather than a journal that
/// silently fails to balance at 2am.
/// <para>
/// <b>Two accounts are here that the brief did not list, and both are load-bearing.</b>
/// <see cref="BankSettlement"/> is where money actually is once it has left the gateway and reached
/// our bank — without it a payout has nowhere to be credited from and the books never close.
/// <see cref="PartnerIncentive"/> is an expense: when a bank's referral fee is shared with the dealer
/// who sourced the lead, that share is a cost we incur, not revenue we did not earn. Posting it as a
/// debit to revenue would understate both gross revenue and cost of acquisition, which is exactly
/// the pair of numbers the business is run on.
/// </para>
/// </remarks>
public static class LedgerAccounts
{
    /// <summary>Asset. Money captured at the payment gateway that has not yet reached our bank.</summary>
    public const string GatewayClearing = "gateway_clearing";

    /// <summary>Asset. Where money is after the gateway settles it to us.</summary>
    public const string BankSettlement = "bank_settlement";

    /// <summary>
    /// Liability. Money we are holding that is not ours.
    /// </summary>
    /// <remarks>
    /// A buyer's payment before delivery is confirmed, a renter's security deposit, an auction
    /// winner's principal before the consignor is paid. Booking any of it as revenue on the day it
    /// arrives is the single most common way a marketplace overstates a quarter.
    /// </remarks>
    public const string EscrowLiability = "escrow_liability";

    /// <summary>Income. What the platform actually earned.</summary>
    public const string PlatformRevenue = "platform_revenue";

    /// <summary>
    /// Liability. Output GST collected on our behalf of the government.
    /// </summary>
    /// <remarks>
    /// Never revenue, not for one minute. It is collected from the customer and owed onward, and the
    /// GSTR-1 filed at month end is reconciled against this account's movements.
    /// </remarks>
    public const string GstPayable = "gst_payable";

    /// <summary>Asset. Commission invoiced to a dealer and not yet collected.</summary>
    public const string CommissionReceivable = "commission_receivable";

    /// <summary>Liability, owner-scoped. What we owe one dealer.</summary>
    public const string DealerWallet = "dealer_wallet";

    /// <summary>Liability, owner-scoped. What we owe one customer — refunds, released deposits.</summary>
    public const string CustomerWallet = "customer_wallet";

    /// <summary>Liability. Payouts instructed to the bank and not yet confirmed as debited.</summary>
    public const string PayoutPending = "payout_pending";

    /// <summary>Expense. Referral and incentive shares paid out to partners and dealers.</summary>
    public const string PartnerIncentive = "partner_incentive";

    public static IReadOnlyList<string> All { get; } =
    [
        GatewayClearing, BankSettlement, EscrowLiability, PlatformRevenue, GstPayable,
        CommissionReceivable, DealerWallet, CustomerWallet, PayoutPending, PartnerIncentive,
    ];

    /// <summary>The accounts that exist once per owner rather than once for the platform.</summary>
    public static IReadOnlySet<string> OwnerScoped { get; } =
        new HashSet<string>(StringComparer.Ordinal) { DealerWallet, CustomerWallet };
}
