using AutoHub.Platform.Features;
using Microsoft.EntityFrameworkCore;
using Module = AutoHub.Platform.Features.Module;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #6 — which modules exist, for whom, and where.
/// </summary>
/// <remarks>
/// The launch plan in one table. Lease is the worked example the north star asks for: the module row
/// is enabled, and it is gated behind a flag whose only rule turns it on for dealers in Bengaluru.
/// Every other city sees a lease module that is off, and turning it on in Pune next month is one
/// more rule — no branch, no release, no second targeting engine.
/// <para>
/// <b>Flags default to off and are turned on by rule.</b> That way a rule set that fails to parse
/// or a condition nobody tested produces "the new thing did not appear", which is a support ticket,
/// rather than "the half-built thing appeared for everyone", which is an incident.
/// </para>
/// </remarks>
internal sealed class FeatureSeeder
{
    private readonly AutoHubDbContext _context;

    public FeatureSeeder(AutoHubDbContext context) => _context = context;

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var created = await SeedFlagsAsync(cancellationToken);
        created += await SeedModulesAsync(cancellationToken);

        var total = await _context.Modules.CountAsync(cancellationToken)
                    + await _context.FeatureFlags.CountAsync(cancellationToken);

        return new SeedStepResult("features", created, total - created);
    }

    // ── Feature flags ────────────────────────────────────────────────────────────────────────────

    private static readonly FeatureFlagRow[] Flags =
    [
        new("instant_offer", true,
            "Shows a buy-it-now price from AutoHub alongside the valuation. Off in cities with no procurement team."),
        new("ai_damage_detection", false,
            "Runs uploaded photos through the damage model. Off until the model beats a human inspector."),
        new("whatsapp_chat", true,
            "Routes buyer/seller chat through WhatsApp instead of in-app only."),
        new("proxy_bidding", true,
            "Lets a dealer set a maximum and have the system bid up to it."),
        new("challan_check", true,
            "Pulls outstanding traffic challans onto the listing detail page."),
        new("home_test_drive", true,
            "Offers a test drive at the buyer's address rather than the dealership."),
        new("bulk_upload", true,
            "The dealer CSV stock importer."),
        new("dealer_crm", true,
            "The dealer's lead pipeline and follow-up reminders."),
        new("premium_membership", true,
            "Sells the annual membership on the pricing page."),
        new("ev_battery_health", true,
            "Collects and displays battery health on electric listings."),
        new("rto_ocr", true,
            "Reads the RC from a photo instead of asking the seller to type it."),

        // Off by default; the rule below is what turns it on, and only where it should be.
        new("lease_pilot", false,
            "The lease pilot. On for dealers in Bengaluru only until the residual-value model is proven."),
    ];

    private async Task<int> SeedFlagsAsync(CancellationToken cancellationToken)
    {
        var known = (await _context.FeatureFlags.Select(flag => flag.Key).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in Flags.Where(row => !known.Contains(row.Key)))
        {
            var flag = SeedGuard.Required(
                FeatureFlag.Create(row.Key, row.DefaultValue, row.Description),
                $"feature flag {row.Key}");

            if (string.Equals(row.Key, "lease_pilot", StringComparison.Ordinal))
            {
                // "Launch Lease in Bengaluru only, to dealers only" — the north star's own example,
                // as one rule over the audience context rather than as a condition in a controller.
                SeedGuard.Required(
                    flag.AddRule(new FlagRule(
                        priority: 100,
                        conditionJson: """
                            {"and":[
                              {"==":[{"var":"_audience.city"},"bengaluru"]},
                              {"==":[{"var":"party.type"},"dealer"]}
                            ]}
                            """,
                        value: true,
                        description: "Bengaluru dealers, pilot cohort.")),
                    "feature flag lease_pilot rule");
            }

            if (string.Equals(row.Key, "instant_offer", StringComparison.Ordinal))
            {
                // A kill switch expressed as a narrow high-priority rule. Instant offer on a
                // commercial vehicle would quote a price from a model trained on cars, so it is off
                // for that segment and on for everyone else — which is what first-match-wins buys.
                SeedGuard.Required(
                    flag.AddRule(new FlagRule(
                        priority: 200,
                        conditionJson: """{"==":[{"var":"vehicle.category"},"commercial"]}""",
                        value: false,
                        description: "Instant offer is not modelled for commercial vehicles.")),
                    "feature flag instant_offer rule");
            }

            _context.FeatureFlags.Add(flag);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Modules ──────────────────────────────────────────────────────────────────────────────────

    private static readonly ModuleRow[] Modules =
    [
        new("catalog", "Catalog", true, null, 1, "Vehicles, taxonomy and search."),
        new("listings", "Listings", true, null, 2, "Offers to sell, rent, lease or attach a vehicle."),
        new("buy", "Buy", true, null, 3, "The buyer-facing marketplace."),
        new("sell", "Sell", true, null, 4, "The seller flow and instant offer."),
        new("auction", "Auction", true, null, 5, "Live dealer-to-dealer auctions."),
        new("rental", "Rental", true, null, 6, "Short-term self-drive and with-driver rental."),
        new("lease", "Lease", true, "lease_pilot", 7, "Long-term lease. Pilot only — see the gating flag."),
        new("attachment", "Taxi attachment", true, null, 8, "Attaching a vehicle to a fleet operator."),
        new("valuation", "Valuation", true, null, 9, "Price estimates and detailed reports."),
        new("inspection", "Inspection", true, null, 10, "The 200-point inspection and its report."),
        new("deals", "Deals", true, null, 11, "Orders, escrow and settlement."),
        new("finance", "Finance", true, null, 12, "The loan marketplace."),
        new("insurance", "Insurance", true, null, 13, "The motor insurance marketplace."),
        new("payments", "Payments", true, null, 14, "Collections, payouts and the ledger."),
        new("chat", "Chat", true, null, 15, "Buyer/seller conversations."),
        new("bookings", "Bookings", true, null, 16, "Test drives, inspections and deliveries."),
        new("documents", "Documents", true, null, 17, "RC, insurance, forms 29/30 and NOCs."),
        new("notifications", "Notifications", true, null, 18, "Push, SMS, WhatsApp, email and in-app."),
        new("reviews", "Reviews", true, null, 19, "Ratings for dealers and partners."),
        new("trust", "Trust and safety", true, null, 20, "KYC, blacklist and fraud signals."),
        new("identity", "Identity", true, null, 21, "Accounts, sessions and roles."),
        new("analytics", "Analytics", true, null, 22, "Dashboards and exports."),
    ];

    private async Task<int> SeedModulesAsync(CancellationToken cancellationToken)
    {
        var known = (await _context.Modules.Select(module => module.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in Modules.Where(row => !known.Contains(row.Code)))
        {
            var module = SeedGuard.Required(
                Module.Create(row.Code, row.Name, row.IsEnabled, row.Description),
                $"module {row.Code}");

            SeedGuard.Required(module.GateBehind(row.GatingFlagKey), $"module {row.Code} gate");
            module.Reorder(row.SortOrder);

            _context.Modules.Add(module);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }
}

internal sealed record FeatureFlagRow(string Key, bool DefaultValue, string Description);

internal sealed record ModuleRow(
    string Code,
    string Name,
    bool IsEnabled,
    string? GatingFlagKey,
    int SortOrder,
    string Description);
