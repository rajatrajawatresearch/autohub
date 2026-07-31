namespace AutoHub.Platform.Access;

/// <summary>
/// The permissions the platform layer itself checks — who may change the registries.
/// </summary>
/// <remarks>
/// These constants are technical vocabulary, not business configuration: each one exists because a
/// specific line of code compares against it, and the prime directive's ban is on business meaning
/// living in code, not on code naming the things it checks. What is configurable is which roles hold
/// them, in which scopes, for whom — and all of that is data.
/// <para>
/// Registry publishing is the most consequential capability in the product. Whoever holds
/// <see cref="PricingPublish"/> can change what every dealer in India is charged, and whoever holds
/// <see cref="WorkflowPublish"/> can remove the approval step before a payout. They are marked
/// dangerous so the admin UI demands a second confirmation and the audit pipeline alerts on the
/// grant rather than only recording it.
/// </para>
/// </remarks>
public static class PlatformPermissions
{
    public const string TaxonomyPublish = "registry.taxonomy.publish";

    public const string FormsPublish = "registry.forms.publish";

    public const string WorkflowPublish = "registry.workflow.publish";

    public const string PricingPublish = "registry.pricing.publish";

    public const string FeaturesManage = "registry.features.manage";

    public const string NotificationsPublish = "registry.notifications.publish";

    public const string IntegrationsManage = "registry.integrations.manage";

    public const string I18nPublish = "registry.i18n.publish";

    public const string GeographyManage = "registry.geo.manage";

    /// <summary>Reading the permission matrix. Separated from editing so audits need no write access.</summary>
    public const string AccessView = "access.permission.view";

    public const string RoleManage = "access.role.manage";

    public const string AssignmentManage = "access.assignment.manage";

    /// <summary>Flushing a registry cache by hand. Diagnostic; harmless but worth an audit row.</summary>
    public const string CacheInvalidate = "platform.cache.invalidate";
}

/// <summary>Declares <see cref="PlatformPermissions"/> to the catalog.</summary>
public sealed class PlatformPermissionContributor : IPermissionContributor
{
    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        PermissionDefinition.Declare(
            PlatformPermissions.TaxonomyPublish, "Publish taxonomy",
            description: "Publish a new version of categories, makes, models and attributes."),

        PermissionDefinition.Declare(
            PlatformPermissions.FormsPublish, "Publish form schemas",
            description: "Publish a form schema version to web, Android and iOS at once."),

        PermissionDefinition.Declare(
            PlatformPermissions.WorkflowPublish, "Publish workflows", isDangerous: true,
            description: "Change the states and approvals an entity must pass through."),

        PermissionDefinition.Declare(
            PlatformPermissions.PricingPublish, "Publish pricing rules", isDangerous: true,
            description: "Change commissions, slabs and fees the platform charges."),

        PermissionDefinition.Declare(
            PlatformPermissions.FeaturesManage, "Manage features and modules",
            description: "Enable modules and target feature flags."),

        PermissionDefinition.Declare(
            PlatformPermissions.NotificationsPublish, "Publish notification templates",
            description: "Edit templates per channel and locale."),

        PermissionDefinition.Declare(
            PlatformPermissions.IntegrationsManage, "Manage integrations", isDangerous: true,
            description: "Switch payment, SMS, KYC and valuation providers and their fallbacks."),

        PermissionDefinition.Declare(
            PlatformPermissions.I18nPublish, "Publish translations",
            description: "Publish user-facing strings for a locale."),

        PermissionDefinition.Declare(
            PlatformPermissions.GeographyManage, "Manage geography",
            description: "Open service cities, edit RTO offices and pincodes."),

        PermissionDefinition.Declare(
            PlatformPermissions.AccessView, "View roles and permissions",
            description: "Read the permission matrix and who holds what."),

        PermissionDefinition.Declare(
            PlatformPermissions.RoleManage, "Manage roles", isDangerous: true,
            description: "Create roles and change which permissions they carry."),

        PermissionDefinition.Declare(
            PlatformPermissions.AssignmentManage, "Assign roles", isDangerous: true,
            description: "Grant and revoke roles, with scope, for any user."),

        PermissionDefinition.Declare(
            PlatformPermissions.CacheInvalidate, "Invalidate registry caches",
            description: "Force a registry cache rebuild."),
    ];
}
