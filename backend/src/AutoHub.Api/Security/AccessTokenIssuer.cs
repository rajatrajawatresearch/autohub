using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoHub.Modules.Identity.Domain;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AutoHub.Api.Security;

/// <summary>
/// Mints the fifteen-minute access token.
/// </summary>
/// <remarks>
/// <b>The token carries identity, not authorisation.</b> No permission list, no role list. That is
/// the single most important decision in this file, and it is the contract's: "the client shows and
/// hides on permissions; the server enforces on permissions".
/// <para>
/// A permission baked into a token is a permission that survives its own revocation for the life of
/// the token, and the only fixes are a short enough lifetime that the product feels broken or a
/// revocation list that reintroduces the database lookup the embedding was meant to avoid. Resolving
/// permissions per request from the RBAC registry — which is Redis-cached and invalidated on write —
/// costs a cache read and makes a revoke take effect immediately.
/// </para>
/// <para>
/// The one authorisation-adjacent claim is <c>tier</c>, and it is there because the rate limiter has
/// to partition a request it is about to reject <i>before</i> anything touches a store.
/// </para>
/// </remarks>
internal sealed class AccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _credentials;

    public AccessTokenIssuer(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessToken Issue(User user, string? tier = null)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.Add(_options.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(HttpRequestIdentity.SubjectClaim, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, AutoHub.SharedKernel.Ids.UuidV7.New().ToString()),
            new(HttpRequestIdentity.TierClaim, tier ?? RateLimitTiers.Authenticated),
        };

        if (!string.IsNullOrWhiteSpace(user.ActiveCityCode))
        {
            claims.Add(new Claim("city", user.ActiveCityCode));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _credentials);

        return new AccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            (int)_options.AccessTokenLifetime.TotalSeconds);
    }

    /// <summary>The validation rules the bearer handler uses. Shared so the two cannot disagree.</summary>
    public static TokenValidationParameters ValidationParameters(JwtOptions options)
        => new()
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            ClockSkew = options.ClockSkew,

            // Without this the handler rewrites `sub` to the WS-Federation nameidentifier URI, and
            // every lookup of "sub" silently returns null. It is the single most common reason a
            // correctly issued token produces an authenticated principal with no user id.
            NameClaimType = HttpRequestIdentity.SubjectClaim,
        };

}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt, int ExpiresInSeconds);
