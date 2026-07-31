using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Security;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoHub.Persistence.Interceptors;

/// <summary>
/// Stamps <see cref="IAuditable"/> on every insert and update.
/// </summary>
/// <remarks>
/// A field developers must remember to set is a field that is wrong. Not usually — the happy path
/// gets written by whoever wrote the entity, and it is correct on the day. It is wrong on the
/// bulk-import path added six months later, on the admin bulk-edit, and on the background job that
/// closes expired lots, because none of those authors were thinking about audit columns. And the
/// one place it is missing is the one place an investigation eventually needs.
/// <para>
/// Interception makes it unforgettable: there is no code path to <c>SaveChanges</c> that does not go
/// through here, so <c>created_by</c> is populated by construction rather than by discipline.
/// </para>
/// <para>
/// <c>CreatedAt</c> is never touched on an update, even though the entity exposes a setter. The
/// setter exists because EF materialises through it; overwriting a creation timestamp on every save
/// would silently destroy the only unambiguous fact on the row.
/// </para>
/// </remarks>
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AuditingInterceptor(ICurrentUser currentUser, IClock clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // One instant for the whole transaction. Reading the clock per entity would give the rows of
        // a single save differing timestamps, which makes "what did this transaction change?"
        // unanswerable by timestamp — the exact question an audit trail exists to answer.
        var now = _clock.UtcNow;
        var actor = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = actor;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = actor;
                    PreserveCreationStamp(entry);
                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Refuses to write <c>created_at</c> / <c>created_by</c> on an update.
    /// </summary>
    /// <remarks>
    /// Marking them unmodified rather than trusting nobody assigned them covers the case that
    /// actually happens: an entity re-attached from a DTO, where every property arrives as modified
    /// and the creation stamp travelling in that DTO is whatever the client felt like sending.
    /// </remarks>
    private static void PreserveCreationStamp(EntityEntry<IAuditable> entry)
    {
        entry.Property(a => a.CreatedAt).IsModified = false;
        entry.Property(a => a.CreatedBy).IsModified = false;
    }
}
