using AutoHub.Platform.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Integrations;

internal sealed class IntegrationProviderConfiguration : IEntityTypeConfiguration<IntegrationProvider>
{
    public void Configure(EntityTypeBuilder<IntegrationProvider> builder)
    {
        builder.ToTable(
            "integration_providers",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_integration_providers_timeout_positive",
                    "timeout_seconds > 0 AND timeout_seconds <= 300");

                table.HasCheckConstraint(
                    "ck_integration_providers_attempts_positive",
                    "max_attempts >= 1 AND max_attempts <= 5");

                // The last line of defence on the one column that must never hold a secret.
                // The entity refuses an obvious paste; the table refuses one that arrived through a
                // migration, a seed script or a support engineer's UPDATE. A reference is short and
                // scheme-prefixed — vault://autohub/payments/razorpay/live — and a gateway key is
                // neither.
                table.HasCheckConstraint(
                    "ck_integration_providers_credential_is_a_reference",
                    "credential_ref ~ '^[a-z][a-z0-9+.-]*:' AND length(credential_ref) <= 128");

                table.HasCheckConstraint(
                    "ck_integration_providers_config_is_object",
                    "config_json IS NULL OR jsonb_typeof(config_json) = 'object'");
            });

        builder.HasKey(provider => provider.Id);

        builder.Property(provider => provider.Code).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(provider => provider.CapabilityCode).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(provider => provider.DisplayName).HasMaxLength(160).IsRequired();

        builder.Property(provider => provider.Environment)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(provider => provider.ConfigJson).AsNullableJsonb();
        builder.Property(provider => provider.CredentialRef).HasMaxLength(128).IsRequired();

        builder.HasOne<IntegrationCapability>()
            .WithMany()
            .HasPrincipalKey(capability => capability.Code)
            .HasForeignKey(provider => provider.CapabilityCode)
            .OnDelete(DeleteBehavior.Restrict);

        // Sandbox and live are separate rows, never one row with a toggle — which is how a test key
        // reaches production at 4pm on a Friday. The environment is therefore part of the key.
        builder.HasIndex(provider => new
            {
                provider.CapabilityCode,
                provider.Code,
                provider.Environment,
            })
            .IsUnique();
    }
}
