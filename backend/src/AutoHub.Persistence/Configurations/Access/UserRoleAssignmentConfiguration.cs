using AutoHub.Platform.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Access;

internal sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable(
            "user_role_assignments",
            Schemas.Platform,
            table =>
            {
                // A scoped grant with no scope id is the dangerous direction of this mistake: it
                // reads as "moderator, city scope" in the admin UI while naming no city, and a
                // sloppier resolver would treat it as every city.
                table.HasCheckConstraint(
                    "ck_user_role_assignments_scope_id_matches_type",
                    "(scope_type = 'Global' AND scope_id IS NULL) OR "
                    + "(scope_type <> 'Global' AND scope_id IS NOT NULL)");

                table.HasCheckConstraint(
                    "ck_user_role_assignments_window_ordered",
                    "valid_from IS NULL OR valid_to IS NULL OR valid_to > valid_from");
            });

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.ScopeType)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        // Never cascaded. Deleting a role out from under live assignments would silently strip
        // access from whoever held it, and the first symptom would be a moderator who cannot
        // moderate.
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(assignment => assignment.RoleId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        // The resolver's only query, and it runs on every authenticated request that is not served
        // from the access cache.
        builder.HasIndex(assignment => assignment.UserId);

        // One live grant per user, role and scope. scope_id is null for global grants, and in
        // PostgreSQL nulls are distinct by default — which would let a user hold the same global
        // role a hundred times and make the resolver's union quietly quadratic. NULLS NOT DISTINCT
        // is what makes the constraint mean what it reads as.
        builder.HasIndex(assignment => new
            {
                assignment.UserId,
                assignment.RoleId,
                assignment.ScopeType,
                assignment.ScopeId,
            })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("valid_to IS NULL");

        // "Who moderates Pune?" — the audience resolver's question when a notification targets
        // permission holders within a scope.
        builder.HasIndex(assignment => new { assignment.ScopeType, assignment.ScopeId })
            .HasFilter("scope_id IS NOT NULL");
    }
}
