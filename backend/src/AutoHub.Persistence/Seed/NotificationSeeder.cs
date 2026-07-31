using AutoHub.Platform.Notifications;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #7 — which events reach which people, on which channel, saying what.
/// </summary>
/// <remarks>
/// Modules raise events and know nothing about messages. The auction module raises
/// <c>auction.bid_placed.v1</c>; whether that becomes a push to the outbid dealer, a WhatsApp to the
/// seller, both or neither is a row here — which is why "change the outbid message in Marathi" is a
/// config change and not a release.
/// <para>
/// <b>Dedupe and throttle are the difference between a notification system and an uninstall.</b> A
/// lot in its final minute takes forty bids; without a dedupe window the losing dealer's phone
/// buzzes forty times and they turn the channel off for ever — costing us every future notification,
/// not only the noisy ones. The window is per lot, because being outbid on a <i>different</i> lot is
/// genuinely new information.
/// </para>
/// <para>
/// <b>SMS and WhatsApp bodies are mirrors, not sources.</b> Under TRAI's TCCCPR rules an Indian
/// commercial SMS is matched at the operator against a template registered in advance on a DLT
/// platform; a body that does not match is accepted, sometimes billed, and silently dropped. So each
/// of these carries its registered template id, and <see cref="NotificationTemplate.Activate"/>
/// refuses to publish one without it. Editing the body without re-registering is a compliance
/// change wearing the clothes of a copy edit.
/// </para>
/// </remarks>
internal sealed class NotificationSeeder
{
    private const string DefaultLocale = "en-IN";

    private const string SenderId = "AUTOHB";

    private readonly AutoHubDbContext _context;

    public NotificationSeeder(AutoHubDbContext context) => _context = context;

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var created = await SeedTemplatesAsync(cancellationToken);
        created += await SeedEventsAsync(cancellationToken);

        var total = await _context.NotificationTemplates.CountAsync(cancellationToken)
                    + await _context.NotificationEvents.CountAsync(cancellationToken);

        return new SeedStepResult("notifications", created, total - created);
    }

    // ── Templates ────────────────────────────────────────────────────────────────────────────────
    //
    // Keyed by (key, channel, locale) because the same event says different things on different
    // surfaces: "Your bid on the 2021 Swift was outbid — new high bid ₹5,15,000" is a reasonable
    // WhatsApp message and a push notification Android truncates at about forty characters.

    private static readonly TemplateRow[] Templates =
    [
        new("identity.otp", NotificationChannel.Sms, null,
            "{code} is your AutoHub verification code. Valid for 5 minutes. Do not share it with anyone.",
            "1107100000000000001",
            [new("code", true, "482915", "The one-time code.")]),

        new("listing.approved", NotificationChannel.Push, "Your listing is live",
            "{vehicle} is now live on AutoHub.", null,
            [new("vehicle", true, "2021 Maruti Suzuki Swift VXi", "Listing title.")]),

        new("listing.approved", NotificationChannel.Whatsapp, null,
            "Good news — your {vehicle} is live on AutoHub. Buyers can now see it and enquire. "
            + "Track enquiries here: {link}",
            "1107100000000000002",
            [
                new("vehicle", true, "2021 Maruti Suzuki Swift VXi", "Listing title."),
                new("link", true, "https://autohub.in/l/019fab0c", "Deep link to the listing."),
            ]),

        new("listing.rejected", NotificationChannel.Push, "Listing needs changes",
            "{vehicle}: {reason}", null,
            [
                new("vehicle", true, "2021 Maruti Suzuki Swift VXi", "Listing title."),
                new("reason", true, "Photos do not match the registration number.", "Moderator's reason."),
            ]),

        new("auction.outbid", NotificationChannel.Push, "You have been outbid",
            "{vehicle} — new high bid {amount}.", null,
            [
                new("vehicle", true, "2021 Mahindra XUV700 AX7", "Lot title."),
                new("amount", true, "₹15,25,000", "The new highest bid, already formatted."),
            ]),

        new("auction.outbid", NotificationChannel.Whatsapp, null,
            "You have been outbid on {vehicle}. The current high bid is {amount} and the lot closes "
            + "at {closesAt}. Bid again: {link}",
            "1107100000000000003",
            [
                new("vehicle", true, "2021 Mahindra XUV700 AX7", "Lot title."),
                new("amount", true, "₹15,25,000", "The new highest bid."),
                new("closesAt", true, "3:45 PM", "Local closing time."),
                new("link", true, "https://autohub.in/a/019fab0c", "Deep link to the lot."),
            ]),

        new("auction.lot_won", NotificationChannel.Push, "You won the lot",
            "{vehicle} is yours at {amount}.", null,
            [
                new("vehicle", true, "2021 Mahindra XUV700 AX7", "Lot title."),
                new("amount", true, "₹15,40,000", "Winning bid."),
            ]),

        new("auction.lot_won", NotificationChannel.Sms, null,
            "You won {vehicle} at {amount} on AutoHub. Payment is due by {dueBy}. - AutoHub",
            "1107100000000000004",
            [
                new("vehicle", true, "2021 Mahindra XUV700 AX7", "Lot title."),
                new("amount", true, "₹15,40,000", "Winning bid."),
                new("dueBy", true, "31 Jul, 6:00 PM", "Payment deadline."),
            ]),

        new("deal.payment_due", NotificationChannel.Sms, null,
            "Payment of {amount} for {vehicle} is due by {dueBy}. Pay here: {link} - AutoHub",
            "1107100000000000005",
            [
                new("amount", true, "₹5,49,000", "Amount due."),
                new("vehicle", true, "2021 Maruti Suzuki Swift VXi", "Vehicle."),
                new("dueBy", true, "31 Jul, 6:00 PM", "Deadline."),
                new("link", true, "https://autohub.in/p/019fab0c", "Payment link."),
            ]),

        new("deal.completed", NotificationChannel.Push, "Transfer complete",
            "{vehicle} is now registered to you.", null,
            [new("vehicle", true, "2021 Maruti Suzuki Swift VXi", "Vehicle.")]),

        new("booking.reminder", NotificationChannel.Push, "Test drive tomorrow",
            "{vehicle} at {time}, {place}.", null,
            [
                new("vehicle", true, "2021 Maruti Suzuki Swift VXi", "Vehicle."),
                new("time", true, "11:00 AM", "Slot time."),
                new("place", true, "Viman Nagar, Pune", "Where."),
            ]),

        new("booking.reminder", NotificationChannel.Sms, null,
            "Reminder: your AutoHub test drive of {vehicle} is at {time} on {date}, {place}.",
            "1107100000000000006",
            [
                new("vehicle", true, "2021 Maruti Suzuki Swift VXi", "Vehicle."),
                new("time", true, "11:00 AM", "Slot time."),
                new("date", true, "30 Jul", "Slot date."),
                new("place", true, "Viman Nagar, Pune", "Where."),
            ]),

        new("insurance.expiring", NotificationChannel.Push, "Insurance expiring",
            "{vehicle} insurance expires on {expiresOn}.", null,
            [
                new("vehicle", true, "2021 Maruti Suzuki Swift VXi", "Vehicle."),
                new("expiresOn", true, "14 Aug 2026", "Policy expiry date."),
            ]),

        new("kyc.verified", NotificationChannel.Push, "You are verified",
            "Your dealership is verified. You can now bid in auctions.", null, []),

        new("kyc.rejected", NotificationChannel.Push, "KYC needs attention",
            "We could not verify your documents: {reason}", null,
            [new("reason", true, "The PAN did not match the GST certificate.", "Reviewer's reason.")]),

        new("loan.offer_ready", NotificationChannel.Push, "Your finance offers are ready",
            "{count} lenders have made an offer.", null,
            [new("count", true, "3", "How many offers.")]),
    ];

    private async Task<int> SeedTemplatesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.NotificationTemplates
            .Select(template => new { template.Key, template.Channel, template.Locale })
            .ToListAsync(cancellationToken);

        var known = existing
            .Select(template => (
                template.Key.ToUpperInvariant(), template.Channel, template.Locale.ToUpperInvariant()))
            .ToHashSet();

        var created = 0;

        foreach (var row in Templates)
        {
            if (!known.Add((row.Key.ToUpperInvariant(), row.Channel, DefaultLocale.ToUpperInvariant())))
            {
                continue;
            }

            var template = NotificationTemplate.Draft(
                row.Key, row.Channel, DefaultLocale, row.Body, row.Subject);

            foreach (var variable in row.Variables)
            {
                template.Declare(new NotificationTemplateVariable(
                    variable.Name, variable.IsRequired, variable.SampleValue, variable.Description));
            }

            if (row.ProviderTemplateId is not null)
            {
                template.RegisterWithProvider(row.ProviderTemplateId, SenderId);
            }

            SeedGuard.Required(template.Activate(), $"notification template {row.Key}/{row.Channel}");

            _context.NotificationTemplates.Add(template);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Event rules ──────────────────────────────────────────────────────────────────────────────
    //
    // One domain event maps to many rules — one per audience. `auction.bid_placed.v1` produces "you
    // were outbid" to the previous high bidder and, separately, "a new bid arrived" to the seller,
    // with different templates, channels and throttles. Expressing both in one row is where
    // notification systems become unreadable.

    private static readonly NotificationChannel[] PushOnly = [NotificationChannel.Push, NotificationChannel.InApp];

    private static readonly NotificationChannel[] PushAndWhatsapp =
        [NotificationChannel.Push, NotificationChannel.InApp, NotificationChannel.Whatsapp];

    private static readonly NotificationChannel[] SmsOnly = [NotificationChannel.Sms];

    private static readonly NotificationChannel[] SmsAndPush =
        [NotificationChannel.Sms, NotificationChannel.Push, NotificationChannel.InApp];

    private static readonly NotificationEventRow[] Events =
    [
        new("identity.otp_requested.notify_user", "identity.otp_requested.v1", "security",
            "identity.otp", NotificationAudience.Actor(), SmsOnly, NotificationUrgency.Transactional),

        new("listings.listing_approved.notify_seller", "listings.listing_approved.v1", "listings",
            "listing.approved", NotificationAudience.AggregateOwner(), PushAndWhatsapp,
            NotificationUrgency.Informational),

        new("listings.listing_rejected.notify_seller", "listings.listing_rejected.v1", "listings",
            "listing.rejected", NotificationAudience.AggregateOwner(), PushOnly,
            NotificationUrgency.Informational),

        new("listings.listing_submitted.notify_moderators", "listings.listing_submitted.v1", "moderation",
            "listing.approved", NotificationAudience.PermissionHolders("listing.moderate"), PushOnly,
            NotificationUrgency.Informational,
            // Moderators do not need a ping per listing during a dealer's bulk upload of two hundred.
            ThrottleMaxPerWindow: 6, ThrottleWindowSeconds: 3600),

        new("auction.bid_placed.notify_previous_bidder", "auction.bid_placed.v1", "auction",
            "auction.outbid", NotificationAudience.Participants(), PushAndWhatsapp,
            NotificationUrgency.Critical,
            // Per lot, and one message a minute. Forty bids in the closing minute is one message.
            DedupeWindowSeconds: 60, DedupeKeyTemplate: "{lotId}",
            ThrottleMaxPerWindow: 20, ThrottleWindowSeconds: 3600),

        new("auction.lot_won.notify_winner", "auction.lot_won.v1", "auction",
            "auction.lot_won", NotificationAudience.Actor(), SmsAndPush,
            NotificationUrgency.Critical),

        new("auction.lot_won.notify_seller", "auction.lot_won.v1", "auction",
            "auction.lot_won", NotificationAudience.AggregateOwner(), PushOnly,
            NotificationUrgency.Informational),

        new("deals.payment_due.notify_buyer", "deals.payment_due.v1", "deals",
            "deal.payment_due", NotificationAudience.Actor(), SmsAndPush,
            NotificationUrgency.Transactional,
            DedupeWindowSeconds: 21_600, DedupeKeyTemplate: "{dealId}"),

        new("deals.deal_completed.notify_buyer", "deals.deal_completed.v1", "deals",
            "deal.completed", NotificationAudience.Actor(), PushOnly,
            NotificationUrgency.Informational),

        new("bookings.reminder_due.notify_customer", "bookings.reminder_due.v1", "bookings",
            "booking.reminder", NotificationAudience.Actor(), SmsAndPush,
            NotificationUrgency.Transactional,
            DedupeWindowSeconds: 43_200, DedupeKeyTemplate: "{bookingId}"),

        new("insurance.policy_expiring.notify_owner", "insurance.policy_expiring.v1", "insurance",
            "insurance.expiring", NotificationAudience.AggregateOwner(), PushOnly,
            NotificationUrgency.Informational,
            // Once a week, however many times the sweeper runs.
            DedupeWindowSeconds: 604_800, DedupeKeyTemplate: "{policyId}"),

        new("trust.kyc_verified.notify_dealer", "trust.kyc_verified.v1", "account",
            "kyc.verified", NotificationAudience.AggregateOwner(), PushOnly,
            NotificationUrgency.Informational),

        new("trust.kyc_rejected.notify_dealer", "trust.kyc_rejected.v1", "account",
            "kyc.rejected", NotificationAudience.AggregateOwner(), PushOnly,
            NotificationUrgency.Informational),

        new("finance.offers_ready.notify_applicant", "finance.offers_ready.v1", "finance",
            "loan.offer_ready", NotificationAudience.Actor(), PushOnly,
            NotificationUrgency.Informational),
    ];

    private async Task<int> SeedEventsAsync(CancellationToken cancellationToken)
    {
        var known = (await _context.NotificationEvents.Select(rule => rule.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in Events.Where(row => !known.Contains(row.Code)))
        {
            var rule = NotificationEvent.Draft(
                row.Code, row.EventType, row.CategoryCode, row.TemplateKey, row.Audience, row.Channels);

            rule.SetUrgency(row.Urgency);

            if (row.DedupeWindowSeconds is { } dedupeWindow)
            {
                rule.SetDedupe(TimeSpan.FromSeconds(dedupeWindow), row.DedupeKeyTemplate);
            }

            if (row.ThrottleMaxPerWindow is { } max && row.ThrottleWindowSeconds is { } window)
            {
                rule.SetThrottle(max, TimeSpan.FromSeconds(window));
            }

            SeedGuard.Required(rule.Activate(), $"notification rule {row.Code}");

            _context.NotificationEvents.Add(rule);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }
}

internal sealed record TemplateVariableRow(
    string Name,
    bool IsRequired,
    string SampleValue,
    string Description);

internal sealed record TemplateRow(
    string Key,
    NotificationChannel Channel,
    string? Subject,
    string Body,
    string? ProviderTemplateId,
    IReadOnlyList<TemplateVariableRow> Variables);

internal sealed record NotificationEventRow(
    string Code,
    string EventType,
    string CategoryCode,
    string TemplateKey,
    NotificationAudience Audience,
    IReadOnlyList<NotificationChannel> Channels,
    NotificationUrgency Urgency,
    int? DedupeWindowSeconds = null,
    string? DedupeKeyTemplate = null,
    int? ThrottleMaxPerWindow = null,
    int? ThrottleWindowSeconds = null);
