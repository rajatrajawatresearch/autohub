using AutoHub.Modules.Payments.Domain;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Payments.Application;

/// <summary>
/// Recomputes a wallet from the ledger and reports what it found.
/// </summary>
/// <remarks>
/// <b>This is the control that proves the projection is a projection.</b> The wallet row is a cache
/// of a <c>SUM</c> over entries; this handler takes the sum, compares, and — if they differ —
/// overwrites the cache. The direction is always ledger to wallet. A repair that adjusted entries to
/// match a wallet would be a repair that decided the cache was authoritative, and at that point the
/// ledger has stopped being the source of truth and nobody has written it down.
/// <para>
/// A non-zero difference is logged as an error rather than swallowed, because the difference itself
/// is not the problem — the code path that produced it is, and it will produce another one tomorrow.
/// </para>
/// <para>
/// <see cref="Wallet.Held"/> is deliberately not rebuilt. It is not a ledger fact: it is a claim
/// staked by a payout batch that has been assembled and not yet instructed, and it lives only in the
/// batch. Rebuilding it from entries would silently release every claim in flight and let a second
/// batch pay the same money out again.
/// </para>
/// </remarks>
public sealed record RebuildWalletCommand : IRequest<Result<WalletRebuildResult>>
{
    public required string OwnerType { get; init; }

    public required Guid OwnerId { get; init; }

    public string CurrencyCode { get; init; } = "INR";

    /// <summary>Reports the difference without writing it. What a nightly check runs.</summary>
    public bool DryRun { get; init; }
}

public sealed record WalletRebuildResult(
    Guid WalletId,
    long ProjectedMinorUnits,
    long LedgerMinorUnits,
    long DifferenceMinorUnits,
    bool WasRepaired);

public sealed class RebuildWalletHandler : IRequestHandler<RebuildWalletCommand, Result<WalletRebuildResult>>
{
    private readonly IWalletRepository _wallets;
    private readonly ILedgerRepository _ledger;
    private readonly ILogger<RebuildWalletHandler> _logger;

    public RebuildWalletHandler(
        IWalletRepository wallets,
        ILedgerRepository ledger,
        ILogger<RebuildWalletHandler> logger)
    {
        _wallets = wallets;
        _ledger = ledger;
        _logger = logger;
    }

    public async Task<Result<WalletRebuildResult>> Handle(
        RebuildWalletCommand request,
        CancellationToken cancellationToken)
    {
        Currency currency;
        try
        {
            currency = Currency.FromCode(request.CurrencyCode);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Validation("wallet.currency_unsupported", "That currency is not supported.")
                .With("currency", request.CurrencyCode);
        }

        var wallet = await _wallets.FindAsync(request.OwnerType, request.OwnerId, currency, cancellationToken);
        if (wallet is null)
        {
            return Error.NotFound("wallet.not_found", "That owner has no wallet.")
                .With("ownerId", request.OwnerId);
        }

        var ledgerBalance = await _ledger.ComputeBalanceAsync(
            wallet.AccountCode, wallet.OwnerId, null, cancellationToken);

        var difference = wallet.Available.MinorUnits - ledgerBalance.MinorUnits;

        if (difference == 0)
        {
            return new WalletRebuildResult(
                wallet.Id, wallet.Available.MinorUnits, ledgerBalance.MinorUnits, 0, false);
        }

        _logger.LogError(
            "Wallet {WalletId} projects {ProjectedMinor} but the ledger says {LedgerMinor} "
            + "(difference {DifferenceMinor}); the write path that caused this needs finding",
            wallet.Id, wallet.Available.MinorUnits, ledgerBalance.MinorUnits, difference);

        if (request.DryRun)
        {
            return new WalletRebuildResult(
                wallet.Id, wallet.Available.MinorUnits, ledgerBalance.MinorUnits, difference, false);
        }

        wallet.RebuildFromLedger(ledgerBalance, wallet.LedgerVersion);
        await _wallets.SaveChangesAsync(cancellationToken);

        return new WalletRebuildResult(
            wallet.Id, wallet.Available.MinorUnits, ledgerBalance.MinorUnits, difference, true);
    }
}
