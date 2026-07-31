namespace AutoHub.Platform.Access;

/// <summary>One permission held over one scope. The atom the resolver produces.</summary>
public sealed record PermissionGrant(string PermissionCode, AccessScopeType ScopeType, Guid? ScopeId);

/// <summary>
/// The cacheable, serialisable form of a resolved user.
/// </summary>
/// <remarks>
/// Deliberately a plain record of plain values rather than the entities it was built from. Putting
/// EF-tracked entities in a cache that outlives a request leaks the change tracker across requests
/// and, worse, hands two threads the same mutable graph.
/// </remarks>
public sealed record PermissionSetSnapshot(
    Guid UserId,
    DateTimeOffset ResolvedAt,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<PermissionGrant> Grants,
    DateTimeOffset? ValidUntil);

/// <summary>
/// Everything one user may do, answered without touching the database.
/// </summary>
/// <remarks>
/// This is what every authorisation check in the platform consults, and it is intentionally the only
/// shape that can answer them: there is no "get roles" accessor to be misused as an authorisation
/// check, because the whole point of the registry is that role names never reach an <c>if</c>.
/// <see cref="RoleCodes"/> exists for display and support tooling only.
/// </remarks>
public sealed class ResolvedPermissionSet
{
    private static readonly IReadOnlySet<Guid> NoScopes = new HashSet<Guid>(0);

    private readonly Dictionary<string, ScopeIndex> _index;

    private ResolvedPermissionSet(
        Guid userId,
        DateTimeOffset resolvedAt,
        DateTimeOffset? validUntil,
        IReadOnlyList<string> roleCodes,
        IReadOnlyList<PermissionGrant> grants,
        Dictionary<string, ScopeIndex> index)
    {
        UserId = userId;
        ResolvedAt = resolvedAt;
        ValidUntil = validUntil;
        RoleCodes = roleCodes;
        Grants = grants;
        _index = index;
        PermissionCodes = index.Keys.ToHashSet(StringComparer.Ordinal);
    }

    public Guid UserId { get; }

    public DateTimeOffset ResolvedAt { get; }

    /// <summary>
    /// When the earliest time-boxed assignment in this set starts or stops applying, if any.
    /// </summary>
    /// <remarks>
    /// A contractor's access ending at 18:00 must actually end at 18:00, not at 18:00 plus whatever
    /// remains of a cache window. The resolver clamps the cache lifetime to this instant.
    /// </remarks>
    public DateTimeOffset? ValidUntil { get; }

    /// <summary>For rendering "Auction Moderator" next to a name. Never for deciding anything.</summary>
    public IReadOnlyList<string> RoleCodes { get; }

    public IReadOnlyList<PermissionGrant> Grants { get; }

    /// <summary>
    /// The flat set of codes held at any scope.
    /// </summary>
    /// <remarks>
    /// This is what <c>GET /auth/me</c> returns and what a <c>WorkflowActor</c> is constructed from.
    /// Both are scope-blind by design — the workflow engine asks "may this actor approve at all?"
    /// and the endpoint that owns the entity then asks the scoped question about that entity's city.
    /// </remarks>
    public IReadOnlySet<string> PermissionCodes { get; }

    public static ResolvedPermissionSet Empty(Guid userId, DateTimeOffset resolvedAt)
        => new(userId, resolvedAt, null, [], [], new Dictionary<string, ScopeIndex>(StringComparer.Ordinal));

    public static ResolvedPermissionSet FromSnapshot(PermissionSetSnapshot snapshot)
    {
        var index = new Dictionary<string, ScopeIndex>(StringComparer.Ordinal);

        foreach (var grant in snapshot.Grants)
        {
            if (!index.TryGetValue(grant.PermissionCode, out var scopes))
            {
                scopes = new ScopeIndex();
                index[grant.PermissionCode] = scopes;
            }

            scopes.Add(grant.ScopeType, grant.ScopeId);
        }

        return new ResolvedPermissionSet(
            snapshot.UserId,
            snapshot.ResolvedAt,
            snapshot.ValidUntil,
            snapshot.RoleCodes,
            snapshot.Grants,
            index);
    }

    public PermissionSetSnapshot ToSnapshot()
        => new(UserId, ResolvedAt, RoleCodes, Grants, ValidUntil);

    /// <summary>
    /// Whether the user holds this permission <b>anywhere</b>.
    /// </summary>
    /// <remarks>
    /// The right question for showing a menu item, a tab, or a button, and the wrong question for
    /// letting an action through. A moderator scoped to Pune holds <c>listing.moderate</c> — this
    /// returns true, and the Mumbai listing they opened must still be refused by
    /// <see cref="Holds(string, AccessScopeType, Guid)"/>. Two methods rather than one optional
    /// parameter, so the scope-blind call is a visible choice at the call site rather than a
    /// forgotten argument.
    /// </remarks>
    public bool Holds(string permission)
        => _index.ContainsKey(permission);

    /// <summary>
    /// Whether the user holds this permission over a specific city, branch or dealer.
    /// </summary>
    /// <remarks>
    /// A global grant satisfies every scope; a scoped grant satisfies only its own scope type and id.
    /// No containment is inferred — see <see cref="AccessScopeType"/> for why that is a geography
    /// question this registry refuses to guess at.
    /// </remarks>
    public bool Holds(string permission, AccessScopeType scopeType, Guid scopeId)
        => _index.TryGetValue(permission, out var scopes) && scopes.Covers(scopeType, scopeId);

    /// <summary>Whether the permission is held unrestricted. The platform-staff check.</summary>
    public bool HoldsGlobally(string permission)
        => _index.TryGetValue(permission, out var scopes) && scopes.IsGlobal;

    /// <summary>
    /// The scope ids this permission is held over, for turning authorisation into a query filter.
    /// </summary>
    /// <remarks>
    /// A regional moderator's queue is "listings in the cities I moderate", and the honest way to
    /// build that is a <c>WHERE city_id = ANY(...)</c>, not fetching every listing and filtering in
    /// memory. Empty when the permission is held globally — callers must check
    /// <see cref="HoldsGlobally"/> first, because "no scope restriction" and "no scopes" are
    /// opposite answers that an empty list cannot distinguish.
    /// </remarks>
    public IReadOnlySet<Guid> ScopesFor(string permission, AccessScopeType scopeType)
        => _index.TryGetValue(permission, out var scopes) ? scopes.Ids(scopeType) : NoScopes;

    private sealed class ScopeIndex
    {
        private readonly Dictionary<AccessScopeType, HashSet<Guid>> _scoped = [];

        public bool IsGlobal { get; private set; }

        public void Add(AccessScopeType scopeType, Guid? scopeId)
        {
            if (scopeType == AccessScopeType.Global)
            {
                IsGlobal = true;
                return;
            }

            // A scoped grant with no id cannot be honoured. Dropping it is the safe direction:
            // the alternative interpretations are "matches nothing" (useless) and "matches
            // everything" (a silent privilege escalation from a malformed row).
            if (scopeId is not { } id)
            {
                return;
            }

            if (!_scoped.TryGetValue(scopeType, out var ids))
            {
                ids = [];
                _scoped[scopeType] = ids;
            }

            ids.Add(id);
        }

        public bool Covers(AccessScopeType scopeType, Guid scopeId)
            => IsGlobal || (_scoped.TryGetValue(scopeType, out var ids) && ids.Contains(scopeId));

        public IReadOnlySet<Guid> Ids(AccessScopeType scopeType)
            => _scoped.TryGetValue(scopeType, out var ids) ? ids : NoScopes;
    }
}
