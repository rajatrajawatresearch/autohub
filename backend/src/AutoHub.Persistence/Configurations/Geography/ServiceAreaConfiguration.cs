using AutoHub.Platform.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Geography;

internal sealed class ServiceAreaConfiguration : IEntityTypeConfiguration<ServiceArea>
{
    public void Configure(EntityTypeBuilder<ServiceArea> builder)
    {
        builder.ToTable(
            "service_areas",
            Schemas.Platform,
            table =>
            {
                // The two shapes have disjoint required fields, and a half-filled row is worse than
                // a missing one: `Covers` returns false for it, so a city looks unserviceable and
                // nobody can tell whether that is the policy or the data.
                table.HasCheckConstraint(
                    "ck_service_areas_shape_complete",
                    "(kind = 'Radius' AND centre_latitude IS NOT NULL AND centre_longitude IS NOT NULL "
                    + "AND radius_metres IS NOT NULL AND radius_metres > 0) OR "
                    + "(kind = 'Polygon' AND boundary_geo_json IS NOT NULL)");

                table.HasCheckConstraint(
                    "ck_service_areas_centre_in_range",
                    "(centre_latitude IS NULL OR centre_latitude BETWEEN -90 AND 90) AND "
                    + "(centre_longitude IS NULL OR centre_longitude BETWEEN -180 AND 180)");
            });

        builder.HasKey(area => area.Id);

        builder.Property(area => area.Code)
            .AsCaseInsensitive()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(area => area.Name).HasMaxLength(160).IsRequired();

        // A module code, not an enum — the module list is registry #6's data, not this assembly's.
        builder.Property(area => area.ModuleCode).AsCaseInsensitive().HasMaxLength(64).IsRequired();

        builder.Property(area => area.Kind)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(area => area.CentreLatitude).HasColumnType("double precision");
        builder.Property(area => area.CentreLongitude).HasColumnType("double precision");

        // GeoJSON rather than a PostGIS geometry: the Platform assembly stays free of a spatial
        // dependency and the value round-trips to the admin map component unchanged. jsonb rather
        // than text so a boundary can be inspected and validated in SQL without re-parsing it.
        builder.Property(area => area.BoundaryGeoJson).AsNullableJsonb();

        builder.HasOne<City>()
            .WithMany()
            .HasForeignKey(area => area.CityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(area => new { area.CityId, area.Code }).IsUniqueAmongLiveRows();

        // "Can we come to this address?" is answered by loading one city's areas for one module,
        // highest priority first, and testing containment in memory.
        builder.HasIndex(area => new { area.CityId, area.ModuleCode, area.Priority })
            .HasFilter("deleted_at IS NULL");
    }
}
