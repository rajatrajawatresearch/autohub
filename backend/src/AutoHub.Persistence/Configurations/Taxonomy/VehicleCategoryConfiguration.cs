using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Taxonomy;

internal sealed class VehicleCategoryConfiguration : IEntityTypeConfiguration<VehicleCategory>
{
    public void Configure(EntityTypeBuilder<VehicleCategory> builder)
    {
        builder.ToTable("vehicle_categories", Schemas.Platform);

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Code)
            .AsCaseInsensitive()
            .IsRequired();

        builder.Property(category => category.Name).HasMaxLength(120).IsRequired();
        builder.Property(category => category.Icon).HasMaxLength(64);

        // Self-reference, and never a cascade: deleting a parent must not silently take a subtree
        // of categories — and every listing under them — with it. The admin path reparents or
        // deactivates, and the database refuses to guess.
        builder.HasOne<VehicleCategory>()
            .WithMany()
            .HasForeignKey(category => category.ParentId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        builder.HasIndex(category => category.Code).IsUniqueAmongLiveRows();

        // The tree is walked parent-first on every snapshot load. Without this the walk is a
        // sequential scan per level.
        builder.HasIndex(category => new { category.ParentId, category.SortOrder });
    }
}
