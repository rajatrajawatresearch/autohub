using AutoHub.Api.Security;
using AutoHub.Modules.Identity.Contracts;
using AutoHub.Modules.Identity.Domain;
using AutoHub.Platform.Access;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Security;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Options;

namespace AutoHub.Api.Application.Auth;

/// <summary>One issued session: the access token, and the refresh secret shown exactly once.</summary>
public sealed record IssuedSession(AccessToken Access, string RefreshSecret, Guid RefreshTokenId);

/// <summary>
/// Mints token pairs and the <c>/auth/me</c> payload.
/// </summary>
/// <remarks>
/// Shared by sign-in and by refresh so the two cannot produce different sessions for the same user.
/// A divergence there presents as permissions that change after a token refresh — alarming to the
/// user, and very hard to reproduce because it only appears fifteen minutes into a session.
/// </remarks>
internal sealed class SessionFactory
{
    private readonly IIdentityStore _store;
    private readonly IPermissionResolver _permissions;
    private readonly AccessTokenIssuer _tokens;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly JwtOptions _options;

    public SessionFactory(
        IIdentityStore store,
        IPermissionResolver permissions,
        AccessTokenIssuer tokens,
        ICurrentUser currentUser,
        IClock clock,
        IOptions<JwtOptions> options)
    {
        _store = store;
        _permissions = permissions;
        _tokens = tokens;
        _currentUser = currentUser;
        _clock = clock;
        _options = options.Value;
    }

    /// <summary>
    /// Starts a new family. One per sign-in, not one per user.
    /// </summary>
    /// <remarks>
    /// Two devices are two families, so revoking one because its token was replayed does not sign
    /// the user out of the other. That is the difference between "we detected something and
    /// contained it" and "we logged you out of everything you own".
    /// </remarks>
    public Task<IssuedSession> StartSessionAsync(User user, string? deviceId, CancellationToken cancellationToken)
        => IssueAsync(user, UuidV7.New(), deviceId, cancellationToken);

    /// <summary>
    /// Rotates within an existing family, retiring the token that was presented.
    /// </summary>
    /// <remarks>
    /// The old token is consumed <i>after</i> its replacement exists. A crash between the two leaves
    /// a usable old token, which the client will simply present again; the other order would leave a
    /// session with no valid token at all and force a re-authentication nobody asked for.
    /// </remarks>
    public async Task<IssuedSession> RotateAsync(
        User user,
        RefreshToken presented,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var issued = await IssueAsync(user, presented.FamilyId, deviceId, cancellationToken);
        presented.Consume(_clock.UtcNow, issued.RefreshTokenId);
        await _store.SaveChangesAsync(cancellationToken);
        return issued;
    }

    public async Task<AuthUserResponse> DescribeAsync(User user, CancellationToken cancellationToken)
    {
        var permissions = await _permissions.ResolveAsync(user.Id, cancellationToken);

        return new AuthUserResponse(
            user.Id.ToString(),
            user.DisplayName,
            user.Phone,
            user.Email,

            // Roles travel for display and support tooling only. The client gates on permissions —
            // §8 of the contract is explicit that a role is a grouping, not a check.
            permissions.RoleCodes,
            permissions.PermissionCodes.Order(StringComparer.Ordinal).ToList(),
            user.ActiveCityCode,
            user.KycLevel);
    }

    private async Task<IssuedSession> IssueAsync(
        User user,
        Guid familyId,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var (token, secret) = RefreshToken.Issue(
            user.Id,
            familyId,
            deviceId ?? _currentUser.DeviceId,
            _currentUser.IpAddress,
            now,
            _options.RefreshTokenLifetime);

        _store.AddRefreshToken(token);
        user.RecordAuthentication(now);
        await _store.SaveChangesAsync(cancellationToken);

        return new IssuedSession(_tokens.Issue(user), secret, token.Id);
    }
}
