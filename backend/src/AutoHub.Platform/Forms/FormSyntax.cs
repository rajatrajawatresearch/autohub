using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AutoHub.Platform.Forms;

/// <summary>
/// The handful of technical invariants a form document must satisfy, in one place.
/// </summary>
/// <remarks>
/// These are not business rules and never become one — they are the constraints that make a key
/// usable as a JSON payload property, a JsonLogic <c>var</c> path, an OpenSearch field name and a
/// translation key at the same time. Business meaning (which fields a sell form has, what a field
/// is called) lives entirely in the registry rows.
/// </remarks>
public static partial class FormSyntax
{
    /// <summary>
    /// Reserved root for facts the server injects into a visibility condition — the selected
    /// category, the caller's city — as opposed to values the user typed.
    /// </summary>
    /// <remarks>
    /// Namespaced so a field can never be named such that it shadows a server fact. Without this,
    /// a form author who adds a field called <c>city</c> silently changes the meaning of every
    /// condition that was written against the request's city.
    /// </remarks>
    public const string ContextRoot = "_context";

    // Lowercase snake_case, letter-initial, bounded length. Bounded because the key becomes a
    // database column name in projections and an index term in search, both of which have limits.
    [GeneratedRegex("^[a-z][a-z0-9_]{0,62}$")]
    private static partial Regex KeyPattern { get; }

    public static bool IsValidKey(string? key) => !string.IsNullOrEmpty(key) && KeyPattern.IsMatch(key);

    /// <summary>
    /// The root names a JsonLogic condition reads, so publish-time validation can prove every one of
    /// them is a field the form actually collects.
    /// </summary>
    /// <remarks>
    /// This is the check that catches <c>fuel_typ</c>. A condition on a misspelled variable evaluates
    /// to null, null is falsy, and the field silently never appears — which surfaces weeks later as
    /// "the EV battery question is missing" rather than as an error anyone could have acted on.
    /// <para>
    /// Only the <c>var</c> operator is inspected. <c>missing</c> and <c>missing_some</c> also name
    /// fields, but they are not used for visibility in this registry and treating their arguments as
    /// variable references would produce false positives on legitimate documents.
    /// </para>
    /// </remarks>
    public static IReadOnlyCollection<string> VariableRoots(JsonNode? condition)
    {
        var roots = new HashSet<string>(StringComparer.Ordinal);
        Collect(condition, roots);
        return roots;
    }

    private static void Collect(JsonNode? node, HashSet<string> roots)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (name, value) in obj)
                {
                    if (string.Equals(name, "var", StringComparison.Ordinal))
                    {
                        if (RootOf(value) is { } root)
                        {
                            roots.Add(root);
                        }

                        continue;
                    }

                    Collect(value, roots);
                }

                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    Collect(item, roots);
                }

                break;

            default:
                break;
        }
    }

    /// <summary>
    /// The first path segment of a <c>var</c> argument. <c>{"var":"vehicle.year"}</c> roots at
    /// <c>vehicle</c>; <c>{"var":["fuel_type", "petrol"]}</c> — the defaulted form — roots at
    /// <c>fuel_type</c>; <c>{"var":""}</c> reads the whole payload and roots at nothing.
    /// </summary>
    /// <summary>
    /// The handler a <see cref="FormRuleKind.Custom"/> rule names, from either authoring form.
    /// </summary>
    /// <remarks>
    /// Two forms are accepted because both are natural: a bare string when the check takes no
    /// arguments (<c>"registration_not_listed"</c>) and an object when it does
    /// (<c>{"handler":"pan_not_blacklisted","args":{…}}</c>). Parsed in one place so publish-time
    /// validation and the runtime engine can never disagree about which handler a rule names — a
    /// disagreement that would publish a form the server then refuses to validate.
    /// </remarks>
    public static string? CustomHandlerCode(JsonNode? ruleValue) => ruleValue switch
    {
        JsonValue value when value.TryGetValue<string>(out var code) && code.Length > 0 => code,
        JsonObject obj when obj["handler"] is JsonValue handler
                            && handler.TryGetValue<string>(out var code) && code.Length > 0 => code,
        _ => null,
    };

    /// <summary>Arguments for a custom handler, when the rule was authored in object form.</summary>
    public static JsonNode? CustomArguments(JsonNode? ruleValue)
        => ruleValue is JsonObject obj ? obj["args"]?.DeepClone() : null;

    private static string? RootOf(JsonNode? argument)
    {
        var raw = argument switch
        {
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            JsonArray array when array.Count > 0
                                 && array[0] is JsonValue first
                                 && first.TryGetValue<string>(out var text) => text,
            _ => null,
        };

        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        var dot = raw.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? raw : raw[..dot];
    }
}
