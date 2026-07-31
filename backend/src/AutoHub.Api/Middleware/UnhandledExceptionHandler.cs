using AutoHub.Api.Http;
using AutoHub.SharedKernel.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AutoHub.Api.Middleware;

/// <summary>
/// The last line: turns a bug into the same envelope every other failure wears.
/// </summary>
/// <remarks>
/// Reaching this handler means something the code did not model went wrong — expected failures
/// travel as <see cref="Result"/> and never get here. So the response says nothing about what
/// happened beyond a stable code and a trace id: the message, the type and the stack are exactly the
/// things an attacker probes an API to collect, and the trace id is what a support engineer needs to
/// find the real detail in the logs.
/// </remarks>
internal sealed class UnhandledExceptionHandler : IExceptionHandler
{
    private readonly ILogger<UnhandledExceptionHandler> _logger;

    public UnhandledExceptionHandler(ILogger<UnhandledExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A cancelled request is the client hanging up, not a fault. Logging it as an error trains
        // everyone to ignore errors, and mobile clients hang up constantly.
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var traceId = ApiProblem.TraceIdOf(context);

        _logger.LogError(
            exception,
            "Unhandled exception on {Method} {Path} (trace {TraceId})",
            context.Request.Method, context.Request.Path, traceId);

        var error = Error.Unexpected(
            "common.unexpected",
            "Something went wrong on our side. Try again shortly.");

        var problem = ApiProblem.Build(error, context);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
