using System.Globalization;
using AutoHub.Platform.Caching;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Vehicles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Geography;

/// <summary>How long a superseded geography snapshot lingers in a node's memory.</summary>
public sealed class GeographyRegistryOptions
{
    /// <summary>
    /// Backstop lifetime for a snapshot entry. Not the invalidation mechanism — a version bump makes
    /// the key unreachable long before this elapses. Long, because geography changes a few times a
    /// year and reloading nineteen thousand pincodes for no reason is pure waste.
    /// </summary>
    public TimeSpan SnapshotTtl { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Serves geography from an in-process snapshot, keyed by the shared registry version.
/// </summary>
/// <remarks>
/// The payload stays in process memory rather than in the distributed tier for the same reason the
/// taxonomy snapshot does: it is a graph of domain entities with private constructors and computed
/// lookups, not a document, and JSON round-tripping it would cost more than the query it saves.
/// Only the version is shared, and because the version is part of the cache key, a publish on one
/// node retires every other node's snapshot on its next read.
/// </remarks>
public sealed class GeographyRegistry : IGeographyRegistry
{
    private const string SnapshotKeyPrefix = "platform:geography:snapshot:v";

    /// <summary>
    /// One loader at a time, process-wide. A cold cache happens on every deploy and every publish,
    /// and without this the first burst of traffic runs the full nineteen-thousand-row load once per
    /// in-flight request.
    /// </summary>
    private static readonly SemaphoreSlim SnapshotGate = new(1, 1);

    private readonly IGeographyStore _store;
    private readonly IRegistryCache _registryCache;
    private readonly IMemoryCache _memory;
    private readonly GeographyRegistryOptions _options;
    private readonly ILogger<GeographyRegistry> _logger;

    public GeographyRegistry(
        IGeographyStore store,
        IRegistryCache registryCache,
        IMemoryCache memory,
        GeographyRegistryOptions options,
        ILogger<GeographyRegistry> logger)
    {
        _store = store;
        _registryCache = registryCache;
        _memory = memory;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<GeographyList<CityView>>> GetServiceableCitiesAsync(
        string? moduleCode,
        CancellationToken cancellationToken)
    {
        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);

        var items = snapshot.Cities
            .Where(c => c.IsActive && c.IsServiceable)
            .Where(c => string.IsNullOrWhiteSpace(moduleCode) || c.Offers(moduleCode))
            .Select(c => ToView(snapshot, c))
            .OfType<CityView>()
            .ToList();

        return new GeographyList<CityView>(version, items);
    }

    public async Task<Result<PincodeResolution>> ResolveByPincodeAsync(
        string pincode,
        CancellationToken cancellationToken)
    {
        var validated = GeographyCode.ValidatePincode(pincode, "geo.pincode.invalid");
        if (validated.IsFailure)
        {
            return Result.Failure<PincodeResolution>(validated.Error);
        }

        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);
        var row = snapshot.Pincode(validated.Value);

        if (row is null || !row.IsActive)
        {
            // A well-formed pincode we do not have is a data gap, not a user error, and is worth
            // distinguishing from a malformed one: the user typed something real and we came up
            // empty, which is a message about us rather than about them.
            return Error.NotFound("geo.pincode.not_found", "We do not have that pincode on file.")
                .With("pincode", validated.Value);
        }

        var state = snapshot.State(row.StateId);
        if (state is null)
        {
            return OrphanedRow("geo.pincode.state_missing", validated.Value);
        }

        var city = row.CityId is { } cityId ? snapshot.City(cityId) : null;

        return new PincodeResolution(
            version,
            row.Code,
            city is null ? null : ToView(snapshot, city),
            ToView(state),
            row.District,
            row.Latitude,
            row.Longitude);
    }

    public async Task<Result<RtoResolution>> ResolveRtoAsync(
        RegistrationNumber registration,
        CancellationToken cancellationToken)
    {
        // A Bharat-series vehicle genuinely has no RTO of record — that is the entire point of the
        // series, which exists so a vehicle can move between states without re-registering. Reporting
        // "not found" would send the caller looking for a data gap that does not exist, so it gets
        // its own code and callers branch on it.
        if (registration.Format == RegistrationFormat.BharatSeries)
        {
            return Error.Validation(
                "geo.rto.not_applicable",
                "A Bharat-series registration is not tied to a single RTO office.")
                .With("registration", registration.Canonical);
        }

        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);
        var office = FindRtoOffice(snapshot, registration.StateOrYearCode, registration.RtoOrSeriesCode);

        if (office is null)
        {
            var code = registration.StateOrYearCode + registration.RtoOrSeriesCode;

            return Error.NotFound("geo.rto.not_found", $"We do not have RTO '{code}' on file.")
                .With("rtoCode", code)
                .With("stateCode", registration.StateOrYearCode);
        }

        var state = snapshot.State(office.StateId);
        if (state is null)
        {
            return OrphanedRow<RtoResolution>("geo.rto.state_missing", office.Code);
        }

        var city = office.CityId is { } cityId ? snapshot.City(cityId) : null;

        return new RtoResolution(
            version,
            office.Code,
            office.Name,
            ToView(state),
            city is null ? null : ToView(snapshot, city),
            office.Address);
    }

    public async Task<Result<GeographyList<ServiceAreaView>>> GetServiceAreasAsync(
        string cityCode,
        string? moduleCode,
        CancellationToken cancellationToken)
    {
        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);
        var city = snapshot.City(cityCode);

        if (city is null)
        {
            return Error.NotFound("geo.city.not_found", $"No city with code '{cityCode}'.")
                .With("cityCode", cityCode);
        }

        var items = snapshot.ServiceAreasOf(city.Id)
            .Where(a => a.IsActive)
            .Where(a => string.IsNullOrWhiteSpace(moduleCode)
                        || string.Equals(a.ModuleCode, moduleCode, StringComparison.OrdinalIgnoreCase))
            .Select(a => ToView(a, city.Code))
            .ToList();

        return new GeographyList<ServiceAreaView>(version, items);
    }

    public async Task<Result<GeographyList<ServiceAreaView>>> FindCoveringAreasAsync(
        double latitude,
        double longitude,
        string? moduleCode,
        CancellationToken cancellationToken)
    {
        var point = Geodesy.ValidatePoint(latitude, longitude, "geo.coverage");
        if (point.IsFailure)
        {
            return Result.Failure<GeographyList<ServiceAreaView>>(point.Error);
        }

        var (snapshot, version) = await GetSnapshotAsync(cancellationToken);

        // A linear scan over every area, on purpose. There are tens of these, not thousands, and a
        // scan of tens costs less than the round trip a spatial index would save. When operations
        // has drawn enough polygons for that to stop being true, this moves behind the store as a
        // PostGIS ST_Contains query and nothing above this method changes.
        var items = snapshot.ServiceAreas
            .Where(a => a.IsActive)
            .Where(a => string.IsNullOrWhiteSpace(moduleCode)
                        || string.Equals(a.ModuleCode, moduleCode, StringComparison.OrdinalIgnoreCase))
            .Where(a => a.Covers(latitude, longitude))
            .OrderByDescending(a => a.Priority)
            .Select(a => ToView(a, snapshot.City(a.CityId)?.Code ?? string.Empty))
            .ToList();

        return new GeographyList<ServiceAreaView>(version, items);
    }

    public async Task<RegistryVersion> InvalidateAsync(CancellationToken cancellationToken)
    {
        var version = await _registryCache.InvalidateAsync(RegistryNames.Geography, cancellationToken);

        _logger.LogInformation("Geography registry invalidated; now {Version}", version);
        return version;
    }

    private static CityView? ToView(GeographySnapshot snapshot, City city)
    {
        var state = snapshot.State(city.StateId);

        // A city whose state row is missing cannot be rendered — the contract requires a stateCode —
        // so it is dropped from the list rather than emitted with an empty one that a client would
        // then try to use as a filter.
        return state is null
            ? null
            : new CityView(
                city.Code,
                city.Name,
                state.Code,
                city.Latitude,
                city.Longitude,
                city.IsServiceable,
                city.EnabledModules);
    }

    private static StateView ToView(State state) => new(state.Code, state.Name, state.GstStateCode);

    /// <summary>
    /// Finds an office from a plate's state and district segments, tolerating the leading zero.
    /// </summary>
    /// <remarks>
    /// The district number is written both ways in the wild and neither is wrong. A plate reads
    /// "MH 01 AB 1234", so the parser hands back <c>01</c>; the RTO's own directory writes the same
    /// office as <c>MH1</c> about as often as <c>MH01</c>, and a dealer's bulk upload will contain
    /// whichever the exporting system preferred. Matching only the literal concatenation would mean
    /// every Mumbai and Delhi registration fails to resolve depending on how the seed data was
    /// keyed — a data-entry detail becoming a product bug.
    /// <para>
    /// So the padded and unpadded forms are both tried, in that order. This is normalisation at the
    /// lookup boundary only: the stored code is never rewritten, so whichever form operations
    /// authored stays authoritative.
    /// </para>
    /// </remarks>
    private static RtoOffice? FindRtoOffice(GeographySnapshot snapshot, string stateCode, string districtDigits)
    {
        var office = snapshot.RtoOffice(stateCode + districtDigits);
        if (office is not null)
        {
            return office;
        }

        var padded = districtDigits.PadLeft(2, '0');
        if (!string.Equals(padded, districtDigits, StringComparison.Ordinal))
        {
            office = snapshot.RtoOffice(stateCode + padded);
            if (office is not null)
            {
                return office;
            }
        }

        // TrimStart can empty the string on a nonsensical "MH00"; an empty district is not an office.
        var trimmed = districtDigits.TrimStart('0');

        return trimmed.Length == 0 || string.Equals(trimmed, districtDigits, StringComparison.Ordinal)
            ? null
            : snapshot.RtoOffice(stateCode + trimmed);
    }

    private static ServiceAreaView ToView(ServiceArea area, string cityCode)
        => new(
            area.Code,
            area.Name,
            area.ModuleCode,
            cityCode,
            area.Kind == ServiceAreaKind.Radius ? "radius" : "polygon",
            area.CentreLatitude,
            area.CentreLongitude,
            area.RadiusMetres,
            area.Priority);

    /// <summary>
    /// A row whose required parent is missing.
    /// </summary>
    /// <remarks>
    /// Unexpected rather than NotFound: the caller asked for something that does exist, and the
    /// reason they cannot have it is our referential integrity, not their input. Surfacing it as a
    /// 404 would send an operator looking for a missing pincode instead of a missing state.
    /// </remarks>
    private static Error OrphanedRow(string code, string identifier)
        => Error.Unexpected(code, "That location is on file but its parent record is missing.")
            .With("identifier", identifier);

    private static Result<T> OrphanedRow<T>(string code, string identifier)
        => Result.Failure<T>(OrphanedRow(code, identifier));

    private async Task<(GeographySnapshot Snapshot, long Version)> GetSnapshotAsync(
        CancellationToken cancellationToken)
    {
        var version = await _registryCache.GetVersionAsync(RegistryNames.Geography, cancellationToken);
        var key = string.Create(CultureInfo.InvariantCulture, $"{SnapshotKeyPrefix}{version.Value}");

        if (_memory.TryGetValue(key, out GeographySnapshot? cached) && cached is not null)
        {
            return (cached, version.Value);
        }

        await SnapshotGate.WaitAsync(cancellationToken);
        try
        {
            if (_memory.TryGetValue(key, out cached) && cached is not null)
            {
                return (cached, version.Value);
            }

            var snapshot = await _store.LoadSnapshotAsync(cancellationToken);
            _memory.Set(key, snapshot, _options.SnapshotTtl);

            _logger.LogInformation("Geography snapshot loaded at {Version}", version);
            return (snapshot, version.Value);
        }
        finally
        {
            SnapshotGate.Release();
        }
    }
}
