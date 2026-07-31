using AutoHub.Platform.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Notifications;

internal sealed class NotificationEventConfiguration : IEntityTypeConfiguration<NotificationEvent>
{
    public void Configure(EntityTypeBuilder<NotificationEvent> builder)
    {
        builder.ToTable(
            "notification_events",
            Schemas.Platform,
            table =>
            {
                // Dedupe collapses forty bids into one buzz; throttling caps the total. Both need a
                // window, and a window of zero or less means "suppress nothing" written as if it
                // meant "suppress everything".
                table.HasCheckConstraint(
                    "ck_notification_events_dedupe_window",
                    "dedupe_window_seconds IS NULL OR dedupe_window_seconds > 0");

                table.HasCheckConstraint(
                    "ck_notification_events_throttle_paired",
                    "(throttle_max_per_window IS NULL) = (throttle_window_seconds IS NULL)");

                table.HasCheckConstraint(
                    "ck_notification_events_throttle_positive",
                    "(throttle_max_per_window IS NULL OR throttle_max_per_window > 0) AND "
                    + "(throttle_window_seconds IS NULL OR throttle_window_seconds > 0)");

                // An active rule that reaches no channel is a rule that appears configured and
                // sends nothing. The entity refuses to activate one; the table refuses to hold one.
                table.HasCheckConstraint(
                    "ck_notification_events_active_has_channels",
                    "NOT is_active OR length(channels_csv) > 0");
            });

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Code).AsCaseInsensitive().HasMaxLength(128).IsRequired();

        // Matched against IDomainEvent.EventType exactly, version suffix included. Binding to the
        // versioned name is what stops a v2 payload being fed to a template that expects v1 fields.
        builder.Property(rule => rule.EventType).AsCaseInsensitive().HasMaxLength(96).IsRequired();

        // The preference bucket a user actually toggles. Users manage six categories, not two
        // hundred switches.
        builder.Property(rule => rule.CategoryCode).AsCaseInsensitive().HasMaxLength(48).IsRequired();

        builder.Property(rule => rule.ChannelsCsv).HasMaxLength(128).IsRequired();
        builder.Property(rule => rule.TemplateKey).AsCaseInsensitive().HasMaxLength(96).IsRequired();
        builder.Property(rule => rule.ConditionJson).AsNullableJsonb();
        builder.Property(rule => rule.DedupeKeyTemplate).HasMaxLength(256);

        builder.Property(rule => rule.Urgency)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        // Table-split rather than a separate table: an audience is one enum and one selector, and
        // it is read on every dispatch alongside the rule that owns it.
        builder.OwnsOne(
            rule => rule.Audience,
            audience =>
            {
                audience.Property(a => a.Kind)
                    .HasColumnName("audience_kind")
                    .HasConversion<string>()
                    .HasMaxLength(24)
                    .IsRequired();

                audience.Property(a => a.Selector)
                    .HasColumnName("audience_selector")
                    .HasMaxLength(512);
            });

        builder.Navigation(rule => rule.Audience).IsRequired();

        builder.HasIndex(rule => rule.Code).IsUnique();

        // The dispatch path's only query: every active rule bound to the event type that just fired.
        builder.HasIndex(rule => rule.EventType)
            .HasFilter("is_active")
            .HasDatabaseName("ix_notification_events_active_by_event_type");
    }
}
