using AutoHub.Platform.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Localization;

internal sealed class I18nResourceConfiguration : IEntityTypeConfiguration<I18nResource>
{
    public void Configure(EntityTypeBuilder<I18nResource> builder)
    {
        builder.ToTable(
            "i18n_resources",
            Schemas.Platform,
            table =>
                // A blank string is worse than an untranslated one: an untranslated button still
                // says "Get my price" and still works, while a blank one is an unlabelled rectangle
                // that a screen reader announces as nothing at all. A string that should not appear
                // is deactivated, which makes it fall back rather than vanish.
                table.HasCheckConstraint("ck_i18n_resources_value_not_blank", "length(btrim(value)) > 0"));

        builder.HasKey(resource => resource.Id);

        // The bundle a screen downloads. Namespaces exist so the home page fetches the strings it
        // needs and not the four thousand belonging to the admin console — on a 3G connection in a
        // tier-3 city that is the difference between a page that loads and one that does not.
        builder.Property(resource => resource.Namespace).AsCaseInsensitive().HasMaxLength(48).IsRequired();

        // Dotted, structural, and never the English text. Keying by the sentence means fixing a typo
        // in English orphans every translation of it.
        builder.Property(resource => resource.Key).AsCaseInsensitive().HasMaxLength(160).IsRequired();

        builder.Property(resource => resource.Locale).AsCaseInsensitive().HasMaxLength(16).IsRequired();
        builder.Property(resource => resource.Value).AsText().IsRequired();

        // Context for the translator: "Button on the valuation card; keep under 18 characters."
        // Devanagari and Tamil set 20–30% wider than Latin at the same size, so the length budget
        // has to travel with the string or the button wraps.
        builder.Property(resource => resource.Description).HasMaxLength(512);

        builder.HasIndex(resource => new { resource.Namespace, resource.Key, resource.Locale }).IsUnique();

        // A bundle read is exactly this: one namespace, one locale, active rows only.
        builder.HasIndex(resource => new { resource.Locale, resource.Namespace })
            .HasFilter("is_active");
    }
}
