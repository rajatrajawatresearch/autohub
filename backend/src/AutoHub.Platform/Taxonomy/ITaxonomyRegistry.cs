using System.Text.Json.Nodes;
using AutoHub.Platform.Caching;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// Registry #1. The answer to "what kinds of vehicle exist, and what do we know about each one?"
/// </summary>
/// <remarks>
/// Nothing in the product carries a list of fuel types, body styles, categories or makes. Every one
/// of those comes from here, which is what makes adding Construction Equipment — with a
/// <c>boom_length</c> attribute, a facet, a form field and a spec row — an afternoon of data entry
/// rather than a release.
/// <para>
/// Every response carries the registry <see cref="TaxonomyTree.Version"/> so the API can serve it as
/// an <c>ETag</c> and a mobile client can persist the body against it. Taxonomy is large, changes
/// rarely, and is needed before the first screen renders — exactly the profile that has to be
/// cacheable at the edge and on the device rather than re-fetched per session.
/// </para>
/// </remarks>
public interface ITaxonomyRegistry
{
    /// <summary>The category tree with each node's effective attribute codes. Drives every picker.</summary>
    Task<Result<TaxonomyTree>> GetTreeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The attribute definitions a category exposes, inherited from its ancestors.
    /// </summary>
    /// <remarks>
    /// Inheritance is resolved server-side and the client receives a flat list. A client that walked
    /// the tree itself would need the override rules, and three clients implementing the same
    /// precedence logic is three chances to disagree about whether a field is required.
    /// </remarks>
    Task<Result<CategoryAttributes>> GetAttributesForCategoryAsync(
        string categoryCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates a submitted attribute payload against a category, collecting every error.
    /// </summary>
    /// <remarks>
    /// The sell flow and the dealer bulk upload both land here, which is the point: a CSV must not
    /// be able to create a listing the sell form would have rejected. Resolving the category's
    /// effective attributes and running <see cref="AttributeValueValidator"/> are one call rather
    /// than two so no caller can do the first and forget the second.
    /// </remarks>
    Task<Result<IReadOnlyDictionary<string, TypedAttributeValue>>> ValidateAttributesAsync(
        string categoryCode,
        JsonObject payload,
        CancellationToken cancellationToken);

    /// <summary>Makes, optionally narrowed to those that actually sell into a category subtree.</summary>
    Task<Result<TaxonomyList<MakeView>>> GetMakesAsync(string? categoryCode, CancellationToken cancellationToken);

    Task<Result<TaxonomyList<ModelView>>> GetModelsAsync(string makeCode, CancellationToken cancellationToken);

    /// <summary>Variants of a model, optionally only those offered in a given model year.</summary>
    Task<Result<TaxonomyList<VariantView>>> GetVariantsAsync(
        string modelCode,
        int? year,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads stored attribute values back for a batch of entities, labelled and typed.
    /// </summary>
    /// <remarks>
    /// Batched because the caller is almost always a list page. Resolving twenty-four search results
    /// one entity at a time is twenty-four round trips to render one screen.
    /// <para>
    /// The labels are the point. The database holds <c>petrol</c>; the spec strip has to say
    /// "Petrol", and only this registry holds the option list that maps one to the other. Every
    /// caller doing that lookup itself is every caller reimplementing the registry.
    /// </para>
    /// </remarks>
    Task<Result<ResolvedAttributeValues>> ResolveAttributeValuesAsync(
        string entityType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Bumps the taxonomy's registry version, retiring every node's cached snapshot.
    /// </summary>
    /// <remarks>
    /// Called by the admin write path the moment anything in the taxonomy changes. Returns the new
    /// version so the caller can echo it — an operator who adds a category and cannot tell whether
    /// the change took effect will add it again.
    /// </remarks>
    Task<RegistryVersion> InvalidateAsync(CancellationToken cancellationToken);
}

/// <summary>A versioned list. Matches the <c>{ version, items }</c> envelope the contract publishes.</summary>
public sealed record TaxonomyList<T>(long Version, IReadOnlyList<T> Items);

public sealed record TaxonomyTree(long Version, IReadOnlyList<CategoryNode> Categories);

/// <summary>
/// One node of the category tree as the API publishes it.
/// </summary>
/// <remarks>
/// <see cref="Attributes"/> is a list of codes, not of definitions, because a tree with fifty nodes
/// each inlining fifteen attribute definitions is a several-hundred-kilobyte response for a screen
/// that renders a dropdown. The definitions are fetched once, for the category the user actually
/// picks, from <see cref="ITaxonomyRegistry.GetAttributesForCategoryAsync"/>.
/// </remarks>
public sealed record CategoryNode(
    string Code,
    string Name,
    string? Icon,
    int SortOrder,
    IReadOnlyList<CategoryNode> Children,
    IReadOnlyList<string> Attributes);

public sealed record CategoryAttributes(long Version, string CategoryCode, IReadOnlyList<AttributeView> Attributes);

/// <summary>
/// An attribute definition on the wire.
/// </summary>
/// <remarks>
/// Property names follow the published contract exactly — <c>required</c>, not <c>isRequired</c> —
/// because the frontend is already built against it. The enums are emitted through
/// <see cref="TaxonomyWireNames"/> rather than by a serializer naming policy, so a C# rename cannot
/// silently change the wire format.
/// </remarks>
public sealed record AttributeView(
    string Code,
    string Label,
    string DataType,
    string? Unit,
    bool Required,
    bool Searchable,
    string FacetStyle,
    string? DisplayGroup,
    int SortOrder,
    NumericRange? Range,
    IReadOnlyList<AttributeOptionView>? Options,
    TextRules? Text)
{
    public static AttributeView From(EffectiveAttribute attribute)
    {
        var definition = attribute.Definition;

        var range = definition.MinNumber is null && definition.MaxNumber is null && definition.Step is null
            ? null
            : new NumericRange(definition.MinNumber, definition.MaxNumber, definition.Step);

        var options = definition.RequiresOptions
            ? definition.Options
                .Where(o => o.IsActive)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Label, StringComparer.Ordinal)
                .Select(o => new AttributeOptionView(o.Value, o.Label, o.SortOrder))
                .ToList()
            : null;

        var text = definition.MinLength is null && definition.MaxLength is null && definition.Pattern is null
            ? null
            : new TextRules(definition.MinLength, definition.MaxLength, definition.Pattern);

        return new AttributeView(
            definition.Code,
            definition.Label,
            definition.DataType.ToWireName(),
            definition.Unit,
            attribute.IsRequired,
            attribute.IsSearchable,
            attribute.FacetStyle.ToWireName(),
            attribute.DisplayGroup,
            attribute.SortOrder,
            range,
            options,
            text);
    }
}

/// <summary>Bounds for an ordered attribute. For money these are minor units, like everywhere else.</summary>
public sealed record NumericRange(decimal? Min, decimal? Max, decimal? Step);

/// <summary>Length and format rules, so the same constraints validate on web, Flutter and the server.</summary>
public sealed record TextRules(int? MinLength, int? MaxLength, string? Pattern);

public sealed record AttributeOptionView(string Value, string Label, int SortOrder);

public sealed record MakeView(string Code, string Name, string? CountryOfOrigin, string? LogoKey);

/// <summary><c>Years</c> is <c>[first, last]</c>, matching the contract's <c>"years": [2005, 2026]</c>.</summary>
public sealed record ModelView(
    string Code,
    string Name,
    string MakeCode,
    string CategoryCode,
    IReadOnlyList<int> Years);

public sealed record VariantView(
    string Code,
    string Name,
    string ModelCode,
    string MakeCode,
    IReadOnlyList<int> Years,
    string? Fuel,
    string? Transmission,
    string? BodyType,
    int? Seats,
    int? EngineCc,
    decimal? PowerBhp);

public sealed record ResolvedAttributeValues(
    long Version,
    string EntityType,
    IReadOnlyDictionary<Guid, IReadOnlyList<ResolvedAttribute>> ByEntity);

/// <summary>
/// One stored value, with everything needed to render it.
/// </summary>
/// <remarks>
/// <see cref="DisplayValue"/> is the localisable label ("Petrol"); <see cref="Value"/> is the typed
/// datum the caller computes with. Both travel because the two are needed by different consumers of
/// the same response — the spec strip renders the label, the finance calculator reads the number.
/// </remarks>
public sealed record ResolvedAttribute(
    string Code,
    string Label,
    string DataType,
    string? Unit,
    string? DisplayGroup,
    int SortOrder,
    TypedAttributeValue Value,
    string? DisplayValue);

/// <summary>
/// Loads taxonomy rows. Implemented by the persistence layer.
/// </summary>
/// <remarks>
/// Declared here so <c>AutoHub.Platform</c> never references EF Core, which keeps the registry
/// unit-testable against an in-memory store — and attribute inheritance and validation are exactly
/// the kind of logic that wants a hundred cheap tests rather than three expensive ones.
/// </remarks>
public interface ITaxonomyStore
{
    /// <summary>
    /// Loads everything except variants.
    /// </summary>
    /// <remarks>
    /// Implementations must return only rows that are not soft-deleted, and must include inactive
    /// rows: a listing created last year still has to resolve the label of an option that has since
    /// been retired. Filtering for editorial visibility happens above this, per response.
    /// </remarks>
    Task<TaxonomySnapshot> LoadSnapshotAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleVariant>> GetVariantsAsync(Guid modelId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EntityAttributeValue>> GetAttributeValuesAsync(
        string entityType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken cancellationToken);
}
