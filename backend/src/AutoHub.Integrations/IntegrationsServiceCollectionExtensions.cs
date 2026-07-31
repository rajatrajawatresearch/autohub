using AutoHub.Integrations.Search;
using AutoHub.Integrations.Storage;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Integrations;

public static class IntegrationsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the fallback adapters for the infrastructure that is not always there.
    /// </summary>
    /// <remarks>
    /// Every registration is a <c>TryAdd</c>, so a host that has OpenSearch or S3 registers its own
    /// first and these never construct. That ordering is the whole mechanism: the production wiring
    /// is a line added above this call, not a flag threaded through it, and the fallback cannot be
    /// left switched on by an environment variable somebody forgot.
    /// </remarks>
    public static IServiceCollection AddIntegrationFallbacks(this IServiceCollection services)
    {
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddSingleton<ISearchIndex, InMemorySearchIndex>();
        services.TryAddSingleton<IObjectStore>(sp => new InMemoryObjectStore(sp.GetRequiredService<IClock>()));

        return services;
    }
}
