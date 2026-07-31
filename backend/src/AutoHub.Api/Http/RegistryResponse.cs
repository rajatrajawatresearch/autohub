using System.Globalization;
using AutoHub.Platform.Caching;
using AutoHub.SharedKernel.Results;
using Microsoft.Net.Http.Headers;

namespace AutoHub.Api.Http;

/// <summary>
/// The caching envelope every registry response wears.
/// </summary>
/// <remarks>
/// §0 of the contract fixes three headers and one behaviour: an <c>ETag</c> of
/// <c>"taxonomy:v41"</c>, a <c>Cache-Control</c> with a stale-while-revalidate window, an
/// <c>X-Registry-Version</c>, and a <c>304</c> when the client sends a matching
/// <c>If-None-Match</c>. Written once here because a registry endpoint that forgets one of them is a
/// Flutter app re-downloading the whole taxonomy on every cold start over a 3G connection.
/// <para>
/// The version is the registry's cache generation, not a timestamp and not a content hash. That is
/// what makes the ETag and the server-side cache key the same fact: they cannot drift, so "the admin
/// changed it, the web app updated, the mobile app did not" is not a state this system can reach.
/// </para>
/// </remarks>
internal static class RegistryResponse
{
    /// <summary>
    /// One minute fresh, ten minutes stale-while-revalidate.
    /// </summary>
    /// <remarks>
    /// The freshness window is the honest cost of the version-bump design: after a publish, a client
    /// may serve the old snapshot for up to this long. The revalidate window is what keeps a CDN
    /// serving instantly through the refetch instead of stalling every user behind one origin miss.
    /// Both are technical cache parameters — the prime directive's explicit exemption.
    /// </remarks>
    private const string CachePolicy = "public, max-age=60, stale-while-revalidate=600";

    public const string VersionHeader = "X-Registry-Version";

    /// <summary>Serialises <paramref name="payload"/> under the caching envelope, or answers 304.</summary>
    public static IResult Json<T>(HttpContext context, RegistryVersion version, T payload)
        => Negotiate(context, version) ?? Microsoft.AspNetCore.Http.Results.Json(payload);

    /// <summary>
    /// Serves a body that is already JSON.
    /// </summary>
    /// <remarks>
    /// Used by the form registry, whose document is produced by <c>FormJson</c> with its own
    /// serializer options — null-omitting, because the renderer treats a present-but-null
    /// <c>options</c> as an empty dropdown. Round-tripping it through the host's serializer would
    /// quietly reintroduce the nulls.
    /// </remarks>
    public static IResult RawJson(HttpContext context, RegistryVersion version, string json)
        => Negotiate(context, version)
           ?? Microsoft.AspNetCore.Http.Results.Text(json, "application/json", System.Text.Encoding.UTF8);

    /// <summary>
    /// Maps a registry <see cref="Result{T}"/> straight to a response.
    /// </summary>
    /// <remarks>
    /// The version is read off the payload rather than fetched separately, so the ETag always
    /// describes the bytes being sent. Fetching it again would open a window in which a publish
    /// lands between the two reads and stamps a new version on an old body — a poisoned cache entry
    /// that only expires on its own.
    /// </remarks>
    public static IResult From<T>(
        HttpContext context,
        Result<T> result,
        string registry,
        Func<T, long> versionOf)
        => result.IsSuccess
            ? Json(context, new RegistryVersion(registry, versionOf(result.Value)), result.Value)
            : ApiProblem.Result(result.Error, context);

    /// <summary>
    /// Writes the headers, and answers <c>304</c> when the client already holds this version.
    /// </summary>
    /// <remarks>
    /// Returns null to mean "carry on and write the body", which reads oddly and is the shape that
    /// keeps the header-writing in one place. The headers are set before the 304 check because a
    /// 304 must carry them too — a conditional response without an ETag is a response the client
    /// cannot revalidate against next time.
    /// </remarks>
    private static IResult? Negotiate(HttpContext context, RegistryVersion version)
    {
        var etag = version.ToETag();

        var headers = context.Response.Headers;
        headers[HeaderNames.ETag] = etag;
        headers[HeaderNames.CacheControl] = CachePolicy;
        headers[VersionHeader] = version.Value.ToString(CultureInfo.InvariantCulture);

        // Vary is not optional here. The i18n and features endpoints answer differently per locale
        // and per caller; without it a shared cache serves one user's Marathi bundle to everyone.
        headers.Append(HeaderNames.Vary, HeaderNames.AcceptLanguage);

        var inbound = context.Request.Headers.IfNoneMatch;

        foreach (var candidate in inbound)
        {
            if (candidate is null)
            {
                continue;
            }

            // A comma-separated list is legal, and a proxy may have weakened the tag to W/"...".
            foreach (var tag in candidate.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var normalised = tag.StartsWith("W/", StringComparison.Ordinal) ? tag[2..] : tag;

                if (string.Equals(normalised, etag, StringComparison.Ordinal) || tag == "*")
                {
                    return Microsoft.AspNetCore.Http.Results.StatusCode(StatusCodes.Status304NotModified);
                }
            }
        }

        return null;
    }
}
