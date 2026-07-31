using AutoHub.Platform.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Access;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", Schemas.Platform);

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Code).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(role => role.Name).HasMaxLength(160).IsRequired();
        builder.Property(role => role.Description).HasMaxLength(512);

        builder.HasMany<RolePermission>(nameof(Role.Permissions))
            .WithOne()
            .HasForeignKey(grant => grant.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(Role.Permissions));

        builder.HasIndex(role => role.Code).IsUniqueAmongLiveRows();
    }
}
