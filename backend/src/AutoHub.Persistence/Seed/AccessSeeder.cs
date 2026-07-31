using AutoHub.Platform.Access;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #5 — permissions, roles, and nothing that checks a role name.
/// </summary>
/// <remarks>
/// The split this seeder keeps is the whole design. <b>Permissions are code's vocabulary</b>: each
/// one exists because a line of code, a workflow transition or an endpoint compares against it, so
/// they are declared in <see cref="SeedPermissionCatalogue"/> and projected into rows. <b>Roles are
/// an operator's grouping</b>: which permissions a Dealer Owner holds is a decision the business
/// makes on a Tuesday, and it is a row, not a release.
/// <para>
/// Notice what is absent — there is no <c>if (role == "dealer")</c> anywhere for this data to feed.
/// The eleven roles below are eleven bundles, and a twelfth ("Regional Auction Moderator — West") is
/// not a new role at all: it is <c>auction_moderator</c> assigned with
/// <see cref="AccessScopeType.City"/> scope, once per city. That is why scope lives on the
/// assignment and never in the role name.
/// </para>
/// <para>
/// Permission rows are reconciled rather than inserted-and-forgotten: a code the catalogue declares
/// but the table lacks is created, and its description is refreshed if it changed. Codes are never
/// renamed, because a rename orphans every grant that references it while the admin screen keeps
/// looking correct.
/// </para>
/// </remarks>
internal sealed class AccessSeeder
{
    private readonly AutoHubDbContext _context;
    private readonly IClock _clock;

    public AccessSeeder(AutoHubDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var created = await SeedPermissionsAsync(cancellationToken);
        created += await SeedRolesAsync(cancellationToken);

        var total = await _context.Permissions.CountAsync(cancellationToken)
                    + await _context.Roles.CountAsync(cancellationToken);

        return new SeedStepResult("rbac", created, total - created);
    }

    private async Task<int> SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.Permissions
            .ToDictionaryAsync(permission => permission.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var created = 0;

        foreach (var definition in SeedPermissionCatalogue.All)
        {
            if (existing.TryGetValue(definition.Code.Value, out var permission))
            {
                // Wording drifts as the product settles; the code never does. Refreshing the label
                // keeps the admin matrix readable without touching a single grant.
                SeedGuard.Required(
                    permission.Describe(definition.Name, definition.Description, definition.IsDangerous),
                    $"permission {definition.Code.Value}");

                continue;
            }

            _context.Permissions.Add(Permission.FromDefinition(definition));
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<int> SeedRolesAsync(CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions
            .ToDictionaryAsync(permission => permission.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existing = await _context.Roles
            .Include(nameof(Role.Permissions))
            .ToDictionaryAsync(role => role.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var now = _clock.UtcNow;
        var created = 0;

        foreach (var row in SeedRoles.All)
        {
            if (!existing.TryGetValue(row.Code, out var role))
            {
                role = SeedGuard.Required(
                    Role.Create(row.Code, row.Name, row.Description, isSystem: true),
                    $"role {row.Code}");

                _context.Roles.Add(role);
                created++;
            }

            // Grants are additive on re-run and re-granting is a no-op, so a role an operator has
            // widened by hand is not narrowed back by a redeploy of the seeder. Taking a permission
            // away is a deliberate act in the admin panel, never a side effect of a restart.
            foreach (var code in row.PermissionCodes)
            {
                // A role granting a permission nobody declares is a checkbox that grants nothing —
                // the failure mode where an access review concludes a team has access it does not.
                if (!permissions.TryGetValue(code, out var permission))
                {
                    throw new InvalidOperationException(
                        $"Role '{row.Code}' grants '{code}', which no permission declares.");
                }

                SeedGuard.Required(
                    role.Grant(permission.Id, now, actorId: null),
                    $"role {row.Code} grant {code}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }
}

/// <summary>
/// Every permission this build's registries, workflows and endpoints check.
/// </summary>
/// <remarks>
/// This is a <i>seed-time</i> catalogue rather than a set of <see cref="IPermissionContributor"/>
/// implementations, and the difference matters. A contributor belongs to the module that owns the
/// code doing the checking — the auction module declares <c>auction.bid.place</c> — and those
/// modules are being written now. Declaring them here would put the auction team's vocabulary in the
/// persistence layer permanently.
/// <para>
/// So this list is temporary scaffolding with a built-in expiry: as each module ships its own
/// contributor, <see cref="PermissionCatalog.Reconcile"/> reports the overlap, and the entries here
/// are deleted. Until then the demo needs a permission matrix that is not empty, and the workflow
/// transitions seeded alongside it need the permissions they name to exist.
/// </para>
/// </remarks>
internal static class SeedPermissionCatalogue
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        // Platform registries — these are already owned by code and declared properly.
        .. new PlatformPermissionContributor().Permissions,

        // ── Catalog ──────────────────────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("catalog.vehicle.read", "View vehicle records"),
        PermissionDefinition.Declare("catalog.vehicle.write", "Edit vehicle records"),

        // ── Listings ─────────────────────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("listing.create", "Create a listing"),
        PermissionDefinition.Declare("listing.update", "Edit a listing"),
        PermissionDefinition.Declare("listing.submit", "Submit a listing for review"),
        PermissionDefinition.Declare("listing.moderate", "Approve or reject listings",
            description: "Holds the gate between a seller's draft and a public listing."),
        PermissionDefinition.Declare("listing.publish", "Publish a listing directly",
            description: "Bypasses review. Granted to dealers whose listings are auto-approved."),
        PermissionDefinition.Declare("listing.reserve", "Reserve or release a listing"),
        PermissionDefinition.Declare("listing.close", "Mark a listing sold"),
        PermissionDefinition.Declare("listing.withdraw", "Withdraw a listing"),
        PermissionDefinition.Declare("listing.read_any", "View any listing in any state",
            description: "Support and moderation. A customer only ever sees their own drafts."),

        // ── Dealer ───────────────────────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("dealer.profile.manage", "Edit the dealership profile"),
        PermissionDefinition.Declare("dealer.staff.manage", "Add and remove dealership staff"),
        PermissionDefinition.Declare("dealer.inventory.read", "View dealership stock"),
        PermissionDefinition.Declare("dealer.inventory.write", "Edit dealership stock"),
        PermissionDefinition.Declare("dealer.inventory.bulk_import", "Bulk-import stock"),
        PermissionDefinition.Declare("dealer.kyc.approve", "Approve a dealer's KYC", isDangerous: true,
            description: "A verified dealer can bid and be paid out. This is the gate to both."),

        // ── Auction ──────────────────────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("auction.event.create", "Create an auction event"),
        PermissionDefinition.Declare("auction.event.moderate", "Moderate a running auction"),
        PermissionDefinition.Declare("auction.lot.create", "Add a lot to an auction"),
        PermissionDefinition.Declare("auction.lot.view", "View auction lots"),
        PermissionDefinition.Declare("auction.lot.open", "Open a lot for bidding"),
        PermissionDefinition.Declare("auction.lot.close", "Close a lot"),
        PermissionDefinition.Declare("auction.lot.cancel", "Cancel a lot", isDangerous: true),
        PermissionDefinition.Declare("auction.lot.approve_below_reserve",
            "Accept a bid below the reserve", isDangerous: true,
            description: "Sells the seller's vehicle for less than they asked. Always a deliberate call."),
        PermissionDefinition.Declare("auction.bid.place", "Place a bid"),
        PermissionDefinition.Declare("auction.bid.retract", "Retract a bid", isDangerous: true),

        // ── Deals ────────────────────────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("deal.view", "View a deal"),
        PermissionDefinition.Declare("deal.confirm", "Confirm a deal"),
        PermissionDefinition.Declare("deal.cancel", "Cancel a deal"),
        PermissionDefinition.Declare("deal.payment.record", "Record a payment against a deal"),
        PermissionDefinition.Declare("deal.document.manage", "Manage transfer paperwork"),
        PermissionDefinition.Declare("deal.document.verify", "Verify transfer paperwork"),
        PermissionDefinition.Declare("deal.refund", "Refund a deal", isDangerous: true),
        PermissionDefinition.Declare("deal.settle", "Settle a deal to the ledger", isDangerous: true),
        PermissionDefinition.Declare("deal.close", "Close a completed deal"),

        // ── Payments ─────────────────────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("payment.view", "View payments"),
        PermissionDefinition.Declare("payment.collect.initiate", "Start a collection"),
        PermissionDefinition.Declare("payment.refund.issue", "Issue a refund", isDangerous: true),
        PermissionDefinition.Declare("payment.payout.approve", "Approve a payout", isDangerous: true),
        PermissionDefinition.Declare("payment.payout.release", "Release an approved payout",
            isDangerous: true,
            description: "Deliberately separate from approval so no one person can both authorise and move money."),

        // ── Bookings ─────────────────────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("booking.view", "View bookings"),
        PermissionDefinition.Declare("booking.confirm", "Confirm a booking"),
        PermissionDefinition.Declare("booking.assign", "Assign a booking to a partner"),
        PermissionDefinition.Declare("booking.start", "Start a booking"),
        PermissionDefinition.Declare("booking.cancel", "Cancel a booking"),
        PermissionDefinition.Declare("booking.complete", "Complete a booking"),

        // ── Rental, lease, attachment ────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("rental.agreement.issue", "Issue a rental agreement"),
        PermissionDefinition.Declare("rental.agreement.activate", "Activate a rental agreement"),
        PermissionDefinition.Declare("rental.agreement.return", "Record a rental return"),
        PermissionDefinition.Declare("rental.agreement.cancel", "Cancel a rental agreement"),
        PermissionDefinition.Declare("rental.agreement.close", "Close and settle a rental"),
        PermissionDefinition.Declare("lease.contract.manage", "Manage lease contracts"),
        PermissionDefinition.Declare("attachment.contract.submit", "Submit an attachment contract"),
        PermissionDefinition.Declare("attachment.contract.verify", "Verify an attachment contract"),
        PermissionDefinition.Declare("attachment.contract.suspend", "Suspend or reinstate an attachment"),
        PermissionDefinition.Declare("attachment.contract.terminate", "Terminate an attachment",
            isDangerous: true),
        PermissionDefinition.Declare("attachment.contract.close", "Complete an attachment term"),

        // ── Valuation and inspection ─────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("valuation.quote.create", "Request a valuation"),
        PermissionDefinition.Declare("valuation.report.purchase", "Buy a detailed valuation report"),
        PermissionDefinition.Declare("valuation.model.manage", "Manage valuation models", isDangerous: true),
        PermissionDefinition.Declare("inspection.book", "Book an inspection"),
        PermissionDefinition.Declare("inspection.assign", "Assign an inspection to an inspector"),
        PermissionDefinition.Declare("inspection.perform", "Perform an inspection"),
        PermissionDefinition.Declare("inspection.report.view", "View inspection reports"),

        // ── Finance and insurance ────────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("finance.loan.submit", "Submit a loan application"),
        PermissionDefinition.Declare("finance.loan.withdraw", "Withdraw a loan application"),
        PermissionDefinition.Declare("finance.loan.route", "Route an application to lenders"),
        PermissionDefinition.Declare("finance.loan.decide", "Record a lender's decision"),
        PermissionDefinition.Declare("finance.loan.disburse", "Record a disbursal", isDangerous: true),
        PermissionDefinition.Declare("finance.lender.manage", "Manage lender partners"),
        PermissionDefinition.Declare("insurance.quote.create", "Request an insurance quote"),
        PermissionDefinition.Declare("insurance.partner.manage", "Manage insurance partners"),

        // ── Chat, documents, reviews ─────────────────────────────────────────────────────────────
        PermissionDefinition.Declare("chat.thread.read", "Read a chat thread"),
        PermissionDefinition.Declare("chat.message.send", "Send a chat message"),
        PermissionDefinition.Declare("chat.thread.moderate", "Moderate a chat thread"),
        PermissionDefinition.Declare("document.view", "View documents"),
        PermissionDefinition.Declare("document.upload", "Upload documents"),
        PermissionDefinition.Declare("document.verify", "Verify documents"),
        PermissionDefinition.Declare("review.create", "Leave a review"),
        PermissionDefinition.Declare("review.moderate", "Moderate reviews"),

        // ── Trust, identity, analytics ───────────────────────────────────────────────────────────
        PermissionDefinition.Declare("trust.kyc.review", "Review a KYC case"),
        PermissionDefinition.Declare("trust.kyc.approve", "Approve or reject a KYC case", isDangerous: true),
        PermissionDefinition.Declare("trust.blacklist.manage", "Manage the blacklist", isDangerous: true),
        PermissionDefinition.Declare("trust.fraud.review", "Review fraud signals"),
        PermissionDefinition.Declare("identity.user.view", "View user accounts"),
        PermissionDefinition.Declare("identity.user.suspend", "Suspend a user", isDangerous: true),
        PermissionDefinition.Declare("identity.user.impersonate", "Impersonate a user", isDangerous: true,
            description: "Support's last resort. Every impersonated action is attributed to both parties."),
        PermissionDefinition.Declare("analytics.dashboard.view", "View analytics dashboards"),
        PermissionDefinition.Declare("analytics.export.run", "Export analytics data"),
        PermissionDefinition.Declare("system.audit.read", "Read the audit trail"),
        PermissionDefinition.Declare("system.integration.configure", "Configure integrations",
            isDangerous: true),
        PermissionDefinition.Declare("system.role.assign", "Assign roles to users", isDangerous: true),
    ];

    public static IReadOnlyList<string> AllCodes { get; } =
        All.Select(definition => definition.Code.Value).ToList();
}

/// <summary>The eleven roles the product ships with, and what each one may actually do.</summary>
internal static class SeedRoles
{
    public const string Customer = "customer";
    public const string DealerOwner = "dealer_owner";
    public const string DealerStaff = "dealer_staff";
    public const string FleetOwner = "fleet_owner";
    public const string Broker = "broker";
    public const string Inspector = "inspector";
    public const string SupportExecutive = "support_executive";
    public const string DeliveryExecutive = "delivery_executive";
    public const string AuctionModerator = "auction_moderator";
    public const string Admin = "admin";
    public const string SuperAdmin = "super_admin";

    private static readonly string[] CustomerPermissions =
    [
        "catalog.vehicle.read",
        "listing.create", "listing.update", "listing.submit", "listing.withdraw", "listing.close",
        "valuation.quote.create", "valuation.report.purchase",
        "inspection.book", "inspection.report.view",
        "booking.view", "booking.confirm", "booking.cancel",
        "deal.view", "deal.confirm", "deal.cancel",
        "payment.view",
        "chat.thread.read", "chat.message.send",
        "document.view", "document.upload",
        "finance.loan.submit", "finance.loan.withdraw",
        "insurance.quote.create",
        "review.create",
        "auction.lot.view",
    ];

    private static readonly string[] DealerStaffPermissions =
    [
        "catalog.vehicle.read", "catalog.vehicle.write",
        "dealer.inventory.read", "dealer.inventory.write",
        "listing.create", "listing.update", "listing.submit", "listing.withdraw",
        "listing.reserve", "listing.close",
        "booking.view", "booking.confirm", "booking.cancel",
        "deal.view", "deal.confirm",
        "chat.thread.read", "chat.message.send",
        "document.view", "document.upload",
        "auction.lot.view",
        "valuation.quote.create",
    ];

    private static readonly string[] DealerOwnerPermissions =
    [
        .. DealerStaffPermissions,
        "dealer.profile.manage", "dealer.staff.manage", "dealer.inventory.bulk_import",
        "listing.publish",
        "auction.bid.place", "auction.bid.retract",
        "deal.cancel", "deal.payment.record", "deal.document.manage",
        "payment.view",
        "finance.loan.submit", "insurance.quote.create",
        "inspection.book", "inspection.report.view",
        "analytics.dashboard.view",
    ];

    private static readonly string[] FleetOwnerPermissions =
    [
        "catalog.vehicle.read", "catalog.vehicle.write",
        "listing.create", "listing.update", "listing.submit", "listing.withdraw",
        "rental.agreement.issue", "rental.agreement.activate", "rental.agreement.return",
        "rental.agreement.cancel", "rental.agreement.close",
        "attachment.contract.submit",
        "lease.contract.manage",
        "booking.view", "booking.confirm", "booking.cancel", "booking.assign",
        "deal.view", "payment.view",
        "chat.thread.read", "chat.message.send",
        "document.view", "document.upload",
        "analytics.dashboard.view",
    ];

    private static readonly string[] BrokerPermissions =
    [
        "catalog.vehicle.read",
        "listing.create", "listing.update", "listing.submit", "listing.withdraw",
        "valuation.quote.create",
        "deal.view",
        "chat.thread.read", "chat.message.send",
        "document.view", "document.upload",
        "booking.view",
    ];

    private static readonly string[] InspectorPermissions =
    [
        "catalog.vehicle.read",
        "listing.read_any",
        "inspection.perform", "inspection.report.view",
        "booking.view", "booking.start", "booking.complete",
        "document.view", "document.upload",
    ];

    private static readonly string[] SupportExecutivePermissions =
    [
        "catalog.vehicle.read",
        "listing.read_any",
        "deal.view", "payment.view",
        "booking.view", "booking.cancel",
        "chat.thread.read", "chat.thread.moderate",
        "document.view",
        "identity.user.view",
        "inspection.report.view",
        "auction.lot.view",
    ];

    private static readonly string[] DeliveryExecutivePermissions =
    [
        "listing.read_any",
        "booking.view", "booking.start", "booking.complete",
        "document.view", "document.upload",
    ];

    private static readonly string[] AuctionModeratorPermissions =
    [
        "catalog.vehicle.read",
        "listing.read_any",
        "auction.event.create", "auction.event.moderate",
        "auction.lot.create", "auction.lot.view", "auction.lot.open", "auction.lot.close",
        "auction.lot.cancel", "auction.lot.approve_below_reserve",
        "auction.bid.retract",
        "deal.view",
        "inspection.report.view",
    ];

    private static readonly string[] AdminPermissions =
    [
        .. SupportExecutivePermissions,
        .. AuctionModeratorPermissions,
        "listing.moderate", "listing.publish", "listing.reserve", "listing.close", "listing.withdraw",
        "catalog.vehicle.write",
        "dealer.profile.manage", "dealer.staff.manage", "dealer.kyc.approve",
        "dealer.inventory.read", "dealer.inventory.write", "dealer.inventory.bulk_import",
        "deal.confirm", "deal.cancel", "deal.payment.record", "deal.document.manage",
        "deal.document.verify", "deal.close",
        "payment.collect.initiate", "payment.payout.approve",
        "booking.confirm", "booking.assign",
        "inspection.book", "inspection.assign",
        "rental.agreement.issue", "rental.agreement.activate", "rental.agreement.return",
        "rental.agreement.cancel", "rental.agreement.close",
        "lease.contract.manage",
        "attachment.contract.verify", "attachment.contract.suspend", "attachment.contract.close",
        "finance.loan.route", "finance.loan.decide", "finance.lender.manage",
        "insurance.partner.manage",
        "valuation.model.manage",
        "trust.kyc.review", "trust.kyc.approve", "trust.fraud.review",
        "review.moderate",
        "document.verify",
        "analytics.dashboard.view", "analytics.export.run",
        "system.audit.read",
        "registry.taxonomy.publish", "registry.forms.publish", "registry.i18n.publish",
        "registry.geo.manage", "registry.features.manage", "registry.notifications.publish",
        "access.permission.view",
    ];

    public static IReadOnlyList<SeedRoleRow> All { get; } =
    [
        new(Customer, "Customer", "Buys, sells and rents on the marketplace.", CustomerPermissions),
        new(DealerOwner, "Dealer Owner",
            "Runs a dealership: stock, staff, auctions and payouts.", DealerOwnerPermissions),
        new(DealerStaff, "Dealer Staff",
            "Works a dealership's stock and enquiries, but cannot bid or change the business's details.",
            DealerStaffPermissions),
        new(FleetOwner, "Fleet Owner",
            "Operates a rental or attachment fleet.", FleetOwnerPermissions),
        new(Broker, "Broker",
            "Lists on behalf of owners. Sees the listing, never the money.", BrokerPermissions),
        new(Inspector, "Inspector",
            "Field partner who performs inspections against the published checklist.", InspectorPermissions),
        new(SupportExecutive, "Support Executive",
            "Reads everything a customer can see and moderates conversations. Moves no money.",
            SupportExecutivePermissions),
        new(DeliveryExecutive, "Delivery Executive",
            "Field partner who runs test drives and deliveries.", DeliveryExecutivePermissions),
        new(AuctionModerator, "Auction Moderator",
            "Runs auction events. Scoped to a city, one assignment per city in the region.",
            AuctionModeratorPermissions),
        new(Admin, "Admin",
            "Full operational control, and the registries that are safe to edit without engineering.",
            AdminPermissions),
        // Everything, including publishing pricing and workflows. Deliberately the only role that
        // can change what the platform charges or remove an approval step before a payout.
        new(SuperAdmin, "Super Admin",
            "Every permission, including the dangerous ones.", SeedPermissionCatalogue.AllCodes),
    ];
}

internal sealed record SeedRoleRow(
    string Code,
    string Name,
    string Description,
    IReadOnlyList<string> PermissionCodes);
