using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Wires the seeder into a host and runs it before the first request is served.
/// </summary>
/// <remarks>
/// Run inline at startup rather than as a hosted service, on purpose. A background seeder races the
/// first request: the API answers <c>/platform/taxonomy/tree</c> with an empty tree, the web app
/// caches that against an ETag, and a developer spends twenty minutes wondering why the categories
/// are missing from a database that visibly has them. Blocking startup for a second or two is the
/// cheaper trade.
/// </remarks>
public static class SeedingExtensions
{
    /// <summary>
    /// Registers the seeder. Call only from a Development host — the data is staged, not real.
    /// </summary>
    public static IServiceCollection AddDevelopmentDataSeeding(this IServiceCollection services)
    {
        services.TryAddScoped<DataSeeder>();
        return services;
    }

    /// <summary>
    /// Seeds now, in its own scope, and lets a failure take the host down.
    /// </summary>
    /// <remarks>
    /// The failure is deliberately fatal. Every way this can fail is a fault in authored seed data —
    /// a workflow that will not validate, a form referencing an attribute that does not exist, a
    /// guard no demo context satisfies — and all of them produce a half-populated database that
    /// behaves strangely later. Failing at boot, with the offending item named, is the only outcome
    /// that gets fixed on the spot.
    /// </remarks>
    public static async Task<IHost> SeedDevelopmentDataAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SeedingExtensions).FullName!);

        logger.LogInformation("Seeding development data…");

        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        var report = await seeder.RunAsync(cancellationToken);

        logger.LogInformation("Seeding finished: {Created} rows created across {Steps} registries.",
            report.TotalCreated, report.Steps.Count);

        return host;
    }
}
