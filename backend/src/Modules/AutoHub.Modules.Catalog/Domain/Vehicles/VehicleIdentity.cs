using AutoHub.Modules.Catalog.Contracts;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Modules.Catalog.Domain.Vehicles;

/// <summary>
/// One identifier a vehicle held, and the period it held it for.
/// </summary>
/// <remarks>
/// <b>This row is the north star's hard requirement, and the reason a registration number is not a
/// column on <see cref="Vehicle"/>.</b> A plate is not a permanent name for a car:
/// <list type="bullet">
/// <item>Interstate transfer surrenders the old number and issues a new one — the same car, two
/// plates, with a gap in between while the NOC is processed.</item>
/// <item>A surrendered number returns to the RTO's pool and is <b>reissued</b>, often years later,
/// to a completely different vehicle.</item>
/// <item>A damaged plate is reissued in the same series, and a temporary registration becomes a
/// permanent one.</item>
/// </list>
/// Modelled as a column, each of those is either an update that destroys history — so the auction lot
/// from 2023 now displays a plate the car did not wear — or a duplicate vehicle. Modelled as a
/// period, all four are ordinary: close the old row, open a new one, and every historical document
/// still resolves the plate it was created with.
/// <para>
/// <b>Uniqueness is enforced only over the active period</b>, by a partial unique index on
/// <c>(identifier_type, value) WHERE valid_to IS NULL</c>. Two <i>live</i> vehicles claiming one live
/// plate is corruption and must be impossible; the same plate appearing on two vehicles at different
/// times is not corruption, it is what the RTO actually did, and a database that refuses to record it
/// forces the application to lie.
/// </para>
/// </remarks>
public sealed class VehicleIdentity : Entity
{
    private VehicleIdentity(
        Guid id,
        Guid vehicleId,
        VehicleIdentifierType identifierType,
        string value,
        DateTimeOffset validFrom,
        string? sourceCode)
        : base(id)
    {
        VehicleId = vehicleId;
        IdentifierType = identifierType;
        Value = value;
        ValidFrom = validFrom;
        SourceCode = sourceCode;
    }

    private VehicleIdentity() { }

    public Guid VehicleId { get; private set; }

    public VehicleIdentifierType IdentifierType { get; private set; }

    /// <summary>Canonical form produced by <see cref="VehicleIdentifier"/>. Never the raw input.</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// When this identifier started applying.
    /// </summary>
    /// <remarks>
    /// Defaults to the moment we learned of it rather than the moment the RTO issued it, because for
    /// most vehicles we never learn the second. Where an RC scan gives a real issue date the caller
    /// supplies it, and the row is then genuinely historical rather than merely observed.
    /// </remarks>
    public DateTimeOffset ValidFrom { get; private set; }

    /// <summary>
    /// When it stopped. <b>Null means active</b>, which is the value the unique index filters on.
    /// </summary>
    /// <remarks>
    /// A sentinel far-future date would have been the other option and is worse: it makes the partial
    /// index impossible, so uniqueness would have to be enforced over a range, and every query for
    /// "the current plate" would carry a comparison against a magic constant that one caller
    /// eventually gets wrong.
    /// </remarks>
    public DateTimeOffset? ValidTo { get; private set; }

    /// <summary>
    /// Where the value came from — <c>vahan</c>, <c>rc_ocr</c>, <c>seller</c>, <c>dealer_feed</c>.
    /// </summary>
    /// <remarks>
    /// A taxonomy option value rather than an enum, following <c>VehicleVariant.FuelCode</c>: the day
    /// a new data provider is onboarded, its provenance code is an insert. Provenance matters here
    /// because a seller-typed chassis number and a VAHAN-sourced one deserve different confidence
    /// when they disagree.
    /// </remarks>
    public string? SourceCode { get; private set; }

    /// <summary>Whether this identifier is in force. The question every read actually asks.</summary>
    public bool IsActive => ValidTo is null;

    internal static VehicleIdentity Open(
        Guid vehicleId,
        VehicleIdentifier identifier,
        DateTimeOffset validFrom,
        string? sourceCode)
        => new(UuidV7.New(), vehicleId, identifier.Type, identifier.Value, validFrom, sourceCode);

    /// <summary>
    /// Closes the period.
    /// </summary>
    /// <remarks>
    /// Clamped so that <c>valid_to</c> is never before <c>valid_from</c>. A clock skew of a few
    /// hundred milliseconds between two API nodes is enough to produce an inverted period, and the
    /// check constraint that guards the table would then reject a perfectly ordinary re-registration
    /// at the worst possible moment.
    /// </remarks>
    internal void Retire(DateTimeOffset at)
        => ValidTo = at < ValidFrom ? ValidFrom : at;

    public VehicleIdentifierView ToView() => new(IdentifierType, Value, ValidFrom, ValidTo);
}
