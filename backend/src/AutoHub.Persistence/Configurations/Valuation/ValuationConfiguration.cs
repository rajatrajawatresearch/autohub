using AutoHub.Modules.Valuation.Domain.Valuations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Valuation;

/// <summary>
/// The quote table. Append-only in practice, and shaped so that stays true.
/// </summary>
/// <remarks>
/// A valuation is an opinion the platform published and, through the instant-offer product,
/// sometimes a commitment. Reproducing one years later needs the inputs it saw and the model version
/// that saw them, both of which are columns here — which is why there is no update path and why
/// <c>inputs_json</c> is stored verbatim rather than re-derived from the vehicle, whose values will
/// have moved by the time anyone asks.
/// </remarks>
internal sealed class ValuationConfiguration : IEntityTypeConfiguration<AutoHub.Modules.Valuation.Domain.Valuations.Valuation>
{
    public void Configure(EntityTypeBuilder<AutoHub.Modules.Valuation.Domain.Valuations.Valuation> builder)
    {
        builder.ToTable(
            "valuations",
            Schemas.Valuation,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_valuations_range",
                    "low_minor_units <= high_minor_units");

                table.HasCheckConstraint(
                    "ck_valuations_confidence",
                    "confidence >= 0 AND confidence <= 1");
            });

        builder.HasKey(valuation => valuation.Id);

        builder.Property(valuation => valuation.RegistrationCanonical).AsNullableCaseInsensitive();
        builder.Property(valuation => valuation.ModelKey).HasMaxLength(64).IsRequired();
        builder.Property(valuation => valuation.ModelVersion).HasMaxLength(96).IsRequired();
        builder.Property(valuation => valuation.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(valuation => valuation.ConfidenceBandCode).HasMaxLength(32).IsRequired();
        builder.Property(valuation => valuation.Confidence).HasColumnType("numeric(4,3)");
        builder.Property(valuation => valuation.InputsJson).AsJsonb().IsRequired();
        builder.Property(valuation => valuation.ForecastJson).AsNullableJsonb();
        builder.Property(valuation => valuation.Explanation).AsNullableText();

        builder.HasMany(valuation => valuation.Channels)
            .WithOne()
            .HasForeignKey(channel => channel.ValuationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(valuation => valuation.Adjustments)
            .WithOne()
            .HasForeignKey(adjustment => adjustment.ValuationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(AutoHub.Modules.Valuation.Domain.Valuations.Valuation.Channels));
        builder.UsesBackingField(nameof(AutoHub.Modules.Valuation.Domain.Valuations.Valuation.Adjustments));

        // "The current valuation for this vehicle" — the price-band guard's query. Shadow rows are
        // excluded from the index itself, so an evaluation model's output can never be returned by a
        // query that forgot to filter it out.
        builder.HasIndex(valuation => new { valuation.VehicleId, valuation.ExpiresAt })
            .HasFilter("is_shadow = false AND vehicle_id IS NOT NULL");

        // Coverage analysis: how many people asked about plates we could not price. The most useful
        // metric this module produces, and it only works because a failed identification still
        // writes the plate.
        builder.HasIndex(valuation => valuation.RegistrationCanonical)
            .HasFilter("registration_canonical IS NOT NULL");

        // Model comparison over a shadow period, which is the evidence a promotion is decided on.
        builder.HasIndex(valuation => new { valuation.ModelVersion, valuation.Id });
    }
}

internal sealed class ValuationChannelAmountConfiguration : IEntityTypeConfiguration<ValuationChannelAmount>
{
    public void Configure(EntityTypeBuilder<ValuationChannelAmount> builder)
    {
        builder.ToTable("valuation_channel_amounts", Schemas.Valuation);

        builder.HasKey(channel => channel.Id);

        builder.Property(channel => channel.ChannelCode).HasMaxLength(64).IsRequired();
        builder.Property(channel => channel.Label).HasMaxLength(120).IsRequired();

        // One amount per channel per quote. Two rows for `market` would make the anchor depend on
        // which one the planner returned, and the anchor is what every other channel is read against.
        builder.HasIndex(channel => new { channel.ValuationId, channel.ChannelCode }).IsUnique();
    }
}

internal sealed class ValuationAdjustmentConfiguration : IEntityTypeConfiguration<ValuationAdjustment>
{
    public void Configure(EntityTypeBuilder<ValuationAdjustment> builder)
    {
        builder.ToTable("valuation_adjustments", Schemas.Valuation);

        builder.HasKey(adjustment => adjustment.Id);

        builder.Property(adjustment => adjustment.FactorCode).HasMaxLength(64).IsRequired();
        builder.Property(adjustment => adjustment.MatchValue).HasMaxLength(64);
        builder.Property(adjustment => adjustment.Label).HasMaxLength(120).IsRequired();
        builder.Property(adjustment => adjustment.Multiplier).HasColumnType("numeric(10,6)");

        builder.HasIndex(adjustment => new { adjustment.ValuationId, adjustment.SortOrder });
    }
}
