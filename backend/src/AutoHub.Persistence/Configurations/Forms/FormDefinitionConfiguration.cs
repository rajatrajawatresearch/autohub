using AutoHub.Platform.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Forms;

internal sealed class FormDefinitionConfiguration : IEntityTypeConfiguration<FormDefinition>
{
    public void Configure(EntityTypeBuilder<FormDefinition> builder)
    {
        builder.ToTable("form_definitions", Schemas.Platform);

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Key).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(definition => definition.Name).HasMaxLength(160).IsRequired();
        builder.Property(definition => definition.Description).HasMaxLength(1024);
        builder.Property(definition => definition.EntityType).HasMaxLength(64).IsRequired();

        builder.HasMany<FormVersion>(nameof(FormDefinition.Versions))
            .WithOne()
            .HasForeignKey(version => version.FormDefinitionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(FormDefinition.Versions));

        builder.HasIndex(definition => definition.Key).IsUniqueAmongLiveRows();
    }
}
