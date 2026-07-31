namespace AutoHub.Api.Security;

/// <summary>
/// How access tokens are signed and how long they last.
/// </summary>
/// <remarks>
/// <b>Symmetric signing, deliberately.</b> One process issues and validates these tokens today, so
/// an asymmetric key pair would add key distribution and a JWKS endpoint to solve a problem — "a
/// service that can verify but not mint" — that does not exist yet. When identity is extracted, the
/// change is this class and a JWKS document, not the endpoints.
/// </remarks>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The signing key. Must come from configuration; there is no default.
    /// </summary>
    /// <remarks>
    /// A default would be a key that is identical on every deployment of this codebase on earth, and
    /// therefore a key with which anyone can mint a token for anyone. Boot fails without it, loudly,
    /// which is the only acceptable behaviour — see <see cref="Validate"/>.
    /// </remarks>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "https://autohub.in";

    public string Audience { get; set; } = "autohub-api";

    /// <summary>
    /// Fifteen minutes, per the contract.
    /// </summary>
    /// <remarks>
    /// Short because an access token cannot be revoked — that is what makes it cheap to verify. The
    /// window between "access revoked" and "access actually gone" is exactly this number, and the
    /// refresh token, which is checked against the database on every rotation, is what closes it.
    /// </remarks>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Thirty days, per the contract. Rotating and single-use, so age is not exposure.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Tolerance for clock difference between issuer and validator.
    /// </summary>
    /// <remarks>
    /// Thirty seconds, not the framework's five-minute default. Five minutes of skew on a
    /// fifteen-minute token means a third of its life can be served by a revoked-but-not-yet-expired
    /// credential, which quietly undoes the reason the lifetime is short.
    /// </remarks>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Fails the host at boot rather than at first sign-in.
    /// </summary>
    /// <remarks>
    /// A misconfigured signing key that surfaces on the first login attempt surfaces in production,
    /// to a user, as a 500. Surfacing it at startup means it surfaces in a deploy, to an engineer.
    /// </remarks>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException(
                $"'{SectionName}:SigningKey' is not configured. The API will not start without one — "
                + "a built-in default would be a signing key shared by every deployment of this code.");
        }

        // HMAC-SHA256 keys shorter than the hash output are silently weakened by the padding rule
        // in RFC 2104, and the JWT library will happily accept one.
        if (System.Text.Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException(
                $"'{SectionName}:SigningKey' must be at least 32 bytes for HMAC-SHA256.");
        }
    }
}

/// <summary>
/// How the OTP challenge behaves — lifetime, length, throttles.
/// </summary>
/// <remarks>
/// Every number here is a fraud-versus-friction trade that operations will want to move without a
/// deployment, which is why it is configuration and not a constant. The one thing that is not
/// configurable is <see cref="ReturnCodeInResponse"/>, which is guarded by the environment rather
/// than by a setting — see its remarks.
/// </remarks>
public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int CodeLength { get; set; } = 6;

    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Five, per the contract's <c>attemptsAllowed</c>. See <c>OtpChallenge.Verify</c>.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>How many codes one number may be sent inside <see cref="ThrottleWindow"/>.</summary>
    public int MaxChallengesPerWindow { get; set; } = 5;

    public TimeSpan ThrottleWindow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Whether the response carries the code itself.
    /// </summary>
    /// <remarks>
    /// <b>This is a development affordance and it is fenced twice.</b> The value is only ever read
    /// after the host has confirmed it is running in the Development environment; a production
    /// deployment that sets it in configuration still gets <c>false</c>, because the environment
    /// check is in code and configuration cannot reach it.
    /// <para>
    /// Why it exists at all: without it, every developer and every automated end-to-end test has to
    /// read a code out of a log line, and the shortcut people invent when that is painful — a fixed
    /// OTP like 000000 accepted everywhere — is a backdoor that ships. An affordance that only
    /// exists in Development is strictly safer than the workaround it prevents.
    /// </para>
    /// </remarks>
    public bool ReturnCodeInResponse { get; set; } = true;
}
