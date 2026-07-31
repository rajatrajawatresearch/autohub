using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Security;
using AutoHub.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoHub.Persistence.Interceptors;

/// <summary>
/// Turns <c>Remove</c> into a <c>deleted_at</c> stamp for anything that is
/// <see cref="ISoftDeletable"/>.
/// </summary>
/// <remarks>
/// A sold listing, a terminated attachment contract and a rejected KYC record all have to survive
/// deletion — tax law, dispute resolution and fraud investigation each need them, sometimes years
/// later. The DPDP Act's erasure right is served by a separate anonymisation job that scrubs
/// personal fields while preserving the transaction record, not by dropping rows.
/// <para>
/// Doing it here rather than by convention means <c>context.Remove(listing)</c> is safe to write.
/// The alternative — every caller remembering to call a <c>MarkDeleted()</c> method — has exactly
/// the same failure mode as hand-written audit columns, and the row it loses is one nobody can get
/// back.
/// </para>
/// <para>
/// Hidden rows are excluded by the global query filters configured in
/// <see cref="AutoHubDbContext"/>, so a soft-deleted row is invisible to every query that does not
/// explicitly call <c>IgnoreQueryFilters</c>.
/// </para>
/// </remarks>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public SoftDeleteInterceptor(ICurrentUser currentUser, IClock clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Convert(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Convert(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Convert(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        var actor = _currentUser.UserId;

        var softDeleted = new Dictionary<Type, HashSet<object>>();

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.Entity.DeletedAt = now;
            entry.Entity.DeletedBy = actor;

            // Modified, not Unchanged-with-two-modified-properties. A soft delete is an update, and
            // an update is what the auditing interceptor needs to see so `updated_at` and
            // `updated_by` are stamped too — "who hid this row" is the whole point of the exercise.
            entry.State = EntityState.Modified;

            Record(softDeleted, entry);
        }

        if (softDeleted.Count > 0)
        {
            SpareCascadedChildren(context, softDeleted);
        }
    }

    private static void Record(Dictionary<Type, HashSet<object>> softDeleted, EntityEntry<ISoftDeletable> entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();

        if (primaryKey is null || primaryKey.Properties.Count != 1)
        {
            return;
        }

        var keyValue = entry.Property(primaryKey.Properties[0].Name).CurrentValue;

        if (keyValue is null)
        {
            return;
        }

        if (!softDeleted.TryGetValue(entry.Metadata.ClrType, out var keys))
        {
            keys = [];
            softDeleted[entry.Metadata.ClrType] = keys;
        }

        keys.Add(keyValue);
    }

    /// <summary>
    /// Rescues children that EF marked for deletion because their parent was removed.
    /// </summary>
    /// <remarks>
    /// This is the bug this interceptor would otherwise introduce, and it is silent. Removing an
    /// attribute definition whose options happen to be loaded makes EF cascade the options to
    /// <c>Deleted</c>; the parent then becomes an <c>UPDATE</c> here while the children stay
    /// <c>DELETE</c>. The result is a soft-deleted definition that can be restored and whose option
    /// list is gone — which surfaces as a fuel dropdown that renders empty on every listing written
    /// before the mistake.
    /// <para>
    /// Cascade only happens for children EF has actually loaded, so this runs rarely. It has to be
    /// right when it does.
    /// </para>
    /// </remarks>
    private static void SpareCascadedChildren(DbContext context, Dictionary<Type, HashSet<object>> softDeleted)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Deleted || entry.Entity is ISoftDeletable)
            {
                continue;
            }

            foreach (var foreignKey in entry.Metadata.GetForeignKeys())
            {
                if (foreignKey.Properties.Count != 1)
                {
                    continue;
                }

                if (!softDeleted.TryGetValue(foreignKey.PrincipalEntityType.ClrType, out var principals))
                {
                    continue;
                }

                var value = entry.Property(foreignKey.Properties[0].Name).CurrentValue;

                if (value is not null && principals.Contains(value))
                {
                    entry.State = EntityState.Unchanged;
                    break;
                }
            }
        }
    }
}
