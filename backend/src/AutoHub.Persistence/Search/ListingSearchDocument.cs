using AutoHub.Modules.Listings.Contracts;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Persistence.Search;

/// <summary>
/// The PostgreSQL search index: one row per publicly visible listing.
/// </summary>
/// <remarks>
/// <b>Why a projection table and not a query over <c>listings</c>.</b> Two reasons, and the first is
/// architectural. Searching the listings table directly would mean deciding <i>in code</i> which
/// workflow states count as publicly visible — reading a state category and interpreting it — which
/// is the status-string comparison this whole module refuses to contain. A projection populated by a
/// workflow <i>effect</i> moves that decision into the registry: an operator wires
/// <c>listings.search_index.publish</c> to whichever transition means "on the market", and the index
/// contains exactly the listings that transition has fired for.
/// <para>
/// The second reason is ordinary: search reads a denormalised row and writes happen once per
/// lifecycle change, which is the right side of that trade for the hottest read path in the product.
/// </para>
/// <para>
/// This type lives in the persistence layer rather than in the listings module because it is not a
/// domain concept — it is one implementation of <c>ISearchIndex</c>, and the OpenSearch adapter that
/// eventually replaces it will not have a table at all.
/// </para>
/// </remarks>
public sealed class ListingSearchDocument
{
    private ListingSearchDocument(Guid listingId) => ListingId = listingId;

    private ListingSearchDocument() { }

    /// <summary>The listing this document projects. Also the primary key — one row per listing.</summary>
    public Guid ListingId { get; private set; }

    public Guid VehicleId { get; private set; }

    public Guid CategoryId { get; private set; }

    public ListingIntent Intent { get; private set; }

    public long PriceMinorUnits { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string CityCode { get; private set; } = string.Empty;

    public int QualityScore { get; private set; }

    public int MediaCount { get; private set; }

    public string? CoverObjectKey { get; private set; }

    public double? CoverAspectRatio { get; private set; }

    /// <summary>
    /// The free-text haystack: title, description and the vehicle's plate, concatenated.
    /// </summary>
    /// <remarks>
    /// Concatenated at index time rather than searched across three columns, so the trigram index
    /// covers one column instead of three and a match on any of them is one index scan. It is not a
    /// <c>tsvector</c>: the queries this serves are substring and plate lookups, where trigram
    /// similarity behaves better than stemmed full text on registration numbers and model names.
    /// </remarks>
    public string SearchText { get; private set; } = string.Empty;

    /// <summary>
    /// Odometer reading, promoted out of the attribute bag because it is a sort key.
    /// </summary>
    /// <remarks>
    /// The contract publishes <c>sort=kms_asc</c>, and a sort cannot run through the EAV join without
    /// materialising every matching row first. One nullable column on the projection is the cheap
    /// answer; the EAV row remains the truth and still answers the range filter.
    /// </remarks>
    public long? KilometresDriven { get; private set; }

    public DateTimeOffset IndexedAt { get; private set; }

    /// <summary>Row identity for the outbox and for auditing, distinct from the listing's id.</summary>
    public Guid Id { get; private set; } = UuidV7.New();

    public static ListingSearchDocument For(Guid listingId) => new(listingId);

    public void Populate(
        Guid vehicleId,
        Guid categoryId,
        ListingIntent intent,
        long priceMinorUnits,
        string currency,
        string cityCode,
        int qualityScore,
        int mediaCount,
        string? coverObjectKey,
        double? coverAspectRatio,
        string searchText,
        long? kilometresDriven,
        DateTimeOffset indexedAt)
    {
        VehicleId = vehicleId;
        CategoryId = categoryId;
        Intent = intent;
        PriceMinorUnits = priceMinorUnits;
        Currency = currency;
        CityCode = cityCode;
        QualityScore = qualityScore;
        MediaCount = mediaCount;
        CoverObjectKey = coverObjectKey;
        CoverAspectRatio = coverAspectRatio;
        SearchText = searchText;
        KilometresDriven = kilometresDriven;
        IndexedAt = indexedAt;
    }
}
