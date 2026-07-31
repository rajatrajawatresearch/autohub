using AutoHub.Platform.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Geography;

internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable(
            "countries",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_countries_dial_code_e164",
                "dial_code ~ '^\\+[0-9]{1,4}$'"));

        builder.HasKey(country => country.Id);

        builder.Property(country => country.Code)
            .AsCaseInsensitive()
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(country => country.Name).HasMaxLength(120).IsRequired();
        builder.Property(country => country.DialCode).HasMaxLength(8).IsRequired();
        builder.Property(country => country.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();

        builder.HasIndex(country => country.Code).IsUniqueAmongLiveRows();
    }
}
