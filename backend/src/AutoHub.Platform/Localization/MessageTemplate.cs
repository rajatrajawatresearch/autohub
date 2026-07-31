using System.Text.RegularExpressions;

namespace AutoHub.Platform.Localization;

/// <summary>
/// The one placeholder syntax used by every piece of authored text in the platform.
/// </summary>
/// <remarks>
/// Localisation strings, notification bodies, SMS templates and email subjects are all authored by
/// the same people in the same admin screens, so they use the same syntax: <c>{name}</c>. One
/// implementation means a translator who learns it once is right everywhere, and it means a
/// placeholder audit — "does the Marathi translation still reference every variable the English one
/// does?" — is a single function rather than four near-identical ones that drift.
/// <para>
/// Deliberately not <c>string.Format</c>: positional <c>{0}</c> arguments are unreadable to a
/// translator working without context, and reordering them for a language with different word order
/// is exactly what translators need to be able to do.
/// </para>
/// </remarks>
public static partial class MessageTemplate
{
    // Named placeholders only. Anything with a space, a brace, or a format specifier is not a
    // placeholder — Hindi and Marathi text contains braces rarely, but JSON samples pasted into a
    // template body contain them constantly.
    [GeneratedRegex(@"\{(?<name>[a-zA-Z_][a-zA-Z0-9_.]{0,63})\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern { get; }

    /// <summary>Every distinct placeholder in the text, in first-appearance order.</summary>
    public static IReadOnlyList<string> Placeholders(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var found = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var match in PlaceholderPattern.Matches(text).Cast<Match>())
        {
            var name = match.Groups["name"].Value;
            if (seen.Add(name))
            {
                found.Add(name);
            }
        }

        return found;
    }

    /// <summary>
    /// Substitutes values into a template.
    /// </summary>
    /// <remarks>
    /// A placeholder with no supplied value is left <b>as written</b> rather than replaced with an
    /// empty string. "Your bid on {vehicle} was outbid" is a bug report; "Your bid on  was outbid"
    /// is a bug that ships, because it looks like prose and nobody greps for double spaces.
    /// </remarks>
    public static string Render(string? template, IReadOnlyDictionary<string, string?>? values)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        if (values is null || values.Count == 0)
        {
            return template;
        }

        return PlaceholderPattern.Replace(
            template,
            match => values.TryGetValue(match.Groups["name"].Value, out var value) && value is not null
                ? value
                : match.Value);
    }

    /// <summary>
    /// Placeholders the candidate text uses that the reference text does not declare.
    /// </summary>
    /// <remarks>
    /// Runs when a translation is saved. A translator who invents <c>{amount}</c> in Tamil for a
    /// string that only ever carried <c>{vehicle}</c> produces a message that renders a literal
    /// brace to a customer, and it will be found by that customer rather than by us.
    /// </remarks>
    public static IReadOnlyList<string> UndeclaredPlaceholders(string? candidate, IEnumerable<string> declared)
    {
        var known = declared.ToHashSet(StringComparer.Ordinal);
        return Placeholders(candidate).Where(p => !known.Contains(p)).ToList();
    }
}
