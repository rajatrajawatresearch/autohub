using AutoHub.Platform.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Forms;

/// <summary>
/// One revision of a form: rows for the questions, and a compiled document for serving them.
/// </summary>
/// <remarks>
/// Both representations are indexed here because both are queried, for different reasons. The rows
/// answer "which forms collect <c>fuel_type</c>, so what breaks if we retire it?" — a join, not a
/// grep. <see cref="FormVersion.SchemaJson"/> answers "give me the sell form" in one row read on the
/// hottest screen in the product, and its GIN index answers the same governance questions against
/// the published document rather than the authored rows, which is what an audit actually asks about.
/// </remarks>
internal sealed class FormVersionConfiguration : IEntityTypeConfiguration<FormVersion>
{
    public void Configure(EntityTypeBuilder<FormVersion> builder)
    {
        builder.ToTable(
            "form_versions",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint("ck_form_versions_version_positive", "version >= 1");

                // Publishing compiles the rows into schema_json. A published version without one is
                // a form the API will serve as an empty document — a blank screen on a customer's
                // phone with no error anywhere.
                table.HasCheckConstraint(
                    "ck_form_versions_published_is_compiled",
                    "(is_published AND published_at IS NOT NULL AND schema_json IS NOT NULL) OR NOT is_published");
            });

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Title).HasMaxLength(200).IsRequired();
        builder.Property(version => version.TitleKey).HasMaxLength(160);
        builder.Property(version => version.SubmitLabel).HasMaxLength(80).IsRequired();
        builder.Property(version => version.SubmitLabelKey).HasMaxLength(160);
        builder.Property(version => version.VariantKey).HasMaxLength(96);

        builder.Property(version => version.SelectorJson).AsNullableJsonb();
        builder.Property(version => version.SchemaJson).AsNullableJsonb();

        builder.HasMany<FormStep>(nameof(FormVersion.Steps))
            .WithOne()
            .HasForeignKey(step => step.FormVersionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(FormVersion.Steps));

        // Version numbers run across the whole definition rather than per variant, so the number
        // alone identifies the document a client is holding — which is what the ETag needs.
        builder.HasIndex(version => new { version.FormDefinitionId, version.Version }).IsUnique();

        // Variant selection compares every published version of one key on every serve.
        builder.HasIndex(version => new { version.FormDefinitionId, version.Priority })
            .IsDescending(false, true)
            .HasFilter("is_published");

        // The governance query: which published forms still ask for an Aadhaar number, which ones
        // bind to an attribute we are about to retire. A full-table scan over jsonb answers it
        // eventually; a GIN index answers it while the compliance call is still on the phone.
        builder.HasIndex(version => version.SchemaJson)
            .HasMethod("gin")
            .HasFilter("schema_json IS NOT NULL");
    }
}
