using AutoHub.Persistence.Converters;
using AutoHub.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Idempotency;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable(
            "idempotency_records",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_idempotency_records_window_ordered",
                    "expires_at > created_at");

                // A status code that is not one is a row written by something other than the
                // middleware, and the next replay would return it to a client as gospel.
                table.HasCheckConstraint(
                    "ck_idempotency_records_status_is_http",
                    "response_status IS NULL OR response_status BETWEEN 100 AND 599");
            });

        // The key is the primary key. A replay check that costs a primary-key lookup can sit in
        // front of every write endpoint without anyone arguing about the overhead, and it is the
        // same index that enforces the uniqueness the whole mechanism depends on.
        builder.HasKey(record => record.Key);

        builder.Property(record => record.Key).HasMaxLength(128);
        builder.Property(record => record.Endpoint).HasMaxLength(256).IsRequired();

        // SHA-256, hex. Hashed rather than stored: the body can be large and can contain personal
        // data, and a table of retained request bodies is a liability that buys nothing — the only
        // question ever asked of it is "is this the same request?".
        builder.Property(record => record.RequestHash).HasMaxLength(64).IsFixedLength().IsRequired();

        builder.Property(record => record.ResponseBody)
            .HasConversion(NullableJsonNodeConverter.Instance)
            .HasColumnType(JsonNodeConverter.ColumnType);

        builder.Property(record => record.ResponseBody)
            .Metadata.SetValueComparer(JsonNodeComparer.Instance);

        // The sweeper's query. Idempotency is a promise with a horizon, and rows past it are dead
        // weight in the index that every write endpoint reads.
        builder.HasIndex(record => record.ExpiresAt);

        // "Show me this user's recent writes" during an incident, without scanning the table.
        builder.HasIndex(record => new { record.UserId, record.CreatedAt })
            .HasFilter("user_id IS NOT NULL");
    }
}
