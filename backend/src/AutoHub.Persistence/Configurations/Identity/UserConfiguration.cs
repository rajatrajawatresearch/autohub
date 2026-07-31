using AutoHub.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Identity;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", Schemas.Identity);

        builder.HasKey(user => user.Id);

        // citext, because a phone number is a natural key humans and CSV imports both produce. The
        // value object canonicalises before it ever reaches here; the column type is the second belt.
        builder.Property(user => user.Phone).AsCaseInsensitive().HasMaxLength(20).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(user => user.Email).AsNullableCaseInsensitive().HasMaxLength(254);
        builder.Property(user => user.ActiveCityCode).HasMaxLength(64);

        // Unique among live rows only. A deleted account must not permanently burn its phone number:
        // people change numbers, numbers are recycled by operators, and a user who deletes and comes
        // back should be able to sign up rather than hit a constraint nobody can explain.
        builder.HasIndex(user => user.Phone).IsUniqueAmongLiveRows();

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL AND deleted_at IS NULL");
    }
}
