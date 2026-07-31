using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Taxonomy;

internal sealed class CategoryAttributeConfiguration : IEntityTypeConfiguration<CategoryAttribute>
{
    public void Configure(EntityTypeBuilder<CategoryAttribute> builder)
    {
        builder.ToTable("category_attributes", Schemas.Platform);

        builder.HasKey(link => link.Id);

        builder.Property(link => link.FacetStyleOverride)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(link => link.DisplayGroupOverride).HasMaxLength(64);

        // Cascade from the category: the join row is meaningless without the node it attaches to.
        builder.HasOne<VehicleCategory>()
            .WithMany()
            .HasForeignKey(link => link.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Never cascaded from the attribute. Deleting a definition that categories still expose
        // would silently shorten every sell form and spec sheet that depends on it; the operator
        // detaches it deliberately, which is the moment they find out what it was used for.
        builder.HasOne<AttributeDefinition>()
            .WithMany()
            .HasForeignKey(link => link.AttributeDefinitionId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        // One row per pair. Two rows for one attribute on one category would make inheritance
        // resolution order-dependent, and the order would be whatever the query planner felt like.
        builder.HasIndex(link => new { link.CategoryId, link.AttributeDefinitionId }).IsUnique();
    }
}
