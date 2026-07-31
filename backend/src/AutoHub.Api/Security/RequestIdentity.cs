using System.Globalization;
using System.Security.Claims;
using AutoHub.SharedKernel.Security;

namespace AutoHub.Api.Security;

/// <summary>
/// The request's own facts — locale, active city, client — alongside who is asking.
/// </summary>
/// <remarks>
/// Separate from <see cref="ICurrentUser"/> because the SharedKernel's contract is deliberately
/// identity-only: it is consumed by the auditing interceptor and by background services that have a
/// user but no HTTP request. Locale and city are transport concerns and belong to the edge.
/// <para>
/// Everything here comes from headers or from validated token claims, never from a request body.
/// A city a client can assert in a payload is a city a client can use to buy the Pune commission
/// rate on a Mumbai deal.
/// </para>
/// </remarks>
public interface IRequestContext
{
    /// <summary>BCP-47 from <c>Accept-Language</c>. Null means "use the registry's default".</summary>
    string? Locale { get; }

    /// <summary>From <c>X-Active-City</c>. Scopes search, pricing and feature evaluation.</summary>
    string? ActiveCityCode { get; }

    /// <summary>From <c>X-Client</c>, e.g. <c>web/0.1.0</c>. Feeds forced-upgrade and flag targeting.</summary>
    string? Client { get; }

    /// <summary>The W3C trace id, echoed into every problem response and every audit row.</summary>
    string TraceId { get; }
}

/// <summary>
/// Reads identity and request context off the current HTTP request.
/// </summary>
/// <remarks>
/// Registered scoped and resolved through <see cref="IHttpContextAccessor"/> rather than captured in
/// a constructor, because the outbox processor and the workflow sweeper resolve the same interfaces
/// from a scope that has no request. Returning nulls there is correct — "nobody was signed in" is a
/// value, not an error — and is exactly what <see cref="UnauthenticatedUser"/> exists to model.
/// </remarks>
internal sealed class HttpRequestIdentity : ICurrentUser, IRequestContext
{
    /// <summary>The claim carrying the user id. Standard <c>sub</c>, not a bespoke name.</summary>
    public const string SubjectClaim = "sub";

    /// <summary>
    /// Rate-limit tier. A claim rather than a lookup so the limiter never touches the database on a
    /// request it is about to reject; stale by at most one access-token lifetime, which is the point.
    /// </summary>
    public const string TierClaim = "tier";

    public const string DeviceHeader = "X-Device-Id";

    public const string ActiveCityHeader = "X-Active-City";

    public const string ClientHeader = "X-Client";

    private readonly IHttpContextAccessor _accessor;

    public HttpRequestIdentity(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirstValue(SubjectClaim)
                ?? _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, CultureInfo.InvariantCulture, out var id) ? id : null;
        }
    }

    public string? DeviceId => Header(DeviceHeader);

    /// <summary>
    /// The caller's address for the audit trail's "from where".
    /// </summary>
    /// <remarks>
    /// Read from the connection, not from <c>X-Forwarded-For</c>. Behind a proxy the correct fix is
    /// forwarded-headers middleware, which validates the chain against known proxies; trusting the
    /// header directly lets any client write whatever address it likes into the fraud record.
    /// </remarks>
    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? Locale
    {
        get
        {
            var header = _accessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();

            if (string.IsNullOrWhiteSpace(header))
            {
                return null;
            }

            // Only the first tag, and without its q-weight. Full content negotiation is a job for
            // the i18n registry's fallback chain, which already knows that mr-IN falls back to
            // hi-IN — reimplementing preference ordering here would give two different answers.
            var first = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            var tag = first?.Split(';', StringSplitOptions.TrimEntries).FirstOrDefault();

            return string.IsNullOrWhiteSpace(tag) || tag == "*" ? null : tag;
        }
    }

    public string? ActiveCityCode => Header(ActiveCityHeader);

    public string? Client => Header(ClientHeader);

    public string TraceId
        => System.Diagnostics.Activity.Current?.Id
           ?? _accessor.HttpContext?.TraceIdentifier
           ?? string.Empty;

    /// <summary>The tier the rate limiter partitions on. Anonymous callers share one bucket.</summary>
    public string Tier
        => _accessor.HttpContext?.User.FindFirstValue(TierClaim)
           ?? (UserId is null ? RateLimitTiers.Anonymous : RateLimitTiers.Authenticated);

    private string? Header(string name)
    {
        var value = _accessor.HttpContext?.Request.Headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

/// <summary>
/// The two tiers the platform can always name without asking anybody.
/// </summary>
/// <remarks>
/// Not a list of business tiers. "Gold dealer" and "enterprise partner" are RBAC and pricing
/// registry vocabulary and arrive as a token claim; these two are the states the transport itself
/// can distinguish — a caller we know, and a caller we do not — and every limit attached to any
/// tier, including these, is configuration.
/// </remarks>
public static class RateLimitTiers
{
    public const string Anonymous = "anonymous";

    public const string Authenticated = "authenticated";
}
