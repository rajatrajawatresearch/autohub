using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Geography;

/// <summary>
/// A six-digit postal code and the place it belongs to.
/// </summary>
/// <remarks>
/// The pincode is the shortest thing an Indian user will type that identifies where they are, which
/// makes it the entry point to almost every location decision in the product: which city to scope
/// search to, whether inspection can reach them, which dealer is nearest, what the delivery fee is.
/// Every one of those starts by resolving this row.
/// <para>
/// The mapping is many-to-one and slightly lossy in the real world — a handful of pincodes straddle
/// municipal boundaries, and India Post redraws them occasionally. That is why resolution returns a
/// city and the <i>coverage</i> question is answered separately by <see cref="ServiceArea"/>: the
/// pincode says roughly where you are, the service area says whether we can actually get there.
/// </para>
/// </remarks>
public sealed class Pincode : AggregateRoot, IAuditable, ISoftDeletable
{
    private Pincode(Guid id, string code, Guid stateId, Guid? cityId) : base(id)
    {
        Code = code;
        StateId = stateId;
        CityId = cityId;
        IsActive = true;
    }

    private Pincode() { }

    /// <summary>Six digits, never starting with zero. The natural key.</summary>
    public string Code { get; private set; } = string.Empty;

    public Guid StateId { get; private set; }

    /// <summary>Null for a pincode in a district we do not model as a city. See the remarks.</summary>
    public Guid? CityId { get; private set; }

    /// <summary>District or taluka name as India Post publishes it. Display only.</summary>
    public string? District { get; private set; }

    /// <summary>
    /// Centroid of the delivery area, when known. Optional because the published pincode directory
    /// does not carry coordinates and they are enriched separately.
    /// </summary>
    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<Pincode> Create(string code, Guid stateId, Guid? cityId = null, string? district = null)
    {
        var validCode = GeographyCode.ValidatePincode(code, "geo.pincode.invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<Pincode>(validCode.Error);
        }

        return new Pincode(UuidV7.New(), validCode.Value, stateId, cityId) { District = district?.Trim() };
    }

    public Result SetCentroid(double latitude, double longitude)
    {
        var point = Geodesy.ValidatePoint(latitude, longitude, "geo.pincode");
        if (point.IsFailure)
        {
            return point.ToResult();
        }

        Latitude = latitude;
        Longitude = longitude;
        return Result.Success();
    }

    public void AssignToCity(Guid? cityId) => CityId = cityId;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
