using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Taxonomy;

internal sealed class VehicleVariantConfiguration : IEntityTypeConfiguration<VehicleVariant>
{
    public void Configure(EntityTypeBuilder<VehicleVariant> builder)
    {
        builder.ToTable(
            "vehicle_variants",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_vehicle_variants_year_range",
                    "year_from IS NULL OR year_to IS NULL OR year_to >= year_from");

                // An EV has no displacement, which is a fact and stays null. Zero is not a fact —
                // it is an import that mapped an empty cell to a default, and it makes an electric
                // car look like a broken petrol one on every spec sheet.
                table.HasCheckConstraint(
                    "ck_vehicle_variants_engine_cc_positive",
                    "engine_cc IS NULL OR engine_cc > 0");

                table.HasCheckConstraint(
                    "ck_vehicle_variants_seats_positive",
                    "seats IS NULL OR seats > 0");

                // The bag is looked up by key by every consumer. An array or a scalar here parses
                // fine and then reads as null for every key anyone asks for.
                table.HasCheckConstraint(
                    "ck_vehicle_variants_spec_is_object",
                    "spec_json IS NULL OR jsonb_typeof(spec_json) = 'object'");
            });

        builder.HasKey(variant => variant.Id);

        builder.Property(variant => variant.Code)
            .AsCaseInsensitive()
            .IsRequired();

        builder.Property(variant => variant.Name).HasMaxLength(160).IsRequired();

        // Attribute option values, resolved against the taxonomy — never enums. Hydrogen, CNG
        // retrofit and strong hybrid all arrived after any enum a developer would have written.
        builder.Property(variant => variant.FuelCode).AsNullableCaseInsensitive();
        builder.Property(variant => variant.TransmissionCode).AsNullableCaseInsensitive();
        builder.Property(variant => variant.BodyTypeCode).AsNullableCaseInsensitive();

        builder.Property(variant => variant.PowerBhp).HasColumnType("numeric(8,2)");

        builder.Property(variant => variant.SpecJson).AsNullableJsonb();

        builder.HasOne<VehicleModel>()
            .WithMany()
            .HasForeignKey(variant => variant.ModelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique within the model, not globally: `vxi-amt` is a Maruti trim name and a dozen other
        // manufacturers use the same letters.
        builder.HasIndex(variant => new { variant.ModelId, variant.Code }).IsUniqueAmongLiveRows();

        // The only query anyone runs against this table: variants of one model, optionally for one
        // model year.
        builder.HasIndex(variant => new { variant.ModelId, variant.IsActive, variant.SortOrder })
            .HasFilter("deleted_at IS NULL");

        // The bag is display-only by design, but "which variants claim a 60 kWh battery?" is a data
        // quality question somebody asks on import day, and a GIN index makes it a containment
        // query instead of a full scan.
        builder.HasIndex(variant => variant.SpecJson).HasMethod("gin");
    }
}
