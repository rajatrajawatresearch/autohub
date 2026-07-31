using AutoHub.Persistence.Converters;
using AutoHub.Platform.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Auditing;

/// <summary>
/// The append-only trail. Every column here is answering a question an investigation asks.
/// </summary>
/// <remarks>
/// No soft delete and no <c>IAuditable</c> columns, on purpose: a row that can be updated or hidden
/// is not an audit trail, it is a log with extra steps. Retention is a scheduled archival job that
/// moves rows out whole, never an <c>UPDATE</c>.
/// </remarks>
internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries", Schemas.Platform);

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Action).AsCaseInsensitive().HasMaxLength(96).IsRequired();
        builder.Property(entry => entry.EntityType).HasMaxLength(96).IsRequired();
        builder.Property(entry => entry.EntityKey).HasMaxLength(160);
        builder.Property(entry => entry.ActorIp).HasMaxLength(64);
        builder.Property(entry => entry.TraceId).HasMaxLength(64);
        builder.Property(entry => entry.Summary).HasMaxLength(512);

        builder.Property(entry => entry.Before)
            .HasConversion(NullableJsonNodeConverter.Instance)
            .HasColumnType(JsonNodeConverter.ColumnType)
            .Metadata.SetValueComparer(JsonNodeComparer.Instance);

        builder.Property(entry => entry.After)
            .HasConversion(NullableJsonNodeConverter.Instance)
            .HasColumnType(JsonNodeConverter.ColumnType)
            .Metadata.SetValueComparer(JsonNodeComparer.Instance);

        // The activity feed: newest first, optionally filtered to one kind of action. Descending on
        // the clustering column so "the last fifty things that happened" is the front of the index.
        builder.HasIndex(entry => entry.OccurredAt).IsDescending();

        builder.HasIndex(entry => new { entry.Action, entry.OccurredAt }).IsDescending(false, true);

        // "What has this person done?" — the first query an access review runs.
        builder.HasIndex(entry => new { entry.ActorId, entry.OccurredAt })
            .IsDescending(false, true)
            .HasFilter("actor_id IS NOT NULL");

        // "What happened to this rule set?" — the first query a billing dispute runs.
        builder.HasIndex(entry => new { entry.EntityType, entry.EntityKey, entry.OccurredAt })
            .IsDescending(false, false, true);
    }
}
