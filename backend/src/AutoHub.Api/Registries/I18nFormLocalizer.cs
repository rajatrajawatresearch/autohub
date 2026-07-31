using AutoHub.Platform.Forms;
using AutoHub.Platform.Localization;

namespace AutoHub.Api.Registries;

/// <summary>
/// Resolves a form's labels through the localisation registry.
/// </summary>
/// <remarks>
/// The wire between registry 2 and registry 9, and the replacement for
/// <c>PassThroughFormLocalizer</c>, which resolves nothing and leaves every label in its authored
/// English.
/// <para>
/// One bundle fetch per served form, not one lookup per label. The bundle is already a cached,
/// merged dictionary, so resolving a hundred keys against it is a hundred dictionary reads rather
/// than a hundred cache round trips.
/// </para>
/// <para>
/// A key with no translation is simply absent from the result, and the compiler falls back to the
/// authored default. That is the registry's own rule — never return an empty string — applied one
/// layer up: a blank field label is a form nobody can fill in, and it is strictly worse than an
/// untranslated one.
/// </para>
/// </remarks>
internal sealed class I18nFormLocalizer : IFormLocalizer
{
    /// <summary>
    /// The bundle forms resolve against.
    /// </summary>
    /// <remarks>
    /// A technical namespace, not business vocabulary — it names a partition of the translation
    /// store the same way a table name does. Which strings are in it is entirely registry data.
    /// </remarks>
    private const string Namespace = "forms";

    private static readonly IReadOnlyDictionary<string, string> None =
        new Dictionary<string, string>(0, StringComparer.Ordinal);

    private readonly II18nRegistry _registry;

    public I18nFormLocalizer(II18nRegistry registry) => _registry = registry;

    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        string? locale,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken)
    {
        if (keys.Count == 0 || string.IsNullOrWhiteSpace(locale))
        {
            return None;
        }

        var bundle = await _registry.GetBundleAsync(locale, Namespace, cancellationToken);

        if (bundle.IsFailure)
        {
            // A missing bundle is a translation gap, not an outage. Falling back to the authored
            // labels shows the user an English form; failing the request shows them nothing.
            return None;
        }

        var resolved = new Dictionary<string, string>(keys.Count, StringComparer.Ordinal);

        foreach (var key in keys)
        {
            if (bundle.Value.Resources.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                resolved[key] = value;
            }
        }

        return resolved;
    }
}
