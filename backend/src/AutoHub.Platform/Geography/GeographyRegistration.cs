using AutoHub.Platform.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Platform.Geography;

public static class GeographyRegistration
{
    /// <summary>
    /// Registers the geography registry. The host still supplies <see cref="IGeographyStore"/>.
    /// </summary>
    /// <remarks>
    /// Scoped for the same reason the taxonomy registry is: it reads through a scoped EF context.
    /// The snapshot itself lives in the singleton memory cache, so it is loaded once per process.
    /// </remarks>
    public static IServiceCollection AddGeographyRegistry(
        this IServiceCollection services,
        Action<GeographyRegistryOptions>? configure = null)
    {
        var options = new GeographyRegistryOptions();
        configure?.Invoke(options);

        services.AddRegistryCache();
        services.TryAddSingleton(options);
        services.TryAddScoped<IGeographyRegistry, GeographyRegistry>();

        return services;
    }
}
