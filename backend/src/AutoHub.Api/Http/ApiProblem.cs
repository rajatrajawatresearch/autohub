using System.Diagnostics;
using AutoHub.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace AutoHub.Api.Http;

/// <summary>
/// Turns an <see cref="Error"/> into exactly the envelope §0 of the contract publishes.
/// </summary>
/// <remarks>
/// One function, used by every endpoint, every pipeline behaviour and the unhandled-exception
/// handler, because the frontend branches on <c>code</c> and nothing else — and a second code path
/// that forgets to emit <c>code</c> is a client that falls through to its generic error screen with
/// no idea what happened.
/// </remarks>
internal static class ApiProblem
{
    /// <summary>
    /// The documentation namespace errors resolve under. A URI, per RFC 9457, not a hyperlink we
    /// promise to keep live.
    /// </summary>
    private const string TypeNamespace = "https://autohub.in/errors/";

    /// <summary>
    /// Maps a failure kind onto a status code.
    /// </summary>
    /// <remarks>
    /// Two of these deserve their reasoning written down.
    /// <para>
    /// <see cref="ErrorKind.Validation"/> is <c>422</c>, not <c>400</c>. The contract's form
    /// validation endpoint specifies 422, and the distinction is real and useful to a client: 400
    /// means "I could not read your request", 422 means "I read it and it is wrong". A client can
    /// retry the first after fixing its serialisation and must not retry the second at all. Malformed
    /// JSON and unroutable URLs still produce 400 from the framework, above this.
    /// </para>
    /// <para>
    /// <see cref="ErrorKind.RuleViolation"/> is <c>422</c> too — a registry rule refused a
    /// semantically valid request — while <see cref="ErrorKind.Conflict"/> is <c>409</c>, because
    /// that one means the world moved underneath the caller and re-reading may make the request
    /// valid. <c>workflow.transition_not_allowed</c> is a 409 for exactly that reason.
    /// </para>
    /// </remarks>
    public static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status422UnprocessableEntity,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.RuleViolation => StatusCodes.Status422UnprocessableEntity,
        ErrorKind.Upstream => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError,
    };

    public static ProblemDetails Build(Error error, HttpContext context)
    {
        var status = StatusFor(error.Kind);

        var problem = new ProblemDetails
        {
            Type = TypeNamespace + error.Code,
            Title = TitleFor(error.Kind),
            Status = status,
            Detail = error.Message,
            Instance = context.Request.Path.HasValue ? context.Request.Path.Value : null,
        };

        // `code` is the only member a client may branch on, so it is always present — including on
        // the paths that produce a problem without an Error to read it from.
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = TraceIdOf(context);

        if (error.Metadata.Count > 0)
        {
            problem.Extensions["metadata"] = error.Metadata;
        }

        // Tells the client whether trying again can possibly help. Without it, every client invents
        // its own retry policy from the status code and gets 502-versus-409 wrong.
        problem.Extensions["retryable"] = error.Kind is ErrorKind.Upstream or ErrorKind.Unexpected;

        return problem;
    }

    public static IResult Result(Error error, HttpContext context)
    {
        var problem = Build(error, context);
        return Microsoft.AspNetCore.Http.Results.Problem(problem);
    }

    public static string TraceIdOf(HttpContext context)
        => Activity.Current?.Id ?? context.TraceIdentifier;

    /// <summary>
    /// A short, stable, human-readable heading per kind.
    /// </summary>
    /// <remarks>
    /// Deliberately generic. The specific sentence a user sees is resolved from the localisation
    /// registry using <see cref="Error.Code"/> as the key; a title written here would be an English
    /// string in application code, which the prime directive bans and which no Marathi speaker would
    /// ever see translated.
    /// </remarks>
    private static string TitleFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => "Validation failed",
        ErrorKind.NotFound => "Not found",
        ErrorKind.Conflict => "Conflict",
        ErrorKind.Unauthorized => "Authentication required",
        ErrorKind.Forbidden => "Forbidden",
        ErrorKind.RuleViolation => "Rule violation",
        ErrorKind.Upstream => "Upstream failure",
        _ => "Unexpected error",
    };
}
