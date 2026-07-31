using System.Globalization;
using System.Text.Json.Nodes;
using AutoHub.Platform.Caching;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Taxonomy;

/// <summary>How long a superseded taxonomy snapshot lingers in a node's memory.</summary>
public sealed class TaxonomyRegistryOptions
{
    /// <summary>
    /// Backstop lifetime for a snapshot entry.
    /// </summary>
    /// <remarks>
    /// Not the invalidation mechanism — a version bump makes the key unreachable long before this
    /// elapses. Its real job is to evict the entries for versions nobody will ask for again, so a
    /// week of registry edits does not accumulate a week of snapshots in a long-lived process.
    /// </remarks>
    public TimeSpan SnapshotTtl { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>How long one model's variant list lives. Held per model, not globally.</summary>
    public TimeSpan VariantTtl { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Serves the taxonomy from an in-process snapshot, keyed by the shared registry version.
/// </summary>
/// <remarks>
/// <b>Why this registry keeps its own memory cache instead of using <see cref="IRegistryCache"/>
/// for the payload.</b> That cache's second tier serialises values to Redis as JSON, which is right
/// for the registries whose payload is already a document — i18n bundles, feature flags, form
/// schemas. The taxonomy snapshot is not a document: it is a graph of domain entities with private
/// constructors, plus a dozen lookup dictionaries computed at construction. Round-tripping it
/// through JSON would cost more than the query it saves and would silently drop everything that is
/// computed rather than stored.
/// <para>
/// So the payload stays in process memory and only the <b>version</b> is shared. The version is part
/// of the cache key, which gives the same guarantee for free: an admin publishes on one node, the
/// version moves, and every other node's key becomes unreachable on its next read. No broadcast, no
/// pub/sub, no cache-coherency protocol to get wrong.
/// </para>
/// </remarks>
public sealed class TaxonomyRegistry : ITaxonomyRegistry
{
    private const string SnapshotKeyPrefix = "platform:taxonomy:snapshot:v";
    private const string VariantKeyPrefix = "platform:taxonomy:variants:v";

    /// <summary>
    /// One loader at a time, process-wide.
    /// </summary>
    /// <remarks>
    /// A cold cache is not a rare event: it happens on every deploy, every scale-out, and every
    /// registry publish. Without this gate the first burst of traffic after any of those runs the
    /// full snapshot query once per in-flight request, which is how a routine deployment becomes a
    /// database incident. The registry is scoped — it reads through a scoped store — so the gate has
    /// to be static to mean anything.
    /// </remarks>
    private static readonly SemaphoreSlim SnapshotGate = new(1, 1);

    private readonly ITaxonomyStore _store;
    private readonly IRegistryCache _registryCache;
    private readonly IMemoryCache _memory;
    private readonly IClock _clock;
    private readonly TaxonomyRegistryOptions _options;
    private readonly ILogger<TaxonomyRegistry> _logger;

    public TaxonomyRegistry(
        ITaxonomyStore store,
        IRegistryCache registryCache,
        IMemoryCache memory,
        IClock clock,
        TaxonomyRegistryOptions options,
        ILogger<TaxonomyRegistry> logger)
    {
        _store = store;
        _registryCache = registryCache;
        _memory = memory;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<TaxonomyTree>> GetTreeAsync(CancellationToken cancellationToken)
    {
        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);

        var categories = snapshot.Roots
            .Where(c => c.IsActive)
            .Select(root => BuildNode(snapshot, root))
            .ToList();

        return new TaxonomyTree(version, categories);
    }

    public async Task<Result<CategoryAttributes>> GetAttributesForCategoryAsync(
        string categoryCode,
        CancellationToken cancellationToken)
    {
        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);
        var category = snapshot.Category(categoryCode);

        if (category is null)
        {
            return CategoryNotFound(categoryCode);
        }

        var attributes = snapshot.EffectiveAttributesOf(category)
            .Select(AttributeView.From)
            .ToList();

        return new CategoryAttributes(version, category.Code, attributes);
    }

    public async Task<Result<CategoryAttributes>> GetSearchableAttributesAsync(
        CancellationToken cancellationToken)
    {
        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);

        // Presented as effective attributes with no category behind them: a definition's own
        // required/searchable/style settings, with none of the per-category overrides applied,
        // because there is no category to take them from. That is the honest projection for a
        // cross-category search, and the fields the facet rail reads — label, style, options —
        // come from the definition either way.
        var attributes = snapshot.Attributes
            .Where(definition => definition.IsSearchable)
            .OrderBy(definition => definition.SortOrder)
            .ThenBy(definition => definition.Code, StringComparer.Ordinal)
            .Select(definition => AttributeView.From(new EffectiveAttribute(
                definition,
                definition.IsRequired,
                definition.IsSearchable,
                definition.FacetStyle,
                definition.DisplayGroup,
                definition.SortOrder,
                Guid.Empty)))
            .ToList();

        // No category, because the caller has none — an unfiltered search spans every category and
        // its facets do too. The empty code says so rather than pretending one was resolved.
        return new CategoryAttributes(version, string.Empty, attributes);
    }

    public async Task<Result<IReadOnlyDictionary<string, TypedAttributeValue>>> ValidateAttributesAsync(
        string categoryCode,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        var (snapshot, _) = await GetSnapshotAsync(cancellationToken);
        var category = snapshot.Category(categoryCode);

        if (category is null)
        {
            return CategoryNotFound(categoryCode);
        }

        return AttributeValueValidator.ValidateAll(snapshot.EffectiveAttributesOf(category), payload);
    }

    public async Task<Result<TaxonomyList<MakeView>>> GetMakesAsync(
        string? categoryCode,
        CancellationToken cancellationToken)
    {
        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);

        IEnumerable<VehicleMake> makes;

        if (string.IsNullOrWhiteSpace(categoryCode))
        {
            // Unfiltered means the full marque list, including makes whose models have all been
            // retired — dealer inventory and historical listings still have to resolve them.
            makes = snapshot.Makes;
        }
        else
        {
            var category = snapshot.Category(categoryCode);

            if (category is null)
            {
                return Result.Failure<TaxonomyList<MakeView>>(CategoryNotFound(categoryCode));
            }

            // A make belongs to a category only through its models: Mahindra sells an SUV, a pickup
            // and a tractor, and pinning the make to one of those would make this query wrong.
            makes = snapshot
                .ModelsInCategories(snapshot.SubtreeIdsOf(category))
                .Where(m => m.IsActive)
                .Select(m => snapshot.Make(m.MakeId))
                .OfType<VehicleMake>();
        }

        var items = makes
            .Where(m => m.IsActive)
            .DistinctBy(m => m.Id)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name, StringComparer.Ordinal)
            .Select(m => new MakeView(m.Code, m.Name, m.CountryOfOrigin, m.LogoKey))
            .ToList();

        return new TaxonomyList<MakeView>(version, items);
    }

    public async Task<Result<TaxonomyList<ModelView>>> GetModelsAsync(
        string makeCode,
        CancellationToken cancellationToken)
    {
        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);
        var make = snapshot.Make(makeCode);

        if (make is null)
        {
            return Result.Failure<TaxonomyList<ModelView>>(
                Error.NotFound("taxonomy.make.not_found", $"No make with code '{makeCode}'.")
                    .With("makeCode", makeCode));
        }

        var currentYear = _clock.TodayInIst.Year;

        var items = snapshot.ModelsOfMake(make.Id)
            .Where(m => m.IsActive)
            .Select(m => new ModelView(
                m.Code,
                m.Name,
                make.Code,
                snapshot.Category(m.CategoryId)?.Code ?? string.Empty,
                YearSpan(m.ProductionFrom, m.ProductionTo, currentYear)))
            .ToList();

        return new TaxonomyList<ModelView>(version, items);
    }

    public async Task<Result<TaxonomyList<VariantView>>> GetVariantsAsync(
        string modelCode,
        int? year,
        CancellationToken cancellationToken)
    {
        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);
        var model = snapshot.Model(modelCode);

        if (model is null)
        {
            return Result.Failure<TaxonomyList<VariantView>>(
                Error.NotFound("taxonomy.model.not_found", $"No model with code '{modelCode}'.")
                    .With("modelCode", modelCode));
        }

        var make = snapshot.Make(model.MakeId);
        var variants = await GetVariantsOfModelAsync(model.Id, version, cancellationToken);
        var currentYear = _clock.TodayInIst.Year;

        // The year filter is applied here rather than in SQL so one cache entry per model serves
        // every year a user clicks through. A trim list is a few dozen rows; a round trip is not.
        var items = variants
            .Where(v => v.IsActive)
            .Where(v => year is null || v.CoversYear(year.Value))
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name, StringComparer.Ordinal)
            .Select(v => new VariantView(
                v.Code,
                v.Name,
                model.Code,
                make?.Code ?? string.Empty,
                YearSpan(v.YearFrom, v.YearTo, currentYear),
                v.FuelCode,
                v.TransmissionCode,
                v.BodyTypeCode,
                v.Seats,
                v.EngineCc,
                v.PowerBhp))
            .ToList();

        return new TaxonomyList<VariantView>(version, items);
    }

    public async Task<Result<ResolvedAttributeValues>> ResolveAttributeValuesAsync(
        string entityType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return Error.Validation("taxonomy.entity_type_required", "An entity type is required.");
        }

        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);

        if (entityIds.Count == 0)
        {
            return new ResolvedAttributeValues(
                version,
                entityType,
                new Dictionary<Guid, IReadOnlyList<ResolvedAttribute>>());
        }

        var rows = await _store.GetAttributeValuesAsync(entityType, entityIds, cancellationToken);
        var byEntity = new Dictionary<Guid, List<ResolvedAttribute>>();

        foreach (var row in rows)
        {
            var definition = snapshot.Attribute(row.AttributeDefinitionId);

            if (definition is null)
            {
                // The definition was hard-deleted out from under a value row. Skipping keeps the
                // page rendering; the log line is what makes the orphan findable.
                _logger.LogWarning(
                    "Attribute value {ValueId} references unknown definition {DefinitionId}",
                    row.Id, row.AttributeDefinitionId);
                continue;
            }

            var value = row.Read(definition);

            if (value.IsFailure)
            {
                _logger.LogWarning(
                    "Attribute value {ValueId} for {Code} is unreadable: {ErrorCode}",
                    row.Id, definition.Code, value.Error.Code);
                continue;
            }

            if (!byEntity.TryGetValue(row.EntityId, out var resolved))
            {
                resolved = [];
                byEntity[row.EntityId] = resolved;
            }

            resolved.Add(new ResolvedAttribute(
                definition.Code,
                definition.Label,
                definition.DataType.ToWireName(),
                definition.Unit,
                definition.DisplayGroup,
                definition.SortOrder,
                value.Value,
                DisplayValue(definition, value.Value)));
        }

        var ordered = new Dictionary<Guid, IReadOnlyList<ResolvedAttribute>>(byEntity.Count);

        foreach (var pair in byEntity)
        {
            ordered[pair.Key] = pair.Value
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Code, StringComparer.Ordinal)
                .ToList();
        }

        return new ResolvedAttributeValues(version, entityType, ordered);
    }

    public async Task<RegistryVersion> InvalidateAsync(CancellationToken cancellationToken)
    {
        var version = await _registryCache.InvalidateAsync(RegistryNames.Taxonomy, cancellationToken);

        _logger.LogInformation("Taxonomy registry invalidated; now {Version}", version);
        return version;
    }

    /// <summary>
    /// Turns a stored value into the label a human reads.
    /// </summary>
    /// <remarks>
    /// Only option types need it: the database holds <c>cng</c> and the spec strip has to say "CNG".
    /// Everything else is formatted at the edge, where the viewer's locale is known — the domain has
    /// no business deciding whether 5,49,000 has a comma after the five.
    /// </remarks>
    private static string? DisplayValue(AttributeDefinition definition, TypedAttributeValue value)
    {
        if (definition.DataType == AttributeDataType.Enum && value.Text is { } single)
        {
            return definition.FindOption(single)?.Label;
        }

        if (definition.DataType == AttributeDataType.MultiEnum && value.Choices.Count > 0)
        {
            var labels = value.Choices
                .Select(c => definition.FindOption(c)?.Label)
                .Where(l => l is not null);

            return string.Join(", ", labels);
        }

        return null;
    }

    /// <summary>
    /// <c>[first, last]</c>, with an open-ended range closed at the current year.
    /// </summary>
    /// <remarks>
    /// The year comes from <see cref="IClock"/> rather than <c>DateTime.UtcNow</c> so a year-boundary
    /// test is a test and not a wait, and so two API nodes cannot disagree about which model years a
    /// nameplate covers.
    /// </remarks>
    private static IReadOnlyList<int> YearSpan(int? from, int? to, int currentYear)
    {
        if (from is null && to is null)
        {
            return [];
        }

        var first = from ?? to!.Value;
        var last = to ?? Math.Max(first, currentYear);

        return [first, last];
    }

    private static CategoryNode BuildNode(TaxonomySnapshot snapshot, VehicleCategory category)
    {
        var children = snapshot.ChildrenOf(category.Id)
            .Where(c => c.IsActive)
            .Select(child => BuildNode(snapshot, child))
            .ToList();

        var attributes = snapshot.EffectiveAttributesOf(category)
            .Select(a => a.Code)
            .ToList();

        return new CategoryNode(category.Code, category.Name, category.Icon, category.SortOrder, children, attributes);
    }

    private static Error CategoryNotFound(string code)
        => Error.NotFound("taxonomy.category.not_found", $"No category with code '{code}'.")
            .With("categoryCode", code);

    private async Task<IReadOnlyList<VehicleVariant>> GetVariantsOfModelAsync(
        Guid modelId,
        long version,
        CancellationToken cancellationToken)
    {
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"{VariantKeyPrefix}{version}:{modelId:N}");

        if (_memory.TryGetValue(key, out IReadOnlyList<VehicleVariant>? cached) && cached is not null)
        {
            return cached;
        }

        var variants = await _store.GetVariantsAsync(modelId, cancellationToken);

        _memory.Set(key, variants, _options.VariantTtl);
        return variants;
    }

    private async Task<(TaxonomySnapshot Snapshot, long Version)> GetSnapshotAsync(
        CancellationToken cancellationToken)
    {
        var version = await _registryCache.GetVersionAsync(RegistryNames.Taxonomy, cancellationToken);
        var key = string.Create(CultureInfo.InvariantCulture, $"{SnapshotKeyPrefix}{version.Value}");

        if (_memory.TryGetValue(key, out TaxonomySnapshot? cached) && cached is not null)
        {
            return (cached, version.Value);
        }

        await SnapshotGate.WaitAsync(cancellationToken);
        try
        {
            // Re-check inside the gate: everyone who queued behind the first loader is now holding a
            // freshly populated cache and has no reason to load it again.
            if (_memory.TryGetValue(key, out cached) && cached is not null)
            {
                return (cached, version.Value);
            }

            var snapshot = await _store.LoadSnapshotAsync(cancellationToken);
            _memory.Set(key, snapshot, _options.SnapshotTtl);

            _logger.LogInformation("Taxonomy snapshot loaded at {Version}", version);
            return (snapshot, version.Value);
        }
        finally
        {
            SnapshotGate.Release();
        }
    }
}
