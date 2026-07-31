using AutoHub.Platform.Localization;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #9 — every user-facing string, including the error messages.
/// </summary>
/// <remarks>
/// The key set below is not invented: it is exactly what the web, dealer and admin apps ask for, so
/// the API answers every lookup those apps make and nothing falls through to the raw key. That is
/// the difference between a localisation registry and a localisation plan.
/// <para>
/// <b>Error codes are translation keys.</b> <c>error.registration.malformed</c> is the message a
/// user reads for the <c>registration.malformed</c> error code, which is why <c>Error.Code</c> is a
/// stable contract and why no validator in this codebase contains a sentence. Rewording an error is
/// a row edit; changing its code is a breaking API change.
/// </para>
/// <para>
/// <b>Fallbacks are authored, not derived.</b> Marathi falls back to Hindi because the scripts are
/// shared and the languages are largely mutually intelligible; Tamil falls back to English because
/// they are not. No algorithm knows that, so it is a column.
/// </para>
/// <para>
/// Hindi and Marathi cover the top strings rather than all of them — the navigation, the calls to
/// action, the field labels a seller reads, and the errors they are most likely to hit. A missing
/// key resolves through the fallback chain to English, which is a usable screen; a blank string is
/// an unlabelled rectangle that a screen reader announces as nothing at all, which is why
/// <see cref="I18nResource"/> refuses to store one.
/// </para>
/// </remarks>
internal sealed class LocalizationSeeder
{
    private const string Web = "web";

    private const string EnglishIndia = "en-IN";
    private const string HindiIndia = "hi-IN";
    private const string MarathiIndia = "mr-IN";

    private readonly AutoHubDbContext _context;

    public LocalizationSeeder(AutoHubDbContext context) => _context = context;

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var created = await SeedLocalesAsync(cancellationToken);
        created += await SeedResourcesAsync(cancellationToken);

        var total = await _context.I18nLocales.CountAsync(cancellationToken)
                    + await _context.I18nResources.CountAsync(cancellationToken);

        return new SeedStepResult("i18n", created, total - created);
    }

    private static readonly LocaleRow[] Locales =
    [
        new(EnglishIndia, "English (India)", "English", null, 1),
        new(HindiIndia, "Hindi", "हिन्दी", EnglishIndia, 2),
        new(MarathiIndia, "Marathi", "मराठी", HindiIndia, 3),
        new("ta-IN", "Tamil", "தமிழ்", EnglishIndia, 4),
        new("te-IN", "Telugu", "తెలుగు", EnglishIndia, 5),
        new("kn-IN", "Kannada", "ಕನ್ನಡ", EnglishIndia, 6),
        new("gu-IN", "Gujarati", "ગુજરાતી", HindiIndia, 7),
        new("bn-IN", "Bengali", "বাংলা", EnglishIndia, 8),
    ];

    private async Task<int> SeedLocalesAsync(CancellationToken cancellationToken)
    {
        var known = (await _context.I18nLocales.Select(locale => locale.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in Locales.Where(row => !known.Contains(row.Code)))
        {
            _context.I18nLocales.Add(
                I18nLocale.Create(row.Code, row.EnglishName, row.NativeName, row.FallbackCode, row.SortOrder));
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── en-IN: complete for every key the three portals resolve ──────────────────────────────────

    private static readonly (string Key, string Value)[] English =
    [
        ("app.name", "AutoHub"),
        ("app.tagline", "India's vehicle marketplace, end to end."),

        ("nav.buy", "Buy"),
        ("nav.sell", "Sell"),
        ("nav.auction", "Auction"),
        ("nav.rent", "Rent"),
        ("nav.lease", "Lease"),
        ("nav.finance", "Finance"),
        ("nav.insurance", "Insurance"),
        ("nav.attachment", "Taxi attachment"),
        ("nav.dashboard", "Dashboard"),
        ("nav.my_listings", "My listings"),
        ("nav.saved", "Saved"),
        ("nav.account", "Account"),

        ("listing.cta.get_valuation", "Get valuation"),
        ("listing.cta.view_details", "View details"),
        ("listing.cta.contact_seller", "Contact seller"),
        ("listing.cta.book_test_drive", "Book test drive"),
        ("listing.cta.make_offer", "Make an offer"),
        ("listing.cta.apply_finance", "Check finance offers"),
        ("listing.badge.inspected", "Inspected"),
        ("listing.badge.verified_rc", "RC verified"),
        ("listing.badge.single_owner", "Single owner"),
        ("listing.badge.non_accidental", "No accident history"),
        ("listing.label.posted", "Posted"),
        ("listing.label.seller_type", "Seller"),
        ("listing.empty.no_listings", "You have not listed a vehicle yet."),

        ("auction.cta.place_bid", "Place bid"),
        ("auction.cta.set_max_bid", "Set maximum bid"),
        ("auction.cta.watch_lot", "Watch this lot"),
        ("auction.state.live", "Live"),
        ("auction.state.closed", "Closed"),
        ("auction.state.scheduled", "Scheduled"),
        ("auction.label.current_bid", "Current bid"),
        ("auction.label.min_next_bid", "Minimum next bid"),
        ("auction.label.reserve_met", "Reserve met"),
        ("auction.label.time_left", "Time left"),
        ("auction.label.lots", "Lots"),
        ("auction.empty.no_bids", "No bids yet. Bidding opens when the reserve is set."),
        ("auction.notice.extended", "Bidding extended — a bid landed in the final seconds."),

        ("valuation.hero.title", "What is your car worth today?"),
        ("valuation.hero.subtitle", "Free · 30 seconds · no phone number needed"),
        ("valuation.label.market_value", "Market value"),
        ("valuation.label.trade_value", "Trade value"),
        ("valuation.label.dealer_offer", "Dealer offer"),
        ("valuation.label.auction_estimate", "Auction estimate"),
        ("valuation.label.quick_sale", "Quick sale"),
        ("valuation.label.as_of", "As of"),
        ("valuation.label.confidence", "Confidence"),

        ("search.empty.no_results",
            "No vehicles match these filters. Widen the price range or clear a filter."),
        ("search.label.filters", "Filters"),
        ("search.label.sort", "Sort"),
        ("search.label.results", "Results"),
        ("search.sort.relevance", "Most relevant"),
        ("search.sort.price_asc", "Price: low to high"),
        ("search.sort.price_desc", "Price: high to low"),
        ("search.sort.newest", "Newest first"),
        ("search.sort.kms_asc", "Fewest kilometres"),

        ("field.kms_driven", "Kms driven"),
        ("field.owners", "Owners"),
        ("field.fuel_type", "Fuel"),
        ("field.transmission", "Transmission"),
        ("field.colour", "Colour"),
        ("field.seats", "Seats"),
        ("field.manufacture_year", "Year"),
        ("field.asking_price", "Asking price"),
        ("field.registration_number", "Registration number"),
        ("field.insurance_validity", "Insurance validity"),
        ("field.rc_status", "RC status"),
        ("field.battery_health", "Battery health"),
        ("field.payload", "Payload"),
        ("field.boom_length", "Boom length"),

        ("form.sell_vehicle.title", "Tell us about your car"),
        ("form.sell_vehicle.submit", "Get my price"),
        ("form.step.identity", "Your car"),
        ("form.step.condition", "Condition and usage"),
        ("form.step.photos", "Photos"),
        ("form.step.price", "Your price and contact"),

        ("common.action.save", "Save"),
        ("common.action.cancel", "Cancel"),
        ("common.action.continue", "Continue"),
        ("common.action.back", "Back"),
        ("common.action.retry", "Try again"),
        ("common.label.loading", "Loading"),
        ("common.label.optional", "Optional"),
        ("common.label.required", "Required"),

        // Error codes are keys. Every one of these is the user-facing text for the machine code the
        // API returns, which is why no error message is ever written in a validator.
        ("error.registration.malformed", "Check the digits — example MH12AB1234."),
        ("error.registration.required", "Enter the registration number shown on the number plate."),
        ("error.catalog.vahan.record_not_found",
            "This registration number isn't in the RTO records. Check the digits, or enter details manually."),
        ("error.catalog.registration.invalid_format", "That is not an Indian registration number."),
        ("error.common.network.unreachable", "Cannot reach AutoHub. Check your connection and try again."),
        ("error.common.network.timeout", "That took too long. Try again in a moment."),
        ("error.common.network.aborted", "The request was cancelled."),
        ("error.common.response.malformed", "We could not read that response. Please try again."),
        ("error.common.request.malformed_json", "Something is wrong with that request."),
        ("error.common.resource.not_found", "We could not find that."),
        ("error.kms.too_high", "Enter the odometer reading in kilometres."),
        ("error.kms.negative", "An odometer reading cannot be negative."),
        ("error.phone.invalid", "Enter a ten-digit Indian mobile number."),
        ("error.email.invalid", "Enter a valid email address."),
        ("error.pincode.invalid", "A pincode is six digits and does not start with zero."),
        ("error.city.required", "Choose a city."),
        ("error.fuel_type.required", "Choose a fuel type."),
        ("error.photos.too_few", "Add at least six photos — listings with fewer barely get enquiries."),
        ("error.identity.otp.incorrect", "That code is not right. Check the SMS and try again."),
        ("error.identity.otp.expired", "That code has expired. Ask for a new one."),
        ("error.identity.token.expired", "Your session has expired. Sign in again."),
        ("error.identity.kyc.pan_invalid", "A PAN is five letters, four digits and a letter."),
        ("error.dealer.legal_name.too_short", "Enter the full registered name."),
        ("error.payments.invoice.gst_number_invalid",
            "That is not a valid GSTIN. The first two digits are the state code."),
        ("error.finance.bureau.consent_missing", "We need your consent before checking your credit score."),
        ("error.inspection.tread.invalid", "Tread depth is measured in millimetres, between 0 and 12."),
        ("error.auction.bid.below_increment", "Your bid must be at least the minimum next bid."),
        ("error.auction.bid.outbid_race", "Someone bid the same amount first. Try a higher bid."),
        ("error.auction.bidder.not_verified", "Only verified dealers can bid. Complete your KYC first."),
        ("error.auction.auction.not_started", "Bidding has not opened on this lot yet."),
        ("error.auction.auction.closed", "This lot has closed."),
        ("error.listings.listing.not_found", "That listing is no longer available."),
        ("error.deals.deal.not_found", "We could not find that deal."),
        ("error.platform.workflow.guard_failed", "The conditions for this action are not met."),
        ("error.platform.workflow.reason_required", "A reason is required for this action."),
        ("error.platform.workflow.transition_not_allowed",
            "This has moved on since you loaded the page. Refresh to see where it is now."),
        ("error.platform.registry.key_not_found", "That is not something we know about."),
        ("error.platform.i18n.locale_unsupported", "That language is not available yet."),

        ("state.draft", "Draft"),
        ("state.pending_review", "Under review"),
        ("state.live", "Live"),
        ("state.reserved", "Reserved"),
        ("state.sold", "Sold"),
        ("state.rejected", "Rejected"),
        ("state.expired", "Expired"),
        ("state.withdrawn", "Withdrawn"),
    ];

    // ── hi-IN: the strings a Hindi-first seller actually reads ───────────────────────────────────

    private static readonly (string Key, string Value)[] Hindi =
    [
        ("app.tagline", "भारत का वाहन बाज़ार, शुरू से अंत तक।"),
        ("nav.buy", "खरीदें"),
        ("nav.sell", "बेचें"),
        ("nav.auction", "नीलामी"),
        ("nav.rent", "किराया"),
        ("nav.lease", "लीज़"),
        ("nav.finance", "फाइनेंस"),
        ("nav.insurance", "बीमा"),
        ("nav.dashboard", "डैशबोर्ड"),
        ("nav.my_listings", "मेरी लिस्टिंग"),
        ("nav.saved", "सहेजे गए"),
        ("nav.account", "खाता"),
        ("listing.cta.get_valuation", "कीमत जानें"),
        ("listing.cta.view_details", "विवरण देखें"),
        ("listing.cta.contact_seller", "विक्रेता से संपर्क करें"),
        ("listing.cta.book_test_drive", "टेस्ट ड्राइव बुक करें"),
        ("listing.badge.inspected", "जाँची गई"),
        ("listing.badge.verified_rc", "आरसी सत्यापित"),
        ("auction.cta.place_bid", "बोली लगाएँ"),
        ("auction.state.live", "चालू"),
        ("auction.state.closed", "समाप्त"),
        ("auction.label.current_bid", "मौजूदा बोली"),
        ("auction.label.min_next_bid", "अगली न्यूनतम बोली"),
        ("auction.label.time_left", "शेष समय"),
        ("valuation.hero.title", "आज आपकी कार की कीमत क्या है?"),
        ("valuation.hero.subtitle", "मुफ़्त · 30 सेकंड · फ़ोन नंबर की ज़रूरत नहीं"),
        ("valuation.label.market_value", "बाज़ार मूल्य"),
        ("valuation.label.dealer_offer", "डीलर ऑफ़र"),
        ("valuation.label.as_of", "दिनांक"),
        ("search.empty.no_results", "इन फ़िल्टरों से कोई वाहन नहीं मिला। कीमत की सीमा बढ़ाएँ या फ़िल्टर हटाएँ।"),
        ("search.label.filters", "फ़िल्टर"),
        ("search.label.sort", "क्रम"),
        ("field.kms_driven", "चली हुई किलोमीटर"),
        ("field.owners", "मालिक"),
        ("field.fuel_type", "ईंधन"),
        ("field.transmission", "ट्रांसमिशन"),
        ("field.colour", "रंग"),
        ("field.manufacture_year", "वर्ष"),
        ("field.asking_price", "माँगी गई कीमत"),
        ("field.registration_number", "पंजीकरण संख्या"),
        ("field.insurance_validity", "बीमा वैधता"),
        ("form.sell_vehicle.title", "अपनी कार के बारे में बताएं"),
        ("form.sell_vehicle.submit", "मेरी कीमत बताएं"),
        ("common.action.save", "सहेजें"),
        ("common.action.cancel", "रद्द करें"),
        ("common.action.continue", "आगे बढ़ें"),
        ("common.action.back", "पीछे"),
        ("common.action.retry", "फिर कोशिश करें"),
        ("error.registration.malformed", "अंक जाँचें — उदाहरण MH12AB1234"),
        ("error.kms.too_high", "ओडोमीटर रीडिंग किलोमीटर में भरें।"),
        ("error.phone.invalid", "दस अंकों का मोबाइल नंबर भरें।"),
        ("error.identity.otp.incorrect", "यह कोड सही नहीं है। एसएमएस देखकर दोबारा भरें।"),
        ("error.common.network.unreachable", "AutoHub से संपर्क नहीं हो पा रहा। कनेक्शन जाँचें।"),
        ("state.live", "चालू"),
        ("state.sold", "बिक गई"),
        ("state.pending_review", "समीक्षा में"),
    ];

    // ── mr-IN: Marathi falls back to Hindi, so it carries the highest-traffic strings only ───────

    private static readonly (string Key, string Value)[] Marathi =
    [
        ("app.tagline", "भारताचा वाहन बाजार, सुरुवातीपासून शेवटपर्यंत."),
        ("nav.buy", "खरेदी"),
        ("nav.sell", "विक्री"),
        ("nav.auction", "लिलाव"),
        ("nav.rent", "भाडे"),
        ("nav.finance", "फायनान्स"),
        ("nav.insurance", "विमा"),
        ("nav.my_listings", "माझ्या जाहिराती"),
        ("nav.account", "खाते"),
        ("listing.cta.get_valuation", "किंमत पहा"),
        ("listing.cta.view_details", "तपशील पहा"),
        ("listing.cta.contact_seller", "विक्रेत्याशी संपर्क साधा"),
        ("listing.cta.book_test_drive", "टेस्ट ड्राइव्ह बुक करा"),
        ("auction.cta.place_bid", "बोली लावा"),
        ("auction.state.live", "सुरू"),
        ("auction.label.current_bid", "सध्याची बोली"),
        ("valuation.hero.title", "आज तुमच्या गाडीची किंमत किती?"),
        ("valuation.hero.subtitle", "मोफत · ३० सेकंद · फोन नंबर नको"),
        ("valuation.label.market_value", "बाजार मूल्य"),
        ("search.empty.no_results", "या फिल्टरमध्ये कोणतेही वाहन नाही. किंमत मर्यादा वाढवा."),
        ("search.label.filters", "फिल्टर"),
        ("field.kms_driven", "चाललेले किलोमीटर"),
        ("field.owners", "मालक"),
        ("field.fuel_type", "इंधन"),
        ("field.colour", "रंग"),
        ("field.manufacture_year", "वर्ष"),
        ("field.asking_price", "मागितलेली किंमत"),
        ("field.registration_number", "नोंदणी क्रमांक"),
        ("form.sell_vehicle.title", "तुमच्या गाडीबद्दल सांगा"),
        ("form.sell_vehicle.submit", "माझी किंमत सांगा"),
        ("common.action.save", "जतन करा"),
        ("common.action.cancel", "रद्द करा"),
        ("common.action.continue", "पुढे"),
        ("error.registration.malformed", "अंक तपासा — उदाहरण MH12AB1234"),
        ("error.phone.invalid", "दहा अंकी मोबाइल क्रमांक टाका."),
        ("state.live", "सुरू"),
        ("state.sold", "विकली"),
    ];

    private async Task<int> SeedResourcesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.I18nResources
            .Select(resource => new { resource.Namespace, resource.Key, resource.Locale })
            .ToListAsync(cancellationToken);

        var known = existing
            .Select(resource => (
                resource.Namespace.ToUpperInvariant(),
                resource.Key.ToUpperInvariant(),
                resource.Locale.ToUpperInvariant()))
            .ToHashSet();

        var created = 0;

        created += Add(EnglishIndia, English);
        created += Add(HindiIndia, Hindi);
        created += Add(MarathiIndia, Marathi);

        await _context.SaveChangesAsync(cancellationToken);
        return created;

        int Add(string locale, IReadOnlyList<(string Key, string Value)> rows)
        {
            var added = 0;

            foreach (var (key, value) in rows)
            {
                if (!known.Add((Web.ToUpperInvariant(), key.ToUpperInvariant(), locale.ToUpperInvariant())))
                {
                    continue;
                }

                _context.I18nResources.Add(SeedGuard.Required(
                    I18nResource.Create(Web, key, locale, value),
                    $"i18n {locale}/{key}"));

                added++;
            }

            return added;
        }
    }
}

internal sealed record LocaleRow(
    string Code,
    string EnglishName,
    string NativeName,
    string? FallbackCode,
    int SortOrder);
