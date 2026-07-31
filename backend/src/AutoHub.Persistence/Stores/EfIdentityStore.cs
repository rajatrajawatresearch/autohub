using AutoHub.Modules.Identity.Contracts;
using AutoHub.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// The identity aggregates, read and written through the one context.
/// </summary>
/// <remarks>
/// Every lookup here is a single-row index seek by design — phone, challenge id, token digest — and
/// each of them sits on the auth path that every other request depends on. There is no query in this
/// file that can degrade into a scan as the table grows, which is the property that matters on the
/// table that will be the largest in the database.
/// </remarks>
public sealed class EfIdentityStore : IIdentityStore
{
    private readonly AutoHubDbContext _context;

    public EfIdentityStore(AutoHubDbContext context) => _context = context;

    public async Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken)
        => await _context.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<User?> FindUserByPhoneAsync(string phone, CancellationToken cancellationToken)
        => await _context.Users.FirstOrDefaultAsync(user => user.Phone == phone, cancellationToken);

    public void AddUser(User user) => _context.Users.Add(user);

    public void AddChallenge(OtpChallenge challenge) => _context.OtpChallenges.Add(challenge);

    public async Task<OtpChallenge?> FindChallengeAsync(Guid challengeId, CancellationToken cancellationToken)
        => await _context.OtpChallenges
            .FirstOrDefaultAsync(challenge => challenge.Id == challengeId, cancellationToken);

    public async Task<int> CountChallengesSinceAsync(
        string phone,
        DateTimeOffset since,
        CancellationToken cancellationToken)
        => await _context.OtpChallenges
            .CountAsync(
                challenge => challenge.Phone == phone && challenge.CreatedAt >= since,
                cancellationToken);

    public void AddRefreshToken(RefreshToken token) => _context.RefreshTokens.Add(token);

    public async Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
        => await _context.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    /// <summary>
    /// The whole lineage, tracked, because reuse detection is about to revoke all of it.
    /// </summary>
    /// <remarks>
    /// Bounded in practice by the refresh lifetime divided by the access lifetime — thirty days of
    /// fifteen-minute tokens is under three thousand rows even for a user who never closes the app,
    /// and the sweeper removes expired ones long before that.
    /// </remarks>
    public async Task<IReadOnlyList<RefreshToken>> GetTokenFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken)
        => await _context.RefreshTokens
            .Where(token => token.FamilyId == familyId)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
