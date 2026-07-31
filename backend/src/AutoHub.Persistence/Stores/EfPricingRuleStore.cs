using AutoHub.Platform.Pricing;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Loads the published rule set for a scope, whole.
/// </summary>
/// <remarks>
/// Rules, their fee components and the components' slabs all load together, because a quote needs
/// every one of them and a lazily missing slab does not fail — it silently computes a zero. Revenue
/// that quietly vanishes is the worst failure mode this store has, and eager loading removes it.
/// <para>
/// Untracked, because the caller caches the result and a tracked graph in a process-wide cache holds
/// a scoped <c>DbContext</c> alive and is shared across threads that will each try to change-track
/// it. Published rule sets are immutable, so there is nothing to track anyway.
/// </para>
/// </remarks>
public sealed class EfPricingRuleStore : IPricingRuleStore
{
    private readonly AutoHubDbContext _context;

    public EfPricingRuleStore(AutoHubDbContext context) => _context = context;

    public async Task<RuleSet?> GetPublishedAsync(string scope, CancellationToken cancellationToken)
        => await _context.PricingRuleSets
            .Include($"{nameof(RuleSet.Rules)}.{nameof(PricingRule.Components)}.{nameof(FeeComponent.Slabs)}")
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .Where(ruleSet => ruleSet.Scope == scope && ruleSet.IsPublished)
            .OrderByDescending(ruleSet => ruleSet.Version)
            .FirstOrDefaultAsync(cancellationToken);
}
