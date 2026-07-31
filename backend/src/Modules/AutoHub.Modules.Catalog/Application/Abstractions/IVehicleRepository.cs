using AutoHub.Modules.Catalog.Domain.Vehicles;

namespace AutoHub.Modules.Catalog.Application.Abstractions;

/// <summary>
/// Reads and writes vehicles.
/// </summary>
/// <remarks>
/// Declared here and implemented in the persistence layer, for the same reason
/// <c>IWorkflowStore</c> and <c>ITaxonomyStore</c> are: the module never references EF Core, so the
/// deduplication algorithm — which is the only genuinely subtle code in this module — is testable
/// against a list rather than a database.
/// <para>
/// It is also what makes extraction a build-file change. When this module becomes a service, the
/// implementation moves with its schema and nothing in <c>Application</c> or <c>Domain</c> is
/// touched.
/// </para>
/// </remarks>
public interface IVehicleRepository
{
    Task<Vehicle?> GetAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Vehicle>> GetManyAsync(
        IReadOnlyCollection<Guid> vehicleIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every vehicle holding any of these identifiers <b>right now</b>.
    /// </summary>
    /// <remarks>
    /// One query for the whole set rather than one per identifier: a submission carries up to four,
    /// and the interesting answer — "these two identifiers point at different vehicles" — is only
    /// visible when they are resolved together.
    /// <para>
    /// Implementations must consider only identities whose period is open. A retired plate matching
    /// is exactly the reissue case, and treating it as a match would resolve a brand new car onto the
    /// previous owner's vehicle, its listings and its history.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<Vehicle>> FindByActiveIdentifiersAsync(
        IReadOnlyCollection<VehicleIdentifier> identifiers,
        CancellationToken cancellationToken);

    /// <summary>Cursor-paginated search over the canonical fleet. Never unbounded.</summary>
    Task<VehiclePage> SearchAsync(VehicleSearchCriteria criteria, CancellationToken cancellationToken);

    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// What a catalog search may filter on.
/// </summary>
/// <remarks>
/// Deliberately narrow. This is the <b>operational</b> search — an admin looking for a plate, a
/// support agent finding a duplicate, a dealer reconciling their feed. Buyer-facing faceted search
/// runs over listings and through the search index, because a vehicle nobody is selling is not a
/// result anybody wants.
/// </remarks>
public sealed record VehicleSearchCriteria
{
    /// <summary>Free text matched against identifiers. Canonicalised before it reaches the store.</summary>
    public string? Query { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? MakeId { get; init; }

    public Guid? ModelId { get; init; }

    public int? ManufactureYearFrom { get; init; }

    public int? ManufactureYearTo { get; init; }

    /// <summary>
    /// Attribute equality filters, keyed by attribute code, matched against the denormalised bag.
    /// </summary>
    /// <remarks>
    /// Containment over the JSONB projection rather than a join per attribute: an admin filtering on
    /// three attributes would otherwise produce a three-way self-join over the EAV table for a query
    /// that runs a few hundred times a day.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Opaque, id-based. Offsets drift when rows are inserted; a UUID v7 cursor does not.</summary>
    public Guid? Cursor { get; init; }

    public int Limit { get; init; } = 24;
}

/// <summary>One page of vehicles plus the cursor that continues it.</summary>
public sealed record VehiclePage(IReadOnlyList<Vehicle> Items, Guid? NextCursor, long TotalEstimate);
