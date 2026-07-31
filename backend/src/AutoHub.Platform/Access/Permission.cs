using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Access;

/// <summary>
/// One thing a user can be allowed to do.
/// </summary>
/// <remarks>
/// This row is what replaces <c>if (user.Role == "Dealer")</c> everywhere in the codebase. A role
/// check hardcodes today's org chart into a compiled assembly; the day operations invents "Verified
/// Dealer" or splits moderation between two teams, every such branch is wrong and only some of them
/// get found. A permission check keeps asking the same question — "may this actor do this?" — while
/// the answer moves to data.
/// <para>
/// Permissions are the one part of this registry that code owns rather than operators: a permission
/// exists because a line of code checks it. <see cref="PermissionCatalog"/> is therefore the source
/// of truth and these rows are its projection, kept in the database so roles can reference them by
/// id and so the admin UI can join to them.
/// </para>
/// </remarks>
public sealed class Permission : AggregateRoot, IAuditable
{
    private Permission(Guid id, string code, string module, string name, bool isDangerous, string? description)
        : base(id)
    {
        Code = code;
        Module = module;
        Name = name;
        IsDangerous = isDangerous;
        Description = description;
    }

    private Permission() { }

    /// <summary>The dotted code, e.g. <c>registry.pricing.publish</c>. Immutable once created.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// First segment of <see cref="Code"/>, stored so the admin matrix can group with an index
    /// instead of a string split, and derived from the code so the two can never disagree.
    /// </summary>
    public string Module { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>
    /// Marks a permission whose misuse is expensive or irreversible — publishing a pricing registry,
    /// approving a payout, deleting a dealer.
    /// </summary>
    /// <remarks>
    /// Consumed by the admin UI, which requires an extra confirmation, and by the audit pipeline,
    /// which alerts on grants rather than just recording them. It is a presentation and monitoring
    /// signal, never an authorisation one — a dangerous permission held is still a permission held,
    /// and a second check here would be a second place to forget.
    /// </remarks>
    public bool IsDangerous { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static Result<Permission> Create(
        string code,
        string name,
        bool isDangerous = false,
        string? description = null)
    {
        var parsed = PermissionCode.TryParse(code);
        if (parsed.IsFailure)
        {
            return Result.Failure<Permission>(parsed.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("permission.name.required", "A permission needs a human-readable name.");
        }

        return new Permission(
            UuidV7.New(),
            parsed.Value.Value,
            parsed.Value.Module,
            name.Trim(),
            isDangerous,
            description);
    }

    /// <summary>Materialises the code's declaration into a row the database and admin UI can join to.</summary>
    public static Permission FromDefinition(PermissionDefinition definition)
        => new(
            UuidV7.New(),
            definition.Code.Value,
            definition.Code.Module,
            definition.Name,
            definition.IsDangerous,
            definition.Description);

    /// <summary>
    /// Updates the descriptive fields. The code is never editable — renaming it would orphan every
    /// grant that references it while leaving the admin UI looking correct.
    /// </summary>
    public Result Describe(string name, string? description, bool isDangerous)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("permission.name.required", "A permission needs a human-readable name.");
        }

        Name = name.Trim();
        Description = description;
        IsDangerous = isDangerous;
        return Result.Success();
    }
}
