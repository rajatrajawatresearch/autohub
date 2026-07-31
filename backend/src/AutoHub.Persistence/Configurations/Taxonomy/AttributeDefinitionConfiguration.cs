using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Taxonomy;

internal sealed class AttributeDefinitionConfiguration : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        builder.ToTable(
            "attribute_definitions",
            Schemas.Platform,
            table =>
            {
                // The entity refuses these on the way in; the table refuses them for everything that
                // does not come through the entity — a bulk import, a data fix, a seeding script.
                // An inverted bound is not a cosmetic problem: it produces a range facet whose
                // slider has no valid position and a validator that rejects every value.
                table.HasCheckConstraint(
                    "ck_attribute_definitions_bounds_ordered",
                    "min_number IS NULL OR max_number IS NULL OR max_number >= min_number");

                table.HasCheckConstraint(
                    "ck_attribute_definitions_step_positive",
                    "step IS NULL OR step > 0");

                table.HasCheckConstraint(
                    "ck_attribute_definitions_lengths_ordered",
                    "(min_length IS NULL OR min_length >= 0) AND "
                    + "(min_length IS NULL OR max_length IS NULL OR max_length >= min_length)");
            });

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Code)
            .AsCaseInsensitive()
            .IsRequired();

        builder.Property(definition => definition.Label).HasMaxLength(160).IsRequired();

        // Stored as the enum's name rather than its number. These tables are read directly by
        // operations far more often than by code, and `integer` in a psql session is a column
        // nobody can interpret without opening the source.
        builder.Property(definition => definition.DataType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(definition => definition.FacetStyle)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(definition => definition.Unit).HasMaxLength(32);
        builder.Property(definition => definition.DisplayGroup).HasMaxLength(64);

        // numeric without precision: these bounds describe attributes as different as kilometres,
        // brake horsepower and paise, and any precision chosen here would be wrong for one of them.
        builder.Property(definition => definition.MinNumber).HasColumnType("numeric");
        builder.Property(definition => definition.MaxNumber).HasColumnType("numeric");
        builder.Property(definition => definition.Step).HasColumnType("numeric");

        builder.Property(definition => definition.Pattern).HasMaxLength(512);

        builder.HasMany<AttributeOption>(nameof(AttributeDefinition.Options))
            .WithOne()
            .HasForeignKey(option => option.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(AttributeDefinition.Options));

        builder.HasIndex(definition => definition.Code).IsUniqueAmongLiveRows();

        // The search indexer asks for exactly this set on every rebuild.
        builder.HasIndex(definition => new { definition.IsSearchable, definition.IsActive })
            .HasFilter("deleted_at IS NULL");
    }
}
