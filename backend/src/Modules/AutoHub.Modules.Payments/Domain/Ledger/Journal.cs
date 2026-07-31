using System.Globalization;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Payments.Domain.Ledger;

/// <summary>
/// A balanced set of ledger entries, posted as one indivisible fact.
/// </summary>
/// <remarks>
/// <b>Why the ledger is the truth and the deal row is not.</b> A deal row says a deal is worth
/// ₹5,54,900 and settled on Tuesday. It cannot say where that money currently is, how much of it is
/// still the buyer's, how much is GST owed onward, how much the seller can withdraw right now, or
/// what remains of it after a partial refund. Those are not attributes of a deal; they are positions
/// across accounts, and only entries produce them.
/// <para>
/// The failure mode when a team gets this backwards is specific and always the same. Balances become
/// columns — <c>wallet_balance</c>, <c>escrow_amount</c>, <c>commission_earned</c> — updated by the
/// handler that happens to be running. Then two handlers update the same column concurrently and one
/// increment is lost; then a refund path forgets one of the four columns it should have moved; then
/// a report sums the deal table while another sums the wallet table and the two disagree by ₹40,000
/// that nobody can locate, because there is no record of <i>why</i> any column holds the number it
/// holds. Every one of those is unrecoverable after the fact: the information needed to reconstruct
/// the truth was never written down.
/// </para>
/// <para>
/// With entries, all of it is recoverable by construction. A balance is <c>SUM</c> over an
/// append-only table. A discrepancy is a query that names the journal that caused it. A correction is
/// a reversing journal that is itself visible. The deal row becomes what it should always have been:
/// a description of an agreement, not an assertion about money.
/// </para>
/// <para>
/// <b>The invariant is enforced three times, deliberately.</b> In <see cref="JournalBuilder"/>,
/// where a caller gets a <c>Result</c> and a readable error. In <see cref="Post"/>, so that no other
/// construction path can bypass the builder. And in the database, by the constraints and trigger in
/// <see cref="LedgerSchema"/> — because the first two protect against bugs in this codebase and the
/// third protects against everything else that will ever hold a connection string.
/// </para>
/// </remarks>
public sealed class Journal : AggregateRoot, IAuditable
{
    private readonly List<LedgerEntry> _entries = [];

    private Journal(Guid id, string reference, string scopeCode, string idempotencyKey, Currency currency)
        : base(id)
    {
        Reference = reference;
        ScopeCode = scopeCode;
        IdempotencyKey = idempotencyKey;
        Currency = currency;
        TotalDebits = Money.Zero(currency);
        TotalCredits = Money.Zero(currency);
    }

    private Journal() { }

    /// <summary>Human-quotable, printed on statements: <c>JN20260729A1B2C3</c>.</summary>
    public string Reference { get; private set; } = string.Empty;

    /// <summary>What kind of event this posting records, e.g. <c>deal.settle</c>, <c>refund</c>.</summary>
    public string ScopeCode { get; private set; } = string.Empty;

    /// <summary>
    /// The key that makes posting idempotent, unique across the whole ledger.
    /// </summary>
    /// <remarks>
    /// Derived from the fact being posted rather than generated — <c>deal:{id}:settle</c>,
    /// <c>payout:{batchId}:instruct</c> — so that a redelivered outbox message, a retried workflow
    /// effect and an operator re-running a batch all compute the same key and the unique index does
    /// the deduplication. A generated key would be unique per attempt, which is precisely the
    /// opposite of what is wanted.
    /// </remarks>
    public string IdempotencyKey { get; private set; } = string.Empty;

    public Currency Currency { get; private set; }

    public Money TotalDebits { get; private set; }

    public Money TotalCredits { get; private set; }

    public IReadOnlyList<LedgerEntry> Entries => _entries;

    public string? SourceType { get; private set; }

    public Guid? SourceId { get; private set; }

    public string? Description { get; private set; }

    /// <summary>The journal this one reverses, if it is a correction. Null on an original posting.</summary>
    public Guid? ReversesJournalId { get; private set; }

    public DateTimeOffset PostedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Builds a journal from drafted lines, or refuses.
    /// </summary>
    /// <remarks>
    /// The only construction path. It is <c>internal</c> to the domain namespace's callers through
    /// <see cref="JournalBuilder"/>, so there is no way to obtain an unbalanced <see cref="Journal"/>
    /// instance at all — the invalid state is not merely rejected on save, it is unrepresentable.
    /// </remarks>
    internal static Result<Journal> Post(JournalDraft draft)
    {
        if (draft.Lines.Count < 2)
        {
            return Error.Validation(
                "ledger.journal_too_few_entries",
                "A journal needs at least one debit and one credit.")
                .With("entryCount", draft.Lines.Count);
        }

        if (string.IsNullOrWhiteSpace(draft.IdempotencyKey))
        {
            return Error.Validation(
                "ledger.journal_idempotency_key_required",
                "Every posting must carry the key that makes it repeatable.");
        }

        var currency = draft.Lines[0].Amount.Currency;
        var debits = 0L;
        var credits = 0L;

        foreach (var line in draft.Lines)
        {
            if (line.Amount.Currency != currency)
            {
                return Error.Validation(
                    "ledger.journal_currency_mixed",
                    "A journal cannot mix currencies. Post the exchange as its own journal.")
                    .With("expected", currency.Code)
                    .With("found", line.Amount.Currency.Code);
            }

            // Zero is rejected as well as negative. A zero entry balances trivially and tells a
            // reader nothing, and it is nearly always a rounding bug that produced an empty split.
            if (!line.Amount.IsPositive)
            {
                return Error.Validation(
                    "ledger.entry_amount_not_positive",
                    "A ledger entry's amount must be positive; direction carries the sign.")
                    .With("accountCode", line.Account.Code);
            }

            var postable = line.Account.EnsurePostable();
            if (postable.IsFailure)
            {
                return Result.Failure<Journal>(postable.Error);
            }

            if (line.Account.Currency != currency)
            {
                return Error.Validation(
                    "ledger.account_currency_mismatch",
                    "An entry was drafted against an account held in another currency.")
                    .With("accountCode", line.Account.Code)
                    .With("accountCurrency", line.Account.Currency.Code);
            }

            if (line.Direction == LedgerDirection.Debit)
            {
                debits += line.Amount.MinorUnits;
            }
            else
            {
                credits += line.Amount.MinorUnits;
            }
        }

        if (debits != credits)
        {
            // The one failure this whole type exists to produce. Returned rather than thrown, and
            // with both totals in the metadata, because the operator who sees it needs to know the
            // size of the hole before they need to know the stack trace.
            return Error.Rule(
                "ledger.journal_unbalanced",
                "This posting does not balance and has not been written.")
                .With("debitsMinorUnits", debits)
                .With("creditsMinorUnits", credits)
                .With("differenceMinorUnits", debits - credits);
        }

        var id = UuidV7.New(draft.PostedAt);

        var journal = new Journal(id, BuildReference(id, draft.PostedAt), draft.ScopeCode, draft.IdempotencyKey, currency)
        {
            SourceType = draft.SourceType,
            SourceId = draft.SourceId,
            Description = draft.Description,
            ReversesJournalId = draft.ReversesJournalId,
            PostedAt = draft.PostedAt,
            TotalDebits = Money.FromMinorUnits(debits, currency),
            TotalCredits = Money.FromMinorUnits(credits, currency),
        };

        var lineNumber = 1;
        foreach (var line in draft.Lines)
        {
            journal._entries.Add(LedgerEntry.Create(
                journal.Id,
                line.Account,
                line.Direction,
                line.Amount,
                lineNumber++,
                draft.PostedAt,
                line.SubjectType ?? draft.SourceType,
                line.SubjectId ?? draft.SourceId,
                line.LineCode,
                line.Memo));
        }

        journal.Raise(new JournalPosted(
            journal.Id,
            draft.PostedAt,
            journal.Reference,
            journal.ScopeCode,
            journal.IdempotencyKey,
            journal.TotalDebits.MinorUnits,
            currency.Code,
            journal.SourceType,
            journal.SourceId));

        return journal;
    }

    /// <summary>
    /// Drafts the journal that undoes this one.
    /// </summary>
    /// <remarks>
    /// Every entry, with its direction flipped, under a new key. A ledger is corrected by adding, so
    /// both the mistake and the correction stay visible — an accountant reading the account can see
    /// what happened and when it was noticed, which a silent UPDATE would have destroyed.
    /// </remarks>
    public IReadOnlyList<JournalLine> ReversalLines(IReadOnlyDictionary<Guid, LedgerAccount> accountsById)
    {
        var lines = new List<JournalLine>(_entries.Count);

        foreach (var entry in _entries.OrderBy(e => e.LineNumber))
        {
            if (!accountsById.TryGetValue(entry.AccountId, out var account))
            {
                continue;
            }

            lines.Add(new JournalLine(
                account,
                entry.Direction == LedgerDirection.Debit ? LedgerDirection.Credit : LedgerDirection.Debit,
                entry.Amount,
                entry.SubjectType,
                entry.SubjectId,
                entry.LineCode,
                $"Reversal of {Reference}"));
        }

        return lines;
    }

    private static string BuildReference(Guid id, DateTimeOffset postedAt)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"JN{postedAt.UtcDateTime:yyyyMMdd}{id.ToString("N", CultureInfo.InvariantCulture)[^6..].ToUpperInvariant()}");
}

/// <summary>One drafted entry, before the journal has been proved to balance.</summary>
public sealed record JournalLine(
    LedgerAccount Account,
    LedgerDirection Direction,
    Money Amount,
    string? SubjectType = null,
    Guid? SubjectId = null,
    string? LineCode = null,
    string? Memo = null);

/// <summary>Everything a journal needs, gathered before validation.</summary>
public sealed record JournalDraft
{
    public required string ScopeCode { get; init; }

    public required string IdempotencyKey { get; init; }

    public required DateTimeOffset PostedAt { get; init; }

    public required IReadOnlyList<JournalLine> Lines { get; init; }

    public string? SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public string? Description { get; init; }

    public Guid? ReversesJournalId { get; init; }
}

/// <summary>
/// The one way to construct a posting.
/// </summary>
/// <remarks>
/// Reads the way an accountant states a posting — debits then credits, each naming its account and
/// its amount — so a code reviewer who is not a developer can still check the journal against the
/// intention. That matters more here than anywhere else in the codebase, because the reviewer who
/// catches a wrong account is usually the finance lead.
/// </remarks>
public sealed class JournalBuilder
{
    private readonly List<JournalLine> _lines = [];
    private readonly string _scopeCode;
    private readonly string _idempotencyKey;
    private readonly DateTimeOffset _postedAt;

    private string? _sourceType;
    private Guid? _sourceId;
    private string? _description;
    private Guid? _reversesJournalId;

    private JournalBuilder(string scopeCode, string idempotencyKey, DateTimeOffset postedAt)
    {
        _scopeCode = scopeCode;
        _idempotencyKey = idempotencyKey;
        _postedAt = postedAt;
    }

    public static JournalBuilder For(string scopeCode, string idempotencyKey, DateTimeOffset postedAt)
        => new(scopeCode, idempotencyKey, postedAt);

    public JournalBuilder Against(string sourceType, Guid sourceId)
    {
        _sourceType = sourceType;
        _sourceId = sourceId;
        return this;
    }

    public JournalBuilder Describing(string description)
    {
        _description = description;
        return this;
    }

    public JournalBuilder Reversing(Guid journalId)
    {
        _reversesJournalId = journalId;
        return this;
    }

    public JournalBuilder Debit(LedgerAccount account, Money amount, string? lineCode = null, string? memo = null)
    {
        _lines.Add(new JournalLine(account, LedgerDirection.Debit, amount, LineCode: lineCode, Memo: memo));
        return this;
    }

    public JournalBuilder Credit(LedgerAccount account, Money amount, string? lineCode = null, string? memo = null)
    {
        _lines.Add(new JournalLine(account, LedgerDirection.Credit, amount, LineCode: lineCode, Memo: memo));
        return this;
    }

    public JournalBuilder Add(JournalLine line)
    {
        _lines.Add(line);
        return this;
    }

    public JournalBuilder AddRange(IEnumerable<JournalLine> lines)
    {
        _lines.AddRange(lines);
        return this;
    }

    public Result<Journal> Build()
        => Journal.Post(new JournalDraft
        {
            ScopeCode = _scopeCode,
            IdempotencyKey = _idempotencyKey,
            PostedAt = _postedAt,
            Lines = _lines,
            SourceType = _sourceType,
            SourceId = _sourceId,
            Description = _description,
            ReversesJournalId = _reversesJournalId,
        });
}
