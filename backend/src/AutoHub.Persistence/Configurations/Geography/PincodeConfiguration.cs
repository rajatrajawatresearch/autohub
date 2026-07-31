using AutoHub.Platform.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Geography;

internal sealed class PincodeConfiguration : IEntityTypeConfiguration<Pincode>
{
    public void Configure(EntityTypeBuilder<Pincode> builder)
    {
        builder.ToTable(
            "pincodes",
            Schemas.Platform,
            table =>
            {
                // India Post never issues a pincode beginning with zero — the first digit is the
                // postal region. A leading zero here is a spreadsheet that dropped it and a lookup
                // that will never match.
                table.HasCheckConstraint("ck_pincodes_shape", "code ~ '^[1-9][0-9]{5}$'");

                table.HasCheckConstraint(
                    "ck_pincodes_coordinates_in_range",
                    "(latitude IS NULL OR latitude BETWEEN -90 AND 90) AND "
                    + "(longitude IS NULL OR longitude BETWEEN -180 AND 180)");

                // Half a coordinate is not a location. Either both are known or neither is.
                table.HasCheckConstraint(
                    "ck_pincodes_coordinates_paired",
                    "(latitude IS NULL) = (longitude IS NULL)");
            });

        builder.HasKey(pincode => pincode.Id);

        builder.Property(pincode => pincode.Code).HasMaxLength(6).IsFixedLength().IsRequired();
        builder.Property(pincode => pincode.District).HasMaxLength(120);

        builder.Property(pincode => pincode.Latitude).HasColumnType("double precision");
        builder.Property(pincode => pincode.Longitude).HasColumnType("double precision");

        builder.HasOne<State>()
            .WithMany()
            .HasForeignKey(pincode => pincode.StateId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        // Optional, and that is a real state rather than missing data: plenty of pincodes belong to
        // districts nobody models as a city, and a required link would mean inventing cities. The
        // link is never nulled on the client either — see WhenPrincipalIsSoftDeleted.
        builder.HasOne<City>()
            .WithMany()
            .HasForeignKey(pincode => pincode.CityId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        builder.HasIndex(pincode => pincode.Code).IsUniqueAmongLiveRows();
        builder.HasIndex(pincode => pincode.CityId).HasFilter("city_id IS NOT NULL");
    }
}
