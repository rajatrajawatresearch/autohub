using System.Text.RegularExpressions;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Access;

/// <summary>
/// A named bundle of permissions. A grouping, never an authorisation check.
/// </summary>
/// <remarks>
/// The distinction is the entire point of this registry and it is worth stating plainly: nothing in
/// the codebase asks what role a user has. Code asks whether the actor holds a permission; roles
/// exist so a human can grant fifteen permissions in one click and revoke them in one more.
/// <para>
/// The moment a role name appears in an <c>if</c>, adding a role becomes a deployment — and the
/// PRD's org chart has regional moderators, branch managers, dealer staff, and partner agents in it,
/// all of which are permutations of the same permissions over different scopes.
/// </para>
/// <para>
/// Roles are deliberately <b>not</b> scoped. "Regional Auction Moderator — West" is not a role; it
/// is the <c>auction_moderator</c> role assigned with a city scope, once per city in the region.
/// Baking the region into the role would produce one role per region per job, which is the
/// combinatorial explosion this design exists to avoid — see <see cref="UserRoleAssignment"/>.
/// </para>
/// </remarks>
public sealed partial class Role : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<RolePermission> _permissions = [];

    private Role(Guid id, string code, string name, string? description, bool isSystem) : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        IsSystem = isSystem;
    }

    private Role() { }

    /// <summary>Stable machine code, e.g. <c>auction_moderator</c>. Never shown to a user.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>
    /// Shipped with the platform and depended on by seed data and by the bootstrap admin.
    /// </summary>
    /// <remarks>
    /// System roles cannot be deleted, because the first thing that happens after someone deletes
    /// <c>super_admin</c> is that nobody can grant it back. They remain editable — an operator
    /// tightening the default moderator role is normal — so this is a delete guard, not a freeze.
    /// </remarks>
    public bool IsSystem { get; private set; }

    public IReadOnlyList<RolePermission> Permissions => _permissions;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex CodePattern { get; }

    public static Result<Role> Create(string code, string name, string? description = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(code) || !CodePattern.IsMatch(code.Trim()))
        {
            return Error.Validation(
                "role.code.malformed",
                "A role code is lowercase letters, digits and underscores, e.g. 'auction_moderator'.")
                .With("input", code);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("role.name.required", "A role needs a human-readable name.");
        }

        return new Role(UuidV7.New(), code.Trim(), name.Trim(), description, isSystem);
    }

    public Result Rename(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("role.name.required", "A role needs a human-readable name.");
        }

        Name = name.Trim();
        Description = description;
        return Result.Success();
    }

    /// <summary>
    /// Grants a permission to this role.
    /// </summary>
    /// <remarks>
    /// Re-granting is a success, not a conflict. The admin permission matrix submits the full set of
    /// checkboxes on every save, and a client that ticks an already-ticked box has expressed the
    /// state it wants — refusing it would turn a no-op into an error dialog.
    /// </remarks>
    public Result Grant(Guid permissionId, DateTimeOffset now, Guid? actorId)
    {
        if (_permissions.Exists(p => p.PermissionId == permissionId))
        {
            return Result.Success();
        }

        _permissions.Add(new RolePermission(Id, permissionId, now, actorId));
        return Result.Success();
    }

    public Result Revoke(Guid permissionId)
    {
        _permissions.RemoveAll(p => p.PermissionId == permissionId);
        return Result.Success();
    }

    public Result Delete(DateTimeOffset now, Guid? actorId)
    {
        if (IsSystem)
        {
            return Error.Conflict(
                "role.system_undeletable",
                "This role ships with the platform and cannot be deleted.")
                .With("roleCode", Code);
        }

        if (DeletedAt.HasValue)
        {
            return Result.Success();
        }

        DeletedAt = now;
        DeletedBy = actorId;
        return Result.Success();
    }
}

/// <summary>
/// A permission granted to a role.
/// </summary>
/// <remarks>
/// Carries who granted it and when. A join table with no audit columns answers "what can this role
/// do?" but not "who gave the support team the ability to approve payouts, and on whose authority?",
/// which is the question that actually gets asked — once, urgently, after the fact.
/// </remarks>
public sealed class RolePermission
{
    public RolePermission(Guid roleId, Guid permissionId, DateTimeOffset grantedAt, Guid? grantedBy)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedAt = grantedAt;
        GrantedBy = grantedBy;
    }

    private RolePermission() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public DateTimeOffset GrantedAt { get; private set; }

    /// <summary>Null when the grant came from seed data rather than from a person.</summary>
    public Guid? GrantedBy { get; private set; }
}
