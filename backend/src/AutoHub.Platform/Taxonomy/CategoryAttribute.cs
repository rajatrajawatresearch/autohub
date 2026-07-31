using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// Which attributes a category exposes, and how that category bends them.
/// </summary>
/// <remarks>
/// Attributes are shared and categories are many, so the relationship is a join rather than a column
/// on either side. <c>fuel_type</c> is defined once and attached to car, SUV, truck and three-wheeler;
/// redefining it per category would give four option lists that drift, and a search facet that
/// disagrees with itself depending on which category the user filtered by first.
/// <para>
/// <b>Attachment is inherited down the tree.</b> A row on <c>car</c> applies to <c>suv</c> and
/// <c>hatchback</c> without being repeated, which is what keeps a fifty-node taxonomy from needing
/// five hundred join rows. Resolution walks child → root and takes the nearest row for each
/// attribute, so a child can tighten what a parent set without the parent knowing.
/// </para>
/// <para>
/// <see cref="IsHidden"/> is the other half of that: inheritance without a way to opt out means the
/// first category that does not want a parent's attribute forces the parent's list to be duplicated
/// onto every sibling. One boolean avoids that entire failure mode.
/// </para>
/// </remarks>
public sealed class CategoryAttribute : Entity, IAuditable
{
    private CategoryAttribute(Guid id, Guid categoryId, Guid attributeDefinitionId) : base(id)
    {
        CategoryId = categoryId;
        AttributeDefinitionId = attributeDefinitionId;
    }

    private CategoryAttribute() { }

    public Guid CategoryId { get; private set; }

    public Guid AttributeDefinitionId { get; private set; }

    /// <summary>
    /// Overrides <see cref="AttributeDefinition.IsRequired"/> for this category and its descendants.
    /// Null keeps the definition's default. Odometer reading is mandatory on a car and absent on a
    /// trailer, and neither is the "wrong" default — they are different categories.
    /// </summary>
    public bool? IsRequiredOverride { get; private set; }

    /// <summary>Overrides <see cref="AttributeDefinition.IsSearchable"/>. Null keeps the default.</summary>
    public bool? IsSearchableOverride { get; private set; }

    /// <summary>
    /// Overrides <see cref="AttributeDefinition.FacetStyle"/>. Null keeps the default. The same
    /// attribute reads as chips where there are four options and as a list where there are forty.
    /// </summary>
    public FacetStyle? FacetStyleOverride { get; private set; }

    /// <summary>Overrides <see cref="AttributeDefinition.DisplayGroup"/>. Null keeps the default.</summary>
    public string? DisplayGroupOverride { get; private set; }

    /// <summary>Overrides <see cref="AttributeDefinition.SortOrder"/>. Null keeps the default.</summary>
    public int? SortOrderOverride { get; private set; }

    /// <summary>
    /// Suppresses an attribute this category would otherwise inherit. Resolution stops at the
    /// nearest row, so hiding on a child does not affect a sibling.
    /// </summary>
    public bool IsHidden { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static CategoryAttribute Attach(Guid categoryId, Guid attributeDefinitionId)
        => new(UuidV7.New(), categoryId, attributeDefinitionId);

    public static CategoryAttribute Hide(Guid categoryId, Guid attributeDefinitionId)
        => new(UuidV7.New(), categoryId, attributeDefinitionId) { IsHidden = true };

    public void Override(
        bool? isRequired = null,
        bool? isSearchable = null,
        FacetStyle? facetStyle = null,
        string? displayGroup = null,
        int? sortOrder = null)
    {
        IsRequiredOverride = isRequired;
        IsSearchableOverride = isSearchable;
        FacetStyleOverride = facetStyle;
        DisplayGroupOverride = displayGroup;
        SortOrderOverride = sortOrder;
    }

    /// <summary>Folds this row's overrides onto a definition to produce what the category actually sees.</summary>
    public EffectiveAttribute ApplyTo(AttributeDefinition definition, Guid owningCategoryId)
        => new(
            definition,
            IsRequiredOverride ?? definition.IsRequired,
            IsSearchableOverride ?? definition.IsSearchable,
            FacetStyleOverride ?? definition.FacetStyle,
            DisplayGroupOverride ?? definition.DisplayGroup,
            SortOrderOverride ?? definition.SortOrder,
            owningCategoryId);
}

/// <summary>
/// An attribute as one category actually sees it — definition plus the nearest overrides applied.
/// </summary>
/// <remarks>
/// Exists so the two consumers of a category's attribute list cannot disagree. The API response and
/// <see cref="AttributeValueValidator"/> both read this, which means a category that made an
/// attribute optional cannot end up with a form that says optional and a server that says required.
/// </remarks>
public sealed record EffectiveAttribute(
    AttributeDefinition Definition,
    bool IsRequired,
    bool IsSearchable,
    FacetStyle FacetStyle,
    string? DisplayGroup,
    int SortOrder,
    Guid InheritedFromCategoryId)
{
    public string Code => Definition.Code;

    public AttributeDataType DataType => Definition.DataType;
}
