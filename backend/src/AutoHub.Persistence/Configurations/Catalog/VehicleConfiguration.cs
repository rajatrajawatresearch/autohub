using AutoHub.Modules.Catalog.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Catalog;

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable(
            "vehicles",
            Schemas.Catalog,
            table => table.HasCheckConstraint(
                "ck_vehicles_manufacture_month",
                "manufacture_month IS NULL OR (manufacture_month BETWEEN 1 AND 12)"));

        builder.HasKey(vehicle => vehicle.Id);

        builder.Property(vehicle => vehicle.ProvenanceCode).HasMaxLength(32);
        builder.Property(vehicle => vehicle.AttributesJson).AsNullableJsonb();

        builder.HasMany(vehicle => vehicle.Identities)
            .WithOne()
            .HasForeignKey(identity => identity.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(Vehicle.Identities));

        // No foreign key to platform.vehicle_categories, and that is deliberate rather than an
        // oversight. A cross-schema constraint is the thing that has to be dropped before this
        // module can be extracted to its own service, so it is not created in the first place — the
        // id is kept and the reference is resolved through the taxonomy registry.
        builder.HasIndex(vehicle => vehicle.CategoryId);
        builder.HasIndex(vehicle => new { vehicle.ModelId, vehicle.ManufactureYear });

        // Containment over the denormalised bag: `attributes @> '{"fuel_type":"cng"}'`. This is what
        // GIN is good at, and it is why the projection exists alongside the EAV rows that answer
        // range queries.
        builder.HasIndex(vehicle => vehicle.AttributesJson)
            .HasMethod("gin")
            .HasFilter("attributes_json IS NOT NULL");
    }
}

/// <summary>
/// The table whose one index is the reason this module exists.
/// </summary>
/// <remarks>
/// <b>Uniqueness over the active period, and nothing else.</b>
/// <code>
/// CREATE UNIQUE INDEX ux_vehicle_identities_active
///     ON catalog.vehicle_identities (identifier_type, value)
///     WHERE valid_to IS NULL;
/// </code>
/// The filter is the whole point. Three alternatives were considered and each fails a real case:
/// <list type="bullet">
/// <item><b>A plain unique index on <c>(identifier_type, value)</c>.</b> It makes re-registration
/// impossible: a car moving from Maharashtra to Karnataka must hold MH12AB1234 historically and
/// KA01CD5678 now, and the old row would block any future vehicle that is ever issued the released
/// number. Since the RTO genuinely reissues surrendered numbers, this index would eventually refuse
/// a perfectly legitimate new car — with a constraint violation nobody could explain.</item>
/// <item><b>No uniqueness at all, enforced in the application.</b> Two API nodes handling two
/// concurrent sell-flow submissions for the same plate both read "no match" and both insert. The
/// duplicate is created in a race that no amount of application-level checking closes, and it
/// surfaces months later as one car with two histories and two valuations.</item>
/// <item><b>An <c>EXCLUDE</c> constraint over a <c>tstzrange</c>, forbidding any overlap.</b> This is
/// the textbook answer for temporal uniqueness and it is wrong here for two reasons. It needs
/// <c>btree_gist</c>, which is not among the extensions this database provisions — but more
/// importantly it would forbid recording history that actually happened: RTO records overlap,
/// surrender dates are approximate, and a partner feed that says a plate was live until the 3rd
/// while our own record says the 1st is a discrepancy to keep, not a write to refuse. Refusing it
/// would push the application into fabricating clean dates.</item>
/// </list>
/// So the constraint states exactly the invariant that must never be violated — <b>two live vehicles
/// cannot claim one live identifier</b> — and stays silent about history, which is observation rather
/// than invariant. The deduplication handler relies on this: it may open a new period only after
/// closing the old one, in the same transaction, because the index refuses any other order.
/// </remarks>
internal sealed class VehicleIdentityConfiguration : IEntityTypeConfiguration<VehicleIdentity>
{
    public void Configure(EntityTypeBuilder<VehicleIdentity> builder)
    {
        builder.ToTable(
            "vehicle_identities",
            Schemas.Catalog,
            table => table.HasCheckConstraint(
                // A period that ends before it starts is unreadable by every query that filters on
                // it, and clock skew between two API nodes is enough to produce one.
                "ck_vehicle_identities_period",
                "valid_to IS NULL OR valid_to >= valid_from"));

        builder.HasKey(identity => identity.Id);

        // citext rather than text, for the reason RegistrationNumberConverter argues: rows written
        // around the domain — a bulk COPY, a repair script, a partner feed — would otherwise create
        // `mh12ab1234` beside `MH12AB1234` and defeat the unique index below without violating it.
        builder.Property(identity => identity.Value)
            .AsCaseInsensitive()
            .IsRequired();

        builder.Property(identity => identity.SourceCode).HasMaxLength(32);

        builder.HasIndex(identity => new { identity.IdentifierType, identity.Value })
            .IsUnique()
            .HasFilter("valid_to IS NULL")
            .HasDatabaseName("ux_vehicle_identities_active");

        // The history lookup: "which vehicle wore this plate in 2022?" — a question the auction and
        // trust modules both ask, and the reason the retired rows are kept at all.
        builder.HasIndex(identity => new { identity.IdentifierType, identity.Value, identity.ValidFrom });

        builder.HasIndex(identity => new { identity.VehicleId, identity.IdentifierType });
    }
}
