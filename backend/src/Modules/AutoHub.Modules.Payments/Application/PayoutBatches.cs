using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.Modules.Payments.Integration;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Payments.Application;

/// <summary>
/// Adds a payee to the open batch for today, claiming their balance while it is assembled.
/// </summary>
/// <remarks>
/// The hold is taken here and no journal is posted, because at this moment nothing has happened in
/// the world. Posting for an intention would put a fiction in the accounts; not holding at all would
/// let a second batch, assembled thirty seconds later, claim the same money and the bank send it
/// twice.
/// </remarks>
public sealed record QueuePayoutCommand : IRequest<Result<Guid>>
{
    public required string PayeeType { get; init; }

    public required Guid PayeeId { get; init; }

    public required long AmountMinorUnits { get; init; }

    public string CurrencyCode { get; init; } = "INR";

    /// <summary>Opaque beneficiary handle at the provider. Never an account number.</summary>
    public required string PayoutAccountRef { get; init; }
}

public sealed class QueuePayoutHandler : IRequestHandler<QueuePayoutCommand, Result<Guid>>
{
    private readonly IPayoutBatchRepository _batches;
    private readonly IWalletRepository _wallets;
    private readonly IClock _clock;

    public QueuePayoutHandler(IPayoutBatchRepository batches, IWalletRepository wallets, IClock clock)
    {
        _batches = batches;
        _wallets = wallets;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(QueuePayoutCommand request, CancellationToken cancellationToken)
    {
        Currency currency;
        try
        {
            currency = Currency.FromCode(request.CurrencyCode);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Validation("payout.currency_unsupported", "That currency is not supported.")
                .With("currency", request.CurrencyCode);
        }

        var amount = Money.FromMinorUnits(request.AmountMinorUnits, currency);

        var wallet = await _wallets.FindAsync(request.PayeeType, request.PayeeId, currency, cancellationToken);
        if (wallet is null)
        {
            return Error.NotFound("payout.wallet_not_found", "That payee has no wallet to pay out from.")
                .With("payeeId", request.PayeeId);
        }

        // Claimed before the batch row exists. If the batch insert then fails, the hold is rolled
        // back with it — both are in one transaction, which is the whole reason they are in one
        // handler.
        var held = wallet.Hold(amount);
        if (held.IsFailure)
        {
            return Result.Failure<Guid>(held.Error);
        }

        // The banking day in IST, because cut-off times are set in IST and a batch that lands on the
        // wrong side of one waits until tomorrow.
        var businessDate = _clock.TodayInIst;

        var batch = await _batches.FindOpenBatchAsync(currency, businessDate, cancellationToken);
        if (batch is null)
        {
            batch = PayoutBatch.Draft(currency, businessDate, _clock.UtcNow);
            await _batches.AddAsync(batch, cancellationToken);
        }

        var instruction = batch.Add(request.PayeeType, request.PayeeId, amount, request.PayoutAccountRef);
        if (instruction.IsFailure)
        {
            return Result.Failure<Guid>(instruction.Error);
        }

        await _batches.SaveChangesAsync(cancellationToken);
        await _wallets.SaveChangesAsync(cancellationToken);

        return instruction.Value.Id;
    }
}

/// <summary>
/// Sends a batch to the bank and posts the journal that moves the money out of wallets.
/// </summary>
/// <remarks>
/// <b>Ledger first, bank second.</b> The journal moves each payee's balance into
/// <c>payout_pending</c> before a single instruction is sent, so a crash between the two leaves the
/// money accounted for as in flight rather than as still withdrawable. The reverse order would let a
/// dealer's wallet be paid out twice across a restart.
/// <para>
/// A provider failure on one instruction does not roll the batch back. The money is already in
/// <c>payout_pending</c>, which is exactly where it should sit while operations decides whether to
/// retry the rail or reverse the instruction — and reversing is a journal, not an undo.
/// </para>
/// </remarks>
public sealed record InstructPayoutBatchCommand : IRequest<Result<int>>
{
    public required Guid PayoutBatchId { get; init; }

    public string? RailCode { get; init; }
}

public sealed class InstructPayoutBatchHandler : IRequestHandler<InstructPayoutBatchCommand, Result<int>>
{
    private readonly IPayoutBatchRepository _batches;
    private readonly IWalletRepository _wallets;
    private readonly ILedgerPostingService _postings;
    private readonly IPaymentGateway _gateway;
    private readonly IClock _clock;
    private readonly ILogger<InstructPayoutBatchHandler> _logger;

    public InstructPayoutBatchHandler(
        IPayoutBatchRepository batches,
        IWalletRepository wallets,
        ILedgerPostingService postings,
        IPaymentGateway gateway,
        IClock clock,
        ILogger<InstructPayoutBatchHandler> logger)
    {
        _batches = batches;
        _wallets = wallets;
        _postings = postings;
        _gateway = gateway;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(
        InstructPayoutBatchCommand request,
        CancellationToken cancellationToken)
    {
        var batch = await _batches.GetAsync(request.PayoutBatchId, cancellationToken);
        if (batch is null)
        {
            return Error.NotFound("payout.batch_not_found", "No payout batch with that id.")
                .With("payoutBatchId", request.PayoutBatchId);
        }

        if (batch.InstructedAt is not null)
        {
            return batch.Instructions.Count;
        }

        var sent = 0;

        foreach (var instruction in batch.Instructions)
        {
            var posted = await _postings.PostAsync(
                new PostingRequest
                {
                    ScopeCode = PostingScopes.PayoutInstruct,
                    IdempotencyKey = $"payout:{instruction.Id:D}:instruct",
                    Currency = batch.Currency,
                    Lines = [new PostingLine(PayoutLineCode, instruction.Amount)],
                    PartiesByRole = new Dictionary<string, PostingParty>(StringComparer.Ordinal)
                    {
                        [PostingTemplateCatalog.Roles.Dealer] =
                            new(instruction.PayeeType, instruction.PayeeId),
                    },
                    SourceType = "payments.payout_instruction",
                    SourceId = instruction.Id,
                    Description = $"Payout instruction in batch {batch.Reference}",
                },
                cancellationToken);

            if (posted.IsFailure)
            {
                return Result.Failure<int>(posted.Error);
            }

            // The claim has become a real ledger movement, so it must stop being counted twice.
            var wallet = await _wallets.FindAsync(
                instruction.PayeeType, instruction.PayeeId, batch.Currency, cancellationToken);

            wallet?.ReleaseHold(instruction.Amount);

            var response = await _gateway.PayoutAsync(
                new PaymentPayoutRequest
                {
                    Amount = instruction.Amount,
                    InstructionReference = instruction.Id.ToString("D", System.Globalization.CultureInfo.InvariantCulture),
                    BeneficiaryRef = instruction.PayoutAccountRef,
                    RailCode = request.RailCode,
                    Narration = batch.Reference,
                },
                new PaymentRoutingFacts
                {
                    AmountMinorUnits = instruction.Amount.MinorUnits,
                },
                $"payout:{instruction.Id:D}",
                cancellationToken);

            if (response.IsSuccess)
            {
                instruction.RecordSent(response.Value.ProviderReference);
                sent++;
            }
            else
            {
                // Not rolled back. The money is in payout_pending, which is the correct place for it
                // while operations decides between retrying the rail and reversing the instruction.
                instruction.RecordFailed(response.Error.Code);

                _logger.LogError(
                    "Payout {InstructionId} in batch {Reference} was refused: {Code}",
                    instruction.Id, batch.Reference, response.Error.Code);
            }
        }

        var marked = batch.MarkInstructed(_clock.UtcNow);
        if (marked.IsFailure)
        {
            return Result.Failure<int>(marked.Error);
        }

        await _batches.SaveChangesAsync(cancellationToken);
        await _wallets.SaveChangesAsync(cancellationToken);

        return sent;
    }

    /// <summary>The line code payout templates map. One code, because a payout has no components.</summary>
    private const string PayoutLineCode = "payout";
}
