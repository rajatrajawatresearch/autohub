using AutoHub.Platform.Pricing;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #4 — all fifteen revenue streams in the PRD, as rows.
/// </summary>
/// <remarks>
/// Read as a table this is the platform's entire commercial model, and that is the point: "what does
/// a gold dealer in Maharashtra actually pay on an ₹8.4 lakh auction win?" is a query here and an
/// afternoon of grepping in a codebase that puts <c>* 0.02m</c> in a service.
/// <para>
/// <b>Every <see cref="FeeBasis"/> is exercised, deliberately.</b> Flat is the listing fee; Percent
/// with a floor and a ceiling is the buy/sell commission; Slab — one band decides the whole rate —
/// is the auction commission; Tiered — each band charges only on the slice inside it — is the loan
/// referral; PercentOfLines is GST, on every single rule set, charged on the commission and never on
/// the vehicle. Anything the engine can do is something an operator can author, and the only way to
/// know that stayed true is to have used all of it.
/// </para>
/// <para>
/// <b>Precedence is demonstrated rather than described.</b> <c>auction.win</c> carries three rules
/// over the same slab table: the standard one at priority 10, a gold-dealer discount at 100, and a
/// Maharashtra-specific gold rate at 200. Exactly one wins, the winning rule's
/// <see cref="PricingRule.Explanation"/> is what the dealer is shown, and adding a fourth rule for a
/// festive campaign changes nobody's fee except the one it names.
/// </para>
/// <para>
/// Amounts are minor units — paise — throughout, and rates are fractions, never percentages.
/// <c>0.18m</c> is eighteen percent; <c>18m</c> would be a hundred-fold error that the database's
/// <c>rate BETWEEN 0 AND 1</c> check constraint catches on the way in rather than on an invoice.
/// </para>
/// </remarks>
internal sealed class PricingSeeder
{
    private const int SeededVersion = 1;

    /// <summary>India's GST on a marketplace service. A row, so a rate change is a row change.</summary>
    private const decimal GstRate = 0.18m;

    private const string GstCode = "gst";

    private readonly AutoHubDbContext _context;
    private readonly IClock _clock;

    public PricingSeeder(AutoHubDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.PricingRuleSets
            .Select(ruleSet => ruleSet.Scope)
            .ToListAsync(cancellationToken);

        var known = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var build in RuleSets)
        {
            var ruleSet = build();

            if (known.Contains(ruleSet.Scope))
            {
                continue;
            }

            ruleSet.Publish(_clock.UtcNow);
            _context.PricingRuleSets.Add(ruleSet);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SeedStepResult("pricing", created, known.Count);
    }

    private static readonly Func<RuleSet>[] RuleSets =
    [
        BuildListingFee,
        BuildPremiumListing,
        BuildDealerSubscription,
        BuildAuctionCommission,
        BuildSaleCommission,
        BuildRentalCommission,
        BuildLeaseCommission,
        BuildAttachmentCommission,
        BuildLoanReferral,
        BuildInsuranceReferral,
        BuildFeaturedDealer,
        BuildAdBanner,
        BuildInspectionCharge,
        BuildValuationReport,
        BuildPremiumMembership,
    ];

    // ── 1. Listing fee ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Free for individuals, ₹199 for a dealer's listing.
    /// </summary>
    /// <remarks>
    /// The zero-rated individual rule is a rule and not an absence of one. "No matching rule" is an
    /// error in the pricing engine, by design — a scope that silently charges nothing is revenue
    /// quietly vanishing — so "this is free" has to be stated.
    /// </remarks>
    private static RuleSet BuildListingFee()
    {
        var ruleSet = RuleSet.Draft("listing.fee", SeededVersion, "Listing fee");

        var free = new PricingRule(
            "listing_fee_individual", "Listing fee — individual seller", 100,
            conditionJson: """{"==":[{"var":"party.type"},"individual"]}""",
            explanation: "Individual sellers list free. We make our money when the car sells.");
        free.AddComponent(new FeeComponent(
            "listing_fee", "Listing fee", FeeBasis.Flat, flatMinorUnits: 0, sortOrder: 1));
        AddGst(free, "listing_fee");
        ruleSet.AddRule(free);

        var dealer = new PricingRule(
            "listing_fee_standard", "Listing fee — standard", 10,
            explanation: "₹199 per listing, plus GST.");
        dealer.AddComponent(new FeeComponent(
            "listing_fee", "Listing fee", FeeBasis.Flat, flatMinorUnits: 19_900, sortOrder: 1));
        AddGst(dealer, "listing_fee");
        ruleSet.AddRule(dealer);

        return ruleSet;
    }

    // ── 2. Premium listing ───────────────────────────────────────────────────────────────────────

    private static RuleSet BuildPremiumListing()
    {
        var ruleSet = RuleSet.Draft("listing.premium", SeededVersion, "Premium listing placement");

        var rule = new PricingRule(
            "premium_listing_standard", "Premium listing — 30 days", 10,
            explanation: "₹999 for thirty days at the top of search, plus GST.");
        rule.AddComponent(new FeeComponent(
            "premium_listing", "Premium placement", FeeBasis.Flat, flatMinorUnits: 99_900, sortOrder: 1));
        AddGst(rule, "premium_listing");
        ruleSet.AddRule(rule);

        return ruleSet;
    }

    // ── 3. Dealer subscription ───────────────────────────────────────────────────────────────────

    private static RuleSet BuildDealerSubscription()
    {
        var ruleSet = RuleSet.Draft("dealer.subscription", SeededVersion, "Dealer subscription");

        var standard = new PricingRule(
            "dealer_subscription_standard", "Dealer subscription — standard", 10,
            explanation: "₹4,999 a month for up to fifty live listings, plus GST.");
        standard.AddComponent(new FeeComponent(
            "dealer_subscription", "Monthly subscription", FeeBasis.Flat,
            flatMinorUnits: 4_99_900, sortOrder: 1));
        AddGst(standard, "dealer_subscription");
        ruleSet.AddRule(standard);

        var gold = new PricingRule(
            "dealer_subscription_gold", "Dealer subscription — gold", 100,
            conditionJson: """{"==":[{"var":"party.tier"},"gold"]}""",
            explanation: "Gold dealers pay ₹3,999 a month with unlimited listings.");
        gold.AddComponent(new FeeComponent(
            "dealer_subscription", "Monthly subscription", FeeBasis.Flat,
            flatMinorUnits: 3_99_900, sortOrder: 1));
        AddGst(gold, "dealer_subscription");
        ruleSet.AddRule(gold);

        return ruleSet;
    }

    // ── 4. Auction commission — the precedence demonstration ─────────────────────────────────────

    /// <summary>The bands the auction commission is charged in, from ₹0 upward. Amounts in paise.</summary>
    private static readonly long[] AuctionBands =
        [0, 2_00_000_00, 5_00_000_00, 15_00_000_00, 50_00_000_00];

    private static RuleSet BuildAuctionCommission()
    {
        var ruleSet = RuleSet.Draft("auction.win", SeededVersion, "Auction commission");

        // Slab, not tiered: the band the hammer price lands in decides the rate for the whole
        // amount. Auction houses quote it that way and dealers expect it, and a tiered table here
        // would produce a fee nobody could reconcile against the invoice they are used to.
        ruleSet.AddRule(AuctionRule(
            "auction_commission_standard", "Auction commission — standard", 10, condition: null,
            rates: [0.030m, 0.025m, 0.020m, 0.015m, 0.010m],
            explanation: "Standard auction commission, by hammer-price band."));

        ruleSet.AddRule(AuctionRule(
            "auction_commission_gold", "Auction commission — gold dealer", 100,
            condition: """{"==":[{"var":"party.tier"},"gold"]}""",
            rates: [0.025m, 0.020m, 0.015m, 0.012m, 0.008m],
            explanation: "Gold dealers pay half a point less on every auction win."));

        // Narrower still, and higher priority, so it beats the plain gold rule. This is the rule the
        // published API contract quotes: 2% for a gold dealer in Maharashtra in the ₹5L–₹15L band.
        ruleSet.AddRule(AuctionRule(
            "auction_commission_gold_mh", "Auction — gold dealer, MH", 200,
            condition: """
                {"and":[
                  {"==":[{"var":"party.tier"},"gold"]},
                  {"==":[{"var":"location.state"},"MH"]}
                ]}
                """,
            rates: [0.030m, 0.025m, 0.020m, 0.015m, 0.010m],
            explanation: "Gold dealers in Maharashtra pay 2% on auction wins (slab ₹5L–₹15L)."));

        return ruleSet;
    }

    private static PricingRule AuctionRule(
        string code,
        string name,
        int priority,
        string? condition,
        IReadOnlyList<decimal> rates,
        string explanation)
    {
        var rule = new PricingRule(code, name, priority, condition, explanation: explanation);

        var commission = new FeeComponent(
            "auction_commission", "Auction commission", FeeBasis.Slab, sortOrder: 1);

        for (var band = 0; band < AuctionBands.Length; band++)
        {
            var upperBound = band + 1 < AuctionBands.Length ? AuctionBands[band + 1] : (long?)null;
            commission.AddSlab(new FeeSlab(AuctionBands[band], upperBound, rates[band], null));
        }

        rule.AddComponent(commission);
        AddGst(rule, "auction_commission");
        return rule;
    }

    // ── 5. Buy/sell commission — the floor-and-ceiling demonstration ──────────────────────────────

    /// <summary>
    /// 1.5% of the sale, but never below ₹2,000 and never above ₹25,000.
    /// </summary>
    /// <remarks>
    /// Both clamps earn their place. Without the floor, 1.5% of a ₹40,000 scooter is ₹600, which does
    /// not cover one inspection visit and one RTO runner. Without the ceiling, the same rule charges
    /// ₹1.5 lakh on a ₹1 crore car for identical work, and that dealer transacts somewhere else.
    /// </remarks>
    private static RuleSet BuildSaleCommission()
    {
        var ruleSet = RuleSet.Draft("sale.commission", SeededVersion, "Buy/sell commission");

        var rule = new PricingRule(
            "sale_commission_standard", "Sale commission — standard", 10,
            explanation: "1.5% of the sale price, minimum ₹2,000, capped at ₹25,000, plus GST.");

        rule.AddComponent(new FeeComponent(
            "sale_commission", "Sale commission", FeeBasis.Percent,
            rate: 0.015m,
            minMinorUnits: 2_000_00,
            maxMinorUnits: 25_000_00,
            sortOrder: 1));

        AddGst(rule, "sale_commission");
        ruleSet.AddRule(rule);

        return ruleSet;
    }

    // ── 6. Rental commission ─────────────────────────────────────────────────────────────────────

    private static RuleSet BuildRentalCommission()
    {
        var ruleSet = RuleSet.Draft("rental.booking", SeededVersion, "Rental commission");

        var rule = new PricingRule(
            "rental_commission_standard", "Rental commission — standard", 10,
            explanation: "10% of the rental value, plus GST.");
        rule.AddComponent(new FeeComponent(
            "rental_commission", "Rental commission", FeeBasis.Percent, rate: 0.10m,
            minMinorUnits: 250_00, sortOrder: 1));
        AddGst(rule, "rental_commission");
        ruleSet.AddRule(rule);

        var fleet = new PricingRule(
            "rental_commission_fleet", "Rental commission — fleet operator", 100,
            conditionJson: """{"==":[{"var":"party.type"},"fleet"]}""",
            explanation: "Fleet operators pay 7% on rental bookings.");
        fleet.AddComponent(new FeeComponent(
            "rental_commission", "Rental commission", FeeBasis.Percent, rate: 0.07m,
            minMinorUnits: 250_00, sortOrder: 1));
        AddGst(fleet, "rental_commission");
        ruleSet.AddRule(fleet);

        return ruleSet;
    }

    // ── 7. Lease commission ──────────────────────────────────────────────────────────────────────

    private static RuleSet BuildLeaseCommission()
    {
        var ruleSet = RuleSet.Draft("lease.origination", SeededVersion, "Lease origination fee");

        var rule = new PricingRule(
            "lease_origination_standard", "Lease origination — standard", 10,
            explanation: "3% of the total lease value at origination, capped at ₹75,000, plus GST.");
        rule.AddComponent(new FeeComponent(
            "lease_commission", "Lease origination fee", FeeBasis.Percent, rate: 0.03m,
            maxMinorUnits: 75_000_00, sortOrder: 1));
        AddGst(rule, "lease_commission");
        ruleSet.AddRule(rule);

        return ruleSet;
    }

    // ── 8. Taxi attachment commission ────────────────────────────────────────────────────────────

    private static RuleSet BuildAttachmentCommission()
    {
        var ruleSet = RuleSet.Draft("attachment.monthly", SeededVersion, "Taxi attachment fee");

        var rule = new PricingRule(
            "attachment_monthly_standard", "Attachment fee — monthly", 10,
            explanation: "₹1,500 a month per attached vehicle, plus GST.");
        rule.AddComponent(new FeeComponent(
            "attachment_fee", "Attachment fee", FeeBasis.Flat, flatMinorUnits: 1_500_00, sortOrder: 1));
        AddGst(rule, "attachment_fee");
        ruleSet.AddRule(rule);

        var largeFleet = new PricingRule(
            "attachment_monthly_fleet", "Attachment fee — fleet of ten or more", 100,
            conditionJson: """{">=":[{"var":"party.attachedVehicles"},10]}""",
            explanation: "Fleets running ten or more attached vehicles pay ₹1,200 each.");
        largeFleet.AddComponent(new FeeComponent(
            "attachment_fee", "Attachment fee", FeeBasis.Flat, flatMinorUnits: 1_200_00, sortOrder: 1));
        AddGst(largeFleet, "attachment_fee");
        ruleSet.AddRule(largeFleet);

        return ruleSet;
    }

    // ── 9. Loan referral — the tiered demonstration ──────────────────────────────────────────────

    /// <summary>
    /// A progressive referral fee: each band charges only on the slice of the loan inside it.
    /// </summary>
    /// <remarks>
    /// Tiered rather than slab, and the difference is the reason both bases exist. On a slab table a
    /// loan of ₹3,00,001 costs meaningfully more to refer than one of ₹3,00,000, so a lender's
    /// operations team quietly writes every sanction one rupee under the boundary. Tiered does not
    /// jump, so nobody has a reason to game it.
    /// </remarks>
    private static RuleSet BuildLoanReferral()
    {
        var ruleSet = RuleSet.Draft("loan.referral", SeededVersion, "Loan referral fee");

        var rule = new PricingRule(
            "loan_referral_standard", "Loan referral — standard", 10,
            explanation: "A progressive referral fee on the sanctioned amount, plus GST.");

        var referral = new FeeComponent("loan_referral", "Loan referral fee", FeeBasis.Tiered, sortOrder: 1);
        referral.AddSlab(new FeeSlab(0, 3_00_000_00, 0.012m, null));
        referral.AddSlab(new FeeSlab(3_00_000_00, 8_00_000_00, 0.009m, null));
        referral.AddSlab(new FeeSlab(8_00_000_00, 20_00_000_00, 0.007m, null));
        referral.AddSlab(new FeeSlab(20_00_000_00, null, 0.005m, null));
        rule.AddComponent(referral);

        AddGst(rule, "loan_referral");
        ruleSet.AddRule(rule);

        return ruleSet;
    }

    // ── 10. Insurance referral ───────────────────────────────────────────────────────────────────

    private static RuleSet BuildInsuranceReferral()
    {
        var ruleSet = RuleSet.Draft("insurance.referral", SeededVersion, "Insurance referral fee");

        var rule = new PricingRule(
            "insurance_referral_standard", "Insurance referral — standard", 10,
            explanation: "12% of the premium, within the IRDAI cap, plus GST.");
        rule.AddComponent(new FeeComponent(
            "insurance_referral", "Insurance referral fee", FeeBasis.Percent, rate: 0.12m,
            maxMinorUnits: 15_000_00, sortOrder: 1));
        AddGst(rule, "insurance_referral");
        ruleSet.AddRule(rule);

        return ruleSet;
    }

    // ── 11. Featured dealer ──────────────────────────────────────────────────────────────────────

    private static RuleSet BuildFeaturedDealer()
    {
        var ruleSet = RuleSet.Draft("dealer.featured", SeededVersion, "Featured dealer placement");

        var rule = new PricingRule(
            "featured_dealer_monthly", "Featured dealer — monthly", 10,
            explanation: "₹9,999 a month for city-page placement, plus GST.");
        rule.AddComponent(new FeeComponent(
            "featured_dealer", "Featured dealer placement", FeeBasis.Flat,
            flatMinorUnits: 9_999_00, sortOrder: 1));
        AddGst(rule, "featured_dealer");
        ruleSet.AddRule(rule);

        return ruleSet;
    }

    // ── 12. Ad banner ────────────────────────────────────────────────────────────────────────────

    private static RuleSet BuildAdBanner()
    {
        var ruleSet = RuleSet.Draft("ads.banner", SeededVersion, "Banner advertising");

        var rule = new PricingRule(
            "ad_banner_weekly", "Banner — weekly slot", 10,
            explanation: "₹24,999 for a week's homepage banner, plus GST.");
        rule.AddComponent(new FeeComponent(
            "ad_banner", "Banner slot", FeeBasis.Flat, flatMinorUnits: 24_999_00, sortOrder: 1));
        AddGst(rule, "ad_banner");
        ruleSet.AddRule(rule);

        return ruleSet;
    }

    // ── 13. Inspection charge — the city-override demonstration ──────────────────────────────────

    private static RuleSet BuildInspectionCharge()
    {
        var ruleSet = RuleSet.Draft("inspection.charge", SeededVersion, "Inspection charge");

        var standard = new PricingRule(
            "inspection_charge_standard", "Inspection — standard", 10,
            explanation: "₹1,499 for a 200-point inspection at your address, plus GST.");
        standard.AddComponent(new FeeComponent(
            "inspection_charge", "Inspection charge", FeeBasis.Flat,
            flatMinorUnits: 1_499_00, sortOrder: 1));
        AddGst(standard, "inspection_charge");
        ruleSet.AddRule(standard);

        // The city override the north star promises: "change the price in Pune only", authored as
        // one narrower rule rather than as a branch in an inspection service.
        var pune = new PricingRule(
            "inspection_charge_pune", "Inspection — Pune launch price", 100,
            conditionJson: """{"==":[{"var":"location.city"},"pune"]}""",
            explanation: "Launch price in Pune: ₹1,199 for a 200-point inspection.");
        pune.AddComponent(new FeeComponent(
            "inspection_charge", "Inspection charge", FeeBasis.Flat,
            flatMinorUnits: 1_199_00, sortOrder: 1));
        AddGst(pune, "inspection_charge");
        ruleSet.AddRule(pune);

        return ruleSet;
    }

    // ── 14. Valuation report ─────────────────────────────────────────────────────────────────────

    private static RuleSet BuildValuationReport()
    {
        var ruleSet = RuleSet.Draft("valuation.report", SeededVersion, "Detailed valuation report");

        var rule = new PricingRule(
            "valuation_report_standard", "Valuation report — standard", 10,
            explanation: "₹499 for the full comparable-sales report. The headline estimate stays free.");
        rule.AddComponent(new FeeComponent(
            "valuation_report", "Valuation report", FeeBasis.Flat, flatMinorUnits: 499_00, sortOrder: 1));
        AddGst(rule, "valuation_report");
        ruleSet.AddRule(rule);

        return ruleSet;
    }

    // ── 15. Premium membership ───────────────────────────────────────────────────────────────────

    private static RuleSet BuildPremiumMembership()
    {
        var ruleSet = RuleSet.Draft("membership.premium", SeededVersion, "Premium membership");

        var rule = new PricingRule(
            "membership_premium_annual", "Premium membership — annual", 10,
            explanation: "₹2,999 a year: free inspections, priority support and no listing fees.");
        rule.AddComponent(new FeeComponent(
            "premium_membership", "Premium membership", FeeBasis.Flat,
            flatMinorUnits: 2_999_00, sortOrder: 1));
        AddGst(rule, "premium_membership");
        ruleSet.AddRule(rule);

        return ruleSet;
    }

    /// <summary>
    /// GST, charged on the fee lines and never on the vehicle.
    /// </summary>
    /// <remarks>
    /// <see cref="FeeBasis.PercentOfLines"/> with an explicit <c>appliesTo</c> is what makes that
    /// true structurally. A percentage of the base amount would put 18% of a ₹8.4 lakh car on the
    /// invoice, which is not a rounding error — it is a number larger than the entire commission.
    /// </remarks>
    private static void AddGst(PricingRule rule, string appliesToCode)
        => rule.AddComponent(new FeeComponent(
            GstCode,
            "GST 18%",
            FeeBasis.PercentOfLines,
            rate: GstRate,
            appliesToCsv: appliesToCode,
            sortOrder: 99));
}
