using AutoHub.Platform.Caching;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Vehicles;

namespace AutoHub.Platform.Geography;

/// <summary>
/// Registry #10. Where the business operates, and what a place on a number plate means.
/// </summary>
/// <remarks>
/// Opening a new service city is the acceptance test for this registry, and it is one row plus a
/// module list. Nothing in the product carries a list of cities, a set of state codes, or an
/// <c>if (city == "pune")</c>.
/// <para>
/// This registry is also the second half of a split the SharedKernel makes deliberately.
/// <see cref="RegistrationNumber"/> parses a plate <i>structurally</i> — it will tell you the state
/// segment is <c>MH</c> and the RTO segment is <c>12</c> — and refuses to know whether either is
/// real, precisely so that opening an RTO is not a deployment. Turning <c>MH12</c> into an actual
/// office in an actual city is <see cref="ResolveRtoAsync"/>, and it is the only place that mapping
/// exists.
/// </para>
/// </remarks>
public interface IGeographyRegistry
{
    /// <summary>
    /// Cities the business operates in, optionally narrowed to those running a given module.
    /// </summary>
    /// <remarks>
    /// The module filter is the difference between "where can I buy a car?" and "where can I bid at
    /// an auction?". Without it the city picker on the auction page lists cities with no yard, and
    /// the first dealer to pick one gets an empty page and a support ticket.
    /// </remarks>
    Task<Result<GeographyList<CityView>>> GetServiceableCitiesAsync(
        string? moduleCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Turns six digits into a place — the shortest thing an Indian user will type to say where
    /// they are, and the entry point to search scope, delivery and inspection.
    /// </summary>
    Task<Result<PincodeResolution>> ResolveByPincodeAsync(string pincode, CancellationToken cancellationToken);

    /// <summary>
    /// Turns the state and RTO segments of a registration number into a real office.
    /// </summary>
    /// <remarks>
    /// Takes the parsed value object rather than a string, so the structural validation has provably
    /// already happened and this method's failures are all about data — an RTO we do not have — never
    /// about format.
    /// </remarks>
    Task<Result<RtoResolution>> ResolveRtoAsync(
        RegistrationNumber registration,
        CancellationToken cancellationToken);

    /// <summary>Coverage areas drawn around a city, most specific first.</summary>
    Task<Result<GeographyList<ServiceAreaView>>> GetServiceAreasAsync(
        string cityCode,
        string? moduleCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Which coverage areas contain a point. The question "can we come to this address?".
    /// </summary>
    /// <remarks>
    /// Highest priority first, so a caller that wants one answer takes the first and a caller that
    /// wants to explain the answer has the rest.
    /// </remarks>
    Task<Result<GeographyList<ServiceAreaView>>> FindCoveringAreasAsync(
        double latitude,
        double longitude,
        string? moduleCode,
        CancellationToken cancellationToken);

    /// <summary>Bumps the geography version, retiring every node's cached snapshot.</summary>
    Task<RegistryVersion> InvalidateAsync(CancellationToken cancellationToken);
}

/// <summary>A versioned list. Matches the <c>{ version, items }</c> envelope in §7 of the contract.</summary>
public sealed record GeographyList<T>(long Version, IReadOnlyList<T> Items);

/// <summary>
/// A city on the wire.
/// </summary>
/// <remarks>
/// Property names match the published contract exactly — <c>lat</c>, <c>lng</c>, <c>serviceable</c>,
/// <c>modules</c> — because the web and Flutter clients are already built against it. Spelling them
/// out here rather than renaming at the API edge means there is one place the contract lives.
/// </remarks>
public sealed record CityView(
    string Code,
    string Name,
    string StateCode,
    double Lat,
    double Lng,
    bool Serviceable,
    IReadOnlyList<string> Modules);

public sealed record StateView(string Code, string Name, string? GstStateCode);

/// <summary>
/// What a pincode resolves to.
/// </summary>
/// <remarks>
/// <see cref="City"/> is nullable and that is a real state, not a gap in the data: plenty of
/// pincodes belong to districts we do not model as cities. A caller that assumed a city would exist
/// would reject a perfectly valid rural address.
/// </remarks>
public sealed record PincodeResolution(
    long Version,
    string Pincode,
    CityView? City,
    StateView State,
    string? District,
    double? Lat,
    double? Lng);

/// <summary>
/// What the state and RTO segments of a plate resolve to.
/// </summary>
/// <remarks>
/// <see cref="City"/> is nullable for the same reason it is on a pincode — a district RTO need not
/// sit in a city we serve — and the state is not, because an office always has one.
/// </remarks>
public sealed record RtoResolution(
    long Version,
    string RtoCode,
    string RtoName,
    StateView State,
    CityView? City,
    string? Address);

public sealed record ServiceAreaView(
    string Code,
    string Name,
    string ModuleCode,
    string CityCode,
    string Kind,
    double? CentreLat,
    double? CentreLng,
    int? RadiusMetres,
    int Priority);

/// <summary>
/// Loads geography rows. Implemented by the persistence layer.
/// </summary>
/// <remarks>
/// Declared here so <c>AutoHub.Platform</c> never references EF Core, and so the registry can be
/// tested against a hand-built snapshot — which is how RTO resolution and polygon containment get
/// the hundred cheap tests they deserve.
/// </remarks>
public interface IGeographyStore
{
    /// <summary>
    /// Loads the whole registry.
    /// </summary>
    /// <remarks>
    /// Implementations must exclude soft-deleted rows and must include inactive ones. A vehicle
    /// registered at an RTO that has since been merged away still has to resolve, or its listing
    /// cannot say where the car is from.
    /// </remarks>
    Task<GeographySnapshot> LoadSnapshotAsync(CancellationToken cancellationToken);
}
