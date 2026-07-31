using AutoHub.SharedKernel.Domain;

namespace AutoHub.Modules.Payments.Domain;

/// <summary>
/// A balanced posting hit the ledger.
/// </summary>
/// <remarks>
/// The event analytics, the dealer's statement feed and the reconciliation dashboard all consume.
/// It carries the idempotency key so a consumer that receives it twice — which at-least-once
/// delivery guarantees — can recognise the repeat rather than double-count it.
/// </remarks>
public sealed record JournalPosted(
    Guid JournalId,
    DateTimeOffset At,
    string Reference,
    string ScopeCode,
    string IdempotencyKey,
    long AmountMinorUnits,
    string Currency,
    string? SourceType,
    Guid? SourceId) : DomainEvent(JournalId, At)
{
    public override string EventType => "payments.journal_posted.v1";
}

/// <summary>Money confirmed captured at a gateway. The only status that may move a deal forward.</summary>
public sealed record PaymentCaptured(
    Guid PaymentIntentId,
    DateTimeOffset At,
    Guid? DealId,
    string ProviderCode,
    string ProviderReference,
    long AmountMinorUnits,
    string Currency) : DomainEvent(PaymentIntentId, At)
{
    public override string EventType => "payments.payment_captured.v1";
}

public sealed record PaymentFailed(
    Guid PaymentIntentId,
    DateTimeOffset At,
    Guid? DealId,
    string ProviderCode,
    string? FailureCode) : DomainEvent(PaymentIntentId, At)
{
    public override string EventType => "payments.payment_failed.v1";
}

public sealed record PayoutBatchInstructed(
    Guid PayoutBatchId,
    DateTimeOffset At,
    string Reference,
    int InstructionCount,
    long TotalMinorUnits,
    string Currency) : DomainEvent(PayoutBatchId, At)
{
    public override string EventType => "payments.payout_batch_instructed.v1";
}

/// <summary>
/// A reconciliation pass found the ledger and the gateway disagreeing.
/// </summary>
/// <remarks>
/// Raised per run rather than per difference: an outage produces thousands of differences and one
/// problem, and an alerting pipeline that receives thousands of events treats none of them as
/// urgent.
/// </remarks>
public sealed record ReconciliationCompleted(
    Guid ReconciliationRunId,
    DateTimeOffset At,
    string ProviderCode,
    DateOnly BusinessDate,
    int MatchedCount,
    int UnmatchedCount,
    long AbsoluteDifferenceMinorUnits,
    string Currency) : DomainEvent(ReconciliationRunId, At)
{
    public override string EventType => "payments.reconciliation_completed.v1";
}
