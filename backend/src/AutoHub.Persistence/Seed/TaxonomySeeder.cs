using System.Text.Json.Nodes;
using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #1 — the Indian vehicle taxonomy the whole marketplace is shaped by.
/// </summary>
/// <remarks>
/// This seeder is the north star's acceptance test written as data. <c>construction</c> is a
/// category like any other, <c>boom_length</c> is an attribute like any other, and neither needed a
/// migration, a C# type or a <c>switch</c> to exist. If a reader wants to know whether the "nothing
/// is hardcoded" claim is real, the construction rows at the bottom of this file are the answer.
/// <para>
/// <b>Every attribute here declares a facet style deliberately.</b> The default is
/// <see cref="FacetStyle.List"/>, and <see cref="AttributeDefinition.Validate"/> rejects a list facet
/// on anything without a closed option set — so a number left on the default would publish a filter
/// rail that renders a checkbox list over kilometres.
/// <see cref="AttributeDefinition.SetPresentation"/> is therefore called on every single definition
/// rather than only where the default is wrong.
/// </para>
/// </remarks>
internal sealed class TaxonomySeeder
{
    private readonly AutoHubDbContext _context;

    public TaxonomySeeder(AutoHubDbContext context) => _context = context;

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var created = 0;

        created += await SeedCategoriesAsync(cancellationToken);
        created += await SeedAttributesAsync(cancellationToken);
        created += await SeedCategoryAttributesAsync(cancellationToken);
        created += await SeedMakesAsync(cancellationToken);
        created += await SeedModelsAsync(cancellationToken);
        created += await SeedVariantsAsync(cancellationToken);

        // Every table this seeder writes, so "created" and "already present" are counted over the
        // same set of rows. The two the earlier version omitted — attribute options and the
        // category-to-attribute links — are together the largest contributors, so a first run
        // reported a *negative* skipped count. That is not a cosmetic slip: the whole reason
        // SeedStepResult carries both halves is to make a natural-key lookup that has silently
        // stopped matching visible, and a number that can go negative cannot be read for that.
        var present = await _context.VehicleCategories.CountAsync(cancellationToken)
                      + await _context.AttributeDefinitions.CountAsync(cancellationToken)
                      + await _context.AttributeOptions.CountAsync(cancellationToken)
                      + await _context.CategoryAttributes.CountAsync(cancellationToken)
                      + await _context.VehicleMakes.CountAsync(cancellationToken)
                      + await _context.VehicleModels.CountAsync(cancellationToken)
                      + await _context.VehicleVariants.CountAsync(cancellationToken);

        return new SeedStepResult("taxonomy", created, present - created);
    }

    // ── Categories ───────────────────────────────────────────────────────────────────────────────

    private static readonly TaxonomyCategoryRow[] Categories =
    [
        new("car", "Car", "car", null, 1),
        new("hatchback", "Hatchback", "car", "car", 1),
        new("sedan", "Sedan", "car", "car", 2),
        new("suv", "SUV", "car", "car", 3),
        new("muv", "MUV", "car", "car", 4),
        new("luxury", "Luxury", "car", "car", 5),

        new("bike", "Bike", "bike", null, 2),
        new("commuter", "Commuter", "bike", "bike", 1),
        new("sports", "Sports", "bike", "bike", 2),
        new("cruiser", "Cruiser", "bike", "bike", 3),
        new("scooter", "Scooter", "scooter", "bike", 4),

        new("commercial", "Commercial", "truck", null, 3),
        new("truck", "Truck", "truck", "commercial", 1),
        new("bus", "Bus", "bus", "commercial", 2),
        new("pickup", "Pickup", "pickup", "commercial", 3),
        new("tempo", "Tempo", "van", "commercial", 4),

        new("taxi", "Taxi", "taxi", null, 4),
        new("ev", "Electric", "bolt", null, 5),
        new("construction", "Construction Equipment", "excavator", null, 6),
    ];

    private async Task<int> SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.VehicleCategories
            .ToDictionaryAsync(category => category.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var created = 0;

        // Parents before children, because a child needs its parent's id and the table is
        // self-referencing. Authored order already satisfies that; the loop does not reorder it.
        foreach (var row in Categories)
        {
            if (existing.ContainsKey(row.Code))
            {
                continue;
            }

            Guid? parentId = row.ParentCode is null
                ? null
                : existing[row.ParentCode].Id;

            var category = SeedGuard.Required(
                VehicleCategory.Create(row.Code, row.Name, parentId, row.Icon, row.SortOrder),
                $"category {row.Code}");

            _context.VehicleCategories.Add(category);
            existing[row.Code] = category;
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Attribute definitions ────────────────────────────────────────────────────────────────────

    private static readonly TaxonomyAttributeRow[] Attributes =
    [
        new("fuel_type", "Fuel", AttributeDataType.Enum, DisplayGroup: "engine", SortOrder: 10,
            IsRequired: true, FacetStyle: FacetStyle.Chips,
            Options:
            [
                new("petrol", "Petrol", 1), new("diesel", "Diesel", 2), new("cng", "CNG", 3),
                new("ev", "Electric", 4), new("hybrid", "Hybrid", 5), new("lpg", "LPG", 6),
            ]),

        new("transmission", "Transmission", AttributeDataType.Enum, DisplayGroup: "engine", SortOrder: 20,
            IsRequired: true, FacetStyle: FacetStyle.Chips,
            Options:
            [
                new("manual", "Manual", 1), new("automatic", "Automatic", 2), new("amt", "AMT", 3),
                new("cvt", "CVT", 4), new("dct", "DCT", 5), new("imt", "iMT", 6),
            ]),

        new("body_type", "Body type", AttributeDataType.Enum, DisplayGroup: "identity", SortOrder: 30,
            FacetStyle: FacetStyle.List,
            Options:
            [
                new("hatchback", "Hatchback", 1), new("sedan", "Sedan", 2), new("suv", "SUV", 3),
                new("muv", "MUV", 4), new("coupe", "Coupe", 5), new("van", "Van", 6),
                new("pickup", "Pickup", 7), new("open", "Open body", 8),
                new("closed_container", "Closed container", 9), new("tipper", "Tipper", 10),
                new("tanker", "Tanker", 11), new("flatbed", "Flatbed", 12),
                new("refrigerated", "Refrigerated", 13), new("tautliner", "Tautliner", 14),
                new("bulker", "Bulker", 15), new("car_carrier", "Car carrier", 16),
                new("bus_body", "Bus", 17), new("backhoe", "Backhoe", 18),
            ]),

        new("colour", "Colour", AttributeDataType.Enum, DisplayGroup: "exterior", SortOrder: 40,
            FacetStyle: FacetStyle.List,
            Options:
            [
                new("white", "White", 1), new("silver", "Silver", 2), new("grey", "Grey", 3),
                new("black", "Black", 4), new("red", "Red", 5), new("blue", "Blue", 6),
                new("brown", "Brown", 7), new("orange", "Orange", 8), new("green", "Green", 9),
                new("yellow", "Yellow", 10), new("maroon", "Maroon", 11), new("beige", "Beige", 12),
            ]),

        // Kilometres deliberately carry no step. `Step` is enforced by the value validator, not only
        // by the slider, so a step of 1,000 would reject an honest odometer reading of 38,417 km and
        // quietly push every seller to round their mileage. The facet buckets are the renderer's
        // problem; the stored value must stay exact.
        new("kms_driven", "Kms driven", AttributeDataType.Integer, Unit: "km", DisplayGroup: "usage",
            SortOrder: 50, IsRequired: true, FacetStyle: FacetStyle.Range, Min: 0, Max: 500_000),

        new("owners", "Owners", AttributeDataType.Integer, DisplayGroup: "ownership", SortOrder: 60,
            IsRequired: true, FacetStyle: FacetStyle.Range, Min: 1, Max: 10, Step: 1),

        new("manufacture_year", "Year", AttributeDataType.Integer, DisplayGroup: "identity",
            SortOrder: 5, IsRequired: true, FacetStyle: FacetStyle.Range, Min: 1990, Max: 2027, Step: 1),

        new("seats", "Seats", AttributeDataType.Integer, DisplayGroup: "comfort", SortOrder: 70,
            FacetStyle: FacetStyle.Range, Min: 2, Max: 60, Step: 1),

        new("insurance_valid_till", "Insurance valid till", AttributeDataType.Date,
            DisplayGroup: "ownership", SortOrder: 80, FacetStyle: FacetStyle.Range),

        // Bounds on a money attribute are minor units, like every other amount in the system:
        // ₹10,000 to ₹10 crore. Written as paise here so the unit is unmistakable at the call site.
        new("asking_price", "Asking price", AttributeDataType.Money, DisplayGroup: "pricing",
            SortOrder: 1, IsRequired: true, FacetStyle: FacetStyle.Range,
            Min: 1_000_000, Max: 10_000_000_000),

        // ── Electric ─────────────────────────────────────────────────────────────────────────────
        new("battery_kwh", "Battery", AttributeDataType.Number, Unit: "kWh", DisplayGroup: "battery",
            SortOrder: 110, IsRequired: true, FacetStyle: FacetStyle.Range, Min: 1, Max: 500),

        new("range_km", "Certified range", AttributeDataType.Integer, Unit: "km",
            DisplayGroup: "battery", SortOrder: 120, FacetStyle: FacetStyle.Range, Min: 20, Max: 1500),

        new("battery_health_pct", "Battery health", AttributeDataType.Integer, Unit: "%",
            DisplayGroup: "battery", SortOrder: 130, IsRequired: true, FacetStyle: FacetStyle.Range,
            Min: 0, Max: 100, Step: 1),

        new("charging_type", "Charging", AttributeDataType.MultiEnum, DisplayGroup: "battery",
            SortOrder: 140, FacetStyle: FacetStyle.Chips,
            Options:
            [
                new("ac_slow", "AC slow (3.3 kW)", 1), new("ac_fast", "AC fast (7.2 kW)", 2),
                new("dc_fast", "DC fast", 3), new("ccs2", "CCS2", 4),
                new("chademo", "CHAdeMO", 5), new("type2", "Type 2", 6),
            ]),

        // ── Commercial ───────────────────────────────────────────────────────────────────────────
        new("payload_kg", "Payload", AttributeDataType.Integer, Unit: "kg", DisplayGroup: "load",
            SortOrder: 210, IsRequired: true, FacetStyle: FacetStyle.Range, Min: 100, Max: 60_000),

        new("axle_config", "Axle configuration", AttributeDataType.Enum, DisplayGroup: "load",
            SortOrder: 220, FacetStyle: FacetStyle.List,
            Options:
            [
                new("4x2", "4x2", 1), new("6x2", "6x2", 2), new("6x4", "6x4", 3),
                new("8x2", "8x2", 4), new("8x4", "8x4", 5), new("10x2", "10x2", 6),
                new("12x2", "12x2", 7), new("14x2", "14x2", 8),
            ]),

        new("permit_type", "Permit", AttributeDataType.Enum, DisplayGroup: "compliance",
            SortOrder: 230, FacetStyle: FacetStyle.List,
            Options:
            [
                new("national", "National permit", 1), new("state", "State permit", 2),
                new("contract_carriage", "Contract carriage", 3),
                new("stage_carriage", "Stage carriage", 4), new("goods", "Goods carriage", 5),
                new("tourist", "All-India tourist", 6), new("private", "Private (non-transport)", 7),
            ]),

        // ── Construction — the north star's proof, and it is only data ───────────────────────────
        new("boom_length", "Boom length", AttributeDataType.Number, Unit: "m", DisplayGroup: "boom",
            SortOrder: 310, IsRequired: true, FacetStyle: FacetStyle.Range, Min: 1, Max: 90),

        new("operating_weight_kg", "Operating weight", AttributeDataType.Integer, Unit: "kg",
            DisplayGroup: "chassis", SortOrder: 320, FacetStyle: FacetStyle.Range,
            Min: 500, Max: 200_000),

        // ── Form vocabularies ────────────────────────────────────────────────────────────────────
        // Attached to no category, and not searchable, on purpose: none of these describes a
        // vehicle, so none belongs on a sell form or in the facet rail. They live here because a
        // form field's options come from this registry and nowhere else — that is the rule that
        // stops the same list of employment types being maintained in a React component, a Flutter
        // widget and a C# enum, and drifting in all three.
        new("check_result", "Check result", AttributeDataType.Enum, DisplayGroup: "inspection",
            SortOrder: 900, IsSearchable: false, FacetStyle: FacetStyle.List,
            Options:
            [
                new("ok", "OK", 1), new("minor_issue", "Minor issue", 2),
                new("major_issue", "Major issue", 3), new("not_applicable", "Not applicable", 4),
                new("not_checked", "Not checked", 5),
            ]),

        new("employment_type", "Employment", AttributeDataType.Enum, DisplayGroup: "applicant",
            SortOrder: 910, IsSearchable: false, FacetStyle: FacetStyle.List,
            Options:
            [
                new("salaried", "Salaried", 1), new("self_employed", "Self-employed professional", 2),
                new("business", "Business owner", 3), new("agriculture", "Agriculture", 4),
                new("retired", "Retired", 5), new("student", "Student", 6),
            ]),

        new("loan_purpose", "Purpose", AttributeDataType.Enum, DisplayGroup: "applicant",
            SortOrder: 920, IsSearchable: false, FacetStyle: FacetStyle.List,
            Options:
            [
                new("used_car_purchase", "Used car purchase", 1),
                new("new_car_purchase", "New car purchase", 2),
                new("refinance", "Refinance an existing loan", 3),
                new("top_up", "Top-up on an existing loan", 4),
                new("commercial_vehicle", "Commercial vehicle purchase", 5),
            ]),

        new("insurance_cover_type", "Cover", AttributeDataType.Enum, DisplayGroup: "cover",
            SortOrder: 930, IsSearchable: false, FacetStyle: FacetStyle.List,
            Options:
            [
                new("comprehensive", "Comprehensive", 1),
                new("third_party", "Third-party only", 2),
                new("own_damage", "Standalone own damage", 3),
            ]),

        // The no-claim bonus ladder is fixed by IRDAI, which is exactly why it is data: when the
        // regulator changes a slab, an operator edits a row instead of waiting for a release.
        new("ncb_slab", "No-claim bonus", AttributeDataType.Enum, DisplayGroup: "cover",
            SortOrder: 940, IsSearchable: false, FacetStyle: FacetStyle.List,
            Options:
            [
                new("none", "No NCB", 1), new("20", "20%", 2), new("25", "25%", 3),
                new("35", "35%", 4), new("45", "45%", 5), new("50", "50%", 6),
            ]),
    ];

    private async Task<int> SeedAttributesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.AttributeDefinitions
            .Select(definition => definition.Code)
            .ToListAsync(cancellationToken);

        var known = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var row in Attributes.Where(row => !known.Contains(row.Code)))
        {
            var definition = SeedGuard.Required(
                AttributeDefinition.Create(row.Code, row.Label, row.DataType),
                $"attribute {row.Code}");

            definition.SetPresentation(
                row.Unit, row.DisplayGroup, row.SortOrder, row.IsRequired, row.IsSearchable, row.FacetStyle);

            SeedGuard.Required(
                definition.SetNumericBounds(row.Min, row.Max, row.Step),
                $"attribute {row.Code} bounds");

            foreach (var option in row.Options)
            {
                SeedGuard.Required(
                    definition.AddOption(option.Value, option.Label, option.SortOrder),
                    $"attribute {row.Code} option {option.Value}");
            }

            // Proving it here rather than trusting the table: a facet style that cannot render is
            // invisible in a seed file and obvious on the search page.
            SeedGuard.Required(definition.Validate(), $"attribute {row.Code}");

            _context.AttributeDefinitions.Add(definition);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Category ↔ attribute attachment ──────────────────────────────────────────────────────────

    private static readonly string[] CarAttributes =
    [
        "manufacture_year", "fuel_type", "transmission", "body_type", "colour",
        "kms_driven", "owners", "seats", "insurance_valid_till", "asking_price",
    ];

    private static readonly string[] BikeAttributes =
    [
        "manufacture_year", "fuel_type", "transmission", "colour",
        "kms_driven", "owners", "insurance_valid_till", "asking_price",
    ];

    private static readonly string[] CommercialAttributes =
    [
        "manufacture_year", "fuel_type", "body_type", "kms_driven", "owners",
        "payload_kg", "axle_config", "permit_type", "insurance_valid_till", "asking_price",
    ];

    private static readonly string[] TaxiAttributes =
    [
        "manufacture_year", "fuel_type", "transmission", "colour", "kms_driven",
        "owners", "seats", "permit_type", "insurance_valid_till", "asking_price",
    ];

    private static readonly string[] ElectricAttributes =
    [
        "manufacture_year", "fuel_type", "transmission", "body_type", "colour",
        "kms_driven", "owners", "seats", "insurance_valid_till", "asking_price",
        "battery_kwh", "range_km", "battery_health_pct", "charging_type",
    ];

    private static readonly string[] ConstructionAttributes =
    [
        "manufacture_year", "fuel_type", "kms_driven", "owners", "asking_price",
        "boom_length", "operating_weight_kg",
    ];

    private async Task<int> SeedCategoryAttributesAsync(CancellationToken cancellationToken)
    {
        var categories = await _context.VehicleCategories
            .ToDictionaryAsync(category => category.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var attributes = await _context.AttributeDefinitions
            .ToDictionaryAsync(definition => definition.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existing = await _context.CategoryAttributes
            .Select(link => new { link.CategoryId, link.AttributeDefinitionId })
            .ToListAsync(cancellationToken);

        var known = existing
            .Select(link => (link.CategoryId, link.AttributeDefinitionId))
            .ToHashSet();

        var created = 0;

        // Attached at the roots only. Inheritance carries them down to hatchback, suv, truck and
        // every leaf added later, which is what keeps a nineteen-node tree from needing two hundred
        // join rows and what makes a new subcategory usable the moment it is inserted.
        created += Attach("car", CarAttributes);
        created += Attach("bike", BikeAttributes);
        created += Attach("commercial", CommercialAttributes);
        created += Attach("taxi", TaxiAttributes);
        created += Attach("ev", ElectricAttributes);
        created += Attach("construction", ConstructionAttributes);

        // Two overrides, both real. Colour is optional on a used hatchback and something a luxury
        // buyer filters on first, so `luxury` tightens it. A scooter has no gearbox, so `scooter`
        // hides a transmission question its sellers would otherwise have to answer meaninglessly —
        // and hides it without touching commuter, sports or cruiser.
        created += Override("luxury", "colour", isRequired: true, facetStyle: FacetStyle.Chips);
        created += Hide("scooter", "transmission");

        await _context.SaveChangesAsync(cancellationToken);
        return created;

        int Attach(string categoryCode, IEnumerable<string> attributeCodes)
        {
            var added = 0;

            foreach (var attributeCode in attributeCodes)
            {
                var key = (categories[categoryCode].Id, attributes[attributeCode].Id);

                if (!known.Add(key))
                {
                    continue;
                }

                _context.CategoryAttributes.Add(CategoryAttribute.Attach(key.Item1, key.Item2));
                added++;
            }

            return added;
        }

        int Override(string categoryCode, string attributeCode, bool isRequired, FacetStyle facetStyle)
        {
            var key = (categories[categoryCode].Id, attributes[attributeCode].Id);

            if (!known.Add(key))
            {
                return 0;
            }

            var link = CategoryAttribute.Attach(key.Item1, key.Item2);
            link.Override(isRequired: isRequired, facetStyle: facetStyle);
            _context.CategoryAttributes.Add(link);
            return 1;
        }

        int Hide(string categoryCode, string attributeCode)
        {
            var key = (categories[categoryCode].Id, attributes[attributeCode].Id);

            if (!known.Add(key))
            {
                return 0;
            }

            _context.CategoryAttributes.Add(CategoryAttribute.Hide(key.Item1, key.Item2));
            return 1;
        }
    }

    // ── Makes ────────────────────────────────────────────────────────────────────────────────────

    private static readonly TaxonomyMakeRow[] Makes =
    [
        new("maruti-suzuki", "Maruti Suzuki", "JP", 1),
        new("hyundai", "Hyundai", "KR", 2),
        new("tata", "Tata Motors", "IN", 3),
        new("mahindra", "Mahindra", "IN", 4),
        new("toyota", "Toyota", "JP", 5),
        new("honda", "Honda", "JP", 6),
        new("kia", "Kia", "KR", 7),
        new("mg", "MG", "GB", 8),
        new("skoda", "Skoda", "CZ", 9),
        new("volkswagen", "Volkswagen", "DE", 10),
        new("renault", "Renault", "FR", 11),
        new("nissan", "Nissan", "JP", 12),
        new("bmw", "BMW", "DE", 13),
        new("mercedes-benz", "Mercedes-Benz", "DE", 14),
        new("audi", "Audi", "DE", 15),
        new("hero", "Hero MotoCorp", "IN", 16),
        new("honda-2w", "Honda 2Wheelers", "JP", 17),
        new("bajaj", "Bajaj Auto", "IN", 18),
        new("tvs", "TVS Motor", "IN", 19),
        new("royal-enfield", "Royal Enfield", "IN", 20),
        new("ashok-leyland", "Ashok Leyland", "IN", 21),
        new("eicher", "Eicher", "IN", 22),
        new("force", "Force Motors", "IN", 23),
        new("byd", "BYD", "CN", 24),
        new("ola-electric", "Ola Electric", "IN", 25),
        new("ather", "Ather Energy", "IN", 26),
        // Not on the original brand list and deliberately added: the construction category is only a
        // proof if something can actually be listed under it.
        new("jcb", "JCB", "GB", 27),
    ];

    private async Task<int> SeedMakesAsync(CancellationToken cancellationToken)
    {
        var known = (await _context.VehicleMakes.Select(make => make.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in Makes.Where(row => !known.Contains(row.Code)))
        {
            var make = SeedGuard.Required(
                VehicleMake.Create(row.Code, row.Name, row.CountryOfOrigin, $"makes/{row.Code}.svg", row.SortOrder),
                $"make {row.Code}");

            _context.VehicleMakes.Add(make);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Models ───────────────────────────────────────────────────────────────────────────────────

    private static readonly TaxonomyModelRow[] Models =
    [
        new("swift", "Swift", "maruti-suzuki", "hatchback", 2005, null),
        new("baleno", "Baleno", "maruti-suzuki", "hatchback", 2015, null),
        new("wagon-r", "WagonR", "maruti-suzuki", "hatchback", 1999, null),
        new("dzire", "Dzire", "maruti-suzuki", "sedan", 2008, null),
        new("ertiga", "Ertiga", "maruti-suzuki", "muv", 2012, null),
        new("brezza", "Brezza", "maruti-suzuki", "suv", 2016, null),

        new("creta", "Creta", "hyundai", "suv", 2015, null),
        new("i20", "i20", "hyundai", "hatchback", 2008, null),
        new("venue", "Venue", "hyundai", "suv", 2019, null),
        new("verna", "Verna", "hyundai", "sedan", 2006, null),

        new("nexon", "Nexon", "tata", "suv", 2017, null),
        new("punch", "Punch", "tata", "suv", 2021, null),
        new("harrier", "Harrier", "tata", "suv", 2019, null),
        new("altroz", "Altroz", "tata", "hatchback", 2020, null),
        new("nexon-ev", "Nexon EV", "tata", "ev", 2020, null),
        new("tiago-ev", "Tiago EV", "tata", "ev", 2022, null),
        new("ace-gold", "Ace Gold", "tata", "tempo", 2005, null),

        new("scorpio-n", "Scorpio N", "mahindra", "suv", 2022, null),
        new("xuv700", "XUV700", "mahindra", "suv", 2021, null),
        new("thar", "Thar", "mahindra", "suv", 2010, null),
        new("bolero-pickup", "Bolero Pickup", "mahindra", "pickup", 2010, null),
        new("xuv300", "XUV300", "mahindra", "suv", 2019, null),

        new("innova-crysta", "Innova Crysta", "toyota", "muv", 2016, null),
        new("fortuner", "Fortuner", "toyota", "suv", 2009, null),
        new("glanza", "Glanza", "toyota", "hatchback", 2019, null),

        new("city", "City", "honda", "sedan", 1998, null),
        new("amaze", "Amaze", "honda", "sedan", 2013, null),

        new("seltos", "Seltos", "kia", "suv", 2019, null),
        new("sonet", "Sonet", "kia", "suv", 2020, null),
        new("carens", "Carens", "kia", "muv", 2022, null),

        new("hector", "Hector", "mg", "suv", 2019, null),
        new("kushaq", "Kushaq", "skoda", "suv", 2021, null),
        new("taigun", "Taigun", "volkswagen", "suv", 2021, null),
        new("kiger", "Kiger", "renault", "suv", 2021, null),
        new("magnite", "Magnite", "nissan", "suv", 2020, null),
        new("e6", "e6", "byd", "ev", 2021, null),

        new("3-series", "3 Series", "bmw", "luxury", 2005, null),
        new("c-class", "C-Class", "mercedes-benz", "luxury", 2004, null),
        new("a4", "A4", "audi", "luxury", 2008, null),

        new("splendor-plus", "Splendor Plus", "hero", "commuter", 2000, null),
        new("pulsar-150", "Pulsar 150", "bajaj", "sports", 2001, null),
        new("classic-350", "Classic 350", "royal-enfield", "cruiser", 2009, null),
        new("activa-6g", "Activa 6G", "honda-2w", "scooter", 2020, null),
        new("jupiter", "Jupiter", "tvs", "scooter", 2013, null),
        new("ather-450x", "450X", "ather", "scooter", 2020, null),
        new("ola-s1-pro", "S1 Pro", "ola-electric", "scooter", 2021, null),

        new("dost", "Dost", "ashok-leyland", "pickup", 2011, null),
        new("boss-1115", "Boss 1115 HB", "ashok-leyland", "truck", 2015, null),
        new("pro-2049", "Pro 2049", "eicher", "truck", 2016, null),
        new("traveller-26", "Traveller 26 Seater", "force", "bus", 2012, null),

        new("3dx-backhoe", "3DX Backhoe Loader", "jcb", "construction", 2010, null),
    ];

    private async Task<int> SeedModelsAsync(CancellationToken cancellationToken)
    {
        var makes = await _context.VehicleMakes
            .ToDictionaryAsync(make => make.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var categories = await _context.VehicleCategories
            .ToDictionaryAsync(category => category.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var known = (await _context.VehicleModels.Select(model => model.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in Models.Where(row => !known.Contains(row.Code)))
        {
            var model = SeedGuard.Required(
                VehicleModel.Create(
                    makes[row.MakeCode].Id,
                    categories[row.CategoryCode].Id,
                    row.Code,
                    row.Name,
                    row.ProductionFrom,
                    row.ProductionTo),
                $"model {row.Code}");

            _context.VehicleModels.Add(model);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Variants ─────────────────────────────────────────────────────────────────────────────────
    //
    // Trim is where a valuation stops guessing: two 2021 Swifts differ by more than a lakh between
    // LXi and ZXi+. These are the real Indian trim ladders, with the powertrain codes pointing at
    // option values in this same registry rather than at an enum.

    // A taxonomy code is two characters minimum, which the single-letter Indian base trims — Creta E,
    // City V, Glanza G — do not satisfy on their own. They carry the transmission they are actually
    // sold with rather than an invented suffix, so `e-mt` still reads as a real trim in a URL.
    private static readonly TaxonomyVariantRow[] Variants =
    [
        // Maruti Suzuki
        new("swift", "lxi", "LXi", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2018, null),
        new("swift", "vxi", "VXi", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2018, null),
        new("swift", "vxi-amt", "VXi AMT", "petrol", "amt", "hatchback", 5, 1197, 88.5m, 2018, null),
        new("swift", "zxi", "ZXi", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2018, null),
        new("swift", "zxi-plus", "ZXi+", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2018, null),
        new("swift", "zxi-plus-amt", "ZXi+ AMT", "petrol", "amt", "hatchback", 5, 1197, 88.5m, 2018, null),

        new("baleno", "sigma", "Sigma", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2019, null),
        new("baleno", "delta", "Delta", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2019, null),
        new("baleno", "zeta", "Zeta", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2019, null),
        new("baleno", "alpha", "Alpha", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2019, null),
        new("baleno", "alpha-amt", "Alpha AMT", "petrol", "amt", "hatchback", 5, 1197, 88.5m, 2019, null),

        new("wagon-r", "lxi", "LXi", "petrol", "manual", "hatchback", 5, 998, 65.7m, 2019, null),
        new("wagon-r", "vxi", "VXi", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2019, null),
        new("wagon-r", "vxi-cng", "VXi CNG", "cng", "manual", "hatchback", 5, 998, 56.7m, 2019, null),
        new("wagon-r", "zxi-plus", "ZXi+", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2019, null),

        new("dzire", "lxi", "LXi", "petrol", "manual", "sedan", 5, 1197, 88.5m, 2017, null),
        new("dzire", "vxi", "VXi", "petrol", "manual", "sedan", 5, 1197, 88.5m, 2017, null),
        new("dzire", "vdi", "VDi", "diesel", "manual", "sedan", 5, 1248, 74.0m, 2017, 2020),
        new("dzire", "zxi", "ZXi", "petrol", "manual", "sedan", 5, 1197, 88.5m, 2017, null),
        new("dzire", "zxi-plus-amt", "ZXi+ AMT", "petrol", "amt", "sedan", 5, 1197, 88.5m, 2017, null),

        new("ertiga", "lxi", "LXi", "petrol", "manual", "muv", 7, 1462, 101.6m, 2018, null),
        new("ertiga", "vxi", "VXi", "petrol", "manual", "muv", 7, 1462, 101.6m, 2018, null),
        new("ertiga", "zxi-plus", "ZXi+", "petrol", "manual", "muv", 7, 1462, 101.6m, 2018, null),

        new("brezza", "lxi", "LXi", "petrol", "manual", "suv", 5, 1462, 101.6m, 2022, null),
        new("brezza", "vxi", "VXi", "petrol", "manual", "suv", 5, 1462, 101.6m, 2022, null),
        new("brezza", "zxi-plus", "ZXi+", "petrol", "automatic", "suv", 5, 1462, 101.6m, 2022, null),

        // Hyundai
        new("creta", "e-mt", "E", "petrol", "manual", "suv", 5, 1497, 113.0m, 2020, null),
        new("creta", "ex", "EX", "diesel", "manual", "suv", 5, 1493, 113.4m, 2020, null),
        new("creta", "sx", "SX", "petrol", "manual", "suv", 5, 1497, 113.0m, 2020, null),
        new("creta", "sx-opt", "SX(O)", "diesel", "automatic", "suv", 5, 1493, 113.4m, 2020, null),
        new("creta", "sx-opt-turbo-dct", "SX(O) Turbo DCT", "petrol", "dct", "suv", 5, 1353, 138.0m, 2020, null),

        new("i20", "magna", "Magna", "petrol", "manual", "hatchback", 5, 1197, 82.0m, 2020, null),
        new("i20", "sportz", "Sportz", "petrol", "manual", "hatchback", 5, 1197, 82.0m, 2020, null),
        new("i20", "asta", "Asta", "petrol", "manual", "hatchback", 5, 1197, 82.0m, 2020, null),
        new("i20", "asta-opt-turbo-dct", "Asta(O) Turbo DCT", "petrol", "dct", "hatchback", 5, 998, 118.0m, 2020, null),

        new("venue", "e-mt", "E", "petrol", "manual", "suv", 5, 1197, 82.0m, 2019, null),
        new("venue", "s-plus", "S+", "petrol", "manual", "suv", 5, 1197, 82.0m, 2019, null),
        new("venue", "sx", "SX", "petrol", "imt", "suv", 5, 998, 118.0m, 2019, null),
        new("venue", "sx-opt", "SX(O)", "petrol", "dct", "suv", 5, 998, 118.0m, 2019, null),

        new("verna", "e-mt", "E", "petrol", "manual", "sedan", 5, 1497, 113.4m, 2020, null),
        new("verna", "sx", "SX", "petrol", "manual", "sedan", 5, 1497, 113.4m, 2020, null),
        new("verna", "sx-opt-turbo", "SX(O) Turbo", "petrol", "dct", "sedan", 5, 1482, 158.0m, 2020, null),

        // Tata
        new("nexon", "xe", "XE", "petrol", "manual", "suv", 5, 1199, 118.0m, 2020, null),
        new("nexon", "xm", "XM", "petrol", "manual", "suv", 5, 1199, 118.0m, 2020, null),
        new("nexon", "xz-plus", "XZ+", "petrol", "manual", "suv", 5, 1199, 118.0m, 2020, null),
        new("nexon", "xz-plus-diesel", "XZ+ Diesel", "diesel", "manual", "suv", 5, 1497, 108.5m, 2020, null),
        new("nexon", "xza-plus", "XZA+ AMT", "petrol", "amt", "suv", 5, 1199, 118.0m, 2020, null),

        new("punch", "pure", "Pure", "petrol", "manual", "suv", 5, 1199, 86.0m, 2021, null),
        new("punch", "adventure", "Adventure", "petrol", "manual", "suv", 5, 1199, 86.0m, 2021, null),
        new("punch", "accomplished", "Accomplished", "petrol", "amt", "suv", 5, 1199, 86.0m, 2021, null),
        new("punch", "creative", "Creative", "petrol", "amt", "suv", 5, 1199, 86.0m, 2021, null),

        new("harrier", "xe", "XE", "diesel", "manual", "suv", 5, 1956, 168.0m, 2020, null),
        new("harrier", "xz", "XZ", "diesel", "manual", "suv", 5, 1956, 168.0m, 2020, null),
        new("harrier", "xz-plus", "XZ+", "diesel", "automatic", "suv", 5, 1956, 168.0m, 2020, null),

        new("altroz", "xe", "XE", "petrol", "manual", "hatchback", 5, 1199, 86.0m, 2020, null),
        new("altroz", "xt", "XT", "petrol", "manual", "hatchback", 5, 1199, 86.0m, 2020, null),
        new("altroz", "xz-plus", "XZ+", "petrol", "manual", "hatchback", 5, 1199, 86.0m, 2020, null),

        new("nexon-ev", "xm-prime", "XM Prime", "ev", "automatic", "suv", 5, null, 127.0m, 2020, null),
        new("nexon-ev", "xz-plus-prime", "XZ+ Prime", "ev", "automatic", "suv", 5, null, 127.0m, 2020, null),
        new("nexon-ev", "xz-plus-lux-max", "XZ+ Lux Max", "ev", "automatic", "suv", 5, null, 143.0m, 2022, null),

        new("tiago-ev", "xe-mr", "XE Medium Range", "ev", "automatic", "hatchback", 5, null, 60.0m, 2022, null),
        new("tiago-ev", "xz-plus-lr", "XZ+ Long Range", "ev", "automatic", "hatchback", 5, null, 74.0m, 2022, null),

        new("ace-gold", "cx", "CX", "diesel", "manual", "closed_container", 2, 694, 20.0m, 2020, null),
        new("ace-gold", "cng-plus", "CNG+", "cng", "manual", "open", 2, 694, 25.0m, 2021, null),

        // Mahindra
        new("scorpio-n", "z2", "Z2", "petrol", "manual", "suv", 7, 1997, 200.0m, 2022, null),
        new("scorpio-n", "z4", "Z4", "diesel", "manual", "suv", 7, 2184, 172.0m, 2022, null),
        new("scorpio-n", "z8", "Z8", "diesel", "automatic", "suv", 7, 2184, 172.0m, 2022, null),
        new("scorpio-n", "z8l", "Z8L", "diesel", "automatic", "suv", 7, 2184, 172.0m, 2022, null),

        new("xuv700", "mx", "MX", "petrol", "manual", "suv", 5, 1999, 197.0m, 2021, null),
        new("xuv700", "ax5", "AX5", "diesel", "manual", "suv", 7, 2198, 182.0m, 2021, null),
        new("xuv700", "ax7", "AX7", "diesel", "automatic", "suv", 7, 2198, 182.0m, 2021, null),
        new("xuv700", "ax7-luxury", "AX7 Luxury", "diesel", "automatic", "suv", 7, 2198, 182.0m, 2022, null),

        new("thar", "ax-opt", "AX(O) Hard Top", "diesel", "manual", "suv", 4, 2184, 130.0m, 2020, null),
        new("thar", "lx-hard-top", "LX Hard Top", "diesel", "automatic", "suv", 4, 2184, 130.0m, 2020, null),
        new("thar", "lx-petrol-at", "LX Petrol AT", "petrol", "automatic", "suv", 4, 1997, 150.0m, 2020, null),

        new("bolero-pickup", "1-2t", "1.2T Extra Long", "diesel", "manual", "pickup", 2, 2523, 70.0m, 2018, null),
        new("bolero-pickup", "1-7t", "1.7T FB", "diesel", "manual", "pickup", 2, 2523, 75.0m, 2018, null),

        new("xuv300", "w4", "W4", "petrol", "manual", "suv", 5, 1197, 110.0m, 2019, null),
        new("xuv300", "w8-opt", "W8(O)", "diesel", "manual", "suv", 5, 1497, 117.0m, 2019, null),

        // Toyota
        new("innova-crysta", "gx", "GX", "diesel", "manual", "muv", 7, 2393, 148.0m, 2018, null),
        new("innova-crysta", "vx", "VX", "diesel", "manual", "muv", 7, 2755, 174.0m, 2018, null),
        new("innova-crysta", "zx-at", "ZX AT", "diesel", "automatic", "muv", 7, 2755, 174.0m, 2018, null),

        new("fortuner", "4x2-mt", "4x2 MT", "diesel", "manual", "suv", 7, 2755, 201.0m, 2018, null),
        new("fortuner", "4x2-at", "4x2 AT", "diesel", "automatic", "suv", 7, 2755, 201.0m, 2018, null),
        new("fortuner", "legender", "Legender 4x2 AT", "diesel", "automatic", "suv", 7, 2755, 201.0m, 2021, null),

        new("glanza", "g-mt", "G", "petrol", "manual", "hatchback", 5, 1197, 88.5m, 2019, null),
        new("glanza", "v-amt", "V", "petrol", "amt", "hatchback", 5, 1197, 88.5m, 2019, null),

        // Honda
        new("city", "sv", "SV", "petrol", "manual", "sedan", 5, 1498, 119.0m, 2017, null),
        new("city", "v-mt", "V", "petrol", "manual", "sedan", 5, 1498, 119.0m, 2017, null),
        new("city", "vx-cvt", "VX CVT", "petrol", "cvt", "sedan", 5, 1498, 119.0m, 2017, null),
        new("city", "zx", "ZX", "petrol", "cvt", "sedan", 5, 1498, 119.0m, 2020, null),

        new("amaze", "e-mt", "E", "petrol", "manual", "sedan", 5, 1199, 90.0m, 2018, null),
        new("amaze", "vx-cvt", "VX CVT", "petrol", "cvt", "sedan", 5, 1199, 90.0m, 2018, null),

        // Kia
        new("seltos", "htk", "HTK", "petrol", "manual", "suv", 5, 1497, 115.0m, 2019, null),
        new("seltos", "htk-plus", "HTK+", "diesel", "manual", "suv", 5, 1493, 115.0m, 2019, null),
        new("seltos", "htx", "HTX", "petrol", "manual", "suv", 5, 1497, 115.0m, 2019, null),
        new("seltos", "gtx-plus-dct", "GTX+ DCT", "petrol", "dct", "suv", 5, 1353, 138.0m, 2019, null),

        new("sonet", "htk-plus", "HTK+", "petrol", "manual", "suv", 5, 1197, 83.0m, 2020, null),
        new("sonet", "htx", "HTX", "diesel", "manual", "suv", 5, 1493, 100.0m, 2020, null),
        new("sonet", "gtx-plus", "GTX+", "petrol", "dct", "suv", 5, 998, 120.0m, 2020, null),

        new("carens", "premium", "Premium", "petrol", "manual", "muv", 7, 1497, 115.0m, 2022, null),
        new("carens", "luxury-plus", "Luxury Plus", "diesel", "automatic", "muv", 7, 1493, 115.0m, 2022, null),

        // Other cars
        new("hector", "style", "Style", "petrol", "manual", "suv", 5, 1451, 143.0m, 2019, null),
        new("hector", "sharp", "Sharp", "diesel", "manual", "suv", 5, 1956, 170.0m, 2019, null),
        new("kushaq", "ambition", "Ambition 1.0 TSI", "petrol", "manual", "suv", 5, 999, 115.0m, 2021, null),
        new("kushaq", "style-at", "Style 1.5 TSI AT", "petrol", "dct", "suv", 5, 1498, 150.0m, 2021, null),
        new("taigun", "highline", "Highline 1.0 TSI", "petrol", "manual", "suv", 5, 999, 115.0m, 2021, null),
        new("kiger", "rxt", "RXT Turbo", "petrol", "manual", "suv", 5, 999, 100.0m, 2021, null),
        new("magnite", "xv-premium", "XV Premium Turbo", "petrol", "cvt", "suv", 5, 999, 100.0m, 2020, null),
        new("e6", "premium", "Premium", "ev", "automatic", "muv", 5, null, 94.0m, 2021, null),

        new("3-series", "330i-m-sport", "330i M Sport", "petrol", "automatic", "sedan", 5, 1998, 258.0m, 2019, null),
        new("c-class", "c220d", "C 220d Progressive", "diesel", "automatic", "sedan", 5, 1993, 200.0m, 2018, null),
        new("a4", "premium-plus", "Premium Plus 40 TFSI", "petrol", "automatic", "sedan", 5, 1984, 190.0m, 2019, null),

        // Two-wheelers
        new("splendor-plus", "drum", "Drum Brake", "petrol", "manual", null, 2, 97, 8.0m, 2018, null),
        new("splendor-plus", "i3s", "i3S Disc", "petrol", "manual", null, 2, 97, 8.0m, 2019, null),
        new("pulsar-150", "single-disc", "Single Disc", "petrol", "manual", null, 2, 149, 14.0m, 2018, null),
        new("pulsar-150", "twin-disc", "Twin Disc", "petrol", "manual", null, 2, 149, 14.0m, 2019, null),
        new("classic-350", "halcyon", "Halcyon", "petrol", "manual", null, 2, 349, 20.2m, 2021, null),
        new("classic-350", "chrome", "Chrome", "petrol", "manual", null, 2, 349, 20.2m, 2021, null),
        new("activa-6g", "standard", "Standard", "petrol", "automatic", null, 2, 109, 7.7m, 2020, null),
        new("activa-6g", "deluxe", "Deluxe", "petrol", "automatic", null, 2, 109, 7.7m, 2020, null),
        new("jupiter", "zx", "ZX Drum", "petrol", "automatic", null, 2, 109, 7.4m, 2018, null),
        new("ather-450x", "gen3", "450X Gen 3", "ev", "automatic", null, 2, null, 8.6m, 2022, null),
        new("ola-s1-pro", "gen2", "S1 Pro Gen 2", "ev", "automatic", null, 2, null, 11.0m, 2023, null),

        // Commercial and construction
        new("dost", "lx", "Dost LX", "diesel", "manual", "open", 2, 1478, 58.0m, 2018, null),
        new("dost", "plus-cng", "Dost+ CNG", "cng", "manual", "closed_container", 2, 1478, 55.0m, 2020, null),
        new("boss-1115", "hb", "1115 HB 4x2", "diesel", "manual", "open", 3, 3839, 150.0m, 2018, null),
        new("pro-2049", "cbc", "Pro 2049 CBC", "diesel", "manual", "open", 3, 2956, 80.0m, 2018, null),
        new("traveller-26", "school", "26 Seater School Bus", "diesel", "manual", "bus_body", 26, 2596, 90.0m, 2016, null),
        // Seat count is left unset on construction equipment on purpose: a backhoe has an operator
        // station, not passenger seats, and reporting "1 seat" would put it on a comfort facet
        // buyers use to choose between a five-seat hatchback and a seven-seat MUV.
        new("3dx-backhoe", "super", "3DX Super", "diesel", "manual", "backhoe", null, 4400, 76.0m, 2015, null),
        new("3dx-backhoe", "xtra", "3DX Xtra", "diesel", "manual", "backhoe", null, 4400, 92.0m, 2019, null),
    ];

    private async Task<int> SeedVariantsAsync(CancellationToken cancellationToken)
    {
        var models = await _context.VehicleModels
            .ToDictionaryAsync(model => model.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existing = await _context.VehicleVariants
            .Select(variant => new { variant.ModelId, variant.Code })
            .ToListAsync(cancellationToken);

        var known = existing
            .Select(variant => (variant.ModelId, variant.Code.ToUpperInvariant()))
            .ToHashSet();

        var created = 0;

        foreach (var row in Variants)
        {
            var modelId = models[row.ModelCode].Id;

            if (!known.Add((modelId, row.Code.ToUpperInvariant())))
            {
                continue;
            }

            var variant = SeedGuard.Required(
                VehicleVariant.Create(modelId, row.Code, row.Name, row.YearFrom, row.YearTo),
                $"variant {row.ModelCode}/{row.Code}");

            variant.SetPowertrain(
                row.FuelCode, row.TransmissionCode, row.BodyTypeCode, row.Seats, row.EngineCc, row.PowerBhp);

            // The spec bag is display-only by design: anything a buyer filters or sorts on is
            // promoted to an attribute definition and stored as an indexed EAV row instead.
            SeedGuard.Required(
                variant.SetSpec(new JsonObject
                {
                    ["displayName"] = $"{models[row.ModelCode].Name} {row.Name}",
                    ["engineCc"] = row.EngineCc,
                    ["powerBhp"] = row.PowerBhp,
                }),
                $"variant {row.ModelCode}/{row.Code} spec");

            _context.VehicleVariants.Add(variant);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }
}

internal sealed record TaxonomyCategoryRow(
    string Code,
    string Name,
    string Icon,
    string? ParentCode,
    int SortOrder);

internal sealed record TaxonomyAttributeOptionRow(string Value, string Label, int SortOrder);

internal sealed record TaxonomyAttributeRow(
    string Code,
    string Label,
    AttributeDataType DataType,
    string? Unit = null,
    string? DisplayGroup = null,
    int SortOrder = 0,
    bool IsRequired = false,
    bool IsSearchable = true,
    FacetStyle FacetStyle = FacetStyle.List,
    decimal? Min = null,
    decimal? Max = null,
    decimal? Step = null,
    IReadOnlyList<TaxonomyAttributeOptionRow>? Options = null)
{
    public IReadOnlyList<TaxonomyAttributeOptionRow> Options { get; } = Options ?? [];
}

internal sealed record TaxonomyMakeRow(string Code, string Name, string CountryOfOrigin, int SortOrder);

internal sealed record TaxonomyModelRow(
    string Code,
    string Name,
    string MakeCode,
    string CategoryCode,
    int? ProductionFrom,
    int? ProductionTo);

internal sealed record TaxonomyVariantRow(
    string ModelCode,
    string Code,
    string Name,
    string? FuelCode,
    string? TransmissionCode,
    string? BodyTypeCode,
    int? Seats,
    int? EngineCc,
    decimal? PowerBhp,
    int? YearFrom,
    int? YearTo);
