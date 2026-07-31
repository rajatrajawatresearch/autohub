using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.SharedKernel.Money;

namespace AutoHub.Modules.Payments.Domain;

/// <summary>
/// The ledger's read and write surface.
/// </summary>
/// <remarks>
/// Note what is absent: there is no <c>Update</c> and no <c>Delete</c>. The repository cannot express
/// them, so no handler can accidentally call them, and the database triggers in
/// <see cref="LedgerSchema"/> refuse them for everything that does not go through here.
/// </remarks>
public interface ILedgerRepository
{
    /// <summary>
    /// Finds a journal already posted under a key.
    /// </summary>
    /// <remarks>
    /// The idempotency check every posting path runs first. Cheaper than catching the unique
    /// constraint violation, and it returns the original journal so the caller can answer with what
    /// was posted rather than with an error the caller has to interpret.
    /// </remarks>
    Task<Journal?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<Journal?> GetAsync(Guid journalId, CancellationToken cancellationToken);

    Task AddAsync(Journal journal, CancellationToken cancellationToken);

    /// <summary>
    /// Re-derives an account's balance from its entries. The authoritative answer.
    /// </summary>
    /// <remarks>
    /// Used by reconciliation and by anything that must be right rather than fast. Everything on a
    /// hot path reads the <see cref="Wallet"/> projection instead — and the two disagreeing is the
    /// incident reconciliation exists to find.
    /// </remarks>
    Task<Money> ComputeBalanceAsync(
        string accountCode,
        Guid? ownerId,
        DateTimeOffset? asOf,
        CancellationToken cancellationToken);

    /// <summary>Entries posted against an account in a window, oldest first. The statement query.</summary>
    Task<IReadOnlyList<LedgerEntry>> GetEntriesAsync(
        string accountCode,
        Guid? ownerId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Resolves account codes to accounts, creating owner-scoped ones on first use.
/// </summary>
/// <remarks>
/// Creation on demand rather than at dealer onboarding, because a wallet account with no entries is
/// a row that exists to make a join succeed. Deferring it means the chart of accounts contains only
/// accounts that have actually been used, which is what a finance team wants to look at.
/// </remarks>
public interface ILedgerAccountResolver
{
    Task<LedgerAccount?> ResolveAsync(
        string accountCode,
        string? ownerType,
        Guid? ownerId,
        Currency currency,
        CancellationToken cancellationToken);
}

public interface IWalletRepository
{
    Task<Wallet?> FindAsync(string ownerType, Guid ownerId, Currency currency, CancellationToken cancellationToken);

    Task<Wallet> GetOrOpenAsync(
        string ownerType,
        Guid ownerId,
        string accountCode,
        Currency currency,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPaymentIntentRepository
{
    Task<PaymentIntent?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<PaymentIntent?> FindByProviderReferenceAsync(
        string providerCode,
        string providerReference,
        CancellationToken cancellationToken);

    Task<PaymentIntent?> GetAsync(Guid paymentIntentId, CancellationToken cancellationToken);

    Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPayoutBatchRepository
{
    Task<PayoutBatch?> GetAsync(Guid batchId, CancellationToken cancellationToken);

    Task<PayoutBatch?> FindOpenBatchAsync(
        Currency currency,
        DateOnly businessDate,
        CancellationToken cancellationToken);

    Task AddAsync(PayoutBatch batch, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
