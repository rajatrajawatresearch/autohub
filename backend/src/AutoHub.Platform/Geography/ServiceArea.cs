using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Geography;

/// <summary>
/// Where one module can actually reach — a radius or a polygon around a city.
/// </summary>
/// <remarks>
/// A city is not a circle and coverage is not a city. Inspection can reach forty kilometres out of
/// Pune along the expressway and not much past Katraj going south; delivery is a different shape
/// again; the auction yard serves a third. Answering "can we come and inspect this car?" with
/// "is the seller's city serviceable?" is how a customer books a slot nobody can attend.
/// <para>
/// Two shapes, because the operations team needs both. A radius is what somebody types in ninety
/// seconds when a new city opens, and it is right often enough. A polygon is what they draw later,
/// once they know where the vans actually go. Supporting only polygons would make opening a city an
/// afternoon of GIS work; supporting only radii would make coverage permanently approximate.
/// </para>
/// </remarks>
public sealed class ServiceArea : AggregateRoot, IAuditable, ISoftDeletable
{
    /// <summary>
    /// The parsed boundary, computed once per process from <see cref="BoundaryGeoJson"/>.
    /// </summary>
    /// <remarks>
    /// The snapshot holding this entity is shared across every request on the node, so two threads
    /// can race to parse. That race is benign — both produce an equal ring and a reference assignment
    /// is atomic — and paying for a lock on a coverage check that runs on every pincode lookup would
    /// cost far more than the duplicate parse it prevents.
    /// </remarks>
    private IReadOnlyList<GeoPoint>? _ring;

    private ServiceArea(Guid id, Guid cityId, string code, string name, string moduleCode) : base(id)
    {
        CityId = cityId;
        Code = code;
        Name = name;
        ModuleCode = moduleCode;
        Kind = ServiceAreaKind.Radius;
        IsActive = true;
    }

    private ServiceArea() { }

    public Guid CityId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Which module this coverage is for — <c>inspection</c>, <c>delivery</c>, <c>rental</c>.
    /// A string because the module list is registry #6's data, not an enum in this assembly.
    /// </summary>
    public string ModuleCode { get; private set; } = string.Empty;

    public ServiceAreaKind Kind { get; private set; }

    public double? CentreLatitude { get; private set; }

    public double? CentreLongitude { get; private set; }

    /// <summary>Radius in metres. Metres, not kilometres — the same reason money is in paise.</summary>
    public int? RadiusMetres { get; private set; }

    /// <summary>
    /// A GeoJSON <c>Polygon</c>. Only the outer ring is honoured; holes are ignored.
    /// </summary>
    /// <remarks>
    /// Stored as GeoJSON rather than a PostGIS geometry column so this assembly stays free of a
    /// spatial dependency and the value round-trips to the admin map component unchanged. The
    /// persistence layer is free to also project it into a <c>geography(Polygon,4326)</c> column for
    /// indexed containment queries — the two are the same boundary, one authored, one indexed.
    /// </remarks>
    public string? BoundaryGeoJson { get; private set; }

    /// <summary>
    /// Higher wins when two areas overlap. Authored, not inferred — an operator who draws a premium
    /// zone inside a standard one has to be able to say which answer they want.
    /// </summary>
    public int Priority { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<ServiceArea> CreateRadius(
        Guid cityId,
        string code,
        string name,
        string moduleCode,
        double centreLatitude,
        double centreLongitude,
        int radiusMetres)
    {
        var created = CreateCore(cityId, code, name, moduleCode);
        if (created.IsFailure)
        {
            return created;
        }

        var point = Geodesy.ValidatePoint(centreLatitude, centreLongitude, "geo.service_area");
        if (point.IsFailure)
        {
            return Result.Failure<ServiceArea>(point.Error);
        }

        if (radiusMetres <= 0)
        {
            return Error.Validation("geo.service_area.radius_invalid", "A radius must be greater than zero.");
        }

        var area = created.Value;
        area.Kind = ServiceAreaKind.Radius;
        area.CentreLatitude = centreLatitude;
        area.CentreLongitude = centreLongitude;
        area.RadiusMetres = radiusMetres;
        return area;
    }

    public static Result<ServiceArea> CreatePolygon(
        Guid cityId,
        string code,
        string name,
        string moduleCode,
        JsonNode boundary)
    {
        var created = CreateCore(cityId, code, name, moduleCode);
        if (created.IsFailure)
        {
            return created;
        }

        var ring = ParseRing(boundary);
        if (ring.IsFailure)
        {
            return Result.Failure<ServiceArea>(ring.Error);
        }

        var area = created.Value;
        area.Kind = ServiceAreaKind.Polygon;
        area.BoundaryGeoJson = boundary.ToJsonString();
        area._ring = ring.Value;
        return area;
    }

    /// <summary>
    /// Whether a point falls inside this area.
    /// </summary>
    /// <remarks>
    /// A malformed boundary returns <c>false</c>, not an exception. The safe direction for coverage
    /// is "we do not serve here": promising a delivery we cannot make costs a refund and a review,
    /// while declining one we could have made costs a lead. Both are bad; only one is expensive.
    /// </remarks>
    public bool Covers(double latitude, double longitude)
    {
        if (!IsActive)
        {
            return false;
        }

        if (Kind == ServiceAreaKind.Radius)
        {
            if (CentreLatitude is not { } lat || CentreLongitude is not { } lon || RadiusMetres is not { } radius)
            {
                return false;
            }

            return Geodesy.DistanceMetres(lat, lon, latitude, longitude) <= radius;
        }

        var ring = Ring();
        return ring is not null && Geodesy.IsPointInRing(ring, latitude, longitude);
    }

    /// <summary>The outer ring, parsed on first use. Null when the stored boundary is unusable.</summary>
    public IReadOnlyList<GeoPoint>? Ring()
    {
        if (_ring is not null)
        {
            return _ring;
        }

        if (string.IsNullOrWhiteSpace(BoundaryGeoJson))
        {
            return null;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(BoundaryGeoJson);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        if (node is null)
        {
            return null;
        }

        var parsed = ParseRing(node);
        if (parsed.IsFailure)
        {
            return null;
        }

        _ring = parsed.Value;
        return _ring;
    }

    public void Prioritise(int priority) => Priority = priority;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static Result<ServiceArea> CreateCore(Guid cityId, string code, string name, string moduleCode)
    {
        var validCode = GeographyCode.ValidateSlug(code, "geo.service_area.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<ServiceArea>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("geo.service_area.name_required", "A service area needs a name.");
        }

        if (string.IsNullOrWhiteSpace(moduleCode))
        {
            return Error.Validation("geo.service_area.module_required", "A service area needs a module code.");
        }

        return new ServiceArea(UuidV7.New(), cityId, validCode.Value, name.Trim(), moduleCode.Trim());
    }

    /// <summary>
    /// Reads the outer ring out of a GeoJSON <c>Polygon</c>.
    /// </summary>
    /// <remarks>
    /// GeoJSON stores each position as <c>[longitude, latitude]</c> — the opposite of how every map
    /// URL, every phone GPS reading and every human writes it. Transposing the two silently relocates
    /// Pune to the Indian Ocean, and because both numbers are plausible latitudes nothing downstream
    /// notices. The order is honoured here, once, and converted into a named
    /// <see cref="GeoPoint"/> so no other code has to remember.
    /// </remarks>
    private static Result<IReadOnlyList<GeoPoint>> ParseRing(JsonNode boundary)
    {
        if (boundary is not JsonObject polygon
            || !polygon.TryGetPropertyValue("coordinates", out var coordinates)
            || coordinates is not JsonArray rings
            || rings.Count == 0
            || rings[0] is not JsonArray outer)
        {
            return Error.Validation(
                "geo.service_area.boundary_malformed",
                "The boundary must be a GeoJSON Polygon with at least an outer ring.");
        }

        var points = new List<GeoPoint>(outer.Count);

        foreach (var position in outer)
        {
            if (position is not JsonArray pair
                || pair.Count < 2
                || pair[0] is not JsonValue longitudeValue
                || pair[1] is not JsonValue latitudeValue
                || !longitudeValue.TryGetValue<double>(out var longitude)
                || !latitudeValue.TryGetValue<double>(out var latitude))
            {
                return Error.Validation(
                    "geo.service_area.boundary_malformed",
                    "Every boundary position must be a [longitude, latitude] pair.");
            }

            if (Geodesy.ValidatePoint(latitude, longitude, "geo.service_area").IsFailure)
            {
                return Error.Validation(
                    "geo.service_area.boundary_out_of_range",
                    "A boundary position is not a valid coordinate.")
                    .With("latitude", latitude)
                    .With("longitude", longitude);
            }

            points.Add(new GeoPoint(latitude, longitude));
        }

        // Three distinct corners is the minimum that encloses anything. GeoJSON also repeats the
        // first position as the last to close the ring, so a valid triangle arrives with four.
        if (points.Count < 3)
        {
            return Error.Validation(
                "geo.service_area.boundary_degenerate",
                "A boundary needs at least three positions.");
        }

        return points;
    }
}

public enum ServiceAreaKind
{
    /// <summary>A circle. What an operator draws in ninety seconds when a city opens.</summary>
    Radius = 1,

    /// <summary>A GeoJSON polygon. What replaces the circle once the routes are known.</summary>
    Polygon = 2,
}
