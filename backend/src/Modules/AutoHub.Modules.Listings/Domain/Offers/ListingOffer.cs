using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Listings.Domain.Offers;

/// <summary>Which side of the table a turn came from.</summary>
/// <remarks>
/// Two values and no more. A broker, an agent and a family member all act <i>as</i> one of the two
/// parties; modelling them as further roles would mean every guard in the negotiation workflow grows
/// a case, and the question a guard actually asks is only ever "is it this side's turn?".
/// </remarks>
public enum OfferSide
{
    Buyer = 1,
    Seller = 2,
}

/// <summary>
/// One move in a negotiation — an offer, a counter, or a counter to a counter.
/// </summary>
/// <remarks>
/// Append-only. The chain is the audit trail of a price agreement, and in a C2C sale that trail is
/// the evidence when one party later says they never agreed to the number. Editing a turn would
/// destroy exactly the thing it exists for.
/// </remarks>
public sealed class OfferTurn : Entity
{
    private OfferTurn(
        Guid id,
        Guid offerId,
        int sequence,
        OfferSide side,
        Guid partyId,
        long amountMinorUnits,
        string currencyCode,
        string? message,
        DateTimeOffset placedAt)
        : base(id)
    {
        OfferId = offerId;
        Sequence = sequence;
        Side = side;
        PartyId = partyId;
        AmountMinorUnits = amountMinorUnits;
        CurrencyCode = currencyCode;
        Message = message;
        PlacedAt = placedAt;
    }

    private OfferTurn() { }

    public Guid OfferId { get; private set; }

    /// <summary>One-based position in the chain. The first turn is the buyer's opening offer.</summary>
    public int Sequence { get; private set; }

    public OfferSide Side { get; private set; }

    /// <summary>The party that made this move, which is not always the party that owns the side.</summary>
    public Guid PartyId { get; private set; }

    public long AmountMinorUnits { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    /// <summary>
    /// The note attached to the move — "can you include the extended warranty?".
    /// </summary>
    /// <remarks>
    /// Held here rather than in the chat thread because it is part of the offer's record. A message
    /// that lives only in chat is a message that disappears when the thread is archived, and the
    /// reason a buyer dropped their price is exactly what a dispute turns on.
    /// </remarks>
    public string? Message { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public Money Amount => Money.FromMinorUnits(AmountMinorUnits, Currency.FromCode(CurrencyCode));

    internal static OfferTurn Place(
        Guid offerId,
        int sequence,
        OfferSide side,
        Guid partyId,
        Money amount,
        string? message,
        DateTimeOffset placedAt)
        => new(
            UuidV7.New(), offerId, sequence, side, partyId,
            amount.MinorUnits, amount.Currency.Code,
            string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            placedAt);
}

/// <summary>
/// A C2C negotiation over one listing. <b>It has no status column either.</b>
/// </summary>
/// <remarks>
/// Open, countered, accepted, declined, expired and withdrawn are states of the
/// <c>listing_offer</c> workflow, not columns here — for the same reason the listing has none. The
/// negotiation's own rules are guards on that workflow: whether a counter is allowed below the last
/// offer, how long a party has to respond, whether an accepted offer needs a deposit before it
/// creates a deal. Every one of those is something a business person will want to change per city
/// and per intent, and none of them belongs in a compiled <c>if</c>.
/// <para>
/// What the aggregate does own is the invariant no workflow can express: <b>turns alternate</b>.
/// The same side cannot move twice in a row, because a buyer who could would simply bid against
/// themselves down, and a seller who could would ratchet the price after the buyer had committed.
/// </para>
/// </remarks>
public sealed class ListingOffer : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<OfferTurn> _turns = [];

    private ListingOffer(Guid id, Guid listingId, Guid buyerPartyId, Guid sellerPartyId, string currencyCode)
        : base(id)
    {
        ListingId = listingId;
        BuyerPartyId = buyerPartyId;
        SellerPartyId = sellerPartyId;
        CurrencyCode = currencyCode;
    }

    private ListingOffer() { }

    public Guid ListingId { get; private set; }

    public Guid BuyerPartyId { get; private set; }

    /// <summary>
    /// Copied from the listing at creation.
    /// </summary>
    /// <remarks>
    /// Copied rather than joined because a dealership can be sold and its listings reassigned, and
    /// the party a buyer negotiated with is a fact about the negotiation, not about the listing's
    /// current owner.
    /// </remarks>
    public Guid SellerPartyId { get; private set; }

    /// <summary>
    /// Fixed at the opening offer, so a counter cannot silently change currency.
    /// </summary>
    public string CurrencyCode { get; private set; } = string.Empty;

    /// <summary>
    /// There is deliberately no "respond by" column here.
    /// </summary>
    /// <remarks>
    /// How long a party has to answer is a business duration — different per city, per intent and
    /// per campaign — so it lives in the workflow registry as the automatic transition's timeout,
    /// and the instant it resolves to is <c>WorkflowSnapshot.DueAt</c>. Copying it onto this row
    /// would put the same number in two places, and the copy is the one that would still say four
    /// hours after an operator changed it to twenty-four.
    /// </remarks>
    public IReadOnlyList<OfferTurn> Turns => _turns;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public OfferTurn? Latest => _turns.Count == 0 ? null : _turns[^1];

    /// <summary>The amount currently on the table — what an "accept" would agree to.</summary>
    public Money? CurrentAmount => Latest?.Amount;

    public static Result<ListingOffer> Open(
        Guid listingId,
        Guid buyerPartyId,
        Guid sellerPartyId,
        Money amount,
        string? message,
        DateTimeOffset now)
    {
        if (buyerPartyId == Guid.Empty || sellerPartyId == Guid.Empty)
        {
            return Error.Validation("listing.offer.parties_required", "An offer needs a buyer and a seller.");
        }

        if (buyerPartyId == sellerPartyId)
        {
            // Bidding on your own listing is the cheapest way to fake demand, and it is the first
            // thing a fraud reviewer looks for. Refusing it here costs nothing and closes the hole.
            return Error.Validation(
                "listing.offer.self_offer",
                "A seller cannot make an offer on their own listing.");
        }

        if (!amount.IsPositive)
        {
            return Error.Validation("listing.offer.amount_invalid", "An offer must be for a positive amount.")
                .With("amountMinorUnits", amount.MinorUnits);
        }

        var offer = new ListingOffer(UuidV7.New(), listingId, buyerPartyId, sellerPartyId, amount.Currency.Code);

        offer._turns.Add(OfferTurn.Place(offer.Id, 1, OfferSide.Buyer, buyerPartyId, amount, message, now));
        offer.Raise(new OfferPlaced(offer.Id, listingId, buyerPartyId, amount.MinorUnits, amount.Currency.Code, now));

        return offer;
    }

    /// <summary>
    /// Adds a counter from the side whose turn it is.
    /// </summary>
    /// <remarks>
    /// The alternation check is the aggregate's whole reason to exist — see the type's remarks. The
    /// currency check is the other half of it: a counter in a different currency compares two numbers
    /// that are not comparable, and the acceptance that follows would agree to the wrong amount.
    /// </remarks>
    public Result<OfferTurn> Counter(
        OfferSide side,
        Guid partyId,
        Money amount,
        string? message,
        DateTimeOffset now)
    {
        if (Latest is not { } latest)
        {
            return Error.Unexpected("listing.offer.no_turns", "This offer has no opening turn.");
        }

        if (latest.Side == side)
        {
            return Error.Conflict(
                "listing.offer.not_your_turn",
                "The other party has not responded yet.")
                .With("lastSide", side.ToString());
        }

        if (!string.Equals(amount.Currency.Code, CurrencyCode, StringComparison.Ordinal))
        {
            return Error.Validation(
                "listing.offer.currency_mismatch",
                "A counter must be in the same currency as the offer.")
                .With("expected", CurrencyCode)
                .With("submitted", amount.Currency.Code);
        }

        if (!amount.IsPositive)
        {
            return Error.Validation("listing.offer.amount_invalid", "A counter must be for a positive amount.")
                .With("amountMinorUnits", amount.MinorUnits);
        }

        var turn = OfferTurn.Place(Id, latest.Sequence + 1, side, partyId, amount, message, now);
        _turns.Add(turn);

        Raise(new OfferCountered(Id, ListingId, side, amount.MinorUnits, amount.Currency.Code, now));
        return turn;
    }

    /// <summary>Which side a party is on, or null if they are neither.</summary>
    /// <remarks>
    /// Returned rather than asserted so the caller can turn "you are not part of this negotiation"
    /// into a 403 with its own message, instead of this method inventing one.
    /// </remarks>
    public OfferSide? SideOf(Guid partyId)
        => partyId == BuyerPartyId ? OfferSide.Buyer
            : partyId == SellerPartyId ? OfferSide.Seller
            : null;
}
