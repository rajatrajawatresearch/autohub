using AutoHub.Platform.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Workflow;

internal sealed class WorkflowStateConfiguration : IEntityTypeConfiguration<WorkflowState>
{
    public void Configure(EntityTypeBuilder<WorkflowState> builder)
    {
        builder.ToTable(
            "workflow_states",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_workflow_states_sla_positive",
                    "sla_minutes IS NULL OR sla_minutes > 0");

                // A state that is both the entrance and the exit is a workflow with one state and
                // no transitions, which `Validate` rejects. Saying so here catches the same mistake
                // made by a seed script that never calls Validate.
                table.HasCheckConstraint(
                    "ck_workflow_states_not_initial_and_terminal",
                    "NOT (is_initial AND is_terminal)");
            });

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Code).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(state => state.Name).HasMaxLength(120).IsRequired();

        // The category is what the design system maps to a StatusPill tone. Stored by name so the
        // mapping is legible in the database and a reordered enum cannot silently repaint every
        // status badge in the product.
        builder.Property(state => state.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex("WorkflowDefinitionId", nameof(WorkflowState.Code)).IsUnique();

        // Exactly one entrance per definition, enforced where it cannot be bypassed. Two initial
        // states means `Start` picks one arbitrarily and half the entities begin in the wrong place.
        builder.HasIndex("WorkflowDefinitionId")
            .IsUnique()
            .HasFilter("is_initial")
            .HasDatabaseName("ux_workflow_states_one_initial_per_definition");
    }
}
