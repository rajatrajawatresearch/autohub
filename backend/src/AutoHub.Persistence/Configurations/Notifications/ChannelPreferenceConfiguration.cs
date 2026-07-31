using AutoHub.Platform.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Notifications;

internal sealed class ChannelPreferenceConfiguration : IEntityTypeConfiguration<ChannelPreference>
{
    public void Configure(EntityTypeBuilder<ChannelPreference> builder)
    {
        builder.ToTable("notification_channel_preferences", Schemas.Platform);

        builder.HasKey(preference => preference.Id);

        builder.Property(preference => preference.Channel)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        // One opinion per channel. Two rows for `push` would make `Allows` return whichever the
        // planner produced first, and a user who turned push off would sometimes still be woken up.
        builder.HasIndex(preference => new { preference.NotificationSubscriptionId, preference.Channel })
            .IsUnique();
    }
}
