using AutoHub.Platform.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Integrations;

internal sealed class IntegrationCapabilityConfiguration : IEntityTypeConfiguration<IntegrationCapability>
{
    public void Configure(EntityTypeBuilder<IntegrationCapability> builder)
    {
        builder.ToTable(
            "integration_capabilities",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_integration_capabilities_code_shape",
                "code ~ '^[a-z][a-z0-9_]*\\.[a-z][a-z0-9_]*$'"));

        builder.HasKey(capability => capability.Id);

        // Dotted domain.verb: payment.collect, kyc.pan_verify. The seam application code asks for,
        // never knowing which vendor answered.
        builder.Property(capability => capability.Code).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(capability => capability.Name).HasMaxLength(160).IsRequired();
        builder.Property(capability => capability.Description).HasMaxLength(512);

        builder.HasIndex(capability => capability.Code).IsUnique();
    }
}
