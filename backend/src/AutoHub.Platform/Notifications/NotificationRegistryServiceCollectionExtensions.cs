using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Platform.Notifications;

public static class NotificationRegistryServiceCollectionExtensions
{
    /// <summary>
    /// Registers registry 7. The host supplies <see cref="INotificationRegistryStore"/>; modules add
    /// their own <see cref="INotificationAudienceResolver"/>.
    /// </summary>
    /// <remarks>
    /// The catch-all audience resolver is appended with <c>TryAddEnumerable</c> so it is registered
    /// exactly once no matter how many modules call this, and so a module's own resolver — which
    /// declares a prefix and therefore sorts ahead of it — always wins.
    /// </remarks>
    public static IServiceCollection AddNotificationRegistry(
        this IServiceCollection services,
        Action<NotificationOptions>? configure = null)
    {
        services.AddOptions<NotificationOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<INotificationSuppressionStore, InMemoryNotificationSuppressionStore>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<INotificationAudienceResolver, EmptyNotificationAudienceResolver>());

        services.TryAddScoped<INotificationRegistry, NotificationRegistry>();

        return services;
    }
}
