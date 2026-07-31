using AutoHub.Platform.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Geography;

internal sealed class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable(
            "states",
            Schemas.Platform,
            table =>
            {
                table.HasCheckConstraint("ck_states_code_shape", "code ~ '^[A-Z]{2}$'");

                // The first two characters of every GSTIN issued in the state. Getting it wrong
                // means charging IGST where CGST plus SGST was due, which is a filing correction
                // rather than a bug report.
                table.HasCheckConstraint(
                    "ck_states_gst_code_shape",
                    "gst_state_code IS NULL OR gst_state_code ~ '^[0-9]{2}$'");
            });

        builder.HasKey(state => state.Id);

        // citext because the code arrives from a plate the user typed — "mh12ab1234" as often as
        // "MH12AB1234" — and the snapshot looks it up case-insensitively. The column agreeing with
        // the lookup is what stops a bulk import creating a second Maharashtra.
        builder.Property(state => state.Code)
            .AsCaseInsensitive()
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(state => state.Name).HasMaxLength(120).IsRequired();
        builder.Property(state => state.GstStateCode).HasMaxLength(2).IsFixedLength();

        builder.HasOne<Country>()
            .WithMany()
            .HasForeignKey(state => state.CountryId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        builder.HasIndex(state => state.Code).IsUniqueAmongLiveRows();
        builder.HasIndex(state => new { state.CountryId, state.SortOrder });
    }
}
