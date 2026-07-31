using AutoHub.Platform.Geography;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Registry #10 — India as the product actually needs to know it.
/// </summary>
/// <remarks>
/// Three separate facts live here and are deliberately not collapsed into one. A <b>state</b> is
/// keyed by the two letters stamped on a number plate, because that is what a user types and what
/// VAHAN returns. An <b>RTO office</b> turns <c>MH12</c> into a real place, which is the half of
/// plate parsing that <c>RegistrationNumber</c> refuses to hardcode. A <b>city</b> exists whether or
/// not the business operates in it, and <see cref="City.EnabledModules"/> is what "operates" means —
/// so opening Nashik for auction is one row edit and no deployment.
/// <para>
/// GST state codes travel with the states because the invoice engine needs them and the alternative
/// is a second lookup table that has to agree with this one. Getting Maharashtra wrong means
/// charging IGST where CGST plus SGST was due, which is a filing correction rather than a bug report.
/// </para>
/// </remarks>
internal sealed class GeographySeeder
{
    private const string CountryCode = "IN";

    private readonly AutoHubDbContext _context;

    public GeographySeeder(AutoHubDbContext context) => _context = context;

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var created = 0;

        var countryId = await SeedCountryAsync(cancellationToken);
        created += await SeedStatesAsync(countryId, cancellationToken);
        created += await SeedCitiesAsync(cancellationToken);
        created += await SeedRtoOfficesAsync(cancellationToken);
        created += await SeedPincodesAsync(cancellationToken);
        created += await SeedServiceAreasAsync(cancellationToken);

        var total = await _context.States.CountAsync(cancellationToken)
                    + await _context.Cities.CountAsync(cancellationToken)
                    + await _context.RtoOffices.CountAsync(cancellationToken)
                    + await _context.Pincodes.CountAsync(cancellationToken)
                    + await _context.ServiceAreas.CountAsync(cancellationToken);

        return new SeedStepResult("geography", created, total - created);
    }

    private async Task<Guid> SeedCountryAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.Countries
            .FirstOrDefaultAsync(country => country.Code == CountryCode, cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        var india = SeedGuard.Required(
            Country.Create(CountryCode, "India", "+91", "INR"),
            "country IN");

        _context.Countries.Add(india);
        await _context.SaveChangesAsync(cancellationToken);
        return india.Id;
    }

    // ── States and union territories ─────────────────────────────────────────────────────────────
    // All 28 states and all 8 union territories, keyed by RTO code, carrying the GST state code.

    private static readonly GeoStateRow[] States =
    [
        new("AP", "Andhra Pradesh", "37"),
        new("AR", "Arunachal Pradesh", "12"),
        new("AS", "Assam", "18"),
        new("BR", "Bihar", "10"),
        new("CG", "Chhattisgarh", "22"),
        new("GA", "Goa", "30"),
        new("GJ", "Gujarat", "24"),
        new("HR", "Haryana", "06"),
        new("HP", "Himachal Pradesh", "02"),
        new("JH", "Jharkhand", "20"),
        new("KA", "Karnataka", "29"),
        new("KL", "Kerala", "32"),
        new("MP", "Madhya Pradesh", "23"),
        new("MH", "Maharashtra", "27"),
        new("MN", "Manipur", "14"),
        new("ML", "Meghalaya", "17"),
        new("MZ", "Mizoram", "15"),
        new("NL", "Nagaland", "13"),
        new("OD", "Odisha", "21"),
        new("PB", "Punjab", "03"),
        new("RJ", "Rajasthan", "08"),
        new("SK", "Sikkim", "11"),
        new("TN", "Tamil Nadu", "33"),
        new("TS", "Telangana", "36"),
        new("TR", "Tripura", "16"),
        new("UP", "Uttar Pradesh", "09"),
        new("UK", "Uttarakhand", "05"),
        new("WB", "West Bengal", "19"),

        new("AN", "Andaman and Nicobar Islands", "35"),
        new("CH", "Chandigarh", "04"),
        new("DD", "Dadra and Nagar Haveli and Daman and Diu", "26"),
        new("DL", "Delhi", "07"),
        new("JK", "Jammu and Kashmir", "01"),
        new("LA", "Ladakh", "38"),
        new("LD", "Lakshadweep", "31"),
        new("PY", "Puducherry", "34"),
    ];

    private async Task<int> SeedStatesAsync(Guid countryId, CancellationToken cancellationToken)
    {
        var known = (await _context.States.Select(state => state.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in States.Where(row => !known.Contains(row.Code)))
        {
            _context.States.Add(SeedGuard.Required(
                State.Create(countryId, row.Code, row.Name, row.GstStateCode),
                $"state {row.Code}"));

            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Cities ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>The full offering. Cities with a yard, an inspector and a delivery partner.</summary>
    private static readonly string[] FullService =
        ["buy", "sell", "auction", "rental", "attachment", "inspection", "finance", "insurance"];

    /// <summary>Marketplace only. No yard, so no auction; no inspector, so no inspection.</summary>
    private static readonly string[] MarketplaceOnly = ["buy", "sell", "finance", "insurance"];

    /// <summary>Listed but not launched. A seller here still has a valid address and a real RTO.</summary>
    private static readonly string[] NotLaunched = [];

    private static readonly GeoCityRow[] Cities =
    [
        new("pune", "Pune", "MH", 18.5204, 73.8567, FullService, 1),
        new("mumbai", "Mumbai", "MH", 19.0760, 72.8777, FullService, 2),
        new("bengaluru", "Bengaluru", "KA", 12.9716, 77.5946, FullService, 3),
        new("delhi", "Delhi", "DL", 28.6139, 77.2090, FullService, 4),
        new("hyderabad", "Hyderabad", "TS", 17.3850, 78.4867, FullService, 5),
        new("chennai", "Chennai", "TN", 13.0827, 80.2707, FullService, 6),
        new("ahmedabad", "Ahmedabad", "GJ", 23.0225, 72.5714, FullService, 7),
        new("kolkata", "Kolkata", "WB", 22.5726, 88.3639, MarketplaceOnly, 8),

        new("thane", "Thane", "MH", 19.2183, 72.9781, MarketplaceOnly, 10),
        new("navi-mumbai", "Navi Mumbai", "MH", 19.0330, 73.0297, MarketplaceOnly, 11),
        new("nashik", "Nashik", "MH", 19.9975, 73.7898, MarketplaceOnly, 12),
        new("nagpur", "Nagpur", "MH", 21.1458, 79.0882, MarketplaceOnly, 13),
        new("chhatrapati-sambhajinagar", "Chhatrapati Sambhajinagar", "MH", 19.8762, 75.3433, NotLaunched, 14),
        new("kolhapur", "Kolhapur", "MH", 16.7050, 74.2433, NotLaunched, 15),

        new("gurugram", "Gurugram", "HR", 28.4595, 77.0266, FullService, 20),
        new("noida", "Noida", "UP", 28.5355, 77.3910, MarketplaceOnly, 21),
        new("faridabad", "Faridabad", "HR", 28.4089, 77.3178, NotLaunched, 22),
        new("ghaziabad", "Ghaziabad", "UP", 28.6692, 77.4538, NotLaunched, 23),

        new("mysuru", "Mysuru", "KA", 12.2958, 76.6394, MarketplaceOnly, 30),
        new("mangaluru", "Mangaluru", "KA", 12.9141, 74.8560, NotLaunched, 31),
        new("hubballi", "Hubballi", "KA", 15.3647, 75.1240, NotLaunched, 32),

        new("coimbatore", "Coimbatore", "TN", 11.0168, 76.9558, MarketplaceOnly, 40),
        new("madurai", "Madurai", "TN", 9.9252, 78.1198, NotLaunched, 41),
        new("kochi", "Kochi", "KL", 9.9312, 76.2673, MarketplaceOnly, 42),
        new("thiruvananthapuram", "Thiruvananthapuram", "KL", 8.5241, 76.9366, NotLaunched, 43),

        new("visakhapatnam", "Visakhapatnam", "AP", 17.6868, 83.2185, MarketplaceOnly, 50),
        new("vijayawada", "Vijayawada", "AP", 16.5062, 80.6480, NotLaunched, 51),

        new("surat", "Surat", "GJ", 21.1702, 72.8311, MarketplaceOnly, 60),
        new("vadodara", "Vadodara", "GJ", 22.3072, 73.1812, MarketplaceOnly, 61),
        new("rajkot", "Rajkot", "GJ", 22.3039, 70.8022, NotLaunched, 62),

        new("jaipur", "Jaipur", "RJ", 26.9124, 75.7873, MarketplaceOnly, 70),
        new("jodhpur", "Jodhpur", "RJ", 26.2389, 73.0243, NotLaunched, 71),
        new("udaipur", "Udaipur", "RJ", 24.5854, 73.7125, NotLaunched, 72),

        new("lucknow", "Lucknow", "UP", 26.8467, 80.9462, MarketplaceOnly, 80),
        new("kanpur", "Kanpur", "UP", 26.4499, 80.3319, NotLaunched, 81),
        new("agra", "Agra", "UP", 27.1767, 78.0081, NotLaunched, 82),
        new("varanasi", "Varanasi", "UP", 25.3176, 82.9739, NotLaunched, 83),

        new("indore", "Indore", "MP", 22.7196, 75.8577, MarketplaceOnly, 90),
        new("bhopal", "Bhopal", "MP", 23.2599, 77.4126, NotLaunched, 91),

        new("chandigarh", "Chandigarh", "CH", 30.7333, 76.7794, MarketplaceOnly, 100),
        new("ludhiana", "Ludhiana", "PB", 30.9010, 75.8573, NotLaunched, 101),
        new("amritsar", "Amritsar", "PB", 31.6340, 74.8723, NotLaunched, 102),

        new("panaji", "Panaji", "GA", 15.4909, 73.8278, MarketplaceOnly, 110),
        new("patna", "Patna", "BR", 25.5941, 85.1376, NotLaunched, 111),
        new("ranchi", "Ranchi", "JH", 23.3441, 85.3096, NotLaunched, 112),
        new("bhubaneswar", "Bhubaneswar", "OD", 20.2961, 85.8245, NotLaunched, 113),
        new("raipur", "Raipur", "CG", 21.2514, 81.6296, NotLaunched, 114),
        new("guwahati", "Guwahati", "AS", 26.1445, 91.7362, NotLaunched, 115),
        new("dehradun", "Dehradun", "UK", 30.3165, 78.0322, NotLaunched, 116),
        new("siliguri", "Siliguri", "WB", 26.7271, 88.3953, NotLaunched, 117),
    ];

    private async Task<int> SeedCitiesAsync(CancellationToken cancellationToken)
    {
        var states = await _context.States
            .ToDictionaryAsync(state => state.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var known = (await _context.Cities.Select(city => city.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in Cities.Where(row => !known.Contains(row.Code)))
        {
            var city = SeedGuard.Required(
                City.Create(states[row.StateCode].Id, row.Code, row.Name, row.Latitude, row.Longitude),
                $"city {row.Code}");

            city.Reposition(row.Latitude, row.Longitude, row.SortOrder);
            city.OpenFor(row.Modules);

            _context.Cities.Add(city);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── RTO offices ──────────────────────────────────────────────────────────────────────────────

    private static readonly GeoRtoRow[] RtoOffices =
    [
        new("MH01", "Mumbai Central (Tardeo)", "MH", "mumbai"),
        new("MH02", "Mumbai West (Andheri)", "MH", "mumbai"),
        new("MH03", "Mumbai East (Wadala)", "MH", "mumbai"),
        new("MH04", "Thane", "MH", "thane"),
        new("MH05", "Kalyan", "MH", "thane"),
        new("MH12", "Pune", "MH", "pune"),
        new("MH14", "Pimpri-Chinchwad", "MH", "pune"),
        new("MH15", "Nashik", "MH", "nashik"),
        new("MH20", "Chhatrapati Sambhajinagar", "MH", "chhatrapati-sambhajinagar"),
        new("MH31", "Nagpur", "MH", "nagpur"),
        new("MH43", "Navi Mumbai (Vashi)", "MH", "navi-mumbai"),
        new("MH46", "Panvel", "MH", "navi-mumbai"),
        new("MH47", "Mumbai North (Borivali)", "MH", "mumbai"),

        new("KA01", "Bengaluru Central (Koramangala)", "KA", "bengaluru"),
        new("KA02", "Bengaluru West (Rajajinagar)", "KA", "bengaluru"),
        new("KA03", "Bengaluru East (Indiranagar)", "KA", "bengaluru"),
        new("KA04", "Bengaluru North (Yeshwanthpur)", "KA", "bengaluru"),
        new("KA05", "Bengaluru South (Jayanagar)", "KA", "bengaluru"),
        new("KA41", "Bengaluru North East (Jnanabharathi)", "KA", "bengaluru"),
        new("KA51", "Bengaluru East (KR Puram)", "KA", "bengaluru"),
        new("KA09", "Mysuru", "KA", "mysuru"),

        new("DL01", "Delhi (Mall Road)", "DL", "delhi"),
        new("DL02", "Delhi (Tilak Marg)", "DL", "delhi"),
        new("DL03", "Delhi (Sheikh Sarai)", "DL", "delhi"),
        new("DL04", "Delhi (Janakpuri)", "DL", "delhi"),
        new("DL05", "Delhi (Loni Road)", "DL", "delhi"),
        new("DL07", "Delhi (Mayur Vihar)", "DL", "delhi"),
        new("DL08", "Delhi (Wazirpur)", "DL", "delhi"),
        new("DL09", "Delhi (Dwarka)", "DL", "delhi"),
        new("DL10", "Delhi (Rajouri Garden)", "DL", "delhi"),
        new("DL12", "Delhi (Vasant Vihar)", "DL", "delhi"),
        new("DL13", "Delhi (Surajmal Vihar)", "DL", "delhi"),

        new("TN01", "Chennai Central (Ayanavaram)", "TN", "chennai"),
        new("TN02", "Chennai North West (Anna Nagar)", "TN", "chennai"),
        new("TN09", "Chennai South (Kotturpuram)", "TN", "chennai"),
        new("TN10", "Chennai North (Tondiarpet)", "TN", "chennai"),
        new("TN22", "Chennai South East (Meenambakkam)", "TN", "chennai"),
        new("TN37", "Coimbatore", "TN", "coimbatore"),

        new("TS07", "Ranga Reddy (Ibrahimpatnam)", "TS", "hyderabad"),
        new("TS08", "Medchal-Malkajgiri", "TS", "hyderabad"),
        new("TS09", "Hyderabad Central Zone (Khairatabad)", "TS", "hyderabad"),
        new("TS10", "Secunderabad", "TS", "hyderabad"),

        new("GJ01", "Ahmedabad", "GJ", "ahmedabad"),
        new("GJ05", "Surat", "GJ", "surat"),
        new("GJ06", "Vadodara", "GJ", "vadodara"),
        new("GJ03", "Rajkot", "GJ", "rajkot"),

        new("WB01", "Kolkata (Beltala)", "WB", "kolkata"),
        new("WB02", "Kolkata (Kasba)", "WB", "kolkata"),

        new("HR26", "Gurugram", "HR", "gurugram"),
        new("UP16", "Gautam Buddha Nagar (Noida)", "UP", "noida"),
        new("UP14", "Ghaziabad", "UP", "ghaziabad"),
        new("UP32", "Lucknow", "UP", "lucknow"),
        new("RJ14", "Jaipur", "RJ", "jaipur"),
        new("MP09", "Indore", "MP", "indore"),
        new("KL07", "Ernakulam", "KL", "kochi"),
        new("CH01", "Chandigarh", "CH", "chandigarh"),
        new("PB10", "Ludhiana", "PB", "ludhiana"),
        new("GA07", "Panaji", "GA", "panaji"),
        new("AP31", "Visakhapatnam", "AP", "visakhapatnam"),
    ];

    private async Task<int> SeedRtoOfficesAsync(CancellationToken cancellationToken)
    {
        var states = await _context.States
            .ToDictionaryAsync(state => state.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var cities = await _context.Cities
            .ToDictionaryAsync(city => city.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var known = (await _context.RtoOffices.Select(office => office.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in RtoOffices.Where(row => !known.Contains(row.Code)))
        {
            Guid? cityId = row.CityCode is not null && cities.TryGetValue(row.CityCode, out var city)
                ? city.Id
                : null;

            _context.RtoOffices.Add(SeedGuard.Required(
                RtoOffice.Create(states[row.StateCode].Id, row.Code, row.Name, cityId),
                $"RTO {row.Code}"));

            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Pincodes ─────────────────────────────────────────────────────────────────────────────────
    // A representative slice rather than all 19,000: the pincode's job in the demo is to resolve a
    // typed six digits to a city, and that is provable with the metros the demo listings sit in.

    private static readonly GeoPincodeRow[] Pincodes =
    [
        new("411001", "MH", "pune", "Pune City"),
        new("411014", "MH", "pune", "Pune (Viman Nagar)"),
        new("411057", "MH", "pune", "Pune (Hinjawadi)"),
        new("411045", "MH", "pune", "Pune (Baner)"),
        new("400001", "MH", "mumbai", "Mumbai (Fort)"),
        new("400051", "MH", "mumbai", "Mumbai (Bandra East)"),
        new("400076", "MH", "mumbai", "Mumbai (Powai)"),
        new("400601", "MH", "thane", "Thane West"),
        new("400703", "MH", "navi-mumbai", "Navi Mumbai (Vashi)"),
        new("422001", "MH", "nashik", "Nashik"),
        new("440001", "MH", "nagpur", "Nagpur"),
        new("560001", "KA", "bengaluru", "Bengaluru GPO"),
        new("560066", "KA", "bengaluru", "Bengaluru (Whitefield)"),
        new("560103", "KA", "bengaluru", "Bengaluru (Bellandur)"),
        new("560078", "KA", "bengaluru", "Bengaluru (JP Nagar)"),
        new("570001", "KA", "mysuru", "Mysuru"),
        new("110001", "DL", "delhi", "New Delhi (Connaught Place)"),
        new("110019", "DL", "delhi", "New Delhi (Kalkaji)"),
        new("110075", "DL", "delhi", "New Delhi (Dwarka)"),
        new("110092", "DL", "delhi", "Delhi (Preet Vihar)"),
        new("122001", "HR", "gurugram", "Gurugram"),
        new("201301", "UP", "noida", "Noida"),
        new("500001", "TS", "hyderabad", "Hyderabad GPO"),
        new("500081", "TS", "hyderabad", "Hyderabad (Gachibowli)"),
        new("600001", "TN", "chennai", "Chennai GPO"),
        new("600096", "TN", "chennai", "Chennai (Perungudi)"),
        new("641001", "TN", "coimbatore", "Coimbatore"),
        new("380001", "GJ", "ahmedabad", "Ahmedabad"),
        new("395001", "GJ", "surat", "Surat"),
        new("390001", "GJ", "vadodara", "Vadodara"),
        new("700001", "WB", "kolkata", "Kolkata GPO"),
        new("302001", "RJ", "jaipur", "Jaipur"),
        new("226001", "UP", "lucknow", "Lucknow"),
        new("452001", "MP", "indore", "Indore"),
        new("682001", "KL", "kochi", "Kochi"),
        new("160001", "CH", "chandigarh", "Chandigarh"),
        new("403001", "GA", "panaji", "Panaji"),
        new("530001", "AP", "visakhapatnam", "Visakhapatnam"),
    ];

    private async Task<int> SeedPincodesAsync(CancellationToken cancellationToken)
    {
        var states = await _context.States
            .ToDictionaryAsync(state => state.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var cities = await _context.Cities
            .ToDictionaryAsync(city => city.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var known = (await _context.Pincodes.Select(pincode => pincode.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var created = 0;

        foreach (var row in Pincodes.Where(row => !known.Contains(row.Code)))
        {
            var pincode = SeedGuard.Required(
                Pincode.Create(row.Code, states[row.StateCode].Id, cities[row.CityCode].Id, row.District),
                $"pincode {row.Code}");

            // The city centroid stands in for the delivery-area centroid the published directory
            // does not carry. Good enough to resolve a city, never used to decide coverage — that is
            // what a ServiceArea is for.
            SeedGuard.Required(
                pincode.SetCentroid(cities[row.CityCode].Latitude, cities[row.CityCode].Longitude),
                $"pincode {row.Code} centroid");

            _context.Pincodes.Add(pincode);
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    // ── Service areas ────────────────────────────────────────────────────────────────────────────
    // Coverage is a radius around an operating hub, not a city boundary, because a city is not a
    // circle and our inspectors are not evenly spread across one.

    private static readonly GeoServiceAreaRow[] ServiceAreas =
    [
        new("pune-inspection", "Pune inspection coverage", "pune", "inspection", 18.5204, 73.8567, 25_000),
        new("pune-delivery", "Pune delivery coverage", "pune", "delivery", 18.5204, 73.8567, 40_000),
        new("pune-auction-yard", "Pune auction yard catchment", "pune", "auction", 18.6298, 73.7997, 60_000),
        new("mumbai-inspection", "Mumbai inspection coverage", "mumbai", "inspection", 19.0760, 72.8777, 30_000),
        new("mumbai-delivery", "Mumbai delivery coverage", "mumbai", "delivery", 19.0760, 72.8777, 45_000),
        new("bengaluru-inspection", "Bengaluru inspection coverage", "bengaluru", "inspection", 12.9716, 77.5946, 30_000),
        new("bengaluru-delivery", "Bengaluru delivery coverage", "bengaluru", "delivery", 12.9716, 77.5946, 45_000),
        new("delhi-inspection", "Delhi NCR inspection coverage", "delhi", "inspection", 28.6139, 77.2090, 45_000),
        new("delhi-delivery", "Delhi NCR delivery coverage", "delhi", "delivery", 28.6139, 77.2090, 60_000),
        new("hyderabad-inspection", "Hyderabad inspection coverage", "hyderabad", "inspection", 17.3850, 78.4867, 30_000),
        new("chennai-inspection", "Chennai inspection coverage", "chennai", "inspection", 13.0827, 80.2707, 30_000),
        new("ahmedabad-inspection", "Ahmedabad inspection coverage", "ahmedabad", "inspection", 23.0225, 72.5714, 25_000),
        new("gurugram-inspection", "Gurugram inspection coverage", "gurugram", "inspection", 28.4595, 77.0266, 25_000),
    ];

    private async Task<int> SeedServiceAreasAsync(CancellationToken cancellationToken)
    {
        var cities = await _context.Cities
            .ToDictionaryAsync(city => city.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var known = (await _context.ServiceAreas.Select(area => area.Code).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var row in ServiceAreas.Where(row => !known.Contains(row.Code)))
        {
            _context.ServiceAreas.Add(SeedGuard.Required(
                ServiceArea.CreateRadius(
                    cities[row.CityCode].Id,
                    row.Code,
                    row.Name,
                    row.ModuleCode,
                    row.Latitude,
                    row.Longitude,
                    row.RadiusMetres),
                $"service area {row.Code}"));

            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }
}

internal sealed record GeoStateRow(string Code, string Name, string GstStateCode);

internal sealed record GeoCityRow(
    string Code,
    string Name,
    string StateCode,
    double Latitude,
    double Longitude,
    IReadOnlyList<string> Modules,
    int SortOrder);

internal sealed record GeoRtoRow(string Code, string Name, string StateCode, string? CityCode);

internal sealed record GeoPincodeRow(string Code, string StateCode, string CityCode, string District);

internal sealed record GeoServiceAreaRow(
    string Code,
    string Name,
    string CityCode,
    string ModuleCode,
    double Latitude,
    double Longitude,
    int RadiusMetres);
