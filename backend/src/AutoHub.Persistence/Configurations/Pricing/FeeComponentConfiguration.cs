using AutoHub.Platform.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Pricing;

internal sealed class FeeComponentConfiguration : IEntityTypeConfiguration<FeeComponent>
{
    public void Configure(EntityTypeBuilder<FeeComponent> builder)
    {
        builder.ToTable(
            "fee_components",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_fee_components_clamp_ordered",
                    "min_minor_units IS NULL OR max_minor_units IS NULL OR max_minor_units >= min_minor_units");

                // A rate is a fraction, never a percentage: 2% is 0.02. The confusion between the
                // two is a 100x billing error, and it is the single most expensive typo available
                // in this table. Bounding it at 1 does not catch every mistake, but it catches the
                // one where somebody typed 2.
                table.HasCheckConstraint(
                    "ck_fee_components_rate_is_a_fraction",
                    "rate IS NULL OR (rate >= 0 AND rate <= 1)");

                // Each basis needs a different field filled in, and a component with neither is a
                // quote line that silently computes to zero — revenue that vanishes without an error.
                table.HasCheckConstraint(
                    "ck_fee_components_basis_has_its_input",
                    "(basis = 'Flat' AND flat_minor_units IS NOT NULL) OR "
                    + "(basis IN ('Percent', 'PercentOfLines') AND rate IS NOT NULL) OR "
                    + "(basis IN ('Slab', 'Tiered'))");
            });

        builder.HasKey(component => component.Id);

        builder.Property(component => component.Code).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(component => component.Label).HasMaxLength(160).IsRequired();

        builder.Property(component => component.Basis)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        // numeric(9,6): six decimal places is enough for a 0.000125 aggregator rate and exact,
        // which a float is not. A commission rate stored as a float is a rounding difference per
        // transaction that nobody can reproduce.
        builder.Property(component => component.Rate).HasColumnType("numeric(9,6)");

        // Amounts are minor units in bigint, everywhere, always.
        builder.Property(component => component.FlatMinorUnits).HasColumnType("bigint");
        builder.Property(component => component.MinMinorUnits).HasColumnType("bigint");
        builder.Property(component => component.MaxMinorUnits).HasColumnType("bigint");

        builder.Property(component => component.AppliesToCsv).HasMaxLength(512);

        builder.HasMany<FeeSlab>(nameof(FeeComponent.Slabs))
            .WithOne()
            .HasForeignKey(slab => slab.FeeComponentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(FeeComponent.Slabs));

        // One line code per rule. Two `gst` components on one rule bills GST twice, and the quote
        // would show both lines and look deliberate.
        builder.HasIndex(component => new { component.PricingRuleId, component.Code }).IsUnique();

        // Lines are emitted in sort order, and the order matters: PercentOfLines charges on the
        // lines computed before it, so GST must sort after the commission it applies to.
        builder.HasIndex(component => new { component.PricingRuleId, component.SortOrder });
    }
}
