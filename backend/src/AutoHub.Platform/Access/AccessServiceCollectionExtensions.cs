using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Platform.Access;

public static class AccessServiceCollectionExtensions
{
    /// <summary>
    /// Registers the RBAC registry.
    /// </summary>
    /// <remarks>
    /// <see cref="IPermissionResolver"/> is <b>scoped</b>, and that lifetime is load-bearing: its
    /// per-request memo is what guarantees one request sees one consistent permission set. Registered
    /// as a singleton it would cache every user forever; as a transient it would lose the memo and
    /// hit Redis on every check.
    /// <para>
    /// <see cref="IAccessStore"/> is deliberately not registered here — it is implemented in the
    /// persistence layer, and Platform must never reference EF Core. The Redis
    /// <see cref="IAccessCache"/> is registered the same way; the in-memory fallback below steps
    /// aside for it because every registration is a <c>TryAdd</c>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddPlatformAccess(
        this IServiceCollection services,
        Action<AccessOptions>? configure = null)
    {
        var options = new AccessOptions();
        configure?.Invoke(options);

        services.AddMemoryCache();
        services.TryAddSingleton(options);
        services.TryAddSingleton<IAccessCache, InMemoryAccessCache>();
        services.TryAddSingleton<IAccessCacheInvalidator, AccessCacheInvalidator>();
        services.TryAddSingleton<PermissionCatalog>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPermissionContributor, PlatformPermissionContributor>());
        services.TryAddScoped<IPermissionResolver, PermissionResolver>();

        return services;
    }
}
