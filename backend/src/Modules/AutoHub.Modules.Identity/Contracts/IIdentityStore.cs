using AutoHub.Modules.Identity.Domain;

namespace AutoHub.Modules.Identity.Contracts;

/// <summary>
/// Reads and writes the identity aggregates.
/// </summary>
/// <remarks>
/// Declared in the module and implemented in the persistence layer, the same split every registry
/// store uses. It keeps the auth flow — which is the piece most worth testing, because getting reuse
/// detection wrong is a security bug rather than a bug — runnable against an in-memory list.
/// <para>
/// There is no <c>GetAllUsers</c> and no unbounded query. Every method here either names one row or
/// takes a bound, because the quality floor's "no unbounded queries reach production" is not a rule
/// that survives being optional on the table that will be the largest in the database.
/// </para>
/// </remarks>
public interface IIdentityStore
{
    Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<User?> FindUserByPhoneAsync(string phone, CancellationToken cancellationToken);

    void AddUser(User user);

    void AddChallenge(OtpChallenge challenge);

    Task<OtpChallenge?> FindChallengeAsync(Guid challengeId, CancellationToken cancellationToken);

    /// <summary>
    /// How many challenges this number has been sent since an instant.
    /// </summary>
    /// <remarks>
    /// The throttle that stops a phone number being used as somebody else's alarm clock, and stops
    /// our SMS spend being an attacker's denial-of-wallet. Counted per number rather than per IP
    /// because the victim is identified by the number, not by who asked.
    /// </remarks>
    Task<int> CountChallengesSinceAsync(
        string phone,
        DateTimeOffset since,
        CancellationToken cancellationToken);

    void AddRefreshToken(RefreshToken token);

    Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Every token in a lineage. Loaded whole so reuse detection can revoke all of it.</summary>
    Task<IReadOnlyList<RefreshToken>> GetTokenFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
