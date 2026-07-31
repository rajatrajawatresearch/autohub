using System.Security.Cryptography;
using System.Text;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Modules.Identity.Domain;

/// <summary>
/// One issued refresh token: single-use, rotating, and a member of a family.
/// </summary>
/// <remarks>
/// The contract fixes the behaviour: "Refresh: 30 days, rotating, single-use, with reuse detection —
/// a replayed refresh token revokes the whole family and forces re-authentication."
/// <para>
/// <b>Why a family and not just a token.</b> Rotation alone detects nothing. If an attacker steals a
/// refresh token and uses it first, the legitimate client's next refresh presents a token that has
/// already been consumed — and the only correct reading of that is "one of these two is a thief, and
/// we cannot tell which". Revoking the entire <see cref="FamilyId"/> logs both out and forces an OTP,
/// which the real owner can pass and the attacker cannot. Revoking only the replayed token would
/// leave the thief holding the newest one.
/// </para>
/// <para>
/// <b>Why only a hash is stored.</b> The token is a bearer credential for thirty days. A database
/// dump that contains them is thirty days of silent account access; a dump of SHA-256 digests of
/// 256 bits of entropy is not brute-forceable and buys the attacker nothing. No salt is needed —
/// unlike a six-digit OTP, the input space is far past the reach of any precomputed table.
/// </para>
/// </remarks>
public sealed class RefreshToken : AggregateRoot
{
    private RefreshToken(
        Guid id,
        Guid userId,
        Guid familyId,
        string tokenHash,
        string? deviceId,
        string? issuedIp,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        DeviceId = deviceId;
        IssuedIp = issuedIp;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    private RefreshToken() { }

    public Guid UserId { get; private set; }

    /// <summary>
    /// The lineage this token belongs to — one sign-in on one device, however many rotations later.
    /// </summary>
    public Guid FamilyId { get; private set; }

    /// <summary>SHA-256, hex. See the remarks on the type for why the token itself is not here.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// From <c>X-Device-Id</c>. Binds the family to an install, so "sign out my other devices" is a
    /// query rather than a guess, and a family suddenly refreshing from a new install is a signal.
    /// </summary>
    public string? DeviceId { get; private set; }

    public string? IssuedIp { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>When this token was exchanged. Non-null means used; presenting it again is the attack.</summary>
    public DateTimeOffset? ConsumedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Machine code, e.g. <c>logout</c>, <c>reuse_detected</c>, <c>rotated</c>.</summary>
    public string? RevokedReason { get; private set; }

    /// <summary>The token issued in exchange. Lets the family be walked forwards from any member.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsUsable(DateTimeOffset now)
        => ConsumedAt is null && RevokedAt is null && now < ExpiresAt;

    /// <summary>
    /// Mints a token and returns the secret alongside it, exactly once.
    /// </summary>
    /// <remarks>
    /// The plaintext is a return value rather than a property because the aggregate must never be
    /// able to hand it out a second time — the only correct lifetime for it is "the response being
    /// written right now".
    /// </remarks>
    public static (RefreshToken Token, string Secret) Issue(
        Guid userId,
        Guid familyId,
        string? deviceId,
        string? issuedIp,
        DateTimeOffset now,
        TimeSpan lifetime)
    {
        // 256 bits, base64url. Long enough that guessing is not a threat model, and URL-safe so a
        // client that puts it in a query string during development does not corrupt it.
        var secret = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        var token = new RefreshToken(
            UuidV7.New(),
            userId,
            familyId,
            Hash(secret),
            deviceId,
            issuedIp,
            now,
            now.Add(lifetime));

        return (token, secret);
    }

    public void Consume(DateTimeOffset now, Guid replacedByTokenId)
    {
        ConsumedAt = now;
        ReplacedByTokenId = replacedByTokenId;
    }

    public void Revoke(DateTimeOffset now, string reason)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokedReason = reason;
    }

    public static string Hash(string secret)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
