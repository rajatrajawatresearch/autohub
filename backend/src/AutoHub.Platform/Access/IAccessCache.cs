using AutoHub.Platform.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace AutoHub.Platform.Access;

/// <summary>
/// The distributed store for resolved permission sets, and the generation counter that expires them.
/// </summary>
/// <remarks>
/// Two operations, because permission invalidation has two shapes — see
/// <see cref="IAccessCacheInvalidator"/>. Keys embed the generation, so bumping it makes every
/// existing key unreachable in one atomic increment without a <c>SCAN</c> over the keyspace, which
/// on a production Redis is a request nobody wants to be responsible for.
/// <para>
/// Orphaned keys from previous generations are never read again and are reclaimed by their TTL. That
/// is the only job the TTL has here.
/// </para>
/// </remarks>
public interface IAccessCache
{
    /// <summary>The current generation. Part of every key.</summary>
    Task<long> GetGenerationAsync(CancellationToken cancellationToken);

    /// <summary>Atomically advances the generation, orphaning every cached set. Returns the new value.</summary>
    Task<long> BumpGenerationAsync(CancellationToken cancellationToken);

    Task<PermissionSetSnapshot?> GetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(
        string key,
        PermissionSetSnapshot snapshot,
        TimeSpan timeToLive,
        CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);
}

/// <summary>Cache key layout. Technical constants — no business meaning, and a typo splits the cache.</summary>
public static class AccessCacheKeys
{
    public static string ForUser(long generation, Guid userId)
        => $"{RegistryNames.Rbac}:perms:g{generation}:{userId:N}";
}

/// <summary>
/// A single-node fallback so the API boots without Redis.
/// </summary>
/// <remarks>
/// Correct for one process and wrong for two, and that is the whole point of naming it explicitly:
/// a generation bump on node A never reaches node B, so a revoked assignment would keep working on
/// every node that did not serve the revoke. It is registered with <c>TryAdd</c> so the Redis
/// adapter simply takes its place, and it exists so that local development and unit tests do not
/// require a running Redis to exercise the resolver.
/// </remarks>
public sealed class InMemoryAccessCache : IAccessCache
{
    private readonly IMemoryCache _cache;
    private long _generation;

    public InMemoryAccessCache(IMemoryCache cache) => _cache = cache;

    public Task<long> GetGenerationAsync(CancellationToken cancellationToken)
        => Task.FromResult(Interlocked.Read(ref _generation));

    public Task<long> BumpGenerationAsync(CancellationToken cancellationToken)
        => Task.FromResult(Interlocked.Increment(ref _generation));

    public Task<PermissionSetSnapshot?> GetAsync(string key, CancellationToken cancellationToken)
        => Task.FromResult(_cache.Get<PermissionSetSnapshot>(key));

    public Task SetAsync(
        string key,
        PermissionSetSnapshot snapshot,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        _cache.Set(key, snapshot, timeToLive);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}

/// <summary>The concrete invalidation path. Injected into every admin handler that edits access.</summary>
public sealed class AccessCacheInvalidator : IAccessCacheInvalidator
{
    private readonly IAccessCache _cache;

    public AccessCacheInvalidator(IAccessCache cache) => _cache = cache;

    public async Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Only the current generation needs removing: older generations are already unreachable
        // because the generation is part of the key.
        var generation = await _cache.GetGenerationAsync(cancellationToken);
        await _cache.RemoveAsync(AccessCacheKeys.ForUser(generation, userId), cancellationToken);
    }

    public Task InvalidateEveryoneAsync(CancellationToken cancellationToken)
        => _cache.BumpGenerationAsync(cancellationToken);
}
