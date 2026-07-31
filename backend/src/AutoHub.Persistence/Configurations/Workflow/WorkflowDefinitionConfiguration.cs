using AutoHub.Platform.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Workflow;

internal sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable(
            "workflow_definitions",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint("ck_workflow_definitions_version_positive", "version >= 1");

                // Published is the one-way door the entity guards. The table guards it too, because
                // a published-at with no published flag reads as a draft that has been live for
                // months, and in-flight instances pinned to it would be running against a definition
                // the admin console shows as editable.
                table.HasCheckConstraint(
                    "ck_workflow_definitions_publish_state",
                    "(is_published AND published_at IS NOT NULL) OR (NOT is_published AND published_at IS NULL)");
            });

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Key).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(definition => definition.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(definition => definition.Name).HasMaxLength(160).IsRequired();
        builder.Property(definition => definition.Description).HasMaxLength(1024);

        builder.HasMany<WorkflowState>(nameof(WorkflowDefinition.States))
            .WithOne()
            .HasForeignKey("WorkflowDefinitionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<WorkflowTransition>(nameof(WorkflowDefinition.Transitions))
            .WithOne()
            .HasForeignKey("WorkflowDefinitionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(WorkflowDefinition.States));
        builder.UsesBackingField(nameof(WorkflowDefinition.Transitions));

        // One row per key and version. A second v3 of the listing workflow would leave
        // `GetDefinitionVersionAsync` picking between two, and the instances pinned to v3 would be
        // running against whichever one the planner returned.
        builder.HasIndex(definition => new { definition.Key, definition.Version }).IsUnique();

        // The engine's hot read: the newest published version of a key. Descending on version so
        // the answer is the first index entry rather than a sort.
        builder.HasIndex(definition => new { definition.Key, definition.Version })
            .IsDescending(false, true)
            .HasFilter("is_published")
            .HasDatabaseName("ix_workflow_definitions_published_key_version");
    }
}
