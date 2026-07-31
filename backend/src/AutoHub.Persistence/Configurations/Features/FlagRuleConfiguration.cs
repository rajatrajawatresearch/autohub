using AutoHub.Platform.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Features;

internal sealed class FlagRuleConfiguration : IEntityTypeConfiguration<FlagRule>
{
    public void Configure(EntityTypeBuilder<FlagRule> builder)
    {
        builder.ToTable(
            "flag_rules",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_flag_rules_condition_shape",
                "condition_json IS NULL OR jsonb_typeof(condition_json) IN ('object', 'boolean')"));

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.ConditionJson).AsNullableJsonb();

        // Shown in the admin list so nobody deletes a kill switch without reading why it exists.
        builder.Property(rule => rule.Description).HasMaxLength(512);

        // Rules are evaluated highest priority first and the first match wins, so a tie makes the
        // answer depend on row order — and a feature flag whose behaviour is unpredictable is worse
        // than no feature flag. The entity refuses a duplicate; the index refuses it for writers
        // that never loaded the entity.
        builder.HasIndex(rule => new { rule.FeatureFlagId, rule.Priority })
            .IsUnique()
            .IsDescending(false, true);
    }
}
