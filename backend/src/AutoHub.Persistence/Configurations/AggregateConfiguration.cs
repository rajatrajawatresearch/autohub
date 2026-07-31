using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations;

/// <summary>
/// The two things nearly every configuration in this assembly has to do.
/// </summary>
/// <remarks>
/// Both exist because the domain model deliberately exposes its collections as
/// <c>IReadOnlyList</c> over a private field. That is right for the domain — an aggregate that hands
/// out a mutable list has no invariants — and it means EF must be told to write through the field
/// rather than the property, on every collection, in every configuration. Told once here, forgotten
/// nowhere.
/// </remarks>
internal static class AggregateConfiguration
{
    /// <summary>
    /// Makes EF populate a read-only navigation through its backing field.
    /// </summary>
    /// <remarks>
    /// Without it EF throws at model-build time on a navigation it cannot add to. The failure is at
    /// least loud; the reason this is a helper rather than a line in each file is that the loud
    /// failure arrives during migration scaffolding, which is the least convenient moment.
    /// </remarks>
    public static void UsesBackingField<TEntity>(this EntityTypeBuilder<TEntity> builder, string navigation)
        where TEntity : class
    {
        var metadata = builder.Metadata.FindNavigation(navigation)
            ?? throw new InvalidOperationException(
                $"{typeof(TEntity).Name} has no navigation '{navigation}' to configure.");

        metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
    }

    /// <summary>
    /// The delete behaviour every reference to a soft-deletable row uses.
    /// </summary>
    /// <remarks>
    /// The database half of a two-part answer. <c>NoAction</c> emits a foreign key with no
    /// <c>ON DELETE</c> clause, so PostgreSQL refuses a hard delete of a row that something still
    /// points at — the right direction for master data, where the mistake worth preventing is a
    /// repair script quietly orphaning a hundred thousand listings.
    /// <para>
    /// The client half lives in <see cref="AutoHubDbContext"/>, which defers cascade evaluation to
    /// <c>SaveChanges</c>. Without that, EF acts the instant <c>Remove(principal)</c> is called —
    /// long before <see cref="Interceptors.SoftDeleteInterceptor"/> can say the removal was going
    /// to be an <c>UPDATE</c> — and throws about a severed required relationship. Neither half works
    /// alone.
    /// </para>
    /// </remarks>
    public const DeleteBehavior WhenPrincipalIsSoftDeleted = DeleteBehavior.NoAction;

    /// <summary>
    /// Uniqueness that ignores rows already hidden.
    /// </summary>
    /// <remarks>
    /// A plain unique index on a soft-deletable natural key makes deletion permanent in the one way
    /// that matters: the code can never be used again, because the hidden row still owns it. An
    /// operator who deletes the <c>suv</c> category by mistake and recreates it would get a
    /// constraint violation and no explanation. Filtering the index on <c>deleted_at IS NULL</c>
    /// keeps the constraint true for live rows and lets the mistake be undone.
    /// </remarks>
    public static IndexBuilder<TEntity> IsUniqueAmongLiveRows<TEntity>(this IndexBuilder<TEntity> builder)
        => builder.IsUnique().HasFilter("deleted_at IS NULL");
}
