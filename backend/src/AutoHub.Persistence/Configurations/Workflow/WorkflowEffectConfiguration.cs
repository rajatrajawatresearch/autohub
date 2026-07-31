using AutoHub.Platform.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Workflow;

internal sealed class WorkflowEffectConfiguration : IEntityTypeConfiguration<WorkflowEffect>
{
    public void Configure(EntityTypeBuilder<WorkflowEffect> builder)
    {
        builder.ToTable(
            "workflow_effects",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_workflow_effects_arguments_are_object",
                "arguments_json IS NULL OR jsonb_typeof(arguments_json) = 'object'"));

        builder.HasKey(effect => effect.Id);

        // A handler key, not a script. The boundary is deliberate: which effects run is data an
        // operator edits, what an effect can do is code a developer wrote and tested. Publishing
        // validates that every code here has a registered handler.
        builder.Property(effect => effect.EffectCode).AsCaseInsensitive().HasMaxLength(96).IsRequired();

        builder.Property(effect => effect.ArgumentsJson).AsNullableJsonb();

        // Effects are ordered, and the order is load-bearing: posting a commission before sending
        // the confirmation means the confirmation is not sent when the posting fails.
        builder.HasIndex("WorkflowTransitionId", nameof(WorkflowEffect.SortOrder));
    }
}
