using AutoHub.Platform.Geography;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Loads the geography registry.
/// </summary>
/// <remarks>
/// The whole registry, in one go, because the whole registry is small: one country, thirty-six
/// states and union territories, about fourteen hundred RTO offices, some nineteen thousand
/// pincodes, and whatever cities and service areas operations has drawn. A few megabytes of objects
/// for data that changes when a new RTO opens — a few times a year.
/// <para>
/// Untracked for the same reason as the taxonomy store: the snapshot outlives the request that
/// loaded it and is shared by every thread on the node.
/// </para>
/// <para>
/// Inactive rows are included. A vehicle registered at an RTO that has since been merged away still
/// carries that code on its plate for the rest of its life, and a listing that cannot say where the
/// car is from is a listing buyers distrust.
/// </para>
/// </remarks>
public sealed class EfGeographyStore : IGeographyStore
{
    private readonly AutoHubDbContext _context;

    public EfGeographyStore(AutoHubDbContext context) => _context = context;

    public async Task<GeographySnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        var countries = await _context.Countries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var states = await _context.States
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var cities = await _context.Cities
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var rtoOffices = await _context.RtoOffices
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var pincodes = await _context.Pincodes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var serviceAreas = await _context.ServiceAreas
            .AsNoTracking()
            .OrderByDescending(area => area.Priority)
            .ToListAsync(cancellationToken);

        return new GeographySnapshot(countries, states, cities, rtoOffices, pincodes, serviceAreas);
    }
}
