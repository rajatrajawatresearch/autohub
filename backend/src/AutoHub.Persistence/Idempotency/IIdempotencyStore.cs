using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Persistence.Idempotency;

/// <summary>
/// The gate every write endpoint passes through.
/// </summary>
/// <remarks>
/// Two calls rather than one wrapper, because the middleware that uses this has to run the handler
/// in between and has to be able to record the status code the handler actually produced. A single
/// <c>ExecuteAsync(key, work)</c> would read better and would force every handler into a delegate,
/// which is a fight with the request pipeline for no gain.
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>
    /// Claims a key, or reports what the first request with it produced.
    /// </summary>
    /// <remarks>
    /// The claim is the row insert, so the uniqueness of the key is enforced by the primary key
    /// rather than by a check-then-insert that two concurrent retries would both pass.
    /// </remarks>
    Task<Result<IdempotencyOutcome>> BeginAsync(
        string key,
        string endpoint,
        Guid? userId,
        string? requestBody,
        CancellationToken cancellationToken);

    /// <summary>Records the response so the next replay of this key can return it.</summary>
    Task CompleteAsync(
        string key,
        int responseStatus,
        JsonNode? responseBody,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gives a claimed key back, because the request it was claimed for did not succeed.
    /// </summary>
    /// <remarks>
    /// Without this, a failed write holds its key until the retention window expires, and the client
    /// that retries with the same key — which is exactly what a client does after a 500 or a dropped
    /// connection — is answered <c>idempotency.request_in_flight</c> for the next twenty-four hours.
    /// The operation never happened and the client is locked out of attempting it, which is a strictly
    /// worse outcome than having no idempotency at all.
    /// <para>
    /// Releasing rather than completing is the distinction that matters: a recorded failure would be
    /// replayed, telling a client its corrected payload is still wrong. A released key is simply
    /// unclaimed, so the retry runs.
    /// </para>
    /// </remarks>
    Task ReleaseAsync(string key, CancellationToken cancellationToken);
}

/// <summary>What <see cref="IIdempotencyStore.BeginAsync"/> decided.</summary>
public abstract record IdempotencyOutcome
{
    private IdempotencyOutcome() { }

    /// <summary>First time this key has been seen. Run the handler.</summary>
    public sealed record Proceed : IdempotencyOutcome;

    /// <summary>Seen before and finished. Return this response verbatim; do not run the handler.</summary>
    public sealed record Replay(int ResponseStatus, JsonNode? ResponseBody) : IdempotencyOutcome;
}
