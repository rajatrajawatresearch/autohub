using AutoHub.Platform.Access;

namespace AutoHub.Modules.Listings.Infrastructure;

/// <summary>
/// The permissions the listings module checks.
/// </summary>
/// <remarks>
/// These names appear on workflow transitions as <c>requiredPermission</c>, which is where they are
/// actually enforced — the engine refuses a transition whose permission the actor does not hold,
/// once, for every caller, rather than each endpoint remembering to check.
/// <para>
/// Note what is <b>not</b> here: there is no <c>listing.publish</c>. Publishing is whichever
/// transition an operator wires to the effect that indexes the listing, and its permission is chosen
/// in the registry — which is exactly why "a listing goes live after moderation in Pune and
/// immediately in Nashik" needs no code.
/// </para>
/// </remarks>
public static class ListingsPermissions
{
    /// <summary>Creating a listing. Held by every verified individual and by dealer staff.</summary>
    public const string ListingCreate = "listing.create";

    /// <summary>Editing a listing's price, description and media. Scoped to the seller's own party.</summary>
    public const string ListingEdit = "listing.edit";

    /// <summary>
    /// Approving, rejecting and holding listings.
    /// </summary>
    /// <remarks>
    /// The permission a "Regional Auction Moderator — West" holds, scoped to four cities. Dangerous
    /// because a moderator can put any listing in front of, or hide it from, the whole market.
    /// </remarks>
    public const string ListingModerate = "listing.moderate";

    /// <summary>Making and answering C2C offers. Separated from editing so a buyer can hold it.</summary>
    public const string OfferParticipate = "listing.offer.participate";
}

/// <summary>Declares <see cref="ListingsPermissions"/> to the RBAC catalog.</summary>
public sealed class ListingsPermissionContributor : IPermissionContributor
{
    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        PermissionDefinition.Declare(
            ListingsPermissions.ListingCreate, "Create listings",
            description: "Offer a vehicle for sale, auction, rental, lease or attachment."),

        PermissionDefinition.Declare(
            ListingsPermissions.ListingEdit, "Edit listings",
            description: "Change a listing's price, description and media."),

        PermissionDefinition.Declare(
            ListingsPermissions.ListingModerate, "Moderate listings", isDangerous: true,
            description: "Approve, reject or hold any listing in scope."),

        PermissionDefinition.Declare(
            ListingsPermissions.OfferParticipate, "Make and answer offers",
            description: "Take part in a C2C price negotiation."),
    ];
}
