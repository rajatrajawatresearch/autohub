using AutoHub.Platform.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Platform.Taxonomy;

public static class TaxonomyRegistration
{
    /// <summary>
    /// Registers the taxonomy registry. The host still supplies <see cref="ITaxonomyStore"/>.
    /// </summary>
    /// <remarks>
    /// Scoped, not singleton, because the store it reads through is a scoped EF <c>DbContext</c> and
    /// capturing one of those in a singleton is the classic way to end up with a context shared
    /// across threads. Both caches it writes to are singletons, so the snapshot is still loaded once
    /// per process rather than once per request — the scope is on the reader, not on the data.
    /// </remarks>
    public static IServiceCollection AddTaxonomyRegistry(
        this IServiceCollection services,
        Action<TaxonomyRegistryOptions>? configure = null)
    {
        var options = new TaxonomyRegistryOptions();
        configure?.Invoke(options);

        services.AddRegistryCache();
        services.TryAddSingleton(options);
        services.TryAddScoped<ITaxonomyRegistry, TaxonomyRegistry>();

        return services;
    }
}
