using AutoHub.Modules.Valuation.Application.Abstractions;
using AutoHub.Modules.Valuation.Domain.Models;
using AutoHub.Platform.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AutoHub.Persistence.Repositories;

public sealed class EfValuationRepository : IValuationRepository
{
    private readonly AutoHubDbContext _context;

    public EfValuationRepository(AutoHubDbContext context) => _context = context;

    public async Task<Modules.Valuation.Domain.Valuations.Valuation?> GetAsync(
        Guid valuationId,
        CancellationToken cancellationToken)
        => await _context.Valuations
            .Include(valuation => valuation.Channels)
            .Include(valuation => valuation.Adjustments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(valuation => valuation.Id == valuationId, cancellationToken);

    /// <summary>
    /// The current opinion of a vehicle's worth.
    /// </summary>
    /// <remarks>
    /// Shadow rows are excluded here rather than by the caller. A model under evaluation deciding
    /// what a seller may ask is exactly what shadow mode exists to prevent, and a filter the caller
    /// has to remember is a filter one caller will forget.
    /// <para>
    /// Ordered by id descending rather than by a timestamp: the primary key is UUID v7, so it is
    /// already chronological, and the index it uses is the one that already exists.
    /// </para>
    /// </remarks>
    public async Task<Modules.Valuation.Domain.Valuations.Valuation?> GetLatestForVehicleAsync(
        Guid vehicleId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
        => await _context.Valuations
            .Include(valuation => valuation.Channels)
            .Include(valuation => valuation.Adjustments)
            .AsSplitQuery()
            .Where(valuation =>
                valuation.VehicleId == vehicleId
                && !valuation.IsShadow
                && valuation.ExpiresAt > asOf)
            .OrderByDescending(valuation => valuation.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(
        Modules.Valuation.Domain.Valuations.Valuation valuation,
        CancellationToken cancellationToken)
        => await _context.Valuations.AddAsync(valuation, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// Loads published valuation models, cached like every other registry.
/// </summary>
/// <remarks>
/// <b>Untracked and cached, for the same reason the taxonomy snapshot is.</b> These definitions are
/// read on every valuation request and change a few times a month; a tracked graph handed to
/// concurrent requests would keep a scoped <c>DbContext</c> alive past its request and hand one
/// change tracker to several threads.
/// <para>
/// Cached in memory only. The graph has private constructors and computed lookups that would not
/// survive a JSON round trip, so the shared registry cache supplies the <i>version</i> — which is
/// part of the cache key — and the payload stays in process. An operator publishing a new model on
/// one node moves the version, and every other node's key becomes unreachable on its next read.
/// </para>
/// </remarks>
public sealed class EfValuationModelStore : IValuationModelStore
{
    private const string CacheKeyPrefix = "valuation:models:v";

    /// <summary>
    /// One loader at a time, process-wide.
    /// </summary>
    /// <remarks>
    /// A cold cache happens on every deploy and every publish. Without this gate the first burst of
    /// traffic after either runs the full load once per in-flight request, which is how a routine
    /// deployment becomes a database incident on the homepage's busiest endpoint.
    /// </remarks>
    private static readonly SemaphoreSlim LoadGate = new(1, 1);

    private readonly AutoHubDbContext _context;
    private readonly IRegistryCache _cache;
    private readonly IMemoryCache _memory;

    public EfValuationModelStore(
        AutoHubDbContext context,
        IRegistryCache cache,
        IMemoryCache memory)
    {
        _context = context;
        _cache = cache;
        _memory = memory;
    }

    public async Task<IReadOnlyList<ValuationModelDefinition>> GetPublishedAsync(
        CancellationToken cancellationToken)
    {
        // Keyed off the pricing registry's version, because a valuation model is a pricing artefact
        // and the admin path that publishes one invalidates that registry. One version to bump beats
        // an eleventh registry name nobody remembers to invalidate.
        var version = await _cache.GetVersionAsync(RegistryNames.Pricing, cancellationToken);
        var key = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{CacheKeyPrefix}{version.Value}");

        if (_memory.TryGetValue(key, out IReadOnlyList<ValuationModelDefinition>? cached) && cached is not null)
        {
            return cached;
        }

        await LoadGate.WaitAsync(cancellationToken);
        try
        {
            if (_memory.TryGetValue(key, out cached) && cached is not null)
            {
                return cached;
            }

            var definitions = await _context.ValuationModels
                .Include(definition => definition.Curves)
                    .ThenInclude(curve => curve.Points)
                .Include(definition => definition.Factors)
                .Include(definition => definition.Channels)
                .Include(definition => definition.ConfidenceWeights)
                .Include(definition => definition.ConfidenceBands)
                .AsSplitQuery()
                .AsNoTrackingWithIdentityResolution()
                .Where(definition => definition.IsPublished)
                .ToListAsync(cancellationToken);

            _memory.Set(key, (IReadOnlyList<ValuationModelDefinition>)definitions, TimeSpan.FromMinutes(30));
            return definitions;
        }
        finally
        {
            LoadGate.Release();
        }
    }
}
