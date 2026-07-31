using AutoHub.Platform.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Workflow;

internal sealed class WorkflowHistoryEntryConfiguration : IEntityTypeConfiguration<WorkflowHistoryEntry>
{
    public void Configure(EntityTypeBuilder<WorkflowHistoryEntry> builder)
    {
        builder.ToTable(
            "workflow_history_entries",
            Schemas.Platform,
            table =>
                // The first entry is the instance beginning: no source state, no action fired.
                // Every later entry has both. A row with one and not the other is a write that
                // bypassed ApplyTransition, which is the only thing allowed to move a state.
                table.HasCheckConstraint(
                    "ck_workflow_history_entries_origin_consistent",
                    "(from_state_code IS NULL AND action_code IS NULL) OR "
                    + "(from_state_code IS NOT NULL AND action_code IS NOT NULL)"));

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.FromStateCode).AsNullableCaseInsensitive().HasMaxLength(64);
        builder.Property(entry => entry.ToStateCode).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(entry => entry.ActionCode).AsNullableCaseInsensitive().HasMaxLength(64);
        builder.Property(entry => entry.Reason).HasMaxLength(1024);

        // The guard's inputs, verbatim. Without them "why was this transition allowed six months
        // ago?" is unanswerable once the rules have moved on — and that is exactly the question a
        // dispute or an audit asks.
        builder.Property(entry => entry.ContextJson).AsNullableJsonb();

        // interval, not a bigint of seconds. Time-in-state is read by humans and aggregated by SQL,
        // and `avg(time_in_previous_state)` on an interval is an SLA report in one line.
        builder.Property(entry => entry.TimeInPreviousState).HasColumnType("interval");

        builder.HasIndex(entry => new { entry.WorkflowInstanceId, entry.OccurredAt });
    }
}
