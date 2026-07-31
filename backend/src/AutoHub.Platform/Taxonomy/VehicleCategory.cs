using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// A node in the vehicle category tree — <c>car</c>, <c>suv</c>, <c>commercial</c>, <c>truck</c>.
/// </summary>
/// <remarks>
/// This is the entity the north star's acceptance test turns on: *"a new vehicle category, with its
/// own attributes, goes live without a deployment."* A category is therefore a row, and the set of
/// categories is never an enum, a lookup table baked into a migration, or a <c>switch</c>.
/// <para>
/// The tree is self-referencing rather than fixed-depth because the depth is genuinely unknown.
/// Today it is category → subcategory; the moment construction equipment arrives it is
/// equipment → earthmoving → excavator → long-reach, and a schema with a <c>subcategory_id</c>
/// column would need a migration to say so.
/// </para>
/// <para>
/// <b>Attributes are inherited down the tree</b> (see <see cref="CategoryAttribute"/>), which is what
/// stops every leaf from having to re-declare <c>fuel_type</c>. Reparenting a node therefore changes
/// which attributes its whole subtree exposes, which is why it is a deliberate operation and not a
/// settable property.
/// </para>
/// <para>
/// <see cref="IsActive"/> and soft deletion are different things and both are needed.
/// Deactivating hides a category from pickers and search while every existing listing under it keeps
/// resolving — the honest state for "we stopped taking new three-wheelers". Soft deletion is the
/// undo for a mistake. Conflating them means either losing history or never being able to retire
/// anything.
/// </para>
/// </remarks>
public sealed class VehicleCategory : AggregateRoot, IAuditable, ISoftDeletable
{
    private VehicleCategory(Guid id, string code, string name, Guid? parentId, string? icon, int sortOrder)
        : base(id)
    {
        Code = code;
        Name = name;
        ParentId = parentId;
        Icon = icon;
        SortOrder = sortOrder;
        IsActive = true;
    }

    private VehicleCategory() { }

    /// <summary>Stable slug used in URLs and search facets, e.g. <c>suv</c>. Never renamed.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Default English label. The user-facing label comes from the i18n registry.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Null for a root category. Points at another row in this table.</summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Icon key, not an icon. The design system owns the glyph; storing an SVG or a hex colour here
    /// would put presentation in the database, where it cannot be themed or dark-moded.
    /// </summary>
    public string? Icon { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>Editorial visibility. Inactive categories still resolve for existing listings.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<VehicleCategory> Create(
        string code,
        string name,
        Guid? parentId = null,
        string? icon = null,
        int sortOrder = 0)
    {
        var validCode = TaxonomyCode.Validate(code, "taxonomy.category.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<VehicleCategory>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("taxonomy.category.name_required", "A category needs a display name.");
        }

        return new VehicleCategory(UuidV7.New(), validCode.Value, name.Trim(), parentId, icon, sortOrder);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("taxonomy.category.name_required", "A category needs a display name.");
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void Reposition(string? icon, int sortOrder)
    {
        Icon = icon;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Moves the node under a new parent.
    /// </summary>
    /// <remarks>
    /// The one thing that must not happen — parenting a node under its own descendant — cannot be
    /// checked from a single entity, which only knows its parent's id. The cycle check belongs to
    /// whoever holds the whole tree, and <see cref="TaxonomySnapshot"/> re-checks it defensively on
    /// every load so a cycle written by a buggy admin path cannot hang a request thread.
    /// </remarks>
    public Result MoveTo(Guid? parentId)
    {
        if (parentId == Id)
        {
            return Error.Validation("taxonomy.category.self_parent", "A category cannot be its own parent.");
        }

        ParentId = parentId;
        return Result.Success();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
