using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Taxonomy;

internal sealed class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> builder)
    {
        builder.ToTable(
            "vehicle_models",
            Schemas.Platform,
            table => table.HasCheckConstraint(
                "ck_vehicle_models_production_range",
                "production_from IS NULL OR production_to IS NULL OR production_to >= production_from"));

        builder.HasKey(model => model.Id);

        builder.Property(model => model.Code)
            .AsCaseInsensitive()
            .IsRequired();

        builder.Property(model => model.Name).HasMaxLength(120).IsRequired();

        builder.HasOne<VehicleMake>()
            .WithMany()
            .HasForeignKey(model => model.MakeId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        builder.HasOne<VehicleCategory>()
            .WithMany()
            .HasForeignKey(model => model.CategoryId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        // Globally unique, matching the published contract: `?model=swift` looks a model up by code
        // alone. Scoping it to the make would make that route ambiguous the first time two
        // manufacturers ship the same nameplate slug, and the ambiguity would surface as a silently
        // wrong variant list rather than an error.
        builder.HasIndex(model => model.Code).IsUniqueAmongLiveRows();

        builder.HasIndex(model => new { model.MakeId, model.SortOrder });

        // "Which makes sell SUVs?" is answered by the models underneath the category subtree.
        builder.HasIndex(model => new { model.CategoryId, model.IsActive });

        builder.HasIndex(model => model.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
