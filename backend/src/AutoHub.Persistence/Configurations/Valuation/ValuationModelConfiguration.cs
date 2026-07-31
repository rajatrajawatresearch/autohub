using AutoHub.Modules.Valuation.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Valuation;

/// <summary>
/// The coefficient registry. Every number the valuation model runs on is a row under here.
/// </summary>
/// <remarks>
/// Shaped like the workflow and pricing registries on purpose — key, version, published flag,
/// immutable once published — because an operator who has learned one registry should not have to
/// learn a third grammar for the third one, and because the reproducibility requirement is identical:
/// a quote from six months ago has to re-run against the coefficients that produced it.
/// </remarks>
internal sealed class ValuationModelDefinitionConfiguration : IEntityTypeConfiguration<ValuationModelDefinition>
{
    public void Configure(EntityTypeBuilder<ValuationModelDefinition> builder)
    {
        builder.ToTable(
            "valuation_models",
            Schemas.Valuation,
            table => table.HasCheckConstraint(
                "ck_valuation_models_horizons",
                "forecast_months > 0 AND resale_horizon_months > 0 AND validity_days > 0"));

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Key).HasMaxLength(64).IsRequired();
        builder.Property(definition => definition.Name).HasMaxLength(120).IsRequired();
        builder.Property(definition => definition.ConditionJson).AsNullableJsonb();
        builder.Property(definition => definition.ExplanationTemplate).AsNullableText();
        builder.Property(definition => definition.RequiredInputsCsv).HasMaxLength(512);

        builder.HasMany(definition => definition.Curves)
            .WithOne()
            .HasForeignKey(curve => curve.ModelDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(definition => definition.Factors)
            .WithOne()
            .HasForeignKey(factor => factor.ModelDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(definition => definition.Channels)
            .WithOne()
            .HasForeignKey(channel => channel.ModelDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(definition => definition.ConfidenceWeights)
            .WithOne()
            .HasForeignKey(weight => weight.ModelDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(definition => definition.ConfidenceBands)
            .WithOne()
            .HasForeignKey(band => band.ModelDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(ValuationModelDefinition.Curves));
        builder.UsesBackingField(nameof(ValuationModelDefinition.Factors));
        builder.UsesBackingField(nameof(ValuationModelDefinition.Channels));
        builder.UsesBackingField(nameof(ValuationModelDefinition.ConfidenceWeights));
        builder.UsesBackingField(nameof(ValuationModelDefinition.ConfidenceBands));

        // One row per key and version. Publishing v4 while v4 already exists would leave two
        // definitions claiming the same `modelVersion`, and a stored quote could then no longer be
        // reproduced unambiguously — which is the one thing this table exists to guarantee.
        builder.HasIndex(definition => new { definition.Key, definition.Version }).IsUnique();

        builder.HasIndex(definition => new { definition.IsPublished, definition.Priority });
    }
}

internal sealed class ValuationCurveConfiguration : IEntityTypeConfiguration<ValuationCurve>
{
    public void Configure(EntityTypeBuilder<ValuationCurve> builder)
    {
        builder.ToTable("valuation_curves", Schemas.Valuation);

        builder.HasKey(curve => curve.Id);

        builder.Property(curve => curve.Code).HasMaxLength(64).IsRequired();

        builder.HasMany(curve => curve.Points)
            .WithOne()
            .HasForeignKey(point => point.CurveId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(ValuationCurve.Points));

        builder.HasIndex(curve => new { curve.ModelDefinitionId, curve.Code }).IsUnique();
    }
}

internal sealed class CurvePointConfiguration : IEntityTypeConfiguration<CurvePoint>
{
    public void Configure(EntityTypeBuilder<CurvePoint> builder)
    {
        builder.ToTable("valuation_curve_points", Schemas.Valuation);

        builder.HasKey(point => point.Id);

        builder.Property(point => point.X).HasColumnType("numeric(12,4)");
        builder.Property(point => point.Y).HasColumnType("numeric(12,6)");

        // Two points at the same x would make interpolation depend on row order. The engine takes
        // the left value defensively; the constraint means it never has to.
        builder.HasIndex(point => new { point.CurveId, point.X }).IsUnique();
    }
}

internal sealed class ValuationFactorConfiguration : IEntityTypeConfiguration<ValuationFactor>
{
    public void Configure(EntityTypeBuilder<ValuationFactor> builder)
    {
        builder.ToTable(
            "valuation_factors",
            Schemas.Valuation,
            table => table.HasCheckConstraint(
                // A factor supplies either a ratio or an amount. A row with both is ambiguous and a
                // row with neither silently does nothing, which is worse than failing.
                "ck_valuation_factors_one_value",
                "num_nonnulls(multiplier, amount_minor_units) = 1"));

        builder.HasKey(factor => factor.Id);

        builder.Property(factor => factor.FactorCode).HasMaxLength(64).IsRequired();
        builder.Property(factor => factor.MatchValue).AsCaseInsensitive().IsRequired();
        builder.Property(factor => factor.Multiplier).HasColumnType("numeric(10,6)");
        builder.Property(factor => factor.Label).HasMaxLength(120);

        // One value per dimension per model. Two `city:pune` rows would price Pune differently
        // depending on the query plan, which is the least debuggable class of pricing bug there is.
        builder.HasIndex(factor => new { factor.ModelDefinitionId, factor.FactorCode, factor.MatchValue })
            .IsUnique();
    }
}

internal sealed class ValuationChannelSpreadConfiguration : IEntityTypeConfiguration<ValuationChannelSpread>
{
    public void Configure(EntityTypeBuilder<ValuationChannelSpread> builder)
    {
        builder.ToTable("valuation_channel_spreads", Schemas.Valuation);

        builder.HasKey(channel => channel.Id);

        builder.Property(channel => channel.ChannelCode).HasMaxLength(64).IsRequired();
        builder.Property(channel => channel.Label).HasMaxLength(120).IsRequired();
        builder.Property(channel => channel.Spread).HasColumnType("numeric(10,6)");

        builder.HasIndex(channel => new { channel.ModelDefinitionId, channel.ChannelCode }).IsUnique();

        // Exactly one anchor per model. Publish-time validation checks it too; the index is what
        // stops a second one arriving through a seed script that skipped the aggregate.
        builder.HasIndex(channel => channel.ModelDefinitionId)
            .IsUnique()
            .HasFilter("is_primary")
            .HasDatabaseName("ux_valuation_channel_spreads_primary");
    }
}

internal sealed class ConfidenceWeightConfiguration : IEntityTypeConfiguration<ConfidenceWeight>
{
    public void Configure(EntityTypeBuilder<ConfidenceWeight> builder)
    {
        builder.ToTable("valuation_confidence_weights", Schemas.Valuation);

        builder.HasKey(weight => weight.Id);

        builder.Property(weight => weight.InputCode).HasMaxLength(64).IsRequired();
        builder.Property(weight => weight.Weight).HasColumnType("numeric(6,4)");

        builder.HasIndex(weight => new { weight.ModelDefinitionId, weight.InputCode }).IsUnique();
    }
}

internal sealed class ConfidenceBandConfiguration : IEntityTypeConfiguration<ConfidenceBand>
{
    public void Configure(EntityTypeBuilder<ConfidenceBand> builder)
    {
        builder.ToTable(
            "valuation_confidence_bands",
            Schemas.Valuation,
            table => table.HasCheckConstraint(
                "ck_valuation_confidence_bands_range",
                "minimum_score >= 0 AND minimum_score <= 1"));

        builder.HasKey(band => band.Id);

        builder.Property(band => band.Code).HasMaxLength(32).IsRequired();
        builder.Property(band => band.MinimumScore).HasColumnType("numeric(4,3)");

        builder.HasIndex(band => new { band.ModelDefinitionId, band.Code }).IsUnique();
    }
}
