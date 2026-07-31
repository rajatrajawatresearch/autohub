using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Payments.Application;

/// <summary>
/// One day's comparison of what a provider says happened against what the ledger says happened.
/// </summary>
/// <remarks>
/// <b>Reconciliation is not a report; it is the control that makes the ledger trustworthy.</b> Every
/// assumption this module makes — that a webhook arrived, that a capture posted, that a payout
/// actually left — is an assumption until a third party's file agrees with it. The differences this
/// run finds are the ones nothing else can find: a capture whose webhook was never delivered, a
/// refund issued at the provider's dashboard by a support agent, a payout that reversed hours after
/// it was accepted.
/// <para>
/// Stored as an aggregate rather than logged, because "when did we last reconcile, and what did we
/// find?" is a question an auditor asks and a Slack message cannot answer.
/// </para>
/// </remarks>
public sealed class ReconciliationRun : AggregateRoot, IAuditable
{
    private readonly List<ReconciliationDifference> _differences = [];

    private ReconciliationRun(Guid id, string providerCode, DateOnly businessDate, Currency currency)
        : base(id)
    {
        ProviderCode = providerCode;
        BusinessDate = businessDate;
        Currency = currency;
        ProviderTotal = Money.Zero(currency);
        LedgerTotal = Money.Zero(currency);
    }

    private ReconciliationRun() { }

    public string ProviderCode { get; private set; } = string.Empty;

    public DateOnly BusinessDate { get; private set; }

    public Currency Currency { get; private set; }

    public Money ProviderTotal { get; private set; }

    public Money LedgerTotal { get; private set; }

    public int MatchedCount { get; private set; }

    public IReadOnlyList<ReconciliationDifference> Differences => _differences;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static ReconciliationRun Start(
        string providerCode,
        DateOnly businessDate,
        Currency currency,
        DateTimeOffset now)
        => new(UuidV7.New(now), providerCode, businessDate, currency) { StartedAt = now };

    public void RecordMatch(Money amount)
    {
        MatchedCount++;
        ProviderTotal += amount;
        LedgerTotal += amount;
    }

    public void RecordDifference(ReconciliationDifference difference)
    {
        _differences.Add(difference);

        if (difference.ProviderAmount is { } provider)
        {
            ProviderTotal += provider;
        }

        if (difference.LedgerAmount is { } ledger)
        {
            LedgerTotal += ledger;
        }
    }

    public void Complete(DateTimeOffset now)
    {
        CompletedAt = now;

        var gap = ProviderTotal.MinorUnits - LedgerTotal.MinorUnits;

        Raise(new ReconciliationCompleted(
            Id,
            now,
            ProviderCode,
            BusinessDate,
            MatchedCount,
            _differences.Count,
            Math.Abs(gap),
            Currency.Code));
    }
}

/// <summary>One line the two sides disagree about.</summary>
public sealed class ReconciliationDifference : Entity
{
    private ReconciliationDifference(Guid id, Guid runId, ReconciliationDifferenceKind kind, string reference)
        : base(id)
    {
        ReconciliationRunId = runId;
        Kind = kind;
        ProviderReference = reference;
    }

    private ReconciliationDifference() { }

    public Guid ReconciliationRunId { get; private set; }

    public ReconciliationDifferenceKind Kind { get; private set; }

    public string ProviderReference { get; private set; } = string.Empty;

    public Money? ProviderAmount { get; private set; }

    public Money? LedgerAmount { get; private set; }

    public Guid? PaymentIntentId { get; private set; }

    public string? Note { get; private set; }

    public static ReconciliationDifference Create(
        Guid runId,
        ReconciliationDifferenceKind kind,
        string providerReference,
        Money? providerAmount,
        Money? ledgerAmount,
        Guid? paymentIntentId,
        string? note)
        => new(UuidV7.New(), runId, kind, providerReference)
        {
            ProviderAmount = providerAmount,
            LedgerAmount = ledgerAmount,
            PaymentIntentId = paymentIntentId,
            Note = note,
        };
}

/// <summary>
/// What kind of disagreement was found.
/// </summary>
/// <remarks>
/// Each value maps to a different remedy, which is why they are distinguished rather than counted
/// together. <see cref="MissingInLedger"/> is money we hold and have not recorded — post it.
/// <see cref="MissingAtProvider"/> is money we recorded and do not hold — investigate before
/// anything else moves. <see cref="AmountMismatch"/> is usually a partial capture or a provider fee
/// deducted at source, and is the one that quietly erodes a clearing balance over months.
/// </remarks>
public enum ReconciliationDifferenceKind
{
    MissingInLedger = 1,
    MissingAtProvider = 2,
    AmountMismatch = 3,
    StatusMismatch = 4,
}

/// <summary>One row of a provider's settlement file, normalised.</summary>
public sealed record ProviderSettlementLine(
    string ProviderReference,
    Money Amount,
    bool IsCredit,
    DateTimeOffset OccurredAt);

/// <summary>
/// Reads a provider's settlement file for a day.
/// </summary>
/// <remarks>
/// An interface because every provider publishes a different format — a CSV over SFTP, a paginated
/// API, an emailed spreadsheet — and because reconciliation must be runnable in a test against a
/// list. The adapters live in <c>AutoHub.Integrations</c>, where things that talk to the outside
/// world belong.
/// </remarks>
public interface IProviderSettlementSource
{
    string ProviderCode { get; }

    Task<Result<IReadOnlyList<ProviderSettlementLine>>> GetSettlementAsync(
        DateOnly businessDate,
        CancellationToken cancellationToken);
}

public interface IReconciliationRepository
{
    Task AddAsync(ReconciliationRun run, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>The entry point operations and the nightly scheduler both call.</summary>
public sealed record RunReconciliationCommand : IRequest<Result<ReconciliationSummary>>
{
    public required string ProviderCode { get; init; }

    public required DateOnly BusinessDate { get; init; }

    public string CurrencyCode { get; init; } = "INR";
}

public sealed record ReconciliationSummary(
    Guid RunId,
    int MatchedCount,
    int DifferenceCount,
    long ProviderTotalMinorUnits,
    long LedgerTotalMinorUnits,
    string Currency);

public sealed class RunReconciliationHandler
    : IRequestHandler<RunReconciliationCommand, Result<ReconciliationSummary>>
{
    private readonly IEnumerable<IProviderSettlementSource> _sources;
    private readonly IPaymentIntentRepository _intents;
    private readonly IReconciliationRepository _runs;
    private readonly IClock _clock;
    private readonly ILogger<RunReconciliationHandler> _logger;

    public RunReconciliationHandler(
        IEnumerable<IProviderSettlementSource> sources,
        IPaymentIntentRepository intents,
        IReconciliationRepository runs,
        IClock clock,
        ILogger<RunReconciliationHandler> logger)
    {
        _sources = sources;
        _intents = intents;
        _runs = runs;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<ReconciliationSummary>> Handle(
        RunReconciliationCommand request,
        CancellationToken cancellationToken)
    {
        var source = _sources.FirstOrDefault(s =>
            string.Equals(s.ProviderCode, request.ProviderCode, StringComparison.Ordinal));

        if (source is null)
        {
            return Error.NotFound(
                "reconciliation.source_not_configured",
                "No settlement source is configured for that provider.")
                .With("providerCode", request.ProviderCode);
        }

        Currency currency;
        try
        {
            currency = Currency.FromCode(request.CurrencyCode);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Validation("reconciliation.currency_unsupported", "That currency is not supported.")
                .With("currency", request.CurrencyCode);
        }

        var settlement = await source.GetSettlementAsync(request.BusinessDate, cancellationToken);
        if (settlement.IsFailure)
        {
            return Result.Failure<ReconciliationSummary>(settlement.Error);
        }

        var now = _clock.UtcNow;
        var run = ReconciliationRun.Start(request.ProviderCode, request.BusinessDate, currency, now);

        foreach (var line in settlement.Value)
        {
            var intent = await _intents.FindByProviderReferenceAsync(
                request.ProviderCode, line.ProviderReference, cancellationToken);

            if (intent is null)
            {
                // Money at the provider that this platform has no record of. The most urgent of the
                // four kinds: it is usually a capture whose webhook was never delivered, and until it
                // is posted the customer has paid and the deal has not moved.
                run.RecordDifference(ReconciliationDifference.Create(
                    run.Id, ReconciliationDifferenceKind.MissingInLedger, line.ProviderReference,
                    line.Amount, null, null,
                    "Settled at the provider with no payment attempt recorded."));

                continue;
            }

            if (intent.JournalId is null)
            {
                run.RecordDifference(ReconciliationDifference.Create(
                    run.Id, ReconciliationDifferenceKind.StatusMismatch, line.ProviderReference,
                    line.Amount, null, intent.Id,
                    "Settled at the provider but never captured here — the webhook was lost."));

                continue;
            }

            if (intent.Amount != line.Amount)
            {
                run.RecordDifference(ReconciliationDifference.Create(
                    run.Id, ReconciliationDifferenceKind.AmountMismatch, line.ProviderReference,
                    line.Amount, intent.Amount, intent.Id,
                    "The provider settled a different amount to the one posted."));

                continue;
            }

            run.RecordMatch(line.Amount);
        }

        run.Complete(_clock.UtcNow);

        await _runs.AddAsync(run, cancellationToken);
        await _runs.SaveChangesAsync(cancellationToken);

        if (run.Differences.Count > 0)
        {
            _logger.LogError(
                "Reconciliation {ProviderCode} {BusinessDate}: {DifferenceCount} differences over {MatchedCount} matches",
                request.ProviderCode, request.BusinessDate, run.Differences.Count, run.MatchedCount);
        }

        return new ReconciliationSummary(
            run.Id,
            run.MatchedCount,
            run.Differences.Count,
            run.ProviderTotal.MinorUnits,
            run.LedgerTotal.MinorUnits,
            currency.Code);
    }
}
