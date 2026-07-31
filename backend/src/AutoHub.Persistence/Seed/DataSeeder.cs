using AutoHub.Persistence.Seed.Demo;
using AutoHub.Platform.Caching;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoHub.Persistence.Seed;

/// <summary>
/// Fills an empty database with everything the platform needs to be demonstrable.
/// </summary>
/// <remarks>
/// <b>Order is a dependency graph, not a preference.</b> Forms are validated against attribute codes,
/// so taxonomy runs first. Cities reference states, which reference the country. Workflows must be
/// published before the demo data can transition anything through them. Each step therefore commits
/// before the next reads, and the sequence below is the graph flattened — reordering it produces a
/// startup failure naming the missing row, which is the correct outcome and not a subtle one.
/// <para>
/// <b>Every step is idempotent.</b> Rerunning against a seeded database creates nothing and reports
/// what it skipped. That matters more than it sounds: a developer restarts the API dozens of times a
/// day, and a seeder that either duplicates rows or throws on the second run is a seeder that gets
/// commented out within a week and rots.
/// </para>
/// <para>
/// <b>This is development-only, and the guard is at the call site rather than here.</b> Seed data is
/// staging for a demo — real dealers, real plates, fabricated transactions — and the one place it
/// must never appear is production. <see cref="SeedingExtensions"/> only wires it up when the host
/// environment is Development, so no configuration flag can accidentally arm it in production.
/// </para>
/// </remarks>
public sealed class DataSeeder
{
    private readonly AutoHubDbContext _context;
    private readonly IClock _clock;
    private readonly IRegistryCache _cache;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        AutoHubDbContext context,
        IClock clock,
        IRegistryCache cache,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _clock = clock;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Applies migrations that exist and have not run, then seeds. Returns what each step did.
    /// </summary>
    /// <remarks>
    /// Migrating here rather than expecting somebody to have run <c>dotnet ef</c> is deliberate for
    /// local development: the failure it prevents — a seeder writing into a schema that is one
    /// migration behind — surfaces as a column-not-found halfway through, leaving a partially seeded
    /// database that then has to be dropped.
    /// <para>
    /// The check for pending migrations is not an optimisation. <c>MigrateAsync</c> also compares the
    /// model against the last migration's snapshot and refuses to run when a developer has changed an
    /// entity without scaffolding a migration — which, on a codebase where several modules are being
    /// written at once, is a normal mid-afternoon state. Asking for pending migrations first means an
    /// unmigrated model in someone else's module does not stop the registries being seeded; when a
    /// migration genuinely is outstanding, <c>MigrateAsync</c> runs and that warning still fires,
    /// which is exactly when it is worth hearing.
    /// </para>
    /// </remarks>
    public async Task<SeedReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);

        if (pending.Any())
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }

        var steps = new List<SeedStepResult>();

        // Registries first, in dependency order.
        steps.Add(await new TaxonomySeeder(_context).SeedAsync(cancellationToken));
        steps.Add(await new GeographySeeder(_context).SeedAsync(cancellationToken));
        steps.Add(await new WorkflowSeeder(_context, _clock).SeedAsync(cancellationToken));
        steps.Add(await new PricingSeeder(_context, _clock).SeedAsync(cancellationToken));
        steps.Add(await new FormSeeder(_context, _clock).SeedAsync(cancellationToken));
        steps.Add(await new AccessSeeder(_context, _clock).SeedAsync(cancellationToken));
        steps.Add(await new FeatureSeeder(_context).SeedAsync(cancellationToken));
        steps.Add(await new NotificationSeeder(_context).SeedAsync(cancellationToken));
        steps.Add(await new IntegrationSeeder(_context).SeedAsync(cancellationToken));
        steps.Add(await new LocalizationSeeder(_context).SeedAsync(cancellationToken));

        // After taxonomy, because the baselines and applicability conditions are authored against
        // category and model codes that must already exist for a quote to resolve one.
        steps.Add(await new ValuationModelSeeder(_context, _clock).SeedAsync(cancellationToken));

        // Then the marketplace itself, which transitions through the workflows published above.
        steps.Add(await new DemoDataSeeder(_context, _clock.UtcNow, _logger).SeedAsync(cancellationToken));

        var report = new SeedReport(steps);

        foreach (var step in steps)
        {
            _logger.LogInformation("Seeded {Step}", step);
        }

        if (report.WasAlreadySeeded)
        {
            _logger.LogInformation("Nothing to seed — the database was already staged.");
        }
        else
        {
            await InvalidateRegistriesAsync(cancellationToken);
        }

        return report;
    }

    /// <summary>
    /// Bumps every registry's cache generation, because the seeder is a writer.
    /// </summary>
    /// <remarks>
    /// The quality floor is explicit that a registry read is cached with <b>explicit invalidation on
    /// write</b>, and this is a write — of all ten registries at once. Without it the seeder is the
    /// one writer in the platform that leaves the cache holding a previous generation's rows.
    /// <para>
    /// <b>The failure it fixes is specific and was a genuinely confusing hour.</b> The cache
    /// generation lives in Redis and the rows live in PostgreSQL, so dropping and reseeding the
    /// database — a routine local reset — leaves Redis holding the old generation number <i>and</i>
    /// the payload cached under it. The reseeded taxonomy lands on the same version, so the key
    /// collides and every code-to-id lookup resolves against the deleted database's ids. That does
    /// not present as a cache problem: it presents as a valuation refusing a vehicle whose category is
    /// visibly correct in the table two queries later.
    /// </para>
    /// <para>
    /// All ten rather than only the ones that reported rows. The registries reference each other — a
    /// form is validated against attribute codes, a workflow guard reads a taxonomy value — so a
    /// partial invalidation is a map from step names to registry names that somebody has to keep
    /// correct, to save ten writes that happen once at startup.
    /// </para>
    /// </remarks>
    private async Task InvalidateRegistriesAsync(CancellationToken cancellationToken)
    {
        foreach (var registry in RegistryNames.All)
        {
            await _cache.InvalidateAsync(registry, cancellationToken);
        }

        _logger.LogInformation(
            "Invalidated {Count} registry caches after seeding", RegistryNames.All.Count);
    }
}
