using AutoHub.Platform.Localization;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Loads locales and translated strings.
/// </summary>
/// <remarks>
/// One locale at a time, one namespace at a time. The merge across the fallback chain — Marathi over
/// Hindi over English — happens in the registry rather than in SQL, because the chain is data that
/// changes when a language launches, and expressing it as a recursive query would move a product
/// decision into a query plan.
/// <para>
/// Untracked: bundles are cached per locale and namespace and served to every request on the node.
/// </para>
/// </remarks>
public sealed class EfI18nResourceStore : II18nResourceStore
{
    private readonly AutoHubDbContext _context;

    public EfI18nResourceStore(AutoHubDbContext context) => _context = context;

    public async Task<IReadOnlyList<I18nLocale>> GetActiveLocalesAsync(CancellationToken cancellationToken)
        => await _context.I18nLocales
            .AsNoTracking()
            .Where(locale => locale.IsActive)
            .OrderBy(locale => locale.SortOrder)
            .ThenBy(locale => locale.Code)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Active resources for exactly one locale and namespace.
    /// </summary>
    /// <remarks>
    /// Inactive rows are excluded here rather than filtered later, and the difference matters:
    /// deactivating a bad translation must make the key <i>fall back</i> to its parent locale, and a
    /// row that reaches the merge as an empty or withdrawn value would instead win and render
    /// nothing. An unlabelled button is worse than an English one.
    /// </remarks>
    public async Task<IReadOnlyList<I18nResource>> GetResourcesAsync(
        string locale,
        string @namespace,
        CancellationToken cancellationToken)
        => await _context.I18nResources
            .AsNoTracking()
            .Where(resource =>
                resource.Locale == locale
                && resource.Namespace == @namespace
                && resource.IsActive)
            .ToListAsync(cancellationToken);
}
