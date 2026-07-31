namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// The whole taxonomy, loaded once and answered from memory.
/// </summary>
/// <remarks>
/// <b>What is in here and why.</b> Categories, attribute definitions, their options, the
/// category-attribute joins, makes and models are a few thousand rows in total and are read on
/// literally every request that renders a picker, a facet rail or a spec sheet. Querying them
/// per-request would be a join per page view for data that changes a few times a month, which the
/// quality floor rules out outright: *"every registry read is cached with explicit invalidation;
/// never a per-request DB hit."*
/// <para>
/// <b>What is deliberately not in here.</b> Variants. The Indian market has tens of thousands of
/// trims and the number only grows, and they are only ever needed one model at a time — nobody has
/// ever asked for "all variants". They are fetched per model and cached per model, so the memory
/// cost tracks the models people actually browse rather than the catalogue's total size.
/// </para>
/// <para>
/// Every lookup a request needs is precomputed here rather than LINQ-scanned at call time. Resolving
/// a category's attributes walks the ancestry chain, and doing that with a linear scan per hop would
/// turn a nine-node tree into a few hundred comparisons on the hottest read path in the product.
/// </para>
/// </remarks>
public sealed class TaxonomySnapshot
{
    /// <summary>
    /// Depth ceiling for every tree walk.
    /// </summary>
    /// <remarks>
    /// A self-referencing table can hold a cycle — one bad admin re-parent is enough — and a cycle
    /// in an ancestry walk is an infinite loop on a request thread. The tree is built defensively so
    /// a data fault degrades into a category with a truncated attribute list, which someone notices
    /// and fixes, rather than a hung worker, which takes the site down.
    /// </remarks>
    private const int MaxTreeDepth = 16;

    private readonly Dictionary<Guid, VehicleCategory> _categoriesById;
    private readonly Dictionary<string, VehicleCategory> _categoriesByCode;
    private readonly Dictionary<Guid, List<VehicleCategory>> _childrenByParent;
    private readonly List<VehicleCategory> _roots;

    private readonly Dictionary<Guid, AttributeDefinition> _attributesById;
    private readonly Dictionary<string, AttributeDefinition> _attributesByCode;
    private readonly Dictionary<Guid, List<CategoryAttribute>> _categoryAttributesByCategory;

    private readonly Dictionary<Guid, VehicleMake> _makesById;
    private readonly Dictionary<string, VehicleMake> _makesByCode;

    private readonly Dictionary<string, VehicleModel> _modelsByCode;
    private readonly Dictionary<Guid, List<VehicleModel>> _modelsByMake;
    private readonly Dictionary<Guid, List<VehicleModel>> _modelsByCategory;

    public TaxonomySnapshot(
        IReadOnlyList<VehicleCategory> categories,
        IReadOnlyList<AttributeDefinition> attributes,
        IReadOnlyList<CategoryAttribute> categoryAttributes,
        IReadOnlyList<VehicleMake> makes,
        IReadOnlyList<VehicleModel> models)
    {
        _categoriesById = categories.ToDictionary(c => c.Id);
        // Codes are matched case-insensitively on the way in, because a query string is typed by a
        // human or pasted from a spreadsheet. They are stored lowercase, so this only ever forgives.
        _categoriesByCode = categories.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        _childrenByParent = [];
        _roots = [];

        foreach (var category in categories.OrderBy(c => c.SortOrder).ThenBy(c => c.Code, StringComparer.Ordinal))
        {
            // A category whose parent id points at a row that is not here — deleted, or filtered out
            // as inactive — is treated as a root rather than dropped. Losing a whole subtree because
            // one ancestor was deactivated is a far worse failure than showing it at the top level.
            if (category.ParentId is { } parentId && _categoriesById.ContainsKey(parentId))
            {
                if (!_childrenByParent.TryGetValue(parentId, out var siblings))
                {
                    siblings = [];
                    _childrenByParent[parentId] = siblings;
                }

                siblings.Add(category);
            }
            else
            {
                _roots.Add(category);
            }
        }

        _attributesById = attributes.ToDictionary(a => a.Id);
        _attributesByCode = attributes.ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase);

        _categoryAttributesByCategory = [];
        foreach (var link in categoryAttributes)
        {
            if (!_categoryAttributesByCategory.TryGetValue(link.CategoryId, out var links))
            {
                links = [];
                _categoryAttributesByCategory[link.CategoryId] = links;
            }

            links.Add(link);
        }

        _makesById = makes.ToDictionary(m => m.Id);
        _makesByCode = makes.ToDictionary(m => m.Code, StringComparer.OrdinalIgnoreCase);

        Makes = [.. makes.OrderBy(m => m.SortOrder).ThenBy(m => m.Name, StringComparer.Ordinal)];
        Attributes = attributes;

        _modelsByCode = models.ToDictionary(m => m.Code, StringComparer.OrdinalIgnoreCase);
        _modelsByMake = [];
        _modelsByCategory = [];

        foreach (var model in models.OrderBy(m => m.SortOrder).ThenBy(m => m.Name, StringComparer.Ordinal))
        {
            if (!_modelsByMake.TryGetValue(model.MakeId, out var byMake))
            {
                byMake = [];
                _modelsByMake[model.MakeId] = byMake;
            }

            byMake.Add(model);

            if (!_modelsByCategory.TryGetValue(model.CategoryId, out var byCategory))
            {
                byCategory = [];
                _modelsByCategory[model.CategoryId] = byCategory;
            }

            byCategory.Add(model);
        }
    }

    /// <summary>Top-level categories, in authored order.</summary>
    public IReadOnlyList<VehicleCategory> Roots => _roots;

    /// <summary>Every make, in authored order.</summary>
    public IReadOnlyList<VehicleMake> Makes { get; }

    /// <summary>Every attribute definition, whatever category it is attached to.</summary>
    public IReadOnlyList<AttributeDefinition> Attributes { get; }

    public VehicleCategory? Category(string code)
        => _categoriesByCode.GetValueOrDefault(code);

    public VehicleCategory? Category(Guid id)
        => _categoriesById.GetValueOrDefault(id);

    public IReadOnlyList<VehicleCategory> ChildrenOf(Guid categoryId)
        => _childrenByParent.TryGetValue(categoryId, out var children) ? children : [];

    public AttributeDefinition? Attribute(Guid id)
        => _attributesById.GetValueOrDefault(id);

    public AttributeDefinition? Attribute(string code)
        => _attributesByCode.GetValueOrDefault(code);

    public IReadOnlyList<CategoryAttribute> AttributeLinksOf(Guid categoryId)
        => _categoryAttributesByCategory.TryGetValue(categoryId, out var links) ? links : [];

    public VehicleMake? Make(string code) => _makesByCode.GetValueOrDefault(code);

    public VehicleMake? Make(Guid id) => _makesById.GetValueOrDefault(id);

    public VehicleModel? Model(string code) => _modelsByCode.GetValueOrDefault(code);

    public IReadOnlyList<VehicleModel> ModelsOfMake(Guid makeId)
        => _modelsByMake.TryGetValue(makeId, out var models) ? models : [];

    /// <summary>The chain from a category to its root, nearest first.</summary>
    /// <remarks>
    /// Nearest-first is what makes attribute resolution a single pass: the first row seen for an
    /// attribute wins, so a child's override naturally beats its parent's without a second sweep.
    /// </remarks>
    public IReadOnlyList<VehicleCategory> AncestryOf(VehicleCategory category)
    {
        var chain = new List<VehicleCategory>(4) { category };
        var visited = new HashSet<Guid> { category.Id };
        var current = category;

        while (chain.Count < MaxTreeDepth && current.ParentId is { } parentId)
        {
            if (!_categoriesById.TryGetValue(parentId, out var parent) || !visited.Add(parent.Id))
            {
                break;
            }

            chain.Add(parent);
            current = parent;
        }

        return chain;
    }

    /// <summary>Every category id at or below a node — what <c>?category=car</c> actually means.</summary>
    /// <remarks>
    /// Filtering by "car" has to include SUV and hatchback, or the first buyer who picks the parent
    /// category sees an empty result page and concludes the site is broken.
    /// </remarks>
    public IReadOnlyList<Guid> SubtreeIdsOf(VehicleCategory category)
    {
        var ids = new List<Guid> { category.Id };
        var visited = new HashSet<Guid> { category.Id };
        var frontier = new Queue<(VehicleCategory Node, int Depth)>();
        frontier.Enqueue((category, 0));

        while (frontier.Count > 0)
        {
            var (node, depth) = frontier.Dequeue();

            if (depth >= MaxTreeDepth)
            {
                continue;
            }

            foreach (var child in ChildrenOf(node.Id))
            {
                if (visited.Add(child.Id))
                {
                    ids.Add(child.Id);
                    frontier.Enqueue((child, depth + 1));
                }
            }
        }

        return ids;
    }

    /// <summary>Models sitting in any of the given categories, deduplicated and ordered.</summary>
    public IReadOnlyList<VehicleModel> ModelsInCategories(IReadOnlyCollection<Guid> categoryIds)
    {
        var models = new List<VehicleModel>();

        foreach (var categoryId in categoryIds)
        {
            if (_modelsByCategory.TryGetValue(categoryId, out var inCategory))
            {
                models.AddRange(inCategory);
            }
        }

        return models;
    }

    /// <summary>
    /// The attributes a category actually exposes, inherited from its ancestors, nearest row wins.
    /// </summary>
    /// <remarks>
    /// This is the single method the whole registry exists for. <c>fuel_type</c> is attached once to
    /// <c>car</c> and every subcategory under it inherits it; an SUV that wants it rendered as chips
    /// rather than a list adds one override row; a trailer that has no fuel adds one hidden row.
    /// None of those is a deployment.
    /// </remarks>
    public IReadOnlyList<EffectiveAttribute> EffectiveAttributesOf(VehicleCategory category)
    {
        var resolved = new Dictionary<Guid, EffectiveAttribute>();
        var settled = new HashSet<Guid>();

        foreach (var node in AncestryOf(category))
        {
            foreach (var link in AttributeLinksOf(node.Id))
            {
                // Nearest ancestor wins, including when the nearest one hides the attribute.
                if (!settled.Add(link.AttributeDefinitionId))
                {
                    continue;
                }

                if (link.IsHidden)
                {
                    continue;
                }

                var definition = Attribute(link.AttributeDefinitionId);

                if (definition is null || !definition.IsActive)
                {
                    continue;
                }

                resolved[link.AttributeDefinitionId] = link.ApplyTo(definition, node.Id);
            }
        }

        return [.. resolved.Values
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Code, StringComparer.Ordinal)];
    }
}
