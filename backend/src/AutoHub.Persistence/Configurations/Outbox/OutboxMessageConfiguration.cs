using AutoHub.Persistence.Converters;
using AutoHub.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(
            "outbox_messages",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint("ck_outbox_messages_attempts_not_negative", "attempts >= 0");

                // A processed message that still carries an error reads, to whoever is looking at
                // the table during an incident, as a delivery that half worked.
                table.HasCheckConstraint(
                    "ck_outbox_messages_processed_has_no_error",
                    "processed_at IS NULL OR error IS NULL");

                table.HasCheckConstraint(
                    "ck_outbox_messages_payload_is_object",
                    "jsonb_typeof(payload) = 'object'");
            });

        builder.HasKey(message => message.Id);

        builder.Property(message => message.EventType).HasMaxLength(96).IsRequired();
        builder.Property(message => message.AggregateType).HasMaxLength(96).IsRequired();

        builder.Property(message => message.Payload)
            .HasConversion(JsonNodeConverter.Instance)
            .Metadata.SetValueComparer(JsonNodeComparer.Instance);

        builder.Property(message => message.Payload)
            .HasColumnType(JsonNodeConverter.ColumnType)
            .IsRequired();

        builder.Property(message => message.Error).AsNullableText();

        // The claim query, and the only index that matters here. Partial on unprocessed rows,
        // because the table is overwhelmingly processed history: a full index would carry every
        // message ever delivered in order to find the handful that have not been. Ordered by
        // aggregate then occurrence because that is exactly the ORDER BY the processor uses, so the
        // claim comes back sorted rather than sorting a batch under a row lock.
        builder.HasIndex(message => new
            {
                message.NextAttemptAt,
                message.AggregateId,
                message.OccurredAt,
            })
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("ix_outbox_messages_pending");

        // Answers the blocker sub-query — "is anything poisoned ahead of this aggregate?" — without
        // touching the main table's heap.
        builder.HasIndex(message => new { message.AggregateId, message.OccurredAt })
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("ix_outbox_messages_pending_by_aggregate");

        // The retention sweep and the "what did we publish yesterday?" question. Processed rows are
        // deleted on a schedule; without this the sweep is a sequential scan of the whole table.
        builder.HasIndex(message => message.ProcessedAt)
            .HasFilter("processed_at IS NOT NULL")
            .HasDatabaseName("ix_outbox_messages_processed");
    }
}
