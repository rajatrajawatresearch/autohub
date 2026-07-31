using System.Text.Json.Nodes;
using AutoHub.Platform.Expressions;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Pricing;

public interface IPricingEngine
{
    /// <summary>
    /// Prices one commercial moment. Nothing in the product computes a fee itself; everything asks.
    /// </summary>
    Task<Result<PriceQuote>> QuoteAsync(PriceQuoteRequest request, CancellationToken cancellationToken);
}

public sealed record PriceQuoteRequest
{
    /// <summary>The rule set to evaluate, e.g. <c>auction.win</c>.</summary>
    public required string Scope { get; init; }

    public required Money BaseAmount { get; init; }

    /// <summary>
    /// The facts rules match on — vehicle, location, party, campaign. Shape is documented per scope;
    /// a rule referencing a fact the context does not carry simply does not match.
    /// </summary>
    public required JsonNode Context { get; init; }

    /// <summary>Lets an operator preview what a rule change will do before publishing it.</summary>
    public DateTimeOffset? AsOf { get; init; }
}

public sealed record PriceQuote
{
    public required string Scope { get; init; }

    public required Money BaseAmount { get; init; }

    public required IReadOnlyList<PriceLine> Lines { get; init; }

    public required Money TotalFees { get; init; }

    /// <summary>Base plus fees. What the payer actually parts with.</summary>
    public required Money Payable { get; init; }

    /// <summary>Which rule won, so support can answer "why this amount?" without reading code.</summary>
    public string? AppliedRuleCode { get; init; }

    public string? Explanation { get; init; }

    public int RuleSetVersion { get; init; }
}

public sealed record PriceLine(
    string Code,
    string Label,
    FeeBasis Basis,
    decimal? Rate,
    Money Amount,
    IReadOnlyList<string> AppliesTo);

/// <summary>Loads published rule sets. Implemented by the persistence layer, cached aggressively.</summary>
public interface IPricingRuleStore
{
    Task<RuleSet?> GetPublishedAsync(string scope, CancellationToken cancellationToken);
}

/// <summary>
/// Evaluates pricing rules into an explainable quote.
/// </summary>
public sealed class PricingEngine : IPricingEngine
{
    private readonly IPricingRuleStore _store;
    private readonly IClock _clock;
    private readonly ILogger<PricingEngine> _logger;

    public PricingEngine(IPricingRuleStore store, IClock clock, ILogger<PricingEngine> logger)
    {
        _store = store;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<PriceQuote>> QuoteAsync(
        PriceQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var ruleSet = await _store.GetPublishedAsync(request.Scope, cancellationToken);
        if (ruleSet is null)
        {
            // Silently charging nothing would be worse: revenue would quietly vanish and nobody
            // would notice until the month closed short.
            _logger.LogError("No published pricing rule set for scope {Scope}", request.Scope);
            return Error.Unexpected(
                "pricing.rule_set_missing",
                $"No published pricing rules for '{request.Scope}'.");
        }

        var asOf = request.AsOf ?? _clock.UtcNow;
        var context = EnrichContext(request);

        var applicable = ruleSet.Rules
            .Where(r => r.IsEffectiveAt(asOf))
            .Where(r => Matches(r, context))
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Code, StringComparer.Ordinal)
            .ToList();

        if (applicable.Count == 0)
        {
            return Error.Rule(
                "pricing.no_matching_rule",
                $"No pricing rule in '{request.Scope}' matches this transaction.")
                .With("scope", request.Scope);
        }

        // Exactly one rule wins. Summing every matching rule would make an operator's harmless-looking
        // new rule silently double an existing fee.
        var winner = applicable[0];

        if (applicable.Count > 1 && applicable[1].Priority == winner.Priority)
        {
            _logger.LogWarning(
                "Pricing scope {Scope}: rules {Winner} and {Runner} tie at priority {Priority}; " +
                "{Winner} chosen by code order",
                request.Scope, winner.Code, applicable[1].Code, winner.Priority, winner.Code);
        }

        var lines = ComputeLines(winner, request.BaseAmount);

        var totalFees = lines.Aggregate(
            Money.Zero(request.BaseAmount.Currency),
            (sum, line) => sum + line.Amount);

        return new PriceQuote
        {
            Scope = request.Scope,
            BaseAmount = request.BaseAmount,
            Lines = lines,
            TotalFees = totalFees,
            Payable = request.BaseAmount + totalFees,
            AppliedRuleCode = winner.Code,
            Explanation = winner.Explanation,
            RuleSetVersion = ruleSet.Version,
        };
    }

    private static List<PriceLine> ComputeLines(PricingRule rule, Money baseAmount)
    {
        var lines = new List<PriceLine>();

        foreach (var component in rule.Components.OrderBy(c => c.SortOrder))
        {
            var amount = component.Basis switch
            {
                FeeBasis.Flat => Money.FromMinorUnits(component.FlatMinorUnits ?? 0, baseAmount.Currency),
                FeeBasis.Percent => baseAmount.MultiplyByRate(component.Rate ?? 0m),
                FeeBasis.Slab => ComputeSlab(component, baseAmount),
                FeeBasis.Tiered => ComputeTiered(component, baseAmount),
                FeeBasis.PercentOfLines => ComputeOnLines(component, lines, baseAmount.Currency),
                _ => Money.Zero(baseAmount.Currency),
            };

            amount = amount.Clamp(
                component.MinMinorUnits is { } min ? Money.FromMinorUnits(min, baseAmount.Currency) : null,
                component.MaxMinorUnits is { } max ? Money.FromMinorUnits(max, baseAmount.Currency) : null);

            lines.Add(new PriceLine(
                component.Code,
                component.Label,
                component.Basis,
                component.Rate,
                amount,
                component.AppliesTo()));
        }

        return lines;
    }

    /// <summary>The band containing the amount decides the whole fee.</summary>
    private static Money ComputeSlab(FeeComponent component, Money baseAmount)
    {
        var slab = component.Slabs.FirstOrDefault(s => s.Contains(baseAmount.MinorUnits));

        if (slab is null)
        {
            return Money.Zero(baseAmount.Currency);
        }

        return slab.FlatMinorUnits is { } flat
            ? Money.FromMinorUnits(flat, baseAmount.Currency)
            : baseAmount.MultiplyByRate(slab.Rate ?? 0m);
    }

    /// <summary>
    /// Progressive: each band charges only on the portion of the amount inside it.
    /// </summary>
    /// <remarks>
    /// The difference from <see cref="ComputeSlab"/> is the whole reason both exist. A flat slab
    /// jumps — one rupee more on the base can cost thousands more in fee, and dealers price
    /// exactly one rupee below the boundary. Tiered does not jump.
    /// </remarks>
    private static Money ComputeTiered(FeeComponent component, Money baseAmount)
    {
        var total = 0L;

        foreach (var slab in component.Slabs.OrderBy(s => s.FromMinorUnits))
        {
            if (baseAmount.MinorUnits <= slab.FromMinorUnits)
            {
                break;
            }

            var upper = slab.ToMinorUnits ?? baseAmount.MinorUnits;
            var portion = Math.Min(baseAmount.MinorUnits, upper) - slab.FromMinorUnits;

            if (portion <= 0)
            {
                continue;
            }

            var slice = Money.FromMinorUnits(portion, baseAmount.Currency);

            total += slab.FlatMinorUnits
                     ?? slice.MultiplyByRate(slab.Rate ?? 0m).MinorUnits;
        }

        return Money.FromMinorUnits(total, baseAmount.Currency);
    }

    /// <summary>A rate charged on other lines — GST on commission, never on the vehicle price.</summary>
    private static Money ComputeOnLines(
        FeeComponent component,
        IReadOnlyList<PriceLine> lines,
        Currency currency)
    {
        var targets = component.AppliesTo();

        var basis = lines
            .Where(l => targets.Count == 0 || targets.Contains(l.Code, StringComparer.Ordinal))
            .Aggregate(Money.Zero(currency), (sum, line) => sum + line.Amount);

        return basis.MultiplyByRate(component.Rate ?? 0m);
    }

    private static bool Matches(PricingRule rule, JsonNode context)
    {
        var condition = ConditionExpression.FromJson(rule.ConditionJson);

        // An unparseable condition must not match. The safe direction for a fee is "does not apply";
        // the alternative is charging every customer under a rule nobody authored.
        return condition.IsSuccess && condition.Value.Evaluate(context);
    }

    /// <summary>
    /// Adds the amount under a reserved key so rules can band on it without the caller having to
    /// remember to include it, and without a caller being able to spoof it.
    /// </summary>
    private static JsonObject EnrichContext(PriceQuoteRequest request)
    {
        var context = request.Context.DeepClone() as JsonObject ?? [];

        context["_amount"] = new JsonObject
        {
            ["minorUnits"] = request.BaseAmount.MinorUnits,
            ["currency"] = request.BaseAmount.Currency.Code,
        };

        return context;
    }
}
