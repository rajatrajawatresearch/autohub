namespace AutoHub.Platform.Access;

/// <summary>
/// Turns a user id into everything they may do.
/// </summary>
/// <remarks>
/// Every authorisation decision in the platform starts here — the endpoint filter, the workflow
/// engine's transition guard, the admin panel's menu, the search query that has to be narrowed to
/// the cities a moderator covers.
/// </remarks>
public interface IPermissionResolver
{
    /// <summary>
    /// Resolves a user's permissions.
    /// </summary>
    /// <remarks>
    /// Returns a set rather than a <c>Result</c>, and that is a security decision rather than a
    /// stylistic one. There is no failure this method could report that a caller should treat
    /// differently from "holds nothing": an unknown user, a user with no roles, and a user whose
    /// roles all expired are the same answer. A signature that can fail invites the
    /// <c>catch { log; continue; }</c> that turns a transient store blip into an unauthorised
    /// action. Store failures throw, the request fails, and nothing is authorised by accident.
    /// </remarks>
    Task<ResolvedPermissionSet> ResolveAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// The one way a cached permission set is discarded.
/// </summary>
/// <remarks>
/// <b>Every admin write that touches roles, role permissions or assignments must call one of these
/// methods in the same unit of work.</b> This is not a performance concern that can be left to a TTL
/// to sort out: between a revoke and a TTL expiry, a user who was fired still holds their
/// permissions, and the length of that window is a security property of the system. TTL exists here
/// only as a backstop against orphaned keys and clock skew — never as the mechanism.
/// <para>
/// The asymmetry between the two methods is deliberate.
/// <see cref="InvalidateUserAsync"/> is precise and cheap: an assignment names its user.
/// <see cref="InvalidateEveryoneAsync"/> is used when a <i>role</i> changes, because working out who
/// holds a role means a query whose answer is already stale by the time it returns — a user granted
/// that role mid-flight would be missed, and missed means over-privileged. Bumping a generation
/// counter invalidates every cached set atomically, in one round trip, and re-resolution costs one
/// indexed query per active user. That is the correct trade: correctness is the requirement,
/// cheapness is the preference.
/// </para>
/// </remarks>
public interface IAccessCacheInvalidator
{
    /// <summary>Call after granting, revoking or re-scoping an assignment for one user.</summary>
    Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Call after any change to a role, a role's permissions, or the permission catalog.</summary>
    Task InvalidateEveryoneAsync(CancellationToken cancellationToken);
}

/// <summary>
/// One user's role assignments, flattened with the permission codes each role grants.
/// </summary>
/// <remarks>
/// A projection rather than the entities, because the resolver needs codes and scopes and nothing
/// else, and because what it produces is cached — see <see cref="PermissionSetSnapshot"/> for why
/// tracked entities must not cross that line.
/// </remarks>
public sealed record RoleGrant(
    Guid RoleId,
    string RoleCode,
    AccessScopeType ScopeType,
    Guid? ScopeId,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    IReadOnlyList<string> PermissionCodes);

/// <summary>
/// Reads the access graph. Implemented in the persistence layer.
/// </summary>
/// <remarks>
/// Declared here, like <c>IWorkflowStore</c> and <c>IPricingRuleStore</c>, so
/// <c>AutoHub.Platform</c> never references EF Core and the resolver's scope arithmetic can be
/// tested against a list rather than a database.
/// </remarks>
public interface IAccessStore
{
    /// <summary>
    /// Every assignment for a user, expired ones included.
    /// </summary>
    /// <remarks>
    /// Validity filtering is the resolver's job, not the query's. Keeping it in one place means the
    /// resolver can also compute when the answer next changes and clamp the cache lifetime to it;
    /// a <c>WHERE valid_to &gt; now()</c> in SQL throws that information away.
    /// </remarks>
    Task<IReadOnlyList<RoleGrant>> GetRoleGrantsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Every permission code the database holds. Fed to <see cref="PermissionCatalog.Reconcile"/>.</summary>
    Task<IReadOnlyList<string>> GetPersistedPermissionCodesAsync(CancellationToken cancellationToken);
}
