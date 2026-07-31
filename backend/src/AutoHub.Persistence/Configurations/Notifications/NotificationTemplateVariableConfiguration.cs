using AutoHub.Platform.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Notifications;

internal sealed class NotificationTemplateVariableConfiguration
    : IEntityTypeConfiguration<NotificationTemplateVariable>
{
    public void Configure(EntityTypeBuilder<NotificationTemplateVariable> builder)
    {
        builder.ToTable(
            "notification_template_variables",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_notification_template_variables_name_shape",
                "name ~ '^[a-zA-Z][a-zA-Z0-9_]*$'"));

        builder.HasKey(variable => variable.Id);

        builder.Property(variable => variable.Name).HasMaxLength(64).IsRequired();

        // What the editor renders in its preview and what a DLT submission uses as the sample.
        // "Your bid on {vehicle}" tells an author nothing about whether the message fits in one SMS
        // segment; "Your bid on 2021 Maruti Swift VXi" does.
        builder.Property(variable => variable.SampleValue).HasMaxLength(256);
        builder.Property(variable => variable.Description).HasMaxLength(512);

        // Declared once per template. A duplicate declaration would let one variable be required and
        // optional at the same time, and which one wins would be row order.
        builder.HasIndex(variable => new { variable.NotificationTemplateId, variable.Name }).IsUnique();
    }
}
