using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Payments.Domain;

/// <summary>
/// A dealer's or customer's balance with the platform — a projection of the ledger, not the truth.
/// </summary>
/// <remarks>
/// <b>Read that again, because it is the distinction the whole module turns on.</b> The authoritative
/// answer to "what do we owe this dealer?" is <c>SUM</c> over <c>ledger_entries</c> against their
/// <c>dealer_wallet</c> account. This row exists because that sum is expensive to compute on every
/// wallet screen and every payout eligibility check, and for no other reason.
/// <para>
/// It is therefore updated <b>only</b> in the same transaction as the journal that caused the
/// movement, never independently, and <see cref="LedgerVersion"/> records the last journal it
/// reflects. Reconciliation recomputes the sum and compares; a mismatch is an incident, and the
/// remedy is always to rebuild this row from the entries, never the reverse.
/// </para>
/// <para>
/// <b><see cref="Available"/> and <see cref="Held"/> are two different kinds of fact and are
/// maintained by two different code paths, deliberately.</b> <see cref="Available"/> mirrors the
/// ledger and moves only when a journal posts. <see cref="Held"/> is a claim staked by a payout batch
/// that has been assembled but not yet instructed — at that moment nothing has happened in the
/// ledger, because nothing has happened in the world, and posting a journal for an intention would
/// put a fiction in the accounts. Without the hold, two batches assembled thirty seconds apart both
/// see the same balance and the bank sends the money twice.
/// </para>
/// </remarks>
public sealed class Wallet : AggregateRoot, IAuditable
{
    private Wallet(Guid id, string ownerType, Guid ownerId, string accountCode, Currency currency)
        : base(id)
    {
        OwnerType = ownerType;
        OwnerId = ownerId;
        AccountCode = accountCode;
        Currency = currency;
        Available = Money.Zero(currency);
        Held = Money.Zero(currency);
    }

    private Wallet() { }

    /// <summary>e.g. <c>identity.dealer</c>. Which module owns <see cref="OwnerId"/>.</summary>
    public string OwnerType { get; private set; } = string.Empty;

    public Guid OwnerId { get; private set; }

    /// <summary>The ledger account this projects — <c>dealer_wallet</c> or <c>customer_wallet</c>.</summary>
    public string AccountCode { get; private set; } = string.Empty;

    public Currency Currency { get; private set; }

    /// <summary>The ledger balance of the owner's wallet account. Moves only with a journal.</summary>
    public Money Available { get; private set; }

    /// <summary>Claimed by a payout batch that has been assembled but not yet instructed.</summary>
    public Money Held { get; private set; }

    /// <summary>What the owner can actually take out right now.</summary>
    public Money Withdrawable => Available - Held;

    /// <summary>The last journal reflected in <see cref="Available"/>. Reconciliation replays from here.</summary>
    public Guid? LedgerVersion { get; private set; }

    /// <summary>Blocks payouts without blocking postings — a fraud review must not stop a refund landing.</summary>
    public bool IsFrozen { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static Wallet Open(string ownerType, Guid ownerId, string accountCode, Currency currency)
        => new(UuidV7.New(), ownerType, ownerId, accountCode, currency);

    /// <summary>Applies a credit entry that has just been posted. Called by the projector, never by a handler.</summary>
    public Result Credit(Money amount, Guid journalId)
    {
        if (!amount.IsPositive)
        {
            return Error.Validation("wallet.credit_not_positive", "A credit must be positive.");
        }

        if (amount.Currency != Currency)
        {
            return CurrencyMismatch(amount);
        }

        Available += amount;
        LedgerVersion = journalId;
        return Result.Success();
    }

    /// <summary>
    /// Applies a debit entry that has just been posted.
    /// </summary>
    /// <remarks>
    /// Refuses to take the balance negative. A wallet that can go negative is the platform lending
    /// money it never agreed to lend, and the loan is discovered at payout time when the dealer has
    /// already spent it.
    /// </remarks>
    public Result Debit(Money amount, Guid journalId)
    {
        if (!amount.IsPositive)
        {
            return Error.Validation("wallet.debit_not_positive", "A debit must be positive.");
        }

        if (amount.Currency != Currency)
        {
            return CurrencyMismatch(amount);
        }

        if (amount > Available)
        {
            return Error.Rule(
                "wallet.insufficient_balance",
                "That would take the wallet below zero.")
                .With("availableMinorUnits", Available.MinorUnits)
                .With("requestedMinorUnits", amount.MinorUnits);
        }

        Available -= amount;
        LedgerVersion = journalId;
        return Result.Success();
    }

    /// <summary>
    /// Stakes a claim on part of the balance while a payout batch is being assembled.
    /// </summary>
    /// <remarks>
    /// Checked against <see cref="Withdrawable"/> rather than <see cref="Available"/>. A dealer with
    /// ₹50,000 already claimed by an unsent batch has ₹50,000 of balance and nothing to withdraw, and
    /// a check against the balance would let a second batch claim the same money.
    /// </remarks>
    public Result Hold(Money amount)
    {
        if (IsFrozen)
        {
            return Error.Rule("wallet.frozen", "This wallet is frozen and cannot be paid out.");
        }

        if (!amount.IsPositive)
        {
            return Error.Validation("wallet.hold_not_positive", "A hold must be positive.");
        }

        if (amount.Currency != Currency)
        {
            return CurrencyMismatch(amount);
        }

        if (amount > Withdrawable)
        {
            return Error.Rule(
                "wallet.insufficient_withdrawable",
                "There is not enough unclaimed balance in this wallet.")
                .With("withdrawableMinorUnits", Withdrawable.MinorUnits)
                .With("requestedMinorUnits", amount.MinorUnits);
        }

        Held += amount;
        return Result.Success();
    }

    /// <summary>
    /// Drops a claim.
    /// </summary>
    /// <remarks>
    /// Called from two opposite places, which is why it is one method. When a batch is instructed the
    /// journal debits the wallet, so the claim has become a real movement and must not be counted
    /// twice. When a batch is cancelled the claim simply never became anything. Both leave
    /// <see cref="Withdrawable"/> correct.
    /// </remarks>
    public Result ReleaseHold(Money amount)
    {
        if (amount.Currency != Currency)
        {
            return CurrencyMismatch(amount);
        }

        if (amount > Held)
        {
            return Error.Rule(
                "wallet.hold_exceeded",
                "Releasing more than is held would invent money.")
                .With("heldMinorUnits", Held.MinorUnits);
        }

        Held -= amount;
        return Result.Success();
    }

    /// <summary>
    /// Overwrites the projection from the ledger. The only sanctioned way to change it out of band.
    /// </summary>
    /// <remarks>
    /// Called by reconciliation when the recomputed sum disagrees. The direction is always ledger to
    /// wallet and never the reverse — a repair that adjusted the ledger to match a wallet would be a
    /// repair that decided the cache was right.
    /// </remarks>
    public void RebuildFromLedger(Money available, Guid? journalId)
    {
        Available = available;
        LedgerVersion = journalId;
    }

    public void Freeze() => IsFrozen = true;

    public void Unfreeze() => IsFrozen = false;

    private Error CurrencyMismatch(Money amount)
        => Error.Validation("wallet.currency_mismatch", "That amount is in a different currency to this wallet.")
            .With("walletCurrency", Currency.Code)
            .With("amountCurrency", amount.Currency.Code);
}
