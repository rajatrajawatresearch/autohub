using AutoHub.Platform.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Geography;

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable(
            "cities",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_cities_coordinates_in_range",
                "latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180"));

        builder.HasKey(city => city.Id);

        builder.Property(city => city.Code)
            .AsCaseInsensitive()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(city => city.Name).HasMaxLength(120).IsRequired();

        builder.Property(city => city.Latitude).HasColumnType("double precision").IsRequired();
        builder.Property(city => city.Longitude).HasColumnType("double precision").IsRequired();

        // The module list is a native text[] rather than a join table or a column per module.
        // A column per module is a migration every time a module launches — precisely the coupling
        // the registries exist to remove — and a join table is three rows and a join for a list that
        // is read whole, every time, and never queried by its elements alone.
        //
        // Mapped through the backing field: the entity exposes IReadOnlyList so nothing outside it
        // can add a module without going through OpenFor, which is what keeps IsServiceable honest.
        builder.Ignore(city => city.EnabledModules);

        builder.Property<List<string>>("_enabledModules")
            .HasColumnName("enabled_modules")
            .HasColumnType(ColumnTypes.TextArray)
            .IsRequired();

        builder.HasOne<State>()
            .WithMany()
            .HasForeignKey(city => city.StateId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        builder.HasIndex(city => city.Code).IsUniqueAmongLiveRows();

        // `GET /platform/geo/cities?serviceable=true` is the first call every client makes.
        builder.HasIndex(city => new { city.IsServiceable, city.SortOrder })
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(city => city.StateId);

        // City pickers are typed into, not scrolled — there are several hundred rows here and a
        // user knows the first four letters of the one they want.
        builder.HasIndex(city => city.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
