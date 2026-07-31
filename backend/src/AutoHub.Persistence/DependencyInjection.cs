using AutoHub.Modules.Auction.Domain;
using AutoHub.Modules.Bookings.Domain;
using AutoHub.Modules.Catalog.Application.Abstractions;
using AutoHub.Modules.Deals.Domain;
using AutoHub.Modules.Identity.Contracts;
using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Payments.Application;
using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.Modules.Valuation.Application.Abstractions;
using AutoHub.Persistence.Idempotency;
using AutoHub.Persistence.Interceptors;
using AutoHub.Persistence.Outbox;
using AutoHub.Persistence.Repositories;
using AutoHub.Persistence.Search;
using AutoHub.Persistence.Stores;
using CatalogAttributeStore = AutoHub.Modules.Catalog.Application.Abstractions.IEntityAttributeStore;
using ListingAttributeStore = AutoHub.Modules.Listings.Application.Abstractions.IEntityAttributeStore;
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
using AutoHub.SharedKernel.Security;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Persistence;

public static class DependencyInjection
{
    /// <summary>The connection string key. One name, so a typo is a startup failure and not a mystery.</summary>
    public const string ConnectionStringName = "Postgres";

    /// <summary>
    /// Wires the database, the interceptors, the registry stores and the outbox.
    /// </summary>
    /// <remarks>
    /// <b>Interceptor order is load-bearing and is the registration order.</b> Soft delete runs
    /// first, converting a <c>Remove</c> into an update so that auditing — which runs second — sees
    /// a modification and stamps who hid the row. The outbox runs last, after both, so the events it
    /// copies belong to entities whose audit columns are already final.
    /// <para>
    /// Everything except the clock is scoped, because the interceptors read the current user and the
    /// stores read a <c>DbContext</c>, and both of those are per request. The clock is a singleton
    /// because an instant has no state.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"No '{ConnectionStringName}' connection string is configured. "
                + "The API cannot start without a database.");

        services.TryAddSingleton<IClock, SystemClock>();

        // The host replaces this with one that reads the request principal. Registered as a fallback
        // so a background service, a migration or a test resolves something rather than failing to
        // construct — and so "nobody was signed in" is a value rather than an exception.
        services.TryAddScoped<ICurrentUser>(_ => UnauthenticatedUser.Instance);

        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<OutboxInterceptor>();

        services.AddDbContext<AutoHubDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", Schemas.Platform);

                // Transient faults are a fact of a managed Postgres: a failover, a connection reset,
                // a brief exhaustion of the pool. Retrying them in the driver is cheaper than
                // surfacing a 500 to a dealer mid-bid. The retry is safe because every write
                // endpoint is idempotent by contract.
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            options.AddInterceptors(
                provider.GetRequiredService<SoftDeleteInterceptor>(),
                provider.GetRequiredService<AuditingInterceptor>(),
                provider.GetRequiredService<OutboxInterceptor>());
        });

        AddRegistryStores(services);
        AddOutbox(services, configuration);
        AddIdempotency(services, configuration);

        return services;
    }

    /// <summary>
    /// The EF implementations of the interfaces <c>AutoHub.Platform</c> declares.
    /// </summary>
    /// <remarks>
    /// Platform declares them and never references EF Core, which is what keeps the engines
    /// unit-testable against in-memory stores. This method is the one place the two halves meet, so
    /// a registry whose store is missing fails at startup with a name rather than at first request
    /// with a null.
    /// </remarks>
    private static void AddRegistryStores(IServiceCollection services)
    {
        services.TryAddScoped<ITaxonomyStore, EfTaxonomyStore>();
        services.TryAddScoped<IFormStore, EfFormStore>();
        services.TryAddScoped<IWorkflowStore, EfWorkflowStore>();
        services.TryAddScoped<IPricingRuleStore, EfPricingRuleStore>();
        services.TryAddScoped<IAccessStore, EfAccessStore>();
        services.TryAddScoped<IFeatureStore, EfFeatureStore>();
        services.TryAddScoped<INotificationRegistryStore, EfNotificationRegistryStore>();
        services.TryAddScoped<IIntegrationRegistryStore, EfIntegrationRegistryStore>();
        services.TryAddScoped<II18nResourceStore, EfI18nResourceStore>();
        services.TryAddScoped<IGeographyStore, EfGeographyStore>();

        // Not a registry, but the same split for the same reason: the auth flow and the audit trail
        // are declared where they are used and implemented where EF Core lives.
        services.TryAddScoped<IIdentityStore, EfIdentityStore>();
        services.TryAddScoped<IAuditTrail, EfAuditTrail>();

        AddModuleRepositories(services);
    }

    /// <summary>
    /// The EF implementations of the interfaces the domain modules declare.
    /// </summary>
    /// <remarks>
    /// Each module declares its own repository interface and never references EF Core, which is what
    /// makes its handlers testable against a list and its extraction to a service a build-file
    /// change. This method is the one place the two halves meet — so a module whose repository is
    /// missing fails at startup with a name, rather than at first request with a null.
    /// <para>
    /// <see cref="EfEntityAttributeStore"/> is registered against two interfaces because two modules
    /// declare the same contract independently. That duplication is the isolation rule taken
    /// literally, and it costs an interface declaration rather than a second instance.
    /// </para>
    /// <para>
    /// <c>ISearchIndex</c> resolves to the PostgreSQL implementation. The OpenSearch adapter lives in
    /// the listings module and is opted into explicitly by the host, because moving every buyer's
    /// search to a different engine should read like a decision in startup rather than hide in a
    /// settings file somebody copied between environments.
    /// </para>
    /// </remarks>
    private static void AddModuleRepositories(IServiceCollection services)
    {
        services.TryAddScoped<IVehicleRepository, EfVehicleRepository>();

        services.TryAddScoped<IListingRepository, EfListingRepository>();
        services.TryAddScoped<IListingOfferRepository, EfListingOfferRepository>();
        services.TryAddScoped<ISearchIndex, PostgresListingSearchIndex>();

        services.TryAddScoped<IValuationRepository, EfValuationRepository>();
        services.TryAddScoped<IValuationModelStore, EfValuationModelStore>();

        services.TryAddScoped<IAuctionRepository, EfAuctionRepository>();
        services.TryAddScoped<IAuctionLotRepository, EfAuctionLotRepository>();
        services.TryAddScoped<IAuctionTypeProfileStore, EfAuctionTypeProfileStore>();

        services.TryAddScoped<IDealRepository, EfDealRepository>();

        services.TryAddScoped<ILedgerRepository, EfLedgerRepository>();
        services.TryAddScoped<ILedgerAccountResolver, EfLedgerAccountResolver>();
        services.TryAddScoped<IWalletRepository, EfWalletRepository>();
        services.TryAddScoped<IPaymentIntentRepository, EfPaymentIntentRepository>();
        services.TryAddScoped<IPayoutBatchRepository, EfPayoutBatchRepository>();
        services.TryAddScoped<IReconciliationRepository, EfReconciliationRepository>();

        // Deliberately Add rather than TryAdd, and registered before the module runs. The payments
        // module registers a code-based template store with TryAdd as a floor so a fresh database
        // still posts; the moment finance edits a template the database is the answer, so the
        // EF-backed store has to be the one that wins.
        services.TryAddScoped<IPostingTemplateStore, EfPostingTemplateStore>();

        services.TryAddScoped<IBookableResourceRepository, EfBookableResourceRepository>();
        services.TryAddScoped<IBookingRepository, EfBookingRepository>();

        // Only the persistence layer can supply this: the advisory lock has to run on the ambient
        // transaction, and the bookings module never references EF Core. A host that forgets it fails
        // to construct the handler at startup rather than double-booking an inspector in production.
        services.TryAddScoped<IResourceDayLock, PostgresResourceDayLock>();

        // One instance serving both modules' identical contracts, so a single request that writes
        // vehicle attributes and listing attributes does both through one change tracker.
        services.AddScoped<EfEntityAttributeStore>();
        services.TryAddScoped<CatalogAttributeStore>(
            provider => provider.GetRequiredService<EfEntityAttributeStore>());
        services.TryAddScoped<ListingAttributeStore>(
            provider => provider.GetRequiredService<EfEntityAttributeStore>());
    }

    private static void AddOutbox(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        // TryAdd, so a host that has a real message bus registers its own dispatcher and this one
        // never runs. Without a fallback the hosted service would fail to resolve and take the API
        // down on a machine with no broker.
        services.TryAddScoped<IOutboxDispatcher, LoggingOutboxDispatcher>();

        services.AddHostedService<OutboxProcessor>();
    }

    private static void AddIdempotency(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdempotencyOptions>(configuration.GetSection(IdempotencyOptions.SectionName));
        services.TryAddScoped<IIdempotencyStore, EfIdempotencyStore>();
    }
}
