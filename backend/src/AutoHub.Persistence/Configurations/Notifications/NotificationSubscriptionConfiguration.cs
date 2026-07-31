using AutoHub.Platform.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Notifications;

internal sealed class NotificationSubscriptionConfiguration : IEntityTypeConfiguration<NotificationSubscription>
{
    public void Configure(EntityTypeBuilder<NotificationSubscription> builder)
    {
        builder.ToTable(
            "notification_subscriptions",
            Schemas.Platform,
            table =>
            {
                // Minutes past midnight IST, so the 22:00–07:00 wrap is arithmetic rather than a
                // special case somebody forgets. Out-of-range minutes make Contains() answer
                // nonsense for every message.
                table.HasCheckConstraint(
                    "ck_notification_subscriptions_quiet_hours_in_range",
                    "(quiet_hours_start_minute_ist IS NULL OR "
                    + "quiet_hours_start_minute_ist BETWEEN 0 AND 1439) AND "
                    + "(quiet_hours_end_minute_ist IS NULL OR "
                    + "quiet_hours_end_minute_ist BETWEEN 0 AND 1439)");

                table.HasCheckConstraint(
                    "ck_notification_subscriptions_quiet_hours_paired",
                    "(quiet_hours_start_minute_ist IS NULL) = (quiet_hours_end_minute_ist IS NULL)");
            });

        builder.HasKey(subscription => subscription.Id);

        // Null is the user-level default that every category inherits. It exists so quiet hours and
        // DND — decisions about a person, not about a topic — are stated once, while channel choices
        // stay per topic.
        builder.Property(subscription => subscription.CategoryCode).AsNullableCaseInsensitive().HasMaxLength(48);

        builder.OwnsOne(
            subscription => subscription.QuietHours,
            quietHours =>
            {
                quietHours.Property(q => q.StartMinuteIst).HasColumnName("quiet_hours_start_minute_ist");
                quietHours.Property(q => q.EndMinuteIst).HasColumnName("quiet_hours_end_minute_ist");
            });

        builder.HasMany<ChannelPreference>(nameof(NotificationSubscription.Channels))
            .WithOne()
            .HasForeignKey(preference => preference.NotificationSubscriptionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(NotificationSubscription.Channels));

        // One row per user per category, with the user-level default as the null row. NULLS NOT
        // DISTINCT is what makes that null row unique — without it a user accumulates a new default
        // every time they open the settings screen.
        builder.HasIndex(subscription => new { subscription.UserId, subscription.CategoryCode })
            .IsUnique()
            .AreNullsDistinct(false);
    }
}
