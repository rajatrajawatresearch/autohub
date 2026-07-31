using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Platform.Localization;

public static class LocalizationRegistryServiceCollectionExtensions
{
    /// <summary>
    /// Registers registry 9. The host supplies <see cref="II18nResourceStore"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="I18nRegistry"/> is registered once and exposed under both of its interfaces rather
    /// than twice, so the notification registry's locale chain is resolved by the same instance —
    /// and therefore the same cache entry — that serves the client bundles.
    /// <para>
    /// <b>Scoped, not singleton</b>, for the same reason the taxonomy and geography registries are:
    /// it reads through <see cref="II18nResourceStore"/>, which the persistence layer implements over
    /// a scoped EF <c>DbContext</c>. A singleton holding one of those shares a change tracker across
    /// threads, and .NET's own scope validation refuses to build the container at all — which is the
    /// good outcome, because the alternative is intermittent corruption under load.
    /// </para>
    /// <para>
    /// The bundles themselves still live in the singleton <c>IRegistryCache</c>, so a scoped reader
    /// costs one object per request and the data is still loaded once per process.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddLocalizationRegistry(
        this IServiceCollection services,
        Action<I18nOptions>? configure = null)
    {
        services.AddOptions<I18nOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddScoped<I18nRegistry>();
        services.TryAddScoped<II18nRegistry>(static sp => sp.GetRequiredService<I18nRegistry>());
        services.TryAddScoped<ILocaleChainResolver>(static sp => sp.GetRequiredService<I18nRegistry>());

        return services;
    }
}
