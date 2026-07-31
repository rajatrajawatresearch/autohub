using System.Globalization;
using System.Text;
using AutoHub.Platform.Features;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Loads the whole feature registry in two statements.
/// </summary>
/// <remarks>
/// Everything at once, because the registry is a few hundred rows and the alternative — a query per
/// flag — turns one page load into forty round trips for data that changes weekly.
/// <para>
/// Untracked and projected into descriptors: the result is cached, and an EF-tracked graph in a
/// cache is a change-tracker leak shared between threads.
/// </para>
/// </remarks>
public sealed class EfFeatureStore : IFeatureStore
{
    private readonly AutoHubDbContext _context;

    public EfFeatureStore(AutoHubDbContext context) => _context = context;

    public async Task<FeatureRegistryData> GetAsync(CancellationToken cancellationToken)
    {
        var modules = await _context.Modules
            .AsNoTracking()
            .OrderBy(module => module.SortOrder)
            .ThenBy(module => module.Code)
            .Select(module => new ModuleDescriptor(module.Code, module.IsEnabled, module.GatingFlagKey))
            .ToListAsync(cancellationToken);

        var flags = await _context.FeatureFlags
            .AsNoTracking()
            .OrderBy(flag => flag.Key)
            .Select(flag => new FlagDescriptor(
                flag.Key,
                flag.DefaultValue,
                flag.Rules
                    .OrderByDescending(rule => rule.Priority)
                    .Select(rule => new FlagRuleDescriptor(rule.Priority, rule.ConditionJson, rule.Value))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new FeatureRegistryData(Fingerprint(modules, flags), modules, flags);
    }

    /// <summary>
    /// A content fingerprint of the registry, served as its version.
    /// </summary>
    /// <remarks>
    /// A fingerprint rather than a counter, because there is no counter to read: flags and modules
    /// are edited row by row and nothing in this registry owns a generation. Clients compare it for
    /// equality to decide whether their cached flags are stale, which is all the contract asks of
    /// it — the number is never ordered and never means "newer than".
    /// <para>
    /// FNV-1a rather than <c>string.GetHashCode</c>, which is randomised per process in .NET: two
    /// API nodes holding identical data must produce the same version, or every client revalidates
    /// on every request that happens to land on the other node.
    /// </para>
    /// </remarks>
    private static int Fingerprint(
        IReadOnlyList<ModuleDescriptor> modules,
        IReadOnlyList<FlagDescriptor> flags)
    {
        var builder = new StringBuilder(512);

        foreach (var module in modules)
        {
            builder.Append(module.Code).Append('|')
                .Append(module.IsEnabled).Append('|')
                .Append(module.GatingFlagKey).Append(';');
        }

        foreach (var flag in flags)
        {
            builder.Append(flag.Key).Append('|').Append(flag.DefaultValue).Append('|');

            foreach (var rule in flag.Rules)
            {
                builder.Append(rule.Priority.ToString(CultureInfo.InvariantCulture)).Append(':')
                    .Append(rule.Value).Append(':')
                    .Append(rule.ConditionJson).Append(',');
            }

            builder.Append(';');
        }

        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;

        foreach (var b in Encoding.UTF8.GetBytes(builder.ToString()))
        {
            hash = (hash ^ b) * prime;
        }

        // Masked to 31 bits so the version is always positive: a negative version renders as
        // `X-Registry-Version: -1843…`, which every client that parses it as unsigned rejects.
        return (int)(hash & 0x7FFFFFFF);
    }
}
