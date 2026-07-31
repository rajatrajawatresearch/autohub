using AutoHub.Platform.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Forms;

internal sealed class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> builder)
    {
        builder.ToTable(
            "form_fields",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_form_fields_config_is_object",
                    "config_json IS NULL OR jsonb_typeof(config_json) = 'object'");

                // A visibility condition must be an object (a JsonLogic operator) or a literal
                // boolean. A bare string or number evaluates truthy, so a malformed condition would
                // show a field to everybody — which is the wrong direction when the field is
                // "battery health" and the vehicle runs on diesel.
                table.HasCheckConstraint(
                    "ck_form_fields_visible_when_shape",
                    "visible_when_json IS NULL OR jsonb_typeof(visible_when_json) IN ('object', 'boolean')");
            });

        builder.HasKey(field => field.Id);

        builder.Property(field => field.Key).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(field => field.Label).HasMaxLength(200).IsRequired();
        builder.Property(field => field.LabelKey).HasMaxLength(160);

        // The renderer's vocabulary, stored by name. A control is a capability someone wrote and
        // tested, and the name is what the API contract publishes — `"control": "photo_set"` — so
        // storing the ordinal would put a translation step between the row and the wire.
        builder.Property(field => field.Control)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(field => field.Unit).HasMaxLength(32);
        builder.Property(field => field.Placeholder).HasMaxLength(200);
        builder.Property(field => field.HelpText).HasMaxLength(512);

        // The link that stops option lists being duplicated: a fuel picker names `fuel_type` and
        // carries no options at all, and the compiler fills them from the taxonomy at serve time.
        builder.Property(field => field.AttributeCode).AsNullableCaseInsensitive().HasMaxLength(64);

        builder.Property(field => field.ConfigJson).AsNullableJsonb();
        builder.Property(field => field.VisibleWhenJson).AsNullableJsonb();

        builder.HasMany<FormValidationRule>(nameof(FormField.Rules))
            .WithOne()
            .HasForeignKey(rule => rule.FormFieldId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(FormField.Rules));

        builder.HasIndex(field => new { field.FormStepId, field.Key }).IsUnique();
        builder.HasIndex(field => new { field.FormStepId, field.SortOrder });

        // "Which forms break if we retire this attribute?" — a query rather than a grep through
        // JSON blobs, which is the reason fields are rows and not only schema_json.
        builder.HasIndex(field => field.AttributeCode).HasFilter("attribute_code IS NOT NULL");
    }
}
