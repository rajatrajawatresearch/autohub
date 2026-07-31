using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Payments.Application;

/// <summary>
/// Keeps wallet balances in step with the entries that caused them.
/// </summary>
/// <remarks>
/// Separated from the posting service so the direction of dependency is unmistakable: entries are
/// written first and the projection follows. Nothing here can change the ledger, and nothing in the
/// ledger asks this what a balance is.
/// </remarks>
public interface IWalletProjector
{
    Task<Result> ApplyAsync(Journal journal, CancellationToken cancellationToken);
}

public sealed class WalletProjector : IWalletProjector
{
    private readonly IWalletRepository _wallets;

    public WalletProjector(IWalletRepository wallets) => _wallets = wallets;

    public async Task<Result> ApplyAsync(Journal journal, CancellationToken cancellationToken)
    {
        foreach (var entry in journal.Entries)
        {
            if (!LedgerAccounts.OwnerScoped.Contains(entry.AccountCode) || entry.AccountOwnerId is not { } ownerId)
            {
                continue;
            }

            var ownerType = string.Equals(entry.AccountCode, LedgerAccounts.DealerWallet, StringComparison.Ordinal)
                ? PaymentOwnerTypes.Dealer
                : PaymentOwnerTypes.User;

            var wallet = await _wallets.GetOrOpenAsync(
                ownerType, ownerId, entry.AccountCode, entry.Amount.Currency, cancellationToken);

            // A wallet is a liability: a credit increases what we owe the owner, a debit reduces it.
            // Reading the direction off the entry rather than off the scope means a new posting rule
            // needs no change here — the projection follows the ledger by construction.
            var applied = entry.Direction == LedgerDirection.Credit
                ? wallet.Credit(entry.Amount, journal.Id)
                : wallet.Debit(entry.Amount, journal.Id);

            if (applied.IsFailure)
            {
                return applied;
            }
        }

        return Result.Success();
    }
}
