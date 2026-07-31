using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoHub.Persistence.Outbox;

/// <summary>
/// Drains the outbox: claims a batch, delivers it in order per aggregate, retries what fails.
/// </summary>
/// <remarks>
/// <b>Why a poller and not a trigger or a listener.</b> <c>LISTEN/NOTIFY</c> would be lower latency
/// and is not durable — a notification delivered while no node is listening is simply gone, so a
/// poller would still be needed as the backstop, and two mechanisms is two things to be wrong.
/// Polling one indexed partial index every couple of seconds costs less than the request that
/// raised the event.
/// <para>
/// <b>Why SELECT … FOR UPDATE SKIP LOCKED.</b> Every API node runs this. Without row locks, two
/// nodes claim the same batch and every consumer sees every event twice; with plain <c>FOR
/// UPDATE</c>, the second node blocks behind the first and the fleet processes the outbox at the
/// speed of one node. <c>SKIP LOCKED</c> gives each node a disjoint batch and lets throughput scale
/// with the fleet, which is the only reason a database-backed queue is viable at all.
/// </para>
/// <para>
/// <b>Why ordering is per aggregate and not global.</b> A global order would mean one worker, and
/// one worker is a throughput ceiling and a single point of stall. Per aggregate is the order that
/// actually matters — a lot's bid must not arrive after the lot's close — and it is achievable
/// concurrently because different aggregates are independent by definition.
/// </para>
/// </remarks>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OutboxOptions> _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;

        if (!options.Enabled)
        {
            _logger.LogInformation("Outbox processor is disabled by configuration");
            return;
        }

        _logger.LogInformation(
            "Outbox processor started: batch {BatchSize}, poll {PollInterval}",
            options.BatchSize,
            options.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            var claimed = 0;

            try
            {
                claimed = await SweepAsync(options, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A sweep that throws is a database problem, not a message problem. Swallowing it
                // here and sleeping is what keeps a five-second Postgres failover from killing the
                // hosted service permanently — a BackgroundService whose ExecuteAsync throws is
                // never restarted, and the outbox would silently stop for the life of the process.
                _logger.LogError(ex, "Outbox sweep failed; retrying after the poll interval");
            }

            // A full batch means there is almost certainly more waiting. Sleeping anyway would cap
            // drain rate at BatchSize per PollInterval, which is exactly the wrong behaviour during
            // the backlog that follows an incident.
            if (claimed >= options.BatchSize)
            {
                continue;
            }

            await Task.Delay(options.PollInterval, stoppingToken);
        }
    }

    private async Task<int> SweepAsync(OutboxOptions options, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AutoHubDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        // The connection retries transient faults, and a retrying strategy refuses to run inside a
        // transaction it did not open — correctly, because retrying one statement of a transaction
        // that has already partly executed would produce a half-applied batch. Handing the whole
        // claim-dispatch-commit unit to the strategy is what makes it retriable as a unit.
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            token => SweepInTransactionAsync(context, dispatcher, clock, options, token),
            cancellationToken);
    }

    private async Task<int> SweepInTransactionAsync(
        AutoHubDbContext context,
        IOutboxDispatcher dispatcher,
        IClock clock,
        OutboxOptions options,
        CancellationToken cancellationToken)
    {
        // A retry re-enters here with the previous attempt's entities still tracked and their
        // claim rolled back. Starting from a clean tracker is what stops the second attempt trying
        // to update rows it no longer holds a lock on.
        context.ChangeTracker.Clear();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var now = clock.UtcNow;
        var batch = await ClaimAsync(context, options, now, cancellationToken);

        if (batch.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return 0;
        }

        // Grouping preserves the claim query's ordering inside each group, which is what makes
        // "stop this aggregate at its first failure" a correct statement about delivery order.
        foreach (var group in batch.GroupBy(message => message.AggregateId))
        {
            await DispatchGroupAsync(group, dispatcher, options, now, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return batch.Count;
    }

    /// <summary>
    /// Delivers one aggregate's messages, stopping at the first failure.
    /// </summary>
    /// <remarks>
    /// Continuing past a failure would deliver <c>lot.closed</c> to a consumer that never received
    /// the <c>bid.placed</c> that closed it, and no amount of idempotency on the consumer's side
    /// repairs an order that was never offered. The rest of the group is deferred to the failed
    /// message's own retry time — not left eligible — because leaving it eligible turns one stuck
    /// message into a sweep that reclaims the same rows every two seconds forever.
    /// </remarks>
    private async Task DispatchGroupAsync(
        IEnumerable<OutboxMessage> group,
        IOutboxDispatcher dispatcher,
        OutboxOptions options,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var blocked = false;

        foreach (var message in group)
        {
            if (blocked)
            {
                message.MarkDeferred(now, options.BaseRetryDelay);
                continue;
            }

            var outcome = await SafeDispatchAsync(dispatcher, message, cancellationToken);

            if (outcome is null)
            {
                message.MarkProcessed(now);
                continue;
            }

            blocked = true;
            message.MarkFailed(now, outcome, options.BaseRetryDelay, options.MaxRetryDelay);

            if (message.IsPoisoned(options.MaxAttempts))
            {
                // Left in the table, unprocessed, with its error. The claim query will not offer it
                // again, and it will not offer anything behind it for the same aggregate either —
                // silently skipping ahead would break the ordering guarantee this table promises.
                _logger.LogError(
                    "Outbox message {MessageId} ({EventType}) is poisoned after {Attempts} attempts: {Error}",
                    message.Id,
                    message.EventType,
                    message.Attempts,
                    outcome);
            }
            else
            {
                _logger.LogWarning(
                    "Outbox message {MessageId} ({EventType}) failed on attempt {Attempts}: {Error}",
                    message.Id,
                    message.EventType,
                    message.Attempts,
                    outcome);
            }
        }
    }

    /// <summary>Returns null on success, or the failure text to record on the row.</summary>
    private static async Task<string?> SafeDispatchAsync(
        IOutboxDispatcher dispatcher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await dispatcher.DispatchAsync(message, cancellationToken);
            return result.IsSuccess ? null : result.Error.ToString();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A throwing consumer is a bug in the consumer, not a reason to lose the message or to
            // take the sweep down with it. It becomes a retry with the exception on the row.
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// Claims a batch under row locks.
    /// </summary>
    /// <remarks>
    /// The <c>NOT EXISTS</c> clause is what keeps the ordering promise honest across sweeps. A
    /// poisoned message is excluded by the <c>attempts</c> filter, and without this clause the
    /// messages queued behind it for the same aggregate would sail past it — turning "ordered per
    /// aggregate" into "ordered until something breaks", which is the same as not ordered.
    /// </remarks>
    private static async Task<List<OutboxMessage>> ClaimAsync(
        AutoHubDbContext context,
        OutboxOptions options,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var maxAttempts = options.MaxAttempts;
        var batchSize = options.BatchSize;

        return await context.OutboxMessages
            .FromSql(
                $"""
                 SELECT m.*
                 FROM platform.outbox_messages AS m
                 WHERE m.processed_at IS NULL
                   AND m.attempts < {maxAttempts}
                   AND m.next_attempt_at <= {now}
                   AND NOT EXISTS (
                         SELECT 1
                         FROM platform.outbox_messages AS blocker
                         WHERE blocker.aggregate_id = m.aggregate_id
                           AND blocker.processed_at IS NULL
                           AND blocker.attempts >= {maxAttempts}
                           AND (blocker.occurred_at, blocker.id) < (m.occurred_at, m.id))
                 ORDER BY m.aggregate_id, m.occurred_at, m.id
                 LIMIT {batchSize}
                 FOR UPDATE SKIP LOCKED
                 """)
            .ToListAsync(cancellationToken);
    }
}
