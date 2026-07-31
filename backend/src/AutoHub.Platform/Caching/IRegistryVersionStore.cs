using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Caching.Distributed;

namespace AutoHub.Platform.Caching;

/// <summary>
/// The shared counter that tells every API node when a registry has changed underneath it.
/// </summary>
/// <remarks>
/// This is the smallest possible piece of shared state that can invalidate an unbounded amount of
/// per-node memory. Reads are constant-size and answered from a five-second in-process cache;
/// writes happen only when an operator publishes a registry change.
/// </remarks>
public interface IRegistryVersionStore
{
    Task<long> GetAsync(string registry, CancellationToken cancellationToken);

    /// <summary>Advances the generation. Returns the new value so the writer can log what it published.</summary>
    Task<long> BumpAsync(string registry, CancellationToken cancellationToken);
}

/// <summary>
/// Single-node version counter. Correct on one process, wrong on a fleet.
/// </summary>
/// <remarks>
/// Registered as the fallback so the API boots with no Redis — local development, integration tests,
/// and the first minutes of an incident where Redis is the thing that is down. On more than one node
/// it will let node B keep serving a stale snapshot after node A publishes, so
/// <see cref="DistributedRegistryVersionStore"/> is what runs in production.
/// </remarks>
public sealed class InMemoryRegistryVersionStore : IRegistryVersionStore
{
    private readonly ConcurrentDictionary<string, long> _versions = new(StringComparer.Ordinal);

    public Task<long> GetAsync(string registry, CancellationToken cancellationToken)
        => Task.FromResult(_versions.GetValueOrDefault(registry, 1L));

    public Task<long> BumpAsync(string registry, CancellationToken cancellationToken)
        => Task.FromResult(_versions.AddOrUpdate(registry, 2L, static (_, current) => current + 1));
}

/// <summary>
/// Version counter held in the distributed cache, so every node sees the same generation.
/// </summary>
/// <remarks>
/// <see cref="BumpAsync"/> is read-modify-write rather than an atomic increment, because doing it
/// atomically would mean taking a <c>StackExchange.Redis</c> dependency in
/// <c>AutoHub.Platform</c> — the one assembly that must stay free of infrastructure so the engines
/// remain unit-testable.
/// <para>
/// The race is bounded and self-correcting: two operators publishing in the same millisecond can
/// land on the same version number, which costs one extra stale read window (seconds) and is fixed
/// by the next publish. The alternative failure — a version that goes <i>backwards</i> — is
/// prevented by writing <c>max(observed, local) + 1</c>, so a node that read a stale value can never
/// stamp a generation older than one it has already served.
/// </para>
/// </remarks>
public sealed class DistributedRegistryVersionStore : IRegistryVersionStore
{
    private readonly IDistributedCache _cache;
    private readonly ConcurrentDictionary<string, long> _highWaterMark = new(StringComparer.Ordinal);
    private readonly string _keyPrefix;

    public DistributedRegistryVersionStore(IDistributedCache cache, string keyPrefix)
    {
        _cache = cache;
        _keyPrefix = keyPrefix;
    }

    public async Task<long> GetAsync(string registry, CancellationToken cancellationToken)
    {
        var stored = await ReadAsync(registry, cancellationToken);
        var local = _highWaterMark.GetValueOrDefault(registry, 1L);
        return Math.Max(stored, local);
    }

    public async Task<long> BumpAsync(string registry, CancellationToken cancellationToken)
    {
        var next = Math.Max(await GetAsync(registry, cancellationToken), 1L) + 1;

        _highWaterMark.AddOrUpdate(registry, next, (_, current) => Math.Max(current, next));

        await _cache.SetStringAsync(
            VersionKey(registry),
            next.ToString(CultureInfo.InvariantCulture),
            // No expiry. A version that expires resets the generation to 1 and makes every node
            // believe its stale snapshot is current again — the exact failure this counter exists
            // to prevent.
            new DistributedCacheEntryOptions(),
            cancellationToken);

        return next;
    }

    private async Task<long> ReadAsync(string registry, CancellationToken cancellationToken)
    {
        var raw = await _cache.GetStringAsync(VersionKey(registry), cancellationToken);

        return long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var version)
            ? version
            : 1L;
    }

    private string VersionKey(string registry) => $"{_keyPrefix}:version:{registry}";
}
