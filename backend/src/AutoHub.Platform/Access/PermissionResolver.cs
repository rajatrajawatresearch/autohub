using System.Collections.Concurrent;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Access;

/// <summary>Tuning knobs for permission resolution. Technical, not business — no registry needed.</summary>
public sealed class AccessOptions
{
    /// <summary>
    /// How long a resolved set may live in the distributed cache.
    /// </summary>
    /// <remarks>
    /// This is a backstop, not the invalidation mechanism — see
    /// <see cref="IAccessCacheInvalidator"/>. It bounds the damage from a missed invalidation and
    /// reclaims keys orphaned by a generation bump; it is not what makes a revoke take effect.
    /// </remarks>
    public TimeSpan SnapshotTimeToLive { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Resolves permissions with two layers of cache: one per request, one across the cluster.
/// </summary>
/// <remarks>
/// The per-request layer is not just a saved round trip. A single request asks this question several
/// times — the endpoint filter, the workflow engine building the list of available actions, the
/// query that narrows a moderation queue to the actor's cities — and if an admin revokes a role
/// between two of those calls, the request would authorise the first half of an operation and refuse
/// the second. Pinning the answer for the lifetime of the request makes each request an all-or-nothing
/// decision against one consistent view.
/// <para>
/// The distributed layer is what keeps the database out of the hot path. Its correctness rests
/// entirely on explicit invalidation, which is why the invalidator is a first-class interface with
/// its own documentation rather than a private detail of this class.
/// </para>
/// </remarks>
public sealed class PermissionResolver : IPermissionResolver
{
    private readonly ConcurrentDictionary<Guid, Task<ResolvedPermissionSet>> _perRequest = new();
    private readonly IAccessStore _store;
    private readonly IAccessCache _cache;
    private readonly AccessOptions _options;
    private readonly IClock _clock;
    private readonly ILogger<PermissionResolver> _logger;

    public PermissionResolver(
        IAccessStore store,
        IAccessCache cache,
        AccessOptions options,
        IClock clock,
        ILogger<PermissionResolver> logger)
    {
        _store = store;
        _cache = cache;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ResolvedPermissionSet> ResolveAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Sharing the in-flight task rather than the result also collapses the concurrent case:
        // an endpoint that fans out to three handlers resolves once, not three times.
        var pending = _perRequest.GetOrAdd(userId, id => ResolveUncachedAsync(id, cancellationToken));

        try
        {
            return await pending;
        }
        catch
        {
            // A faulted task must not be memoised for the rest of the request — the caller may
            // retry, and a cached failure would make the retry pointless.
            _perRequest.TryRemove(userId, out _);
            throw;
        }
    }

    private async Task<ResolvedPermissionSet> ResolveUncachedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var generation = await _cache.GetGenerationAsync(cancellationToken);
        var key = AccessCacheKeys.ForUser(generation, userId);
        var now = _clock.UtcNow;

        var cached = await _cache.GetAsync(key, cancellationToken);
        if (cached is not null && (cached.ValidUntil is null || cached.ValidUntil > now))
        {
            return ResolvedPermissionSet.FromSnapshot(cached);
        }

        // Belt and braces: a cache that ignored the TTL, or a clock that moved, must not extend a
        // time-boxed assignment past its end. Cheaper to re-resolve than to explain.
        if (cached is not null)
        {
            _logger.LogDebug("Permission snapshot for {UserId} passed its validity boundary; re-resolving", userId);
        }

        var grants = await _store.GetRoleGrantsAsync(userId, cancellationToken);
        var snapshot = BuildSnapshot(userId, grants, now);

        await _cache.SetAsync(key, snapshot, TimeToLiveFor(snapshot, now), cancellationToken);

        return ResolvedPermissionSet.FromSnapshot(snapshot);
    }

    /// <summary>
    /// Flattens assignments into permission-per-scope grants, keeping only what applies right now.
    /// </summary>
    /// <remarks>
    /// The flattening is what makes the hot path cheap: <see cref="ResolvedPermissionSet"/> answers
    /// "may this actor moderate in Pune?" from a dictionary lookup, with no knowledge that a role
    /// was ever involved. Roles are an authoring convenience that stops existing at this line.
    /// </remarks>
    private static PermissionSetSnapshot BuildSnapshot(
        Guid userId,
        IReadOnlyList<RoleGrant> roleGrants,
        DateTimeOffset now)
    {
        var active = roleGrants
            .Where(g => (g.ValidFrom is null || now >= g.ValidFrom) && (g.ValidTo is null || now < g.ValidTo))
            .ToList();

        if (active.Count == 0)
        {
            return ResolvedPermissionSet.Empty(userId, now).ToSnapshot() with
            {
                ValidUntil = NextBoundary(roleGrants, now),
            };
        }

        var seen = new HashSet<(string Code, AccessScopeType Scope, Guid? Id)>();
        var grants = new List<PermissionGrant>();

        foreach (var roleGrant in active)
        {
            foreach (var code in roleGrant.PermissionCodes)
            {
                // Two roles granting the same permission over the same scope is the normal case,
                // not an anomaly — deduplicating here keeps the snapshot small enough that caching
                // it is worth doing.
                if (seen.Add((code, roleGrant.ScopeType, roleGrant.ScopeId)))
                {
                    grants.Add(new PermissionGrant(code, roleGrant.ScopeType, roleGrant.ScopeId));
                }
            }
        }

        var roleCodes = active
            .Select(g => g.RoleCode)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        return new PermissionSetSnapshot(userId, now, roleCodes, grants, NextBoundary(roleGrants, now));
    }

    /// <summary>
    /// The next instant at which this user's answer changes on its own.
    /// </summary>
    /// <remarks>
    /// Both directions matter: an assignment that starts at 09:00 must not be served from a snapshot
    /// taken at 08:58, and one that ends at 18:00 must not survive to 18:04.
    /// </remarks>
    private static DateTimeOffset? NextBoundary(IReadOnlyList<RoleGrant> roleGrants, DateTimeOffset now)
    {
        DateTimeOffset? next = null;

        foreach (var grant in roleGrants)
        {
            foreach (var boundary in new[] { grant.ValidFrom, grant.ValidTo })
            {
                if (boundary is { } at && at > now && (next is null || at < next))
                {
                    next = at;
                }
            }
        }

        return next;
    }

    private TimeSpan TimeToLiveFor(PermissionSetSnapshot snapshot, DateTimeOffset now)
    {
        var ttl = _options.SnapshotTimeToLive;

        if (snapshot.ValidUntil is { } until && until - now < ttl)
        {
            ttl = until - now;
        }

        // A non-positive TTL would be interpreted as "no expiry" by some cache implementations —
        // exactly the wrong answer for an entry that is already at its boundary.
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1);
    }
}
