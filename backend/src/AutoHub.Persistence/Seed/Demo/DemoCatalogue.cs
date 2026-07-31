namespace AutoHub.Persistence.Seed.Demo;

/// <summary>
/// The marketplace the client is shown: real dealers, real plates, real prices.
/// </summary>
/// <remarks>
/// Every registration number here carries the RTO code of the city the vehicle sits in — a Pune car
/// is <c>MH12</c> or <c>MH14</c>, a Navi Mumbai one is <c>MH43</c>, a Bengaluru one is <c>KA01</c>
/// through <c>KA51</c>. That is not decoration. The geography registry resolves those codes to real
/// offices, the valuation model reads provenance from them, and "Test Vehicle 1" would make every
/// one of those paths untestable while looking, on a screenshot, exactly like a product nobody has
/// used.
/// <para>
/// Prices are in <b>paise</b> and reflect what these vehicles actually trade for. Kilometres,
/// ownership counts and model years are internally consistent — a 2016 Swift with 18,000 km and one
/// owner is a listing a dealer would question, so there isn't one.
/// </para>
/// <para>
/// This catalogue is public and deliberately free of ids. The listings, auction and deals modules
/// are being built alongside this seeder; when their tables exist their own seeders read these rows
/// and mint their own keys, rather than inventing a second, contradictory demo dataset. Until then
/// <see cref="DemoDataSeeder"/> materialises what the platform layer can hold on its own: the
/// vehicles' attribute values, and every entity's lifecycle in the workflow registry.
/// </para>
/// </remarks>
public static class DemoCatalogue
{
    // ── Dealers ──────────────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<DemoDealer> Dealers { get; } =
    [
        new("shreeji-motors", "Shreeji Motors Pvt Ltd", "Shreeji Motors", "pune",
            DealerTier.Gold, KycState.Verified, "27AABCS1429B1ZQ", "Nilesh Shah", "+919822014567"),
        new("deccan-auto", "Deccan Auto Wheels LLP", "Deccan Auto", "pune",
            DealerTier.Silver, KycState.Verified, "27AAFCD8821L1ZR", "Sujata Kulkarni", "+919011223344"),
        new("kalyan-car-bazaar", "Kalyan Car Bazaar", "Kalyan Car Bazaar", "mumbai",
            DealerTier.Gold, KycState.Verified, "27AAGCK4410M1ZP", "Imran Shaikh", "+919820098765"),
        new("andheri-premium", "Andheri Premium Cars Pvt Ltd", "Andheri Premium", "mumbai",
            DealerTier.Platinum, KycState.Verified, "27AACCA9902N1ZK", "Rohan Mehta", "+919833445566"),
        new("whitefield-wheels", "Whitefield Wheels Pvt Ltd", "Whitefield Wheels", "bengaluru",
            DealerTier.Gold, KycState.Verified, "29AABCW7731P1ZJ", "Anand Raghavan", "+919845567788"),
        new("jayanagar-motors", "Jayanagar Motors", "Jayanagar Motors", "bengaluru",
            DealerTier.Silver, KycState.UnderReview, "29AAECJ1120Q1ZH", "Deepa Nair", "+919886677889"),
        new("dwarka-auto-hub", "Dwarka Auto Hub Pvt Ltd", "Dwarka Auto Hub", "delhi",
            DealerTier.Gold, KycState.DocumentsRequested, "07AAGCD5567R1ZM", "Harpreet Singh", "+919810011223"),
        new("rajouri-car-mart", "Rajouri Car Mart", "Rajouri Car Mart", "delhi",
            DealerTier.Bronze, KycState.Rejected, "07AABFR3390S1ZN", "Vikas Chauhan", "+919899887766"),
    ];

    // ── Listings ─────────────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<DemoListing> Listings { get; } =
    [
        // ── Pune ─────────────────────────────────────────────────────────────────────────────────
        new("MH12AB1234", "pune", "swift", "vxi", 2021, 38_400, 1, 5_49_000_00, "red", "petrol",
            "manual", "shreeji-motors", ListingState.Live, "2026-11-14", 3),
        new("MH12KJ4471", "pune", "baleno", "zeta", 2020, 44_900, 1, 6_15_000_00, "blue", "petrol",
            "manual", "shreeji-motors", ListingState.Live, "2026-09-02", 6),
        new("MH14CD8890", "pune", "creta", "sx", 2021, 52_100, 1, 13_75_000_00, "white", "petrol",
            "automatic", "shreeji-motors", ListingState.Live, "2027-01-19", 9),
        new("MH12PQ2210", "pune", "nexon", "xz-plus", 2022, 26_800, 1, 10_45_000_00, "blue", "petrol",
            "manual", "deccan-auto", ListingState.Live, "2026-12-05", 2),
        new("MH14EF3345", "pune", "wagon-r", "vxi-cng", 2019, 71_300, 2, 4_35_000_00, "silver", "cng",
            "manual", "deccan-auto", ListingState.Live, "2026-08-22", 12),
        new("MH12RS7788", "pune", "xuv700", "ax7", 2022, 34_200, 1, 22_90_000_00, "grey", "diesel",
            "automatic", "shreeji-motors", ListingState.Live, "2027-03-11", 5),
        new("MH12TU1102", "pune", "city", "vx-cvt", 2020, 48_600, 1, 11_20_000_00, "white", "petrol",
            "cvt", null, ListingState.Live, "2026-10-30", 8),
        new("MH14GH5521", "pune", "thar", "lx-hard-top", 2021, 31_500, 1, 14_25_000_00, "red", "diesel",
            "automatic", "deccan-auto", ListingState.Live, "2026-12-28", 4),
        new("MH12VW9034", "pune", "dzire", "vxi", 2019, 66_700, 2, 6_10_000_00, "silver", "petrol",
            "manual", null, ListingState.Live, "2026-09-15", 15),
        new("MH12XY6650", "pune", "nexon-ev", "xz-plus-prime", 2022, 22_400, 1, 13_60_000_00, "blue", "ev",
            "automatic", "shreeji-motors", ListingState.Live, "2027-02-08", 7),
        new("MH14JK2287", "pune", "seltos", "htx", 2021, 41_900, 1, 14_10_000_00, "white", "petrol",
            "manual", "deccan-auto", ListingState.Live, "2026-11-01", 11),
        new("MH12LM4419", "pune", "innova-crysta", "vx", 2019, 92_600, 2, 18_50_000_00, "grey", "diesel",
            "manual", null, ListingState.Live, "2026-08-05", 18),
        new("MH12NO8802", "pune", "punch", "accomplished", 2022, 19_800, 1, 8_45_000_00, "orange", "petrol",
            "amt", "deccan-auto", ListingState.Live, "2027-04-02", 1),
        new("MH14ZA1195", "pune", "activa-6g", "deluxe", 2021, 14_200, 1, 68_000_00, "grey", "petrol",
            "automatic", null, ListingState.Live, "2026-10-11", 6),
        new("MH12BC3376", "pune", "classic-350", "chrome", 2022, 9_400, 1, 1_78_000_00, "black", "petrol",
            "manual", null, ListingState.Live, "2026-12-19", 3),
        new("MH14DE7743", "pune", "dost", "lx", 2019, 1_18_000, 1, 5_25_000_00, "white", "diesel",
            "manual", null, ListingState.Live, "2026-09-27", 14),
        new("MH12FG9981", "pune", "3dx-backhoe", "xtra", 2020, 4_900, 1, 26_50_000_00, "yellow", "diesel",
            "manual", null, ListingState.Live, "2026-11-23", 20),
        new("MH12HI2264", "pune", "harrier", "xz-plus", 2021, 45_300, 1, 17_80_000_00, "white", "diesel",
            "automatic", "shreeji-motors", ListingState.PendingReview, "2026-10-08", 0),

        // ── Mumbai ───────────────────────────────────────────────────────────────────────────────
        new("MH01AB5567", "mumbai", "i20", "asta", 2021, 33_700, 1, 8_35_000_00, "blue", "petrol",
            "manual", "kalyan-car-bazaar", ListingState.Live, "2026-12-01", 4),
        new("MH02CD1129", "mumbai", "fortuner", "4x2-at", 2020, 58_900, 1, 34_50_000_00, "white", "diesel",
            "automatic", "andheri-premium", ListingState.Live, "2027-02-14", 10),
        new("MH01EF8834", "mumbai", "3-series", "330i-m-sport", 2021, 27_600, 1, 42_90_000_00, "black", "petrol",
            "automatic", "andheri-premium", ListingState.Live, "2027-01-05", 6),
        new("MH03GH4402", "mumbai", "swift", "zxi-plus-amt", 2022, 21_100, 1, 7_25_000_00, "white", "petrol",
            "amt", "kalyan-car-bazaar", ListingState.Live, "2027-03-22", 2),
        new("MH02IJ6690", "mumbai", "c-class", "c220d", 2019, 49_800, 1, 38_75_000_00, "silver", "diesel",
            "automatic", "andheri-premium", ListingState.Live, "2026-11-09", 13),
        new("MH01KL3318", "mumbai", "venue", "sx-opt", 2021, 36_400, 1, 10_15_000_00, "grey", "petrol",
            "dct", "kalyan-car-bazaar", ListingState.Live, "2026-10-27", 9),
        new("MH43MN7725", "mumbai", "ertiga", "zxi-plus", 2020, 61_200, 2, 9_60_000_00, "white", "petrol",
            "manual", null, ListingState.Live, "2026-09-06", 16),
        new("MH04OP1043", "mumbai", "brezza", "zxi-plus", 2022, 24_500, 1, 11_95_000_00, "blue", "petrol",
            "automatic", "kalyan-car-bazaar", ListingState.Live, "2027-04-18", 3),
        new("MH02QR5581", "mumbai", "a4", "premium-plus", 2020, 42_300, 1, 36_20_000_00, "white", "petrol",
            "automatic", "andheri-premium", ListingState.Live, "2026-12-13", 7),
        new("MH01ST9967", "mumbai", "altroz", "xz-plus", 2021, 29_900, 1, 7_40_000_00, "grey", "petrol",
            "manual", null, ListingState.Live, "2026-11-30", 12),
        new("MH43UV2214", "mumbai", "magnite", "xv-premium", 2021, 34_800, 1, 7_85_000_00, "red", "petrol",
            "cvt", "kalyan-car-bazaar", ListingState.Live, "2026-10-02", 8),
        new("MH03WX6678", "mumbai", "jupiter", "zx", 2020, 18_600, 1, 58_000_00, "silver", "petrol",
            "automatic", null, ListingState.Live, "2026-09-19", 21),
        new("MH02YZ4436", "mumbai", "scorpio-n", "z8l", 2023, 18_200, 1, 24_75_000_00, "black", "diesel",
            "automatic", "andheri-premium", ListingState.Live, "2027-05-07", 1),
        new("MH01BC8890", "mumbai", "ace-gold", "cng-plus", 2021, 74_500, 1, 4_15_000_00, "white", "cng",
            "manual", null, ListingState.Live, "2026-08-29", 17),
        new("MH04DE2257", "mumbai", "traveller-26", "school", 2018, 1_86_000, 2, 12_40_000_00, "white", "diesel",
            "manual", null, ListingState.Live, "2026-10-16", 22),
        new("MH01FG6703", "mumbai", "amaze", "vx-cvt", 2020, 39_100, 1, 7_10_000_00, "silver", "petrol",
            "cvt", "kalyan-car-bazaar", ListingState.PendingReview, "2026-11-21", 0),
        new("MH02HI1148", "mumbai", "hector", "sharp", 2021, 46_700, 1, 15_30_000_00, "grey", "diesel",
            "manual", "andheri-premium", ListingState.Sold, "2026-09-24", 24),
        new("MH03JK5594", "mumbai", "wagon-r", "lxi", 2018, 82_400, 2, 3_45_000_00, "blue", "petrol",
            "manual", null, ListingState.Sold, "2026-07-30", 25),

        // ── Bengaluru ────────────────────────────────────────────────────────────────────────────
        new("KA01AB3341", "bengaluru", "creta", "sx-opt-turbo-dct", 2022, 28_900, 1, 16_45_000_00, "black",
            "petrol", "dct", "whitefield-wheels", ListingState.Live, "2027-03-04", 5),
        new("KA03CD7787", "bengaluru", "seltos", "gtx-plus-dct", 2021, 37_500, 1, 15_60_000_00, "white",
            "petrol", "dct", "whitefield-wheels", ListingState.Live, "2026-12-22", 8),
        new("KA05EF2233", "bengaluru", "baleno", "alpha-amt", 2020, 51_800, 1, 6_70_000_00, "blue",
            "petrol", "amt", "jayanagar-motors", ListingState.Live, "2026-10-05", 11),
        new("KA51GH6679", "bengaluru", "nexon-ev", "xz-plus-lux-max", 2023, 16_400, 1, 15_90_000_00, "white",
            "ev", "automatic", "whitefield-wheels", ListingState.Live, "2027-06-01", 2),
        new("KA01IJ1125", "bengaluru", "tiago-ev", "xz-plus-lr", 2023, 12_900, 1, 9_25_000_00, "blue",
            "ev", "automatic", "whitefield-wheels", ListingState.Live, "2027-05-14", 4),
        new("KA03KL5571", "bengaluru", "city", "zx", 2021, 34_600, 1, 12_85_000_00, "grey",
            "petrol", "cvt", "jayanagar-motors", ListingState.Live, "2026-11-17", 9),
        new("KA05MN9917", "bengaluru", "xuv300", "w8-opt", 2020, 55_300, 2, 8_90_000_00, "red",
            "diesel", "manual", null, ListingState.Live, "2026-09-11", 14),
        new("KA01OP4463", "bengaluru", "kushaq", "style-at", 2022, 25_700, 1, 15_20_000_00, "white",
            "petrol", "dct", "whitefield-wheels", ListingState.Live, "2027-02-25", 6),
        new("KA51QR8809", "bengaluru", "ather-450x", "gen3", 2022, 8_700, 1, 1_12_000_00, "grey",
            "ev", "automatic", null, ListingState.Live, "2026-12-09", 10),
        new("KA03ST2255", "bengaluru", "carens", "luxury-plus", 2022, 31_200, 1, 16_80_000_00, "white",
            "diesel", "automatic", "jayanagar-motors", ListingState.Live, "2027-01-28", 7),
        new("KA05UV6601", "bengaluru", "glanza", "v-amt", 2021, 29_400, 1, 7_55_000_00, "silver",
            "petrol", "amt", null, ListingState.Live, "2026-10-20", 13),
        new("KA01WX1147", "bengaluru", "sonet", "gtx-plus", 2021, 33_100, 1, 10_90_000_00, "blue",
            "petrol", "dct", "whitefield-wheels", ListingState.Live, "2026-11-26", 5),
        new("KA51YZ5593", "bengaluru", "ola-s1-pro", "gen2", 2023, 6_300, 1, 98_000_00, "black",
            "ev", "automatic", null, ListingState.Live, "2027-04-09", 3),
        new("KA03AB9939", "bengaluru", "e6", "premium", 2022, 41_600, 1, 21_40_000_00, "white",
            "ev", "automatic", "whitefield-wheels", ListingState.Live, "2027-03-17", 12),
        new("KA05CD3385", "bengaluru", "verna", "sx-opt-turbo", 2021, 38_800, 1, 13_15_000_00, "red",
            "petrol", "dct", "jayanagar-motors", ListingState.PendingReview, "2026-12-31", 0),
        new("KA01EF7731", "bengaluru", "taigun", "highline", 2022, 27_200, 1, 14_45_000_00, "grey",
            "petrol", "manual", "whitefield-wheels", ListingState.Sold, "2027-01-11", 23),

        // ── Delhi ────────────────────────────────────────────────────────────────────────────────
        new("DL01AB2277", "delhi", "swift", "zxi", 2021, 35_900, 1, 6_05_000_00, "white", "petrol",
            "manual", "dwarka-auto-hub", ListingState.Live, "2026-11-06", 6),
        new("DL03CD6623", "delhi", "creta", "ex", 2020, 61_400, 2, 12_30_000_00, "silver", "diesel",
            "manual", "dwarka-auto-hub", ListingState.Live, "2026-09-29", 15),
        new("DL08EF1169", "delhi", "fortuner", "legender", 2022, 32_700, 1, 44_60_000_00, "white", "diesel",
            "automatic", "dwarka-auto-hub", ListingState.Live, "2027-04-25", 4),
        new("DL10GH5515", "delhi", "i20", "asta-opt-turbo-dct", 2022, 22_800, 1, 9_70_000_00, "red", "petrol",
            "dct", "rajouri-car-mart", ListingState.Live, "2027-02-18", 8),
        new("DL12IJ9961", "delhi", "xuv700", "ax5", 2022, 29_600, 1, 20_15_000_00, "grey", "diesel",
            "manual", "dwarka-auto-hub", ListingState.Live, "2027-01-02", 5),
        new("DL01KL4407", "delhi", "dzire", "zxi-plus-amt", 2021, 40_200, 1, 7_95_000_00, "white", "petrol",
            "amt", "rajouri-car-mart", ListingState.Live, "2026-12-16", 10),
        new("DL03MN8853", "delhi", "thar", "ax-opt", 2021, 36_100, 1, 13_40_000_00, "black", "diesel",
            "manual", null, ListingState.Live, "2026-10-24", 12),
        new("DL08OP3399", "delhi", "punch", "creative", 2023, 15_700, 1, 9_15_000_00, "white", "petrol",
            "amt", "dwarka-auto-hub", ListingState.Live, "2027-05-20", 2),
        new("DL10QR7745", "delhi", "kiger", "rxt", 2021, 43_500, 1, 7_30_000_00, "orange", "petrol",
            "manual", "rajouri-car-mart", ListingState.Live, "2026-11-13", 9),
        new("DL12ST1191", "delhi", "innova-crysta", "zx-at", 2021, 54_800, 1, 25_60_000_00, "grey", "diesel",
            "automatic", "dwarka-auto-hub", ListingState.Live, "2027-03-29", 7),
        new("DL01UV5537", "delhi", "splendor-plus", "i3s", 2020, 26_400, 1, 48_000_00, "black", "petrol",
            "manual", null, ListingState.Live, "2026-08-18", 19),
        new("DL03WX9983", "delhi", "pulsar-150", "twin-disc", 2021, 19_100, 1, 76_000_00, "blue", "petrol",
            "manual", null, ListingState.Live, "2026-12-26", 11),
        new("DL08YZ4429", "delhi", "bolero-pickup", "1-7t", 2020, 96_300, 1, 5_85_000_00, "white", "diesel",
            "manual", null, ListingState.Live, "2026-09-08", 16),
        new("DL10AB8875", "delhi", "pro-2049", "cbc", 2019, 2_14_000, 1, 13_90_000_00, "blue", "diesel",
            "manual", null, ListingState.Live, "2026-10-13", 20),
        new("DL12CD2221", "delhi", "harrier", "xz", 2020, 58_700, 2, 15_10_000_00, "grey", "diesel",
            "manual", "rajouri-car-mart", ListingState.Rejected, "2026-11-04", 0),
        new("DL01EF6667", "delhi", "amaze", "e-mt", 2019, 71_900, 2, 4_95_000_00, "silver", "petrol",
            "manual", null, ListingState.Rejected, "2026-08-11", 0),

        // Drafts — never submitted, so they exercise the initial state and nothing else.
        new("MH12ZZ1010", "pune", "swift", "lxi", 2018, 78_500, 2, 4_20_000_00, "silver", "petrol",
            "manual", null, ListingState.Draft, "2026-09-01", 0),
        new("MH01ZZ2020", "mumbai", "baleno", "delta", 2019, 63_200, 2, 5_35_000_00, "grey", "petrol",
            "manual", null, ListingState.Draft, "2026-10-07", 0),
        new("KA01ZZ3030", "bengaluru", "venue", "s-plus", 2020, 47_600, 1, 8_20_000_00, "white", "petrol",
            "manual", null, ListingState.Draft, "2026-11-12", 0),
        new("DL01ZZ4040", "delhi", "wagon-r", "zxi-plus", 2021, 32_800, 1, 5_65_000_00, "blue", "petrol",
            "manual", null, ListingState.Draft, "2026-12-03", 0),
    ];

    // ── Auctions ─────────────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<DemoAuction> Auctions { get; } =
    [
        new("pune-weekly-0731", "Pune Weekly Dealer Auction", "pune",
        [
            new("MH12RS7788", 21_50_000_00, LotState.Live,
            [
                new("Dealer ••42", 21_50_000_00, 96),
                new("Dealer ••17", 21_75_000_00, 74),
                new("Dealer ••42", 22_00_000_00, 51),
                new("Dealer ••08", 22_40_000_00, 22),
            ]),
            new("MH12XY6650", 12_80_000_00, LotState.Live,
            [
                new("Dealer ••17", 12_80_000_00, 88),
                new("Dealer ••31", 13_05_000_00, 46),
            ]),
            new("MH14GH5521", 13_50_000_00, LotState.InExtension,
            [
                new("Dealer ••08", 13_50_000_00, 40),
                new("Dealer ••42", 13_80_000_00, 12),
                new("Dealer ••31", 14_05_000_00, 2),
            ]),
            new("MH12LM4419", 19_00_000_00, LotState.ReserveNotMet,
            [
                new("Dealer ••17", 17_50_000_00, 130),
                new("Dealer ••08", 18_20_000_00, 95),
            ]),
            new("MH14DE7743", 4_90_000_00, LotState.Sold,
            [
                new("Dealer ••31", 4_90_000_00, 210),
                new("Dealer ••42", 5_10_000_00, 180),
                new("Dealer ••17", 5_35_000_00, 155),
            ]),
        ]),

        new("mumbai-premium-0731", "Mumbai Premium SUV Auction", "mumbai",
        [
            new("MH02CD1129", 33_00_000_00, LotState.Live,
            [
                new("Dealer ••55", 33_00_000_00, 110),
                new("Dealer ••23", 33_80_000_00, 82),
                new("Dealer ••55", 34_25_000_00, 39),
            ]),
            new("MH02YZ4436", 23_50_000_00, LotState.Live,
            [
                new("Dealer ••23", 23_50_000_00, 71),
                new("Dealer ••90", 24_10_000_00, 34),
            ]),
            new("MH01EF8834", 41_00_000_00, LotState.Live,
            [
                new("Dealer ••90", 41_00_000_00, 64),
            ]),
            new("MH02QR5581", 35_00_000_00, LotState.Sold,
            [
                new("Dealer ••55", 35_00_000_00, 240),
                new("Dealer ••23", 35_90_000_00, 205),
                new("Dealer ••90", 36_60_000_00, 168),
            ]),
        ]),

        new("bengaluru-ev-0731", "Bengaluru EV and Hatchback Auction", "bengaluru",
        [
            new("KA51GH6679", 15_00_000_00, LotState.Live,
            [
                new("Dealer ••61", 15_00_000_00, 102),
                new("Dealer ••77", 15_40_000_00, 58),
                new("Dealer ••61", 15_75_000_00, 26),
            ]),
            new("KA01IJ1125", 8_80_000_00, LotState.Live,
            [
                new("Dealer ••77", 8_80_000_00, 93),
                new("Dealer ••61", 9_05_000_00, 44),
            ]),
            new("KA03AB9939", 20_50_000_00, LotState.Live,
            [
                new("Dealer ••44", 20_50_000_00, 77),
            ]),
            new("KA05EF2233", 6_40_000_00, LotState.Sold,
            [
                new("Dealer ••44", 6_40_000_00, 260),
                new("Dealer ••77", 6_60_000_00, 221),
            ]),
            new("KA05UV6601", 7_20_000_00, LotState.ReserveNotMet,
            [
                new("Dealer ••61", 6_80_000_00, 190),
            ]),
        ]),
    ];

    // ── Deals and bookings ───────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<DemoDeal> Deals { get; } =
    [
        new("MH02HI1148", "kalyan-car-bazaar", 15_30_000_00, DealState.Completed, 24),
        new("MH03JK5594", null, 3_45_000_00, DealState.Completed, 25),
        new("KA01EF7731", "whitefield-wheels", 14_45_000_00, DealState.RtoTransfer, 12),
        new("MH12AB1234", "shreeji-motors", 5_49_000_00, DealState.DocumentsPending, 6),
        new("KA01WX1147", "whitefield-wheels", 10_90_000_00, DealState.PaymentReceived, 3),
        new("DL08OP3399", "dwarka-auto-hub", 9_15_000_00, DealState.AwaitingPayment, 1),
    ];

    public static IReadOnlyList<DemoBooking> Bookings { get; } =
    [
        new("MH12AB1234", BookingKind.TestDrive, BookingState.Completed, 5),
        new("MH12PQ2210", BookingKind.TestDrive, BookingState.Confirmed, 1),
        new("KA01AB3341", BookingKind.TestDrive, BookingState.InProgress, 0),
        new("DL08EF1169", BookingKind.Inspection, BookingState.Confirmed, 2),
        new("MH01AB5567", BookingKind.Inspection, BookingState.Completed, 9),
        new("KA03KL5571", BookingKind.TestDrive, BookingState.Cancelled, 4),
        new("MH02CD1129", BookingKind.Delivery, BookingState.Requested, 0),
    ];
}

/// <summary>A dealership as the demo shows it, including where its KYC actually got to.</summary>
public sealed record DemoDealer(
    string Code,
    string LegalName,
    string TradeName,
    string CityCode,
    string Tier,
    string KycState,
    string Gstin,
    string OwnerName,
    string OwnerPhone);

/// <summary>One vehicle offered for sale.</summary>
/// <remarks>
/// <c>Registration</c> is the canonical plate and its RTO code always matches <c>CityCode</c>.
/// <c>AskingPriceMinor</c> is paise — never rupees and never a formatted string. <c>DealerCode</c>
/// is null for a private seller, which is roughly a third of the corpus, as in life.
/// <para>
/// <c>AgeDays</c> is how long ago the listing was created, kept under thirty days so the listing
/// workflow's expiry timer is still in the future for every live listing — which is what a real
/// marketplace looks like and what makes the timer demonstrable rather than already fired.
/// </para>
/// </remarks>
public sealed record DemoListing(
    string Registration,
    string CityCode,
    string ModelCode,
    string VariantCode,
    int Year,
    int KmsDriven,
    int Owners,
    long AskingPriceMinor,
    string ColourCode,
    string FuelCode,
    string TransmissionCode,
    string? DealerCode,
    string State,
    string InsuranceValidTill,
    int AgeDays);

public sealed record DemoAuction(
    string Code,
    string Name,
    string CityCode,
    IReadOnlyList<DemoLot> Lots);

/// <summary>One vehicle under the hammer.</summary>
/// <remarks><c>ReserveMinor</c> is the seller's floor in paise, and is never shown to bidders.</remarks>
public sealed record DemoLot(
    string Registration,
    long ReserveMinor,
    string State,
    IReadOnlyList<DemoBid> Bids);

/// <summary>One bid on a lot.</summary>
/// <remarks>
/// <c>BidderMask</c> is what other bidders see. A dealer auction is not anonymous to the operator and
/// is never identifiable to a rival, because a bidder who can see who they are up against bids
/// differently. <c>MinutesAgo</c> is how long before the demo's "now" the bid landed.
/// </remarks>
public sealed record DemoBid(string BidderMask, long AmountMinor, int MinutesAgo);

public sealed record DemoDeal(
    string Registration,
    string? DealerCode,
    long AmountMinor,
    string State,
    int AgeDays);

public sealed record DemoBooking(
    string Registration,
    string Kind,
    string State,
    int DaysAgo);

/// <summary>Dealer tiers, as the pricing registry's conditions spell them.</summary>
/// <remarks>
/// These are the literal values <c>party.tier</c> is compared against in the seeded pricing rules,
/// named here so the demo dealers and the commission rules cannot disagree about how "gold" is spelt.
/// </remarks>
public static class DealerTier
{
    public const string Bronze = "bronze";
    public const string Silver = "silver";
    public const string Gold = "gold";
    public const string Platinum = "platinum";
}

/// <summary>The <c>kyc_case</c> workflow states the demo dealers are parked in.</summary>
public static class KycState
{
    public const string Verified = "verified";
    public const string UnderReview = "under_review";
    public const string DocumentsRequested = "documents_requested";
    public const string Rejected = "rejected";
}

/// <summary>The <c>listing</c> workflow states the demo listings are parked in.</summary>
public static class ListingState
{
    public const string Draft = "draft";
    public const string PendingReview = "pending_review";
    public const string Live = "live";
    public const string Sold = "sold";
    public const string Rejected = "rejected";
}

/// <summary>The <c>auction_lot</c> workflow states the demo lots are parked in.</summary>
public static class LotState
{
    public const string Live = "live";
    public const string InExtension = "in_extension";
    public const string ReserveNotMet = "reserve_not_met";
    public const string Sold = "sold";
}

/// <summary>The <c>deal</c> workflow states the demo deals are parked in.</summary>
public static class DealState
{
    public const string AwaitingPayment = "awaiting_payment";
    public const string PaymentReceived = "payment_received";
    public const string DocumentsPending = "documents_pending";
    public const string RtoTransfer = "rto_transfer";
    public const string Completed = "completed";
}

/// <summary>The <c>booking</c> workflow states the demo bookings are parked in.</summary>
public static class BookingState
{
    public const string Requested = "requested";
    public const string Confirmed = "confirmed";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
}

public static class BookingKind
{
    public const string TestDrive = "test_drive";
    public const string Inspection = "inspection";
    public const string Delivery = "delivery";
}
