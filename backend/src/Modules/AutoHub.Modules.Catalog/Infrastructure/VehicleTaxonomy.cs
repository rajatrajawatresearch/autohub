using System.Globalization;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Catalog.Infrastructure;

/// <summary>
/// A flat, serialisable index of the taxonomy's codes and ids.
/// </summary>
/// <remarks>
/// Deliberately not <c>TaxonomySnapshot</c>. That type is a graph of domain entities with private
/// constructors and a dozen dictionaries computed at construction, which is right for the registry
/// that owns it and impossible to round-trip through the shared registry cache's JSON tier. This is
/// a few thousand string-to-id pairs — small enough to sit in Redis, so a cold node after a deploy
/// answers a code lookup without touching the database.
/// </remarks>
internal sealed record TaxonomyIndex(
    IReadOnlyDictionary<string, TaxonomyRef> CategoriesByCode,
    IReadOnlyDictionary<string, TaxonomyRef> MakesByCode,
    IReadOnlyDictionary<string, ModelRef> ModelsByCode,
    IReadOnlyDictionary<Guid, TaxonomyRef> ById);

/// <summary>A model plus the make and category it hangs off, so parentage can be checked in one hop.</summary>
internal sealed record ModelRef(Guid Id, string Code, string Name, Guid MakeId, Guid CategoryId);

/// <summary>A variant reduced to what classification needs. Held per model, never fleet-wide.</summary>
internal sealed record VariantRef(Guid Id, string Code, string Name);

/// <summary>
/// Resolves taxonomy codes to ids and back, out of a cached index.
/// </summary>
/// <remarks>
/// Reads through <see cref="ITaxonomyStore"/> — the platform's own store interface — rather than
/// through a second query of its own, so there is one definition of "which taxonomy rows exist" and
/// the soft-delete filter cannot be forgotten in one of two places.
/// <para>
/// Variants are fetched per model rather than indexed globally. The Indian market has tens of
/// thousands of trims and they are only ever needed one model at a time; holding all of them to
/// answer "is <c>vxi-amt</c> a Swift?" would be several megabytes per node for a question that
/// arrives with the model already known.
/// </para>
/// </remarks>
internal sealed class VehicleTaxonomy : IVehicleTaxonomy
{
    private const string IndexKey = "catalog:taxonomy-index";
    private const string VariantKeyPrefix = "catalog:variants:";

    private readonly ITaxonomyStore _store;
    private readonly IRegistryCache _cache;

    public VehicleTaxonomy(ITaxonomyStore store, IRegistryCache cache)
    {
        _store = store;
        _cache = cache;
    }

    public async Task<Result<TaxonomyRef>> ResolveCategoryAsync(
        string categoryCode,
        CancellationToken cancellationToken)
    {
        var index = await GetIndexAsync(cancellationToken);

        return index.CategoriesByCode.TryGetValue(categoryCode, out var category)
            ? category
            : NotFound("category", categoryCode);
    }

    public async Task<Result<VehicleClassification>> ResolveClassificationAsync(
        string categoryCode,
        string? makeCode,
        string? modelCode,
        string? variantCode,
        CancellationToken cancellationToken)
    {
        var index = await GetIndexAsync(cancellationToken);

        if (!index.CategoriesByCode.TryGetValue(categoryCode, out var category))
        {
            return NotFound("category", categoryCode);
        }

        TaxonomyRef? make = null;
        if (!string.IsNullOrWhiteSpace(makeCode))
        {
            if (!index.MakesByCode.TryGetValue(makeCode, out var resolvedMake))
            {
                return NotFound("make", makeCode);
            }

            make = resolvedMake;
        }

        ModelRef? model = null;
        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            if (!index.ModelsByCode.TryGetValue(modelCode, out var resolvedModel))
            {
                return NotFound("model", modelCode);
            }

            // A model that does not belong to the submitted make is a "Maruti Suzuki Nexon" — a row
            // no query flags and that quietly poisons every price statistic keyed on the pair.
            if (make is not null && resolvedModel.MakeId != make.Id)
            {
                return Mismatch("model", modelCode, "make", make.Code);
            }

            model = resolvedModel;

            // The make is derivable once the model is known, and deriving it is strictly better than
            // leaving it null: a caller who sent only a model still gets a complete classification.
            make ??= index.ById.GetValueOrDefault(resolvedModel.MakeId);
        }

        TaxonomyRef? variant = null;
        if (!string.IsNullOrWhiteSpace(variantCode))
        {
            if (model is null)
            {
                return Error.Validation(
                    "catalog.taxonomy.variant_needs_model",
                    "A variant can only be resolved against a model.")
                    .With("variantCode", variantCode);
            }

            var variants = await GetVariantsAsync(model.Id, cancellationToken);
            var match = variants.FirstOrDefault(v =>
                string.Equals(v.Code, variantCode, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                return Mismatch("variant", variantCode, "model", model.Code);
            }

            variant = new TaxonomyRef(match.Id, match.Code, match.Name);
        }

        return new VehicleClassification(
            category,
            make,
            model is null ? null : new TaxonomyRef(model.Id, model.Code, model.Name),
            variant);
    }

    public async Task<Result<TaxonomyFilters>> ResolveFiltersAsync(
        string? categoryCode,
        string? makeCode,
        string? modelCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(categoryCode)
            && string.IsNullOrWhiteSpace(makeCode)
            && string.IsNullOrWhiteSpace(modelCode))
        {
            // No filter is the common case on an admin search, and it must not cost a cache read.
            return TaxonomyFilters.None;
        }

        var index = await GetIndexAsync(cancellationToken);

        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            if (!index.CategoriesByCode.TryGetValue(categoryCode, out var category))
            {
                return NotFound("category", categoryCode);
            }

            categoryId = category.Id;
        }

        Guid? makeId = null;
        if (!string.IsNullOrWhiteSpace(makeCode))
        {
            if (!index.MakesByCode.TryGetValue(makeCode, out var make))
            {
                return NotFound("make", makeCode);
            }

            makeId = make.Id;
        }

        Guid? modelId = null;
        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            if (!index.ModelsByCode.TryGetValue(modelCode, out var model))
            {
                return NotFound("model", modelCode);
            }

            modelId = model.Id;
        }

        return new TaxonomyFilters(categoryId, makeId, modelId);
    }

    public async Task<Result<IReadOnlyDictionary<Guid, TaxonomyRef>>> DescribeAsync(
        IReadOnlyCollection<Guid> taxonomyIds,
        CancellationToken cancellationToken)
    {
        var index = await GetIndexAsync(cancellationToken);
        var described = new Dictionary<Guid, TaxonomyRef>(taxonomyIds.Count);

        foreach (var id in taxonomyIds)
        {
            // Ids that no longer resolve are skipped rather than failing the batch. A make deleted
            // out from under a five-year-old vehicle must not take a search results page down; the
            // caller renders what it has, which is what a user would expect to see.
            if (index.ById.TryGetValue(id, out var reference))
            {
                described[id] = reference;
            }
        }

        return described;
    }

    private async Task<TaxonomyIndex> GetIndexAsync(CancellationToken cancellationToken)
        => await _cache.GetOrLoadRequiredAsync(
            RegistryNames.Taxonomy,
            IndexKey,
            BuildIndexAsync,
            cancellationToken);

    private async Task<TaxonomyIndex> BuildIndexAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _store.LoadSnapshotAsync(cancellationToken);

        var categories = new Dictionary<string, TaxonomyRef>(StringComparer.OrdinalIgnoreCase);
        var makes = new Dictionary<string, TaxonomyRef>(StringComparer.OrdinalIgnoreCase);
        var models = new Dictionary<string, ModelRef>(StringComparer.OrdinalIgnoreCase);
        var byId = new Dictionary<Guid, TaxonomyRef>();

        foreach (var root in snapshot.Roots)
        {
            IndexCategory(snapshot, root, categories, byId, depth: 0);
        }

        foreach (var make in snapshot.Makes)
        {
            var reference = new TaxonomyRef(make.Id, make.Code, make.Name);
            makes[make.Code] = reference;
            byId[make.Id] = reference;

            foreach (var model in snapshot.ModelsOfMake(make.Id))
            {
                models[model.Code] = new ModelRef(model.Id, model.Code, model.Name, model.MakeId, model.CategoryId);
                byId[model.Id] = new TaxonomyRef(model.Id, model.Code, model.Name);
            }
        }

        return new TaxonomyIndex(categories, makes, models, byId);
    }

    /// <summary>
    /// Walks the category tree depth-first, with the same depth ceiling the snapshot uses.
    /// </summary>
    /// <remarks>
    /// A self-referencing table can hold a cycle — one mistaken re-parent is enough — and a cycle in
    /// a recursive walk is an infinite loop on whichever thread happens to be rebuilding the cache.
    /// Truncating produces a category somebody notices; not truncating produces an outage.
    /// </remarks>
    private static void IndexCategory(
        TaxonomySnapshot snapshot,
        VehicleCategory category,
        Dictionary<string, TaxonomyRef> categories,
        Dictionary<Guid, TaxonomyRef> byId,
        int depth)
    {
        if (depth > 16)
        {
            return;
        }

        var reference = new TaxonomyRef(category.Id, category.Code, category.Name);
        categories[category.Code] = reference;
        byId[category.Id] = reference;

        foreach (var child in snapshot.ChildrenOf(category.Id))
        {
            IndexCategory(snapshot, child, categories, byId, depth + 1);
        }
    }

    private async Task<IReadOnlyList<VariantRef>> GetVariantsAsync(Guid modelId, CancellationToken cancellationToken)
    {
        var key = string.Create(CultureInfo.InvariantCulture, $"{VariantKeyPrefix}{modelId:N}");

        return await _cache.GetOrLoadRequiredAsync<IReadOnlyList<VariantRef>>(
            RegistryNames.Taxonomy,
            key,
            async token =>
            {
                var variants = await _store.GetVariantsAsync(modelId, token);
                return [.. variants.Select(v => new VariantRef(v.Id, v.Code, v.Name))];
            },
            cancellationToken);
    }

    private static Error NotFound(string kind, string code)
        => Error.NotFound($"catalog.taxonomy.{kind}_not_found", $"No {kind} with code '{code}'.")
            .With($"{kind}Code", code);

    private static Error Mismatch(string kind, string code, string parentKind, string parentCode)
        => Error.Validation(
            $"catalog.taxonomy.{kind}_not_under_{parentKind}",
            $"'{code}' is not a {kind} of {parentKind} '{parentCode}'.")
            .With($"{kind}Code", code)
            .With($"{parentKind}Code", parentCode);
}
