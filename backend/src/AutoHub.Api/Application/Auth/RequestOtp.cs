using System.Security.Cryptography;
using AutoHub.Api.Application.Behaviours;
using AutoHub.Api.Security;
using AutoHub.Modules.Identity.Contracts;
using AutoHub.Modules.Identity.Domain;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace AutoHub.Api.Application.Auth;

/// <summary>
/// <c>POST /auth/otp/request</c>.
/// </summary>
/// <remarks>
/// <see cref="Purpose"/> is a free string, not an enum, because which purposes exist — and which SMS
/// template and throttle bucket each one uses — is notification-registry data. The endpoint only has
/// to carry it through.
/// </remarks>
internal sealed record RequestOtpCommand(string Phone, string? Purpose, string? Channel)
    : IRequest<Result<OtpChallengeResponse>>, ITransactionalRequest;

internal sealed class RequestOtpValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpValidator()
    {
        // Length only. Whether the number is well-formed is PhoneNumber's job and it produces a
        // better error than a regex here could; duplicating the pattern would guarantee the two
        // disagree eventually, and the one that runs first would win.
        RuleFor(command => command.Phone)
            .NotEmpty().WithErrorCode("identity.phone.required")
            .MaximumLength(20).WithErrorCode("identity.phone.malformed");

        RuleFor(command => command.Purpose)
            .MaximumLength(32).WithErrorCode("identity.otp.purpose_invalid");
    }
}

internal sealed class RequestOtpHandler : IRequestHandler<RequestOtpCommand, Result<OtpChallengeResponse>>
{
    /// <summary>Used when the caller does not say why. Login is the overwhelming majority.</summary>
    private const string DefaultPurpose = "login";

    /// <summary>
    /// India's calling code, as the default for a national number typed without one.
    /// </summary>
    /// <remarks>
    /// Configuration would be more correct and is deferred deliberately: it is one value, it is read
    /// in one place, and inventing a settings section for it before a second country exists is
    /// ceremony. The value object already takes it as a parameter, so the change is this line.
    /// </remarks>
    private const string DefaultCallingCode = "+91";

    private readonly IIdentityStore _store;
    private readonly IOtpDeliveryChannel _delivery;
    private readonly IClock _clock;
    private readonly OtpOptions _options;
    private readonly IHostEnvironment _environment;

    public RequestOtpHandler(
        IIdentityStore store,
        IOtpDeliveryChannel delivery,
        IClock clock,
        IOptions<OtpOptions> options,
        IHostEnvironment environment)
    {
        _store = store;
        _delivery = delivery;
        _clock = clock;
        _options = options.Value;
        _environment = environment;
    }

    public async Task<Result<OtpChallengeResponse>> Handle(
        RequestOtpCommand command,
        CancellationToken cancellationToken)
    {
        var phone = PhoneNumber.TryParse(command.Phone, DefaultCallingCode);
        if (phone.IsFailure)
        {
            return Result.Failure<OtpChallengeResponse>(phone.Error);
        }

        var now = _clock.UtcNow;

        var recent = await _store.CountChallengesSinceAsync(
            phone.Value.Value,
            now - _options.ThrottleWindow,
            cancellationToken);

        if (recent >= _options.MaxChallengesPerWindow)
        {
            // Per number, not per caller. The person being harassed is identified by the number, and
            // an attacker who rotates IP addresses to keep someone's phone buzzing all night is the
            // scenario this stops. It is also the scenario where our SMS bill is the second victim.
            return Error.Rule(
                "identity.otp.throttled",
                "Too many codes requested for this number. Try again later.")
                .With("retryAfterSeconds", (int)_options.ThrottleWindow.TotalSeconds);
        }

        var code = GenerateCode(_options.CodeLength);
        var purpose = string.IsNullOrWhiteSpace(command.Purpose) ? DefaultPurpose : command.Purpose;
        var channel = string.IsNullOrWhiteSpace(command.Channel) ? _delivery.Channel : command.Channel;

        var challenge = OtpChallenge.Issue(
            phone.Value,
            purpose,
            channel,
            code,
            _options.MaxAttempts,
            now,
            _options.Lifetime,
            _options.ResendCooldown);

        _store.AddChallenge(challenge);
        await _store.SaveChangesAsync(cancellationToken);

        var delivered = await _delivery.SendAsync(phone.Value.Value, code, purpose, cancellationToken);
        if (delivered.IsFailure)
        {
            return Result.Failure<OtpChallengeResponse>(delivered.Error);
        }

        return new OtpChallengeResponse(
            challenge.Id.ToString(),
            (int)_options.Lifetime.TotalSeconds,
            (int)_options.ResendCooldown.TotalSeconds,
            phone.Value.ToMaskedDisplay(),
            channel,
            _options.MaxAttempts,
            RevealCode(code));
    }

    /// <summary>
    /// Returns the code in the response in Development, and nowhere else.
    /// </summary>
    /// <remarks>
    /// <b>The environment check is in code and cannot be reached by configuration.</b> A production
    /// deployment that sets <c>Otp:ReturnCodeInResponse</c> — by accident, by a copied settings
    /// file, or by an attacker who reaches the config store — still gets null here, because
    /// <c>IHostEnvironment.IsDevelopment()</c> is decided by <c>ASPNETCORE_ENVIRONMENT</c> at
    /// process start and no setting overrides it. The flag can only ever turn the affordance
    /// <i>off</i> within Development, never on outside it.
    /// <para>
    /// It exists because the alternative developers reach for is a fixed OTP accepted everywhere,
    /// and that ships. An affordance that is structurally incapable of leaving Development is
    /// strictly safer than the workaround it prevents.
    /// </para>
    /// </remarks>
    private string? RevealCode(string code)
        => _environment.IsDevelopment() && _options.ReturnCodeInResponse ? code : null;

    /// <summary>
    /// A uniformly distributed numeric code.
    /// </summary>
    /// <remarks>
    /// <c>RandomNumberGenerator</c>, never <c>Random</c>: a code drawn from a seeded PRNG is
    /// predictable to anyone who can observe two of them, which turns a one-in-a-million guess into
    /// arithmetic. <c>GetInt32</c> also rejects modulo bias, so no digit is more likely than another.
    /// </remarks>
    private static string GenerateCode(int length)
    {
        var digits = new char[length];

        for (var i = 0; i < length; i++)
        {
            digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(digits);
    }
}
