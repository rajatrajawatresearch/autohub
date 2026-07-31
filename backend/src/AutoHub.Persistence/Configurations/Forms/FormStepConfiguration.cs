using AutoHub.Platform.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Forms;

internal sealed class FormStepConfiguration : IEntityTypeConfiguration<FormStep>
{
    public void Configure(EntityTypeBuilder<FormStep> builder)
    {
        builder.ToTable("form_steps", Schemas.Platform);

        builder.HasKey(step => step.Id);

        builder.Property(step => step.Key).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(step => step.Title).HasMaxLength(200).IsRequired();
        builder.Property(step => step.TitleKey).HasMaxLength(160);
        builder.Property(step => step.Description).HasMaxLength(1024);
        builder.Property(step => step.DescriptionKey).HasMaxLength(160);

        builder.HasMany<FormField>(nameof(FormStep.Fields))
            .WithOne()
            .HasForeignKey(field => field.FormStepId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(FormStep.Fields));

        // The step key is what resume-where-you-left-off and funnel analytics both join on, so two
        // steps sharing one on a form would merge two pages into one number.
        builder.HasIndex(step => new { step.FormVersionId, step.Key }).IsUnique();

        builder.HasIndex(step => new { step.FormVersionId, step.SortOrder });
    }
}
