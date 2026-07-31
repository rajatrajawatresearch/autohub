using AutoHub.Platform.Access;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Security;

namespace AutoHub.Modules.Listings.Infrastructure;

/// <summary>
/// Builds the workflow actor for the caller of the current request.
/// </summary>
/// <remarks>
/// <b>The anonymous case is the reason this exists.</b> <see cref="WorkflowActor.System"/> has a null
/// user id, and the engine reads a null user id as "the scheduler" — an actor that holds every
/// permission by definition. Handing an unauthenticated visitor that actor would offer them
/// "Approve" on a listing awaiting moderation, and the engine would let them fire it.
/// <para>
/// So an anonymous caller gets a <b>non-null sentinel id with an empty permission set</b>. That
/// actor is not the system, holds nothing, and can therefore fire only transitions that require no
/// permission at all — which is precisely the right answer for a public listing page, where a
/// visitor should see the state and none of the buttons.
/// </para>
/// <para>
/// The permission set is resolved per request rather than read from the token. A permission revoked
/// thirty seconds ago must be gone, and a copy carried in a JWT for the next fifteen minutes is a
/// copy that says otherwise.
/// </para>
/// </remarks>
public sealed class WorkflowActorFactory
{
    /// <summary>
    /// The id an unauthenticated caller acts under.
    /// </summary>
    /// <remarks>
    /// Deliberately <see cref="Guid.Empty"/> and never a real user: it can never collide with an
    /// issued id, it is obvious in a log line, and it makes "there was no user" distinguishable from
    /// "the system did it", which is exactly the distinction <c>ICurrentUser</c> preserves.
    /// </remarks>
    public static readonly Guid AnonymousActorId = Guid.Empty;

    private static readonly IReadOnlySet<string> NoPermissions = new HashSet<string>(StringComparer.Ordinal);

    private readonly ICurrentUser _currentUser;
    private readonly IPermissionResolver _permissions;

    public WorkflowActorFactory(ICurrentUser currentUser, IPermissionResolver permissions)
    {
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<WorkflowActor> ForCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return new WorkflowActor(AnonymousActorId, NoPermissions);
        }

        var resolved = await _permissions.ResolveAsync(userId, cancellationToken);
        return new WorkflowActor(userId, resolved.PermissionCodes);
    }
}
