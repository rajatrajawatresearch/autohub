using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Platform.Features;

public static class FeatureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the feature and module registry.
    /// </summary>
    /// <remarks>
    /// <see cref="IFeatureRegistry"/> is scoped so its per-request memo holds one consistent answer
    /// for the life of a request. <see cref="IFeatureStore"/> comes from the persistence layer, and
    /// the Redis <see cref="IFeatureCache"/> replaces the in-memory fallback below by being
    /// registered first — everything here is a <c>TryAdd</c>.
    /// </remarks>
    public static IServiceCollection AddPlatformFeatures(
        this IServiceCollection services,
        Action<FeatureOptions>? configure = null)
    {
        var options = new FeatureOptions();
        configure?.Invoke(options);

        services.AddMemoryCache();
        services.TryAddSingleton(options);
        services.TryAddSingleton<IFeatureCache, InMemoryFeatureCache>();
        services.TryAddSingleton<IFeatureCacheInvalidator, FeatureCacheInvalidator>();
        services.TryAddScoped<IFeatureRegistry, FeatureRegistry>();

        return services;
    }
}
