using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #3 — the lifecycles of everything that has one.
/// </summary>
/// <remarks>
/// Eight workflows, and not one status column anywhere in the schema. Between them they exercise
/// both engine features the demo has to prove: the listing workflow carries a <b>guarded</b>
/// transition (a listing cannot be submitted for review without the minimum photo set) and an
/// <b>automatic timer</b> transition (a live listing expires after thirty days without anybody
/// touching it), and the deal, booking, rental and loan workflows each carry a timer of their own
/// for the window that actually expires in that business.
/// <para>
/// <b>No effects are wired.</b> An effect is a named handler a developer registers, and this build
/// registers none — the modules that own <c>ledger.post_commission</c> and
/// <c>notification.send</c> are not written yet. Publishing a transition that references a handler
/// nobody implements does not degrade gracefully: <see cref="WorkflowEffectDispatcher"/> refuses the
/// whole transition, so every seeded workflow would be inert. Effects are attached by the module
/// that supplies the handler, in the version it publishes.
/// </para>
/// <para>
/// Published definitions are immutable, so seeding is version-keyed: if <c>listing</c> v1 already
/// exists this seeder leaves it alone rather than trying to edit it. A changed lifecycle is a new
/// version, exactly as it would be in production.
/// </para>
/// </remarks>
internal sealed class WorkflowSeeder
{
    private const int SeededVersion = 1;

    /// <summary>
    /// Nothing is registered, and that is deliberate — see this type's remarks.
    /// </summary>
    private static readonly IReadOnlySet<string> NoEffectHandlers = new HashSet<string>(StringComparer.Ordinal);

    private readonly AutoHubDbContext _context;
    private readonly IClock _clock;

    public WorkflowSeeder(AutoHubDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.WorkflowDefinitions
            .Where(definition => definition.Version == SeededVersion)
            .Select(definition => definition.Key)
            .ToListAsync(cancellationToken);

        var known = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var build in Definitions)
        {
            var definition = build();

            if (known.Contains(definition.Key))
            {
                continue;
            }

            SeedGuard.Required(
                definition.Publish(NoEffectHandlers, _clock.UtcNow),
                $"workflow {definition.Key} v{definition.Version}");

            _context.WorkflowDefinitions.Add(definition);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SeedStepResult("workflow", created, known.Count);
    }

    private static readonly Func<WorkflowDefinition>[] Definitions =
    [
        BuildListing,
        BuildDeal,
        BuildAuctionLot,
        BuildBooking,
        BuildKycCase,
        BuildLoanApplication,
        BuildRentalAgreement,
        BuildAttachmentContract,
    ];

    // ── listing ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The lifecycle the whole marketplace hangs off, and the one that proves the engine.
    /// </summary>
    /// <remarks>
    /// Two transitions here are doing more than moving a state.
    /// <para>
    /// <c>submit</c> carries a <b>guard</b>: a listing with three blurred photos wastes a moderator's
    /// time and converts badly, so the minimum photo count is enforced by the workflow rather than by
    /// a validator somebody can forget to call from the bulk importer. Raising the minimum to eight
    /// is an edit to this rule, not a release.
    /// </para>
    /// <para>
    /// <c>expire</c> carries an <b>automatic timer</b>: nobody delists a car they sold privately, so
    /// a marketplace without an expiry sweeper slowly fills with vehicles that are not for sale,
    /// which is the single fastest way to lose buyer trust. Thirty days is the number, and it is a
    /// number in a row.
    /// </para>
    /// </remarks>
    private static WorkflowDefinition BuildListing()
    {
        var definition = WorkflowDefinition.Draft("listing", SeededVersion, "listings.listing", "Listing lifecycle");

        AddStates(definition,
            new("draft", "Draft", StateCategory.Draft, IsInitial: true),
            new("pending_review", "Under review", StateCategory.AwaitingAction, SlaMinutes: 240),
            new("live", "Live", StateCategory.InProgress),
            new("reserved", "Reserved", StateCategory.InProgress, SlaMinutes: 2880),
            new("sold", "Sold", StateCategory.Succeeded, IsTerminal: true),
            new("rejected", "Rejected", StateCategory.Failed, IsTerminal: true),
            new("expired", "Expired", StateCategory.Cancelled, IsTerminal: true),
            new("withdrawn", "Withdrawn", StateCategory.Cancelled, IsTerminal: true));

        AddTransitions(definition,
            new("submit", "Submit for review", "draft", "pending_review", "listing.submit",
                GuardJson: """{">=":[{"var":"media.photoCount"},6]}"""),
            new("discard", "Discard draft", "draft", "withdrawn", "listing.withdraw"),
            new("approve", "Approve", "pending_review", "live", "listing.moderate"),
            new("reject", "Reject", "pending_review", "rejected", "listing.moderate", RequiresReason: true),
            new("reserve", "Mark reserved", "live", "reserved", "listing.reserve"),
            new("sell", "Mark sold", "live", "sold", "listing.close"),
            new("withdraw", "Withdraw", "live", "withdrawn", "listing.withdraw", RequiresReason: true),
            // Thirty days. The engine schedules this the moment the listing goes live and clears it
            // on any manual move, so a car that sells on day three never expires.
            new("expire", "Expire", "live", "expired", AutomaticAfterMinutes: 43_200),
            new("release", "Release reservation", "reserved", "live", "listing.reserve"),
            new("complete", "Complete sale", "reserved", "sold", "listing.close"));

        return definition;
    }

    // ── deal ─────────────────────────────────────────────────────────────────────────────────────

    private static WorkflowDefinition BuildDeal()
    {
        var definition = WorkflowDefinition.Draft("deal", SeededVersion, "deals.deal", "Deal settlement");

        AddStates(definition,
            new("initiated", "Initiated", StateCategory.Draft, IsInitial: true),
            new("awaiting_payment", "Awaiting payment", StateCategory.AwaitingAction, SlaMinutes: 1440),
            new("payment_received", "Payment received", StateCategory.InProgress),
            new("documents_pending", "Documents pending", StateCategory.AwaitingAction, SlaMinutes: 4320),
            new("rto_transfer", "RTO transfer", StateCategory.InProgress, SlaMinutes: 20_160),
            new("completed", "Completed", StateCategory.Succeeded, IsTerminal: true),
            new("cancelled", "Cancelled", StateCategory.Cancelled, IsTerminal: true),
            new("refunded", "Refunded", StateCategory.Failed, IsTerminal: true));

        AddTransitions(definition,
            new("confirm", "Confirm deal", "initiated", "awaiting_payment", "deal.confirm"),
            new("abandon", "Abandon", "initiated", "cancelled", "deal.cancel", RequiresReason: true),
            new("capture_payment", "Record payment", "awaiting_payment", "payment_received", "deal.payment.record"),
            // Forty-eight hours. A token that is never paid holds a vehicle off the market, and the
            // seller is the one who loses by it — so the window closes itself.
            new("payment_window_expired", "Payment window expired", "awaiting_payment", "cancelled",
                AutomaticAfterMinutes: 2880),
            new("start_paperwork", "Start paperwork", "payment_received", "documents_pending",
                "deal.document.manage"),
            new("refund_before_papers", "Refund", "payment_received", "refunded", "deal.refund",
                RequiresReason: true),
            new("documents_verified", "Documents verified", "documents_pending", "rto_transfer",
                "deal.document.verify",
                GuardJson: """{"==":[{"var":"documents.allVerified"},true]}"""),
            new("documents_rejected", "Reject documents", "documents_pending", "refunded", "deal.refund",
                RequiresReason: true),
            new("transfer_complete", "Transfer complete", "rto_transfer", "completed", "deal.close"),
            new("transfer_failed", "Transfer failed", "rto_transfer", "refunded", "deal.refund",
                RequiresReason: true));

        return definition;
    }

    // ── auction_lot ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The highest-margin lifecycle, and the one with the most expensive failure modes.
    /// </summary>
    /// <remarks>
    /// Anti-snipe extension is a state rather than a mutable end time, because "extend the clock"
    /// implemented as an <c>UPDATE lots SET ends_at = …</c> has no audit trail and no way to answer a
    /// dealer asking why a lot they were watching ran three minutes longer than the page said. As a
    /// state with a timer, every extension is a history row with a timestamp and an actor.
    /// </remarks>
    private static WorkflowDefinition BuildAuctionLot()
    {
        var definition = WorkflowDefinition.Draft("auction_lot", SeededVersion, "auction.lot", "Auction lot");

        AddStates(definition,
            new("scheduled", "Scheduled", StateCategory.Draft, IsInitial: true),
            new("live", "Live", StateCategory.InProgress),
            new("in_extension", "Anti-snipe extension", StateCategory.InProgress),
            new("reserve_not_met", "Reserve not met", StateCategory.AwaitingAction, SlaMinutes: 720),
            new("sold", "Sold", StateCategory.Succeeded, IsTerminal: true),
            new("unsold", "Unsold", StateCategory.Failed, IsTerminal: true),
            new("cancelled", "Cancelled", StateCategory.Cancelled, IsTerminal: true));

        AddTransitions(definition,
            new("open", "Open lot", "scheduled", "live", "auction.lot.open"),
            new("cancel_before_open", "Cancel", "scheduled", "cancelled", "auction.lot.cancel",
                RequiresReason: true),
            // A bid inside the last thirty seconds moves the lot into extension. The guard is what
            // stops a bid at minute two from resetting a clock nobody was racing.
            new("extend", "Extend for late bid", "live", "in_extension", "auction.bid.place",
                GuardJson: """{"<=":[{"var":"lot.secondsRemaining"},30]}"""),
            new("hammer_down", "Close — reserve met", "live", "sold", "auction.lot.close",
                GuardJson: """{">=":[{"var":"lot.highestBidMinor"},{"var":"lot.reserveMinor"}]}"""),
            new("close_under_reserve", "Close — reserve not met", "live", "reserve_not_met",
                "auction.lot.close"),
            new("cancel_live", "Cancel live lot", "live", "cancelled", "auction.lot.cancel",
                RequiresReason: true),
            // Two minutes of extension, then back to the open floor so a further late bid extends again.
            new("extension_elapsed", "Extension elapsed", "in_extension", "live", AutomaticAfterMinutes: 2),
            new("accept_below_reserve", "Accept below reserve", "reserve_not_met", "sold",
                "auction.lot.approve_below_reserve"),
            new("decline_below_reserve", "Decline", "reserve_not_met", "unsold", "auction.lot.close",
                RequiresReason: true));

        return definition;
    }

    // ── booking ──────────────────────────────────────────────────────────────────────────────────

    private static WorkflowDefinition BuildBooking()
    {
        var definition = WorkflowDefinition.Draft("booking", SeededVersion, "booking.booking", "Booking");

        AddStates(definition,
            new("requested", "Requested", StateCategory.Draft, IsInitial: true),
            new("confirmed", "Confirmed", StateCategory.InProgress),
            new("in_progress", "In progress", StateCategory.InProgress),
            new("completed", "Completed", StateCategory.Succeeded, IsTerminal: true),
            new("no_show", "No show", StateCategory.Failed, IsTerminal: true),
            new("cancelled", "Cancelled", StateCategory.Cancelled, IsTerminal: true));

        AddTransitions(definition,
            new("confirm", "Confirm slot", "requested", "confirmed", "booking.confirm"),
            // Two hours. An unconfirmed test-drive request holds a slot another buyer wanted.
            new("release_slot", "Release unconfirmed slot", "requested", "cancelled",
                AutomaticAfterMinutes: 120),
            new("start", "Start", "confirmed", "in_progress", "booking.start"),
            new("cancel", "Cancel", "confirmed", "cancelled", "booking.cancel", RequiresReason: true),
            new("complete", "Complete", "in_progress", "completed", "booking.complete"),
            new("mark_no_show", "Mark no-show", "in_progress", "no_show", "booking.complete",
                RequiresReason: true));

        return definition;
    }

    // ── kyc_case ─────────────────────────────────────────────────────────────────────────────────

    private static WorkflowDefinition BuildKycCase()
    {
        var definition = WorkflowDefinition.Draft("kyc_case", SeededVersion, "trust.kyc_case", "KYC case");

        AddStates(definition,
            new("submitted", "Submitted", StateCategory.AwaitingAction, IsInitial: true, SlaMinutes: 480),
            new("documents_requested", "More documents needed", StateCategory.AwaitingAction, SlaMinutes: 2880),
            new("under_review", "Under review", StateCategory.InProgress, SlaMinutes: 1440),
            new("verified", "Verified", StateCategory.Succeeded, IsTerminal: true),
            new("rejected", "Rejected", StateCategory.Failed, IsTerminal: true),
            new("expired", "Expired", StateCategory.Cancelled, IsTerminal: true));

        AddTransitions(definition,
            new("pick_up", "Start review", "submitted", "under_review", "trust.kyc.review"),
            new("request_documents", "Request documents", "submitted", "documents_requested",
                "trust.kyc.review", RequiresReason: true),
            new("documents_received", "Documents received", "documents_requested", "under_review",
                "trust.kyc.review"),
            // Seven days. A case waiting on a dealer who has moved on should not sit in a queue for ever.
            new("documents_timeout", "Lapse", "documents_requested", "expired", AutomaticAfterMinutes: 10_080),
            // PAN verification is a hard gate: approving a dealer whose PAN never came back verified is
            // how a payout leaves the platform to a name nobody checked.
            new("approve", "Approve", "under_review", "verified", "trust.kyc.approve",
                GuardJson: """{"==":[{"var":"kyc.panVerified"},true]}"""),
            new("reject", "Reject", "under_review", "rejected", "trust.kyc.approve", RequiresReason: true));

        return definition;
    }

    // ── loan_application ─────────────────────────────────────────────────────────────────────────

    private static WorkflowDefinition BuildLoanApplication()
    {
        var definition = WorkflowDefinition.Draft(
            "loan_application", SeededVersion, "finance.loan_application", "Loan application");

        AddStates(definition,
            new("draft", "Draft", StateCategory.Draft, IsInitial: true),
            new("submitted", "Submitted", StateCategory.AwaitingAction, SlaMinutes: 720),
            new("lender_review", "With lender", StateCategory.InProgress, SlaMinutes: 4320),
            new("approved", "Approved", StateCategory.InProgress),
            new("disbursed", "Disbursed", StateCategory.Succeeded, IsTerminal: true),
            new("declined", "Declined", StateCategory.Failed, IsTerminal: true),
            new("withdrawn", "Withdrawn", StateCategory.Cancelled, IsTerminal: true));

        AddTransitions(definition,
            // Bureau consent is legally required before a score is pulled, so it gates submission
            // rather than being checked inside whichever handler happens to call the bureau.
            new("submit", "Submit application", "draft", "submitted", "finance.loan.submit",
                GuardJson: """{"==":[{"var":"applicant.bureauConsent"},true]}"""),
            new("abandon", "Abandon", "draft", "withdrawn", "finance.loan.withdraw"),
            new("route_to_lender", "Send to lender", "submitted", "lender_review", "finance.loan.route"),
            new("withdraw", "Withdraw", "submitted", "withdrawn", "finance.loan.withdraw",
                RequiresReason: true),
            new("approve", "Approve", "lender_review", "approved", "finance.loan.decide"),
            new("decline", "Decline", "lender_review", "declined", "finance.loan.decide",
                RequiresReason: true),
            new("disburse", "Disburse", "approved", "disbursed", "finance.loan.disburse"),
            // Fourteen days. A sanction letter has an expiry and the platform must not imply otherwise.
            new("offer_lapsed", "Offer lapsed", "approved", "declined", AutomaticAfterMinutes: 20_160));

        return definition;
    }

    // ── rental_agreement ─────────────────────────────────────────────────────────────────────────

    private static WorkflowDefinition BuildRentalAgreement()
    {
        var definition = WorkflowDefinition.Draft(
            "rental_agreement", SeededVersion, "rental.agreement", "Rental agreement");

        AddStates(definition,
            new("draft", "Draft", StateCategory.Draft, IsInitial: true),
            new("awaiting_deposit", "Awaiting deposit", StateCategory.AwaitingAction, SlaMinutes: 720),
            new("active", "Active", StateCategory.InProgress),
            new("overdue", "Overdue", StateCategory.AwaitingAction, SlaMinutes: 1440),
            new("returned", "Returned", StateCategory.InProgress, SlaMinutes: 2880),
            new("closed", "Closed", StateCategory.Succeeded, IsTerminal: true),
            new("cancelled", "Cancelled", StateCategory.Cancelled, IsTerminal: true));

        AddTransitions(definition,
            new("issue", "Issue agreement", "draft", "awaiting_deposit", "rental.agreement.issue"),
            new("cancel_draft", "Cancel", "draft", "cancelled", "rental.agreement.cancel",
                RequiresReason: true),
            new("deposit_paid", "Deposit received", "awaiting_deposit", "active",
                "rental.agreement.activate"),
            // Twelve hours to pay the deposit, or the vehicle goes back on the calendar.
            new("deposit_timeout", "Deposit not received", "awaiting_deposit", "cancelled",
                AutomaticAfterMinutes: 720),
            new("return_vehicle", "Record return", "active", "returned", "rental.agreement.return"),
            new("flag_overdue", "Flag overdue", "active", "overdue", AutomaticAfterMinutes: 1440),
            new("late_return", "Record late return", "overdue", "returned", "rental.agreement.return"),
            new("write_off", "Write off", "overdue", "cancelled", "rental.agreement.cancel",
                RequiresReason: true),
            new("close", "Close and settle", "returned", "closed", "rental.agreement.close"));

        return definition;
    }

    // ── attachment_contract ──────────────────────────────────────────────────────────────────────

    private static WorkflowDefinition BuildAttachmentContract()
    {
        var definition = WorkflowDefinition.Draft(
            "attachment_contract", SeededVersion, "attachment.contract", "Taxi attachment contract");

        AddStates(definition,
            new("draft", "Draft", StateCategory.Draft, IsInitial: true),
            new("pending_verification", "Pending verification", StateCategory.AwaitingAction, SlaMinutes: 2880),
            new("active", "Active", StateCategory.InProgress),
            new("suspended", "Suspended", StateCategory.AwaitingAction, SlaMinutes: 10_080),
            new("completed", "Completed", StateCategory.Succeeded, IsTerminal: true),
            new("terminated", "Terminated", StateCategory.Cancelled, IsTerminal: true));

        AddTransitions(definition,
            new("submit", "Submit for verification", "draft", "pending_verification",
                "attachment.contract.submit"),
            new("abandon", "Abandon", "draft", "terminated", "attachment.contract.terminate"),
            // A taxi attachment without a live permit and a valid fitness certificate is a vehicle
            // that cannot legally carry a passenger, so the guard is the compliance check itself.
            new("verify", "Verify and activate", "pending_verification", "active",
                "attachment.contract.verify",
                GuardJson: """
                    {"and":[
                      {"==":[{"var":"compliance.permitValid"},true]},
                      {"==":[{"var":"compliance.fitnessValid"},true]}
                    ]}
                    """),
            new("reject", "Reject", "pending_verification", "terminated", "attachment.contract.verify",
                RequiresReason: true),
            new("suspend", "Suspend", "active", "suspended", "attachment.contract.suspend",
                RequiresReason: true),
            new("terminate", "Terminate", "active", "terminated", "attachment.contract.terminate",
                RequiresReason: true),
            new("complete", "Complete term", "active", "completed", "attachment.contract.close"),
            new("reinstate", "Reinstate", "suspended", "active", "attachment.contract.suspend"),
            new("terminate_suspended", "Terminate", "suspended", "terminated",
                "attachment.contract.terminate", RequiresReason: true));

        return definition;
    }

    // ── plumbing ─────────────────────────────────────────────────────────────────────────────────

    private static void AddStates(WorkflowDefinition definition, params WorkflowStateRow[] rows)
    {
        foreach (var row in rows)
        {
            SeedGuard.Required(
                definition.AddState(new WorkflowState(
                    row.Code, row.Name, row.Category, row.IsInitial, row.IsTerminal, row.SlaMinutes)),
                $"workflow {definition.Key} state {row.Code}");
        }
    }

    private static void AddTransitions(WorkflowDefinition definition, params WorkflowTransitionRow[] rows)
    {
        foreach (var row in rows)
        {
            SeedGuard.Required(
                definition.AddTransition(new WorkflowTransition(
                    row.ActionCode,
                    row.Label,
                    row.FromStateCode,
                    row.ToStateCode,
                    row.RequiredPermission,
                    row.GuardJson,
                    row.RequiresReason,
                    row.AutomaticAfterMinutes)),
                $"workflow {definition.Key} transition {row.ActionCode}");
        }
    }
}

internal sealed record WorkflowStateRow(
    string Code,
    string Name,
    StateCategory Category,
    bool IsInitial = false,
    bool IsTerminal = false,
    int? SlaMinutes = null);

internal sealed record WorkflowTransitionRow(
    string ActionCode,
    string Label,
    string FromStateCode,
    string ToStateCode,
    string? RequiredPermission = null,
    string? GuardJson = null,
    bool RequiresReason = false,
    int? AutomaticAfterMinutes = null);
