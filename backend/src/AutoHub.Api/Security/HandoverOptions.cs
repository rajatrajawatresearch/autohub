namespace AutoHub.Api.Security;

/// <summary>
/// The server-held key that makes a handover code useless on its own.
/// </summary>
/// <remarks>
/// The code presented at a vehicle handover is six digits — a million values — and a plain hash of a
/// six-digit number is a rainbow table somebody built years ago. Keying the hash means a leaked
/// database column gives up nothing without this secret, which is why it has no default: a built-in
/// one would be a key shared by every deployment of this code, and the column would be back to being
/// a lookup table.
/// <para>
/// Kept alongside <see cref="JwtOptions"/> rather than in the bookings module because it is a
/// deployment secret like the signing key, sourced the same way and validated at the same moment —
/// at boot, in a deploy, in front of an engineer, rather than at the kerb in front of a customer
/// whose car will not release.
/// </para>
/// </remarks>
public sealed class HandoverOptions
{
    public const string SectionName = "Bookings";

    public string HandoverSecret { get; set; } = string.Empty;

    /// <summary>Fails the host at boot rather than at the first handover.</summary>
    public string Validated()
    {
        if (string.IsNullOrWhiteSpace(HandoverSecret))
        {
            throw new InvalidOperationException(
                $"'{SectionName}:HandoverSecret' is not configured. The API will not start without one — "
                + "a built-in default would be a handover secret shared by every deployment of this code.");
        }

        // Shorter than the HMAC-SHA256 output is silently weakened by RFC 2104's padding rule, and
        // the cryptography library will accept it without complaint.
        if (System.Text.Encoding.UTF8.GetByteCount(HandoverSecret) < 32)
        {
            throw new InvalidOperationException(
                $"'{SectionName}:HandoverSecret' must be at least 32 bytes for HMAC-SHA256.");
        }

        return HandoverSecret;
    }
}
