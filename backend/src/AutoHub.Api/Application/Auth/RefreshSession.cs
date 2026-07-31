using AutoHub.Modules.Identity.Contracts;
using AutoHub.Modules.Identity.Domain;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Api.Application.Auth;

/// <summary>
/// <c>POST /auth/refresh</c> — rotating, single-use, with reuse detection.
/// </summary>
/// <remarks>
/// <b>Deliberately not <c>ITransactionalRequest</c>.</b> That behaviour rolls back when the
/// handler returns a failure, which is right for a publish that half-happened and catastrophically
/// wrong here: reuse detection's whole job is to revoke a token family and <i>then</i> fail the
/// request, and a rollback would undo the revocation and hand the attacker back the credential we
/// had just taken away. The failure path of this command contains its most important write.
/// <para>
/// Consistency is kept inside the handler instead. Rotation issues the replacement before consuming
/// the presented token, so the only interleaving a crash can produce is "the old token still works"
/// — which the client recovers from by presenting it again.
/// </para>
/// </remarks>
internal sealed record RefreshSessionCommand(string RefreshToken, string? DeviceId)
    : IRequest<Result<TokenPairResponse>>;

internal sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionValidator()
        => RuleFor(command => command.RefreshToken)
            .NotEmpty().WithErrorCode("identity.refresh.required")
            .MaximumLength(512).WithErrorCode("identity.refresh.invalid");
}

internal sealed class RefreshSessionHandler
    : IRequestHandler<RefreshSessionCommand, Result<TokenPairResponse>>
{
    /// <summary>The reason stamped on every token in a family that was revoked by reuse detection.</summary>
    private const string ReuseDetected = "reuse_detected";

    private readonly IIdentityStore _store;
    private readonly SessionFactory _sessions;
    private readonly IClock _clock;
    private readonly ILogger<RefreshSessionHandler> _logger;

    public RefreshSessionHandler(
        IIdentityStore store,
        SessionFactory sessions,
        IClock clock,
        ILogger<RefreshSessionHandler> logger)
    {
        _store = store;
        _sessions = sessions;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<TokenPairResponse>> Handle(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        var hash = RefreshToken.Hash(command.RefreshToken);
        var token = await _store.FindRefreshTokenAsync(hash, cancellationToken);

        if (token is null)
        {
            // A token we have never issued. Could be garbage, could be a guess; either way the only
            // safe answer is the same one an expired token gets, so the response cannot be used to
            // distinguish "wrong" from "revoked".
            return Unauthenticated();
        }

        var now = _clock.UtcNow;

        if (token.ConsumedAt is not null)
        {
            // The attack. This token was already exchanged, so either an attacker is replaying a
            // stolen one or the legitimate client is presenting one an attacker already used — and
            // there is no way to tell which. Revoking the whole family logs both out and forces an
            // OTP, which the real owner can pass and the thief cannot.
            await RevokeFamilyAsync(token, now, cancellationToken);

            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; family {FamilyId} revoked",
                token.UserId, token.FamilyId);

            return Error.Unauthorized(
                "identity.refresh.reuse_detected",
                "Your session was ended for security. Sign in again.");
        }

        if (!token.IsUsable(now))
        {
            return Unauthenticated();
        }

        var user = await _store.FindUserAsync(token.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            // The account was suspended or deleted mid-session. The refresh path is the only place
            // that check can bite, because an access token cannot be revoked — which is exactly why
            // its lifetime is fifteen minutes.
            await RevokeFamilyAsync(token, now, cancellationToken);
            return Unauthenticated();
        }

        var session = await _sessions.RotateAsync(user, token, command.DeviceId, cancellationToken);

        return new TokenPairResponse(
            session.Access.Value,
            session.RefreshSecret,
            session.Access.ExpiresInSeconds,
            "Bearer");
    }

    private async Task RevokeFamilyAsync(
        RefreshToken token,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var member in await _store.GetTokenFamilyAsync(token.FamilyId, cancellationToken))
        {
            member.Revoke(now, ReuseDetected);
        }

        await _store.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// One indistinguishable answer for every unusable token.
    /// </summary>
    /// <remarks>
    /// Unknown, expired and revoked all return this. Separate codes would let a caller probe which
    /// tokens exist, and there is nothing a client can usefully do differently — all three mean
    /// "sign in again".
    /// </remarks>
    private static Error Unauthenticated()
        => Error.Unauthorized("identity.refresh.invalid", "Your session has ended. Sign in again.");
}
