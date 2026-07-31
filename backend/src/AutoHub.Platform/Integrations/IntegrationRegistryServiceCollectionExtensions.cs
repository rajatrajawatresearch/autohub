using AutoHub.Platform.Integrations.PaymentCollect;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Platform.Integrations;

public static class IntegrationRegistryServiceCollectionExtensions
{
    /// <summary>
    /// Registers registry 8. The host supplies <see cref="IIntegrationRegistryStore"/>.
    /// </summary>
    /// <remarks>
    /// Adapters are registered into a single <see cref="IIntegrationAdapter"/> collection rather
    /// than under their generic interfaces, because the invoker needs to enumerate every adapter in
    /// the build to find the one matching a routed provider. Registering them individually under
    /// <c>IIntegrationAdapter&lt;TRequest, TResponse&gt;</c> would make that enumeration impossible
    /// without knowing every capability's types in advance — which is the coupling the registry
    /// exists to remove.
    /// </remarks>
    public static IServiceCollection AddIntegrationRegistry(
        this IServiceCollection services,
        Action<CircuitBreakerOptions>? configureCircuitBreaker = null)
    {
        var breakerOptions = new CircuitBreakerOptions();
        configureCircuitBreaker?.Invoke(breakerOptions);

        services.TryAddSingleton(breakerOptions);
        services.TryAddSingleton<ICircuitBreakerStore>(sp => new InMemoryCircuitBreakerStore(
            sp.GetRequiredService<IClock>(),
            breakerOptions));

        services.TryAddSingleton<ISecretResolver, EnvironmentSecretResolver>();
        services.TryAddScoped<IIntegrationRouter, IntegrationRouter>();
        services.TryAddScoped<IIntegrationInvoker, IntegrationInvoker>();

        return services;
    }

    /// <summary>
    /// Adds the two simulating <c>payment.collect</c> adapters.
    /// </summary>
    /// <remarks>
    /// Separate call, and never invoked by <see cref="AddIntegrationRegistry"/>, so that wiring
    /// simulators into a host is always something someone typed on purpose. They refuse to run
    /// unless their provider row also says <c>simulate</c>, but a second lock on the door costs
    /// nothing.
    /// </remarks>
    public static IServiceCollection AddSimulatedPaymentAdapters(this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IIntegrationAdapter, RazorpayPaymentCollectAdapter>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IIntegrationAdapter, CashfreePaymentCollectAdapter>());

        return services;
    }
}
