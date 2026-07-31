using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Catalog.Contracts;

/// <summary>
/// The kinds of identifier a vehicle can be known by, in descending order of permanence.
/// </summary>
/// <remarks>
/// The order is load-bearing and is the reason this is an ordinal enum rather than a set of strings.
/// Deduplication resolves an incoming triple by trusting the <b>most permanent</b> identifier that
/// matches: a VIN is stamped into the chassis at the factory and never changes, while a registration
/// number is issued by an RTO, surrendered on interstate transfer, and reissued to a different
/// vehicle years later. When the two disagree, the plate is the thing that moved.
/// <para>
/// This lives in <c>Contracts</c> rather than <c>Domain</c> because other modules genuinely need it:
/// a listing search by plate, an auction lot's provenance panel and an inspection's RC capture all
/// name an identifier type, and none of them may reach into the catalog's domain to do so.
/// </para>
/// </remarks>
public enum VehicleIdentifierType
{
    /// <summary>The number plate. Not stable over time — see the type's remarks.</summary>
    Registration = 1,

    /// <summary>Engine number. Replaced with the engine; weaker than a chassis number.</summary>
    Engine = 2,

    /// <summary>
    /// Chassis number as printed on the RC. In India this is frequently the last six characters of
    /// the VIN rather than the whole of it, which is why it is a separate type and not a VIN.
    /// </summary>
    Chassis = 3,

    /// <summary>The 17-character ISO 3779 VIN. The strongest identifier a vehicle has.</summary>
    Vin = 4,
}

/// <summary>
/// The entity-type keys this module owns in the platform's shared EAV table.
/// </summary>
/// <remarks>
/// A technical key, in the prime directive's explicit exemption. Its value is that
/// <c>catalog.vehicle</c> appears in the taxonomy registry's attribute-value rows, in cache keys and
/// in search projections — and a typo in any of those silently returns an empty attribute list
/// rather than an error.
/// </remarks>
public static class CatalogEntityTypes
{
    public const string Vehicle = "catalog.vehicle";
}

/// <summary>One identifier a vehicle is or was known by, as other modules see it.</summary>
public sealed record VehicleIdentifierView(
    VehicleIdentifierType Type,
    string Value,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo)
{
    public bool IsActive => ValidTo is null;
}

/// <summary>
/// Everything another module is allowed to know about a canonical vehicle.
/// </summary>
/// <remarks>
/// Deliberately a flat projection and not the aggregate. A listing needs the plate to render and the
/// model ids to build a title; it must never hold a <c>Vehicle</c>, because holding it is how a
/// module ends up writing to it, and the moment two modules write a vehicle the deduplication that
/// this whole module exists for stops being enforceable.
/// </remarks>
public sealed record VehicleSummary
{
    public required Guid VehicleId { get; init; }

    public required Guid CategoryId { get; init; }

    public Guid? MakeId { get; init; }

    public Guid? ModelId { get; init; }

    public Guid? VariantId { get; init; }

    public int? ManufactureYear { get; init; }

    public DateOnly? FirstRegisteredOn { get; init; }

    /// <summary>The active registration, canonical form, or null for an unregistered vehicle.</summary>
    public string? Registration { get; init; }

    /// <summary>Plate-grouped for display: <c>MH 12 AB 1234</c>. Never re-derived by the caller.</summary>
    public string? RegistrationDisplay { get; init; }

    /// <summary>Taxonomy option value naming where this record came from — <c>vahan</c>, <c>seller</c>.</summary>
    public string? ProvenanceCode { get; init; }

    public required IReadOnlyList<VehicleIdentifierView> Identifiers { get; init; }
}

/// <summary>An identifier a caller supplies when it wants a vehicle resolved.</summary>
public sealed record VehicleIdentifierInput(VehicleIdentifierType Type, string Value);

/// <summary>What resolution did, so the caller can tell a match from a creation.</summary>
public enum VehicleResolution
{
    /// <summary>No identifier matched anything live. A new canonical vehicle was created.</summary>
    Created = 1,

    /// <summary>An existing vehicle matched and was returned unchanged.</summary>
    Matched = 2,

    /// <summary>
    /// An existing vehicle matched and gained identifiers, or had a superseded plate retired.
    /// </summary>
    Reconciled = 3,
}

/// <summary>
/// The read side of the catalog, for modules that must not reference its domain.
/// </summary>
/// <remarks>
/// This interface is the whole of the module isolation rule made concrete. Listings, Auction,
/// Valuation and Inspection all need a vehicle; none of them may join to <c>catalog.vehicles</c>,
/// because after this module is extracted to its own service there is no table on the other side of
/// that join.
/// </remarks>
public interface IVehicleDirectory
{
    Task<Result<VehicleSummary>> GetAsync(Guid vehicleId, CancellationToken cancellationToken);

    /// <summary>
    /// Summaries for a batch of vehicles, keyed by id.
    /// </summary>
    /// <remarks>
    /// Batched because the caller is a list page. Resolving twenty-four search results one vehicle
    /// at a time is twenty-four round trips to render one screen — and after extraction it is
    /// twenty-four HTTP calls, which is the same mistake with a worse constant.
    /// </remarks>
    Task<Result<IReadOnlyDictionary<Guid, VehicleSummary>>> GetManyAsync(
        IReadOnlyCollection<Guid> vehicleIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves an incoming identifier set to a canonical vehicle, creating one if nothing matches.
    /// </summary>
    /// <remarks>
    /// The single entry point that stops one car existing three times — once as a C2C listing, once
    /// in a dealer's stock feed and once as an auction lot. Every path that can introduce a vehicle
    /// goes through here, which is why it is on the contract and not only on the module's own bus.
    /// </remarks>
    Task<Result<VehicleSummary>> ResolveOrCreateAsync(
        Guid categoryId,
        IReadOnlyList<VehicleIdentifierInput> identifiers,
        CancellationToken cancellationToken);

    /// <summary>
    /// Looks a vehicle up by one identifier, without creating anything.
    /// </summary>
    /// <remarks>
    /// The read-only half of resolution, and the entry point the homepage hero needs: an anonymous
    /// visitor typing a plate to see a price must not create a canonical vehicle as a side effect,
    /// because most of them are browsing and the catalog would fill with cars nobody is selling.
    /// Creation happens when the visitor commits to listing.
    /// <para>
    /// Returns <c>catalog.vehicle.not_found</c> rather than a null, so "we have never seen this
    /// plate" reaches the caller as a branch it has to handle rather than a null it can forget.
    /// </para>
    /// </remarks>
    Task<Result<VehicleSummary>> FindByIdentifierAsync(
        VehicleIdentifierInput identifier,
        CancellationToken cancellationToken);
}
