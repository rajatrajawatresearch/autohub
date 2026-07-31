using AutoHub.Platform.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Pricing;

internal sealed class RuleSetConfiguration : IEntityTypeConfiguration<RuleSet>
{
    public void Configure(EntityTypeBuilder<RuleSet> builder)
    {
        builder.ToTable(
            "pricing_rule_sets",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint("ck_pricing_rule_sets_version_positive", "version >= 1");

                table.HasCheckConstraint(
                    "ck_pricing_rule_sets_publish_state",
                    "(is_published AND published_at IS NOT NULL) OR (NOT is_published AND published_at IS NULL)");
            });

        builder.HasKey(ruleSet => ruleSet.Id);

        // The commercial moment: auction.win, rental.booking, loan.referral.
        builder.Property(ruleSet => ruleSet.Scope).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(ruleSet => ruleSet.Name).HasMaxLength(160).IsRequired();

        builder.HasMany<PricingRule>(nameof(RuleSet.Rules))
            .WithOne()
            .HasForeignKey(rule => rule.RuleSetId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(RuleSet.Rules));

        builder.HasIndex(ruleSet => new { ruleSet.Scope, ruleSet.Version }).IsUnique();

        // Exactly one published set per scope, enforced by the database rather than by convention.
        // Two would make `GetPublishedAsync` pick one arbitrarily, and the answer to "what did we
        // charge?" would change between two requests with no change to any rule.
        builder.HasIndex(ruleSet => ruleSet.Scope)
            .IsUnique()
            .HasFilter("is_published")
            .HasDatabaseName("ux_pricing_rule_sets_one_published_per_scope");
    }
}
