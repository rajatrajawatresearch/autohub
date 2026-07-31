using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Modules.Listings.Domain.Offers;

namespace AutoHub.Modules.Listings.Application.Abstractions;

/// <summary>
/// Reads and writes listings.
/// </summary>
/// <remarks>
/// Declared here and implemented in the persistence layer, so this project never references EF Core
/// and the handlers are testable against a list. It is also the reason extraction is a build-file
/// change: the implementation moves with the schema and nothing in <c>Application</c> or
/// <c>Domain</c> is touched.
/// <para>
/// There is deliberately no <c>GetByStatusAsync</c> and no status parameter anywhere on this
/// interface. Asking "which listings are live?" is a question for the workflow engine's instance
/// table joined on entity id, not for a column here that would have to be kept in step with it.
/// </para>
/// </remarks>
public interface IListingRepository
{
    Task<Listing?> GetAsync(Guid listingId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Listing>> GetManyAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken);

    Task AddAsync(Listing listing, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Reads and writes C2C negotiations.</summary>
/// <remarks>
/// Separate from <see cref="IListingRepository"/> because an offer is its own aggregate with its own
/// workflow and its own concurrency. Loading a listing to add a counter-offer would take a write
/// lock on a row every buyer on the page is reading.
/// </remarks>
public interface IListingOfferRepository
{
    Task<ListingOffer?> GetAsync(Guid offerId, CancellationToken cancellationToken);

    /// <summary>
    /// The open negotiation between this buyer and this listing, if one exists.
    /// </summary>
    /// <remarks>
    /// Used to stop a buyer opening a second negotiation on a listing they are already negotiating.
    /// Two parallel chains on one listing means two prices a seller can accept, and the second
    /// acceptance is a deal on a car that has already been sold.
    /// <para>
    /// "Open" is not a column. The implementation joins the offer to its workflow instance and takes
    /// the ones the engine has not completed — <c>completed_at IS NULL</c> — which is the same fact
    /// the engine itself uses, read from the same row rather than copied into a second one that
    /// would eventually disagree with it.
    /// </para>
    /// </remarks>
    Task<ListingOffer?> FindOpenAsync(Guid listingId, Guid buyerPartyId, CancellationToken cancellationToken);

    Task AddAsync(ListingOffer offer, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
