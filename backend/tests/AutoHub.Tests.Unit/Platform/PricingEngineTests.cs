using System.Text.Json.Nodes;
using AutoHub.Platform.Pricing;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoHub.Tests.Unit.Platform;

public class PricingEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 29, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Charges_a_flat_percentage_and_GST_on_the_commission_only()
    {
        var set = RuleSet.Draft("auction.win", 1, "Auction");
        var rule = new PricingRule("standard", "Standard auction commission", priority: 0);
        rule.AddComponent(new FeeComponent(
            "auction_commission", "Auction commission", FeeBasis.Percent, rate: 0.02m, sortOrder: 1));
        rule.AddComponent(new FeeComponent(
            "gst", "GST 18%", FeeBasis.PercentOfLines, rate: 0.18m,
            appliesToCsv: "auction_commission", sortOrder: 2));
        set.AddRule(rule);

        var quote = await QuoteAsync(set, Money.FromMinorUnits(84_735_000, Currency.Inr));

        quote.Lines.Should().HaveCount(2);
        quote.Lines[0].Amount.MinorUnits.Should().Be(1_694_700);

        // The GST line is charged on the commission, never on the vehicle's ₹8.47L price.
        // Getting this wrong is a 100× billing error, not a rounding one.
        quote.Lines[1].Amount.MinorUnits.Should().Be(305_046);
        quote.TotalFees.MinorUnits.Should().Be(1_999_746);
        quote.Payable.MinorUnits.Should().Be(86_734_746);
    }

    [Fact]
    public async Task The_higher_priority_rule_wins_and_only_that_rule_applies()
    {
        var set = RuleSet.Draft("auction.win", 1, "Auction");

        var standard = new PricingRule("standard", "Standard", priority: 0);
        standard.AddComponent(new FeeComponent("commission", "Commission", FeeBasis.Percent, rate: 0.02m));
        set.AddRule(standard);

        var gold = new PricingRule(
            "gold_dealer",
            "Gold dealer discount",
            priority: 100,
            conditionJson: """{"==":[{"var":"party.tier"},"gold"]}""",
            explanation: "Gold dealers pay 1.5% on auction wins.");
        gold.AddComponent(new FeeComponent("commission", "Commission", FeeBasis.Percent, rate: 0.015m));
        set.AddRule(gold);

        var quote = await QuoteAsync(
            set,
            Money.FromMinorUnits(10_000_000, Currency.Inr),
            new JsonObject { ["party"] = new JsonObject { ["tier"] = "gold" } });

        quote.AppliedRuleCode.Should().Be("gold_dealer");

        // Exactly one rule applies. Summing both would silently double the fee the day an operator
        // adds a discount rule and forgets that the base rule still matches.
        quote.Lines.Should().ContainSingle();
        quote.Lines[0].Amount.MinorUnits.Should().Be(150_000);
        quote.Explanation.Should().Contain("1.5%");
    }

    [Fact]
    public async Task Falls_back_to_the_base_rule_when_the_condition_does_not_match()
    {
        var set = RuleSet.Draft("auction.win", 1, "Auction");

        var standard = new PricingRule("standard", "Standard", priority: 0);
        standard.AddComponent(new FeeComponent("commission", "Commission", FeeBasis.Percent, rate: 0.02m));
        set.AddRule(standard);

        var gold = new PricingRule(
            "gold_dealer", "Gold", priority: 100,
            conditionJson: """{"==":[{"var":"party.tier"},"gold"]}""");
        gold.AddComponent(new FeeComponent("commission", "Commission", FeeBasis.Percent, rate: 0.015m));
        set.AddRule(gold);

        var quote = await QuoteAsync(
            set,
            Money.FromMinorUnits(10_000_000, Currency.Inr),
            new JsonObject { ["party"] = new JsonObject { ["tier"] = "silver" } });

        quote.AppliedRuleCode.Should().Be("standard");
        quote.Lines[0].Amount.MinorUnits.Should().Be(200_000);
    }

    [Fact]
    public async Task Slab_charges_the_rate_of_the_band_the_amount_lands_in()
    {
        var set = RuleSet.Draft("auction.win", 1, "Auction");
        var rule = new PricingRule("slabbed", "Slabbed", priority: 0);

        var component = new FeeComponent("commission", "Commission", FeeBasis.Slab);
        component.AddSlab(new FeeSlab(0, 50_000_00, rate: 0.03m, flatMinorUnits: null));
        component.AddSlab(new FeeSlab(50_000_00, 150_000_00, rate: 0.02m, flatMinorUnits: null));
        component.AddSlab(new FeeSlab(150_000_00, null, rate: 0.01m, flatMinorUnits: null));
        rule.AddComponent(component);
        set.AddRule(rule);

        var quote = await QuoteAsync(set, Money.FromMinorUnits(100_000_00, Currency.Inr));

        // ₹1,00,000 lands in the middle band, so the WHOLE amount is charged at 2% → ₹2,000.
        quote.Lines[0].Amount.MinorUnits.Should().Be(2_000_00);
    }

    [Fact]
    public async Task Tiered_charges_each_band_only_on_the_portion_inside_it()
    {
        var set = RuleSet.Draft("attachment.monthly", 1, "Attachment");
        var rule = new PricingRule("tiered", "Tiered", priority: 0);

        var component = new FeeComponent("commission", "Commission", FeeBasis.Tiered);
        component.AddSlab(new FeeSlab(0, 50_000_00, rate: 0.03m, flatMinorUnits: null));
        component.AddSlab(new FeeSlab(50_000_00, null, rate: 0.01m, flatMinorUnits: null));
        rule.AddComponent(component);
        set.AddRule(rule);

        var quote = await QuoteAsync(set, Money.FromMinorUnits(100_000_00, Currency.Inr));

        // ₹50,000 at 3% = ₹1,500, plus ₹50,000 at 1% = ₹500 → ₹2,000.
        // Identical to the slab result at this particular amount, which is exactly why the boundary
        // test below matters more than this one: tiered does not jump, so nobody prices one rupee
        // under a band edge to dodge a fee.
        quote.Lines[0].Amount.MinorUnits.Should().Be(2_000_00);
    }

    [Fact]
    public async Task Tiered_does_not_jump_at_a_band_boundary()
    {
        var set = RuleSet.Draft("attachment.monthly", 1, "Attachment");
        var rule = new PricingRule("tiered", "Tiered", priority: 0);

        var component = new FeeComponent("commission", "Commission", FeeBasis.Tiered);
        component.AddSlab(new FeeSlab(0, 50_000_00, rate: 0.03m, flatMinorUnits: null));
        component.AddSlab(new FeeSlab(50_000_00, null, rate: 0.01m, flatMinorUnits: null));
        rule.AddComponent(component);
        set.AddRule(rule);

        var justUnder = await QuoteAsync(set, Money.FromMinorUnits(50_000_00 - 1, Currency.Inr));
        var justOver = await QuoteAsync(set, Money.FromMinorUnits(50_000_00 + 1, Currency.Inr));

        var delta = justOver.TotalFees.MinorUnits - justUnder.TotalFees.MinorUnits;
        delta.Should().BeLessThan(10, "a two-paise change in base must not move the fee materially");
    }

    [Fact]
    public async Task A_floor_keeps_a_small_sale_from_costing_the_platform_money()
    {
        var set = RuleSet.Draft("sale.complete", 1, "Sale");
        var rule = new PricingRule("standard", "Standard", priority: 0);
        rule.AddComponent(new FeeComponent(
            "commission", "Commission", FeeBasis.Percent, rate: 0.02m,
            minMinorUnits: 199_00, maxMinorUnits: 25_000_00));
        set.AddRule(rule);

        // An old ₹9,000 scooter: 2% is ₹180, below the ₹199 floor, so the floor wins and the sale
        // still covers what it costs the platform to process.
        var scooter = await QuoteAsync(set, Money.FromMinorUnits(9_000_00, Currency.Inr));

        // A ₹2 crore bus: 2% would be ₹4,00,000, which the ₹25,000 cap trims to something sane.
        var bus = await QuoteAsync(set, Money.FromMinorUnits(2_00_00_000_00, Currency.Inr));

        scooter.Lines[0].Amount.MinorUnits.Should().Be(199_00);
        bus.Lines[0].Amount.MinorUnits.Should().Be(25_000_00);
    }

    [Fact]
    public async Task An_expired_rule_is_ignored()
    {
        var set = RuleSet.Draft("listing.create", 1, "Listing");

        var festive = new PricingRule(
            "festive_free", "Festive free listing", priority: 100,
            effectiveFrom: Now.AddDays(-30), effectiveTo: Now.AddDays(-1));
        festive.AddComponent(new FeeComponent("listing_fee", "Listing fee", FeeBasis.Flat, flatMinorUnits: 0));
        set.AddRule(festive);

        var standard = new PricingRule("standard", "Standard", priority: 0);
        standard.AddComponent(new FeeComponent(
            "listing_fee", "Listing fee", FeeBasis.Flat, flatMinorUnits: 499_00));
        set.AddRule(standard);

        var quote = await QuoteAsync(set, Money.FromMinorUnits(0, Currency.Inr));

        quote.AppliedRuleCode.Should().Be("standard");
        quote.Lines[0].Amount.MinorUnits.Should().Be(499_00);
    }

    [Fact]
    public async Task A_missing_rule_set_fails_loudly_rather_than_charging_nothing()
    {
        var store = new StubStore(null);
        var engine = new PricingEngine(store, new FixedClock(Now), NullLogger<PricingEngine>.Instance);

        var result = await engine.QuoteAsync(
            new PriceQuoteRequest
            {
                Scope = "auction.win",
                BaseAmount = Money.FromRupees(1000),
                Context = new JsonObject(),
            },
            CancellationToken.None);

        // Silently returning a zero fee would make revenue vanish without anyone noticing
        // until the month closed short.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("pricing.rule_set_missing");
    }

    [Fact]
    public async Task A_rule_whose_condition_will_not_parse_does_not_match()
    {
        var set = RuleSet.Draft("auction.win", 1, "Auction");

        var broken = new PricingRule("broken", "Broken", priority: 100, conditionJson: "not json at all");
        broken.AddComponent(new FeeComponent("commission", "Commission", FeeBasis.Percent, rate: 0.5m));
        set.AddRule(broken);

        var standard = new PricingRule("standard", "Standard", priority: 0);
        standard.AddComponent(new FeeComponent("commission", "Commission", FeeBasis.Percent, rate: 0.02m));
        set.AddRule(standard);

        var quote = await QuoteAsync(set, Money.FromMinorUnits(10_000_000, Currency.Inr));

        // "Does not apply" is the safe direction for a fee. The alternative is charging every
        // customer 50% under a rule nobody authored.
        quote.AppliedRuleCode.Should().Be("standard");
    }

    private static async Task<PriceQuote> QuoteAsync(RuleSet set, Money amount, JsonNode? context = null)
    {
        set.Publish(Now);
        var engine = new PricingEngine(
            new StubStore(set), new FixedClock(Now), NullLogger<PricingEngine>.Instance);

        var result = await engine.QuoteAsync(
            new PriceQuoteRequest
            {
                Scope = set.Scope,
                BaseAmount = amount,
                Context = context ?? new JsonObject(),
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: result.IsFailure ? result.Error.ToString() : string.Empty);
        return result.Value;
    }

    private sealed class StubStore(RuleSet? set) : IPricingRuleStore
    {
        public Task<RuleSet?> GetPublishedAsync(string scope, CancellationToken cancellationToken)
            => Task.FromResult(set);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;

        public DateOnly TodayInIst => DateOnly.FromDateTime(now.ToOffset(SystemClock.IstOffset).Date);
    }
}
