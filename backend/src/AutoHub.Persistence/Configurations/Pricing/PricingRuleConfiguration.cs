using AutoHub.Platform.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Pricing;

internal sealed class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        builder.ToTable(
            "pricing_rules",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_pricing_rules_effective_window",
                "effective_from IS NULL OR effective_to IS NULL OR effective_to > effective_from"));

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Code).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(rule => rule.Name).HasMaxLength(160).IsRequired();

        builder.Property(rule => rule.ConditionJson).AsNullableJsonb();

        // The plain-language reason shown to whoever pays. A dealer who cannot see why they were
        // charged 2% opens a ticket, so it is part of the rule rather than a comment on it.
        builder.Property(rule => rule.Explanation).AsNullableText();

        builder.HasIndex(rule => new { rule.RuleSetId, rule.Code }).IsUnique();

        // The engine loads a whole rule set and orders by priority descending. Indexing it lets the
        // load come back sorted instead of sorting a hundred rules on every quote.
        builder.HasIndex(rule => new { rule.RuleSetId, rule.Priority })
            .IsDescending(false, true);
    }
}
