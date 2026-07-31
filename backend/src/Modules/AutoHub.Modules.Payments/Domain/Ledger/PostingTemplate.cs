using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Modules.Payments.Domain.Ledger;

/// <summary>
/// The rule that turns one priced line into one pair of ledger entries.
/// </summary>
/// <remarks>
/// This is what keeps the accounting out of the code. A deal arrives carrying lines the pricing
/// registry produced — <c>principal</c>, <c>auction_commission</c>, <c>gst</c>,
/// <c>security_deposit</c> — and this table says, per scope and per line code, which account is
/// debited and which is credited. Introducing a new fee is then a pricing rule plus a posting
/// template: two rows, no deploy, and no <c>switch</c> on a line code anywhere.
/// <para>
/// <see cref="DebitOwnerRole"/> and <see cref="CreditOwnerRole"/> resolve owner-scoped accounts. A
/// template saying "credit <c>dealer_wallet</c> for the party in role <c>seller</c>" works for every
/// deal without naming a dealer, which is what lets one row serve eight thousand of them.
/// </para>
/// <para>
/// <see cref="ConditionJson"/> is JsonLogic over the same context the pricing rules saw, so a
/// posting can differ by state — a Maharashtra deal crediting a state-specific GST account, for
/// instance — without a second scope.
/// </para>
/// </remarks>
public sealed class PostingTemplate : Entity, IAuditable
{
    private PostingTemplate(
        Guid id,
        string scopeCode,
        string lineCode,
        string debitAccountCode,
        string creditAccountCode)
        : base(id)
    {
        ScopeCode = scopeCode;
        LineCode = lineCode;
        DebitAccountCode = debitAccountCode;
        CreditAccountCode = creditAccountCode;
    }

    private PostingTemplate() { }

    /// <summary>The commercial moment, e.g. <c>deal.collect</c>, <c>deal.settle</c>, <c>refund</c>.</summary>
    public string ScopeCode { get; private set; } = string.Empty;

    /// <summary>The pricing registry line this maps, or <c>principal</c> for the deal's own amount.</summary>
    public string LineCode { get; private set; } = string.Empty;

    public string DebitAccountCode { get; private set; } = string.Empty;

    /// <summary>Party role whose sub-ledger the debit lands in. Null means the control account.</summary>
    public string? DebitOwnerRole { get; private set; }

    public string CreditAccountCode { get; private set; } = string.Empty;

    public string? CreditOwnerRole { get; private set; }

    /// <summary>JsonLogic over the posting context. Null applies unconditionally.</summary>
    public string? ConditionJson { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>Why this posting is what it is. Read by whoever inherits the chart of accounts.</summary>
    public string? Rationale { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static PostingTemplate Define(
        string scopeCode,
        string lineCode,
        string debitAccountCode,
        string creditAccountCode,
        string? debitOwnerRole = null,
        string? creditOwnerRole = null,
        int sortOrder = 0,
        string? conditionJson = null,
        string? rationale = null)
        => new(UuidV7.New(), scopeCode, lineCode, debitAccountCode, creditAccountCode)
        {
            DebitOwnerRole = debitOwnerRole,
            CreditOwnerRole = creditOwnerRole,
            SortOrder = sortOrder,
            ConditionJson = conditionJson,
            Rationale = rationale,
        };
}

/// <summary>
/// Loads posting templates. Implemented in persistence, cached like every other registry read.
/// </summary>
public interface IPostingTemplateStore
{
    Task<IReadOnlyList<PostingTemplate>> GetForScopeAsync(string scopeCode, CancellationToken cancellationToken);
}

/// <summary>
/// The scope codes the ledger posts under.
/// </summary>
/// <remarks>
/// Technical identifiers for the moment being posted, not business configuration: each one exists
/// because a handler in this module posts it. Which accounts each touches is entirely
/// <see cref="PostingTemplate"/> rows.
/// </remarks>
public static class PostingScopes
{
    /// <summary>Money arrives at the gateway against a deal.</summary>
    public const string DealCollect = "deal.collect";

    /// <summary>A deal closes and what was held is released to whoever earned it.</summary>
    public const string DealSettle = "deal.settle";

    /// <summary>Commission invoiced before it is collected — an auction win, a subscription.</summary>
    public const string CommissionInvoice = "commission.invoice";

    /// <summary>A refundable deposit is taken.</summary>
    public const string DepositHold = "deposit.hold";

    /// <summary>A deposit is given back, in whole or in part.</summary>
    public const string DepositRelease = "deposit.release";

    /// <summary>Money goes back to the payer.</summary>
    public const string Refund = "refund";

    /// <summary>A lender or insurer pays us for a referral.</summary>
    public const string ReferralReceipt = "referral.receipt";

    /// <summary>A share of a referral fee is passed to the dealer who sourced it.</summary>
    public const string ReferralShare = "referral.share";

    /// <summary>A payout is instructed to the bank.</summary>
    public const string PayoutInstruct = "payout.instruct";

    /// <summary>The bank confirms the payout left.</summary>
    public const string PayoutConfirm = "payout.confirm";

    /// <summary>The gateway settles its clearing balance into our bank account.</summary>
    public const string GatewaySettlement = "gateway.settlement";
}

/// <summary>
/// The default chart of accounts and posting templates — the worked examples, as data.
/// </summary>
/// <remarks>
/// Seed rows, not behaviour. Everything here is editable by finance the moment it is in the database;
/// it lives in code so that a fresh environment has a working, correct ledger on the first run rather
/// than an empty one that silently posts nothing.
/// <para>
/// <b>The five flows this seeds, worked through. All figures in paise.</b>
/// </para>
/// <para>
/// <b>1 — C2C sale with escrow.</b> Buyer pays ₹5,49,000 principal, ₹5,000 platform fee, ₹900 GST.
/// <code>
/// deal.collect        Dr gateway_clearing      55,490,000
///                        Cr escrow_liability      54,900,000   principal — the seller's money, not ours
///                        Cr platform_revenue         500,000   the fee we actually earned
///                        Cr gst_payable               90,000   collected for the government
/// deal.settle         Dr escrow_liability      54,900,000      delivery confirmed, hold released
///                        Cr dealer_wallet[seller] 54,900,000
/// payout.instruct     Dr dealer_wallet[seller] 54,900,000
///                        Cr payout_pending        54,900,000
/// payout.confirm      Dr payout_pending        54,900,000
///                        Cr bank_settlement       54,900,000
/// </code>
/// Nothing is recognised as revenue on the day the money arrives except the ₹5,000 we earned. The
/// ₹5,49,000 sits as a liability until the vehicle changes hands, which is what it is.
/// </para>
/// <para>
/// <b>2 — Auction win with commission and GST.</b> Hammer price ₹8,47,350, commission 2%, GST 18% on
/// the commission. These are the API contract's own §4 figures.
/// <code>
/// commission.invoice  Dr commission_receivable  1,999,746
///                        Cr platform_revenue       1,694,700   2% of 84,735,000
///                        Cr gst_payable              305,046   18% of the commission, not of the bid
/// deal.collect        Dr gateway_clearing      86,734,746
///                        Cr commission_receivable  1,999,746   the invoice is now paid
///                        Cr escrow_liability      84,735,000   the consignor's money
/// </code>
/// Invoicing before collecting is what makes an unpaid commission visible as a receivable rather than
/// as revenue that never showed up.
/// </para>
/// <para>
/// <b>3 — Rental security deposit, held then released.</b> ₹10,000 deposit, ₹2,000 of damage charged
/// on return.
/// <code>
/// deposit.hold        Dr gateway_clearing       1,000,000
///                        Cr escrow_liability       1,000,000   never revenue, not for one minute
/// deposit.release     Dr escrow_liability       1,000,000
///                        Cr customer_wallet[renter]  800,000   the part being given back
///                        Cr platform_revenue         169,492   damage recovery, net of tax
///                        Cr gst_payable               30,508
/// </code>
/// </para>
/// <para>
/// <b>4 — Refund of the sale in flow 1.</b> A new journal, never an edit or a delete.
/// <code>
/// refund              Dr escrow_liability      54,900,000
///                     Dr platform_revenue         500,000     the fee is given back too
///                     Dr gst_payable               90,000     and so is the tax on it
///                        Cr gateway_clearing      55,490,000
/// </code>
/// The original journal stays exactly where it was. An account statement shows both, which is what
/// lets a dispute be settled by reading rather than by remembering.
/// </para>
/// <para>
/// <b>5 — Loan referral payout.</b> A bank pays ₹8,000 for a funded referral; ₹2,000 is passed to the
/// dealer who sourced it.
/// <code>
/// referral.receipt    Dr gateway_clearing         800,000
///                        Cr platform_revenue         677,966   ₹8,000 is tax-inclusive
///                        Cr gst_payable              122,034
/// referral.share      Dr partner_incentive        200,000     a cost we incur, not revenue we lost
///                        Cr dealer_wallet[dealer]    200,000
/// </code>
/// Posting the dealer's share as an expense rather than as a debit to revenue is what keeps gross
/// revenue and cost of acquisition separately reportable — the two numbers the business is run on.
/// </para>
/// </remarks>
public static class PostingTemplateCatalog
{
    /// <summary>Party role codes the default templates resolve owner-scoped accounts through.</summary>
    /// <remarks>
    /// These are taxonomy values, reproduced here only because the seed rows have to name something.
    /// Nothing in this module compares against them; the resolver looks up whatever role a template
    /// asks for in whatever roles the deal actually carries.
    /// </remarks>
    public static class Roles
    {
        public const string Seller = "seller";

        public const string Buyer = "buyer";

        public const string Renter = "renter";

        public const string Dealer = "dealer";
    }

    public static IReadOnlyList<LedgerAccount> DefaultAccounts(SharedKernel.Money.Currency currency) =>
    [
        LedgerAccount.Control(LedgerAccounts.GatewayClearing, "Gateway clearing", LedgerAccountType.Asset, currency, allowsContraBalance: true),
        LedgerAccount.Control(LedgerAccounts.BankSettlement, "Bank settlement", LedgerAccountType.Asset, currency),
        LedgerAccount.Control(LedgerAccounts.EscrowLiability, "Escrow held", LedgerAccountType.Liability, currency),
        LedgerAccount.Control(LedgerAccounts.PlatformRevenue, "Platform revenue", LedgerAccountType.Income, currency),
        LedgerAccount.Control(LedgerAccounts.GstPayable, "GST payable", LedgerAccountType.Liability, currency),
        LedgerAccount.Control(LedgerAccounts.CommissionReceivable, "Commission receivable", LedgerAccountType.Asset, currency),
        LedgerAccount.Control(LedgerAccounts.DealerWallet, "Dealer wallets", LedgerAccountType.Liability, currency),
        LedgerAccount.Control(LedgerAccounts.CustomerWallet, "Customer wallets", LedgerAccountType.Liability, currency),
        LedgerAccount.Control(LedgerAccounts.PayoutPending, "Payouts in flight", LedgerAccountType.Liability, currency),
        LedgerAccount.Control(LedgerAccounts.PartnerIncentive, "Partner incentives", LedgerAccountType.Expense, currency),
    ];

    public static IReadOnlyList<PostingTemplate> DefaultTemplates() =>
    [
        // ── 1 and 2: money arriving against a deal ───────────────────────────────────────────────
        PostingTemplate.Define(
            PostingScopes.DealCollect, "principal",
            LedgerAccounts.GatewayClearing, LedgerAccounts.EscrowLiability, sortOrder: 10,
            rationale: "The principal belongs to the seller until the deal completes. Holding it as a "
                     + "liability is what stops a quarter being flattered by money we are only carrying."),

        PostingTemplate.Define(
            PostingScopes.DealCollect, "platform_fee",
            LedgerAccounts.GatewayClearing, LedgerAccounts.PlatformRevenue, sortOrder: 20,
            rationale: "The only part of a collection that is genuinely ours."),

        PostingTemplate.Define(
            PostingScopes.DealCollect, "auction_commission",
            LedgerAccounts.GatewayClearing, LedgerAccounts.CommissionReceivable, sortOrder: 30,
            rationale: "Auction commission is invoiced at knock-down, so collecting it clears the "
                     + "receivable rather than recognising revenue a second time."),

        PostingTemplate.Define(
            PostingScopes.DealCollect, "gst",
            LedgerAccounts.GatewayClearing, LedgerAccounts.GstPayable, sortOrder: 40,
            rationale: "Output tax is collected on the government's behalf and owed onward. Never revenue."),

        PostingTemplate.Define(
            PostingScopes.CommissionInvoice, "auction_commission",
            LedgerAccounts.CommissionReceivable, LedgerAccounts.PlatformRevenue, sortOrder: 10,
            rationale: "Recognised when the hammer falls, which is when it was earned — not when it "
                     + "is paid. Unpaid commission then shows as a receivable instead of vanishing."),

        PostingTemplate.Define(
            PostingScopes.CommissionInvoice, "gst",
            LedgerAccounts.CommissionReceivable, LedgerAccounts.GstPayable, sortOrder: 20,
            rationale: "GST liability arises on invoice under Indian law, not on receipt."),

        // ── Releasing what was held ──────────────────────────────────────────────────────────────
        PostingTemplate.Define(
            PostingScopes.DealSettle, "principal",
            LedgerAccounts.EscrowLiability, LedgerAccounts.DealerWallet,
            creditOwnerRole: Roles.Seller, sortOrder: 10,
            rationale: "Delivery is confirmed, so what we were holding becomes what we owe the seller."),

        // ── 3: rental deposits ───────────────────────────────────────────────────────────────────
        PostingTemplate.Define(
            PostingScopes.DepositHold, "security_deposit",
            LedgerAccounts.GatewayClearing, LedgerAccounts.EscrowLiability, sortOrder: 10,
            rationale: "A deposit is the renter's money the whole time we hold it."),

        PostingTemplate.Define(
            PostingScopes.DepositRelease, "security_deposit",
            LedgerAccounts.EscrowLiability, LedgerAccounts.CustomerWallet,
            creditOwnerRole: Roles.Renter, sortOrder: 10,
            rationale: "The refundable part goes back to the renter's wallet, then out on a payout."),

        PostingTemplate.Define(
            PostingScopes.DepositRelease, "damage_recovery",
            LedgerAccounts.EscrowLiability, LedgerAccounts.PlatformRevenue, sortOrder: 20,
            rationale: "The forfeited part is earned, and is taxable, so it is split out rather than "
                     + "netted off the refund."),

        PostingTemplate.Define(
            PostingScopes.DepositRelease, "gst",
            LedgerAccounts.EscrowLiability, LedgerAccounts.GstPayable, sortOrder: 30,
            rationale: "Tax on the forfeited portion."),

        // ── 4: refunds ───────────────────────────────────────────────────────────────────────────
        PostingTemplate.Define(
            PostingScopes.Refund, "principal",
            LedgerAccounts.EscrowLiability, LedgerAccounts.GatewayClearing, sortOrder: 10,
            rationale: "A refund is a new journal in the opposite direction. The original stays."),

        PostingTemplate.Define(
            PostingScopes.Refund, "platform_fee",
            LedgerAccounts.PlatformRevenue, LedgerAccounts.GatewayClearing, sortOrder: 20,
            rationale: "Revenue is given back too, in the period the refund happens, not by restating "
                     + "the period that recognised it."),

        PostingTemplate.Define(
            PostingScopes.Refund, "gst",
            LedgerAccounts.GstPayable, LedgerAccounts.GatewayClearing, sortOrder: 30,
            rationale: "The output tax liability reduces with the refund it was charged on."),

        // ── 5: referral revenue and the share paid on ────────────────────────────────────────────
        PostingTemplate.Define(
            PostingScopes.ReferralReceipt, "referral_fee",
            LedgerAccounts.GatewayClearing, LedgerAccounts.PlatformRevenue, sortOrder: 10,
            rationale: "A lender's referral fee is revenue on receipt; there is nothing held for anyone."),

        PostingTemplate.Define(
            PostingScopes.ReferralReceipt, "gst",
            LedgerAccounts.GatewayClearing, LedgerAccounts.GstPayable, sortOrder: 20,
            rationale: "Referral fees are quoted tax-inclusive, so the tax is separated on receipt."),

        PostingTemplate.Define(
            PostingScopes.ReferralShare, "referral_share",
            LedgerAccounts.PartnerIncentive, LedgerAccounts.DealerWallet,
            creditOwnerRole: Roles.Dealer, sortOrder: 10,
            rationale: "The dealer's share is a cost of acquisition, not a reduction of revenue. "
                     + "Debiting revenue instead would understate both numbers the business runs on."),

        // ── Payouts and gateway settlement ───────────────────────────────────────────────────────
        PostingTemplate.Define(
            PostingScopes.PayoutInstruct, "payout",
            LedgerAccounts.DealerWallet, LedgerAccounts.PayoutPending,
            debitOwnerRole: Roles.Dealer, sortOrder: 10,
            rationale: "Instructing a payout stops it being withdrawable twice while the bank has it."),

        PostingTemplate.Define(
            PostingScopes.PayoutConfirm, "payout",
            LedgerAccounts.PayoutPending, LedgerAccounts.BankSettlement, sortOrder: 10,
            rationale: "The bank confirms the debit, so the money has actually left."),

        PostingTemplate.Define(
            PostingScopes.GatewaySettlement, "settlement",
            LedgerAccounts.BankSettlement, LedgerAccounts.GatewayClearing, sortOrder: 10,
            rationale: "The gateway's payout to us. Clearing returns to zero over a settlement cycle, "
                     + "and a clearing balance that does not is the first sign of a missing capture."),
    ];
}
