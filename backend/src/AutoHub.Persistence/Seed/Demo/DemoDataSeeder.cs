using System.Globalization;
using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Catalog.Domain.Vehicles;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Persistence.Search;
using AutoHub.Persistence.Stores;
using AutoHub.Platform.Taxonomy;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoHub.Persistence.Seed.Demo;

/// <summary>
/// Turns <see cref="DemoCatalogue"/> into a database a client can be shown.
/// </summary>
/// <remarks>
/// <b>Where the demo data lives, and why it lives there.</b> Catalog owns tables now, so every demo
/// vehicle is a real <see cref="Vehicle"/> aggregate carrying a real
/// <c>VehicleIdentity</c> period for its plate. The auction, deals and bookings modules still have
/// no tables, so rather than invent a parallel set that would contradict theirs, everything they
/// own is staged in the two platform structures that are deliberately entity-agnostic and will
/// still be correct afterwards:
/// <list type="bullet">
/// <item><see cref="EntityAttributeValue"/> rows under <c>catalog.vehicle</c> and
/// <c>listings.listing</c> — the vehicles' real specifications and asking prices, indexed exactly as
/// a search facet will read them.</item>
/// <item><see cref="WorkflowInstance"/> rows and their history — every listing, lot, deal, booking
/// and KYC case in a real state, reached by real transitions through the real engine.</item>
/// </list>
/// <para>
/// <b>The vehicles are aggregates rather than bare ids, and that is not cosmetic.</b> An earlier
/// version minted a loose <c>Guid</c> per demo car and hung attribute rows off it, which reads as
/// equivalent and is not: a plate that resolves to no vehicle makes the valuation hero — the one
/// anonymous path in the product — answer <c>valuation.insufficient_inputs</c> for every car in the
/// demo. Anything that starts from an identifier rather than from an id was silently unreachable.
/// </para>
/// <para>
/// <b>The transitions are fired, not fabricated.</b> Nothing here writes a state code into a row.
/// Every listing that is live got there by <c>submit</c> passing its photo-count guard and
/// <c>approve</c> being fired; every lot that sold got there through <c>hammer_down</c>, whose guard
/// compares the highest bid against the reserve. So the demo is also a test: if a guard is wrong, the
/// seeder fails on startup rather than the client finding out during the walkthrough.
/// </para>
/// <para>
/// <b>Attribute values are validated against the registry before they are written.</b> Running them
/// through <see cref="AttributeValueValidator"/> is what stops the demo containing a 900,000 km
/// two-year-old car, and it exercises the same code path a dealer's bulk upload will.
/// </para>
/// <para>
/// Idempotency is at the step, not the row: if any <c>listing</c> workflow instance already exists,
/// the demo has been staged and this seeder does nothing. Per-row idempotency would need a stable
/// natural key for entities whose owning modules do not exist yet, and inventing one would leave a
/// second identity scheme behind for someone to reconcile later.
/// </para>
/// </remarks>
internal sealed class DemoDataSeeder
{
    private const string VehicleEntityType = "catalog.vehicle";
    private const string ListingEntityType = "listings.listing";

    private const string ListingWorkflow = "listing";
    private const string AuctionLotWorkflow = "auction_lot";
    private const string DealWorkflow = "deal";
    private const string BookingWorkflow = "booking";
    private const string KycWorkflow = "kyc_case";

    /// <summary>
    /// Provenance recorded on every demo vehicle and plate.
    /// </summary>
    /// <remarks>
    /// The honest code, not the flattering one. Every figure in <see cref="DemoCatalogue"/> was
    /// authored by a person, so <c>seller</c> is what it is; <c>vahan</c> would tell the valuation
    /// router these are RTO-confirmed records and buy the demo a confidence score it has not earned.
    /// </remarks>
    private const string DemoProvenanceCode = "seller";

    /// <summary>
    /// Photos on every demo listing, which is what gets them past the submit guard.
    /// </summary>
    /// <remarks>
    /// Above the workflow's minimum of six on purpose. A demo whose listings sit exactly on a
    /// threshold tells you nothing about whether the threshold is enforced.
    /// </remarks>
    private const int DemoPhotoCount = 9;

    /// <summary>The form every demo listing claims to have been created through.</summary>
    private const string DemoSellFormKey = "sell_vehicle";

    private readonly AutoHubDbContext _context;
    private readonly SeedClock _clock;
    private readonly WorkflowEngine _engine;
    private readonly ILogger _logger;

    public DemoDataSeeder(AutoHubDbContext context, DateTimeOffset now, ILogger logger)
    {
        _context = context;
        _clock = new SeedClock(now);
        _logger = logger;

        // The engine is built by hand rather than resolved, for two reasons. It needs the seed clock
        // and nothing else in the process may see that clock; and no effect handlers are registered
        // in this build, so an empty dispatcher is the honest wiring rather than a stub of one.
        _engine = new WorkflowEngine(
            new EfWorkflowStore(context),
            new WorkflowEffectDispatcher([], NullLogger<WorkflowEffectDispatcher>.Instance),
            _clock,
            NullLogger<WorkflowEngine>.Instance);
    }

    public async Task<SeedStepResult> SeedAsync(CancellationToken cancellationToken)
    {
        var alreadyStaged = await _context.WorkflowInstances
            .AnyAsync(instance => instance.WorkflowKey == ListingWorkflow, cancellationToken);

        if (alreadyStaged)
        {
            _logger.LogInformation("Demo data already present; leaving it alone.");
            return new SeedStepResult("demo", 0, DemoCatalogue.Listings.Count);
        }

        var attributes = await LoadAttributesAsync(cancellationToken);
        var now = _clock.UtcNow;

        var created = await SeedDealerKycAsync(now, cancellationToken);
        created += await SeedListingsAsync(attributes, now, cancellationToken);
        created += await SeedAuctionLotsAsync(now, cancellationToken);
        created += await SeedDealsAsync(now, cancellationToken);
        created += await SeedBookingsAsync(now, cancellationToken);

        return new SeedStepResult("demo", created, 0);
    }

    // ── Dealers and their KYC cases ──────────────────────────────────────────────────────────────

    private async Task<int> SeedDealerKycAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var created = 0;

        foreach (var dealer in DemoCatalogue.Dealers)
        {
            var caseId = UuidV7.New();

            var context = new JsonObject
            {
                ["dealer"] = new JsonObject
                {
                    ["code"] = dealer.Code,
                    ["legalName"] = dealer.LegalName,
                    ["tradeName"] = dealer.TradeName,
                    ["city"] = dealer.CityCode,
                    ["tier"] = dealer.Tier,
                    ["gstin"] = dealer.Gstin,
                },
                ["kyc"] = new JsonObject
                {
                    // The guard on `approve` reads this. A rejected case is rejected precisely
                    // because it is false, so the guard is doing work rather than decorating a path.
                    ["panVerified"] = !string.Equals(dealer.KycState, KycState.Rejected, StringComparison.Ordinal),
                    ["gstVerified"] = true,
                },
            };

            // Cases are staged over the last few days so the review queue has a believable age
            // spread, and so `documents_requested` is still inside its seven-day lapse timer.
            _clock.MoveTo(now.AddDays(-3).AddHours(created * 4));

            await DriveAsync(KycWorkflow, caseId, context, KycPath(dealer.KycState), cancellationToken);
            created++;
        }

        _logger.LogInformation("Staged {Count} dealer KYC cases", created);
        return created;
    }

    private static IReadOnlyList<DemoStep> KycPath(string state) => state switch
    {
        KycState.Verified =>
        [
            new("pick_up", null, TimeSpan.FromHours(6)),
            new("approve", null, TimeSpan.FromHours(3)),
        ],
        KycState.UnderReview => [new("pick_up", null, TimeSpan.FromHours(9))],
        KycState.DocumentsRequested =>
        [
            new("request_documents", "GST certificate is illegible. Please re-upload.", TimeSpan.FromHours(5)),
        ],
        KycState.Rejected =>
        [
            new("pick_up", null, TimeSpan.FromHours(4)),
            new("reject", "PAN does not match the name on the GST certificate.", TimeSpan.FromHours(2)),
        ],
        _ => [],
    };

    // ── Listings and their vehicles ──────────────────────────────────────────────────────────────

    private async Task<int> SeedListingsAsync(
        IReadOnlyDictionary<string, AttributeDefinition> attributes,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var variants = await _context.VehicleVariants
            .ToDictionaryAsync(variant => variant.Code + "@" + variant.ModelId, StringComparer.Ordinal, cancellationToken);

        var models = await _context.VehicleModels
            .ToDictionaryAsync(model => model.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var created = 0;

        foreach (var listing in DemoCatalogue.Listings)
        {
            var model = models[listing.ModelCode];
            variants.TryGetValue(listing.VariantCode + "@" + model.Id, out var variant);

            var vehicle = RegisterVehicle(listing, model, variant, now);
            if (vehicle.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Demo vehicle '{listing.Registration}' could not be registered — {vehicle.Error}");
            }

            WriteVehicleAttributes(attributes, vehicle.Value, listing, variant, now);

            var row = CreateListingRow(listing, model, variant, vehicle.Value.Id, now);
            var listingId = row.Id;

            Write(attributes, ListingEntityType, listingId, "asking_price",
                TypedAttributeValue.OfMoney(Money.FromMinorUnits(listing.AskingPriceMinor, Currency.Inr)));

            _clock.MoveTo(now.AddDays(-listing.AgeDays).AddHours(-4));

            await DriveAsync(
                ListingWorkflow,
                listingId,
                ListingContext(listing, model.Name, variant?.Name),
                ListingPath(listing.State),
                cancellationToken);

            created++;
        }

        // The search projection is normally written by the `search.index` workflow effect on
        // `listing.approved`. The seeder runs the engine with an empty effect dispatcher on purpose
        // — no handlers are registered in this build — so nothing indexes these, and an unindexed
        // listing is invisible to the one screen a client opens first. Indexing them here keeps the
        // demo honest without pretending an effect ran that did not.
        await SaveAndIndexAsync(cancellationToken);

        _logger.LogInformation("Staged {Count} listings with their vehicles", created);
        return created;
    }

    /// <summary>
    /// Persists the staged listings and writes their search documents.
    /// </summary>
    /// <remarks>
    /// The index is built by hand rather than resolved for the same reason the workflow engine is:
    /// it needs the seed clock, and nothing else in the process may see that clock.
    /// <para>
    /// Only listings that reached a public state are indexed. A rejected listing with a search
    /// document is a rejected listing a buyer can find, which is the bug this seeder exists to
    /// avoid demonstrating.
    /// </para>
    /// </remarks>
    private async Task SaveAndIndexAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);

        var index = new PostgresListingSearchIndex(_context, _clock);

        var publicStates = new[] { ListingState.Live, ListingState.Sold };

        var indexable = await _context.WorkflowInstances
            .Where(instance =>
                instance.WorkflowKey == ListingWorkflow
                && publicStates.Contains(instance.CurrentStateCode))
            .Select(instance => instance.EntityId)
            .ToListAsync(cancellationToken);

        foreach (var listingId in indexable)
        {
            var indexed = await index.IndexAsync(listingId, cancellationToken);
            if (indexed.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Demo listing {listingId} could not be indexed — {indexed.Error}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Indexed {Count} publicly visible listings", indexable.Count);
    }

    /// <summary>
    /// Creates the real <see cref="Listing"/> row behind a demo entry.
    /// </summary>
    /// <remarks>
    /// This used to mint a loose <c>Guid</c> and hang attribute rows off it, because when the demo
    /// seeder was written <c>listings.listings</c> did not exist yet. It does now, and the gap was
    /// not cosmetic: search reads the table, so a demo with sixty vehicles and no listing rows
    /// returned an empty marketplace on the one screen a client looks at first.
    /// <para>
    /// The workflow instance and the EAV rows are keyed on the listing's own id rather than on one
    /// minted alongside it, so there is exactly one identity per listing rather than two that have
    /// to be kept in step.
    /// </para>
    /// </remarks>
    private Listing CreateListingRow(
        DemoListing listing,
        VehicleModel model,
        VehicleVariant? variant,
        Guid vehicleId,
        DateTimeOffset now)
    {
        var listedAt = now.AddDays(-listing.AgeDays);

        var draft = Listing.Draft(
            vehicleId: vehicleId,
            categoryId: model.CategoryId,
            intent: ListingIntent.Sale,
            sellerPartyId: SellerPartyId(listing.DealerCode),
            cityCode: listing.CityCode,
            price: Money.FromMinorUnits(listing.AskingPriceMinor, Currency.Inr),
            formKey: DemoSellFormKey,
            formVersion: 1,
            now: listedAt);

        if (draft.IsFailure)
        {
            throw new InvalidOperationException(
                $"Demo listing '{listing.Registration}' could not be drafted — {draft.Error}");
        }

        var row = draft.Value;

        var ownership = listing.Owners == 1
            ? "First owner"
            : string.Create(CultureInfo.InvariantCulture, $"{listing.Owners} owners");

        row.Describe(
            ComposeTitle(listing.Year, model.Name, variant?.Name),
            string.Create(
                CultureInfo.GetCultureInfo("en-IN"),
                $"{ownership}, {listing.KmsDriven:N0} km. Insurance valid until {listing.InsuranceValidTill}."));

        // The denormalised bag search reads, mirroring the EAV rows written alongside it.
        row.ProjectAttributes(new JsonObject
        {
            ["manufacture_year"] = listing.Year,
            ["kms_driven"] = listing.KmsDriven,
            ["owners"] = listing.Owners,
            ["fuel_type"] = listing.FuelCode,
            ["transmission"] = listing.TransmissionCode,
            ["colour"] = listing.ColourCode,
            ["insurance_valid_till"] = listing.InsuranceValidTill,
        });

        if (listing.State is ListingState.Live or ListingState.Sold)
        {
            row.MarkPublished(listedAt.AddHours(4));
        }

        _context.Listings.Add(row);
        return row;
    }

    /// <summary>
    /// A stable party id per dealer code.
    /// </summary>
    /// <remarks>
    /// Derived rather than looked up because dealers have no party rows yet — they exist in the demo
    /// only as KYC workflow cases. <c>listings.seller_party_id</c> carries no foreign key, which is
    /// the modular-monolith rule working as intended: a cross-module reference is an id, not a join,
    /// so Listings can be extracted without dragging Identity along.
    /// <para>
    /// Deriving it from the code keeps the same dealer pointing at the same party across reseeds, so
    /// "all stock from Sharma Motors" is a stable query rather than a different answer each run.
    /// </para>
    /// </remarks>
    /// <summary>
    /// "2019 Eicher Pro 2049 CBC", not "2019 Pro 2049 Pro 2049 CBC".
    /// </summary>
    /// <remarks>
    /// Variant names in the taxonomy usually repeat the model — "Swift VXi", "Pro 2049 CBC" — so
    /// concatenating model and variant blindly stutters. Dropping the model when the variant already
    /// carries it is what a person writing the headline by hand would do, and this string is the
    /// first thing a buyer reads.
    /// </remarks>
    private static string ComposeTitle(int year, string modelName, string? variantName)
    {
        if (string.IsNullOrWhiteSpace(variantName))
        {
            return $"{year} {modelName}";
        }

        return variantName.StartsWith(modelName, StringComparison.OrdinalIgnoreCase)
            ? $"{year} {variantName}"
            : $"{year} {modelName} {variantName}";
    }

    private static Guid SellerPartyId(string? dealerCode)
    {
        var seed = dealerCode ?? "individual_seller";
        Span<byte> bytes = stackalloc byte[16];
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
        hash.AsSpan(0, 16).CopyTo(bytes);
        return new Guid(bytes);
    }

    /// <summary>
    /// Creates the canonical vehicle behind one demo listing, plate and all.
    /// </summary>
    /// <remarks>
    /// Built through the aggregate's own factory and <c>RecordIdentifier</c> rather than by
    /// constructing rows, so the demo is subject to the same rules a sell flow is — the plate is
    /// canonicalised by <c>VehicleIdentifier</c>, the manufacture year is checked against the
    /// registration date, and a malformed demo plate fails at boot instead of becoming a vehicle no
    /// lookup can find.
    /// <para>
    /// <c>seller</c> is the provenance because that is the truth: these figures were authored, not
    /// returned by VAHAN. Marking them <c>vahan</c> would overstate their confidence to every model
    /// that weighs provenance, starting with the valuation router.
    /// </para>
    /// </remarks>
    private Result<Vehicle> RegisterVehicle(
        DemoListing listing,
        VehicleModel model,
        VehicleVariant? variant,
        DateTimeOffset now)
    {
        var identifier = VehicleIdentifier.TryParse(VehicleIdentifierType.Registration, listing.Registration);
        if (identifier.IsFailure)
        {
            return Result.Failure<Vehicle>(identifier.Error);
        }

        var vehicle = Vehicle.Register(model.CategoryId, now);
        vehicle.Classify(model.MakeId, model.Id, variant?.Id);

        var provenance = vehicle.SetProvenance(listing.Year, null, null, DemoProvenanceCode);
        if (provenance.IsFailure)
        {
            return Result.Failure<Vehicle>(provenance.Error);
        }

        // Dated to when the car was first on the road rather than to now, so the identity period is
        // as old as the vehicle. A demo whose plates all start today makes every "held since" figure
        // on screen read as a platform that launched this morning.
        var recorded = vehicle.RecordIdentifier(
            identifier.Value,
            now,
            DemoProvenanceCode,
            validFrom: now.AddDays(-listing.AgeDays).AddYears(listing.Year - now.Year));

        if (recorded.IsFailure)
        {
            return Result.Failure<Vehicle>(recorded.Error);
        }

        _context.Vehicles.Add(vehicle);
        return vehicle;
    }

    private static JsonObject ListingContext(DemoListing listing, string modelName, string? variantName)
        => new()
        {
            ["listing"] = new JsonObject
            {
                ["title"] = $"{listing.Year} {modelName} {variantName ?? string.Empty}".TrimEnd(),
                ["registration"] = listing.Registration,
                ["city"] = listing.CityCode,
                ["priceMinor"] = listing.AskingPriceMinor,
                ["sellerType"] = listing.DealerCode is null ? "individual" : "dealer",
                ["dealerCode"] = listing.DealerCode,
            },
            ["vehicle"] = new JsonObject
            {
                ["model"] = listing.ModelCode,
                ["variant"] = listing.VariantCode,
                ["year"] = listing.Year,
                ["fuel"] = listing.FuelCode,
                ["kmsDriven"] = listing.KmsDriven,
            },
            // The guard on `submit` reads this. Six is the minimum; the demo carries nine.
            ["media"] = new JsonObject { ["photoCount"] = DemoPhotoCount },
        };

    private static IReadOnlyList<DemoStep> ListingPath(string state) => state switch
    {
        ListingState.Draft => [],
        ListingState.PendingReview => [new("submit", null, TimeSpan.FromHours(2))],
        ListingState.Live =>
        [
            new("submit", null, TimeSpan.FromHours(2)),
            new("approve", null, TimeSpan.FromHours(2)),
        ],
        ListingState.Sold =>
        [
            new("submit", null, TimeSpan.FromHours(2)),
            new("approve", null, TimeSpan.FromHours(2)),
            new("sell", null, TimeSpan.FromDays(1)),
        ],
        ListingState.Rejected =>
        [
            new("submit", null, TimeSpan.FromHours(2)),
            new("reject", "Photos do not match the registration number on the RC.", TimeSpan.FromHours(3)),
        ],
        _ => [],
    };

    // ── Auctions ─────────────────────────────────────────────────────────────────────────────────

    private async Task<int> SeedAuctionLotsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var created = 0;

        foreach (var auction in DemoCatalogue.Auctions)
        {
            foreach (var lot in auction.Lots)
            {
                var lotId = UuidV7.New();
                var highest = lot.Bids.Count == 0 ? 0L : lot.Bids.Max(bid => bid.AmountMinor);

                var context = new JsonObject
                {
                    ["auction"] = new JsonObject
                    {
                        ["code"] = auction.Code,
                        ["name"] = auction.Name,
                        ["city"] = auction.CityCode,
                    },
                    ["lot"] = new JsonObject
                    {
                        ["registration"] = lot.Registration,
                        ["reserveMinor"] = lot.ReserveMinor,
                        ["highestBidMinor"] = highest,
                        ["bidCount"] = lot.Bids.Count,
                        // The `extend` guard reads this: a bid inside the last thirty seconds is what
                        // moves a lot into anti-snipe extension, and nothing else does.
                        ["secondsRemaining"] = string.Equals(lot.State, LotState.InExtension, StringComparison.Ordinal)
                            ? 18
                            : 240,
                    },
                    // The bid ladder travels on the transition context so it survives verbatim on the
                    // history row. When the auction module's tables land it reads the same ladder out
                    // of DemoCatalogue and writes real bid rows; until then this is where it lives.
                    ["bids"] = BidLadder(lot),
                };

                // Lots are staged close to "now" so a live one still has time on the clock and the
                // extended one's two-minute timer is genuinely about to fire.
                _clock.MoveTo(now.AddMinutes(-(lot.Bids.Count == 0 ? 30 : lot.Bids.Max(bid => bid.MinutesAgo) + 15)));

                await DriveAsync(AuctionLotWorkflow, lotId, context, LotPath(lot.State), cancellationToken);
                created++;
            }
        }

        _logger.LogInformation("Staged {Count} auction lots across {Auctions} auctions",
            created, DemoCatalogue.Auctions.Count);

        return created;
    }

    private static JsonArray BidLadder(DemoLot lot)
    {
        var ladder = new JsonArray();

        foreach (var bid in lot.Bids.OrderByDescending(bid => bid.MinutesAgo))
        {
            ladder.Add(new JsonObject
            {
                ["bidderMask"] = bid.BidderMask,
                ["amountMinor"] = bid.AmountMinor,
                ["minutesAgo"] = bid.MinutesAgo,
            });
        }

        return ladder;
    }

    private static IReadOnlyList<DemoStep> LotPath(string state) => state switch
    {
        LotState.Live => [new("open", null, TimeSpan.FromMinutes(5))],
        LotState.InExtension =>
        [
            new("open", null, TimeSpan.FromMinutes(5)),
            new("extend", null, TimeSpan.FromMinutes(9)),
        ],
        LotState.Sold =>
        [
            new("open", null, TimeSpan.FromMinutes(5)),
            new("hammer_down", null, TimeSpan.FromMinutes(20)),
        ],
        LotState.ReserveNotMet =>
        [
            new("open", null, TimeSpan.FromMinutes(5)),
            new("close_under_reserve", null, TimeSpan.FromMinutes(25)),
        ],
        _ => [],
    };

    // ── Deals ────────────────────────────────────────────────────────────────────────────────────

    private async Task<int> SeedDealsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var created = 0;

        foreach (var deal in DemoCatalogue.Deals)
        {
            var dealId = UuidV7.New();

            var context = new JsonObject
            {
                ["deal"] = new JsonObject
                {
                    ["registration"] = deal.Registration,
                    ["amountMinor"] = deal.AmountMinor,
                    ["dealerCode"] = deal.DealerCode,
                },
                // The guard on `documents_verified` reads this. A deal that has not reached RTO
                // transfer never had it true, which is exactly the point of the gate.
                ["documents"] = new JsonObject
                {
                    ["allVerified"] = deal.State is DealState.RtoTransfer or DealState.Completed,
                },
            };

            _clock.MoveTo(now.AddDays(-deal.AgeDays));

            await DriveAsync(DealWorkflow, dealId, context, DealPath(deal.State), cancellationToken);
            created++;
        }

        _logger.LogInformation("Staged {Count} deals", created);
        return created;
    }

    /// <summary>
    /// The prefix of the settlement path that ends in the state we want.
    /// </summary>
    /// <remarks>
    /// Spelled out per state rather than built by truncating a list, because a deal's path is the
    /// one place where an off-by-one in "how far did this get" is invisible: every prefix is a
    /// legitimate state, so a wrong answer looks entirely plausible on screen.
    /// </remarks>
    private static IReadOnlyList<DemoStep> DealPath(string state) => state switch
    {
        DealState.AwaitingPayment => [Confirm],
        DealState.PaymentReceived => [Confirm, CapturePayment],
        DealState.DocumentsPending => [Confirm, CapturePayment, StartPaperwork],
        DealState.RtoTransfer => [Confirm, CapturePayment, StartPaperwork, DocumentsVerified],
        DealState.Completed =>
            [Confirm, CapturePayment, StartPaperwork, DocumentsVerified, TransferComplete],
        _ => [],
    };

    private static readonly DemoStep Confirm = new("confirm", null, TimeSpan.FromMinutes(20));

    private static readonly DemoStep CapturePayment = new("capture_payment", null, TimeSpan.FromHours(6));

    private static readonly DemoStep StartPaperwork = new("start_paperwork", null, TimeSpan.FromHours(4));

    private static readonly DemoStep DocumentsVerified = new("documents_verified", null, TimeSpan.FromDays(1));

    private static readonly DemoStep TransferComplete = new("transfer_complete", null, TimeSpan.FromDays(4));

    // ── Bookings ─────────────────────────────────────────────────────────────────────────────────

    private async Task<int> SeedBookingsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var created = 0;

        foreach (var booking in DemoCatalogue.Bookings)
        {
            var bookingId = UuidV7.New();

            var context = new JsonObject
            {
                ["booking"] = new JsonObject
                {
                    ["registration"] = booking.Registration,
                    ["kind"] = booking.Kind,
                },
            };

            _clock.MoveTo(now.AddDays(-booking.DaysAgo).AddMinutes(-30));

            await DriveAsync(BookingWorkflow, bookingId, context, BookingPath(booking.State), cancellationToken);
            created++;
        }

        _logger.LogInformation("Staged {Count} bookings", created);
        return created;
    }

    private static IReadOnlyList<DemoStep> BookingPath(string state) => state switch
    {
        BookingState.Requested => [],
        BookingState.Confirmed => [new("confirm", null, TimeSpan.FromMinutes(12))],
        BookingState.InProgress =>
        [
            new("confirm", null, TimeSpan.FromMinutes(12)),
            new("start", null, TimeSpan.FromMinutes(20)),
        ],
        BookingState.Completed =>
        [
            new("confirm", null, TimeSpan.FromMinutes(12)),
            new("start", null, TimeSpan.FromHours(20)),
            new("complete", null, TimeSpan.FromMinutes(45)),
        ],
        BookingState.Cancelled =>
        [
            new("confirm", null, TimeSpan.FromMinutes(12)),
            new("cancel", "Buyer asked to move the slot to next weekend.", TimeSpan.FromHours(5)),
        ],
        _ => [],
    };

    // ── Attribute values ─────────────────────────────────────────────────────────────────────────

    private async Task<IReadOnlyDictionary<string, AttributeDefinition>> LoadAttributesAsync(
        CancellationToken cancellationToken)
        // Options are eager-loaded because the validator resolves every enum value against them, and
        // a definition without its options rejects every value it is given.
        => await _context.AttributeDefinitions
            .Include(nameof(AttributeDefinition.Options))
            .ToDictionaryAsync(definition => definition.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

    /// <summary>
    /// Writes one vehicle's specification, into the EAV rows and the vehicle's own bag at once.
    /// </summary>
    /// <remarks>
    /// Both are filled in a single pass rather than the bag being rebuilt from a later query, because
    /// the two are only guaranteed to agree if nothing can write to one without writing to the other.
    /// A second pass is a second place to forget an attribute, and the symptom — a spec sheet missing
    /// a row that a facet search still matches on — is one nobody would look for here.
    /// </remarks>
    private void WriteVehicleAttributes(
        IReadOnlyDictionary<string, AttributeDefinition> attributes,
        Vehicle vehicle,
        DemoListing listing,
        VehicleVariant? variant,
        DateTimeOffset now)
    {
        var vehicleId = vehicle.Id;
        var projection = new JsonObject();

        Write(attributes, VehicleEntityType, vehicleId, "manufacture_year",
            TypedAttributeValue.OfInteger(listing.Year), projection);
        Write(attributes, VehicleEntityType, vehicleId, "fuel_type",
            TypedAttributeValue.OfEnum(listing.FuelCode), projection);
        Write(attributes, VehicleEntityType, vehicleId, "transmission",
            TypedAttributeValue.OfEnum(listing.TransmissionCode), projection);
        Write(attributes, VehicleEntityType, vehicleId, "colour",
            TypedAttributeValue.OfEnum(listing.ColourCode), projection);
        Write(attributes, VehicleEntityType, vehicleId, "kms_driven",
            TypedAttributeValue.OfInteger(listing.KmsDriven), projection);
        Write(attributes, VehicleEntityType, vehicleId, "owners",
            TypedAttributeValue.OfInteger(listing.Owners), projection);
        Write(attributes, VehicleEntityType, vehicleId, "insurance_valid_till",
            TypedAttributeValue.OfDate(DateOnly.ParseExact(
                listing.InsuranceValidTill, "yyyy-MM-dd", CultureInfo.InvariantCulture)), projection);

        if (variant?.BodyTypeCode is { } bodyType)
        {
            Write(attributes, VehicleEntityType, vehicleId, "body_type",
                TypedAttributeValue.OfEnum(bodyType), projection);
        }

        if (variant?.Seats is { } seats)
        {
            Write(attributes, VehicleEntityType, vehicleId, "seats",
                TypedAttributeValue.OfInteger(seats), projection);
        }

        if (DemoSpecs.Electric.TryGetValue(listing.ModelCode, out var electric))
        {
            Write(attributes, VehicleEntityType, vehicleId, "battery_kwh",
                TypedAttributeValue.OfNumber(electric.BatteryKwh), projection);
            Write(attributes, VehicleEntityType, vehicleId, "range_km",
                TypedAttributeValue.OfInteger(electric.RangeKm), projection);
            Write(attributes, VehicleEntityType, vehicleId, "battery_health_pct",
                TypedAttributeValue.OfInteger(DemoSpecs.BatteryHealth(listing.KmsDriven)), projection);
            Write(attributes, VehicleEntityType, vehicleId, "charging_type",
                TypedAttributeValue.OfChoices(electric.ChargingTypes), projection);
        }

        if (DemoSpecs.Commercial.TryGetValue(listing.ModelCode, out var commercial))
        {
            Write(attributes, VehicleEntityType, vehicleId, "payload_kg",
                TypedAttributeValue.OfInteger(commercial.PayloadKg), projection);
            Write(attributes, VehicleEntityType, vehicleId, "axle_config",
                TypedAttributeValue.OfEnum(commercial.AxleConfig), projection);
            Write(attributes, VehicleEntityType, vehicleId, "permit_type",
                TypedAttributeValue.OfEnum(commercial.PermitType), projection);
        }

        // The north star's acceptance test, as data on a row: a category whose attributes no C# type
        // has ever heard of, filterable the moment it is written.
        if (DemoSpecs.Construction.TryGetValue(listing.ModelCode, out var construction))
        {
            Write(attributes, VehicleEntityType, vehicleId, "boom_length",
                TypedAttributeValue.OfNumber(construction.BoomLengthMetres), projection);
            Write(attributes, VehicleEntityType, vehicleId, "operating_weight_kg",
                TypedAttributeValue.OfInteger(construction.OperatingWeightKg), projection);
        }

        vehicle.ProjectAttributes(projection, now);
    }

    /// <summary>
    /// Validates a value against its own definition, then writes the row.
    /// </summary>
    /// <remarks>
    /// The validation is not ceremony. It is the same call the sell form and the dealer bulk upload
    /// make, so demo data that would have been rejected from either cannot get in through the back
    /// door — and a bound tightened in the taxonomy seeder fails here, at startup, rather than
    /// producing a listing the product's own validator disagrees with.
    /// </remarks>
    private void Write(
        IReadOnlyDictionary<string, AttributeDefinition> attributes,
        string entityType,
        Guid entityId,
        string attributeCode,
        TypedAttributeValue value,
        JsonObject? projection = null)
    {
        var definition = attributes[attributeCode];

        var effective = new EffectiveAttribute(
            definition,
            definition.IsRequired,
            definition.IsSearchable,
            definition.FacetStyle,
            definition.DisplayGroup,
            definition.SortOrder,
            Guid.Empty);

        var json = value.ToJson();

        var validated = AttributeValueValidator.Validate(effective, json);

        if (validated.IsFailure)
        {
            throw new InvalidOperationException(
                $"Demo value for '{attributeCode}' is not acceptable to the taxonomy registry — "
                + validated.Error);
        }

        _context.EntityAttributeValues.Add(
            EntityAttributeValue.Create(entityType, entityId, definition.Id, value));

        // Cloned, because the node handed to the row and the node put in the bag must not be the
        // same instance — JsonNode parents itself to whichever object claims it first, and the
        // second claim throws.
        if (projection is not null)
        {
            projection[definition.Code] = json?.DeepClone();
        }
    }

    // ── Workflow driving ─────────────────────────────────────────────────────────────────────────

    private async Task DriveAsync(
        string workflowKey,
        Guid entityId,
        JsonNode context,
        IReadOnlyList<DemoStep> steps,
        CancellationToken cancellationToken)
    {
        Require(
            await _engine.StartAsync(workflowKey, entityId, context, actorId: null, cancellationToken),
            $"start {workflowKey}");

        foreach (var step in steps)
        {
            _clock.Advance(step.After);

            Require(
                await _engine.FireAsync(
                    new WorkflowFireRequest
                    {
                        WorkflowKey = workflowKey,
                        EntityId = entityId,
                        ActionCode = step.ActionCode,
                        // The seeder is the scheduler, not a person: it holds every permission and
                        // records no actor id, so the history reads as "system" rather than as a
                        // moderator who does not exist.
                        Actor = WorkflowActor.System,
                        Context = context,
                        Reason = step.Reason,
                    },
                    cancellationToken),
                $"{workflowKey}/{step.ActionCode}");
        }
    }

    private static void Require(Result<WorkflowSnapshot> result, string what)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Demo workflow step '{what}' was refused — {result.Error}");
        }
    }
}

/// <summary>One transition the demo fires, and how long after the previous one it happened.</summary>
internal sealed record DemoStep(string ActionCode, string? Reason, TimeSpan After);

/// <summary>
/// The per-category specifications the demo vehicles carry beyond the shared ones.
/// </summary>
/// <remarks>
/// Kept as a lookup rather than as columns on <see cref="DemoListing"/> because that is the shape of
/// the underlying truth: an electric hatchback has a battery and no payload, a tipper has a payload
/// and no boom, and a backhoe loader has a boom. A record wide enough for all three would be mostly
/// nulls, which is exactly the design the attribute registry exists to avoid.
/// </remarks>
internal static class DemoSpecs
{
    private static readonly string[] CarCharging = ["ac_slow", "ac_fast", "dc_fast", "ccs2"];

    private static readonly string[] ScooterCharging = ["ac_slow"];

    public static IReadOnlyDictionary<string, ElectricSpec> Electric { get; } =
        new Dictionary<string, ElectricSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["nexon-ev"] = new(40.5m, 437, CarCharging),
            ["tiago-ev"] = new(24.0m, 315, CarCharging),
            ["e6"] = new(71.7m, 520, CarCharging),
            ["ather-450x"] = new(3.7m, 150, ScooterCharging),
            ["ola-s1-pro"] = new(4.0m, 195, ScooterCharging),
        };

    public static IReadOnlyDictionary<string, CommercialSpec> Commercial { get; } =
        new Dictionary<string, CommercialSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["dost"] = new(1250, "4x2", "goods"),
            ["ace-gold"] = new(750, "4x2", "goods"),
            ["bolero-pickup"] = new(1700, "4x2", "goods"),
            ["pro-2049"] = new(4900, "4x2", "national"),
            ["boss-1115"] = new(6800, "4x2", "national"),
            ["traveller-26"] = new(2100, "4x2", "contract_carriage"),
        };

    public static IReadOnlyDictionary<string, ConstructionSpec> Construction { get; } =
        new Dictionary<string, ConstructionSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["3dx-backhoe"] = new(5.97m, 7830),
        };

    /// <summary>
    /// State of health, derived from distance rather than invented.
    /// </summary>
    /// <remarks>
    /// Roughly one percentage point per 8,000 km, floored at 80% — the warranty threshold Indian
    /// manufacturers publish — and ceilinged at 98%, because a used battery that reports 100% is a
    /// battery nobody has measured. Deterministic on purpose: a demo whose figures move between runs
    /// is a demo somebody will screenshot twice and notice.
    /// </remarks>
    public static long BatteryHealth(int kmsDriven)
        => Math.Clamp(100 - (kmsDriven / 8_000), 80, 98);
}

internal sealed record ElectricSpec(decimal BatteryKwh, long RangeKm, IReadOnlyList<string> ChargingTypes);

internal sealed record CommercialSpec(long PayloadKg, string AxleConfig, string PermitType);

internal sealed record ConstructionSpec(decimal BoomLengthMetres, long OperatingWeightKg);
