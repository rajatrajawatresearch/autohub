using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;
using Json.Logic;

namespace AutoHub.Platform.Expressions;

/// <summary>
/// A boolean condition stored as data and evaluated at runtime.
/// </summary>
/// <remarks>
/// This is the single mechanism by which business conditions enter the system without entering the
/// codebase. A workflow guard, a pricing rule's applicability, a feature flag's targeting, and a
/// notification's audience filter are all the same thing: a JsonLogic document evaluated against a
/// context document.
/// <para>
/// JsonLogic was chosen over a bespoke expression language for one reason that matters more than
/// elegance: it has no loops, no function definitions, and no I/O. A rule authored by an operations
/// user in the admin panel therefore cannot hang a request thread or read a file, which a
/// general-purpose scripting engine absolutely can. The expressiveness ceiling is the point.
/// </para>
/// <para>
/// Example — "auction commission applies to commercial vehicles in Maharashtra above ₹5,00,000":
/// <code>
/// { "and": [
///     { "==": [ { "var": "vehicle.category" }, "commercial" ] },
///     { "==": [ { "var": "location.state" },   "MH" ] },
///     { "&gt;":  [ { "var": "deal.amount_minor" }, 50000000 ] }
/// ]}
/// </code>
/// </para>
/// </remarks>
public sealed class ConditionExpression
{
    private readonly JsonNode? _rule;

    private ConditionExpression(JsonNode? rule, bool isAlwaysTrue)
    {
        _rule = rule;
        IsAlwaysTrue = isAlwaysTrue;
    }

    /// <summary>
    /// A rule with no condition applies unconditionally. Modelled explicitly so that "no condition"
    /// and "a condition that failed to parse" can never be confused — one applies, the other must not.
    /// </summary>
    public bool IsAlwaysTrue { get; }

    public static ConditionExpression Always { get; } = new(null, isAlwaysTrue: true);

    public static Result<ConditionExpression> FromJson(JsonNode? rule)
    {
        if (rule is null)
        {
            return Always;
        }

        // A condition must be an object (an operator) or a literal boolean. A bare string or number
        // is almost always an authoring mistake that would otherwise evaluate truthy and silently
        // apply a fee to every transaction on the platform.
        if (rule is JsonObject)
        {
            return new ConditionExpression(rule, isAlwaysTrue: false);
        }

        if (rule is JsonValue value && value.TryGetValue<bool>(out var literal))
        {
            return literal
                ? Always
                : new ConditionExpression(JsonNode.Parse("""{"==":[1,0]}"""), isAlwaysTrue: false);
        }

        return Error.Validation(
            "condition.malformed",
            "A condition must be a JsonLogic object or a literal boolean.");
    }

    public static Result<ConditionExpression> FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Always;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return Error.Validation("condition.invalid_json", ex.Message);
        }

        return FromJson(node);
    }

    /// <summary>
    /// Evaluates against a context document.
    /// </summary>
    /// <remarks>
    /// Failure is never silently truthy. A rule that references a variable the context does not
    /// carry evaluates to <c>null</c> in JsonLogic, and a null result means "does not apply" — the
    /// safe direction for a fee, a permission, and a state transition alike.
    /// </remarks>
    public bool Evaluate(JsonNode context)
    {
        if (IsAlwaysTrue)
        {
            return true;
        }

        try
        {
            var outcome = JsonLogic.Apply(_rule!, context);
            return IsTruthy(outcome);
        }
        catch (JsonLogicException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// JsonLogic truthiness, which follows JavaScript: empty string, 0, empty array and null are
    /// false. Implemented here rather than inferred at call sites so every registry agrees.
    /// </summary>
    private static bool IsTruthy(JsonNode? node) => node switch
    {
        null => false,
        JsonArray array => array.Count > 0,
        JsonObject => true,
        JsonValue value => value.TryGetValue<bool>(out var b) ? b
            : value.TryGetValue<decimal>(out var d) ? d != 0
            : value.TryGetValue<string>(out var s) ? s.Length > 0
            : true,
        _ => false,
    };

    public JsonNode? ToJson() => _rule?.DeepClone();
}
