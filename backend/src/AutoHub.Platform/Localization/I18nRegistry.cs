using AutoHub.Platform.Caching;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoHub.Platform.Localization;

/// <summary>
/// Merges a locale's resources down its fallback chain and serves the result from cache.
/// </summary>
public sealed class I18nRegistry : II18nRegistry, ILocaleChainResolver
{
    private readonly II18nResourceStore _store;
    private readonly IRegistryCache _cache;
    private readonly I18nOptions _options;
    private readonly ILogger<I18nRegistry> _logger;

    public I18nRegistry(
        II18nResourceStore store,
        IRegistryCache cache,
        IOptions<I18nOptions> options,
        ILogger<I18nRegistry> logger)
    {
        _store = store;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<I18nBundle>> GetBundleAsync(
        string locale,
        string @namespace,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(@namespace))
        {
            return Error.Validation("i18n.namespace_required", "A namespace is required.");
        }

        var requested = Normalise(locale);

        return await _cache.GetOrLoadRequiredAsync(
            RegistryNames.I18n,
            $"bundle:{requested}:{@namespace}",
            token => BuildBundleAsync(requested, @namespace, token),
            cancellationToken);
    }

    public async Task<string> TranslateAsync(
        string key,
        string locale,
        string @namespace,
        IReadOnlyDictionary<string, string?>? variables,
        CancellationToken cancellationToken)
    {
        var bundle = await GetBundleAsync(locale, @namespace, cancellationToken);

        // A failed bundle load still has to produce readable text. Returning the key keeps an SMS
        // shaped like an SMS instead of shaped like a stack trace.
        return bundle.IsSuccess
            ? bundle.Value.Get(key, variables)
            : MessageTemplate.Render(key, variables);
    }

    /// <summary>
    /// Walks <c>mr-IN → hi-IN → en-IN</c>, stopping at a cycle or at the configured depth.
    /// </summary>
    /// <remarks>
    /// The default locale is appended even when it is not reachable through the authored chain. A
    /// misconfigured fallback edge must not be able to produce a bundle with no English in it —
    /// that would take a whole locale's untranslated keys from "shows English" to "shows raw keys".
    /// </remarks>
    public async Task<IReadOnlyList<string>> ResolveChainAsync(string locale, CancellationToken cancellationToken)
    {
        var requested = Normalise(locale);
        var edges = await GetLocaleEdgesAsync(cancellationToken);

        var chain = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = requested;

        while (current is not null && chain.Count < _options.MaxFallbackDepth && visited.Add(current))
        {
            chain.Add(current);

            var edge = edges.FirstOrDefault(
                e => string.Equals(e.Code, current, StringComparison.OrdinalIgnoreCase));

            if (edge is null)
            {
                // An unknown locale is not an error — a client can send anything in
                // Accept-Language. It simply has no parent, so the chain ends and the default is
                // appended below.
                break;
            }

            current = edge.FallbackCode;
        }

        if (!chain.Contains(_options.DefaultLocale, StringComparer.OrdinalIgnoreCase))
        {
            chain.Add(_options.DefaultLocale);
        }

        return chain;
    }

    private async Task<I18nBundle> BuildBundleAsync(
        string locale,
        string @namespace,
        CancellationToken cancellationToken)
    {
        var chain = await ResolveChainAsync(locale, cancellationToken);
        var version = await _cache.GetVersionAsync(RegistryNames.I18n, cancellationToken);

        var merged = new Dictionary<string, string>(StringComparer.Ordinal);

        // Walk the chain in reverse so the most general locale is laid down first and each more
        // specific locale overwrites it. Building forwards would need a "does this key already
        // exist?" check on every insert, which is the same work with an extra chance to get it wrong.
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var resources = await _store.GetResourcesAsync(chain[i], @namespace, cancellationToken);

            foreach (var resource in resources)
            {
                if (!resource.IsActive || string.IsNullOrWhiteSpace(resource.Value))
                {
                    // A blank or withdrawn override must not shadow the parent locale's good value.
                    // This is where "never return an empty string" is actually enforced — everything
                    // downstream is a consequence of nothing empty ever entering the dictionary.
                    continue;
                }

                merged[resource.Key] = resource.Value;
            }
        }

        _logger.LogDebug(
            "Built i18n bundle {Namespace}/{Locale} from [{Chain}] with {Count} keys",
            @namespace, locale, string.Join(" → ", chain), merged.Count);

        return new I18nBundle
        {
            Locale = locale,
            Namespace = @namespace,
            Version = version.Value,
            Resources = merged,
            ResolvedFrom = chain,
        };
    }

    private async Task<IReadOnlyList<LocaleEdge>> GetLocaleEdgesAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetOrLoadRequiredAsync<IReadOnlyList<LocaleEdge>>(
            RegistryNames.I18n,
            "locale-edges",
            async token =>
            {
                var active = await _store.GetActiveLocalesAsync(token);
                return active
                    .Where(l => l.IsActive)
                    .Select(l => new LocaleEdge(l.Code, l.FallbackCode))
                    .ToList();
            },
            cancellationToken);
    }

    /// <summary>
    /// Falls back to the default rather than guessing. <c>Accept-Language</c> arrives from browsers,
    /// WebViews and Flutter clients in every shape imaginable, and normalising it here means the
    /// cache key space stays small instead of growing a variant per client quirk.
    /// </summary>
    private string Normalise(string? locale)
        => string.IsNullOrWhiteSpace(locale) ? _options.DefaultLocale : locale.Trim();

    /// <summary>
    /// The cacheable projection of a locale. The entity itself is not cached: it carries audit
    /// columns and an EF identity, neither of which survives a round trip through JSON, and both of
    /// which would be misleading if handed back from Redis as though they were tracked.
    /// </summary>
    private sealed record LocaleEdge(string Code, string? FallbackCode);
}
