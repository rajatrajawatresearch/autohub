namespace AutoHub.Platform.Forms;

/// <summary>
/// The taxonomy registry, seen through the narrow hole the form registry needs.
/// </summary>
/// <remarks>
/// Registry 2 depends on registry 1 for exactly one thing — the options, label and unit of an
/// attribute — and this interface is that one thing. Depending on the taxonomy aggregates directly
/// would make every change to how attributes are stored a change to the form compiler, and would
/// make the compiler untestable without a taxonomy fixture.
/// <para>
/// Batched by design. A 200-point inspection checklist references dozens of attributes; resolving
/// them one at a time is the classic registry N+1, and it lands on the request path of the busiest
/// screen the partner app has.
/// </para>
/// </remarks>
public interface IFormAttributeSource
{
    /// <summary>Resolves the requested codes. Codes with no definition are simply absent from the result.</summary>
    Task<IReadOnlyDictionary<string, FormAttribute>> ResolveAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every attribute code that exists, for publish-time validation.
    /// </summary>
    /// <remarks>
    /// Returned as a set rather than checked one code at a time so publishing a large form is one
    /// round trip, and so the check is a pure set operation the domain can run without I/O.
    /// </remarks>
    Task<IReadOnlySet<string>> KnownCodesAsync(CancellationToken cancellationToken);
}

/// <summary>An attribute definition, reduced to what a form field needs from it.</summary>
public sealed record FormAttribute(
    string Code,
    string Label,
    string? LabelKey,
    string? Unit,
    IReadOnlyList<CompiledOption> Options);

/// <summary>
/// Stands in for the taxonomy registry until it is wired up, so the API boots.
/// </summary>
/// <remarks>
/// Deliberately empty rather than permissive: it knows no codes, so any form that references an
/// attribute fails to publish with a clear message instead of publishing against a taxonomy that
/// was never consulted.
/// </remarks>
public sealed class EmptyFormAttributeSource : IFormAttributeSource
{
    private static readonly IReadOnlyDictionary<string, FormAttribute> None =
        new Dictionary<string, FormAttribute>(0, StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> NoCodes = new HashSet<string>(StringComparer.Ordinal);

    public Task<IReadOnlyDictionary<string, FormAttribute>> ResolveAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken) => Task.FromResult(None);

    public Task<IReadOnlySet<string>> KnownCodesAsync(CancellationToken cancellationToken)
        => Task.FromResult(NoCodes);
}

/// <summary>
/// The localisation registry, seen through the hole the form compiler needs.
/// </summary>
/// <remarks>
/// One batched call per served form, not one per label — a form has a hundred strings and the i18n
/// registry is a cache lookup, but a hundred cache lookups per request is still a hundred.
/// </remarks>
public interface IFormLocalizer
{
    /// <summary>
    /// Resolves translation keys for a locale. Keys with no translation are absent, and the caller
    /// falls back to the authored default — never to an empty string, which renders as a blank label
    /// nobody can act on.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        string? locale,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken);
}

/// <summary>Resolves nothing, so every label falls back to the authored default. Boots without registry 9.</summary>
public sealed class PassThroughFormLocalizer : IFormLocalizer
{
    private static readonly IReadOnlyDictionary<string, string> None =
        new Dictionary<string, string>(0, StringComparer.Ordinal);

    public Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        string? locale,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken) => Task.FromResult(None);
}
