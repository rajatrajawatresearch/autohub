namespace AutoHub.SharedKernel.Results;

/// <summary>
/// A failure with a stable machine-readable code.
/// </summary>
/// <remarks>
/// The <see cref="Code"/> is a contract. Clients — the web apps, the Flutter apps, partner banks —
/// branch on it, and translators key off it. It must never change once shipped, and it must never
/// be a sentence. Format is <c>module.resource.condition</c>, e.g. <c>listing.media.too_many</c>.
/// <para>
/// The <see cref="Message"/> is a developer-facing default. Real user-facing text is resolved from
/// the localisation registry using <see cref="Code"/> as the key, so no user ever sees this string.
/// </para>
/// </remarks>
public sealed record Error
{
    private Error(string code, string message, ErrorKind kind, IReadOnlyDictionary<string, object?>? metadata)
    {
        Code = code;
        Message = message;
        Kind = kind;
        Metadata = metadata ?? EmptyMetadata;
    }

    private static readonly IReadOnlyDictionary<string, object?> EmptyMetadata =
        new Dictionary<string, object?>(0);

    public string Code { get; }

    public string Message { get; }

    public ErrorKind Kind { get; }

    /// <summary>
    /// Structured detail the API surfaces alongside the error — the offending field, the limit that
    /// was exceeded, the state a transition was refused from. Never put PII in here.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; }

    public static Error Validation(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorKind.Validation, metadata);

    public static Error NotFound(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorKind.NotFound, metadata);

    public static Error Conflict(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorKind.Conflict, metadata);

    public static Error Forbidden(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorKind.Forbidden, metadata);

    public static Error Unauthorized(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorKind.Unauthorized, metadata);

    /// <summary>A rule in the pricing, workflow, or RBAC registry refused the operation.</summary>
    public static Error Rule(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorKind.RuleViolation, metadata);

    /// <summary>A third party we depend on failed. Distinguished from our own faults so it can be retried.</summary>
    public static Error Upstream(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorKind.Upstream, metadata);

    public static Error Unexpected(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorKind.Unexpected, metadata);

    public Error With(string key, object? value)
    {
        var copy = new Dictionary<string, object?>(Metadata) { [key] = value };
        return new Error(Code, Message, Kind, copy);
    }

    public override string ToString() => $"{Code}: {Message}";
}

public enum ErrorKind
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    RuleViolation = 6,
    Upstream = 7,
    Unexpected = 8,
}
