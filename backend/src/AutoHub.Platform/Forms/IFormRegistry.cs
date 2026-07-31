using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Forms;

/// <summary>
/// The only way a form reaches a screen, and the only way a submission is checked.
/// </summary>
/// <remarks>
/// One renderer on web, one on Android, one on iOS, and none of them knows what a sell form
/// contains. They ask this. The server validates the same document with the same rules, so the two
/// answers cannot drift — which is the property that makes "add a field without a deploy" safe
/// rather than merely possible.
/// </remarks>
public interface IFormRegistry
{
    /// <summary>
    /// The published form for a key, in the variant that fits the caller's context.
    /// </summary>
    Task<Result<CompiledForm>> GetPublishedAsync(
        string key,
        FormContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks a payload against the same document the client rendered.
    /// </summary>
    /// <remarks>
    /// Success is an empty <see cref="Result"/>; failure carries the per-field machine codes under
    /// <c>fieldErrors</c> in <see cref="Error.Metadata"/>, which is exactly the
    /// <c>422 metadata.fieldErrors</c> shape the contract specifies. Codes, never sentences — the
    /// sentence is the localisation registry's job and it differs per locale.
    /// </remarks>
    Task<Result> ValidateAsync(
        string key,
        FormContext context,
        JsonObject payload,
        FormValidationMode mode,
        CancellationToken cancellationToken);
}

/// <summary>
/// What the caller's situation is, for variant selection and visibility conditions.
/// </summary>
/// <remarks>
/// Carried as opaque key/value pairs rather than a typed record on purpose. The whole point of the
/// registry is that a new dimension — "vehicle is imported", "dealer is on the enterprise plan" —
/// becomes a selector an operator writes, and a typed context would make each new dimension a
/// backend change, which is the thing we are avoiding.
/// </remarks>
public sealed class FormContext
{
    private FormContext(IReadOnlyDictionary<string, string> values, string? locale)
    {
        Values = values;
        Locale = locale;
    }

    public static FormContext Empty { get; } =
        new(new Dictionary<string, string>(0, StringComparer.Ordinal), null);

    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>BCP-47 tag from <c>Accept-Language</c>, e.g. <c>mr-IN</c>. Null uses the authored defaults.</summary>
    public string? Locale { get; }

    /// <summary>
    /// Parses the contract's <c>?context=category:suv</c> query parameter.
    /// </summary>
    /// <remarks>
    /// Malformed pairs are dropped rather than rejected. A stale link with a mangled query string
    /// should serve the base sell form, not a 400 — the user came here to sell a car, and a
    /// validation error about a parameter they never typed is a dead end they cannot escape.
    /// </remarks>
    public static FormContext Parse(string? context, string? locale = null)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(context))
        {
            foreach (var pair in context.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separator = pair.IndexOf(':', StringComparison.Ordinal);
                if (separator <= 0 || separator == pair.Length - 1)
                {
                    continue;
                }

                values[pair[..separator]] = pair[(separator + 1)..];
            }
        }

        return new FormContext(values, locale);
    }

    public static FormContext From(IReadOnlyDictionary<string, string> values, string? locale = null)
        => new(values, locale);

    public JsonObject ToJson()
    {
        var json = new JsonObject();

        foreach (var (key, value) in Values)
        {
            json[key] = value;
        }

        return json;
    }

    /// <summary>
    /// A stable, order-independent string for cache keys. Two callers who sent the same pairs in a
    /// different order must hit the same cache entry, or the cache halves in usefulness for free.
    /// </summary>
    public string CacheDiscriminator()
        => Values.Count == 0
            ? "-"
            : string.Join(',', Values.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}:{p.Value}"));
}

/// <summary>
/// Reads and writes form definitions.
/// </summary>
/// <remarks>
/// Declared here and implemented in the persistence layer so <c>AutoHub.Platform</c> never
/// references EF Core, and so the compiler and validation engine can be tested against an in-memory
/// store — which matters, because publish-time validation is exactly the kind of logic that needs a
/// hundred cheap tests rather than three expensive ones.
/// </remarks>
public interface IFormStore
{
    /// <summary>
    /// The definition and all its versions. Loaded whole because variant selection needs to compare
    /// every published version, and because a form is small — a few hundred rows at the very worst.
    /// </summary>
    Task<FormDefinition?> GetAsync(string key, CancellationToken cancellationToken);

    Task AddAsync(FormDefinition definition, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
