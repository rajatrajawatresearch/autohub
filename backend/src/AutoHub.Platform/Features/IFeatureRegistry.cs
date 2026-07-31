using System.Text.Json.Nodes;

namespace AutoHub.Platform.Features;

/// <summary>
/// Who is asking, and from where.
/// </summary>
/// <remarks>
/// Every field here is derived server-side from the access token and the request headers, never
/// from the request body. A flag that a client can talk its way into by posting
/// <c>{"tier":"gold"}</c> is not a flag, it is a suggestion.
/// </remarks>
public sealed record FeatureAudience
{
    public Guid? UserId { get; init; }

    /// <summary>From <c>X-Active-City</c>, validated against the geography registry before it gets here.</summary>
    public string? CityCode { get; init; }

    /// <summary>
    /// Anything else worth targeting on — party type, dealer tier, app platform, app version.
    /// </summary>
    /// <remarks>
    /// Open-ended on purpose. The day marketing wants a flag targeted at users who have completed a
    /// valuation, that must be a rule an operator writes, not a new column, a new migration and a
    /// new deploy. Keys land at the root of the evaluation context under the caller's own names.
    /// </remarks>
    public JsonObject? Attributes { get; init; }

    public static FeatureAudience Anonymous { get; } = new();
}

/// <summary>
/// The flat booleans a client receives — §5 of the registry API contract.
/// </summary>
/// <remarks>
/// Booleans and nothing else. The rules that produced them stay on the server, because targeting
/// logic shipped to a client is targeting logic an attacker can read: the flag names alone leak the
/// roadmap, and the conditions leak which cities, which dealer tiers, and which thresholds the
/// business cares about. A client that receives <c>{"lease": false}</c> learns nothing except that
/// it should not render lease.
/// </remarks>
public sealed record FeatureSnapshot
{
    public required int Version { get; init; }

    public required IReadOnlyDictionary<string, bool> Modules { get; init; }

    public required IReadOnlyDictionary<string, bool> Flags { get; init; }
}

/// <summary>Evaluates modules and flags for one audience.</summary>
public interface IFeatureRegistry
{
    /// <summary>
    /// The full answer for this audience, ready to serve as <c>GET /platform/features</c>.
    /// </summary>
    Task<FeatureSnapshot> EvaluateAsync(FeatureAudience audience, CancellationToken cancellationToken);

    /// <summary>
    /// One flag, for a server-side branch.
    /// </summary>
    /// <remarks>
    /// Falls back to the flag's default when the key is unknown rather than throwing, so deleting a
    /// retired flag from the registry cannot take an endpoint down before the code that reads it is
    /// removed. An unknown key evaluates to <c>false</c>.
    /// </remarks>
    Task<bool> IsEnabledAsync(string flagKey, FeatureAudience audience, CancellationToken cancellationToken);

    /// <summary>Whether a module may be used at all. The check every module endpoint makes first.</summary>
    Task<bool> IsModuleEnabledAsync(string moduleCode, FeatureAudience audience, CancellationToken cancellationToken);
}

/// <summary>The registry as the evaluator sees it: detached, immutable, cacheable.</summary>
/// <remarks>
/// Descriptors rather than entities. The snapshot outlives the request that loaded it, and an
/// EF-tracked graph in a cache is a change-tracker leak shared between threads.
/// </remarks>
public sealed record FeatureRegistryData(
    int Version,
    IReadOnlyList<ModuleDescriptor> Modules,
    IReadOnlyList<FlagDescriptor> Flags)
{
    public static FeatureRegistryData Empty { get; } = new(0, [], []);
}

public sealed record ModuleDescriptor(string Code, bool IsEnabled, string? GatingFlagKey);

public sealed record FlagDescriptor(string Key, bool DefaultValue, IReadOnlyList<FlagRuleDescriptor> Rules);

public sealed record FlagRuleDescriptor(int Priority, string? ConditionJson, bool Value);

/// <summary>
/// Loads the published registry. Implemented in the persistence layer.
/// </summary>
/// <remarks>
/// One method, returning everything, because the whole registry is a few hundred rows and the
/// alternative — a query per flag — turns one page load into forty round trips.
/// </remarks>
public interface IFeatureStore
{
    Task<FeatureRegistryData> GetAsync(CancellationToken cancellationToken);
}
