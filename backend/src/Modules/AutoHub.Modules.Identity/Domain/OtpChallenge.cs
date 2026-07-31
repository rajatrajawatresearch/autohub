using System.Security.Cryptography;
using System.Text;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Identity.Domain;

/// <summary>
/// One outstanding "we texted you a code" and everything needed to judge the answer.
/// </summary>
/// <remarks>
/// The code is stored as a salted SHA-256 digest, never in the clear. An OTP is a credential for the
/// five minutes it lives, and a database dump — or a support engineer with read access — must not
/// be able to sign in as anybody who happens to be mid-login.
/// <para>
/// <b>Every refusal below is a real attack, not a hypothetical.</b> Unbounded attempts turn a
/// six-digit code into a one-million-guess brute force that finishes in seconds; a challenge that
/// can be verified twice lets a shoulder-surfed code be replayed after the owner has used it; a
/// challenge with no expiry is a credential that outlives the session it was minted for.
/// </para>
/// <para>
/// The attempt counter is incremented <i>before</i> the digest is compared, so a crash or a
/// cancelled request between the two cannot hand an attacker a free guess.
/// </para>
/// </remarks>
public sealed class OtpChallenge : AggregateRoot
{
    private OtpChallenge(
        Guid id,
        string phone,
        string purpose,
        string channel,
        string codeHash,
        string salt,
        int maxAttempts,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset resendAfter)
        : base(id)
    {
        Phone = phone;
        Purpose = purpose;
        Channel = channel;
        CodeHash = codeHash;
        Salt = salt;
        MaxAttempts = maxAttempts;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        ResendAfter = resendAfter;
    }

    private OtpChallenge() { }

    public string Phone { get; private set; } = string.Empty;

    /// <summary>
    /// <c>login</c>, <c>signup</c>, <c>step_up</c>. A string, not an enum: which purposes exist, and
    /// which SMS template and throttle bucket each uses, is notification-registry data.
    /// </summary>
    public string Purpose { get; private set; } = string.Empty;

    /// <summary>Which channel carried it — <c>sms</c>, <c>whatsapp</c>, <c>voice</c>.</summary>
    public string Channel { get; private set; } = string.Empty;

    public string CodeHash { get; private set; } = string.Empty;

    /// <summary>
    /// Per-challenge salt.
    /// </summary>
    /// <remarks>
    /// Without it, the digest of a six-digit code is a one-million-entry rainbow table that fits in
    /// memory, and the whole point of hashing evaporates.
    /// </remarks>
    public string Salt { get; private set; } = string.Empty;

    public int Attempts { get; private set; }

    public int MaxAttempts { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>When a resend is allowed. Stops a resend button from becoming an SMS bill.</summary>
    public DateTimeOffset ResendAfter { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    /// <summary>Set when the challenge is burned without succeeding — expired, or out of attempts.</summary>
    public DateTimeOffset? FailedAt { get; private set; }

    public bool IsOpen => ConsumedAt is null && FailedAt is null;

    public static OtpChallenge Issue(
        PhoneNumber phone,
        string purpose,
        string channel,
        string code,
        int maxAttempts,
        DateTimeOffset now,
        TimeSpan lifetime,
        TimeSpan resendCooldown)
    {
        var salt = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

        return new OtpChallenge(
            UuidV7.New(),
            phone.Value,
            purpose,
            channel,
            HashCode(code, salt),
            salt,
            maxAttempts,
            now,
            now.Add(lifetime),
            now.Add(resendCooldown));
    }

    /// <summary>
    /// Judges an answer and burns whatever it costs.
    /// </summary>
    /// <remarks>
    /// Returns a <see cref="Result"/> rather than a boolean so each refusal keeps its own machine
    /// code: a client that cannot tell "expired" from "wrong" from "locked out" cannot tell the user
    /// whether to retype or to request a new code, and the user gives up.
    /// </remarks>
    public Result Verify(string? code, DateTimeOffset now)
    {
        if (ConsumedAt is not null)
        {
            return Error.Conflict("identity.otp.already_used", "That code has already been used.");
        }

        if (FailedAt is not null)
        {
            return Error.Conflict("identity.otp.closed", "That code is no longer valid. Request a new one.");
        }

        if (now >= ExpiresAt)
        {
            FailedAt = now;
            return Error.Validation("identity.otp.expired", "That code has expired. Request a new one.");
        }

        if (Attempts >= MaxAttempts)
        {
            FailedAt = now;
            return Error.Validation("identity.otp.too_many_attempts", "Too many attempts. Request a new code.");
        }

        // Counted first, so an abandoned or cancelled request costs the attacker an attempt too.
        Attempts++;

        if (string.IsNullOrWhiteSpace(code) || !Matches(code))
        {
            if (Attempts >= MaxAttempts)
            {
                FailedAt = now;
            }

            return Error.Validation("identity.otp.incorrect", "That code is not right.")
                .With("attemptsRemaining", Math.Max(0, MaxAttempts - Attempts));
        }

        ConsumedAt = now;
        return Result.Success();
    }

    public bool CanResendAt(DateTimeOffset now) => now >= ResendAfter;

    private bool Matches(string code)
    {
        var candidate = Encoding.UTF8.GetBytes(HashCode(code, Salt));
        var expected = Encoding.UTF8.GetBytes(CodeHash);

        // Constant time. A comparison that returns early leaks how many leading characters were
        // right, which turns a million-guess brute force into sixty.
        return CryptographicOperations.FixedTimeEquals(candidate, expected);
    }

    private static string HashCode(string code, string salt)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{code}")));
}
