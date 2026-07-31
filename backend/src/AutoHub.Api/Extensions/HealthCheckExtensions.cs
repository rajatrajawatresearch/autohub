using System.Text.Json;
using AutoHub.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace AutoHub.Api.Extensions;

internal static class HealthCheckExtensions
{
    /// <summary>Only checks tagged this way run on <c>/health/ready</c>.</summary>
    private const string ReadyTag = "ready";

    public static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: [ReadyTag])
            .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Degraded, tags: [ReadyTag]);

        return builder;
    }

    /// <summary>
    /// Two probes with deliberately different jobs.
    /// </summary>
    /// <remarks>
    /// <b>Liveness says nothing about dependencies</b>, and that is the point: a liveness check that
    /// fails when Postgres blips is a check that restarts every node at once, turning a recoverable
    /// database incident into a total outage. It answers one question — is this process able to
    /// serve a request at all — and the only correct response to it failing is a restart.
    /// <para>
    /// <b>Readiness does check dependencies</b>, because the only correct response to it failing is
    /// to stop sending traffic to this node while it recovers. Redis reports <c>Degraded</c> rather
    /// than <c>Unhealthy</c>: without it the registry cache falls back to process memory and the API
    /// still serves correct answers on one node, which is worth staying in the load balancer for.
    /// Postgres reports <c>Unhealthy</c>, because without it there is nothing to serve.
    /// </para>
    /// </remarks>
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
            .ExcludeFromDescription()
            .AllowAnonymous();

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
            ResponseWriter = WriteReadinessAsync,
        })
        .AllowAnonymous();

        return app;
    }

    private static async Task WriteReadinessAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = (int)report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    durationMs = (int)entry.Value.Duration.TotalMilliseconds,

                    // The exception message, never the exception. A stack trace on an unauthenticated
                    // endpoint tells an attacker the framework, the driver version and the file
                    // layout of the host.
                    description = entry.Value.Description ?? entry.Value.Exception?.Message,
                },
                StringComparer.Ordinal),
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, ReadinessJson));
    }

    private static readonly JsonSerializerOptions ReadinessJson = new(JsonSerializerDefaults.Web);
}

/// <summary>
/// Proves the database is reachable and the schema is the one this build expects.
/// </summary>
/// <remarks>
/// <c>CanConnectAsync</c> alone is not enough. A node that connects to a database missing this
/// build's migrations will accept traffic and fail every request with a column-not-found — which
/// looks like an application bug and is really a deploy that ran ahead of its migration. Checking
/// for pending migrations turns that into a node that never enters the load balancer.
/// </remarks>
internal sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly AutoHubDbContext _context;

    public PostgresHealthCheck(AutoHubDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _context.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Unhealthy("Postgres is not reachable.");
            }

            var pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            var outstanding = pending.ToList();

            return outstanding.Count == 0
                ? HealthCheckResult.Healthy("Postgres is reachable and up to date.")
                : HealthCheckResult.Unhealthy(
                    $"{outstanding.Count} migration(s) have not been applied: {string.Join(", ", outstanding)}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Postgres check failed.", ex);
        }
    }
}

/// <summary>
/// Pings Redis, and reports its absence as degraded rather than fatal.
/// </summary>
/// <remarks>
/// Resolves the multiplexer optionally, because a host configured without a Redis connection string
/// is a supported configuration — the registry cache is memory-only there and says so.
/// </remarks>
internal sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _redis;

    public RedisHealthCheck(IServiceProvider services)
        => _redis = services.GetService<IConnectionMultiplexer>();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_redis is null)
        {
            return HealthCheckResult.Degraded("No Redis configured; registry caching is per-node.");
        }

        try
        {
            var latency = await _redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis responded in {(int)latency.TotalMilliseconds}ms.");
        }
        catch (RedisException ex)
        {
            return HealthCheckResult.Degraded("Redis is unreachable; registry caching is per-node.", ex);
        }
    }
}
