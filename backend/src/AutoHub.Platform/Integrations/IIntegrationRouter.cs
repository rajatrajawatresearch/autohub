using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Integrations;

/// <summary>
/// Decides who serves a capability, in what order, right now. Registry 8's read side.
/// </summary>
public interface IIntegrationRouter
{
    /// <summary>
    /// The ordered chain to try: primary first, then fallbacks, with dead providers already removed.
    /// </summary>
    Task<Result<IReadOnlyList<ResolvedProvider>>> ResolveAsync(
        string capabilityCode,
        IntegrationContext context,
        CancellationToken cancellationToken);

    /// <summary>Feeds the circuit breaker. Called by the invoker after every attempt.</summary>
    Task RecordOutcomeAsync(string providerCode, string capabilityCode, bool succeeded, CancellationToken cancellationToken);
}

public sealed record IntegrationContext
{
    /// <summary>
    /// The facts routes match on: <c>{"city":"pune","amountMinor":250000,"method":"upi",
    /// "party":{"tier":"gold"}}</c>. Shape is documented per capability.
    /// </summary>
    public JsonNode? Facts { get; init; }

    /// <summary>
    /// The value that keeps a retry on the same provider as its first attempt.
    /// </summary>
    /// <remarks>
    /// Use the caller's <c>Idempotency-Key</c>. Canary splitting hashes this, so a retried payment
    /// deterministically re-selects the provider that already has the charge — which is what stops a
    /// timeout from becoming a double debit.
    /// </remarks>
    public string? AffinityKey { get; init; }

    /// <summary>
    /// Which environment's providers to route to. Defaults to live; tests and the admin's "test this
    /// provider" button pass sandbox.
    /// </summary>
    public IntegrationEnvironment Environment { get; init; } = IntegrationEnvironment.Live;
}

/// <summary>One link in the chain, with everything an adapter needs except the secret.</summary>
public sealed record ResolvedProvider
{
    public required string CapabilityCode { get; init; }

    public required string ProviderCode { get; init; }

    /// <summary>Zero-based position in the chain. Zero is the primary; anything above is a fallback.</summary>
    public required int Order { get; init; }

    public required string CredentialRef { get; init; }

    public JsonNode? Config { get; init; }

    public required TimeSpan Timeout { get; init; }

    public required int MaxAttempts { get; init; }

    /// <summary>
    /// The breaker's view of this provider. <see cref="CircuitState.HalfOpen"/> means this call is
    /// the probe, which callers may want to log differently — an unexpected failure here is
    /// information, not a surprise.
    /// </summary>
    public required CircuitState CircuitState { get; init; }
}

/// <summary>Loads providers and routes. Implemented in persistence, read through the registry cache.</summary>
public interface IIntegrationRegistryStore
{
    Task<IReadOnlyList<ProviderRoute>> GetRoutesAsync(string capabilityCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<IntegrationProvider>> GetProvidersAsync(string capabilityCode, CancellationToken cancellationToken);
}

/// <summary>The cacheable projection of a route plus the provider it points at.</summary>
/// <remarks>
/// Joined at cache-build time rather than at call time. Routing runs on the payment path, where the
/// difference between one cache read and two lookups plus a join is latency a customer feels while
/// staring at a spinner with their card details on screen.
/// </remarks>
public sealed record RouteSnapshot
{
    public required string CapabilityCode { get; init; }

    public required string ProviderCode { get; init; }

    public required int Priority { get; init; }

    public string? ConditionJson { get; init; }

    public int? TrafficPercent { get; init; }

    public DateTimeOffset? EffectiveFrom { get; init; }

    public DateTimeOffset? EffectiveTo { get; init; }

    public required IntegrationEnvironment Environment { get; init; }

    public required string CredentialRef { get; init; }

    public string? ConfigJson { get; init; }

    public required int TimeoutSeconds { get; init; }

    public required int MaxAttempts { get; init; }

    public bool IsEffectiveAt(DateTimeOffset at)
        => (EffectiveFrom is null || at >= EffectiveFrom)
           && (EffectiveTo is null || at < EffectiveTo);
}
