using AutoHub.Api.Application.Behaviours;
using AutoHub.Modules.Identity.Contracts;
using AutoHub.Modules.Identity.Domain;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Security;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Api.Application.Auth;

/// <summary><c>POST /auth/logout</c>.</summary>
internal sealed record LogoutCommand(string? RefreshToken) : IRequest<Result>, ITransactionalRequest;

internal sealed class LogoutHandler : IRequestHandler<LogoutCommand, Result>
{
    private const string Reason = "logout";

    private readonly IIdentityStore _store;
    private readonly IClock _clock;

    public LogoutHandler(IIdentityStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <summary>
    /// Ends one session — the family the presented token belongs to.
    /// </summary>
    /// <remarks>
    /// <b>Always succeeds.</b> A logout that can fail is a logout a client has to retry, and a
    /// client that cannot log out is a client that leaves a live session on a shared device. An
    /// unknown or already-revoked token is the state the caller asked for, so it is reported as
    /// success rather than as a 404 that tells them nothing actionable.
    /// <para>
    /// The whole family, not just the presented token, because rotation means the token the client
    /// holds may not be the newest one this device was issued.
    /// </para>
    /// </remarks>
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result.Success();
        }

        var token = await _store.FindRefreshTokenAsync(
            RefreshToken.Hash(command.RefreshToken), cancellationToken);

        if (token is null)
        {
            return Result.Success();
        }

        var now = _clock.UtcNow;

        foreach (var member in await _store.GetTokenFamilyAsync(token.FamilyId, cancellationToken))
        {
            member.Revoke(now, Reason);
        }

        await _store.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary><c>GET /auth/me</c> — resolved permissions, never roles-as-authorisation.</summary>
internal sealed record GetCurrentUserQuery : IRequest<Result<AuthUserResponse>>;

internal sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<AuthUserResponse>>
{
    private readonly IIdentityStore _store;
    private readonly SessionFactory _sessions;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserHandler(IIdentityStore store, SessionFactory sessions, ICurrentUser currentUser)
    {
        _store = store;
        _sessions = sessions;
        _currentUser = currentUser;
    }

    public async Task<Result<AuthUserResponse>> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Error.Unauthorized("identity.token.expired", "Sign in again to continue.");
        }

        var user = await _store.FindUserAsync(userId, cancellationToken);

        // A valid token for a user who no longer exists. Reported as an authentication failure, not
        // a 404: the client's correct response is to sign in again, and a 404 would send it looking
        // for a resource instead.
        return user is null
            ? Error.Unauthorized("identity.user.not_found", "Sign in again to continue.")
            : await _sessions.DescribeAsync(user, cancellationToken);
    }
}
