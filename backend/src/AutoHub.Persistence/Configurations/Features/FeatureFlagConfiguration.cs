using AutoHub.Platform.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Features;

internal sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable(
            "feature_flags",
            Schemas.Platform,
            table => table.HasCheckConstraint("ck_feature_flags_key_shape", "key ~ '^[a-z][a-z0-9_]*$'"));

        builder.HasKey(flag => flag.Id);

        builder.Property(flag => flag.Key).AsCaseInsensitive().HasMaxLength(64).IsRequired();

        // Written for the person who finds this flag eighteen months from now and has to decide
        // whether deleting it is safe.
        builder.Property(flag => flag.Description).HasMaxLength(512);

        builder.HasMany<FlagRule>(nameof(FeatureFlag.Rules))
            .WithOne()
            .HasForeignKey(rule => rule.FeatureFlagId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(FeatureFlag.Rules));

        builder.HasIndex(flag => flag.Key).IsUnique();
    }
}
