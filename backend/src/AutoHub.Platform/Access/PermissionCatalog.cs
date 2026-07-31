namespace AutoHub.Platform.Access;

/// <summary>One permission, declared by the code that checks it.</summary>
public sealed record PermissionDefinition
{
    public required PermissionCode Code { get; init; }

    /// <summary>Default English label for the admin matrix. Localised via the i18n registry.</summary>
    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>See <see cref="Permission.IsDangerous"/> — a UI and alerting signal, not a check.</summary>
    public bool IsDangerous { get; init; }

    public string Module => Code.Module;

    public static PermissionDefinition Declare(
        string code,
        string name,
        bool isDangerous = false,
        string? description = null)
        => new()
        {
            Code = PermissionCode.Declare(code),
            Name = name,
            IsDangerous = isDangerous,
            Description = description,
        };
}

/// <summary>
/// A module's declaration of the permissions its code checks.
/// </summary>
/// <remarks>
/// Each module contributes its own, so adding <c>auction.bid.retract</c> is a change in the auction
/// module rather than an edit to a central list that the auction team does not own and will forget.
/// </remarks>
public interface IPermissionContributor
{
    IReadOnlyList<PermissionDefinition> Permissions { get; }
}

/// <summary>
/// Every permission code the codebase actually checks.
/// </summary>
/// <remarks>
/// This is the one part of the RBAC registry that lives in code, and the reason is not laziness.
/// A permission exists because a line of code checks it; a row in the permissions table that no code
/// checks is a checkbox in the admin UI that grants nothing — which is strictly worse than no
/// checkbox, because an operator ticks it and believes access was granted. The catalog is therefore
/// the source of truth and the table is its projection.
/// <para>
/// The admin permission matrix renders from here rather than from a <c>SELECT</c>, so it is complete
/// on the day a module ships instead of on the day someone remembers to run a seed script.
/// <see cref="Reconcile"/> surfaces the drift between the two in both directions.
/// </para>
/// </remarks>
public sealed class PermissionCatalog
{
    private readonly Dictionary<string, PermissionDefinition> _byCode;

    public PermissionCatalog(IEnumerable<IPermissionContributor> contributors)
    {
        _byCode = new Dictionary<string, PermissionDefinition>(StringComparer.Ordinal);

        foreach (var definition in contributors.SelectMany(c => c.Permissions))
        {
            // Two modules claiming one code is a name collision that would let either module's
            // grant satisfy the other's check. Failing at startup is the only honest response;
            // last-one-wins would resolve it silently and differently per DI registration order.
            if (!_byCode.TryAdd(definition.Code.Value, definition))
            {
                throw new InvalidOperationException(
                    $"Permission '{definition.Code.Value}' is declared by more than one module.");
            }
        }

        All = _byCode.Values
            .OrderBy(d => d.Module, StringComparer.Ordinal)
            .ThenBy(d => d.Code.Value, StringComparer.Ordinal)
            .ToList();

        Codes = _byCode.Keys.ToHashSet(StringComparer.Ordinal);

        ByModule = All
            .GroupBy(d => d.Module, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, IReadOnlyList<PermissionDefinition> (g) => g.ToList(), StringComparer.Ordinal);
    }

    /// <summary>Every declared permission, grouped-friendly order: module then code.</summary>
    public IReadOnlyList<PermissionDefinition> All { get; }

    public IReadOnlySet<string> Codes { get; }

    /// <summary>The matrix's row groups, keyed by the code's first segment.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<PermissionDefinition>> ByModule { get; }

    public bool Contains(string code) => _byCode.ContainsKey(code);

    public PermissionDefinition? Find(string code)
        => _byCode.TryGetValue(code, out var definition) ? definition : null;

    /// <summary>
    /// Compares the catalog against what the database holds.
    /// </summary>
    /// <remarks>
    /// Run on boot. <see cref="PermissionCatalogDrift.Missing"/> is seedable — the row simply has not
    /// been created yet. <see cref="PermissionCatalogDrift.Orphaned"/> is the interesting one: a code
    /// in the table that no code checks means either a module was deleted and its grants were not, or
    /// somebody typed the code by hand. Both leave roles carrying permissions that do nothing, which
    /// is how an access review concludes that a team has more access than it does.
    /// </remarks>
    public PermissionCatalogDrift Reconcile(IEnumerable<string> persistedCodes)
    {
        var persisted = persistedCodes.ToHashSet(StringComparer.Ordinal);

        return new PermissionCatalogDrift(
            Codes.Except(persisted, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList(),
            persisted.Except(Codes, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList());
    }
}

/// <summary>The difference between what the code checks and what the database stores.</summary>
public sealed record PermissionCatalogDrift(
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> Orphaned)
{
    public bool IsClean => Missing.Count == 0 && Orphaned.Count == 0;
}
