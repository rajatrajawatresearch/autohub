namespace AutoHub.Platform.Caching;

/// <summary>
/// A registry's cache generation. Bumped on every write, embedded in every cache key.
/// </summary>
/// <remarks>
/// The contract in <c>docs/contracts/registry-api.md</c> requires every registry response to carry
/// <c>X-Registry-Version</c> and an <c>ETag</c> so a Flutter app on a train can hold a taxonomy
/// snapshot for a week and revalidate with one 304. This type is the single source of both, which is
/// what stops the header and the cache key drifting apart — a bug that presents as "the admin
/// changed it, the web app updated, the mobile app did not".
/// </remarks>
public readonly record struct RegistryVersion(string Registry, long Value)
{
    /// <summary>
    /// The strong ETag form the contract specifies: <c>"taxonomy:v41"</c>.
    /// </summary>
    /// <remarks>
    /// Strong, not weak. Registry payloads are byte-identical for a given version by construction —
    /// they are produced by the same serialiser from the same immutable snapshot — so a weak
    /// comparator would give up range requests and conditional PUTs for nothing.
    /// </remarks>
    public string ToETag() => $"\"{Registry}:v{Value}\"";

    public override string ToString() => $"{Registry}:v{Value}";
}

/// <summary>
/// The cache namespace of each registry.
/// </summary>
/// <remarks>
/// The prime directive bans business constants in code and explicitly permits technical ones —
/// "HTTP codes, cache keys, cryptographic params". These are cache keys. They carry no business
/// meaning, no operator ever needs them changed, and a typo in one silently splits a cache into two
/// halves that never agree, which is precisely the class of bug a constant prevents.
/// </remarks>
public static class RegistryNames
{
    public const string Taxonomy = "taxonomy";

    public const string Forms = "forms";

    public const string Workflow = "workflow";

    public const string Pricing = "pricing";

    public const string Rbac = "rbac";

    public const string Features = "features";

    public const string Notifications = "notifications";

    public const string Integrations = "integrations";

    public const string I18n = "i18n";

    public const string Geography = "geography";

    /// <summary>All ten, for the admin "cache state" panel and for warm-up on boot.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Taxonomy, Forms, Workflow, Pricing, Rbac,
        Features, Notifications, Integrations, I18n, Geography,
    ];
}
