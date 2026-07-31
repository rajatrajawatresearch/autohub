using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Access;

/// <summary>
/// Where a permission applies, not just whether it is held.
/// </summary>
/// <remarks>
/// A marketplace operating in forty cities has moderators who may moderate their own city and
/// nobody else's, branch managers who may discount at their branch, and dealer staff who may edit
/// their employer's inventory. Without a scope, each of those is a separate role and a separate code
/// path; with one, they are the same role assigned four different ways.
/// <para>
/// Containment between scopes — that a branch sits inside a city, that a dealer operates in three —
/// is geography and org data owned by registry 10, not by RBAC. The resolver therefore never infers
/// it: callers enforce the scope they actually mean, and <see cref="Global"/> is the only implicit
/// superset. Guessing containment here would mean a city-scoped grant silently covering a branch the
/// geography team moved last week.
/// </para>
/// </remarks>
public enum AccessScopeType
{
    /// <summary>Everywhere. The only scope that satisfies a check at any other scope.</summary>
    Global = 1,

    /// <summary>One city, keyed by the geography registry's city id.</summary>
    City = 2,

    /// <summary>One physical branch or hub.</summary>
    Branch = 3,

    /// <summary>One dealer organisation. How dealer staff get access to their own inventory.</summary>
    Dealer = 4,
}

/// <summary>
/// One user holding one role over one scope.
/// </summary>
/// <remarks>
/// "Regional Auction Moderator — West" is four rows: the <c>auction_moderator</c> role assigned to
/// one user with <see cref="AccessScopeType.City"/> scope, once for Mumbai, Pune, Nashik and Surat.
/// Adding Nagpur to the region is a fifth row. It is not a new role, not a new permission, and
/// emphatically not a new code path.
/// </remarks>
public sealed class UserRoleAssignment : AggregateRoot, IAuditable
{
    private UserRoleAssignment(
        Guid id,
        Guid userId,
        Guid roleId,
        AccessScopeType scopeType,
        Guid? scopeId,
        DateTimeOffset? validFrom,
        DateTimeOffset? validTo)
        : base(id)
    {
        UserId = userId;
        RoleId = roleId;
        ScopeType = scopeType;
        ScopeId = scopeId;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    private UserRoleAssignment() { }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public AccessScopeType ScopeType { get; private set; }

    /// <summary>
    /// The city, branch or dealer this assignment covers. Null exactly when the scope is
    /// <see cref="AccessScopeType.Global"/>.
    /// </summary>
    public Guid? ScopeId { get; private set; }

    /// <summary>Null means "since always". Lets an assignment be staged ahead of a joiner's start date.</summary>
    public DateTimeOffset? ValidFrom { get; private set; }

    /// <summary>
    /// Null means open-ended. Set by <see cref="Revoke"/>, and settable up front for temporary
    /// access — the festive-season moderator, the contractor with a six-week engagement.
    /// </summary>
    public DateTimeOffset? ValidTo { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static Result<UserRoleAssignment> Create(
        Guid userId,
        Guid roleId,
        AccessScopeType scopeType,
        Guid? scopeId,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validTo = null)
    {
        // A scoped grant with no scope id is the dangerous direction of this mistake: it would read
        // as "moderator, city scope" in the admin UI while matching no city and, on a sloppier
        // implementation, every city.
        if (scopeType == AccessScopeType.Global && scopeId.HasValue)
        {
            return Error.Validation(
                "access.assignment.global_scope_id",
                "A global assignment cannot name a scope.");
        }

        if (scopeType != AccessScopeType.Global && !scopeId.HasValue)
        {
            return Error.Validation(
                "access.assignment.scope_id_required",
                $"A {scopeType} assignment must name the {scopeType.ToString().ToLowerInvariant()} it covers.");
        }

        if (validFrom.HasValue && validTo.HasValue && validTo <= validFrom)
        {
            return Error.Validation(
                "access.assignment.invalid_window",
                "An assignment's end must come after its start.");
        }

        return new UserRoleAssignment(UuidV7.New(), userId, roleId, scopeType, scopeId, validFrom, validTo);
    }

    public bool IsActiveAt(DateTimeOffset at)
        => (ValidFrom is null || at >= ValidFrom)
           && (ValidTo is null || at < ValidTo);

    /// <summary>
    /// Ends the assignment.
    /// </summary>
    /// <remarks>
    /// Revocation closes the window; it does not delete the row. "Who was able to approve payouts
    /// last March?" is a question a fraud investigation asks, and a deleted row answers it with
    /// silence. Re-granting later is a new row, so the history reads as the sequence of decisions
    /// that actually happened.
    /// </remarks>
    public Result Revoke(DateTimeOffset now)
    {
        if (ValidTo.HasValue && ValidTo <= now)
        {
            return Result.Success();
        }

        ValidTo = now;
        return Result.Success();
    }
}
