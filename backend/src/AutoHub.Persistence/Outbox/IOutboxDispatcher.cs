using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Persistence.Outbox;

/// <summary>
/// Delivers one outbox message to whatever is listening.
/// </summary>
/// <remarks>
/// The seam between "the event is durably recorded" and "the event reached its consumers". Today
/// that is an in-process bus; tomorrow it is RabbitMQ or a partner webhook. Neither the interceptor
/// that wrote the row nor the processor that claimed it needs to know which, which is what makes
/// moving from one to the other a registration change rather than a rewrite.
/// <para>
/// Implementations must be idempotent. Delivery is at-least-once by construction — a handler that
/// succeeds and then loses its acknowledgement to a dying process will be handed the same message
/// again — so "I have already seen this event id" is the consumer's problem to solve, and it is
/// cheaper to solve there than to attempt exactly-once here, which is not achievable at all.
/// </para>
/// </remarks>
public interface IOutboxDispatcher
{
    Task<Result> DispatchAsync(OutboxMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// The dispatcher that runs until a real bus exists: it logs and succeeds.
/// </summary>
/// <remarks>
/// Registered as the fallback so the API boots and the outbox drains on a machine with no broker —
/// a developer laptop, a CI run, the first minutes of an incident where the broker is what is down.
/// <para>
/// It succeeds rather than fails on purpose. A no-op that failed would let the outbox grow without
/// bound and every message would eventually poison itself, so the first day the real dispatcher is
/// registered it would inherit a backlog of dead rows. Draining is the honest behaviour for a
/// deployment that has deliberately configured no consumers.
/// </para>
/// </remarks>
public sealed class LoggingOutboxDispatcher : IOutboxDispatcher
{
    private readonly ILogger<LoggingOutboxDispatcher> _logger;

    public LoggingOutboxDispatcher(ILogger<LoggingOutboxDispatcher> logger) => _logger = logger;

    public Task<Result> DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Outbox {EventType} for aggregate {AggregateId} dispatched to no consumers (no bus configured)",
            message.EventType,
            message.AggregateId);

        return Task.FromResult(Result.Success());
    }
}
