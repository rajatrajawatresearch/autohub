using AutoHub.Platform.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Workflow;

/// <summary>
/// Where each entity sits in its lifecycle. There is no <c>status</c> column anywhere else.
/// </summary>
/// <remarks>
/// The indexes here are the price of that indirection and they are the reason it is affordable.
/// "What state is this listing in?" is a unique-index seek on <c>(workflow_key, entity_id)</c>, and
/// "what is due?" is a range scan on a partial index over open instances — both cheaper than the
/// join a status column would have avoided, because the column would have needed an index too.
/// </remarks>
internal sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable(
            "workflow_instances",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint("ck_workflow_instances_version_positive", "definition_version >= 1");

                table.HasCheckConstraint(
                    "ck_workflow_instances_entered_after_started",
                    "state_entered_at >= started_at");

                // A completed instance has nothing left to do, so a due timer on one is a scheduler
                // that will keep picking it up forever and finding no transition to fire.
                table.HasCheckConstraint(
                    "ck_workflow_instances_completed_has_no_timer",
                    "completed_at IS NULL OR due_at IS NULL");
            });

        builder.HasKey(instance => instance.Id);

        builder.Property(instance => instance.WorkflowKey).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(instance => instance.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(instance => instance.CurrentStateCode).AsCaseInsensitive().HasMaxLength(64).IsRequired();

        builder.HasMany<WorkflowHistoryEntry>(nameof(WorkflowInstance.History))
            .WithOne()
            .HasForeignKey(entry => entry.WorkflowInstanceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(WorkflowInstance.History));

        // One live workflow per entity per key. Two would mean two answers to "what state is this
        // in?", and the API would return whichever came back first.
        builder.HasIndex(instance => new { instance.WorkflowKey, instance.EntityId }).IsUnique();

        // The scheduler's only query. Partial, because a completed instance is dead weight in it and
        // the overwhelming majority of rows are completed within a week of being created.
        builder.HasIndex(instance => instance.DueAt)
            .HasFilter("due_at IS NOT NULL AND completed_at IS NULL")
            .HasDatabaseName("ix_workflow_instances_due");

        // SLA reporting and the operations queue: "what is sitting in pending_review, oldest first?"
        builder.HasIndex(instance => new
            {
                instance.WorkflowKey,
                instance.CurrentStateCode,
                instance.StateEnteredAt,
            })
            .HasFilter("completed_at IS NULL")
            .HasDatabaseName("ix_workflow_instances_open_by_state");
    }
}
