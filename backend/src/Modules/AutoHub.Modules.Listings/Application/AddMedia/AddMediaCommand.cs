using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Listings.Application.AddMedia;

/// <summary>
/// Attaches an already-uploaded object to a listing.
/// </summary>
/// <remarks>
/// <b>The bytes never pass through here.</b> The client uploads directly to the object store against
/// a pre-signed URL and then tells the API which key it wrote. Routing twenty photographs of a truck
/// through an application server would double the bandwidth bill, hold a request thread for the
/// duration of a mobile upload, and put the API's timeout in the middle of a seller's flow on a
/// train.
/// <para>
/// It follows that the key is <i>claimed</i>, not proven, at this point. Virus scanning, dimension
/// extraction and perceptual hashing happen asynchronously against the stored object, and the
/// listing's workflow is what decides whether an unscanned photo may be shown — which is a rule an
/// operator can tighten without a release.
/// </para>
/// </remarks>
public sealed record AddMediaCommand : IRequest<Result<ListingDetail>>
{
    public required Guid ListingId { get; init; }

    /// <summary>Key in the object store the client uploaded to, not a URL.</summary>
    public required string ObjectKey { get; init; }

    /// <summary>Taxonomy option value — <c>photo</c>, <c>video</c>, <c>document</c>, <c>360</c>.</summary>
    public required string MediaTypeCode { get; init; }

    /// <summary>Which prompt from the form's photo set this shot answers, if any.</summary>
    public string? PromptCode { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    /// <summary>
    /// Content hash of the uploaded bytes.
    /// </summary>
    /// <remarks>
    /// Optional because a client on a slow phone should not be made to hash a 12-megapixel image
    /// before it can attach it. When present it deduplicates the upload and feeds fraud detection;
    /// when absent the asynchronous pipeline fills it in.
    /// </remarks>
    public string? ContentHash { get; init; }

    /// <summary>Makes this the cover after attaching. Ignored when it is the first media item.</summary>
    public bool MakeCover { get; init; }
}
