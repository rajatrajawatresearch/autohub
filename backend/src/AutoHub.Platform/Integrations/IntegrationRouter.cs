using System.Text.Json.Nodes;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Expressions;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Integrations;

/// <summary>
/// Builds the provider chain for a capability and keeps dead providers out of it.
/// </summary>
public sealed class IntegrationRouter : IIntegrationRouter
{
    private readonly IIntegrationRegistryStore _store;
    private readonly ICircuitBreakerStore _circuits;
    private readonly IRegistryCache _cache;
    private readonly IClock _clock;
    private readonly ILogger<IntegrationRouter> _logger;

    public IntegrationRouter(
        IIntegrationRegistryStore store,
        ICircuitBreakerStore circuits,
        IRegistryCache cache,
        IClock clock,
        ILogger<IntegrationRouter> logger)
    {
        _store = store;
        _circuits = circuits;
        _cache = cache;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<ResolvedProvider>>> ResolveAsync(
        string capabilityCode,
        IntegrationContext context,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var routes = await GetRoutesAsync(capabilityCode, cancellationToken);

        var candidates = routes
            .Where(r => r.Environment == context.Environment)
            .Where(r => r.IsEffectiveAt(now))
            .Where(r => Matches(r, context))
            .Where(r => InCanarySlice(r, context))
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.ProviderCode, StringComparer.Ordinal)
            .ToList();

        if (candidates.Count == 0)
        {
            // Distinguishable from "everything is down": there is no configuration at all, which is
            // an operator problem rather than a vendor problem, and the two need different alerts.
            _logger.LogError(
                "No integration route matches {Capability} in {Environment}",
                capabilityCode, context.Environment);

            return Error.Unexpected(
                "integration.no_route",
                $"No provider is configured for '{capabilityCode}'.")
                .With("capability", capabilityCode);
        }

        // The breaker is read once per candidate and the result carried forward. Re-reading it while
        // building the response would double the lookups on the payment path for a value that cannot
        // meaningfully have changed in the microseconds between.
        var inspected = new List<(RouteSnapshot Route, CircuitSnapshot Circuit)>(candidates.Count);

        foreach (var candidate in candidates)
        {
            inspected.Add((
                candidate,
                await _circuits.GetAsync(CircuitKey(candidate.ProviderCode, capabilityCode), cancellationToken)));
        }

        var chain = inspected.Where(x => x.Circuit.State != CircuitState.Open).ToList();

        // Every provider open at once. Returning nothing would mean failing 100% of calls for as
        // long as the breakers stay open — including the moment a vendor comes back — so the one
        // closest to its probe time is admitted as a half-open attempt. A slow call that might
        // succeed beats a fast failure that certainly will not.
        if (chain.Count == 0)
        {
            _logger.LogWarning(
                "Every provider for {Capability} is circuit-open; probing the one next due",
                capabilityCode);

            chain.Add(inspected
                .OrderBy(x => x.Circuit.NextProbeAt ?? DateTimeOffset.MaxValue)
                .First());
        }

        return chain
            .Select((x, index) => new ResolvedProvider
            {
                CapabilityCode = capabilityCode,
                ProviderCode = x.Route.ProviderCode,
                Order = index,
                CredentialRef = x.Route.CredentialRef,
                Config = string.IsNullOrWhiteSpace(x.Route.ConfigJson)
                    ? null
                    : JsonNode.Parse(x.Route.ConfigJson),
                Timeout = TimeSpan.FromSeconds(x.Route.TimeoutSeconds),
                MaxAttempts = x.Route.MaxAttempts,
                CircuitState = x.Circuit.State,
            })
            .ToList();
    }

    public Task RecordOutcomeAsync(
        string providerCode,
        string capabilityCode,
        bool succeeded,
        CancellationToken cancellationToken)
        => succeeded
            ? _circuits.RecordSuccessAsync(CircuitKey(providerCode, capabilityCode), cancellationToken)
            : _circuits.RecordFailureAsync(CircuitKey(providerCode, capabilityCode), cancellationToken);

    private async Task<IReadOnlyList<RouteSnapshot>> GetRoutesAsync(
        string capabilityCode,
        CancellationToken cancellationToken)
        => await _cache.GetOrLoadRequiredAsync<IReadOnlyList<RouteSnapshot>>(
            RegistryNames.Integrations,
            $"routes:{capabilityCode}",
            async token =>
            {
                var routes = await _store.GetRoutesAsync(capabilityCode, token);
                var providers = await _store.GetProvidersAsync(capabilityCode, token);

                var byCode = providers
                    .Where(p => p.IsActive)
                    .ToDictionary(p => p.Code, StringComparer.Ordinal);

                var snapshots = new List<RouteSnapshot>();

                foreach (var route in routes.Where(r => r.IsActive))
                {
                    if (!byCode.TryGetValue(route.ProviderCode, out var provider))
                    {
                        // A route pointing at a deactivated provider is how a vendor is switched off:
                        // deactivate the provider once, and every route referencing it drops out
                        // without anyone having to find them all.
                        continue;
                    }

                    snapshots.Add(new RouteSnapshot
                    {
                        CapabilityCode = capabilityCode,
                        ProviderCode = route.ProviderCode,
                        Priority = route.Priority,
                        ConditionJson = route.ConditionJson,
                        TrafficPercent = route.TrafficPercent,
                        EffectiveFrom = route.EffectiveFrom,
                        EffectiveTo = route.EffectiveTo,
                        Environment = provider.Environment,
                        CredentialRef = provider.CredentialRef,
                        ConfigJson = provider.ConfigJson,
                        TimeoutSeconds = provider.TimeoutSeconds,
                        MaxAttempts = provider.MaxAttempts,
                    });
                }

                return snapshots;
            },
            cancellationToken);

    private static bool Matches(RouteSnapshot route, IntegrationContext context)
    {
        var condition = ConditionExpression.FromJson(route.ConditionJson);

        // An unparseable condition must not match. Silently routing every payment through a provider
        // whose condition nobody could parse is worse than routing none through it — the fallback
        // still works, and the missing provider is visible.
        return condition.IsSuccess && condition.Value.Evaluate(context.Facts?.DeepClone() ?? new JsonObject());
    }

    /// <summary>
    /// Deterministic canary slicing.
    /// </summary>
    /// <remarks>
    /// The bucket comes from a stable hash of the affinity key, so the same payment attempt always
    /// lands in the same bucket — on the first try and on every retry after it. Using
    /// <c>Random.Shared</c> here would be the single most expensive line in the codebase: a retried
    /// charge would re-roll, land on the other gateway, and debit the customer twice.
    /// <para>
    /// With no affinity key there is nothing stable to hash, so the route is taken rather than
    /// gambled on. A caller that has not supplied an idempotency key should not be silently entered
    /// into an experiment.
    /// </para>
    /// </remarks>
    private static bool InCanarySlice(RouteSnapshot route, IntegrationContext context)
    {
        if (route.TrafficPercent is not { } percent || percent >= 100)
        {
            return true;
        }

        if (string.IsNullOrEmpty(context.AffinityKey))
        {
            return true;
        }

        var bucket = (int)(StableHash(context.AffinityKey) % 100);
        return bucket < percent;
    }

    /// <summary>
    /// FNV-1a. Chosen because <see cref="string.GetHashCode()"/> is randomised per process in .NET,
    /// which would put the same payment in a different bucket on every node and on every restart —
    /// exactly the non-determinism this function exists to avoid.
    /// </summary>
    private static uint StableHash(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;

        foreach (var c in value)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }

    private static string CircuitKey(string providerCode, string capabilityCode)
        => $"{capabilityCode}:{providerCode}";
}
