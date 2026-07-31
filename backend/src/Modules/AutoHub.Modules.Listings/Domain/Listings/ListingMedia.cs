using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Listings.Domain.Listings;

/// <summary>
/// One photo, video or document attached to a listing.
/// </summary>
/// <remarks>
/// <b>An object-store key, never a URL.</b> The CDN host changes, the signing policy changes, the
/// rendition suffix changes, and a stored URL bakes today's answer to all three into a row that will
/// outlive them. The resolvable URL is built at the edge, where the viewer's device, connection and
/// authentication are known — which is also the only place that can decide whether to serve AVIF.
/// <para>
/// <see cref="ContentHash"/> is what makes the same photo uploaded twice one row instead of two, and
/// it is also the first signal trust uses: the identical image appearing under two sellers in two
/// cities is a scraped listing, and it is the single most common fraud pattern on Indian classifieds.
/// </para>
/// </remarks>
public sealed class ListingMedia : Entity, IAuditable
{
    private ListingMedia(Guid id, Guid listingId, string objectKey, string mediaTypeCode)
        : base(id)
    {
        ListingId = listingId;
        ObjectKey = objectKey;
        MediaTypeCode = mediaTypeCode;
    }

    private ListingMedia() { }

    public Guid ListingId { get; private set; }

    /// <summary>Key in the object store, e.g. <c>listings/019fab.../front-34.avif</c>.</summary>
    public string ObjectKey { get; private set; } = string.Empty;

    /// <summary>
    /// Taxonomy option value — <c>photo</c>, <c>video</c>, <c>document</c>, <c>360</c>.
    /// </summary>
    /// <remarks>
    /// A code rather than an enum, following <c>VehicleVariant.FuelCode</c>. A 360-degree spin and an
    /// AR walkaround both arrived after any enum a developer would have written, and each would have
    /// been a migration; here each is an insert.
    /// </remarks>
    public string MediaTypeCode { get; private set; } = string.Empty;

    /// <summary>
    /// Which shot this is — <c>front_34</c>, <c>odometer</c>, <c>engine_bay</c>.
    /// </summary>
    /// <remarks>
    /// Comes from the form registry's <c>photo_set</c> prompts, so the set of expected shots is
    /// editable per category without a release. Null for an unprompted upload, which is allowed:
    /// refusing a photo because it does not match a prompt is how a seller gives up.
    /// </remarks>
    public string? PromptCode { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>
    /// The one image the card, the share preview and the search result use.
    /// </summary>
    /// <remarks>
    /// Exactly one per listing, enforced by <see cref="Listing.SetCover"/> rather than by a unique
    /// index, because "the cover moved" is one operation and two rows changing — a partial unique
    /// index would reject the intermediate state of any reordering that did not happen to unset
    /// first.
    /// </remarks>
    public bool IsCover { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    /// <summary>
    /// Perceptual or content hash of the bytes. Deduplicates uploads and feeds fraud detection.
    /// </summary>
    public string? ContentHash { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    internal static Result<ListingMedia> Create(
        Guid listingId,
        string objectKey,
        string mediaTypeCode,
        string? promptCode,
        int sortOrder,
        int? width,
        int? height,
        string? contentHash)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return Error.Validation("listing.media.object_key_required", "Media needs an object-store key.");
        }

        if (string.IsNullOrWhiteSpace(mediaTypeCode))
        {
            return Error.Validation("listing.media.type_required", "Media needs a type.");
        }

        return new ListingMedia(UuidV7.New(), listingId, objectKey.Trim(), mediaTypeCode.Trim())
        {
            PromptCode = promptCode,
            SortOrder = sortOrder,
            Width = width,
            Height = height,
            ContentHash = contentHash,
        };
    }

    internal void MakeCover(bool isCover) => IsCover = isCover;

    internal void Reorder(int sortOrder) => SortOrder = sortOrder;

    /// <summary>
    /// Aspect ratio, for the layout to reserve space before the image arrives.
    /// </summary>
    /// <remarks>
    /// Sent rather than computed on the client because the client does not know the dimensions until
    /// the image has loaded, and reserving no space is what produces the layout shift that makes a
    /// gallery feel broken on a slow connection.
    /// </remarks>
    public double? AspectRatio
        => Width is > 0 && Height is > 0 ? (double)Width.Value / Height.Value : null;
}
