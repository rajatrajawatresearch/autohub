using System.Globalization;
using System.Threading.RateLimiting;
using AutoHub.Api.Http;
using AutoHub.Api.Security;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AutoHub.Api.Extensions;

internal static class SecurityExtensions
{
    /// <summary>The origins the three portals run on in development.</summary>
    private const string CorsPolicy = "autohub-portals";

    /// <summary>RFC 9457's media type. Clients branch on it to decide whether to parse a problem.</summary>
    private const string ProblemContentType = "application/problem+json";

    public static WebApplicationBuilder AddApiSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));

        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        jwt.Validate();

        builder.Services.AddHttpContextAccessor();

        // One instance behind three interfaces. The auditing interceptor asks for ICurrentUser, the
        // endpoints ask for IRequestContext, and the rate limiter asks for the concrete type; a
        // second instance would read the same headers twice and, worse, could be registered with a
        // different lifetime and answer differently within one request.
        builder.Services.AddScoped<HttpRequestIdentity>();
        builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpRequestIdentity>());
        builder.Services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<HttpRequestIdentity>());

        builder.Services.AddSingleton<AccessTokenIssuer>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = AccessTokenIssuer.ValidationParameters(jwt);

                // Tokens travel to a browser over http://localhost in development. Demanding HTTPS
                // metadata there would make the whole auth flow untestable locally; anywhere else it
                // stays on, because a bearer token over plaintext is a bearer token in a proxy log.
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    // The framework's default 401 has an empty body. Clients branch on `code`, so an
                    // empty body is a client that cannot tell "your token expired, refresh it" from
                    // "the server is broken" — and the two demand opposite behaviour.
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        var error = Error.Unauthorized(
                            "identity.token.expired",
                            "Sign in again to continue.");

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = ProblemContentType;
                        await context.Response.WriteAsJsonAsync(
                            ApiProblem.Build(error, context.HttpContext));
                    },
                    OnForbidden = async context =>
                    {
                        var error = Error.Forbidden(
                            "access.permission_denied",
                            "You do not have permission to do this.");

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = ProblemContentType;
                        await context.Response.WriteAsJsonAsync(
                            ApiProblem.Build(error, context.HttpContext));
                    },
                };
            });

        builder.Services.AddAuthorization();

        // AddSingleton, not TryAddSingleton, and after AddAuthorization rather than before.
        // AddAuthorization already registers DefaultAuthorizationPolicyProvider, so a TryAdd here
        // silently stands aside and every permission-named policy resolves to nothing — which
        // surfaces as "The AuthorizationPolicy named 'registry.pricing.publish' was not found" on
        // the first admin request, long after the wiring was written.
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:3000", "http://localhost:3001", "http://localhost:3002"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            // The registry headers are the whole point of the caching contract, and a browser cannot
            // read a response header it was not told about. Without this, every portal refetches the
            // full taxonomy on every mount because it cannot see the ETag it was sent.
            .WithExposedHeaders(RegistryResponse.VersionHeader, "ETag", "Retry-After")
            .AllowCredentials()));

        builder.Services.Configure<RateLimitOptions>(
            builder.Configuration.GetSection(RateLimitOptions.SectionName));

        builder.Services.AddRateLimiter(ConfigureRateLimiter);

        return builder;
    }

    public static WebApplication UseApiSecurity(this WebApplication app)
    {
        app.UseCors(CorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    /// <summary>
    /// One partition per caller, with the permit count taken from the caller's tier.
    /// </summary>
    /// <remarks>
    /// <b>The tiers and their limits are configuration, not code.</b> "Gold dealers get 600 requests
    /// a minute" is a commercial decision that will change without a deploy, so the limiter reads a
    /// dictionary and the tier itself arrives as a token claim resolved from the RBAC and pricing
    /// registries. The only thing compiled in is the shape of the mechanism.
    /// <para>
    /// Partitioned by user id when there is one, by remote address when there is not. Partitioning
    /// anonymous callers by address is imperfect behind carrier-grade NAT — a whole town can share
    /// one — which is why the anonymous limit is a bucket sized for a town rather than a person, and
    /// why anything genuinely expensive requires a token.
    /// </para>
    /// </remarks>
    private static void ConfigureRateLimiter(RateLimiterOptions options)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var settings = context.RequestServices
                .GetRequiredService<IOptions<RateLimitOptions>>().Value;

            var identity = context.RequestServices.GetRequiredService<HttpRequestIdentity>();
            var tier = identity.Tier;

            var partitionKey = identity.UserId?.ToString()
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            var permits = settings.PermitsFor(tier);

            return RateLimitPartition.GetFixedWindowLimiter(
                $"{tier}:{partitionKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permits,
                    Window = settings.Window,

                    // No queue. A caller over their limit should be told immediately so their own
                    // backoff can work; holding their request open consumes a connection on both
                    // sides and turns a throttle into a latency spike.
                    QueueLimit = 0,
                });
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var window)
                ? (int)window.TotalSeconds
                : 60;

            context.HttpContext.Response.Headers.RetryAfter =
                retryAfter.ToString(CultureInfo.InvariantCulture);

            var error = Error.Rule("common.rate_limited", "Too many requests. Try again shortly.")
                .With("retryAfterSeconds", retryAfter);

            var problem = ApiProblem.Build(error, context.HttpContext);
            problem.Status = StatusCodes.Status429TooManyRequests;
            problem.Extensions["retryAfterSeconds"] = retryAfter;
            problem.Extensions["retryable"] = true;

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = ProblemContentType;
            await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        };
    }
}

/// <summary>
/// Per-tier request budgets.
/// </summary>
/// <remarks>
/// A dictionary keyed by tier name rather than a property per tier, because the set of tiers is
/// registry vocabulary and a property per tier would make "add a platinum tier" a code change.
/// </remarks>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Applied to a tier the configuration does not name. Deliberately modest.</summary>
    public int DefaultPermits { get; set; } = 120;

    /// <summary>Tier name to permits per <see cref="Window"/>. Bound from configuration.</summary>
    public Dictionary<string, int> Tiers { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [RateLimitTiers.Anonymous] = 60,
        [RateLimitTiers.Authenticated] = 300,
    };

    public int PermitsFor(string tier)
        => Tiers.TryGetValue(tier, out var permits) ? permits : DefaultPermits;
}
