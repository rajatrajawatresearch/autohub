using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Platform.Integrations;

/// <summary>
/// "When this is true, try this provider, in this position."
/// </summary>
/// <remarks>
/// Routes are the whole registry. Several match a given call; they sort by
/// <see cref="Priority"/> into a chain, and the invoker walks it — primary, then first fallback,
/// then second. Migrating from Razorpay to Cashfree is three edits over three weeks: add Cashfree at
/// priority 20 behind Razorpay at 10, watch; move Cashfree to 5 for Pune only, watch; delete
/// Razorpay's route. At no point is there a deployment, and at every point there is a way back.
/// </remarks>
public sealed class ProviderRoute : AggregateRoot, IAuditable
{
    private ProviderRoute(Guid id, string capabilityCode, string providerCode, int priority) : base(id)
    {
        CapabilityCode = capabilityCode;
        ProviderCode = providerCode;
        Priority = priority;
    }

    private ProviderRoute() { }

    public string CapabilityCode { get; private set; } = string.Empty;

    public string ProviderCode { get; private set; } = string.Empty;

    /// <summary>
    /// Position in the chain. <b>Lower is tried first</b>, and gaps are encouraged — authoring at
    /// 10, 20, 30 leaves room to insert a provider at 15 without renumbering the others.
    /// </summary>
    /// <remarks>
    /// The opposite convention to the pricing registry, where higher priority wins, and the
    /// difference is not an inconsistency: pricing picks exactly one rule, so "highest wins" reads
    /// naturally; routing produces an ordered list, so "first is first" is the only convention that
    /// does not require mental arithmetic when reading a chain.
    /// </remarks>
    public int Priority { get; private set; }

    /// <summary>
    /// JsonLogic over the call context — city, amount, segment, payment method. Null always matches.
    /// </summary>
    /// <remarks>
    /// This is where "route UPI under ₹2,000 to the cheapest aggregator and cards to the one with
    /// the best success rate" lives. Both facts change monthly based on settlement reports, and
    /// neither should ever have been a branch in a payment handler.
    /// </remarks>
    public string? ConditionJson { get; private set; }

    /// <summary>
    /// Share of matching traffic this route takes, 1–100. Null means all of it.
    /// </summary>
    /// <remarks>
    /// A canary. Nobody sane moves 100% of payment volume to a new gateway on the strength of a
    /// sandbox integration — 5% for a day, read the success rate, then decide.
    /// <para>
    /// The split is computed from a deterministic hash of the call's affinity key rather than a
    /// random number, so a retried payment lands on the same provider as its first attempt. A random
    /// split would send the retry of a timed-out Razorpay charge to Cashfree, and the customer would
    /// be debited twice.
    /// </para>
    /// </remarks>
    public int? TrafficPercent { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>Lets a route be scheduled — a vendor's maintenance window, a contract's end date.</summary>
    public DateTimeOffset? EffectiveFrom { get; private set; }

    public DateTimeOffset? EffectiveTo { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static ProviderRoute Create(
        string capabilityCode,
        string providerCode,
        int priority,
        string? conditionJson = null)
        => new(UuidV7.New(), capabilityCode, providerCode, priority) { ConditionJson = conditionJson };

    public void SetCanary(int? trafficPercent) => TrafficPercent = trafficPercent;

    public void SetWindow(DateTimeOffset? from, DateTimeOffset? to)
    {
        EffectiveFrom = from;
        EffectiveTo = to;
    }

    public void Deactivate() => IsActive = false;

    public bool IsEffectiveAt(DateTimeOffset at)
        => IsActive
           && (EffectiveFrom is null || at >= EffectiveFrom)
           && (EffectiveTo is null || at < EffectiveTo);
}
