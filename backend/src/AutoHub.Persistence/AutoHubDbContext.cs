using System.Reflection;
using AutoHub.Modules.Auction.Domain;
using AutoHub.Modules.Bookings.Domain;
using AutoHub.Modules.Catalog.Domain.Vehicles;
using AutoHub.Modules.Deals.Domain;
using AutoHub.Modules.Identity.Domain;
using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Modules.Listings.Domain.Offers;
using AutoHub.Modules.Payments.Application;
using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.Modules.Valuation.Domain.Models;
using AutoHub.Modules.Valuation.Domain.Valuations;
using AutoHub.Persistence.Converters;
using AutoHub.Persistence.Idempotency;
using AutoHub.Persistence.Naming;
using AutoHub.Persistence.Outbox;
using AutoHub.Persistence.Search;
using AutoHub.Platform.Access;
using AutoHub.Platform.Auditing;
using AutoHub.Platform.Features;
using AutoHub.Platform.Forms;
using AutoHub.Platform.Geography;
using AutoHub.Platform.Integrations;
using AutoHub.Platform.Localization;
using AutoHub.Platform.Notifications;
using AutoHub.Platform.Pricing;
using AutoHub.Platform.Taxonomy;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Module = AutoHub.Platform.Features.Module;

namespace AutoHub.Persistence;

/// <summary>
/// The one database context, with a PostgreSQL schema per module.
/// </summary>
/// <remarks>
/// <b>Why one context and not twenty.</b> The modular monolith's promise is that a module can be
/// extracted to its own service by changing project references and a connection string. A context
/// per module sounds like it serves that promise and actually undermines it, because it buys the
/// separation at a price paid on every single request:
/// <list type="bullet">
/// <item>A transaction cannot span two contexts without a distributed transaction coordinator, so
/// writing a deal and its outbox row — the mechanism that makes eventing reliable — would already
/// need two-phase commit on day one, for a monolith running against one database.</item>
/// <item>Every context is a separate connection from the pool and a separate model to build and
/// cache. Twenty of them is twenty connections per request-ish and a cold start measured in
/// seconds.</item>
/// <item>The change tracker would not see across modules, so <c>SaveChanges</c> would have to be
/// called once per context in an order somebody maintains by hand, and partial failure would be a
/// state nobody designed.</item>
/// </list>
/// <b>What the schemas buy instead.</b> Boundaries that are visible in the database and enforceable
/// by it: <c>GRANT</c> is per schema, <c>pg_dump --schema</c> is per schema, and a cross-schema
/// foreign key is a line in a migration that a reviewer can see and object to. The isolation rule —
/// a module never reads another module's tables, only its contracts and its events — is a rule this
/// layout makes checkable rather than aspirational.
/// <para>
/// <b>What would have to change to split it.</b> Three things, in this order. First, no cross-schema
/// foreign key may exist: a module owning a reference to another module's row keeps the id and drops
/// the constraint, so the tables can be moved apart at all. Second, every cross-module read must
/// already go through a contract rather than a join, because after the split the join has no table
/// to bind to — this is why <c>EntityAttributeValue.EntityType</c> is a string and not a foreign
/// key. Third, the outbox becomes per-service: the interceptor is unchanged, but the processor in
/// the extracted service reads its own table and publishes to a broker rather than to an in-process
/// bus. Nothing in the domain layer moves, which is the point of doing the first two now.
/// </para>
/// </remarks>
public sealed class AutoHubDbContext : DbContext
{
    public AutoHubDbContext(DbContextOptions<AutoHubDbContext> options) : base(options)
    {
        // Cascades are evaluated at SaveChanges, not the instant Remove is called, and this is what
        // makes soft deletion work at all. EF's default is to cascade immediately, which means
        // `context.Remove(attributeDefinition)` severs its dependents' required relationships and
        // throws before SoftDeleteInterceptor has had any chance to say the removal was going to be
        // an UPDATE. Deferring puts the interceptor first: by the time cascade runs, the principal
        // is Modified rather than Deleted and there is nothing to cascade from.
        ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
        ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
    }

    // ── Registry #1: Taxonomy ────────────────────────────────────────────────────────────────────
    public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();

    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();

    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();

    public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();

    public DbSet<VehicleMake> VehicleMakes => Set<VehicleMake>();

    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();

    public DbSet<VehicleVariant> VehicleVariants => Set<VehicleVariant>();

    public DbSet<EntityAttributeValue> EntityAttributeValues => Set<EntityAttributeValue>();

    // ── Registry #2: Form schema ─────────────────────────────────────────────────────────────────
    public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();

    public DbSet<FormVersion> FormVersions => Set<FormVersion>();

    public DbSet<FormStep> FormSteps => Set<FormStep>();

    public DbSet<FormField> FormFields => Set<FormField>();

    public DbSet<FormValidationRule> FormValidationRules => Set<FormValidationRule>();

    // ── Registry #3: Workflow ────────────────────────────────────────────────────────────────────
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();

    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();

    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();

    public DbSet<WorkflowEffect> WorkflowEffects => Set<WorkflowEffect>();

    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();

    public DbSet<WorkflowHistoryEntry> WorkflowHistoryEntries => Set<WorkflowHistoryEntry>();

    // ── Registry #4: Pricing and commission ──────────────────────────────────────────────────────
    public DbSet<RuleSet> PricingRuleSets => Set<RuleSet>();

    public DbSet<PricingRule> PricingRules => Set<PricingRule>();

    public DbSet<FeeComponent> FeeComponents => Set<FeeComponent>();

    public DbSet<FeeSlab> FeeSlabs => Set<FeeSlab>();

    // ── Registry #5: RBAC ────────────────────────────────────────────────────────────────────────
    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    // ── Registry #6: Feature and module ──────────────────────────────────────────────────────────
    public DbSet<Module> Modules => Set<Module>();

    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    public DbSet<FlagRule> FlagRules => Set<FlagRule>();

    // ── Registry #7: Notification ────────────────────────────────────────────────────────────────
    public DbSet<NotificationEvent> NotificationEvents => Set<NotificationEvent>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<NotificationTemplateVariable> NotificationTemplateVariables
        => Set<NotificationTemplateVariable>();

    public DbSet<NotificationSubscription> NotificationSubscriptions => Set<NotificationSubscription>();

    public DbSet<ChannelPreference> NotificationChannelPreferences => Set<ChannelPreference>();

    // ── Registry #8: Integration ─────────────────────────────────────────────────────────────────
    public DbSet<IntegrationCapability> IntegrationCapabilities => Set<IntegrationCapability>();

    public DbSet<IntegrationProvider> IntegrationProviders => Set<IntegrationProvider>();

    public DbSet<ProviderRoute> ProviderRoutes => Set<ProviderRoute>();

    // ── Registry #9: Localisation ────────────────────────────────────────────────────────────────
    public DbSet<I18nLocale> I18nLocales => Set<I18nLocale>();

    public DbSet<I18nResource> I18nResources => Set<I18nResource>();

    // ── Registry #10: Geography and master data ──────────────────────────────────────────────────
    public DbSet<Country> Countries => Set<Country>();

    public DbSet<State> States => Set<State>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<Pincode> Pincodes => Set<Pincode>();

    public DbSet<RtoOffice> RtoOffices => Set<RtoOffice>();

    public DbSet<ServiceArea> ServiceAreas => Set<ServiceArea>();

    // ── Identity: who is asking ──────────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();

    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ── Module: Catalog — the canonical vehicle ──────────────────────────────────────────────────
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<VehicleIdentity> VehicleIdentities => Set<VehicleIdentity>();

    // ── Module: Listings — an intent to transact a vehicle ───────────────────────────────────────
    // There is no listing status DbSet, and no status column on the listing itself. State lives in
    // WorkflowInstances above, which is the whole point of the workflow registry.
    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<ListingMedia> ListingMedia => Set<ListingMedia>();

    public DbSet<ListingOffer> ListingOffers => Set<ListingOffer>();

    public DbSet<OfferTurn> ListingOfferTurns => Set<OfferTurn>();

    /// <summary>
    /// The PostgreSQL search projection, written by a workflow effect rather than by a save hook.
    /// </summary>
    /// <remarks>
    /// Which listings are in here is decided by whichever transition an operator wired
    /// <c>listings.search_index.publish</c> to — so "publicly visible" is registry configuration and
    /// not a state comparison in a query.
    /// </remarks>
    public DbSet<ListingSearchDocument> ListingSearchDocuments => Set<ListingSearchDocument>();

    // ── Module: Valuation — the price, and the coefficients behind it ────────────────────────────
    public DbSet<Modules.Valuation.Domain.Valuations.Valuation> Valuations
        => Set<Modules.Valuation.Domain.Valuations.Valuation>();

    public DbSet<ValuationChannelAmount> ValuationChannelAmounts => Set<ValuationChannelAmount>();

    public DbSet<ValuationAdjustment> ValuationAdjustments => Set<ValuationAdjustment>();

    public DbSet<ValuationModelDefinition> ValuationModels => Set<ValuationModelDefinition>();

    public DbSet<ValuationCurve> ValuationCurves => Set<ValuationCurve>();

    public DbSet<CurvePoint> ValuationCurvePoints => Set<CurvePoint>();

    public DbSet<ValuationFactor> ValuationFactors => Set<ValuationFactor>();

    public DbSet<ValuationChannelSpread> ValuationChannelSpreads => Set<ValuationChannelSpread>();

    public DbSet<ConfidenceWeight> ValuationConfidenceWeights => Set<ConfidenceWeight>();

    public DbSet<ConfidenceBand> ValuationConfidenceBands => Set<ConfidenceBand>();

    // ── Module: Auction — the sale event, its lots, and every admission decision ─────────────────
    public DbSet<Modules.Auction.Domain.Auction> Auctions => Set<Modules.Auction.Domain.Auction>();

    public DbSet<AuctionTypeProfile> AuctionTypeProfiles => Set<AuctionTypeProfile>();

    public DbSet<AuctionLot> AuctionLots => Set<AuctionLot>();

    public DbSet<Bid> Bids => Set<Bid>();

    // ── Module: Deals — the settlement spine every intent shares ─────────────────────────────────
    public DbSet<Deal> Deals => Set<Deal>();

    public DbSet<DealParty> DealParties => Set<DealParty>();

    public DbSet<DealLine> DealLines => Set<DealLine>();

    // ── Module: Bookings — every appointment the platform makes ──────────────────────────────────
    public DbSet<BookableResource> BookableResources => Set<BookableResource>();

    public DbSet<CalendarWindow> CalendarWindows => Set<CalendarWindow>();

    public DbSet<CalendarException> CalendarExceptions => Set<CalendarException>();

    public DbSet<Booking> Bookings => Set<Booking>();

    // ── Module: Payments — the double-entry ledger and the money that moves through it ───────────
    // The ledger is the truth. Wallets are a projection of it, and reconciliation exists because a
    // projection can be wrong in a way an append-only entry table cannot.
    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();

    public DbSet<Journal> LedgerJournals => Set<Journal>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public DbSet<PostingTemplate> PostingTemplates => Set<PostingTemplate>();

    public DbSet<Wallet> Wallets => Set<Wallet>();

    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();

    public DbSet<PayoutBatch> PayoutBatches => Set<PayoutBatch>();

    public DbSet<PayoutInstruction> PayoutInstructions => Set<PayoutInstruction>();

    /// <summary>
    /// The nightly proof that the ledger and the provider agree — including the nights they did.
    /// </summary>
    /// <remarks>
    /// A run with no differences is not a row to skip writing. "Did reconciliation run every night
    /// last quarter?" is answered by these rows existing, and a scheduler that quietly stopped is
    /// invisible if only failures are recorded.
    /// </remarks>
    public DbSet<ReconciliationRun> ReconciliationRuns => Set<ReconciliationRun>();

    public DbSet<ReconciliationDifference> ReconciliationDifferences => Set<ReconciliationDifference>();

    // ── Infrastructure the whole platform depends on ─────────────────────────────────────────────
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    /// <summary>
    /// The append-only admin and money trail. Distinct from the <c>IAuditable</c> columns, which say
    /// who last touched a row; this says what the change actually did, and keeps the before.
    /// </summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>
    /// Model-wide type mappings, applied before any configuration runs.
    /// </summary>
    /// <remarks>
    /// Every rule here is one that must hold for every property of its type, everywhere. Stating
    /// them once is the difference between "instants are UTC" being a property of the schema and it
    /// being a property of whoever wrote each configuration.
    /// </remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>()
            .HaveColumnType(UtcDateTimeOffsetConverter.ColumnType);

        configurationBuilder
            .Properties<DateTimeOffset?>()
            .HaveConversion<UtcDateTimeOffsetConverter>()
            .HaveColumnType(UtcDateTimeOffsetConverter.ColumnType);

        // Codes and keys are the longest strings in the registries and none of them approaches this.
        // An unbounded text column is fine in PostgreSQL, but a stated bound is what stops a bulk
        // import putting a paragraph where a slug belongs.
        configurationBuilder.Properties<string>().HaveMaxLength(512);

        // Added last so it runs after the built-in conventions and can clear up after them.
        configurationBuilder.Conventions.Add(_ => new DropConventionScratchStateConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Declared so a fresh database is created with them rather than depending on whoever
        // provisioned the server having remembered. citext backs case-insensitive natural keys,
        // pg_trgm backs fuzzy name search, btree_gin lets one index mix a scalar with a JSONB
        // column, and pgcrypto/unaccent are used by the search and token paths.
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("btree_gin");
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("unaccent");

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplyConcurrencyTokens(modelBuilder);
        ApplyClientGeneratedKeys(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);

        // Last, so it rewrites every name the steps above produced.
        SnakeCaseNaming.ApplyTo(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Points every aggregate's <see cref="AggregateRoot.RowVersion"/> at PostgreSQL's <c>xmin</c>.
    /// </summary>
    /// <remarks>
    /// <c>xmin</c> is the id of the transaction that last wrote the row. It already exists on every
    /// table, it is maintained by the engine, and it costs no column, no trigger and no write
    /// amplification — which is why it is preferred here over a <c>bigint</c> version column that
    /// every <c>UPDATE</c> would have to remember to bump.
    /// <para>
    /// Applied in a loop rather than per configuration because "every aggregate is concurrency
    /// checked" is a property of the model, and a single forgotten configuration would silently make
    /// one aggregate last-write-wins. Two admins editing the same commission rule is a conflict we
    /// surface, not one we lose.
    /// </para>
    /// </remarks>
    private static void ApplyConcurrencyTokens(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var property = entityType.FindProperty(nameof(AggregateRoot.RowVersion));

            if (property is null)
            {
                continue;
            }

            property.SetColumnName("xmin");
            property.SetColumnType("xid");
            property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            property.IsConcurrencyToken = true;
        }
    }

    /// <summary>
    /// Tells EF that this application, not the database, produces every primary key.
    /// </summary>
    /// <remarks>
    /// EF's key convention marks a <see cref="Guid"/> primary key as store-generated on add. Every id
    /// in this model is minted by <c>UuidV7</c> in a factory method instead, so that
    /// annotation is simply untrue — and it is untrue in a way that breaks writes rather than reads.
    /// <para>
    /// The failure is specific and was found by the seeder: when a child entity is added to a
    /// <b>tracked</b> parent's collection — a <c>WorkflowHistoryEntry</c> appended to a
    /// <c>WorkflowInstance</c> that was loaded from the database, a <c>RolePermission</c> granted to
    /// an existing <c>Role</c> — change detection sees an entity whose key is already populated. With
    /// the key declared store-generated, EF reads a populated key as "the store gave this to us, so
    /// the row exists" and tracks the child as <c>Modified</c>. It then issues an <c>UPDATE</c>
    /// against a row that was never inserted, which affects zero rows and surfaces as a
    /// <c>DbUpdateConcurrencyException</c> — a message about optimistic concurrency for a problem
    /// that has nothing to do with concurrency.
    /// <para>
    /// Left unstated, that makes <i>every</i> workflow transition fail: the engine loads an instance,
    /// appends one history row, and saves. The first transition on any entity in the product would
    /// have thrown.
    /// </para>
    /// </para>
    /// <para>
    /// Applied as a loop rather than per configuration because "the application mints ids" is a
    /// property of the whole model, and one forgotten configuration reintroduces the bug in exactly
    /// one aggregate — which is far harder to find than all of them failing at once.
    /// </para>
    /// </remarks>
    private static void ApplyClientGeneratedKeys(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var key = entityType.FindPrimaryKey();

            // Composite keys are join tables whose columns are foreign keys; they are never minted.
            if (key is null || key.Properties.Count != 1)
            {
                continue;
            }

            var property = key.Properties[0];

            if (property.ClrType == typeof(Guid))
            {
                property.ValueGenerated = ValueGenerated.Never;
            }
        }
    }

    /// <summary>
    /// Hides soft-deleted rows from every query that does not ask for them.
    /// </summary>
    /// <remarks>
    /// The filter is what makes soft deletion invisible rather than merely available. Without it,
    /// every query in twenty modules would have to end in <c>.Where(x =&gt; x.DeletedAt == null)</c>,
    /// and the one that forgets is a deleted dealer reappearing in a search result.
    /// <para>
    /// Applied by reflection because <c>HasQueryFilter</c> needs the entity's closed generic type,
    /// and the alternative is repeating one identical line in every configuration of a soft-deletable
    /// entity — which is the same "remember to" problem the auditing interceptor exists to remove.
    /// </para>
    /// </remarks>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        var method = typeof(AutoHubDbContext).GetMethod(
            nameof(ApplySoftDeleteFilter),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || !typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            method.MakeGenericMethod(entityType.ClrType).Invoke(null, [modelBuilder]);
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
        => modelBuilder.Entity<TEntity>().HasQueryFilter(entity => entity.DeletedAt == null);
}
