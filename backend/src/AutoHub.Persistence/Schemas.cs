namespace AutoHub.Persistence;

/// <summary>
/// The PostgreSQL schema each module's tables live in.
/// </summary>
/// <remarks>
/// These are technical names, not business vocabulary — they carry no rule, no label and no
/// percentage, and no operator will ever want one changed. The prime directive's exemption for
/// "cache keys, column names" is exactly this category.
/// <para>
/// The value of naming them here rather than typing the string into forty configurations is that a
/// module's schema becomes greppable: "what is in <c>auction</c>?" is a reference search, and a
/// module extracted to its own service is a search-and-delete of one constant rather than a hunt
/// through string literals.
/// </para>
/// </remarks>
public static class Schemas
{
    /// <summary>The ten registries, the outbox and the idempotency ledger.</summary>
    public const string Platform = "platform";

    public const string Identity = "identity";

    public const string Catalog = "catalog";

    public const string Listings = "listings";

    public const string Auction = "auction";

    public const string Rental = "rental";

    public const string Lease = "lease";

    public const string Attachment = "attachment";

    public const string Valuation = "valuation";

    public const string Inspection = "inspection";

    public const string Deals = "deals";

    public const string Finance = "finance";

    public const string Insurance = "insurance";

    public const string Payments = "payments";

    public const string Chat = "chat";

    public const string Booking = "booking";

    public const string Documents = "documents";

    public const string Notification = "notification";

    public const string Review = "review";

    public const string Trust = "trust";

    public const string Analytics = "analytics";

    /// <summary>Every schema the context owns. Used to create them on first migration.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Platform, Identity, Catalog, Listings, Auction, Rental, Lease, Attachment,
        Valuation, Inspection, Deals, Finance, Insurance, Payments, Chat, Booking,
        Documents, Notification, Review, Trust, Analytics,
    ];
}
