using AutoHub.Modules.Listings.Domain.Listings;

namespace AutoHub.Modules.Listings.Application.Abstractions;

/// <summary>
/// How many photos a listing of this shape is expected to carry, and which shots.
/// </summary>
/// <remarks>
/// Every number here comes from the form schema registry's <c>photo_set</c> configuration, which is
/// why "six photos for a car, twelve for a truck, three for a two-wheeler" is a form edit rather
/// than a release — and why the same numbers drive the seller's progress bar, the quality score and
/// the upload limit without any of the three holding its own copy.
/// </remarks>
public sealed record MediaPolicy(int Minimum, int Maximum, IReadOnlyList<string> Prompts);

/// <summary>Resolves the media policy for a listing from its form.</summary>
public interface IMediaPolicy
{
    Task<MediaPolicy> ForAsync(Listing listing, CancellationToken cancellationToken);
}
