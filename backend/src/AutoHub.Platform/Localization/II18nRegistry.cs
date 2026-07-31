using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Localization;

/// <summary>
/// Serves translated bundles. Registry 9.
/// </summary>
/// <remarks>
/// One rule governs everything here: <b>never return an empty string</b>. Every lookup path ends at
/// the key itself, which is ugly, obviously wrong, greppable, and — crucially — non-empty, so the
/// button still has a hit target and the screen reader still has something to announce.
/// </remarks>
public interface II18nRegistry
{
    /// <summary>
    /// The whole namespace for a locale, already merged down its fallback chain.
    /// </summary>
    /// <remarks>
    /// Bundles are served whole rather than key-by-key because the client caches them against the
    /// registry ETag and re-fetches only on a version change. Per-key resolution would put a network
    /// round trip behind every label on the page.
    /// </remarks>
    Task<Result<I18nBundle>> GetBundleAsync(string locale, string @namespace, CancellationToken cancellationToken);

    /// <summary>
    /// One string, rendered with its variables. For server-side text — SMS bodies, PDF invoices,
    /// push payloads — where there is no client bundle to resolve against.
    /// </summary>
    Task<string> TranslateAsync(
        string key,
        string locale,
        string @namespace,
        IReadOnlyDictionary<string, string?>? variables,
        CancellationToken cancellationToken);
}

/// <summary>
/// The fallback chain for a locale, most specific first.
/// </summary>
/// <remarks>
/// Exposed separately because the notification registry needs exactly this and nothing else: given a
/// dealer whose preferred locale is <c>mr-IN</c>, which template do we send when only Hindi and
/// English versions were authored? Reimplementing the chain there would let the two registries
/// disagree about what a user's language is.
/// </remarks>
public interface ILocaleChainResolver
{
    Task<IReadOnlyList<string>> ResolveChainAsync(string locale, CancellationToken cancellationToken);
}

/// <summary>An immutable, cacheable snapshot of one namespace in one locale.</summary>
public sealed record I18nBundle
{
    public required string Locale { get; init; }

    public required string Namespace { get; init; }

    /// <summary>The registry generation this snapshot was built from — the client's ETag.</summary>
    public required long Version { get; init; }

    public required IReadOnlyDictionary<string, string> Resources { get; init; }

    /// <summary>
    /// The chain actually walked, e.g. <c>["mr-IN", "hi-IN", "en-IN"]</c>. Returned so the admin
    /// translation console can show coverage — "84% Marathi, 16% inherited from Hindi" — which is
    /// the number that tells someone whether a language is ready to launch.
    /// </summary>
    public required IReadOnlyList<string> ResolvedFrom { get; init; }

    /// <summary>
    /// Resolves a key, ending at the key itself. Never returns null, never returns empty.
    /// </summary>
    public string Get(string key)
        => Resources.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : key;

    public string Get(string key, IReadOnlyDictionary<string, string?>? variables)
        => MessageTemplate.Render(Get(key), variables);
}

/// <summary>Loads locales and resources. Implemented in persistence, called through the cache.</summary>
public interface II18nResourceStore
{
    Task<IReadOnlyList<I18nLocale>> GetActiveLocalesAsync(CancellationToken cancellationToken);

    /// <summary>Active resources for exactly one locale — the merge happens in the registry.</summary>
    Task<IReadOnlyList<I18nResource>> GetResourcesAsync(
        string locale,
        string @namespace,
        CancellationToken cancellationToken);
}

public sealed class I18nOptions
{
    /// <summary>
    /// The locale every chain ends at. English is the base language of authoring, so a key that
    /// exists at all exists here.
    /// </summary>
    public string DefaultLocale { get; set; } = "en-IN";

    /// <summary>
    /// Guards against an operator accidentally creating a fallback cycle (mr → hi → mr). Cycles are
    /// also detected structurally; this is the second belt.
    /// </summary>
    public int MaxFallbackDepth { get; set; } = 6;
}
