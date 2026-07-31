using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Loads the taxonomy registry.
/// </summary>
/// <remarks>
/// <b>Everything here is untracked, and that is a correctness requirement rather than a
/// performance one.</b> The snapshot this returns is cached for the lifetime of the process and
/// handed to every concurrent request on the node. A tracked graph in that position keeps a scoped
/// <c>DbContext</c> alive long after its request ended, and hands the same change tracker to threads
/// that will each try to use it — which surfaces as an intermittent "a second operation was started
/// on this context" under load and never in development.
/// <para>
/// <b>Inactive rows are included; deleted rows are not.</b> A listing created last year still has to
/// resolve the label of an option that has since been retired — a spec strip that renders
/// <c>bs3</c> instead of "BS3" is a bug the seller sees. Editorial visibility is filtered per
/// response, above this layer. Soft-deleted rows are excluded by the model's global query filters,
/// so this store does not repeat the predicate and cannot forget it.
/// </para>
/// </remarks>
public sealed class EfTaxonomyStore : ITaxonomyStore
{
    private readonly AutoHubDbContext _context;

    public EfTaxonomyStore(AutoHubDbContext context) => _context = context;

    public async Task<TaxonomySnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        // Five statements rather than one join. These are independent sets that the snapshot indexes
        // in memory, and joining them would multiply every attribute row by every category that
        // exposes it before throwing the duplicates away.
        var categories = await _context.VehicleCategories
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Options travel with their definition: an enum attribute without its option list is a
        // dropdown a seller cannot complete and a facet with nothing in it.
        var attributes = await _context.AttributeDefinitions
            .Include(nameof(AttributeDefinition.Options))
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync(cancellationToken);

        var categoryAttributes = await _context.CategoryAttributes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var makes = await _context.VehicleMakes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var models = await _context.VehicleModels
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new TaxonomySnapshot(categories, attributes, categoryAttributes, makes, models);
    }

    /// <summary>
    /// Variants of one model.
    /// </summary>
    /// <remarks>
    /// Deliberately outside the snapshot. The Indian market has tens of thousands of trims and the
    /// number only grows, and nobody has ever asked for all of them — they are needed one model at a
    /// time, so they are fetched and cached one model at a time and the memory cost tracks the
    /// models people actually browse.
    /// </remarks>
    public async Task<IReadOnlyList<VehicleVariant>> GetVariantsAsync(
        Guid modelId,
        CancellationToken cancellationToken)
        => await _context.VehicleVariants
            .AsNoTracking()
            .Where(variant => variant.ModelId == modelId)
            .OrderBy(variant => variant.SortOrder)
            .ThenBy(variant => variant.Name)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Stored attribute values for a batch of entities.
    /// </summary>
    /// <remarks>
    /// Batched because the caller is almost always a list page: resolving twenty-four search results
    /// one entity at a time is twenty-four round trips to render one screen.
    /// </remarks>
    public async Task<IReadOnlyList<EntityAttributeValue>> GetAttributeValuesAsync(
        string entityType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken cancellationToken)
    {
        if (entityIds.Count == 0)
        {
            return [];
        }

        // ToArray so Npgsql sends one array parameter rather than expanding the ids into an IN list
        // that produces a distinct query plan per batch size and blows the plan cache.
        var ids = entityIds.ToArray();

        return await _context.EntityAttributeValues
            .AsNoTracking()
            .Where(value => value.EntityType == entityType && ids.Contains(value.EntityId))
            .ToListAsync(cancellationToken);
    }
}
