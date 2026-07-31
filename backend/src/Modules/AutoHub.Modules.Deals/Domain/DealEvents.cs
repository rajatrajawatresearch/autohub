using AutoHub.SharedKernel.Domain;

namespace AutoHub.Modules.Deals.Domain;

/// <summary>
/// Everything the deal spine tells the rest of the platform.
/// </summary>
/// <remarks>
/// These are the seam between the settlement spine and the five modules that feed it. The auction
/// module does not call the payments module; it opens a deal, and payments reacts to
/// <see cref="DealSettled"/>. That indirection is what stops the auction's hot bid path from
/// carrying a gateway timeout, and it is why a payout can be retried without replaying a bid.
/// <para>
/// Amounts travel as minor units and a currency code rather than as a <c>Money</c>, because these
/// payloads are serialised into the outbox and read by consumers — partner webhooks, the analytics
/// pipeline, eventually another service — that do not share this codebase's types.
/// </para>
/// </remarks>
public sealed record DealOpened(
    Guid DealId,
    DateTimeOffset At,
    string IntentCode,
    string PricingScope,
    string? SourceType,
    Guid? SourceId,
    long BaseAmountMinorUnits,
    string Currency) : DomainEvent(DealId, At)
{
    public override string EventType => "deals.deal_opened.v1";
}

public sealed record DealPartyAttached(
    Guid DealId,
    DateTimeOffset At,
    Guid DealPartyId,
    string RoleCode,
    Guid? PartyId) : DomainEvent(DealId, At)
{
    public override string EventType => "deals.party_attached.v1";
}

public sealed record DealQuoted(
    Guid DealId,
    DateTimeOffset At,
    string PricingScope,
    string? AppliedRuleCode,
    int RuleSetVersion,
    long FeeTotalMinorUnits,
    long PayableMinorUnits,
    string Currency) : DomainEvent(DealId, At)
{
    public override string EventType => "deals.deal_quoted.v1";
}

public sealed record DealAccepted(
    Guid DealId,
    DateTimeOffset At,
    long PayableMinorUnits,
    string Currency) : DomainEvent(DealId, At)
{
    public override string EventType => "deals.deal_accepted.v1";
}

/// <summary>
/// The event the payments module posts a ledger journal from.
/// </summary>
/// <remarks>
/// <see cref="SettlementReference"/> is the idempotency key, carried on the event so a consumer that
/// receives it twice — which the outbox's at-least-once delivery guarantees will happen — posts one
/// journal rather than two.
/// </remarks>
public sealed record DealSettled(
    Guid DealId,
    DateTimeOffset At,
    string SettlementReference,
    string PricingScope,
    long PayableMinorUnits,
    string Currency) : DomainEvent(DealId, At)
{
    public override string EventType => "deals.deal_settled.v1";
}

public sealed record DealCancelled(
    Guid DealId,
    DateTimeOffset At,
    string Reason,
    long PayableMinorUnits,
    string Currency) : DomainEvent(DealId, At)
{
    public override string EventType => "deals.deal_cancelled.v1";
}
