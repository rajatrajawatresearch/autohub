using AutoHub.Platform.Access;

namespace AutoHub.Modules.Catalog.Infrastructure;

/// <summary>
/// The permissions the catalog's code checks.
/// </summary>
/// <remarks>
/// Technical vocabulary, in the same sense as <c>PlatformPermissions</c>: each constant exists
/// because a specific endpoint compares against it. What is configurable — and what a role check
/// would have hardcoded — is which roles hold them, over which cities and for which dealers.
/// <para>
/// Reading a vehicle is deliberately not a permission. A vehicle carries no personal data and no
/// price; it is a make, a model and a plate, and gating it would mean an anonymous valuation quote
/// needs an access token, which is exactly the friction §10 of the contract refuses.
/// </para>
/// </remarks>
public static class CatalogPermissions
{
    /// <summary>
    /// Editing a vehicle's attribute values. Held by dealers for their own stock and by staff.
    /// </summary>
    public const string VehicleEdit = "catalog.vehicle.edit";

    /// <summary>
    /// Moving a vehicle between taxonomy categories, which changes the attributes it may hold.
    /// </summary>
    /// <remarks>
    /// Dangerous because a recategorisation silently invalidates every stored attribute the new
    /// category does not define, and the values are not recoverable from the listing that showed
    /// them.
    /// </remarks>
    public const string VehicleRecategorise = "catalog.vehicle.recategorise";

    /// <summary>
    /// Resolving an identity conflict by merging two vehicle records.
    /// </summary>
    /// <remarks>
    /// The most consequential thing anyone can do in this module. Merging two vehicles merges their
    /// listings, their inspection reports and their deals, and there is no undo — so it is a
    /// separate permission from ordinary editing, and it is flagged dangerous.
    /// </remarks>
    public const string VehicleMerge = "catalog.vehicle.merge";
}

/// <summary>Declares <see cref="CatalogPermissions"/> to the RBAC catalog.</summary>
public sealed class CatalogPermissionContributor : IPermissionContributor
{
    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        PermissionDefinition.Declare(
            CatalogPermissions.VehicleEdit, "Edit vehicle details",
            description: "Change a vehicle's specification attributes."),

        PermissionDefinition.Declare(
            CatalogPermissions.VehicleRecategorise, "Recategorise a vehicle", isDangerous: true,
            description: "Move a vehicle to a different taxonomy category, changing which attributes it may hold."),

        PermissionDefinition.Declare(
            CatalogPermissions.VehicleMerge, "Merge duplicate vehicles", isDangerous: true,
            description: "Resolve an identity conflict by merging two vehicle records. There is no undo."),
    ];
}
