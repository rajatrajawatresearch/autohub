using System.Text.Json.Nodes;
using AutoHub.Platform.Forms;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #2 — every form in the product, as data.
/// </summary>
/// <remarks>
/// Six forms, one renderer, three clients. Nothing in <c>apps/web</c>, the dealer portal or the
/// Flutter apps knows what a sell form contains — they fetch this document and draw it, which is why
/// adding a question is an afternoon rather than a fortnight and two app-store reviews.
/// <para>
/// <b>Option lists are never authored here.</b> A <c>select</c>, <c>radio</c> or <c>multiselect</c>
/// field carries an attribute code and no options at all; the schema compiler fills them from the
/// taxonomy registry at serve time. That is the mechanism that makes adding CNG to <c>fuel_type</c>
/// appear simultaneously in the sell flow, the dealer bulk-upload template and the inspection
/// checklist without republishing any of them — and <see cref="FormVersion.Validate"/> rejects a
/// form that tries to do it the other way.
/// </para>
/// <para>
/// The inspection checklist is sixty-six real checkpoints, and every one of them is the same
/// <c>select</c> over the single <c>check_result</c> attribute. Sixty-six copies of a five-item
/// option list is sixty-six places for "N/A" to be spelled differently.
/// </para>
/// <para>
/// No <see cref="FormRuleKind.Custom"/> rules are seeded. A custom rule names a server-side handler
/// — "this registration is already listed", "this PAN is blacklisted" — and this build registers
/// none, so publishing one would fail validation at exactly the right moment: now, rather than on
/// the first submission.
/// </para>
/// </remarks>
internal sealed class FormSeeder
{
    private const int SeededVersion = 1;

    private readonly AutoHubDbContext _context;
    private readonly IClock _clock;

    public FormSeeder(AutoHubDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        // Validation is run against the attribute codes that actually exist, not against a list
        // repeated here. A form referencing an attribute the taxonomy seeder did not create is a
        // publish failure with the field named, which is the whole point of the check.
        var attributeCodes = await _context.AttributeDefinitions
            .Select(definition => definition.Code)
            .ToListAsync(cancellationToken);

        var publishContext = new FormPublishContext
        {
            KnownAttributeCodes = attributeCodes.ToHashSet(StringComparer.Ordinal),
            KnownCustomRuleCodes = new HashSet<string>(StringComparer.Ordinal),
        };

        var known = (await _context.FormDefinitions.Select(form => form.Key).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var build in Forms)
        {
            var form = build();

            if (known.Contains(form.Key))
            {
                continue;
            }

            SeedGuard.Required(
                form.PublishVersion(SeededVersion, publishContext, _clock.UtcNow),
                $"form {form.Key} v{SeededVersion}");

            _context.FormDefinitions.Add(form);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SeedStepResult("forms", created, known.Count);
    }

    private static readonly Func<FormDefinition>[] Forms =
    [
        BuildSellVehicle,
        BuildDealerOnboarding,
        BuildDealerStockIntake,
        BuildInspectionChecklist,
        BuildLoanApplication,
        BuildInsuranceQuote,
    ];

    // ── Shared validation patterns ───────────────────────────────────────────────────────────────
    // Structural shapes, not business rules: what a plate, a PAN, a GSTIN and a pincode look like is
    // fixed by the issuing authority, and none of them is something an operator gets to redefine.

    private const string RegistrationPattern = "^[A-Z]{2}[0-9]{1,2}[A-Z]{0,3}[0-9]{1,4}$";

    private const string PanPattern = "^[A-Z]{5}[0-9]{4}[A-Z]$";

    private const string GstinPattern = "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][0-9A-Z]Z[0-9A-Z]$";

    private const string PincodePattern = "^[1-9][0-9]{5}$";

    // ── sell_vehicle ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The highest-traffic form in the product, and the one the contract publishes verbatim.
    /// </summary>
    /// <remarks>
    /// <c>battery_health_pct</c> is the field the north star points at. It is required, and it is
    /// invisible unless <c>fuel_type</c> is <c>ev</c> — a JsonLogic condition over the form's own
    /// values, evaluated identically on web, on Flutter and on the server. Supporting electric
    /// vehicles needed no code on any of the three.
    /// </remarks>
    private static FormDefinition BuildSellVehicle()
    {
        var form = FormDefinition.Draft("sell_vehicle", "Sell a vehicle", "listings.listing");
        form.Describe("The seller-facing listing flow. Four steps, because a twenty-eight field page is abandoned.");

        var version = new FormVersion(
            SeededVersion,
            "Tell us about your car",
            "Get my price",
            titleKey: "form.sell_vehicle.title",
            submitLabelKey: "form.sell_vehicle.submit");

        Step(version, "identity", "Your car", "We pull most of this from the RTO record.", 1);

        Field(version, "identity",
            new FormField("registration_number", "Registration number", FormControl.Plate,
                required: true,
                labelKey: "field.registration_number",
                placeholder: "MH 12 AB 1234",
                helpText: "Exactly as printed on the plate.",
                sortOrder: 1));
        Rule(version, "identity", "registration_number",
            new FormValidationRule(FormRuleKind.Pattern, "registration.malformed",
                "Check the digits — example MH12AB1234.", JsonValue.Create(RegistrationPattern)));

        Field(version, "identity",
            new FormField("vehicle", "Make, model and variant", FormControl.VehiclePicker,
                required: true,
                helpText: "Pick the exact trim — it changes the price by more than a lakh.",
                sortOrder: 2));

        Field(version, "identity",
            new FormField("manufacture_year", "Year of manufacture", FormControl.Number,
                required: true, attributeCode: "manufacture_year", sortOrder: 3));
        Rule(version, "identity", "manufacture_year",
            new FormValidationRule(FormRuleKind.Min, "year.too_old",
                "We can only value vehicles made in 1990 or later.", JsonValue.Create(1990)));
        Rule(version, "identity", "manufacture_year",
            new FormValidationRule(FormRuleKind.Max, "year.in_future",
                "That model year has not been built yet.", JsonValue.Create(2027)));

        Step(version, "condition", "Condition and usage", null, 2);

        Field(version, "condition",
            new FormField("fuel_type", "Fuel", FormControl.Select,
                required: true, labelKey: "field.fuel_type", attributeCode: "fuel_type", sortOrder: 1));

        Field(version, "condition",
            new FormField("transmission", "Transmission", FormControl.Select,
                attributeCode: "transmission", sortOrder: 2));

        Field(version, "condition",
            new FormField("kms_driven", "Kms driven", FormControl.Number,
                required: true, labelKey: "field.kms_driven", unit: "km",
                attributeCode: "kms_driven",
                helpText: "The odometer reading today, not at the last service.",
                sortOrder: 3));
        Rule(version, "condition", "kms_driven",
            new FormValidationRule(FormRuleKind.Min, "kms.negative",
                "An odometer reading cannot be negative.", JsonValue.Create(0)));
        Rule(version, "condition", "kms_driven",
            new FormValidationRule(FormRuleKind.Max, "kms.too_high",
                "Enter the odometer reading in kilometres.", JsonValue.Create(500_000)));

        Field(version, "condition",
            new FormField("owners", "Owners so far", FormControl.Number,
                required: true, labelKey: "field.owners", attributeCode: "owners", sortOrder: 4));

        Field(version, "condition",
            new FormField("colour", "Colour", FormControl.Select, attributeCode: "colour", sortOrder: 5));

        // The conditional field from the published contract. Required and hidden is a legitimate
        // combination precisely because the condition names a field this form collects — publish-time
        // validation proves that, so a typo like `fuel_typ` cannot ship a question nobody can answer.
        Field(version, "condition",
            new FormField("battery_health_pct", "Battery health", FormControl.Slider,
                required: true, unit: "%", attributeCode: "battery_health_pct",
                helpText: "From the vehicle's own battery report or the last service record.",
                configJson: """{"min":0,"max":100,"step":1}""",
                visibleWhenJson: """{"==":[{"var":"fuel_type"},"ev"]}""",
                sortOrder: 6));

        Field(version, "condition",
            new FormField("insurance_valid_till", "Insurance valid till", FormControl.Date,
                labelKey: "field.insurance_validity", attributeCode: "insurance_valid_till", sortOrder: 7));

        Step(version, "photos", "Photos", "Six photos is the minimum a buyer will enquire on.", 3);

        Field(version, "photos",
            new FormField("photos", "Photos", FormControl.PhotoSet,
                required: true,
                configJson: """
                    {"min":6,"max":20,"prompts":[
                      "Front 3/4","Rear 3/4","Odometer","Interior","Engine bay","Tyres",
                      "Left side","Right side","Boot","Dashboard","RC book","Service record"]}
                    """,
                sortOrder: 1));
        Rule(version, "photos", "photos",
            new FormValidationRule(FormRuleKind.Min, "photos.too_few",
                "Add at least six photos — listings with fewer barely get enquiries.",
                JsonValue.Create(6)));

        Step(version, "price", "Your price and contact", null, 4);

        Field(version, "price",
            new FormField("asking_price", "Asking price", FormControl.Money,
                required: true, attributeCode: "asking_price",
                helpText: "You can change this later. We will show you what similar cars sold for.",
                sortOrder: 1));

        Field(version, "price",
            new FormField("seller_name", "Your name", FormControl.Text, required: true, sortOrder: 2));
        Rule(version, "price", "seller_name",
            new FormValidationRule(FormRuleKind.MinLength, "name.too_short",
                "Enter your full name.", JsonValue.Create(2)));

        Field(version, "price",
            new FormField("seller_phone", "Mobile number", FormControl.Phone,
                required: true, placeholder: "+91 98765 43210", sortOrder: 3));
        Rule(version, "price", "seller_phone",
            new FormValidationRule(FormRuleKind.Phone, "phone.invalid",
                "Enter a ten-digit Indian mobile number."));

        Field(version, "price",
            new FormField("pickup_pincode", "Where is the car?", FormControl.Text,
                required: true, placeholder: "411014", sortOrder: 4));
        Rule(version, "price", "pickup_pincode",
            new FormValidationRule(FormRuleKind.Pattern, "pincode.invalid",
                "A pincode is six digits and does not start with zero.",
                JsonValue.Create(PincodePattern)));

        SeedGuard.Required(form.AddVersion(version), "form sell_vehicle version");
        return form;
    }

    // ── dealer_onboarding ────────────────────────────────────────────────────────────────────────

    private static FormDefinition BuildDealerOnboarding()
    {
        var form = FormDefinition.Draft("dealer_onboarding", "Dealer onboarding", "identity.dealer");
        form.Describe("Everything compliance needs before a dealer can list, bid or be paid out.");

        var version = new FormVersion(
            SeededVersion, "Register your dealership", "Submit for verification");

        Step(version, "business", "Business details", null, 1);

        Field(version, "business",
            new FormField("legal_name", "Registered legal name", FormControl.Text,
                required: true,
                helpText: "As printed on the GST certificate, not the board outside.",
                sortOrder: 1));
        Rule(version, "business", "legal_name",
            new FormValidationRule(FormRuleKind.MinLength, "dealer.legal_name.too_short",
                "Enter the full registered name.", JsonValue.Create(3)));

        Field(version, "business",
            new FormField("trade_name", "Trade name", FormControl.Text,
                placeholder: "The name buyers will see", sortOrder: 2));

        Field(version, "business",
            new FormField("gstin", "GSTIN", FormControl.Text,
                required: true, placeholder: "27AAAAA0000A1Z5", sortOrder: 3));
        Rule(version, "business", "gstin",
            new FormValidationRule(FormRuleKind.Pattern, "payments.invoice.gst_number_invalid",
                "That is not a valid GSTIN. The first two digits are the state code.",
                JsonValue.Create(GstinPattern)));

        Field(version, "business",
            new FormField("pan", "PAN", FormControl.Text, required: true, placeholder: "AAAAA0000A",
                sortOrder: 4));
        Rule(version, "business", "pan",
            new FormValidationRule(FormRuleKind.Pattern, "identity.kyc.pan_invalid",
                "A PAN is five letters, four digits and a letter.", JsonValue.Create(PanPattern)));

        Field(version, "business",
            new FormField("year_established", "Year established", FormControl.Number, sortOrder: 5));

        Step(version, "documents", "Documents", "Clear photographs are fine — we do not need originals.", 2);

        Field(version, "documents",
            new FormField("gst_certificate", "GST certificate", FormControl.File, required: true, sortOrder: 1));
        Field(version, "documents",
            new FormField("pan_card", "PAN card", FormControl.File, required: true, sortOrder: 2));
        Field(version, "documents",
            new FormField("address_proof", "Address proof", FormControl.File, required: true, sortOrder: 3));
        Field(version, "documents",
            new FormField("cancelled_cheque", "Cancelled cheque", FormControl.File,
                required: true,
                helpText: "Payouts go to this account and no other.",
                sortOrder: 4));
        Field(version, "documents",
            new FormField("showroom_photos", "Showroom photos", FormControl.PhotoSet,
                configJson: """{"min":2,"max":8,"prompts":["Frontage with signage","Display area","Workshop"]}""",
                sortOrder: 5));

        Step(version, "contact", "Owner and location", null, 3);

        Field(version, "contact",
            new FormField("owner_name", "Owner name", FormControl.Text, required: true, sortOrder: 1));
        Field(version, "contact",
            new FormField("owner_phone", "Owner mobile", FormControl.Phone, required: true, sortOrder: 2));
        Rule(version, "contact", "owner_phone",
            new FormValidationRule(FormRuleKind.Phone, "phone.invalid",
                "Enter a ten-digit Indian mobile number."));
        Field(version, "contact",
            new FormField("owner_email", "Owner email", FormControl.Text, sortOrder: 3));
        Rule(version, "contact", "owner_email",
            new FormValidationRule(FormRuleKind.Email, "email.invalid", "Enter a valid email address."));
        Field(version, "contact",
            new FormField("showroom_address", "Showroom address", FormControl.Address,
                required: true, sortOrder: 4));

        Step(version, "plan", "How you will use AutoHub", null, 4);

        Field(version, "plan",
            new FormField("expected_monthly_listings", "Vehicles you list in a month", FormControl.Number,
                sortOrder: 1));
        Field(version, "plan",
            new FormField("wants_auction_access", "I want to bid in dealer auctions", FormControl.Toggle,
                sortOrder: 2));
        Field(version, "plan",
            new FormField("accepts_terms", "I accept the dealer terms of service", FormControl.Checkbox,
                required: true, sortOrder: 3));
        Field(version, "plan",
            new FormField("accepts_commission_schedule", "I accept the published commission schedule",
                FormControl.Checkbox, required: true, sortOrder: 4));

        SeedGuard.Required(form.AddVersion(version), "form dealer_onboarding version");
        return form;
    }

    // ── dealer_stock_intake ──────────────────────────────────────────────────────────────────────

    private static FormDefinition BuildDealerStockIntake()
    {
        var form = FormDefinition.Draft("dealer_stock_intake", "Dealer stock intake", "listings.listing");
        form.Describe("The dealer's own listing form. Same attributes as the sell flow, plus the numbers only a dealer sees.");

        var version = new FormVersion(SeededVersion, "Add a vehicle to your stock", "Add to stock");

        Step(version, "vehicle", "Vehicle", null, 1);
        Field(version, "vehicle",
            new FormField("registration_number", "Registration number", FormControl.Plate,
                required: true, placeholder: "MH 12 AB 1234", sortOrder: 1));
        Rule(version, "vehicle", "registration_number",
            new FormValidationRule(FormRuleKind.Pattern, "registration.malformed",
                "Check the digits — example MH12AB1234.", JsonValue.Create(RegistrationPattern)));
        Field(version, "vehicle",
            new FormField("vehicle", "Make, model and variant", FormControl.VehiclePicker,
                required: true, sortOrder: 2));
        Field(version, "vehicle",
            new FormField("manufacture_year", "Year", FormControl.Number,
                required: true, attributeCode: "manufacture_year", sortOrder: 3));

        Step(version, "condition", "Condition", null, 2);
        Field(version, "condition",
            new FormField("fuel_type", "Fuel", FormControl.Select,
                required: true, attributeCode: "fuel_type", sortOrder: 1));
        Field(version, "condition",
            new FormField("transmission", "Transmission", FormControl.Select,
                attributeCode: "transmission", sortOrder: 2));
        Field(version, "condition",
            new FormField("body_type", "Body type", FormControl.Select,
                attributeCode: "body_type", sortOrder: 3));
        Field(version, "condition",
            new FormField("colour", "Colour", FormControl.Select, attributeCode: "colour", sortOrder: 4));
        Field(version, "condition",
            new FormField("kms_driven", "Kms driven", FormControl.Number,
                required: true, unit: "km", attributeCode: "kms_driven", sortOrder: 5));
        Field(version, "condition",
            new FormField("owners", "Owners", FormControl.Number,
                required: true, attributeCode: "owners", sortOrder: 6));
        Field(version, "condition",
            new FormField("insurance_valid_till", "Insurance valid till", FormControl.Date,
                attributeCode: "insurance_valid_till", sortOrder: 7));

        Step(version, "pricing", "Pricing", "Only the asking price is ever shown to a buyer.", 3);
        Field(version, "pricing",
            new FormField("asking_price", "Asking price", FormControl.Money,
                required: true, attributeCode: "asking_price", sortOrder: 1));
        Field(version, "pricing",
            new FormField("floor_price", "Floor price", FormControl.Money,
                helpText: "The lowest you will accept. Never shown to buyers; used to auto-decline offers.",
                sortOrder: 2));
        Field(version, "pricing",
            new FormField("procurement_cost", "What you paid", FormControl.Money,
                helpText: "Feeds your margin report. Private to your dealership.",
                sortOrder: 3));

        Step(version, "media", "Media and documents", null, 4);
        Field(version, "media",
            new FormField("photos", "Photos", FormControl.PhotoSet,
                required: true,
                configJson: """{"min":6,"max":24,"prompts":["Front 3/4","Rear 3/4","Odometer","Interior","Engine bay","Tyres"]}""",
                sortOrder: 1));
        Field(version, "media",
            new FormField("rc_copy", "RC copy", FormControl.File, required: true, sortOrder: 2));
        Field(version, "media",
            new FormField("stock_notes", "Notes for your team", FormControl.RichText, sortOrder: 3));

        SeedGuard.Required(form.AddVersion(version), "form dealer_stock_intake version");
        return form;
    }

    // ── inspection_checklist ─────────────────────────────────────────────────────────────────────

    private static readonly InspectionSectionRow[] InspectionSections =
    [
        new("engine_transmission", "Engine and transmission", 1,
        [
            new("engine_oil_leak", "Engine oil leak"),
            new("engine_coolant_level", "Coolant level and condition"),
            new("engine_starting", "Cold start behaviour"),
            new("engine_idling", "Idle stability"),
            new("engine_noise", "Abnormal engine noise"),
            new("engine_smoke", "Exhaust smoke colour"),
            new("engine_mounts", "Engine mounts"),
            new("radiator_condition", "Radiator and fan"),
            new("belt_condition", "Drive belts"),
            new("clutch_engagement", "Clutch engagement point"),
            new("gearbox_shifting", "Gear shift quality"),
            new("transmission_oil_leak", "Transmission oil leak"),
        ]),

        new("exterior_body", "Exterior and body", 2,
        [
            new("bonnet_panel", "Bonnet"),
            new("front_bumper", "Front bumper"),
            new("rear_bumper", "Rear bumper"),
            new("roof_panel", "Roof panel"),
            new("boot_lid", "Boot lid"),
            new("front_left_door", "Front left door"),
            new("front_right_door", "Front right door"),
            new("rear_left_door", "Rear left door"),
            new("rear_right_door", "Rear right door"),
            new("left_fender", "Left fender"),
            new("right_fender", "Right fender"),
            new("windscreen_front", "Front windscreen"),
            new("windscreen_rear", "Rear windscreen"),
            new("paint_uniformity", "Paint match across panels"),
        ]),

        new("interior_cabin", "Interior and cabin", 3,
        [
            new("dashboard_condition", "Dashboard"),
            new("driver_seat", "Driver seat"),
            new("passenger_seats", "Passenger seats"),
            new("seat_belts", "Seat belts and retractors"),
            new("floor_mats", "Floor and mats"),
            new("roof_lining", "Roof lining"),
            new("ac_cooling", "Air conditioning cooling"),
            new("ac_blower", "Blower on all speeds"),
            new("infotainment_unit", "Infotainment unit"),
            new("speakers", "Speakers"),
            new("power_windows", "Power windows"),
            new("central_locking", "Central locking"),
        ]),

        new("tyres_brakes", "Tyres, brakes and suspension", 4,
        [
            new("tyre_front_left", "Front left tyre tread"),
            new("tyre_front_right", "Front right tyre tread"),
            new("tyre_rear_left", "Rear left tyre tread"),
            new("tyre_rear_right", "Rear right tyre tread"),
            new("spare_tyre", "Spare tyre and tools"),
            new("brake_pads_front", "Front brake pads"),
            new("brake_pads_rear", "Rear brake pads"),
            new("brake_discs", "Brake discs and drums"),
            new("handbrake_travel", "Handbrake travel"),
            new("suspension_noise", "Suspension noise over bumps"),
        ]),

        new("electrical_electronics", "Electrical and electronics", 5,
        [
            new("battery_health_check", "Battery health"),
            new("alternator_output", "Alternator output"),
            new("headlamp_left", "Left headlamp"),
            new("headlamp_right", "Right headlamp"),
            new("tail_lamps", "Tail lamps"),
            new("indicators", "Indicators and hazards"),
            new("horn_working", "Horn"),
            new("wiper_motor", "Wiper motor and blades"),
            new("washer_system", "Washer system"),
            new("reverse_camera", "Reverse camera"),
            new("parking_sensors", "Parking sensors"),
            new("abs_warning_lamp", "ABS and airbag warning lamps"),
        ]),

        new("documents_compliance", "Documents and compliance", 6,
        [
            new("rc_original", "Original RC present"),
            new("insurance_valid", "Insurance in force"),
            new("puc_certificate", "PUC certificate"),
            new("road_tax_paid", "Road tax paid to date"),
            new("chassis_number_match", "Chassis number matches RC"),
            new("hypothecation_status", "Hypothecation cleared"),
        ]),
    ];

    private static FormDefinition BuildInspectionChecklist()
    {
        var form = FormDefinition.Draft("inspection_checklist", "Inspection checklist", "inspection.report");
        form.Describe("Sixty-six checkpoints across six sections. The vendor adds eleven brake checks by inserting rows.");

        var version = new FormVersion(SeededVersion, "Vehicle inspection", "Submit inspection report");

        foreach (var section in InspectionSections)
        {
            Step(version, section.Key, section.Title, null, section.SortOrder);

            var order = 1;
            foreach (var point in section.Points)
            {
                Field(version, section.Key,
                    new FormField(point.Key, point.Label, FormControl.Select,
                        attributeCode: "check_result", sortOrder: order++));
            }
        }

        Step(version, "summary", "Summary", "The grade the buyer sees on the listing badge.", 90);

        Field(version, "summary",
            new FormField("odometer_reading", "Odometer reading", FormControl.Number,
                required: true, unit: "km", attributeCode: "kms_driven", sortOrder: 1));
        Field(version, "summary",
            new FormField("overall_grade", "Overall grade", FormControl.Slider,
                required: true,
                configJson: """{"min":1,"max":5,"step":1}""",
                helpText: "1 is scrap, 5 is showroom.",
                sortOrder: 2));
        Field(version, "summary",
            new FormField("inspection_photos", "Inspection photos", FormControl.PhotoSet,
                required: true,
                configJson: """
                    {"min":10,"max":40,"prompts":[
                      "Front 3/4","Rear 3/4","Odometer","Engine bay","Chassis number","Tyre tread gauge",
                      "Underbody","Boot floor","Roof","Any damage found"]}
                    """,
                sortOrder: 3));
        Field(version, "summary",
            new FormField("inspector_notes", "Inspector notes", FormControl.RichText, sortOrder: 4));

        SeedGuard.Required(form.AddVersion(version), "form inspection_checklist version");
        return form;
    }

    // ── loan_application ─────────────────────────────────────────────────────────────────────────

    private static FormDefinition BuildLoanApplication()
    {
        var form = FormDefinition.Draft("loan_application", "Loan application", "finance.loan_application");
        form.Describe("One application, routed to several lenders. Bureau consent is collected explicitly and stored.");

        var version = new FormVersion(SeededVersion, "Apply for finance", "Check my offers");

        Step(version, "applicant", "About you", null, 1);
        Field(version, "applicant",
            new FormField("full_name", "Full name", FormControl.Text, required: true, sortOrder: 1));
        Field(version, "applicant",
            new FormField("date_of_birth", "Date of birth", FormControl.Date, required: true, sortOrder: 2));
        Field(version, "applicant",
            new FormField("pan", "PAN", FormControl.Text, required: true, placeholder: "AAAAA0000A",
                sortOrder: 3));
        Rule(version, "applicant", "pan",
            new FormValidationRule(FormRuleKind.Pattern, "identity.kyc.pan_invalid",
                "A PAN is five letters, four digits and a letter.", JsonValue.Create(PanPattern)));
        Field(version, "applicant",
            new FormField("mobile", "Mobile number", FormControl.Phone, required: true, sortOrder: 4));
        Rule(version, "applicant", "mobile",
            new FormValidationRule(FormRuleKind.Phone, "phone.invalid",
                "Enter a ten-digit Indian mobile number."));
        Field(version, "applicant",
            new FormField("email", "Email", FormControl.Text, sortOrder: 5));
        Rule(version, "applicant", "email",
            new FormValidationRule(FormRuleKind.Email, "email.invalid", "Enter a valid email address."));
        Field(version, "applicant",
            new FormField("residence_pincode", "Residence pincode", FormControl.Text,
                required: true, sortOrder: 6));
        Rule(version, "applicant", "residence_pincode",
            new FormValidationRule(FormRuleKind.Pattern, "pincode.invalid",
                "A pincode is six digits and does not start with zero.",
                JsonValue.Create(PincodePattern)));

        // Consent is a field with an audit trail, not a checkbox in a modal nobody can produce later.
        // Pulling a bureau score without recorded consent is a regulatory finding, not a bug.
        Field(version, "applicant",
            new FormField("bureau_consent", "I consent to a credit bureau check", FormControl.Checkbox,
                required: true,
                helpText: "Required by the lender. A soft enquiry does not affect your score.",
                sortOrder: 7));

        Step(version, "employment", "Income", null, 2);
        Field(version, "employment",
            new FormField("employment_type", "Employment", FormControl.Select,
                required: true, attributeCode: "employment_type", sortOrder: 1));
        Field(version, "employment",
            new FormField("employer_name", "Employer or business name", FormControl.Text, sortOrder: 2));
        Field(version, "employment",
            new FormField("monthly_income", "Monthly income", FormControl.Money,
                required: true, sortOrder: 3));
        Field(version, "employment",
            new FormField("years_in_current_role", "Years in current role", FormControl.Number, sortOrder: 4));
        Field(version, "employment",
            new FormField("existing_emi", "Existing EMIs each month", FormControl.Money, sortOrder: 5));

        Step(version, "loan", "The loan", null, 3);
        Field(version, "loan",
            new FormField("loan_purpose", "Purpose", FormControl.Select,
                required: true, attributeCode: "loan_purpose", sortOrder: 1));
        Field(version, "loan",
            new FormField("loan_amount", "Amount you need", FormControl.Money, required: true, sortOrder: 2));
        Field(version, "loan",
            new FormField("down_payment", "Down payment you can make", FormControl.Money, sortOrder: 3));
        Field(version, "loan",
            new FormField("tenure_months", "Tenure in months", FormControl.Number, required: true, sortOrder: 4));
        Rule(version, "loan", "tenure_months",
            new FormValidationRule(FormRuleKind.Min, "tenure.too_short",
                "The shortest tenure a lender will write is twelve months.", JsonValue.Create(12)));
        Rule(version, "loan", "tenure_months",
            new FormValidationRule(FormRuleKind.Max, "tenure.too_long",
                "The longest tenure on a used vehicle is eighty-four months.", JsonValue.Create(84)));

        Step(version, "documents", "Documents", "Upload now or send them to the lender later.", 4);
        Field(version, "documents",
            new FormField("identity_proof", "Identity proof", FormControl.File, required: true, sortOrder: 1));
        Field(version, "documents",
            new FormField("address_proof", "Address proof", FormControl.File, required: true, sortOrder: 2));
        Field(version, "documents",
            new FormField("income_proof", "Salary slips or ITR", FormControl.File, sortOrder: 3));
        Field(version, "documents",
            new FormField("bank_statement", "Six months bank statement", FormControl.File, sortOrder: 4));

        SeedGuard.Required(form.AddVersion(version), "form loan_application version");
        return form;
    }

    // ── insurance_quote ──────────────────────────────────────────────────────────────────────────

    private static FormDefinition BuildInsuranceQuote()
    {
        var form = FormDefinition.Draft("insurance_quote", "Insurance quote", "insurance.quote");
        form.Describe("Enough to price a motor policy without asking for anything the RC already says.");

        var version = new FormVersion(SeededVersion, "Insure your vehicle", "Show me quotes");

        Step(version, "vehicle", "Vehicle", null, 1);
        Field(version, "vehicle",
            new FormField("registration_number", "Registration number", FormControl.Plate,
                required: true, placeholder: "MH 12 AB 1234", sortOrder: 1));
        Rule(version, "vehicle", "registration_number",
            new FormValidationRule(FormRuleKind.Pattern, "registration.malformed",
                "Check the digits — example MH12AB1234.", JsonValue.Create(RegistrationPattern)));
        Field(version, "vehicle",
            new FormField("manufacture_year", "Year of manufacture", FormControl.Number,
                required: true, attributeCode: "manufacture_year", sortOrder: 2));
        Field(version, "vehicle",
            new FormField("expected_idv", "Insured declared value you want", FormControl.Money,
                helpText: "Leave blank and we will use the insurer's default IDV.",
                sortOrder: 3));

        Step(version, "cover", "Cover", null, 2);
        Field(version, "cover",
            new FormField("cover_type", "Cover", FormControl.Select,
                required: true, attributeCode: "insurance_cover_type", sortOrder: 1));
        Field(version, "cover",
            new FormField("zero_depreciation", "Zero depreciation", FormControl.Toggle, sortOrder: 2));
        Field(version, "cover",
            new FormField("engine_protect", "Engine protection", FormControl.Toggle, sortOrder: 3));
        Field(version, "cover",
            new FormField("roadside_assistance", "Roadside assistance", FormControl.Toggle, sortOrder: 4));

        Step(version, "history", "Policy history", null, 3);
        Field(version, "history",
            new FormField("previous_policy_expiry", "Previous policy expiry", FormControl.Date, sortOrder: 1));
        Field(version, "history",
            new FormField("ncb_slab", "No-claim bonus", FormControl.Select,
                attributeCode: "ncb_slab", sortOrder: 2));
        Field(version, "history",
            new FormField("claims_last_year", "Claims in the last year", FormControl.Number, sortOrder: 3));

        Step(version, "contact", "Contact", null, 4);
        Field(version, "contact",
            new FormField("applicant_name", "Your name", FormControl.Text, required: true, sortOrder: 1));
        Field(version, "contact",
            new FormField("applicant_mobile", "Mobile number", FormControl.Phone, required: true, sortOrder: 2));
        Rule(version, "contact", "applicant_mobile",
            new FormValidationRule(FormRuleKind.Phone, "phone.invalid",
                "Enter a ten-digit Indian mobile number."));
        Field(version, "contact",
            new FormField("applicant_email", "Email", FormControl.Text, sortOrder: 3));
        Rule(version, "contact", "applicant_email",
            new FormValidationRule(FormRuleKind.Email, "email.invalid", "Enter a valid email address."));

        SeedGuard.Required(form.AddVersion(version), "form insurance_quote version");
        return form;
    }

    // ── plumbing ─────────────────────────────────────────────────────────────────────────────────

    private static void Step(
        FormVersion version, string key, string title, string? description, int sortOrder)
        => SeedGuard.Required(
            version.AddStep(new FormStep(key, title, titleKey: $"form.step.{key}",
                description: description, sortOrder: sortOrder)),
            $"form step {key}");

    private static void Field(FormVersion version, string stepKey, FormField field)
        => SeedGuard.Required(version.AddField(stepKey, field), $"form field {stepKey}.{field.Key}");

    private static void Rule(FormVersion version, string stepKey, string fieldKey, FormValidationRule rule)
        => SeedGuard.Required(
            version.AddRule(stepKey, fieldKey, rule),
            $"form rule {stepKey}.{fieldKey}.{rule.Rule}");
}

internal sealed record InspectionPointRow(string Key, string Label);

internal sealed record InspectionSectionRow(
    string Key,
    string Title,
    int SortOrder,
    IReadOnlyList<InspectionPointRow> Points);
