using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Platform.Pricing;

/// <summary>
/// Every rupee the platform earns, expressed as data.
/// </summary>
/// <remarks>
/// The PRD lists fifteen revenue streams. Written as code, each is a percentage buried in a service
/// somewhere, and "what does a gold dealer in Pune actually pay on a ₹8L auction win?" becomes an
/// afternoon of grepping. Written here, it is a query — and changing it is a form, not a release.
/// <para>
/// A rule set is scoped to one commercial moment (<c>auction.win</c>, <c>rental.booking</c>,
/// <c>loan.referral</c>). Within it, rules compete: the most specific rule whose condition matches
/// wins, and its fee components produce the quote lines.
/// </para>
/// </remarks>
public sealed class RuleSet : AggregateRoot, IAuditable
{
    private readonly List<PricingRule> _rules = [];

    private RuleSet(Guid id, string scope, int version, string name) : base(id)
    {
        Scope = scope;
        Version = version;
        Name = name;
    }

    private RuleSet() { }

    /// <summary>The commercial moment, e.g. <c>auction.win</c>, <c>attachment.monthly</c>.</summary>
    public string Scope { get; private set; } = string.Empty;

    public int Version { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public IReadOnlyList<PricingRule> Rules => _rules;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static RuleSet Draft(string scope, int version, string name)
        => new(UuidV7.New(), scope, version, name);

    public void AddRule(PricingRule rule) => _rules.Add(rule);

    public void Publish(DateTimeOffset now)
    {
        IsPublished = true;
        PublishedAt = now;
    }

    /// <summary>
    /// Stands a published rule set down so its successor can take over.
    /// </summary>
    /// <remarks>
    /// Pricing differs from the workflow and form registries here, and the schema says so: a partial
    /// unique index enforces <b>exactly one published rule set per scope</b>. That is the right shape
    /// for money — "which rules am I being charged under?" must have one answer, not a highest-version
    /// tiebreak — and it means publishing v3 has to withdraw v2 in the same transaction.
    /// <para>
    /// The row is kept, never deleted. A quote issued last month names the version that produced it,
    /// and an invoice nobody can explain is worse than a table with a few dead rows in it.
    /// </para>
    /// <para>
    /// <see cref="PublishedAt"/> is cleared with the flag, because a check constraint ties the two
    /// together — the column means "when this became the live rule set", and a withdrawn row that
    /// still carries a date claims to be live in every report that reads it. When a rule set went
    /// live, and who published it, lives in the audit trail, which is append-only and is the right
    /// place for a history that must survive the row changing state.
    /// </para>
    /// </remarks>
    public void Withdraw()
    {
        IsPublished = false;
        PublishedAt = null;
    }
}

/// <summary>One priced outcome, and the conditions under which it applies.</summary>
public sealed class PricingRule
{
    public PricingRule(
        string code,
        string name,
        int priority,
        string? conditionJson = null,
        DateTimeOffset? effectiveFrom = null,
        DateTimeOffset? effectiveTo = null,
        string? explanation = null)
    {
        Code = code;
        Name = name;
        Priority = priority;
        ConditionJson = conditionJson;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Explanation = explanation;
    }

    private PricingRule() { }

    private readonly List<FeeComponent> _components = [];

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid RuleSetId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Higher wins. Specificity is authored, not inferred — a system that guesses which of two
    /// overlapping rules is "more specific" is a system nobody can predict the billing of.
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>JsonLogic over the quote context. Null means it always applies — the base case.</summary>
    public string? ConditionJson { get; private set; }

    public DateTimeOffset? EffectiveFrom { get; private set; }

    public DateTimeOffset? EffectiveTo { get; private set; }

    /// <summary>
    /// Plain-language reason shown to the payer. A dealer who cannot see why they were charged 2%
    /// opens a support ticket, so the explanation is part of the rule, not an afterthought.
    /// </summary>
    public string? Explanation { get; private set; }

    public IReadOnlyList<FeeComponent> Components => _components;

    public void AddComponent(FeeComponent component) => _components.Add(component);

    public bool IsEffectiveAt(DateTimeOffset at)
        => (EffectiveFrom is null || at >= EffectiveFrom)
           && (EffectiveTo is null || at < EffectiveTo);
}

/// <summary>One line on the quote.</summary>
public sealed class FeeComponent
{
    public FeeComponent(
        string code,
        string label,
        FeeBasis basis,
        decimal? rate = null,
        long? flatMinorUnits = null,
        long? minMinorUnits = null,
        long? maxMinorUnits = null,
        string? appliesToCsv = null,
        int sortOrder = 0)
    {
        Code = code;
        Label = label;
        Basis = basis;
        Rate = rate;
        FlatMinorUnits = flatMinorUnits;
        MinMinorUnits = minMinorUnits;
        MaxMinorUnits = maxMinorUnits;
        AppliesToCsv = appliesToCsv;
        SortOrder = sortOrder;
    }

    private FeeComponent() { }

    private readonly List<FeeSlab> _slabs = [];

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid PricingRuleId { get; private set; }

    /// <summary>Ledger-meaningful code, e.g. <c>auction_commission</c>, <c>gst</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public FeeBasis Basis { get; private set; }

    /// <summary>Fraction, not percent: 2% is <c>0.02m</c>. Percent-vs-fraction confusion is a 100× bug.</summary>
    public decimal? Rate { get; private set; }

    public long? FlatMinorUnits { get; private set; }

    /// <summary>Floor. A 2% commission on a ₹40,000 scooter should still cover the cost of the sale.</summary>
    public long? MinMinorUnits { get; private set; }

    /// <summary>Cap. Stops a percentage fee on a ₹2 crore bus from becoming absurd.</summary>
    public long? MaxMinorUnits { get; private set; }

    /// <summary>
    /// For <see cref="FeeBasis.PercentOfLines"/> — the component codes this is charged on.
    /// GST applies to the commission, not to the vehicle's price.
    /// </summary>
    public string? AppliesToCsv { get; private set; }

    public IReadOnlyList<FeeSlab> Slabs => _slabs;

    public int SortOrder { get; private set; }

    public void AddSlab(FeeSlab slab) => _slabs.Add(slab);

    public IReadOnlyList<string> AppliesTo()
        => string.IsNullOrWhiteSpace(AppliesToCsv)
            ? []
            : AppliesToCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>A band in a slab or tiered fee.</summary>
public sealed class FeeSlab
{
    public FeeSlab(long fromMinorUnits, long? toMinorUnits, decimal? rate, long? flatMinorUnits)
    {
        FromMinorUnits = fromMinorUnits;
        ToMinorUnits = toMinorUnits;
        Rate = rate;
        FlatMinorUnits = flatMinorUnits;
    }

    private FeeSlab() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid FeeComponentId { get; private set; }

    /// <summary>Inclusive lower bound.</summary>
    public long FromMinorUnits { get; private set; }

    /// <summary>Exclusive upper bound. Null is the open-ended top band.</summary>
    public long? ToMinorUnits { get; private set; }

    public decimal? Rate { get; private set; }

    public long? FlatMinorUnits { get; private set; }

    public bool Contains(long amount)
        => amount >= FromMinorUnits && (ToMinorUnits is null || amount < ToMinorUnits);
}

public enum FeeBasis
{
    /// <summary>A fixed amount. Listing fee, valuation report.</summary>
    Flat = 1,

    /// <summary><see cref="FeeComponent.Rate"/> × the base amount.</summary>
    Percent = 2,

    /// <summary>The band containing the base amount decides the rate or flat. Only that band applies.</summary>
    Slab = 3,

    /// <summary>
    /// Progressive: each band charges on the portion of the amount that falls inside it.
    /// This is how income tax works, and it is what people mean when a slab table "shouldn't jump".
    /// </summary>
    Tiered = 4,

    /// <summary>A rate on other lines. GST on commission.</summary>
    PercentOfLines = 5,
}
