using AutoHub.Api.Registries;
using AutoHub.Integrations;
using AutoHub.Platform.Access;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Features;
using AutoHub.Platform.Forms;
using AutoHub.Platform.Geography;
using AutoHub.Platform.Integrations;
using AutoHub.Platform.Localization;
using AutoHub.Platform.Notifications;
using AutoHub.Platform.Pricing;
using AutoHub.Platform.Taxonomy;
using AutoHub.Platform.Workflow;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace AutoHub.Api.Extensions;

internal static class PlatformExtensions
{
    /// <summary>
    /// Registers the ten registries, the two engines and the effect dispatcher.
    /// </summary>
    /// <remarks>
    /// Order matters exactly once, and it is at the top: Redis is registered before
    /// <c>AddRegistryCache</c> so the distributed tier picks it up. Every registry registration is a
    /// <c>TryAdd</c>, so "register the real thing first, let the fallback stand aside" is the single
    /// pattern the whole file follows.
    /// </remarks>
    public static WebApplicationBuilder AddPlatformRegistries(this WebApplicationBuilder builder)
    {
        AddRedis(builder);

        builder.Services.AddRegistryCache();

        builder.Services.AddTaxonomyRegistry();
        builder.Services.AddFormRegistry();
        builder.Services.AddWorkflowEngine();
        builder.Services.AddPricingEngine();
        builder.Services.AddPlatformAccess();
        builder.Services.AddPlatformFeatures();
        builder.Services.AddNotificationRegistry();
        builder.Services.AddIntegrationRegistry();
        builder.Services.AddLocalizationRegistry();
        builder.Services.AddGeographyRegistry();

        builder.Services.AddIntegrationFallbacks();

        return builder;
    }

    /// <summary>
    /// Registry 2, wired to registries 1 and 9.
    /// </summary>
    /// <remarks>
    /// The Platform assembly ships <c>EmptyFormAttributeSource</c> and
    /// <c>PassThroughFormLocalizer</c> so it can be tested and booted alone. The host replaces both
    /// with the real registries here — which is the only place in the codebase that knows registry 2
    /// depends on registry 1, and is exactly where that knowledge belongs.
    /// </remarks>
    private static IServiceCollection AddFormRegistry(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.TryAddScoped<IFormAttributeSource, TaxonomyFormAttributeSource>();
        services.TryAddScoped<IFormLocalizer, I18nFormLocalizer>();
        services.TryAddScoped<FormSchemaCompiler>();
        services.TryAddScoped<FormValidationEngine>();
        services.TryAddScoped<IFormRegistry, FormRegistry>();

        return services;
    }

    /// <summary>
    /// Registry 3 and its effect dispatcher.
    /// </summary>
    /// <remarks>
    /// The dispatcher is registered even with no handlers behind it, and that is the useful state
    /// rather than an incomplete one: <c>KnownEffectCodes</c> is empty, so a workflow that names an
    /// effect no handler implements is refused at publish time with the effect code in the message.
    /// A module adds <c>IWorkflowEffectHandler</c> implementations and they are discovered here.
    /// </remarks>
    private static IServiceCollection AddWorkflowEngine(this IServiceCollection services)
    {
        services.TryAddScoped<IWorkflowEffectDispatcher, WorkflowEffectDispatcher>();
        services.TryAddScoped<IWorkflowEngine, WorkflowEngine>();

        return services;
    }

    private static IServiceCollection AddPricingEngine(this IServiceCollection services)
    {
        services.TryAddScoped<IPricingEngine, PricingEngine>();
        return services;
    }

    /// <summary>
    /// Connects Redis, or does not, and either way the API starts.
    /// </summary>
    /// <remarks>
    /// <c>AbortOnConnectFail = false</c> is the whole trick: the multiplexer is created in a
    /// disconnected state and reconnects in the background, so a Redis that is down at boot is a
    /// degraded cache rather than a host that will not start. The registry cache and the permission
    /// cache both fall back to process memory, which is correct on one node and stale on a fleet —
    /// the right trade for an incident, and the wrong one to leave running, which is why the
    /// readiness probe reports it.
    /// </remarks>
    private static void AddRedis(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 2_000;
            options.ClientName = "autohub-api";
            return ConnectionMultiplexer.Connect(options);
        });

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "autohub:";
        });
    }
}
