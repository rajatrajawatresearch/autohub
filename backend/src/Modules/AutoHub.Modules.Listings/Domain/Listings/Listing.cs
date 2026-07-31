using System.Text.Json.Nodes;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Listings.Domain.Listings;

/// <summary>
/// An intent to transact a canonical vehicle. <b>It has no status column.</b>
/// </summary>
/// <remarks>
/// <b>The absence of a status is the single most important thing in this module.</b> Where a listing
/// sits in its lifecycle lives in the workflow registry — <c>platform.workflow_instances</c> — and is
/// read through <c>IWorkflowEngine</c>. Nothing here compares against <c>draft</c>,
/// <c>pending_review</c>, <c>live</c>, <c>sold</c> or <c>rejected</c>, because those are rows an
/// operator edits and not vocabulary a compiler should know.
/// <para>
/// The temptation to add one is constant and every argument for it is plausible: "just for the
/// search index", "just for the admin list", "just to avoid a join". Each of them creates a second
/// source of truth for the one fact that changes most often in the product, and the two sources
/// disagree the first time a transition fails halfway. Adding a state to the lifecycle would then
/// mean a migration, a backfill, a search reindex, and finding every <c>switch</c> — which is
/// precisely the cost this architecture was built to avoid. The join is cheap; the divergence is not.
/// </para>
/// <para>
/// What lives here instead is everything that is <i>true of the offer</i> regardless of where it sits:
/// the vehicle, the intent, the asking price, the seller, the city, the media, the pinned form
/// version and the quality score.
/// </para>
/// <para>
/// <b>Why the form version is pinned.</b> A listing created against v7 of the sell form must keep
/// rendering and re-validating against v7 forever. Without the pin, publishing v8 would silently
/// invalidate every draft in flight — every seller who left the tab open comes back to a form
/// complaining about a field that did not exist when they started.
/// </para>
/// </remarks>
public sealed class Listing : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<ListingMedia> _media = [];

    private Listing(
        Guid id,
        Guid vehicleId,
        Guid categoryId,
        ListingIntent intent,
        Guid sellerPartyId,
        string cityCode,
        long priceMinorUnits,
        string priceCurrencyCode)
        : base(id)
    {
        VehicleId = vehicleId;
        CategoryId = categoryId;
        Intent = intent;
        SellerPartyId = sellerPartyId;
        CityCode = cityCode;
        PriceMinorUnits = priceMinorUnits;
        PriceCurrencyCode = priceCurrencyCode;
        QualityScore = 0;
    }

    private Listing() { }

    /// <summary>The canonical vehicle. Resolved through the catalog, never created here.</summary>
    public Guid VehicleId { get; private set; }

    /// <summary>
    /// Copied from the vehicle at creation, and deliberately not re-read.
    /// </summary>
    /// <remarks>
    /// Denormalised because every attribute validation and every facet build needs it, and because
    /// a listing's attributes were validated against the category the listing was created in.
    /// Re-reading the vehicle's current category would mean a recategorisation elsewhere silently
    /// invalidates a live listing's stored values.
    /// </remarks>
    public Guid CategoryId { get; private set; }

    public ListingIntent Intent { get; private set; }

    /// <summary>
    /// Who is offering. An identity-module party id, not a user id.
    /// </summary>
    /// <remarks>
    /// A party because a dealer's listing outlives the salesperson who created it — keyed to a user,
    /// the whole inventory would need reassigning every time somebody left.
    /// </remarks>
    public Guid SellerPartyId { get; private set; }

    /// <summary>Geography registry city code. Scopes search, pricing and feature evaluation.</summary>
    public string CityCode { get; private set; } = string.Empty;

    /// <summary>
    /// Asking price in minor units. For auction it is the reserve; for rental and lease it is a rate.
    /// </summary>
    /// <remarks>
    /// Stored as a <c>bigint</c> and a currency code rather than through a value converter, so both
    /// halves of an amount are columns a reader can see in psql and neither can be materialised
    /// against the wrong currency. <see cref="Price"/> rebuilds the <c>Money</c> from the pair, which
    /// is the same shape <c>EntityAttributeValue</c> uses for exactly the same reason.
    /// </remarks>
    public long PriceMinorUnits { get; private set; }

    public string PriceCurrencyCode { get; private set; } = string.Empty;

    /// <summary>Rate period for rental, lease and attachment — <c>per_day</c>, <c>per_month</c>.</summary>
    /// <remarks>
    /// Null for an outright sale, and a taxonomy option value rather than an enum so a weekly rate
    /// or a per-kilometre attachment rate is data. Whether an intent requires one is a workflow guard,
    /// not a check here.
    /// </remarks>
    public string? PricePeriodCode { get; private set; }

    /// <summary>Seller-authored headline. Null until the seller writes one; never auto-generated.</summary>
    /// <remarks>
    /// A generated title reads as a generated title, and the listing card already composes
    /// year + make + model from the catalog. This is the sentence a seller adds on top of that.
    /// </remarks>
    public string? Title { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Form registry key the seller filled in, e.g. <c>sell_vehicle</c>.</summary>
    public string FormKey { get; private set; } = string.Empty;

    /// <summary>The exact form version. See the type's remarks on why it is pinned.</summary>
    public int FormVersion { get; private set; }

    public int QualityScore { get; private set; }

    /// <summary>The signal breakdown behind <see cref="QualityScore"/>, as a JSON array.</summary>
    public string? QualitySignalsJson { get; private set; }

    /// <summary>
    /// The denormalised attribute bag, rebuilt whenever the EAV rows are written.
    /// </summary>
    /// <remarks>
    /// Same argument as on the vehicle: the EAV rows are the truth and are what range facets index,
    /// while this column serves containment queries and the API response without a join per
    /// attribute.
    /// </remarks>
    public string? AttributesJson { get; private set; }

    public IReadOnlyList<ListingMedia> Media => _media;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public Money Price => Money.FromMinorUnits(PriceMinorUnits, Currency.FromCode(PriceCurrencyCode));

    public ListingQuality Quality => ListingQuality.FromStorage(QualityScore, QualitySignalsJson);

    public ListingMedia? Cover => _media.Find(m => m.IsCover);

    public static Result<Listing> Draft(
        Guid vehicleId,
        Guid categoryId,
        ListingIntent intent,
        Guid sellerPartyId,
        string cityCode,
        Money price,
        string formKey,
        int formVersion,
        DateTimeOffset now)
    {
        if (sellerPartyId == Guid.Empty)
        {
            return Error.Validation("listing.seller_required", "A listing needs a seller.");
        }

        if (string.IsNullOrWhiteSpace(cityCode))
        {
            return Error.Validation("listing.city_required", "A listing needs a city.");
        }

        // Zero is allowed and negative is not. "Price on application" is a real listing and is
        // expressed as zero plus a workflow guard; a negative asking price is a client bug that
        // would flow straight into a commission calculation.
        if (price.IsNegative)
        {
            return Error.Validation("listing.price_negative", "A listing price cannot be negative.")
                .With("priceMinorUnits", price.MinorUnits);
        }

        var listing = new Listing(
            UuidV7.New(), vehicleId, categoryId, intent, sellerPartyId,
            cityCode.Trim(), price.MinorUnits, price.Currency.Code)
        {
            FormKey = formKey,
            FormVersion = formVersion,
        };

        listing.Raise(new ListingCreated(listing.Id, vehicleId, intent, sellerPartyId, now));
        return listing;
    }

    public Result Reprice(Money price, string? pricePeriodCode, DateTimeOffset now)
    {
        if (price.IsNegative)
        {
            return Error.Validation("listing.price_negative", "A listing price cannot be negative.")
                .With("priceMinorUnits", price.MinorUnits);
        }

        if (price.MinorUnits == PriceMinorUnits
            && string.Equals(price.Currency.Code, PriceCurrencyCode, StringComparison.Ordinal)
            && string.Equals(pricePeriodCode, PricePeriodCode, StringComparison.Ordinal))
        {
            // A no-op reprice must not emit an event. Buyers get a "price dropped" notification from
            // this stream, and a nightly dealer feed that resends the same price would otherwise
            // notify every watcher every night until they mute the listing.
            return Result.Success();
        }

        var previous = PriceMinorUnits;

        PriceMinorUnits = price.MinorUnits;
        PriceCurrencyCode = price.Currency.Code;
        PricePeriodCode = pricePeriodCode;

        Raise(new ListingRepriced(Id, previous, price.MinorUnits, price.Currency.Code, now));
        return Result.Success();
    }

    public void Describe(string? title, string? description)
    {
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Attaches media, deduplicating by content hash and making the first photo the cover.
    /// </summary>
    /// <remarks>
    /// Deduplication by hash rather than by object key: a seller who uploads the same photo twice
    /// gets two keys and one image, and a gallery that shows it twice looks like a mistake the
    /// seller made. The first photo becomes the cover because a listing with no cover renders a grey
    /// box, and asking the seller to pick one before they have uploaded anything is a dialog nobody
    /// completes.
    /// </remarks>
    public Result<ListingMedia> AttachMedia(
        string objectKey,
        string mediaTypeCode,
        string? promptCode,
        int? width,
        int? height,
        string? contentHash,
        int maxMedia,
        DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(contentHash))
        {
            var duplicate = _media.Find(m =>
                string.Equals(m.ContentHash, contentHash, StringComparison.Ordinal));

            if (duplicate is not null)
            {
                return duplicate;
            }
        }

        if (_media.Count >= maxMedia)
        {
            return Error.Validation(
                "listing.media.too_many",
                $"A listing can carry at most {maxMedia} media items.")
                .With("max", maxMedia);
        }

        var created = ListingMedia.Create(
            Id, objectKey, mediaTypeCode, promptCode, _media.Count, width, height, contentHash);

        if (created.IsFailure)
        {
            return created;
        }

        var media = created.Value;

        if (_media.Count == 0)
        {
            media.MakeCover(true);
        }

        _media.Add(media);
        Raise(new ListingMediaAttached(Id, media.Id, now));

        return media;
    }

    /// <summary>Moves the cover, unsetting whichever item held it.</summary>
    public Result SetCover(Guid mediaId)
    {
        var target = _media.Find(m => m.Id == mediaId);

        if (target is null)
        {
            return Error.NotFound("listing.media.not_found", "That media item is not on this listing.")
                .With("mediaId", mediaId);
        }

        foreach (var media in _media)
        {
            media.MakeCover(media.Id == mediaId);
        }

        return Result.Success();
    }

    /// <summary>
    /// Records that the listing went public.
    /// </summary>
    /// <remarks>
    /// Called by the workflow effect wired to whichever transition means "on the market", never by a
    /// handler that decided for itself. It exists as a method — rather than the effect reaching for
    /// <c>Raise</c> — because raising an event is the aggregate's business, and an aggregate whose
    /// events can be raised from outside has no control over its own history.
    /// <para>
    /// It sets no field, and that is the point: publication is a workflow state, not a column.
    /// </para>
    /// </remarks>
    public void MarkPublished(DateTimeOffset now)
        => Raise(new ListingPublished(Id, VehicleId, Intent, CityCode, now));

    public void Score(ListingQuality quality)
    {
        QualityScore = quality.Score;
        QualitySignalsJson = quality.ToJson();
    }

    /// <summary>Replaces the denormalised attribute bag after the EAV rows have been written.</summary>
    public void ProjectAttributes(JsonObject attributes) => AttributesJson = attributes.ToJsonString();

    public JsonObject Attributes()
        => string.IsNullOrWhiteSpace(AttributesJson)
            ? []
            : JsonNode.Parse(AttributesJson) as JsonObject ?? [];

    public ListingSummary ToSummary() => new()
    {
        ListingId = Id,
        VehicleId = VehicleId,
        Intent = Intent,
        SellerPartyId = SellerPartyId,
        PriceMinorUnits = PriceMinorUnits,
        PriceCurrency = PriceCurrencyCode,
        PricePeriodCode = PricePeriodCode,
        CityCode = CityCode,
        QualityScore = QualityScore,
        MediaCount = _media.Count,
        CoverMediaKey = Cover?.ObjectKey,
    };
}
