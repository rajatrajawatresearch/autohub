using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;

namespace AutoHub.Modules.Payments.Domain.Ledger;

/// <summary>
/// One side of one posting. Written once, never updated, never deleted.
/// </summary>
/// <remarks>
/// <b>Immutability is the whole design, not a nicety.</b> A ledger that can be edited is a ledger
/// whose history is whatever the last person to run an UPDATE decided it was, and every property
/// anyone wants from a ledger — that a balance can be re-derived, that a dispute can be adjudicated,
/// that an auditor can trust a report — depends on the rows being append-only. A mistake is
/// corrected by posting its reverse, so the correction is itself visible and dated.
/// <para>
/// That is why this type has no setter, no <c>Update</c>, no <c>UpdatedAt</c> and does not implement
/// <c>IAuditable</c>: there is no second author to record. It is also why it is not
/// <c>ISoftDeletable</c> — a hidden ledger row is a ledger row that stops appearing in the balance
/// while continuing to have happened.
/// </para>
/// <para>
/// <see cref="Amount"/> is always positive and direction is carried separately, rather than signing
/// the amount. Signed amounts make "sum the debits" a filter on a sign convention that two reports
/// will eventually implement differently, and they make a negative debit — which is a credit wearing
/// a disguise — representable.
/// </para>
/// </remarks>
public sealed class LedgerEntry : Entity
{
    private LedgerEntry(
        Guid id,
        Guid journalId,
        Guid accountId,
        string accountCode,
        LedgerDirection direction,
        Money amount,
        int lineNumber)
        : base(id)
    {
        JournalId = journalId;
        AccountId = accountId;
        AccountCode = accountCode;
        Direction = direction;
        Amount = amount;
        LineNumber = lineNumber;
    }

    private LedgerEntry() { }

    public Guid JournalId { get; private set; }

    public Guid AccountId { get; private set; }

    /// <summary>Denormalised from the account so a trial balance needs no join. Never edited.</summary>
    public string AccountCode { get; private set; } = string.Empty;

    /// <summary>Whose sub-ledger this entry lands in. Null for a platform-level account.</summary>
    public Guid? AccountOwnerId { get; private set; }

    public LedgerDirection Direction { get; private set; }

    /// <summary>Always positive. The sign lives in <see cref="Direction"/>.</summary>
    public Money Amount { get; private set; }

    /// <summary>Position within the journal. Makes the posting reproducible line for line.</summary>
    public int LineNumber { get; private set; }

    /// <summary>What this entry is about, e.g. <c>deals.deal</c> plus the deal's id.</summary>
    public string? SubjectType { get; private set; }

    public Guid? SubjectId { get; private set; }

    /// <summary>The pricing registry line code this entry came from, e.g. <c>auction_commission</c>.</summary>
    public string? LineCode { get; private set; }

    /// <summary>Free text for the statement a dealer reads. Never carries a rule or a rate.</summary>
    public string? Memo { get; private set; }

    public DateTimeOffset PostedAt { get; private set; }

    internal static LedgerEntry Create(
        Guid journalId,
        LedgerAccount account,
        LedgerDirection direction,
        Money amount,
        int lineNumber,
        DateTimeOffset postedAt,
        string? subjectType,
        Guid? subjectId,
        string? lineCode,
        string? memo)
        => new(UuidV7.New(), journalId, account.Id, account.Code, direction, amount, lineNumber)
        {
            AccountOwnerId = account.OwnerId,
            PostedAt = postedAt,
            SubjectType = subjectType,
            SubjectId = subjectId,
            LineCode = lineCode,
            Memo = memo,
        };

    /// <summary>
    /// The entry's effect on the account's balance, in minor units.
    /// </summary>
    /// <remarks>
    /// Computed rather than stored so it can never disagree with <see cref="Direction"/>. Reports
    /// that need it in SQL read the generated column the migration defines from the same rule.
    /// </remarks>
    public long SignedMinorUnits(LedgerDirection normalBalance)
        => Direction == normalBalance ? Amount.MinorUnits : -Amount.MinorUnits;
}
