using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoHub.Platform.Caching;

public static class RegistryCacheServiceCollectionExtensions
{
    /// <summary>
    /// Registers the two-level registry cache shared by all ten registries.
    /// </summary>
    /// <remarks>
    /// The distributed tier is resolved optionally on purpose. The host decides whether Redis exists;
    /// if it does not — a developer laptop, a CI run, an incident — the cache silently becomes
    /// memory-only and the API still serves. A registry cache that refuses to construct without Redis
    /// turns a cache outage into a total outage.
    /// </remarks>
    public static IServiceCollection AddRegistryCache(
        this IServiceCollection services,
        Action<RegistryCacheOptions>? configure = null)
    {
        services.AddMemoryCache();
        services.AddOptions<RegistryCacheOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IRegistryVersionStore>(static provider =>
        {
            var options = provider.GetRequiredService<IOptions<RegistryCacheOptions>>().Value;
            var distributed = provider.GetService<IDistributedCache>();

            return distributed is not null && options.UseDistributedTier
                ? new DistributedRegistryVersionStore(distributed, options.KeyPrefix)
                : new InMemoryRegistryVersionStore();
        });

        services.TryAddSingleton<IRegistryCache>(static provider => new TwoLevelRegistryCache(
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<IRegistryVersionStore>(),
            provider.GetRequiredService<IOptions<RegistryCacheOptions>>(),
            provider.GetRequiredService<ILogger<TwoLevelRegistryCache>>(),
            provider.GetService<IDistributedCache>()));

        return services;
    }
}
