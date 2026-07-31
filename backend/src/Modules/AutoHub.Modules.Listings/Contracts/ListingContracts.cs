using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Listings.Contracts;

/// <summary>
/// The transaction shape a listing offers over a vehicle.
/// </summary>
/// <remarks>
/// This is the north star's branch point: one vehicle, many intents. Sale, auction, rental, lease,
/// attachment and exchange are not six modules with six tables — they are six values of this enum
/// over one listing row, one workflow definition and one deal spine. Adding "spare parts" or "driver
/// marketplace" in v2 is a value here plus a workflow, not a vertical codebase.
/// <para>
/// It is an enum rather than a taxonomy code, and that is a deliberate exception to the prime
/// directive. Each value has a workflow definition, a pricing scope and a module behind it — a
/// seventh intent is a new set of those, which is code by definition. What the directive forbids is
/// business <i>vocabulary</i> in code: statuses, fuel types, cities. A branch in the domain model is
/// not vocabulary.
/// </para>
/// </remarks>
public enum ListingIntent
{
    /// <summary>Outright sale, C2C or from a dealer's stock.</summary>
    Sale = 1,

    /// <summary>Consigned to a dealer auction. Price is a reserve, not an ask.</summary>
    Auction = 2,

    /// <summary>Self-drive or chauffeured rental. Price is a rate over a period.</summary>
    Rental = 3,

    /// <summary>Long-term lease. Price is a monthly rental with a term and a residual.</summary>
    Lease = 4,

    /// <summary>Attached to a taxi or fleet operator. Price is the operator's periodic payment.</summary>
    Attachment = 5,

    /// <summary>Offered in part-exchange against another vehicle.</summary>
    Exchange = 6,
}

/// <summary>
/// The published spelling of <see cref="ListingIntent"/>.
/// </summary>
/// <remarks>
/// Written down rather than derived, for the same reason <c>TaxonomyWireNames</c> is: the contract
/// fixes <c>intent=sale</c> in a query string that three clients already build, and a C# rename must
/// break the build rather than the apps in the field.
/// </remarks>
public static class ListingIntentWireNames
{
    public static string ToWireName(this ListingIntent intent) => intent switch
    {
        ListingIntent.Sale => "sale",
        ListingIntent.Auction => "auction",
        ListingIntent.Rental => "rental",
        ListingIntent.Lease => "lease",
        ListingIntent.Attachment => "attachment",
        ListingIntent.Exchange => "exchange",
        _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "Unmapped listing intent."),
    };

    public static Result<ListingIntent> Parse(string? wireName) => wireName switch
    {
        "sale" => ListingIntent.Sale,
        "auction" => ListingIntent.Auction,
        "rental" => ListingIntent.Rental,
        "lease" => ListingIntent.Lease,
        "attachment" => ListingIntent.Attachment,
        "exchange" => ListingIntent.Exchange,
        _ => Error.Validation("listing.intent_unknown", $"'{wireName}' is not a listing intent.")
            .With("intent", wireName),
    };
}

/// <summary>
/// The entity-type keys this module owns in the platform's shared tables.
/// </summary>
/// <remarks>
/// Technical keys, in the prime directive's stated exemption. They appear in the EAV table, in
/// workflow instances and in cache keys, and a typo in any of them returns an empty result rather
/// than an error — which is exactly the class of bug a constant prevents.
/// </remarks>
public static class ListingEntityTypes
{
    public const string Listing = "listings.listing";

    public const string Offer = "listings.offer";
}

/// <summary>
/// The workflow definition keys this module starts instances of.
/// </summary>
/// <remarks>
/// <b>The key is code; every state behind it is data.</b> That distinction is the whole architecture
/// of this module. Nothing here names <c>draft</c>, <c>pending_review</c>, <c>live</c> or
/// <c>sold</c>, because those are rows an operator edits — inserting a "Legal Hold" state before
/// payout must not be a deployment. What is compiled in is only the name of the machine to start,
/// which is the one thing an operator will never want changed.
/// </remarks>
public static class ListingWorkflows
{
    public const string Listing = "listing";

    public const string Offer = "listing_offer";
}

/// <summary>Everything another module is allowed to know about a listing.</summary>
/// <remarks>
/// There is no status on this record, and its absence is the point. A caller that wants to know
/// where a listing sits asks the workflow engine, which evaluates the guards and permissions for
/// <i>that</i> caller and returns the actions they may actually fire. A status copied onto a
/// projection is a status that goes stale between the copy and the read.
/// </remarks>
public sealed record ListingSummary
{
    public required Guid ListingId { get; init; }

    public required Guid VehicleId { get; init; }

    public required ListingIntent Intent { get; init; }

    public required Guid SellerPartyId { get; init; }

    public required long PriceMinorUnits { get; init; }

    public required string PriceCurrency { get; init; }

    /// <summary>Rate period for rental, lease and attachment — <c>per_day</c>, <c>per_month</c>.</summary>
    public string? PricePeriodCode { get; init; }

    public required string CityCode { get; init; }

    public required int QualityScore { get; init; }

    public required int MediaCount { get; init; }

    public string? CoverMediaKey { get; init; }
}

/// <summary>
/// The read side of listings, for modules that must not reference its domain.
/// </summary>
/// <remarks>
/// Auction needs a lot's listing, Deals needs the seller and the price, Chat needs the thread's
/// subject. None of them may join to <c>listings.listings</c>, because after extraction there is no
/// table on the far side of that join.
/// </remarks>
public interface IListingDirectory
{
    Task<Result<ListingSummary>> GetAsync(Guid listingId, CancellationToken cancellationToken);

    /// <summary>Batched, because the caller is a list page or a settlement run.</summary>
    Task<Result<IReadOnlyDictionary<Guid, ListingSummary>>> GetManyAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken);
}
