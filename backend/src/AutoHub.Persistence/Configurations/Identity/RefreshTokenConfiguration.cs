using AutoHub.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Identity;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(
            "refresh_tokens",
            Schemas.Identity,
            table => table.HasCheckConstraint(
                "ck_refresh_tokens_window_ordered",
                "expires_at > issued_at"));

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(token => token.DeviceId).HasMaxLength(128);
        builder.Property(token => token.IssuedIp).HasMaxLength(64);
        builder.Property(token => token.RevokedReason).HasMaxLength(64);

        // The refresh path looks the token up by digest on every rotation, so this index is on the
        // hot path of every signed-in session. Unique because two rows sharing a digest would make
        // reuse detection ambiguous — and ambiguity here means either locking out a real user or
        // letting a replay through.
        builder.HasIndex(token => token.TokenHash).IsUnique();

        // Reuse detection revokes a whole lineage in one statement. Without this index that is a
        // sequential scan of every token ever issued, executed at the exact moment we believe an
        // account is being attacked.
        builder.HasIndex(token => token.FamilyId);

        // "Sign out my other devices", and the sweep for expired rows.
        builder.HasIndex(token => new { token.UserId, token.ExpiresAt });

        // No foreign key to users, deliberately. Identity is the module most likely to be extracted
        // to its own service first, and a cross-schema constraint is exactly the thing that would
        // have to be dropped to do it. Holding the id and enforcing the relationship in the module
        // is the pattern the whole codebase already uses for cross-module references.
    }
}
