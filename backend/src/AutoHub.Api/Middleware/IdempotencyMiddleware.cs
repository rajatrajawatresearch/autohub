using System.Text.Json.Nodes;
using AutoHub.Api.Http;
using AutoHub.Persistence.Idempotency;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Security;
using Microsoft.Extensions.Logging;

namespace AutoHub.Api.Middleware;

/// <summary>
/// Makes every write endpoint replay-safe, once, in front of all of them.
/// </summary>
/// <remarks>
/// The quality floor states it plainly — "every write endpoint is idempotent
/// (<c>Idempotency-Key</c> header); payments and bids demand it" — and the reason it lives in
/// middleware rather than in each handler is that a handler which forgets is a handler nobody
/// notices until a dealer has bid twice.
/// <para>
/// <b>The key is required, not optional.</b> Treating a missing key as "just run it" makes the
/// guarantee opt-in by accident: the one client that forgets to send it gets no protection and no
/// warning. The published API client already sends one on every non-GET request, so requiring it
/// costs nothing and closes the hole.
/// </para>
/// <para>
/// The response body is buffered so the original can be recorded and replayed byte for byte. That
/// is a real cost — a write response held in memory — and it is bounded by
/// <see cref="MaxRecordedBodyBytes"/>: past that the response is returned but not recorded, so a
/// replay re-runs rather than returning a truncated body that would parse as valid.
/// </para>
/// </remarks>
internal sealed class IdempotencyMiddleware
{
    public const string HeaderName = "Idempotency-Key";

    /// <summary>
    /// Ceiling on a recorded response body.
    /// </summary>
    /// <remarks>
    /// Write responses are small — an id, a status, a quote. A megabyte is far past anything this
    /// API returns from a POST and small enough that buffering cannot be turned into a memory
    /// exhaustion attack by a client that asks for a large export via POST.
    /// </remarks>
    private const int MaxRecordedBodyBytes = 1024 * 1024;

    private static readonly string[] MutatingMethods = ["POST", "PUT", "PATCH", "DELETE"];

    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIdempotencyStore store,
        ICurrentUser currentUser)
    {
        if (!RequiresKey(context))
        {
            await _next(context);
            return;
        }

        var key = context.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(key))
        {
            await WriteProblemAsync(context, Error.Validation(
                "idempotency.key_required",
                $"Every write requires an '{HeaderName}' header. Send a UUID."));
            return;
        }

        if (key.Length > 128)
        {
            await WriteProblemAsync(context, Error.Validation(
                "idempotency.key_too_long",
                $"An '{HeaderName}' may be at most 128 characters."));
            return;
        }

        var body = await ReadBodyAsync(context);

        // Method plus route template, not the raw path. Two bids on two different lots hit the same
        // template and are distinguished by their bodies, which is what the request hash is for; the
        // template is here to catch a key reused across two genuinely different operations.
        var endpoint = $"{context.Request.Method} {context.GetEndpoint()?.DisplayName ?? context.Request.Path}";

        var outcome = await store.BeginAsync(key, endpoint, currentUser.UserId, body, context.RequestAborted);

        if (outcome.IsFailure)
        {
            await WriteProblemAsync(context, outcome.Error);
            return;
        }

        if (outcome.Value is IdempotencyOutcome.Replay replay)
        {
            _logger.LogInformation("Replaying idempotent response for {Endpoint}", endpoint);

            context.Response.StatusCode = replay.ResponseStatus;
            context.Response.Headers["Idempotency-Replayed"] = "true";

            if (replay.ResponseBody is not null)
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(replay.ResponseBody.ToJsonString(), context.RequestAborted);
            }

            return;
        }

        await RunAndRecordAsync(context, store, key);
    }

    private async Task RunAndRecordAsync(HttpContext context, IIdempotencyStore store, string key)
    {
        var original = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = original;

            buffer.Position = 0;
            await buffer.CopyToAsync(original, context.RequestAborted);
            buffer.Position = 0;
        }

        // Only successful writes are recorded. A replayed 500 would hand the client a permanent
        // failure for a request that might succeed on retry, and a replayed 422 would tell them
        // their corrected payload is still wrong.
        //
        // The claim is released rather than merely left unrecorded. Leaving it would keep the row
        // in-flight until its retention expired, so the client retrying after a 500 — which is what
        // every client does — would be answered `idempotency.request_in_flight` for the next
        // twenty-four hours, locked out of an operation that never happened.
        //
        // CancellationToken.None on both paths: the response has been written and the ledger has to
        // reach a terminal state whether or not the caller is still listening. Passing the request's
        // token would leave the row in flight in precisely the case idempotency exists for — the
        // client that disconnected mid-write.
        if (context.Response.StatusCode is < 200 or >= 300)
        {
            await store.ReleaseAsync(key, CancellationToken.None);
            return;
        }

        JsonNode? recorded = null;

        if (buffer.Length is > 0 and <= MaxRecordedBodyBytes)
        {
            try
            {
                recorded = JsonNode.Parse(buffer);
            }
            catch (System.Text.Json.JsonException)
            {
                // A non-JSON success body — a redirect, a file. Recording the status alone still
                // makes the replay correct; it simply returns an empty body, which is the honest
                // answer to "I already did this and cannot re-send the bytes".
                recorded = null;
            }
        }

        await store.CompleteAsync(key, context.Response.StatusCode, recorded, CancellationToken.None);
    }

    /// <summary>
    /// Reads the body without consuming it.
    /// </summary>
    /// <remarks>
    /// <c>EnableBuffering</c> then rewind, so model binding downstream still sees the whole stream.
    /// Without the rewind every write endpoint receives an empty body, which presents as "all my
    /// fields are null" and is maddening to diagnose.
    /// </remarks>
    private static async Task<string?> ReadBodyAsync(HttpContext context)
    {
        if (context.Request.ContentLength is null or 0)
        {
            return null;
        }

        context.Request.EnableBuffering();

        using var reader = new StreamReader(
            context.Request.Body,
            System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;
        return body;
    }

    private static bool RequiresKey(HttpContext context)
        => MutatingMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase)
           && context.Request.Path.StartsWithSegments("/api", StringComparison.Ordinal)
           && context.GetEndpoint()?.Metadata.GetMetadata<IdempotencyExemptAttribute>() is null;

    private static async Task WriteProblemAsync(HttpContext context, Error error)
    {
        var problem = ApiProblem.Build(error, context);
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }
}
