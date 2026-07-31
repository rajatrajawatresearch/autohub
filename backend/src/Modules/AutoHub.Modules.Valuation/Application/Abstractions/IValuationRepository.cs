using AutoHub.Modules.Valuation.Domain.Models;

namespace AutoHub.Modules.Valuation.Application.Abstractions;

/// <summary>
/// Reads and writes valuations.
/// </summary>
/// <remarks>
/// There is no <c>Update</c>, and its absence is the point: a valuation is immutable once written,
/// so the only write is an insert. An interface that offered an update would be an invitation to
/// re-run a quote in place and destroy the reproducibility the whole aggregate exists for.
/// </remarks>
public interface IValuationRepository
{
    Task<Domain.Valuations.Valuation?> GetAsync(Guid valuationId, CancellationToken cancellationToken);

    /// <summary>
    /// The most recent valuation for a vehicle that has not expired, served or shadow excluded.
    /// </summary>
    /// <remarks>
    /// Shadow runs are excluded here rather than filtered by the caller. A shadow quote returned to a
    /// listing's price-band guard would be a model under evaluation silently deciding what a seller
    /// may ask, which is precisely what shadow mode exists to avoid.
    /// </remarks>
    Task<Domain.Valuations.Valuation?> GetLatestForVehicleAsync(
        Guid vehicleId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);

    Task AddAsync(Domain.Valuations.Valuation valuation, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Loads published valuation model definitions.
/// </summary>
/// <remarks>
/// Declared here and implemented in the persistence layer, like every other registry store, so the
/// hedonic model — which is the only genuinely intricate arithmetic in this module — is testable
/// against a hand-built definition rather than a seeded database.
/// <para>
/// Implementations return every published definition rather than the one that matches, because
/// matching runs a JsonLogic condition against the inputs and that is the engine's job, not SQL's.
/// The set is small — a handful of rows — and is cached through the registry cache.
/// </para>
/// </remarks>
public interface IValuationModelStore
{
    Task<IReadOnlyList<ValuationModelDefinition>> GetPublishedAsync(CancellationToken cancellationToken);
}
