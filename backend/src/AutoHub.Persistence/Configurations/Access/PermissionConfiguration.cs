using AutoHub.Platform.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Access;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable(
            "permissions",
            Schemas.Platform,
            table =>
                // module is the first segment of code, derived rather than entered. Enforcing the
                // relationship here means the admin matrix's grouping can never disagree with the
                // permission a check actually asks for.
                table.HasCheckConstraint(
                    "ck_permissions_module_is_code_prefix",
                    "code LIKE module || '.%'"));

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Code).AsCaseInsensitive().HasMaxLength(128).IsRequired();
        builder.Property(permission => permission.Module).AsCaseInsensitive().HasMaxLength(48).IsRequired();
        builder.Property(permission => permission.Name).HasMaxLength(160).IsRequired();
        builder.Property(permission => permission.Description).HasMaxLength(512);

        // These rows are a projection of PermissionCatalog, which is code — a permission exists
        // because a line of code checks it. Uniqueness is what makes reconciliation on boot an
        // upsert rather than a duplicate-detection problem.
        builder.HasIndex(permission => permission.Code).IsUnique();

        builder.HasIndex(permission => permission.Module);
    }
}
