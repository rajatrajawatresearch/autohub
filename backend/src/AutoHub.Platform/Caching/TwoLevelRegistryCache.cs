using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoHub.Platform.Caching;

/// <summary>
/// In-process memory in front of Redis, with version-stamped keys.
/// </summary>
/// <remarks>
/// <b>Why the version is in the key rather than an eviction being broadcast.</b>
/// <para>
/// The obvious design is key-by-key eviction: an admin edits a commission slab, the write path
/// deletes <c>pricing:auction.win</c> from Redis, done. It works perfectly on one node and fails
/// quietly on a fleet, because there is no way to reach into the <c>MemoryCache</c> of the other
/// eleven pods. You can publish an eviction message on a Redis channel and hope every subscriber is
/// connected; a pod that was mid-restart, GC-paused, or briefly partitioned misses it and then
/// serves the old commission for as long as its TTL allows — indefinitely if the entry is re-read
/// and re-extended. Nobody notices until a dealer is billed at last month's rate.
/// </para>
/// <para>
/// So we invert it. Instead of pushing invalidation out, we make every reader pull one tiny fact:
/// the registry's generation counter. Cache keys embed it —
/// <c>autohub:registry:pricing:v41:auction.win</c> — so publishing v42 does not evict anything; it
/// makes every v41 key unreachable at once, on every node, without any node needing to be told.
/// Stale entries then age out on their own TTL and cost nothing but a little memory in the meantime.
/// </para>
/// <para>
/// The only shared state is a counter, the only staleness is
/// <see cref="RegistryCacheOptions.VersionTtl"/>, and a node that cannot reach Redis at all degrades
/// to "slightly stale" rather than "wrong forever" — which is the correct failure direction for
/// configuration data.
/// </para>
/// </remarks>
public sealed class TwoLevelRegistryCache : IRegistryCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IMemoryCache _memory;
    private readonly IDistributedCache? _distributed;
    private readonly IRegistryVersionStore _versions;
    private readonly RegistryCacheOptions _options;
    private readonly ILogger<TwoLevelRegistryCache> _logger;

    /// <summary>
    /// One in-flight load per key. Twenty pods restarting after a deploy will each miss on the same
    /// taxonomy tree; without this, each of the fifty concurrent requests on a pod issues its own
    /// query and the stampede lands on the database at the exact moment it is least welcome.
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<Task<CacheSlot>>> _inFlight =
        new(StringComparer.Ordinal);

    public TwoLevelRegistryCache(
        IMemoryCache memory,
        IRegistryVersionStore versions,
        IOptions<RegistryCacheOptions> options,
        ILogger<TwoLevelRegistryCache> logger,
        IDistributedCache? distributed = null)
    {
        _memory = memory;
        _versions = versions;
        _options = options.Value;
        _logger = logger;
        _distributed = distributed;
    }

    public async Task<T?> GetOrLoadAsync<T>(
        string registry,
        string key,
        Func<CancellationToken, Task<T?>> loader,
        CancellationToken cancellationToken)
        where T : class
    {
        var version = await GetVersionAsync(registry, cancellationToken);
        var cacheKey = BuildKey(registry, version.Value, key);

        if (_memory.TryGetValue<CacheSlot>(cacheKey, out var hit) && hit is not null)
        {
            return hit.Value as T;
        }

        // The token of whichever caller wins the race drives the shared load. If that caller hangs
        // up mid-flight the others see the cancellation and retry — acceptable, because a registry
        // load is a single indexed query and the retry costs milliseconds. The alternative, an
        // unattached token, is a load nobody can cancel.
        var lazy = _inFlight.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task<CacheSlot>>(
                () => LoadAsync(registry, cacheKey, loader, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var slot = await lazy.Value;
            return slot.Value as T;
        }
        finally
        {
            _inFlight.TryRemove(cacheKey, out _);
        }
    }

    public async Task<RegistryVersion> GetVersionAsync(string registry, CancellationToken cancellationToken)
    {
        var versionKey = $"{_options.KeyPrefix}:__version:{registry}";

        if (_memory.TryGetValue<long>(versionKey, out var cached) && cached > 0)
        {
            return new RegistryVersion(registry, cached);
        }

        var version = await _versions.GetAsync(registry, cancellationToken);

        _memory.Set(versionKey, version, _options.VersionTtl);
        return new RegistryVersion(registry, version);
    }

    public async Task<RegistryVersion> InvalidateAsync(string registry, CancellationToken cancellationToken)
    {
        var version = await _versions.BumpAsync(registry, cancellationToken);

        // Drop this node's memoised generation so the node that performed the write serves the new
        // snapshot on its very next read. Every other node picks it up within VersionTtl.
        _memory.Remove($"{_options.KeyPrefix}:__version:{registry}");

        _logger.LogInformation("Registry {Registry} invalidated; now at v{Version}", registry, version);

        return new RegistryVersion(registry, version);
    }

    private async Task<CacheSlot> LoadAsync<T>(
        string registry,
        string cacheKey,
        Func<CancellationToken, Task<T?>> loader,
        CancellationToken cancellationToken)
        where T : class
    {
        var fromDistributed = await ReadDistributedAsync<T>(cacheKey, cancellationToken);
        if (fromDistributed is not null)
        {
            StoreInMemory(cacheKey, fromDistributed);
            return fromDistributed;
        }

        var loaded = await loader(cancellationToken);
        var slot = new CacheSlot(loaded);

        StoreInMemory(cacheKey, slot);
        await WriteDistributedAsync(registry, cacheKey, loaded, cancellationToken);

        return slot;
    }

    private void StoreInMemory(string cacheKey, CacheSlot slot)
        => _memory.Set(cacheKey, slot, slot.Value is null ? _options.NegativeTtl : _options.MemoryTtl);

    private async Task<CacheSlot?> ReadDistributedAsync<T>(string cacheKey, CancellationToken cancellationToken)
        where T : class
    {
        if (_distributed is null || !_options.UseDistributedTier)
        {
            return null;
        }

        try
        {
            var bytes = await _distributed.GetAsync(cacheKey, cancellationToken);
            if (bytes is null)
            {
                return null;
            }

            // A stored "null" is a cached miss, and is distinguishable from an absent key: Redis
            // returns no bytes for the latter and four bytes for the former.
            return new CacheSlot(JsonSerializer.Deserialize<T>(bytes, SerializerOptions));
        }
        catch (JsonException ex)
        {
            // A payload written by a previous deploy whose shape has since changed. Treating it as a
            // miss self-heals on the next write; throwing would take the endpoint down until someone
            // flushed Redis by hand.
            _logger.LogWarning(ex, "Discarding unreadable registry cache entry {CacheKey}", cacheKey);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Distributed registry cache unavailable reading {CacheKey}", cacheKey);
            return null;
        }
    }

    private async Task WriteDistributedAsync<T>(
        string registry,
        string cacheKey,
        T? value,
        CancellationToken cancellationToken)
        where T : class
    {
        if (_distributed is null || !_options.UseDistributedTier)
        {
            return;
        }

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

            await _distributed.SetAsync(
                cacheKey,
                bytes,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        value is null ? _options.NegativeTtl : _options.DistributedTtl,
                },
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Redis being down must degrade this to a memory-only cache, never fail the request.
            // The registries are on the read path of every page in the product.
            _logger.LogWarning(ex, "Could not write {Registry} registry cache entry to Redis", registry);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Registry {Registry} payload is not serialisable and will not be shared", registry);
        }
    }

    private string BuildKey(string registry, long version, string key)
        => $"{_options.KeyPrefix}:{registry}:v{version}:{key}";

    /// <summary>
    /// Boxes the cached value so "cached as absent" and "not cached" stay distinguishable in
    /// <see cref="IMemoryCache"/>, which has no way to express the difference on its own.
    /// </summary>
    private sealed record CacheSlot(object? Value);
}
