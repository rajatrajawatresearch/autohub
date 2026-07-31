using AutoHub.SharedKernel.Time;

namespace AutoHub.Platform.Integrations;

/// <summary>
/// Whether a provider is currently worth talking to.
/// </summary>
/// <remarks>
/// A gateway that is down does not fail fast — it fails <i>slowly</i>, holding every request open
/// for the full timeout. Thirty seconds times the request rate is how a single dead vendor consumes
/// every thread on every API node and takes the whole platform down with it. The breaker converts
/// that slow failure into an instant one, which is the difference between "payments are degraded"
/// and "AutoHub is offline".
/// </remarks>
public enum CircuitState
{
    /// <summary>Healthy. Calls go through.</summary>
    Closed = 1,

    /// <summary>Failing. Calls are skipped without being attempted.</summary>
    Open = 2,

    /// <summary>Probing. One call is allowed through to find out whether it has recovered.</summary>
    HalfOpen = 3,
}

public sealed record CircuitSnapshot(
    string Key,
    CircuitState State,
    int ConsecutiveFailures,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? NextProbeAt);

public interface ICircuitBreakerStore
{
    Task<CircuitSnapshot> GetAsync(string key, CancellationToken cancellationToken);

    Task RecordSuccessAsync(string key, CancellationToken cancellationToken);

    Task RecordFailureAsync(string key, CancellationToken cancellationToken);
}

public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Consecutive failures before the circuit opens.
    /// </summary>
    /// <remarks>
    /// Consecutive rather than a rate, because a rate needs a window and a window needs enough
    /// traffic to be meaningful. At 3am a capability might see four calls an hour, and a 50%
    /// error-rate threshold would either trip on one blip or never trip at all.
    /// </remarks>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// How long a circuit stays open before one probe is allowed through.
    /// </summary>
    /// <remarks>
    /// Long enough that a restarting vendor is not hammered, short enough that a two-minute blip
    /// does not cost twenty minutes of failover. Payment gateways publish recovery times in this
    /// range; longer would mean paying fallback pricing for far longer than necessary.
    /// </remarks>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Per-node breaker state.
/// </summary>
/// <remarks>
/// Per-node is a deliberate default, not just the easy one. Each node observes its own connectivity,
/// and the failures that matter most in practice are frequently local — one pod's DNS resolver, one
/// availability zone's route to a vendor, one node's exhausted socket pool. Sharing the state would
/// let a single sick node open the circuit for eleven healthy ones.
/// <para>
/// The cost is that each node has to learn independently, spending up to
/// <see cref="CircuitBreakerOptions.FailureThreshold"/> requests to discover what its neighbour
/// already knows. Where a vendor's outage is genuinely global and that cost is worth removing, a
/// Redis-backed implementation of <see cref="ICircuitBreakerStore"/> drops in without the router
/// changing.
/// </para>
/// </remarks>
public sealed class InMemoryCircuitBreakerStore : ICircuitBreakerStore
{
    private readonly Dictionary<string, CircuitSnapshot> _circuits = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();
    private readonly IClock _clock;
    private readonly CircuitBreakerOptions _options;

    public InMemoryCircuitBreakerStore(IClock clock, CircuitBreakerOptions options)
    {
        _clock = clock;
        _options = options;
    }

    public Task<CircuitSnapshot> GetAsync(string key, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Current(key, _clock.UtcNow));
        }
    }

    public Task RecordSuccessAsync(string key, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            // A single success closes the circuit outright rather than decrementing towards healthy.
            // Half-open exists to ask one question — "are you back?" — and a yes is a complete answer.
            _circuits[key] = new CircuitSnapshot(key, CircuitState.Closed, 0, null, null);
        }

        return Task.CompletedTask;
    }

    public Task RecordFailureAsync(string key, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        lock (_gate)
        {
            var current = Current(key, now);
            var failures = current.ConsecutiveFailures + 1;

            // A failed probe re-opens for the full duration rather than trying again immediately.
            // Otherwise a vendor that is down for an hour is probed continuously, which is the
            // hammering the breaker was installed to prevent.
            var shouldOpen = failures >= _options.FailureThreshold || current.State == CircuitState.HalfOpen;

            _circuits[key] = shouldOpen
                ? new CircuitSnapshot(key, CircuitState.Open, failures, now, now + _options.OpenDuration)
                : new CircuitSnapshot(key, CircuitState.Closed, failures, null, null);
        }

        return Task.CompletedTask;
    }

    private CircuitSnapshot Current(string key, DateTimeOffset now)
    {
        if (!_circuits.TryGetValue(key, out var snapshot))
        {
            return new CircuitSnapshot(key, CircuitState.Closed, 0, null, null);
        }

        if (snapshot.State == CircuitState.Open && snapshot.NextProbeAt <= now)
        {
            snapshot = snapshot with { State = CircuitState.HalfOpen };
            _circuits[key] = snapshot;
        }

        return snapshot;
    }
}
