using AutoHub.Platform.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Localization;

internal sealed class I18nLocaleConfiguration : IEntityTypeConfiguration<I18nLocale>
{
    public void Configure(EntityTypeBuilder<I18nLocale> builder)
    {
        builder.ToTable(
            "i18n_locales",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint("ck_i18n_locales_code_shape", "code ~ '^[a-z]{2}(-[A-Z]{2})?$'");

                // A locale that falls back to itself is an infinite chain. The registry guards the
                // depth as a second belt; this catches the one-hop case at the source.
                table.HasCheckConstraint(
                    "ck_i18n_locales_fallback_is_not_self",
                    "fallback_code IS NULL OR fallback_code <> code");
            });

        builder.HasKey(locale => locale.Id);

        builder.Property(locale => locale.Code).AsCaseInsensitive().HasMaxLength(16).IsRequired();
        builder.Property(locale => locale.EnglishName).HasMaxLength(80).IsRequired();

        // The name in its own script — मराठी, தமிழ். A language picker that lists "Marathi" in Latin
        // is unusable by exactly the person who needs it.
        builder.Property(locale => locale.NativeName).HasMaxLength(80).IsRequired();

        // A code, not a foreign key. The fallback edge is authored per locale — Marathi falls back
        // to Hindi, Tamil to English — and a constraint would force locales to be created in
        // dependency order by an import that has no reason to know one.
        builder.Property(locale => locale.FallbackCode).AsNullableCaseInsensitive().HasMaxLength(16);

        builder.HasIndex(locale => locale.Code).IsUnique();
    }
}
