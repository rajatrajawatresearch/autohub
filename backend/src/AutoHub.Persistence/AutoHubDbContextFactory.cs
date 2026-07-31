using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoHub.Persistence;

/// <summary>
/// Builds a context for <c>dotnet ef</c> without starting the API.
/// </summary>
/// <remarks>
/// The tooling's default is to boot the startup project's host and pull the context out of its
/// service provider. That works, and it means a migration cannot be scaffolded while the API is
/// mid-refactor, while an unrelated hosted service fails to construct, or before authentication is
/// wired at all. Migrations are the one thing that must never be blocked by the state of the
/// application, so this factory gives the tooling a context that depends on nothing but a
/// connection string.
/// <para>
/// It is a design-time type only. Nothing in the running application resolves it, and the credential
/// below is the local development one — a real environment supplies
/// <c>AUTOHUB_POSTGRES_CONNECTION</c>, and no deployed process ever runs this code path.
/// </para>
/// </remarks>
public sealed class AutoHubDbContextFactory : IDesignTimeDbContextFactory<AutoHubDbContext>
{
    private const string ConnectionVariable = "AUTOHUB_POSTGRES_CONNECTION";

    private const string LocalDevelopmentConnection =
        "Host=localhost;Port=5433;Database=autohub;Username=autohub;Password=autohub_dev";

    public AutoHubDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable)
            ?? LocalDevelopmentConnection;

        var options = new DbContextOptionsBuilder<AutoHubDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", Schemas.Platform))
            .Options;

        return new AutoHubDbContext(options);
    }
}
