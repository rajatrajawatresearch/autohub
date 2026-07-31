using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Platform.Taxonomy;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Listings.Application.GetListing;

public sealed record GetListingQuery(Guid ListingId) : IRequest<Result<ListingDetail>>;

/// <summary>One media item as the gallery renders it.</summary>
/// <remarks>
/// <see cref="ObjectKey"/> and not a URL, for the reason argued on <see cref="ListingMedia"/>: the
/// CDN host and the signing policy are edge concerns and change without the media changing.
/// </remarks>
public sealed record ListingMediaView(
    Guid MediaId,
    string ObjectKey,
    string MediaTypeCode,
    string? PromptCode,
    bool IsCover,
    int SortOrder,
    double? AspectRatio);

/// <summary>
/// One row of the contract's <c>specStrip</c>.
/// </summary>
/// <remarks>
/// <b>Order and content come from the taxonomy registry</b>, exactly as §9 says — the strip is the
/// category's attributes in their authored sort order, so adding <c>payload_kg</c> to Commercial
/// puts it on the card with no code change.
/// <para>
/// <see cref="Value"/> is the datum, not a formatted string: <c>38400</c>, never "38,400". Indian
/// digit grouping is locale-dependent and the server does not know the viewer's locale, so grouping
/// happens at the edge — the same rule that keeps money in minor units. <see cref="Unit"/> travels
/// separately so the client can render "38,400 km" or "38.4k km" as the space allows.
/// </para>
/// </remarks>
public sealed record SpecStripItem(string Code, string Label, string Value, string? Unit);

/// <summary>
/// A listing with everything a detail page needs, and no status anywhere on it.
/// </summary>
/// <remarks>
/// <see cref="Workflow"/> is where the state lives, and it is a snapshot resolved for
/// <b>this caller</b>: the actions listed are the ones this user may actually fire, with guards and
/// permissions already evaluated server-side. A client that decided for itself which buttons to show
/// would show "Approve" to a customer and collect a 403 for it.
/// </remarks>
public sealed record ListingDetail
{
    public required ListingSummary Listing { get; init; }

    /// <summary>The seller's headline if they wrote one, otherwise the vehicle's composed title.</summary>
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required WorkflowSnapshot Workflow { get; init; }

    public required ListingQuality Quality { get; init; }

    public required IReadOnlyList<ListingMediaView> Media { get; init; }

    public required IReadOnlyList<ResolvedAttribute> Attributes { get; init; }

    public required IReadOnlyList<SpecStripItem> SpecStrip { get; init; }

    /// <summary>
    /// The canonical vehicle, or null when the catalog could not answer.
    /// </summary>
    /// <remarks>
    /// Nullable rather than fatal. After extraction this is a network call, and a listing page that
    /// returns 500 because the catalog service is slow is a worse outcome than a listing page with
    /// the plate missing — the price, the photos and the seller are all still there and still useful.
    /// </remarks>
    public VehicleSummary? Vehicle { get; init; }
}
