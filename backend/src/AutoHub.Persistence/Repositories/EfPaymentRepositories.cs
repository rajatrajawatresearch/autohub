using AutoHub.Modules.Payments.Application;
using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.SharedKernel.Money;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Repositories;

/// <summary>
/// The ledger's read and write surface. Note what it cannot express: update and delete.
/// </summary>
/// <remarks>
/// There is no <c>Update</c> here because the interface has none and the database refuses one — the
/// immutability trigger in <see cref="LedgerSchema"/> rejects any change to an entry. A correction is
/// a reversing journal, which is a new set of rows, which is the only version of accounting history
/// an auditor can rely on.
/// </remarks>
public sealed class EfLedgerRepository : ILedgerRepository
{
    private readonly AutoHubDbContext _context;

    public EfLedgerRepository(AutoHubDbContext context) => _context = context;

    /// <summary>
    /// Finds a journal already posted under a key.
    /// </summary>
    /// <remarks>
    /// Run before every posting. Cheaper than catching the unique violation, and it returns the
    /// original so the caller can answer with what was posted rather than with an error the caller
    /// then has to interpret.
    /// </remarks>
    public async Task<Journal?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
        => await _context.LedgerJournals
            .Include(journal => journal.Entries)
            .FirstOrDefaultAsync(journal => journal.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<Journal?> GetAsync(Guid journalId, CancellationToken cancellationToken)
        => await _context.LedgerJournals
            .Include(journal => journal.Entries)
            .FirstOrDefaultAsync(journal => journal.Id == journalId, cancellationToken);

    public async Task AddAsync(Journal journal, CancellationToken cancellationToken)
        => await _context.LedgerJournals.AddAsync(journal, cancellationToken);

    /// <summary>
    /// Re-derives an account's balance from its entries. The authoritative answer.
    /// </summary>
    /// <remarks>
    /// Summed in the database rather than in memory: an account with two years of entries is a table
    /// scan either way, and doing it here means the rows never cross the wire. The direction is
    /// folded into the sum by the account's normal balance, so the number this returns points the way
    /// the account points and a caller cannot get the sign wrong.
    /// <para>
    /// The currency comes from the account rather than from the entries. An account holds exactly one
    /// currency by construction, and reading it from the first entry would return nothing meaningful
    /// for an account that has none yet — which is precisely when reconciliation asks.
    /// </para>
    /// </remarks>
    public async Task<Money> ComputeBalanceAsync(
        string accountCode,
        Guid? ownerId,
        DateTimeOffset? asOf,
        CancellationToken cancellationToken)
    {
        var account = await _context.LedgerAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Code == accountCode && candidate.OwnerId == ownerId,
                cancellationToken);

        if (account is null)
        {
            return Money.ZeroInr;
        }

        var entries = _context.LedgerEntries
            .Where(entry => entry.AccountCode == accountCode && entry.AccountOwnerId == ownerId);

        if (asOf is { } cutoff)
        {
            entries = entries.Where(entry => entry.PostedAt <= cutoff);
        }

        var debits = await entries
            .Where(entry => entry.Direction == LedgerDirection.Debit)
            .SumAsync(entry => entry.Amount.MinorUnits, cancellationToken);

        var credits = await entries
            .Where(entry => entry.Direction == LedgerDirection.Credit)
            .SumAsync(entry => entry.Amount.MinorUnits, cancellationToken);

        var signed = account.NormalBalance == LedgerDirection.Debit
            ? debits - credits
            : credits - debits;

        return Money.FromMinorUnits(signed, account.Currency);
    }

    /// <summary>Entries posted against an account in a window, oldest first. The statement query.</summary>
    public async Task<IReadOnlyList<LedgerEntry>> GetEntriesAsync(
        string accountCode,
        Guid? ownerId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
        => await _context.LedgerEntries
            .AsNoTracking()
            .Where(entry => entry.AccountCode == accountCode && entry.AccountOwnerId == ownerId)
            .Where(entry => entry.PostedAt >= from && entry.PostedAt <= to)
            .OrderBy(entry => entry.PostedAt)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// Resolves account codes to accounts, cloning owner-scoped ones from their control account.
/// </summary>
/// <remarks>
/// Creation on demand rather than at dealer onboarding, because a wallet account with no entries is
/// a row that exists to make a join succeed. The chart of accounts then contains only accounts that
/// have actually been used, which is what a finance team wants to look at.
/// <para>
/// The clone is <c>Add</c>ed to the change tracker and not saved here. Saving would commit a new
/// account outside the posting's transaction, so a journal that later failed to balance would leave
/// an orphan account behind — and the caller's <c>SaveChanges</c> is the one that knows whether the
/// posting stood.
/// </para>
/// </remarks>
public sealed class EfLedgerAccountResolver : ILedgerAccountResolver
{
    private readonly AutoHubDbContext _context;

    public EfLedgerAccountResolver(AutoHubDbContext context) => _context = context;

    public async Task<LedgerAccount?> ResolveAsync(
        string accountCode,
        string? ownerType,
        Guid? ownerId,
        Currency currency,
        CancellationToken cancellationToken)
    {
        var existing = await _context.LedgerAccounts
            .FirstOrDefaultAsync(
                account => account.Code == accountCode
                    && account.OwnerId == ownerId
                    && account.Currency == currency,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        // A control account is never created on demand. Its type and its contra rule are finance
        // decisions, and inventing one here would let a typo in a posting template silently open a
        // new account rather than fail the posting.
        if (ownerId is not { } owner || ownerType is null)
        {
            return null;
        }

        var control = await _context.LedgerAccounts
            .FirstOrDefaultAsync(
                account => account.Code == accountCode
                    && account.OwnerId == null
                    && account.Currency == currency,
                cancellationToken);

        if (control is null)
        {
            return null;
        }

        var scoped = LedgerAccount.ForOwner(control, ownerType, owner, $"{control.Name} — {owner:D}");

        await _context.LedgerAccounts.AddAsync(scoped, cancellationToken);

        return scoped;
    }
}

public sealed class EfWalletRepository : IWalletRepository
{
    private readonly AutoHubDbContext _context;

    public EfWalletRepository(AutoHubDbContext context) => _context = context;

    public async Task<Wallet?> FindAsync(
        string ownerType,
        Guid ownerId,
        Currency currency,
        CancellationToken cancellationToken)
        => await _context.Wallets
            .FirstOrDefaultAsync(
                wallet => wallet.OwnerType == ownerType
                    && wallet.OwnerId == ownerId
                    && wallet.Currency == currency,
                cancellationToken);

    /// <summary>
    /// Finds a wallet or opens one, without saving.
    /// </summary>
    /// <remarks>
    /// Left unsaved for the reason the account resolver is: the wallet is opened as part of applying
    /// a journal, and a wallet committed separately from the entry that justified it is a balance
    /// with nothing behind it. The unique index on <c>(owner_type, owner_id, account_code, currency)</c>
    /// is what makes two concurrent first-credits collide rather than open two wallets and hide half
    /// the money in the one nobody reads.
    /// </remarks>
    public async Task<Wallet> GetOrOpenAsync(
        string ownerType,
        Guid ownerId,
        string accountCode,
        Currency currency,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Wallets
            .FirstOrDefaultAsync(
                wallet => wallet.OwnerType == ownerType
                    && wallet.OwnerId == ownerId
                    && wallet.AccountCode == accountCode
                    && wallet.Currency == currency,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var opened = Wallet.Open(ownerType, ownerId, accountCode, currency);

        await _context.Wallets.AddAsync(opened, cancellationToken);

        return opened;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// Payment intents, and the two lookups that make a duplicate harmless.
/// </summary>
/// <remarks>
/// The same payment arrives twice in two ways — the caller retrying with the same
/// <c>Idempotency-Key</c>, and the vendor redelivering a webhook for the same reference — and both
/// queries here have a unique index behind them so the second arrival finds this row instead of
/// creating another and capturing the money again.
/// </remarks>
public sealed class EfPaymentIntentRepository : IPaymentIntentRepository
{
    private readonly AutoHubDbContext _context;

    public EfPaymentIntentRepository(AutoHubDbContext context) => _context = context;

    public async Task<PaymentIntent?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
        => await _context.PaymentIntents
            .FirstOrDefaultAsync(intent => intent.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<PaymentIntent?> FindByProviderReferenceAsync(
        string providerCode,
        string providerReference,
        CancellationToken cancellationToken)
        => await _context.PaymentIntents
            .FirstOrDefaultAsync(
                intent => intent.ProviderCode == providerCode
                    && intent.ProviderReference == providerReference,
                cancellationToken);

    public async Task<PaymentIntent?> GetAsync(Guid paymentIntentId, CancellationToken cancellationToken)
        => await _context.PaymentIntents
            .FirstOrDefaultAsync(intent => intent.Id == paymentIntentId, cancellationToken);

    public async Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken)
        => await _context.PaymentIntents.AddAsync(intent, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

public sealed class EfPayoutBatchRepository : IPayoutBatchRepository
{
    private readonly AutoHubDbContext _context;

    public EfPayoutBatchRepository(AutoHubDbContext context) => _context = context;

    public async Task<PayoutBatch?> GetAsync(Guid batchId, CancellationToken cancellationToken)
        => await _context.PayoutBatches
            .Include(batch => batch.Instructions)
            .FirstOrDefaultAsync(batch => batch.Id == batchId, cancellationToken);

    /// <summary>
    /// The batch still being assembled for a banking day, if there is one.
    /// </summary>
    /// <remarks>
    /// "Open" is <c>instructed_at IS NULL</c> and matches the partial unique index exactly, so two
    /// schedulers that both decide to open today's batch collide in the database rather than sending
    /// every dealer two transfers.
    /// </remarks>
    public async Task<PayoutBatch?> FindOpenBatchAsync(
        Currency currency,
        DateOnly businessDate,
        CancellationToken cancellationToken)
        => await _context.PayoutBatches
            .Include(batch => batch.Instructions)
            .FirstOrDefaultAsync(
                batch => batch.Currency == currency
                    && batch.BusinessDate == businessDate
                    && batch.InstructedAt == null,
                cancellationToken);

    public async Task AddAsync(PayoutBatch batch, CancellationToken cancellationToken)
        => await _context.PayoutBatches.AddAsync(batch, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// Stores reconciliation runs and the differences they found.
/// </summary>
/// <remarks>
/// Write-only by interface, and that is the whole shape of the thing: a run is written once, at the
/// end of the comparison, and read afterwards by reports rather than by this module. There is no
/// <c>Get</c> because nothing in the payments module ever loads a past run to act on it — acting on a
/// difference is an operator's decision, taken against the difference's own remedy.
/// </remarks>
public sealed class EfReconciliationRepository : IReconciliationRepository
{
    private readonly AutoHubDbContext _context;

    public EfReconciliationRepository(AutoHubDbContext context) => _context = context;

    public async Task AddAsync(ReconciliationRun run, CancellationToken cancellationToken)
        => await _context.ReconciliationRuns.AddAsync(run, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// Serves posting templates from the database, replacing the module's in-code floor.
/// </summary>
/// <remarks>
/// The module registers a code-based store with <c>TryAdd</c> so a fresh environment posts correctly
/// before anybody has seeded the chart of accounts. This one is registered over it, because the whole
/// point of the templates is that finance can edit them — and the moment they do, the database is the
/// answer and the constants are stale.
/// </remarks>
public sealed class EfPostingTemplateStore : IPostingTemplateStore
{
    private readonly AutoHubDbContext _context;

    public EfPostingTemplateStore(AutoHubDbContext context) => _context = context;

    public async Task<IReadOnlyList<PostingTemplate>> GetForScopeAsync(
        string scopeCode,
        CancellationToken cancellationToken)
        => await _context.PostingTemplates
            .AsNoTracking()
            .Where(template => template.ScopeCode == scopeCode && template.IsActive)
            .OrderBy(template => template.SortOrder)
            .ThenBy(template => template.Id)
            .ToListAsync(cancellationToken);
}
