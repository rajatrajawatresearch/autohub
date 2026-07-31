using System.Text.Json;
using AutoHub.Persistence.Outbox;
using AutoHub.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoHub.Persistence.Interceptors;

/// <summary>
/// Copies every raised domain event into the outbox table inside the same transaction as the state
/// change that raised it.
/// </summary>
/// <remarks>
/// <b>This is the guarantee that "the deal was created but nobody was told" is unreachable.</b>
/// Saving the aggregate and publishing its events are two systems, and any ordering of two systems
/// without a shared transaction leaves a window. Publish first and the process can die before the
/// commit, announcing a deal that does not exist. Commit first and the process can die before the
/// publish, creating a deal nobody hears about — no escrow opened, no dealer notified, no
/// commission posted, and no error anywhere.
/// <para>
/// Writing the event as a row in the aggregate's own transaction removes the window entirely:
/// PostgreSQL commits both or neither. Delivery then becomes a separate, retryable problem owned by
/// <see cref="OutboxProcessor"/>, and the worst failure it can produce is a late notification rather
/// than a lost one.
/// </para>
/// <para>
/// Events are cleared in <c>SavedChanges</c>, never in <c>SavingChanges</c>. Clearing before the
/// commit means a save that fails and is retried by the caller has already lost its events — the
/// outbox rows rolled back with everything else, and the in-memory list that could have produced
/// them again is empty.
/// </para>
/// </remarks>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Payload serialisation settings.
    /// </summary>
    /// <remarks>
    /// camelCase because the payload is a wire contract that consumers outside this codebase read,
    /// including partner webhooks; nulls are written rather than skipped so a consumer can tell
    /// "the field was absent from this version of the event" from "the field was null".
    /// </remarks>
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Enqueue(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Enqueue(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Clear(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Clear(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private static void Enqueue(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Materialised before adding anything, because adding outbox rows mutates the change
        // tracker and enumerating it while it changes throws.
        var roots = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        if (roots.Count == 0)
        {
            return;
        }

        var messages = new List<OutboxMessage>();

        foreach (var root in roots)
        {
            var aggregateType = root.GetType().Name;

            foreach (var domainEvent in root.DomainEvents)
            {
                messages.Add(OutboxMessage.From(domainEvent, aggregateType, PayloadOptions));
            }
        }

        context.Set<OutboxMessage>().AddRange(messages);
    }

    private static void Clear(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<AggregateRoot>())
        {
            entry.Entity.ClearDomainEvents();
        }
    }
}
