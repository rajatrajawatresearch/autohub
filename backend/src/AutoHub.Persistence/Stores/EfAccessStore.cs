using AutoHub.Platform.Access;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Reads the access graph: a user's role assignments, flattened with the codes each role grants.
/// </summary>
/// <remarks>
/// A projection rather than entities, because the resolver needs codes and scopes and nothing else,
/// and because what it produces is cached — a tracked <c>Role</c> graph crossing that line would
/// hold a scoped context open and be shared across threads.
/// <para>
/// <b>Expired assignments are returned, not filtered.</b> Pushing <c>valid_to &gt; now()</c> into SQL
/// looks like an obvious win and throws away the thing the resolver needs most: knowing <i>when</i>
/// the answer next changes, so it can clamp the cache lifetime to that instant. Without it, a
/// permission that expires at 18:00 stays cached until the TTL happens to lapse, and the length of
/// that window is a security property of the system rather than a performance one.
/// </para>
/// <para>
/// Roles carry a soft-delete filter, so a deleted role contributes no join rows and therefore no
/// permissions. That is the correct reading of a deleted role, and it happens without this query
/// saying anything about it.
/// </para>
/// </remarks>
public sealed class EfAccessStore : IAccessStore
{
    private readonly AutoHubDbContext _context;

    public EfAccessStore(AutoHubDbContext context) => _context = context;

    public async Task<IReadOnlyList<RoleGrant>> GetRoleGrantsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from assignment in _context.UserRoleAssignments.AsNoTracking()
            join role in _context.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            where assignment.UserId == userId
            select new
            {
                assignment.RoleId,
                RoleCode = role.Code,
                assignment.ScopeType,
                assignment.ScopeId,
                assignment.ValidFrom,
                assignment.ValidTo,

                // A correlated sub-select rather than a second join, so one role granting forty
                // permissions produces one row with forty codes instead of forty rows repeating
                // the assignment's columns.
                PermissionCodes = (
                    from grant in _context.RolePermissions
                    join permission in _context.Permissions on grant.PermissionId equals permission.Id
                    where grant.RoleId == role.Id
                    select permission.Code).ToList(),
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new RoleGrant(
            row.RoleId,
            row.RoleCode,
            row.ScopeType,
            row.ScopeId,
            row.ValidFrom,
            row.ValidTo,
            row.PermissionCodes))];
    }

    /// <summary>
    /// Every permission code the database holds, for reconciliation against the code's catalog.
    /// </summary>
    /// <remarks>
    /// The catalog in code is the source of truth — a permission exists because a line of code
    /// checks it — and these rows are its projection, kept so roles can reference them by id and the
    /// admin matrix can join to them. Reconciliation on boot is what stops the two drifting.
    /// </remarks>
    public async Task<IReadOnlyList<string>> GetPersistedPermissionCodesAsync(
        CancellationToken cancellationToken)
        => await _context.Permissions
            .AsNoTracking()
            .Select(permission => permission.Code)
            .ToListAsync(cancellationToken);
}
