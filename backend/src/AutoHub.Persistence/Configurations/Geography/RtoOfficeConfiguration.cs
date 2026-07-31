using AutoHub.Platform.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Geography;

internal sealed class RtoOfficeConfiguration : IEntityTypeConfiguration<RtoOffice>
{
    public void Configure(EntityTypeBuilder<RtoOffice> builder)
    {
        builder.ToTable(
            "rto_offices",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_rto_offices_code_shape",
                "code ~ '^[A-Z]{2}[0-9]{1,2}$'"));

        builder.HasKey(office => office.Id);

        // citext, and for the same reason as the state code: this value is reconstructed from a
        // plate the user typed. `RegistrationNumber` canonicalises to uppercase, but the row this
        // joins to may have been loaded by a script that did not.
        builder.Property(office => office.Code)
            .AsCaseInsensitive()
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(office => office.Name).HasMaxLength(160).IsRequired();
        builder.Property(office => office.Address).HasMaxLength(512);

        builder.HasOne<State>()
            .WithMany()
            .HasForeignKey(office => office.StateId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        // Optional: an RTO's jurisdiction is a district, and several districts have no city in our
        // list. Requiring one would either invent cities or drop offices.
        builder.HasOne<City>()
            .WithMany()
            .HasForeignKey(office => office.CityId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        builder.HasIndex(office => office.Code).IsUniqueAmongLiveRows();
        builder.HasIndex(office => office.StateId);
    }
}
