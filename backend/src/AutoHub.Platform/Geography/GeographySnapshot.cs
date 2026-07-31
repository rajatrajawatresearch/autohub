namespace AutoHub.Platform.Geography;

/// <summary>
/// All of Indian geography, loaded once and answered from memory.
/// </summary>
/// <remarks>
/// The whole registry is small enough to hold: one country, thirty-six states and union territories,
/// about fourteen hundred RTO offices, some nineteen thousand pincodes, and however many cities and
/// service areas operations has drawn. That is a few megabytes of objects for data that changes when
/// a new RTO opens — a few times a year.
/// <para>
/// Everything else follows from that. Resolving a pincode is a dictionary lookup rather than a
/// query, which matters because it happens on the checkout path, the delivery-estimate path, the
/// sell form and the search scope — several times per user session, on data that is effectively
/// immutable.
/// </para>
/// <para>
/// The one thing that would break this is a second country, at which point the snapshot becomes
/// per-country and the lookups take a country id. That is a change to this class only, which is why
/// the country is already a row rather than an assumption.
/// </para>
/// </remarks>
public sealed class GeographySnapshot
{
    private readonly Dictionary<Guid, Country> _countriesById;

    private readonly Dictionary<Guid, State> _statesById;
    private readonly Dictionary<string, State> _statesByCode;

    private readonly Dictionary<Guid, City> _citiesById;
    private readonly Dictionary<string, City> _citiesByCode;

    private readonly Dictionary<string, RtoOffice> _rtoOfficesByCode;

    private readonly Dictionary<string, Pincode> _pincodesByCode;

    private readonly Dictionary<Guid, List<ServiceArea>> _serviceAreasByCity;

    public GeographySnapshot(
        IReadOnlyList<Country> countries,
        IReadOnlyList<State> states,
        IReadOnlyList<City> cities,
        IReadOnlyList<RtoOffice> rtoOffices,
        IReadOnlyList<Pincode> pincodes,
        IReadOnlyList<ServiceArea> serviceAreas)
    {
        _countriesById = countries.ToDictionary(c => c.Id);

        _statesById = states.ToDictionary(s => s.Id);
        // Case-insensitive on the way in because a plate is typed as "mh12ab1234" as often as not,
        // and a pasted spreadsheet cell is whatever the last tool that touched it decided.
        _statesByCode = states.ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

        _citiesById = cities.ToDictionary(c => c.Id);
        _citiesByCode = cities.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        _rtoOfficesByCode = rtoOffices.ToDictionary(r => r.Code, StringComparer.OrdinalIgnoreCase);

        _pincodesByCode = pincodes.ToDictionary(p => p.Code, StringComparer.Ordinal);

        _serviceAreasByCity = [];
        foreach (var area in serviceAreas.OrderByDescending(a => a.Priority))
        {
            if (!_serviceAreasByCity.TryGetValue(area.CityId, out var areas))
            {
                areas = [];
                _serviceAreasByCity[area.CityId] = areas;
            }

            areas.Add(area);
        }

        Cities = [.. cities
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name, StringComparer.Ordinal)];

        ServiceAreas = serviceAreas;
    }

    /// <summary>Every city, in authored order. Serviceability is filtered per response, not here.</summary>
    public IReadOnlyList<City> Cities { get; }

    public IReadOnlyList<ServiceArea> ServiceAreas { get; }

    public Country? Country(Guid id) => _countriesById.GetValueOrDefault(id);

    public State? State(string code) => _statesByCode.GetValueOrDefault(code);

    public State? State(Guid id) => _statesById.GetValueOrDefault(id);

    public City? City(string code) => _citiesByCode.GetValueOrDefault(code);

    public City? City(Guid id) => _citiesById.GetValueOrDefault(id);

    public RtoOffice? RtoOffice(string code) => _rtoOfficesByCode.GetValueOrDefault(code);

    public Pincode? Pincode(string code) => _pincodesByCode.GetValueOrDefault(code);

    public IReadOnlyList<ServiceArea> ServiceAreasOf(Guid cityId)
        => _serviceAreasByCity.TryGetValue(cityId, out var areas) ? areas : [];
}
