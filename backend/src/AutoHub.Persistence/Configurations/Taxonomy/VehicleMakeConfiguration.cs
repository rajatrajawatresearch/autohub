using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Taxonomy;

internal sealed class VehicleMakeConfiguration : IEntityTypeConfiguration<VehicleMake>
{
    public void Configure(EntityTypeBuilder<VehicleMake> builder)
    {
        builder.ToTable(
            "vehicle_makes",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_vehicle_makes_country_alpha2",
                "country_of_origin IS NULL OR country_of_origin ~ '^[A-Z]{2}$'"));

        builder.HasKey(make => make.Id);

        builder.Property(make => make.Code)
            .AsCaseInsensitive()
            .IsRequired();

        builder.Property(make => make.Name).HasMaxLength(120).IsRequired();
        builder.Property(make => make.CountryOfOrigin).HasMaxLength(2).IsFixedLength();
        builder.Property(make => make.LogoKey).HasMaxLength(256);

        builder.HasIndex(make => make.Code).IsUniqueAmongLiveRows();

        // Make pickers are typed into, not scrolled. Trigram indexing is what makes "maru" find
        // Maruti Suzuki without a leading-wildcard scan of the whole table.
        builder.HasIndex(make => make.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
