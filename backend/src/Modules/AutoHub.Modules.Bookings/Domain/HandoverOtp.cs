using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AutoHub.Modules.Bookings.Domain;

/// <summary>
/// Issues and checks the code presented at handover.
/// </summary>
/// <remarks>
/// Separated from <see cref="Booking"/> so the aggregate owns the policy — expiry, attempt cap,
/// single use — and this owns the cryptography. Mixing the two is how a test that needs a
/// deterministic code ends up weakening the generator.
/// </remarks>
public interface IHandoverOtp
{
    /// <summary>Generates a code and the hash to store. The plaintext is returned once and never kept.</summary>
    HandoverCode Issue(Guid bookingId);

    bool Matches(Guid bookingId, string presentedCode, string storedHash);
}

/// <summary>The plaintext to send and the hash to store. The plaintext must never be persisted.</summary>
public sealed record HandoverCode(string Code, string Hash);

/// <summary>
/// The default implementation: a six-digit code, HMAC-SHA256 hashed, compared in constant time.
/// </summary>
/// <remarks>
/// <b>Why a keyed hash and not a plain one.</b> The code space is a million values. A plain SHA-256
/// of a six-digit number is a rainbow table somebody has already built, so a leaked database column
/// would give up every live code instantly. Keying the hash with a server-held secret means the
/// column is useless without it, and the booking id is mixed in so the same code on two bookings
/// hashes differently.
/// <para>
/// <b>Why <see cref="RandomNumberGenerator"/> and not <c>Random</c>.</b> <c>Random</c> is seeded
/// predictably and its output is reproducible from a handful of observations. This code is the last
/// thing standing between a stranger and a vehicle.
/// </para>
/// <para>
/// <b>Why constant-time comparison.</b> An ordinary string comparison returns as soon as it finds a
/// difference, so the time it takes leaks how many leading digits were right. Six digits guessed one
/// position at a time is sixty attempts rather than a million, and the attempt cap on the aggregate
/// is set for the million.
/// </para>
/// </remarks>
public sealed class HandoverOtp : IHandoverOtp
{
    private readonly byte[] _key;

    /// <summary>
    /// Takes the server secret. Supplied from configuration, never from a constant.
    /// </summary>
    public HandoverOtp(string secret) => _key = Encoding.UTF8.GetBytes(secret);

    /// <summary>Six digits: what a person can read aloud over a phone without error.</summary>
    private const int Digits = 6;

    private const int UpperBound = 1_000_000;

    public HandoverCode Issue(Guid bookingId)
    {
        var value = RandomNumberGenerator.GetInt32(UpperBound);
        var code = value.ToString(CultureInfo.InvariantCulture).PadLeft(Digits, '0');

        return new HandoverCode(code, Hash(bookingId, code));
    }

    public bool Matches(Guid bookingId, string presentedCode, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(presentedCode))
        {
            return false;
        }

        var presented = Encoding.UTF8.GetBytes(Hash(bookingId, presentedCode.Trim()));
        var stored = Encoding.UTF8.GetBytes(storedHash);

        return CryptographicOperations.FixedTimeEquals(presented, stored);
    }

    private string Hash(Guid bookingId, string code)
    {
        var payload = Encoding.UTF8.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{bookingId:N}:{code}"));

        return Convert.ToHexString(HMACSHA256.HashData(_key, payload));
    }
}
