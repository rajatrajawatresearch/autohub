using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module = AutoHub.Platform.Features.Module;

namespace AutoHub.Persistence.Configurations.Features;

internal sealed class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable(
            "modules",
            Schemas.Platform,
            table => table.HasCheckConstraint("ck_modules_code_shape", "code ~ '^[a-z][a-z0-9_]*$'"));

        builder.HasKey(module => module.Id);

        builder.Property(module => module.Code).AsCaseInsensitive().HasMaxLength(64).IsRequired();
        builder.Property(module => module.Name).HasMaxLength(120).IsRequired();
        builder.Property(module => module.Description).HasMaxLength(512);

        // A key, not a foreign key. The gating flag may not exist yet — a module is often created
        // before the flag that will stage its rollout — and a constraint here would force the two
        // to be authored in an order the admin UI does not impose. An unknown key evaluates false,
        // which is the safe direction for a launch gate.
        builder.Property(module => module.GatingFlagKey).AsNullableCaseInsensitive().HasMaxLength(64);

        builder.HasIndex(module => module.Code).IsUnique();
    }
}
