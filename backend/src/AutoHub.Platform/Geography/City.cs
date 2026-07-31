using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Geography;

/// <summary>
/// A city, and — separately — whether the business actually operates in it.
/// </summary>
/// <remarks>
/// This is registry #10's whole reason for existing: *"open a new service city without a deploy."*
/// A city is a row, launching it is a boolean, and launching only Lease in Bengaluru is one entry in
/// <see cref="EnabledModules"/>.
/// <para>
/// <b>Existing and being serviceable are different facts.</b> Nashik exists whether or not we have
/// an inspector there — a seller in Nashik still has a valid address, still has an RTO, and a
/// listing there is still findable by a buyer willing to travel. Collapsing the two into one flag
/// means either hiding real places or promising coverage we do not have.
/// </para>
/// <para>
/// <see cref="EnabledModules"/> is per-city rather than per-tenant because the operational reality
/// is per-city: auction needs a yard, inspection needs an inspector, rental needs a fleet, and each
/// of those arrives in a different city in a different month. A global module flag would force the
/// slowest city to gate the fastest.
/// </para>
/// </remarks>
public sealed class City : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<string> _enabledModules = [];

    private City(Guid id, Guid stateId, string code, string name, double latitude, double longitude) : base(id)
    {
        StateId = stateId;
        Code = code;
        Name = name;
        Latitude = latitude;
        Longitude = longitude;
        IsActive = true;
    }

    private City() { }

    public Guid StateId { get; private set; }

    /// <summary>URL slug — <c>pune</c>. Travels in <c>?city=</c> and in the <c>X-Active-City</c> header.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// City centroid. Used for distance sorting and map centring, never for a delivery decision —
    /// coverage is a <see cref="ServiceArea"/>, because a city is not a circle.
    /// </summary>
    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    /// <summary>Whether the business operates here at all. Independent of <see cref="IsActive"/>.</summary>
    public bool IsServiceable { get; private set; }

    /// <summary>
    /// Module codes live here, e.g. <c>buy</c>, <c>sell</c>, <c>auction</c>, <c>rental</c>.
    /// </summary>
    /// <remarks>
    /// Strings, not an enum, and not a set of boolean columns. The module list is registry #6's data;
    /// a column per module would mean a migration every time a module launches, which is the exact
    /// coupling the ten registries exist to remove.
    /// </remarks>
    public IReadOnlyList<string> EnabledModules => _enabledModules;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<City> Create(Guid stateId, string code, string name, double latitude, double longitude)
    {
        var validCode = GeographyCode.ValidateSlug(code, "geo.city.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<City>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("geo.city.name_required", "A city needs a name.");
        }

        var point = Geodesy.ValidatePoint(latitude, longitude, "geo.city");
        if (point.IsFailure)
        {
            return Result.Failure<City>(point.Error);
        }

        return new City(UuidV7.New(), stateId, validCode.Value, name.Trim(), latitude, longitude);
    }

    /// <summary>Whether a given module is live here. The only correct way to ask.</summary>
    public bool Offers(string moduleCode)
        => IsServiceable && _enabledModules.Contains(moduleCode, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Opens the city for business with a module list.
    /// </summary>
    /// <remarks>
    /// Replaces rather than merges. An operator editing this list is looking at the whole list, and
    /// a merge would make removing a module impossible through the same screen that adds one.
    /// </remarks>
    public void OpenFor(IEnumerable<string> moduleCodes)
    {
        _enabledModules.Clear();

        foreach (var code in moduleCodes.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            var trimmed = code.Trim();

            if (!_enabledModules.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                _enabledModules.Add(trimmed);
            }
        }

        IsServiceable = _enabledModules.Count > 0;
    }

    /// <summary>
    /// Stops taking new business here without hiding the city.
    /// </summary>
    /// <remarks>
    /// The module list is kept deliberately. Suspending a city for a week — a flood, a strike, a
    /// yard closure — and then having to remember which four modules were live is how a city comes
    /// back missing one.
    /// </remarks>
    public void Suspend() => IsServiceable = false;

    public void Resume() => IsServiceable = _enabledModules.Count > 0;

    public void Reposition(double latitude, double longitude, int sortOrder)
    {
        Latitude = latitude;
        Longitude = longitude;
        SortOrder = sortOrder;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
