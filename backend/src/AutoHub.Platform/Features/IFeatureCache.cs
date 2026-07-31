using AutoHub.Platform.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace AutoHub.Platform.Features;

/// <summary>
/// Caches the whole feature registry under one key.
/// </summary>
/// <remarks>
/// Simpler than <see cref="Access.IAccessCache"/> and for a specific reason: what is cached here is
/// the registry <i>data</i>, which is global, not the <i>evaluation</i>, which is per user. One key
/// means invalidation is one delete, with no generation counter and no keyspace scan.
/// <para>
/// Caching per-user evaluations would multiply the invalidation surface by the number of active
/// users to save a handful of JsonLogic applies over a few hundred rows — a worse cache with a
/// larger blast radius. The evaluator memoises per request instead.
/// </para>
/// <para>
/// As everywhere in this codebase, the TTL is a backstop against a missed invalidation, not the
/// mechanism: <see cref="IFeatureCacheInvalidator"/> is called by the admin write that changed the
/// flag, so a kill switch takes effect on the next request rather than on the next expiry.
/// </para>
/// </remarks>
public interface IFeatureCache
{
    Task<FeatureRegistryData?> GetAsync(CancellationToken cancellationToken);

    Task SetAsync(FeatureRegistryData data, TimeSpan timeToLive, CancellationToken cancellationToken);

    Task InvalidateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The invalidation path for registry 6.
/// </summary>
/// <remarks>
/// Call in the same unit of work as any write to a module, a flag, or a flag rule. A flag exists so
/// somebody can turn a broken feature off during an incident; a flag that takes a cache window to
/// take effect is a flag that does not do the one thing it was built for.
/// </remarks>
public interface IFeatureCacheInvalidator
{
    Task InvalidateAsync(CancellationToken cancellationToken);
}

/// <summary>Cache key layout. A technical constant — see <c>RegistryNames</c>.</summary>
public static class FeatureCacheKeys
{
    public static string Registry => $"{RegistryNames.Features}:registry";
}

/// <summary>
/// A single-node fallback so the API boots without Redis.
/// </summary>
/// <remarks>
/// Correct in one process and wrong in two: an invalidation on node A leaves node B serving the old
/// registry until its TTL runs out. Registered with <c>TryAdd</c> so the Redis adapter replaces it,
/// and useful in tests, where a real cache would only hide ordering bugs.
/// </remarks>
public sealed class InMemoryFeatureCache : IFeatureCache
{
    private readonly IMemoryCache _cache;

    public InMemoryFeatureCache(IMemoryCache cache) => _cache = cache;

    public Task<FeatureRegistryData?> GetAsync(CancellationToken cancellationToken)
        => Task.FromResult(_cache.Get<FeatureRegistryData>(FeatureCacheKeys.Registry));

    public Task SetAsync(FeatureRegistryData data, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        _cache.Set(FeatureCacheKeys.Registry, data, timeToLive);
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(CancellationToken cancellationToken)
    {
        _cache.Remove(FeatureCacheKeys.Registry);
        return Task.CompletedTask;
    }
}

/// <summary>Invalidates whichever <see cref="IFeatureCache"/> is registered.</summary>
public sealed class FeatureCacheInvalidator : IFeatureCacheInvalidator
{
    private readonly IFeatureCache _cache;

    public FeatureCacheInvalidator(IFeatureCache cache) => _cache = cache;

    public Task InvalidateAsync(CancellationToken cancellationToken) => _cache.InvalidateAsync(cancellationToken);
}
