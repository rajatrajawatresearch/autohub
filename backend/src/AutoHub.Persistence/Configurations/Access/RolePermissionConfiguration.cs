using AutoHub.Platform.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Access;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions", Schemas.Platform);

        builder.HasKey(grant => grant.Id);

        // Restrict, not cascade. A permission is only removed when the code that checked it is
        // removed, and at that moment the grants that referenced it are the record of who used to
        // hold it. Cascading would delete the answer to "who could approve payouts last March?"
        // as a side effect of a refactor.
        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(grant => grant.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // One grant per pair. The admin matrix submits every checkbox on every save, so without
        // this a save that re-ticks an already-ticked box would add a second row each time.
        builder.HasIndex(grant => new { grant.RoleId, grant.PermissionId }).IsUnique();

        // The resolver's read: every permission code a user's roles grant, in one join.
        builder.HasIndex(grant => grant.PermissionId);
    }
}
