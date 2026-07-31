using AutoHub.Platform.Integrations;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Loads the routes and providers for one capability.
/// </summary>
/// <remarks>
/// Both reads are untracked and both are cached by the router, because this sits on the payment
/// path: the difference between one cache read and two queries plus a join is latency a customer
/// feels while staring at a spinner with their card details on screen.
/// <para>
/// Inactive providers are returned. The router needs them to explain a chain that has gone empty —
/// "Razorpay is configured but disabled" is a different incident from "Razorpay was never
/// configured", and an operator at 2am needs to be able to tell them apart.
/// </para>
/// </remarks>
public sealed class EfIntegrationRegistryStore : IIntegrationRegistryStore
{
    private readonly AutoHubDbContext _context;

    public EfIntegrationRegistryStore(AutoHubDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProviderRoute>> GetRoutesAsync(
        string capabilityCode,
        CancellationToken cancellationToken)
        => await _context.ProviderRoutes
            .AsNoTracking()
            .Where(route => route.CapabilityCode == capabilityCode)
            // Lower priority is tried first, so the chain arrives in the order the invoker walks it.
            .OrderBy(route => route.Priority)
            .ThenBy(route => route.ProviderCode)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IntegrationProvider>> GetProvidersAsync(
        string capabilityCode,
        CancellationToken cancellationToken)
        => await _context.IntegrationProviders
            .AsNoTracking()
            .Where(provider => provider.CapabilityCode == capabilityCode)
            .OrderBy(provider => provider.Code)
            .ToListAsync(cancellationToken);
}
