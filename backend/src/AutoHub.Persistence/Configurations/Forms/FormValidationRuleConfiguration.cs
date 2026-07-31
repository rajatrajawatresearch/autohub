using AutoHub.Platform.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Forms;

internal sealed class FormValidationRuleConfiguration : IEntityTypeConfiguration<FormValidationRule>
{
    public void Configure(EntityTypeBuilder<FormValidationRule> builder)
    {
        builder.ToTable("form_validation_rules", Schemas.Platform);

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Rule)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        // The bound, the pattern, or the option list. jsonb because a rule's argument is a number
        // for `max`, a string for `pattern` and an array for `oneOf`, and one typed column cannot
        // be all three without inventing three.
        builder.Property(rule => rule.ValueJson).AsNullableJsonb();

        // The code travels with the rule, not with the rule kind: a `max` on the odometer is
        // `kms.too_high` and a `max` on the asking price is `price.above_cap`, and a translator
        // needs to tell them apart. Once shipped it never changes — clients branch on it.
        builder.Property(rule => rule.Code).HasMaxLength(96).IsRequired();
        builder.Property(rule => rule.Message).HasMaxLength(512).IsRequired();

        builder.HasIndex(rule => new { rule.FormFieldId, rule.SortOrder });
    }
}
