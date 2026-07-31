using AutoHub.Platform.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Notifications;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable(
            "notification_templates",
            Schemas.Platform,
            table =>
                // In India an SMS or WhatsApp body that does not match its DLT-registered template
                // is not rejected loudly — it is accepted, sometimes billed, and silently dropped in
                // the operator's network. The only symptom is a support queue full of "I never got
                // the code". An active template on those channels without its provider id is
                // therefore a compliance fault, not a data-quality one, and the table says so.
                table.HasCheckConstraint(
                    "ck_notification_templates_registered_before_active",
                    "NOT is_active OR channel NOT IN ('Sms', 'Whatsapp') OR provider_template_id IS NOT NULL"));

        builder.HasKey(template => template.Id);

        builder.Property(template => template.Key).AsCaseInsensitive().HasMaxLength(96).IsRequired();

        builder.Property(template => template.Channel)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        // BCP-47 with region: hi-IN is not hi. citext because a client's Accept-Language arrives in
        // whatever casing the platform felt like — `en-in`, `en-IN`, `EN-in` are one locale.
        builder.Property(template => template.Locale).AsCaseInsensitive().HasMaxLength(16).IsRequired();

        builder.Property(template => template.Subject).HasMaxLength(256);
        builder.Property(template => template.Body).AsText().IsRequired();
        builder.Property(template => template.ProviderTemplateId).HasMaxLength(128);
        builder.Property(template => template.SenderId).HasMaxLength(32);

        builder.HasMany<NotificationTemplateVariable>(nameof(NotificationTemplate.Variables))
            .WithOne()
            .HasForeignKey(variable => variable.NotificationTemplateId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(NotificationTemplate.Variables));

        // Keyed by the triple, because the same event says different things on different surfaces:
        // a sentence that reads well on WhatsApp has about forty characters before Android truncates
        // it in a push.
        builder.HasIndex(template => new { template.Key, template.Channel, template.Locale }).IsUnique();
    }
}
