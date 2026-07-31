using AutoHub.Platform.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Workflow;

internal sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable(
            "workflow_transitions",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_workflow_transitions_timer_positive",
                "automatic_after_minutes IS NULL OR automatic_after_minutes > 0"));

        builder.HasKey(transition => transition.Id);

        builder.Property(transition => transition.ActionCode).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(transition => transition.Label).HasMaxLength(120).IsRequired();

        // State codes, not foreign keys. The definition is authored as a document and validated as
        // one — `Validate` proves every from/to resolves before publishing — and a foreign key here
        // would force states to be inserted before transitions in every seed and every import,
        // which is an ordering constraint on data that is written as one unit anyway.
        builder.Property(transition => transition.FromStateCode).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(transition => transition.ToStateCode).AsCaseInsensitive().HasMaxLength(64).IsRequired();

        builder.Property(transition => transition.RequiredPermission).HasMaxLength(128);

        builder.Property(transition => transition.GuardJson).AsNullableJsonb();

        builder.HasMany<WorkflowEffect>(nameof(WorkflowTransition.Effects))
            .WithOne()
            .HasForeignKey("WorkflowTransitionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(WorkflowTransition.Effects));

        // One action per source state. Two `approve` transitions out of `pending_review` would make
        // `FindTransition` return whichever was authored first, and the other would be dead
        // configuration that an operator can see and edit but that never fires.
        builder.HasIndex(
                "WorkflowDefinitionId",
                nameof(WorkflowTransition.FromStateCode),
                nameof(WorkflowTransition.ActionCode))
            .IsUnique();

        // The timer sweeper asks "which transitions fire on their own?" once per definition load.
        builder.HasIndex("WorkflowDefinitionId", nameof(WorkflowTransition.AutomaticAfterMinutes))
            .HasFilter("automatic_after_minutes IS NOT NULL");
    }
}
