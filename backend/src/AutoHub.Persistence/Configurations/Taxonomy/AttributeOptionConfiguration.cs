using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Taxonomy;

internal sealed class AttributeOptionConfiguration : IEntityTypeConfiguration<AttributeOption>
{
    public void Configure(EntityTypeBuilder<AttributeOption> builder)
    {
        builder.ToTable("attribute_options", Schemas.Platform);

        builder.HasKey(option => option.Id);

        builder.Property(option => option.Value)
            .AsCaseInsensitive()
            .IsRequired();

        builder.Property(option => option.Label).HasMaxLength(160).IsRequired();

        // Unique within its attribute, not globally: `manual` is a transmission and could equally be
        // a documentation type. Scoping the constraint to the attribute is what lets vocabularies
        // overlap without colliding.
        builder.HasIndex(option => new { option.AttributeDefinitionId, option.Value }).IsUnique();

        builder.HasIndex(option => new { option.AttributeDefinitionId, option.SortOrder });
    }
}
