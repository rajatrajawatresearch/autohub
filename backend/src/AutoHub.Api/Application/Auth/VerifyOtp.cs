using System.Globalization;
using AutoHub.Modules.Identity.Contracts;
using AutoHub.Modules.Identity.Domain;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;

namespace AutoHub.Api.Application.Auth;

/// <summary>
/// <c>POST /auth/otp/verify</c> — the only place a session is created.
/// </summary>
/// <remarks>
/// <b>Deliberately not <c>ITransactionalRequest</c></b>, for the same reason as
/// <c>RefreshSessionCommand</c>: the attempt counter is incremented on the failing path, and a
/// behaviour that rolls back on failure would roll the counter back too. The lockout would then
/// never trigger and a six-digit code would be a one-million-guess brute force with no ceiling —
/// a security hole created entirely by a correctness mechanism applied one endpoint too widely.
/// <para>
/// The remaining steps are individually consistent. If token issuance fails after the user row is
/// created, the caller is left with an account and no session, and their next OTP signs them in.
/// </para>
/// </remarks>
internal sealed record VerifyOtpCommand(string ChallengeId, string Code, string? DeviceId)
    : IRequest<Result<AuthSessionResponse>>;

internal sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpValidator()
    {
        RuleFor(command => command.ChallengeId)
            .NotEmpty().WithErrorCode("identity.otp.challenge_required");

        RuleFor(command => command.Code)
            .NotEmpty().WithErrorCode("identity.otp.code_required")
            .MaximumLength(12).WithErrorCode("identity.otp.incorrect");
    }
}

internal sealed class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand, Result<AuthSessionResponse>>
{
    /// <summary>
    /// What the shell should route to when the account has no name yet.
    /// </summary>
    /// <remarks>
    /// A machine code the client maps to a route, not a sentence. The contract's <c>nextStep</c>
    /// exists so the shell stops guessing from the shape of the user object — a guess that gets it
    /// wrong the first time a field becomes optional.
    /// </remarks>
    private const string CompleteProfileStep = "complete_profile";

    private readonly IIdentityStore _store;
    private readonly SessionFactory _sessions;
    private readonly IClock _clock;

    public VerifyOtpHandler(IIdentityStore store, SessionFactory sessions, IClock clock)
    {
        _store = store;
        _sessions = sessions;
        _clock = clock;
    }

    public async Task<Result<AuthSessionResponse>> Handle(
        VerifyOtpCommand command,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(command.ChallengeId, CultureInfo.InvariantCulture, out var challengeId))
        {
            return Error.Validation("identity.otp.challenge_not_found", "That code request has expired.");
        }

        var challenge = await _store.FindChallengeAsync(challengeId, cancellationToken);
        if (challenge is null)
        {
            return Error.NotFound("identity.otp.challenge_not_found", "That code request has expired.");
        }

        var verification = challenge.Verify(command.Code, _clock.UtcNow);

        // Saved on both paths, deliberately. A failed attempt has to be durable or the attempt
        // counter is advisory: an attacker who aborts the connection after each guess would
        // otherwise get unlimited tries against a counter that never commits.
        await _store.SaveChangesAsync(cancellationToken);

        if (verification.IsFailure)
        {
            return Result.Failure<AuthSessionResponse>(verification.Error);
        }

        var existing = await _store.FindUserByPhoneAsync(challenge.Phone, cancellationToken);
        var isNewUser = existing is null;

        if (existing is null)
        {
            // Sign-up and sign-in are one flow. A separate "register" step would ask a user who has
            // already proved they hold the number to prove something else, and every extra screen
            // between a phone number and a working account costs conversions.
            var registration = User.Register(
                PhoneNumber.TryParse(challenge.Phone, string.Empty).Value,
                displayName: null);

            if (registration.IsFailure)
            {
                return Result.Failure<AuthSessionResponse>(registration.Error);
            }

            existing = registration.Value;
            _store.AddUser(existing);
            await _store.SaveChangesAsync(cancellationToken);
        }
        else if (!existing.IsActive)
        {
            return Error.Forbidden(
                "identity.user.suspended",
                "This account cannot sign in. Contact support.");
        }

        var session = await _sessions.StartSessionAsync(existing, command.DeviceId, cancellationToken);
        var user = await _sessions.DescribeAsync(existing, cancellationToken);

        return new AuthSessionResponse(
            session.Access.Value,
            session.RefreshSecret,
            session.Access.ExpiresInSeconds,
            "Bearer",
            user,
            isNewUser,
            isNewUser ? CompleteProfileStep : null);
    }
}
