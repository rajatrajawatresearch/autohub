using AutoHub.Platform.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Integrations;

internal sealed class ProviderRouteConfiguration : IEntityTypeConfiguration<ProviderRoute>
{
    public void Configure(EntityTypeBuilder<ProviderRoute> builder)
    {
        builder.ToTable(
            "provider_routes",
            Schemas.Platform,
            table =>
            {
                // A canary share. Zero means the route exists and takes nothing, which reads in the
                // admin list as configured and behaves as absent; null already means "all of it".
                table.HasCheckConstraint(
                    "ck_provider_routes_traffic_percent",
                    "traffic_percent IS NULL OR traffic_percent BETWEEN 1 AND 100");

                table.HasCheckConstraint(
                    "ck_provider_routes_window_ordered",
                    "effective_from IS NULL OR effective_to IS NULL OR effective_to > effective_from");

                table.HasCheckConstraint(
                    "ck_provider_routes_condition_shape",
                    "condition_json IS NULL OR jsonb_typeof(condition_json) IN ('object', 'boolean')");
            });

        builder.HasKey(route => route.Id);

        builder.Property(route => route.CapabilityCode).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(route => route.ProviderCode).AsCaseInsensitive().HasMaxLength(64).IsRequired();

        // Where "route UPI under ₹2,000 to the cheapest aggregator and cards to the one with the
        // best success rate" lives. Both facts change monthly from settlement reports, and neither
        // should ever have been a branch in a payment handler.
        builder.Property(route => route.ConditionJson).AsNullableJsonb();

        builder.HasOne<IntegrationCapability>()
            .WithMany()
            .HasPrincipalKey(capability => capability.Code)
            .HasForeignKey(route => route.CapabilityCode)
            .OnDelete(DeleteBehavior.Cascade);

        // Lower is tried first, and gaps are encouraged: authoring at 10, 20, 30 leaves room to
        // insert a provider at 15 without renumbering the chain.
        builder.HasIndex(route => new { route.CapabilityCode, route.Priority });

        // One route per provider per capability. Two would put the same vendor in the chain twice,
        // so a failover would retry the gateway that just failed instead of moving on.
        builder.HasIndex(route => new { route.CapabilityCode, route.ProviderCode })
            .IsUnique()
            .HasFilter("is_active");
    }
}
