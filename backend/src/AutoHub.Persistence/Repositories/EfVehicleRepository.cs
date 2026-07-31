using AutoHub.Modules.Catalog.Application.Abstractions;
using AutoHub.Modules.Catalog.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Repositories;

/// <summary>
/// The EF implementation of the catalog's repository.
/// </summary>
/// <remarks>
/// Tracked, unlike the registry stores: these aggregates are loaded to be changed, and the change
/// tracker is what turns "close the old plate period and open a new one" into two statements in one
/// transaction without the handler writing SQL.
/// </remarks>
public sealed class EfVehicleRepository : IVehicleRepository
{
    private readonly AutoHubDbContext _context;

    public EfVehicleRepository(AutoHubDbContext context) => _context = context;

    public async Task<Vehicle?> GetAsync(Guid vehicleId, CancellationToken cancellationToken)
        => await _context.Vehicles
            .Include(vehicle => vehicle.Identities)
            .FirstOrDefaultAsync(vehicle => vehicle.Id == vehicleId, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> GetManyAsync(
        IReadOnlyCollection<Guid> vehicleIds,
        CancellationToken cancellationToken)
    {
        if (vehicleIds.Count == 0)
        {
            return [];
        }

        // ToArray so Npgsql sends one array parameter instead of expanding the ids into an IN list,
        // which would produce a distinct query plan per batch size and churn the plan cache.
        var ids = vehicleIds.ToArray();

        return await _context.Vehicles
            .Include(vehicle => vehicle.Identities)
            .Where(vehicle => ids.Contains(vehicle.Id))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Every vehicle holding any of these identifiers right now.
    /// </summary>
    /// <remarks>
    /// The <c>ValidTo == null</c> predicate is the one that matters, and it is the same condition the
    /// partial unique index filters on — so this query uses that index rather than scanning history.
    /// Matching a retired identifier would resolve a brand new car onto the previous holder of a
    /// reissued plate, along with its listings, its inspections and its deals.
    /// </remarks>
    public async Task<IReadOnlyList<Vehicle>> FindByActiveIdentifiersAsync(
        IReadOnlyCollection<VehicleIdentifier> identifiers,
        CancellationToken cancellationToken)
    {
        if (identifiers.Count == 0)
        {
            return [];
        }

        var types = identifiers.Select(i => i.Type).Distinct().ToArray();
        var values = identifiers.Select(i => i.Value).Distinct().ToArray();

        // Matched on the cross-product of types and values, then narrowed in memory. One indexed
        // query beats one round trip per identifier, and the false positives it can admit — a VIN
        // whose text happens to equal a chassis number — are removed by the exact-pair filter below.
        var candidateIds = await _context.VehicleIdentities
            .Where(identity =>
                identity.ValidTo == null
                && types.Contains(identity.IdentifierType)
                && values.Contains(identity.Value))
            .Select(identity => identity.VehicleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
        {
            return [];
        }

        var candidates = await _context.Vehicles
            .Include(vehicle => vehicle.Identities)
            .Where(vehicle => candidateIds.Contains(vehicle.Id))
            .ToListAsync(cancellationToken);

        return [.. candidates.Where(vehicle => identifiers.Any(vehicle.Holds))];
    }

    public async Task<VehiclePage> SearchAsync(
        VehicleSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var query = _context.Vehicles
            .Include(vehicle => vehicle.Identities)
            .AsQueryable();

        if (criteria.CategoryId is { } categoryId)
        {
            query = query.Where(vehicle => vehicle.CategoryId == categoryId);
        }

        if (criteria.MakeId is { } makeId)
        {
            query = query.Where(vehicle => vehicle.MakeId == makeId);
        }

        if (criteria.ModelId is { } modelId)
        {
            query = query.Where(vehicle => vehicle.ModelId == modelId);
        }

        if (criteria.ManufactureYearFrom is { } from)
        {
            query = query.Where(vehicle => vehicle.ManufactureYear >= from);
        }

        if (criteria.ManufactureYearTo is { } to)
        {
            query = query.Where(vehicle => vehicle.ManufactureYear <= to);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            // Prefix rather than substring: the column is citext with a B-tree index, and a leading
            // wildcard cannot use it. A plate search is a prefix search in practice — people type
            // the state and RTO first.
            var term = criteria.Query;

            query = query.Where(vehicle => _context.VehicleIdentities
                .Any(identity =>
                    identity.VehicleId == vehicle.Id
                    && identity.ValidTo == null
                    && identity.Value.StartsWith(term)));
        }

        foreach (var (code, value) in criteria.Attributes)
        {
            // Containment against the denormalised bag. Written as a JSON fragment so PostgreSQL can
            // use the GIN index; a per-attribute join would be one self-join per filter.
            var fragment = System.Text.Json.JsonSerializer.Serialize(
                new Dictionary<string, string> { [code] = value });

            query = query.Where(vehicle =>
                vehicle.AttributesJson != null
                && EF.Functions.JsonContains(vehicle.AttributesJson, fragment));
        }

        if (criteria.Cursor is { } cursor)
        {
            query = query.Where(vehicle => vehicle.Id.CompareTo(cursor) > 0);
        }

        // One extra row rather than a second COUNT query: its presence is what tells us there is a
        // next page, and it costs one row instead of a full scan of the filtered set.
        var page = await query
            .OrderBy(vehicle => vehicle.Id)
            .Take(criteria.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > criteria.Limit;
        var items = hasMore ? page[..criteria.Limit] : page;

        // An estimate, as §0 of the contract requires. An exact COUNT(*) over a filtered
        // multi-million-row table is a sequential scan on the hottest query in the product, and
        // nobody has ever needed to know there are 1,284 rather than 1,300.
        var estimate = hasMore ? items.Count + 1L : items.Count;

        return new VehiclePage(items, hasMore ? items[^1].Id : null, estimate);
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        => await _context.Vehicles.AddAsync(vehicle, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}
