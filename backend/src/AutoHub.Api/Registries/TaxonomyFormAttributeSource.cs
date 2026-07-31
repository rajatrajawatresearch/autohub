using AutoHub.Platform.Caching;
using AutoHub.Platform.Forms;
using AutoHub.Platform.Taxonomy;
using Microsoft.Extensions.Caching.Memory;

namespace AutoHub.Api.Registries;

/// <summary>
/// Feeds the form registry its option lists from the taxonomy registry.
/// </summary>
/// <remarks>
/// This is the wire between registry 2 and registry 1, and it is the reason adding CNG to
/// <c>fuel_type</c> changes the sell form without republishing it: options are resolved at serve
/// time, not baked in at publish. <c>EmptyFormAttributeSource</c> — which knows no codes and
/// therefore refuses to publish any form that references one — is the fallback this replaces.
/// <para>
/// It reads the store directly rather than going through <see cref="ITaxonomyRegistry"/> because
/// that interface answers per-category questions and this needs a flat lookup by code across the
/// whole registry. The snapshot is cached in process memory under the shared registry version, the
/// same mechanism <c>TaxonomyRegistry</c> uses — so a publish invalidates both at once, and neither
/// can serve a version the other has moved past.
/// </para>
/// </remarks>
internal sealed class TaxonomyFormAttributeSource : IFormAttributeSource
{
    private const string CacheKeyPrefix = "api:forms:attributes:v";

    private static readonly TimeSpan SnapshotTtl = TimeSpan.FromMinutes(30);

    private readonly ITaxonomyStore _store;
    private readonly IRegistryCache _registryCache;
    private readonly IMemoryCache _memory;

    public TaxonomyFormAttributeSource(
        ITaxonomyStore store,
        IRegistryCache registryCache,
        IMemoryCache memory)
    {
        _store = store;
        _registryCache = registryCache;
        _memory = memory;
    }

    public async Task<IReadOnlyDictionary<string, FormAttribute>> ResolveAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken)
    {
        var all = await LoadAsync(cancellationToken);

        var resolved = new Dictionary<string, FormAttribute>(codes.Count, StringComparer.Ordinal);

        foreach (var code in codes)
        {
            // Absent rather than null for an unknown code. The compiler treats absence as "the
            // taxonomy no longer defines this" and refuses to serve the form, which is the loud
            // failure an operator who retired an attribute needs to see.
            if (all.TryGetValue(code, out var attribute))
            {
                resolved[code] = attribute;
            }
        }

        return resolved;
    }

    public async Task<IReadOnlySet<string>> KnownCodesAsync(CancellationToken cancellationToken)
        => (await LoadAsync(cancellationToken)).Keys.ToHashSet(StringComparer.Ordinal);

    private async Task<Dictionary<string, FormAttribute>> LoadAsync(CancellationToken cancellationToken)
    {
        var version = await _registryCache.GetVersionAsync(RegistryNames.Taxonomy, cancellationToken);
        var key = CacheKeyPrefix + version.Value;

        if (_memory.TryGetValue(key, out Dictionary<string, FormAttribute>? cached) && cached is not null)
        {
            return cached;
        }

        var snapshot = await _store.LoadSnapshotAsync(cancellationToken);

        var byCode = new Dictionary<string, FormAttribute>(StringComparer.Ordinal);

        foreach (var definition in snapshot.Attributes)
        {
            byCode[definition.Code] = new FormAttribute(
                definition.Code,
                definition.Label,

                // The i18n key the mobile apps re-resolve against their offline bundle. Derived from
                // the code rather than stored, so a new attribute is translatable the moment it
                // exists instead of after somebody remembers to fill in a second column.
                $"attribute.{definition.Code}.label",
                definition.Unit,
                definition.Options
                    .Where(option => option.IsActive)
                    .OrderBy(option => option.SortOrder)
                    .ThenBy(option => option.Label, StringComparer.Ordinal)
                    .Select(option => new CompiledOption(option.Value, option.Label, option.SortOrder))
                    .ToList());
        }

        _memory.Set(key, byCode, SnapshotTtl);
        return byCode;
    }
}
