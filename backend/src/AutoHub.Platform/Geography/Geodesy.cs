using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Geography;

/// <summary>
/// The two geometric questions this registry has to answer, and nothing else.
/// </summary>
/// <remarks>
/// Deliberately tiny. PostGIS is the authority on geometry — <c>ST_Contains</c> over a GiST index is
/// how "which of four hundred service areas cover this point?" gets answered at scale, and it is
/// already an extension on the database. What lives here is the single-area check the domain needs
/// to be able to make without a round trip: given one area we already hold in memory and one point,
/// is the point inside it?
/// <para>
/// That distinction matters because the store interface is shaped so the two can swap. Today the
/// snapshot holds every area and the check runs here; when there are thousands, the same call goes
/// to the database and nothing above changes.
/// </para>
/// </remarks>
internal static class Geodesy
{
    /// <summary>Mean Earth radius in metres (IUGG). Distances here are for coverage, not navigation.</summary>
    private const double EarthRadiusMetres = 6_371_008.8;

    public static Result<bool> ValidatePoint(double latitude, double longitude, string errorPrefix)
    {
        if (double.IsNaN(latitude) || latitude is < -90 or > 90)
        {
            return Error.Validation($"{errorPrefix}.latitude_invalid", "Latitude must be between -90 and 90.")
                .With("latitude", latitude);
        }

        if (double.IsNaN(longitude) || longitude is < -180 or > 180)
        {
            return Error.Validation($"{errorPrefix}.longitude_invalid", "Longitude must be between -180 and 180.")
                .With("longitude", longitude);
        }

        return true;
    }

    /// <summary>
    /// Great-circle distance in metres.
    /// </summary>
    /// <remarks>
    /// Haversine, not the flat-earth approximation. Over the ~50 km radii a service area uses, the
    /// planar shortcut is off by tens of metres — irrelevant — but it also degrades badly with
    /// longitude near the poles, and writing the correct one costs four lines. Vincenty's ellipsoidal
    /// formula would be more accurate still and is pointless here: nobody's delivery decision turns
    /// on ten metres.
    /// </remarks>
    public static double DistanceMetres(double lat1, double lon1, double lat2, double lon2)
    {
        var phi1 = double.DegreesToRadians(lat1);
        var phi2 = double.DegreesToRadians(lat2);
        var deltaPhi = double.DegreesToRadians(lat2 - lat1);
        var deltaLambda = double.DegreesToRadians(lon2 - lon1);

        var a = (Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2))
                + (Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2));

        return 2 * EarthRadiusMetres * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>
    /// Point-in-polygon by ray casting, on the raw lat/lng plane.
    /// </summary>
    /// <remarks>
    /// Treating degrees as a plane is wrong in general and correct enough here: a service-area
    /// polygon spans tens of kilometres, over which the projection error is far smaller than the
    /// precision with which anyone drew the boundary in the first place. It would be wrong for a
    /// polygon that crosses the antimeridian, which no Indian service area does.
    /// <para>
    /// The algorithm counts how many times an eastward ray from the point crosses the boundary; odd
    /// means inside. Both endpoint tests use the same strict <c>&gt;</c>, which is what makes a vertex
    /// lying exactly on the ray count for one edge and not its neighbour. Mixing <c>&gt;</c> with
    /// <c>&gt;=</c> there is the classic way this function reports a point outside a polygon it is
    /// plainly inside.
    /// </para>
    /// </remarks>
    public static bool IsPointInRing(IReadOnlyList<GeoPoint> ring, double latitude, double longitude)
    {
        if (ring.Count < 3)
        {
            return false;
        }

        var inside = false;

        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var current = ring[i];
            var previous = ring[j];

            if (current.Latitude > latitude != previous.Latitude > latitude)
            {
                var crossingLongitude = ((previous.Longitude - current.Longitude)
                                         * (latitude - current.Latitude)
                                         / (previous.Latitude - current.Latitude))
                                        + current.Longitude;

                if (longitude < crossingLongitude)
                {
                    inside = !inside;
                }
            }
        }

        return inside;
    }
}

/// <summary>A latitude/longitude pair, in that order.</summary>
/// <remarks>
/// Named fields rather than a tuple because GeoJSON stores coordinates as <c>[longitude, latitude]</c>
/// and almost everything else in the world writes them the other way round. A positional pair here
/// would make the transposition invisible at exactly the point where it happens.
/// </remarks>
public readonly record struct GeoPoint(double Latitude, double Longitude);
