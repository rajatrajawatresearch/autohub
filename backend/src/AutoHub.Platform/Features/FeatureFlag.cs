using System.Text.RegularExpressions;
using AutoHub.Platform.Expressions;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Features;

/// <summary>
/// One boolean the product asks about, and the rules that decide it per user.
/// </summary>
/// <remarks>
/// A flag is not a config value. Config is the same for everyone; a flag is a different answer for
/// a Pune dealer on the Flutter app than for an anonymous visitor on the web, and that difference is
/// what makes a staged rollout, a city pilot, and a kill switch the same mechanism.
/// <para>
/// <see cref="DefaultValue"/> is the answer when no rule matches, and it is the answer that ships
/// first. Defaulting a new flag to <c>false</c> and turning it on by rule means the failure mode of
/// a broken rule set is "the new thing did not appear" rather than "the half-built thing appeared
/// for everyone".
/// </para>
/// </remarks>
public sealed partial class FeatureFlag : AggregateRoot, IAuditable
{
    private readonly List<FlagRule> _rules = [];

    private FeatureFlag(Guid id, string key, string? description, bool defaultValue) : base(id)
    {
        Key = key;
        Description = description;
        DefaultValue = defaultValue;
    }

    private FeatureFlag() { }

    /// <summary>Stable key, e.g. <c>instant_offer</c>. Sent to clients verbatim in the flags map.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// What the flag controls, in a sentence. Written for the person who finds it eighteen months
    /// from now and needs to know whether deleting it is safe.
    /// </summary>
    public string? Description { get; private set; }

    public bool DefaultValue { get; private set; }

    public IReadOnlyList<FlagRule> Rules => _rules;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex KeyPattern { get; }

    public static Result<string> ParseKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || !KeyPattern.IsMatch(key.Trim()))
        {
            return Error.Validation(
                "feature.key.malformed",
                "A flag key is lowercase letters, digits and underscores, e.g. 'instant_offer'.")
                .With("input", key);
        }

        return key.Trim();
    }

    public static Result<FeatureFlag> Create(string key, bool defaultValue = false, string? description = null)
    {
        var parsed = ParseKey(key);

        return parsed.IsFailure
            ? Result.Failure<FeatureFlag>(parsed.Error)
            : new FeatureFlag(UuidV7.New(), parsed.Value, description, defaultValue);
    }

    public void SetDefault(bool defaultValue) => DefaultValue = defaultValue;

    public void Describe(string? description) => Description = description;

    /// <summary>
    /// Adds a targeting rule.
    /// </summary>
    /// <remarks>
    /// The condition is validated on the way in rather than on the way out. A rule that will not
    /// parse is skipped at evaluation time — the safe direction — but skipped silently it looks
    /// exactly like a rule that did not match, and the operator who wrote it spends an afternoon
    /// wondering why their pilot city sees nothing.
    /// </remarks>
    public Result AddRule(FlagRule rule)
    {
        var condition = ConditionExpression.FromJson(rule.ConditionJson);
        if (condition.IsFailure)
        {
            return Result.Failure(condition.Error);
        }

        if (_rules.Exists(r => r.Priority == rule.Priority))
        {
            return Error.Conflict(
                "feature.rule.priority_taken",
                $"Another rule on '{Key}' already has priority {rule.Priority}.")
                .With("priority", rule.Priority);
        }

        _rules.Add(rule);
        return Result.Success();
    }

    public void RemoveRule(Guid ruleId) => _rules.RemoveAll(r => r.Id == ruleId);
}

/// <summary>
/// "For this audience, the answer is this."
/// </summary>
/// <remarks>
/// Rules are evaluated highest priority first and the <b>first match wins</b> — it does not OR them
/// together. That is what lets a narrow rule turn a flag off for one segment while a broad rule
/// leaves it on for everyone else, which is the shape every incident response takes: "disable
/// instant offer for commercial vehicles in Kerala, leave it alone everywhere else."
/// <para>
/// Priorities are unique within a flag and authored, never inferred. A resolver that guessed which
/// of two overlapping rules was "more specific" would be a resolver nobody could predict, and a
/// feature flag whose behaviour is unpredictable is worse than no feature flag.
/// </para>
/// </remarks>
public sealed class FlagRule
{
    public FlagRule(int priority, string? conditionJson, bool value, string? description = null)
    {
        Priority = priority;
        ConditionJson = conditionJson;
        Value = value;
        Description = description;
    }

    private FlagRule() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid FeatureFlagId { get; private set; }

    /// <summary>Higher is evaluated first.</summary>
    public int Priority { get; private set; }

    /// <summary>
    /// JsonLogic over the evaluation context. Null matches everyone — a catch-all that should sit at
    /// the lowest priority, since anything below it is unreachable.
    /// </summary>
    /// <example>
    /// Dealers in Bengaluru:
    /// <code>
    /// { "and": [
    ///     { "==": [ { "var": "_audience.city" }, "bengaluru" ] },
    ///     { "==": [ { "var": "party.type" },     "dealer"    ] }
    /// ]}
    /// </code>
    /// </example>
    public string? ConditionJson { get; private set; }

    public bool Value { get; private set; }

    /// <summary>Why this rule exists. Shown in the admin list so nobody deletes a kill switch.</summary>
    public string? Description { get; private set; }
}
