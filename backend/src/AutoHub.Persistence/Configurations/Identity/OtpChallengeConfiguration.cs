using AutoHub.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Identity;

internal sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable(
            "otp_challenges",
            Schemas.Identity,
            table => table.HasCheckConstraint(
                "ck_otp_challenges_attempts_bounded",
                "attempts >= 0 AND attempts <= max_attempts"));

        builder.HasKey(challenge => challenge.Id);

        builder.Property(challenge => challenge.Phone).AsCaseInsensitive().HasMaxLength(20).IsRequired();
        builder.Property(challenge => challenge.Purpose).HasMaxLength(32).IsRequired();
        builder.Property(challenge => challenge.Channel).HasMaxLength(16).IsRequired();
        builder.Property(challenge => challenge.CodeHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(challenge => challenge.Salt).HasMaxLength(32).IsFixedLength().IsRequired();

        // The throttle query: "how many codes has this number been sent since ...". Ordered by time
        // so the count is an index-only range scan rather than a scan of every challenge ever issued.
        builder.HasIndex(challenge => new { challenge.Phone, challenge.CreatedAt });

        // The sweeper's query. Challenges are dense and short-lived; without this the cleanup job
        // reads the whole table every few minutes forever.
        builder.HasIndex(challenge => challenge.ExpiresAt);
    }
}
