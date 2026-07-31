namespace AutoHub.Platform.Caching;

/// <summary>
/// The read path every registry shares.
/// </summary>
/// <remarks>
/// The quality floor says it plainly: "every registry read is cached with explicit invalidation on
/// write; never a per-request DB hit." A search request touches taxonomy, i18n, features and
/// pricing before it touches a vehicle; without this, the ten registries would turn one page view
/// into a dozen round trips to Postgres and the whole metadata-driven bet would read as slow.
/// <para>
/// Values handed back are <b>shared instances</b>. The same object is returned to every concurrent
/// request on this node, so anything stored here must be immutable — records, frozen dictionaries,
/// read-only lists. A caller that mutates a cached bundle corrupts it for every other request until
/// the entry expires, and the resulting bug report will say "the Tamil strings randomly appear in
/// Hindi".
/// </para>
/// </remarks>
public interface IRegistryCache
{
    /// <summary>
    /// Returns the cached value, or loads and caches it.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> from <paramref name="loader"/> is cached too, for a deliberately shorter window.
    /// Without negative caching, one bad locale in a client's config — <c>hi_IN</c> instead of
    /// <c>hi-IN</c> — becomes a database query on every request that app ever makes.
    /// </remarks>
    Task<T?> GetOrLoadAsync<T>(
        string registry,
        string key,
        Func<CancellationToken, Task<T?>> loader,
        CancellationToken cancellationToken)
        where T : class;

    /// <summary>The current generation, for the <c>ETag</c> and <c>X-Registry-Version</c> headers.</summary>
    Task<RegistryVersion> GetVersionAsync(string registry, CancellationToken cancellationToken);

    /// <summary>
    /// Called by a registry write. Every reader on every node misses on its next read and reloads.
    /// </summary>
    Task<RegistryVersion> InvalidateAsync(string registry, CancellationToken cancellationToken);
}

public static class RegistryCacheExtensions
{
    /// <summary>
    /// For loads that always produce a value — a merged bundle, a rule set, a list that can be empty
    /// but never absent.
    /// </summary>
    /// <remarks>
    /// Exists because <c>Task&lt;T&gt;</c> is not covariant, so a loader declared as returning
    /// <c>Task&lt;I18nBundle&gt;</c> will not bind to a parameter of <c>Task&lt;I18nBundle?&gt;</c>.
    /// Without this overload every registry would open with the same noisy cast, and one of them
    /// would eventually silence the mismatch with <c>!</c> instead.
    /// </remarks>
    public static async Task<T> GetOrLoadRequiredAsync<T>(
        this IRegistryCache cache,
        string registry,
        string key,
        Func<CancellationToken, Task<T>> loader,
        CancellationToken cancellationToken)
        where T : class
    {
        var cached = await cache.GetOrLoadAsync<T>(
            registry,
            key,
            async token => await loader(token),
            cancellationToken);

        // A null can only arrive from a negative entry written under the same key by an earlier
        // shape of the code. Reloading is cheap and self-healing; propagating a null the signature
        // says is impossible is not.
        return cached ?? await loader(cancellationToken);
    }
}

public sealed class RegistryCacheOptions
{
    /// <summary>
    /// Namespace for every key this cache writes. Distinct per environment so a staging deploy
    /// pointed at a shared Redis cannot serve staging taxonomy to production.
    /// </summary>
    public string KeyPrefix { get; set; } = "autohub:registry";

    /// <summary>
    /// How long a loaded snapshot lives in process memory. This is a backstop, not the invalidation
    /// mechanism — a version bump makes the entry unreachable long before it expires.
    /// </summary>
    public TimeSpan MemoryTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long a snapshot lives in Redis. Longer than memory: its job is to spare a cold node the
    /// database on deploy, when twenty pods start at once and all miss simultaneously.
    /// </summary>
    public TimeSpan DistributedTtl { get; set; } = TimeSpan.FromHours(6);

    /// <summary>Short, because "not found" is usually a client bug that will be fixed shortly.</summary>
    public TimeSpan NegativeTtl { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// How long a node may keep serving against a version it has not re-checked.
    /// </summary>
    /// <remarks>
    /// This single number is the upper bound on registry staleness across the whole fleet, and it is
    /// the entire cost of the version-bump design: an admin publishes, and within this window every
    /// node notices. Five seconds costs one tiny Redis read per registry per node per five seconds —
    /// cheaper by orders of magnitude than checking the freshness of each cached entry.
    /// </remarks>
    public TimeSpan VersionTtl { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Turns the Redis tier off. Set when running without Redis; the memory tier still applies and
    /// the API still boots.
    /// </summary>
    public bool UseDistributedTier { get; set; } = true;
}
