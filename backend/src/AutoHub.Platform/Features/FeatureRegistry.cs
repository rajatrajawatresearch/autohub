using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using AutoHub.Platform.Expressions;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Features;

/// <summary>Tuning knobs for feature evaluation. Technical, not business.</summary>
public sealed class FeatureOptions
{
    /// <summary>
    /// Backstop lifetime for the cached registry. Not the invalidation mechanism — see
    /// <see cref="IFeatureCacheInvalidator"/>.
    /// </summary>
    public TimeSpan RegistryTimeToLive { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Evaluates modules and flags server-side.
/// </summary>
/// <remarks>
/// Everything about this class is arranged so the client never learns how a decision was made. It
/// loads rules, applies them, and returns booleans; there is no code path that serialises a
/// condition towards a client. That is not paranoia about a competitor reading the roadmap out of a
/// JSON payload — though they would — it is that client-side targeting is client-side
/// authorisation: a rule saying "gold dealers only" evaluated in a browser is a rule a browser can
/// decide it satisfies.
/// </remarks>
public sealed class FeatureRegistry : IFeatureRegistry
{
    private readonly ConcurrentDictionary<string, Task<FeatureSnapshot>> _perRequest = new(StringComparer.Ordinal);
    private readonly IFeatureStore _store;
    private readonly IFeatureCache _cache;
    private readonly FeatureOptions _options;
    private readonly ILogger<FeatureRegistry> _logger;

    public FeatureRegistry(
        IFeatureStore store,
        IFeatureCache cache,
        FeatureOptions options,
        ILogger<FeatureRegistry> logger)
    {
        _store = store;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public async Task<FeatureSnapshot> EvaluateAsync(
        FeatureAudience audience,
        CancellationToken cancellationToken)
    {
        // Memoised per request for the same reason permissions are: several handlers ask, and a
        // request that saw a module enabled in its first check and disabled in its third would do
        // half of something.
        var pending = _perRequest.GetOrAdd(Fingerprint(audience), _ => EvaluateUncachedAsync(audience, cancellationToken));

        try
        {
            return await pending;
        }
        catch
        {
            _perRequest.TryRemove(Fingerprint(audience), out _);
            throw;
        }
    }

    public async Task<bool> IsEnabledAsync(
        string flagKey,
        FeatureAudience audience,
        CancellationToken cancellationToken)
    {
        var snapshot = await EvaluateAsync(audience, cancellationToken);
        return snapshot.Flags.TryGetValue(flagKey, out var enabled) && enabled;
    }

    public async Task<bool> IsModuleEnabledAsync(
        string moduleCode,
        FeatureAudience audience,
        CancellationToken cancellationToken)
    {
        var snapshot = await EvaluateAsync(audience, cancellationToken);
        return snapshot.Modules.TryGetValue(moduleCode, out var enabled) && enabled;
    }

    private async Task<FeatureSnapshot> EvaluateUncachedAsync(
        FeatureAudience audience,
        CancellationToken cancellationToken)
    {
        var data = await LoadAsync(cancellationToken);
        var context = BuildEvaluationContext(audience);

        var flags = new Dictionary<string, bool>(data.Flags.Count, StringComparer.Ordinal);
        foreach (var flag in data.Flags)
        {
            flags[flag.Key] = EvaluateFlag(flag, context);
        }

        var modules = new Dictionary<string, bool>(data.Modules.Count, StringComparer.Ordinal);
        foreach (var module in data.Modules)
        {
            modules[module.Code] = EvaluateModule(module, flags);
        }

        return new FeatureSnapshot
        {
            Version = data.Version,
            Modules = modules,
            Flags = flags,
        };
    }

    private async Task<FeatureRegistryData> LoadAsync(CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync(cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var data = await _store.GetAsync(cancellationToken);
        await _cache.SetAsync(data, _options.RegistryTimeToLive, cancellationToken);
        return data;
    }

    /// <summary>
    /// Highest priority first, first match wins, default when nothing matches.
    /// </summary>
    /// <remarks>
    /// First-match rather than any-match is what makes a narrow "off" rule able to override a broad
    /// "on" rule. Under any-match-wins, disabling a feature for one city during an incident would
    /// require editing every rule that turns it on.
    /// </remarks>
    private bool EvaluateFlag(FlagDescriptor flag, JsonObject context)
    {
        foreach (var rule in flag.Rules.OrderByDescending(r => r.Priority))
        {
            var condition = ConditionExpression.FromJson(rule.ConditionJson);

            if (condition.IsFailure)
            {
                // Skipping is the safe direction — an unparseable rule must not decide anything —
                // but it is logged loudly, because a silently skipped rule looks identical to a rule
                // that simply did not match, and the operator who authored it will not guess.
                _logger.LogError(
                    "Feature flag {FlagKey} rule at priority {Priority} has an unparseable condition: {Reason}",
                    flag.Key, rule.Priority, condition.Error.Message);

                continue;
            }

            if (condition.Value.Evaluate(context))
            {
                return rule.Value;
            }
        }

        return flag.DefaultValue;
    }

    /// <summary>
    /// A module is available when its master switch is on and its gating flag, if any, evaluates true.
    /// </summary>
    /// <remarks>
    /// A gate pointing at a flag that no longer exists resolves to unavailable. The alternative —
    /// treating a missing gate as "no restriction" — means deleting a flag silently launches the
    /// module it was holding back, everywhere, to everyone.
    /// </remarks>
    private bool EvaluateModule(ModuleDescriptor module, Dictionary<string, bool> flags)
    {
        if (!module.IsEnabled)
        {
            return false;
        }

        if (module.GatingFlagKey is not { } gate)
        {
            return true;
        }

        if (flags.TryGetValue(gate, out var gateOpen))
        {
            return gateOpen;
        }

        _logger.LogError(
            "Module {ModuleCode} is gated behind unknown flag {FlagKey}; treating the module as unavailable",
            module.Code, gate);

        return false;
    }

    /// <summary>
    /// Builds what rules may see: the caller's attributes, plus server-derived facts they cannot shadow.
    /// </summary>
    /// <remarks>
    /// Identity and city live under the reserved <c>_audience</c> key, the same guard the workflow
    /// engine uses. Without it, an attribute bag carrying <c>city</c> could talk a rule into
    /// enabling a city pilot for someone who is not in that city.
    /// </remarks>
    private static JsonObject BuildEvaluationContext(FeatureAudience audience)
    {
        var context = audience.Attributes?.DeepClone() as JsonObject ?? [];

        context["_audience"] = new JsonObject
        {
            ["userId"] = audience.UserId?.ToString(),
            ["isAuthenticated"] = audience.UserId.HasValue,
            ["city"] = audience.CityCode,
        };

        return context;
    }

    private static string Fingerprint(FeatureAudience audience)
        => $"{audience.UserId:N}|{audience.CityCode}|{audience.Attributes?.ToJsonString()}";
}
