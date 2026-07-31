using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoHub.Persistence.Idempotency;

/// <summary>
/// Idempotency backed by the primary key of <c>platform.idempotency_records</c>.
/// </summary>
/// <remarks>
/// The claim is an <c>INSERT</c>, deliberately. A <c>SELECT</c>-then-<c>INSERT</c> would let two
/// simultaneous retries — which is the entire scenario this exists for — both find nothing and both
/// proceed. Letting the database's unique constraint decide the winner is the only version of this
/// check that is correct under concurrency, and it costs one round trip either way.
/// </remarks>
public sealed class EfIdempotencyStore : IIdempotencyStore
{
    private readonly AutoHubDbContext _context;
    private readonly IClock _clock;
    private readonly IOptions<IdempotencyOptions> _options;

    public EfIdempotencyStore(
        AutoHubDbContext context,
        IClock clock,
        IOptions<IdempotencyOptions> options)
    {
        _context = context;
        _clock = clock;
        _options = options;
    }

    public async Task<Result<IdempotencyOutcome>> BeginAsync(
        string key,
        string endpoint,
        Guid? userId,
        string? requestBody,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var requestHash = IdempotencyRecord.ComputeHash(requestBody);

        var existing = await _context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        if (existing is not null)
        {
            return Evaluate(existing, endpoint, userId, requestHash, now);
        }

        _context.IdempotencyRecords.Add(IdempotencyRecord.Begin(
            key,
            endpoint,
            userId,
            requestHash,
            now,
            _options.Value.Retention));

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost the race to another retry of the same request. That other request is either
            // still running or has finished, and both of those are answers this method already
            // knows how to give — so re-read and give one, rather than surfacing a constraint
            // violation to a client whose only mistake was a flaky connection.
            _context.ChangeTracker.Clear();

            var winner = await _context.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

            return winner is null
                ? Error.Unexpected("idempotency.claim_failed", "The idempotency key could not be claimed.")
                : Evaluate(winner, endpoint, userId, requestHash, now);
        }

        return new IdempotencyOutcome.Proceed();
    }

    public async Task CompleteAsync(
        string key,
        int responseStatus,
        JsonNode? responseBody,
        CancellationToken cancellationToken)
    {
        var record = await _context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        if (record is null)
        {
            // The sweeper cannot have removed it inside one request, so this means the caller never
            // claimed the key. Silently inserting one here would record a response against a request
            // whose body was never hashed, which makes the next replay check meaningless.
            return;
        }

        record.Complete(responseStatus, responseBody);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(string key, CancellationToken cancellationToken)
    {
        // Deleted rather than marked abandoned. The row's only purpose is to answer "has this key
        // been used?", and after a failure the honest answer is no — a tombstone would need every
        // branch of Evaluate to learn about a third state, to conclude the same thing.
        //
        // Scoped to in-flight records so this can never erase a completed one. Completing and
        // releasing are mutually exclusive in the middleware, but a row that is already completed is
        // a replayable response somebody is entitled to, and losing it is not a failure any caller
        // could diagnose.
        await _context.IdempotencyRecords
            .Where(r => r.Key == key && r.ResponseStatus == null)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Decides what an already-claimed key means for this request.
    /// </summary>
    /// <remarks>
    /// Every branch that is not a clean replay fails loudly. The alternative — returning the stored
    /// response to a request that differs from the one that produced it — tells a client that
    /// something happened when it did not, and that lie is acted on immediately: the app closes the
    /// bid sheet, the dealer believes they bid, and nothing was placed.
    /// </remarks>
    private static Result<IdempotencyOutcome> Evaluate(
        IdempotencyRecord record,
        string endpoint,
        Guid? userId,
        string requestHash,
        DateTimeOffset now)
    {
        if (record.ExpiresAt <= now)
        {
            return Error.Conflict(
                "idempotency.key_expired",
                "This idempotency key is older than the replay window. Send a new key.")
                .With("key", record.Key);
        }

        if (!string.Equals(record.Endpoint, endpoint, StringComparison.Ordinal))
        {
            return Error.Conflict(
                "idempotency.key_reused_on_other_endpoint",
                "This idempotency key was already used for a different endpoint.")
                .With("originalEndpoint", record.Endpoint);
        }

        if (record.UserId != userId)
        {
            return Error.Conflict(
                "idempotency.key_belongs_to_another_user",
                "This idempotency key was already used by a different caller.");
        }

        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return Error.Conflict(
                "idempotency.request_mismatch",
                "This idempotency key was already used with a different request body.")
                .With("key", record.Key);
        }

        if (record.IsInFlight)
        {
            return Error.Conflict(
                "idempotency.request_in_flight",
                "The original request with this key is still being processed. Retry shortly.")
                .With("key", record.Key);
        }

        return new IdempotencyOutcome.Replay(record.ResponseStatus!.Value, record.ResponseBody);
    }
}
