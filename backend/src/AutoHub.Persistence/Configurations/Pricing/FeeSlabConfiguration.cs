using AutoHub.Platform.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Pricing;

internal sealed class FeeSlabConfiguration : IEntityTypeConfiguration<FeeSlab>
{
    public void Configure(EntityTypeBuilder<FeeSlab> builder)
    {
        builder.ToTable(
            "fee_slabs",
            Schemas.Platform,
            table =>
            {
                // The upper bound is exclusive, so equal bounds is an empty band that silently
                // charges nothing for the amounts an operator thought they had covered.
                table.HasCheckConstraint(
                    "ck_fee_slabs_band_ordered",
                    "to_minor_units IS NULL OR to_minor_units > from_minor_units");

                table.HasCheckConstraint("ck_fee_slabs_from_not_negative", "from_minor_units >= 0");

                table.HasCheckConstraint(
                    "ck_fee_slabs_rate_is_a_fraction",
                    "rate IS NULL OR (rate >= 0 AND rate <= 1)");

                // A band that says neither a rate nor a flat amount contributes zero, which reads on
                // the quote as a slab that was configured and is simply generous.
                table.HasCheckConstraint(
                    "ck_fee_slabs_has_a_charge",
                    "rate IS NOT NULL OR flat_minor_units IS NOT NULL");
            });

        builder.HasKey(slab => slab.Id);

        builder.Property(slab => slab.FromMinorUnits).HasColumnType("bigint").IsRequired();
        builder.Property(slab => slab.ToMinorUnits).HasColumnType("bigint");
        builder.Property(slab => slab.Rate).HasColumnType("numeric(9,6)");
        builder.Property(slab => slab.FlatMinorUnits).HasColumnType("bigint");

        // Tiered evaluation walks the bands in ascending order and charges on the portion of the
        // amount inside each. Out of order, the arithmetic is simply wrong rather than slow.
        builder.HasIndex(slab => new { slab.FeeComponentId, slab.FromMinorUnits }).IsUnique();
    }
}
