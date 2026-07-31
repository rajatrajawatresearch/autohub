using AutoHub.Persistence;
using AutoHub.Platform.Caching;
using AutoHub.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AutoHub.Api.Application.Workflow;

/// <summary>
/// Turns <c>/{module}/{resource}/{id}/workflow</c> into a workflow key.
/// </summary>
/// <remarks>
/// The contract's workflow endpoints are generic — one route serves listings, deals, auction lots
/// and loan applications — which means the mapping from a URL to a workflow cannot be a
/// <c>switch</c> in this file. A new module would then need a code change to get a workflow
/// endpoint, and the whole point of the registry is that it does not.
/// <para>
/// So the mapping is derived from data: every published definition already declares its
/// <c>EntityType</c> as <c>{module}.{entity}</c>, and the route's module and resource segments are
/// matched against it. <c>/listings/listings/{id}/workflow</c> resolves through
/// <c>listings.listing</c>; <c>/auction/lots/{id}/workflow</c> through <c>auction.lot</c>.
/// </para>
/// <para>
/// The only convention baked in is that the URL is plural and the entity type is singular, which is
/// the naming rule the north star already fixes for both. When a module wants a resource name that
/// does not follow from its entity type, the honest fix is to publish the definition with the
/// entity type it wants matched — not to add a special case here.
/// </para>
/// </remarks>
internal sealed class WorkflowRouteResolver
{
    private const string CacheKeyPrefix = "api:workflow:routes:v";

    /// <summary>
    /// Backstop only. The real invalidation is the registry version in the key, so publishing a new
    /// workflow makes this entry unreachable rather than merely stale.
    /// </summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly AutoHubDbContext _context;
    private readonly IRegistryCache _registryCache;
    private readonly IMemoryCache _memory;

    public WorkflowRouteResolver(
        AutoHubDbContext context,
        IRegistryCache registryCache,
        IMemoryCache memory)
    {
        _context = context;
        _registryCache = registryCache;
        _memory = memory;
    }

    public async Task<Result<string>> ResolveAsync(
        string module,
        string resource,
        CancellationToken cancellationToken)
    {
        var routes = await LoadAsync(cancellationToken);

        foreach (var entityType in Candidates(module, resource))
        {
            if (routes.TryGetValue(entityType, out var workflowKey))
            {
                return workflowKey;
            }
        }

        return Error.NotFound(
            "workflow.route_unknown",
            $"No published workflow governs '{module}/{resource}'.")
            .With("module", module)
            .With("resource", resource);
    }

    /// <summary>
    /// Entity types this route could mean, most likely first.
    /// </summary>
    /// <remarks>
    /// Two conventions meet here and they disagree by one character. The north star fixes API paths
    /// as <b>kebab-case and plural</b> (<c>/trust/kyc-cases</c>) and database and code identifiers as
    /// <b>snake_case and singular</b> (<c>trust.kyc_case</c>). Translating between them is this
    /// method's whole job, and doing it here — rather than asking every module to publish a
    /// definition whose entity type happens to match a URL — keeps both conventions intact.
    /// </remarks>
    private static IEnumerable<string> Candidates(string module, string resource)
    {
        var singular = Singularise(resource);

        yield return $"{module}.{singular.Replace("-", "_", StringComparison.Ordinal)}";
        yield return $"{module}.{singular}";

        // A module whose entity type is itself plural — rare, and cheaper to tolerate than to
        // forbid, because forbidding it means a module cannot publish a workflow until somebody
        // renames its aggregate.
        yield return $"{module}.{resource.Replace("-", "_", StringComparison.Ordinal)}";
        yield return $"{module}.{resource}";
    }

    /// <summary>
    /// English plural to singular, for the handful of shapes a resource segment actually takes.
    /// </summary>
    /// <remarks>
    /// Deliberately not a pluralisation library. Resource names in this API are authored, lowercase,
    /// and drawn from a vocabulary of a few dozen nouns; a full inflector would add a dependency and
    /// a class of surprises ("data" → "datum") for a set of words we control.
    /// </remarks>
    private static string Singularise(string resource)
    {
        if (resource.EndsWith("ies", StringComparison.Ordinal) && resource.Length > 3)
        {
            return string.Concat(resource.AsSpan(0, resource.Length - 3), "y");
        }

        if (resource.EndsWith("ses", StringComparison.Ordinal)
            || resource.EndsWith("xes", StringComparison.Ordinal)
            || resource.EndsWith("ches", StringComparison.Ordinal)
            || resource.EndsWith("shes", StringComparison.Ordinal))
        {
            return resource[..^2];
        }

        return resource.EndsWith('s') ? resource[..^1] : resource;
    }

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken cancellationToken)
    {
        var version = await _registryCache.GetVersionAsync(RegistryNames.Workflow, cancellationToken);
        var key = CacheKeyPrefix + version.Value;

        if (_memory.TryGetValue(key, out Dictionary<string, string>? cached) && cached is not null)
        {
            return cached;
        }

        // Keys only — a few dozen rows of two short strings. The definitions themselves are loaded
        // by the engine, which needs their states and transitions; loading them here would pull the
        // whole registry into memory to answer a routing question.
        var rows = await _context.WorkflowDefinitions
            .AsNoTracking()
            .Where(definition => definition.IsPublished)
            .OrderByDescending(definition => definition.Version)
            .Select(definition => new { definition.EntityType, definition.Key })
            .ToListAsync(cancellationToken);

        var routes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            // Highest version first, so the newest published definition for an entity type wins.
            routes.TryAdd(row.EntityType, row.Key);
        }

        _memory.Set(key, routes, CacheTtl);
        return routes;
    }
}
