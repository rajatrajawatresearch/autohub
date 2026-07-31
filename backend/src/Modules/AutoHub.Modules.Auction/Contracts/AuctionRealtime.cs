using System.Globalization;
using System.Text.Json.Nodes;

namespace AutoHub.Modules.Auction.Contracts;

/// <summary>
/// The outbound socket envelope from §11 of the registry API contract.
/// </summary>
/// <remarks>
/// <b>The socket is a read-side broadcast and nothing else.</b> Bids are POSTed to
/// <c>/auction/lots/{id}/bids</c> with an <c>Idempotency-Key</c>; they are never accepted over the
/// socket. A socket carries no status code, no retry semantics and no idempotency, so a dropped
/// frame during a send is indistinguishable from a bid that was never made — and the client's only
/// options are to lose the bid or to send it again and risk two. Over HTTP, a timeout is retried with
/// the same key and the gate returns the original decision.
/// <para>
/// <b><see cref="Seq"/> is the gate's counter, not a per-connection frame number.</b> That is what
/// makes replay work: a client that reconnects sends <c>?sinceSeq=1042</c> and receives exactly the
/// events it missed, in order, with no gap and no duplicate. A per-connection counter would be
/// meaningless to the server after the connection died, and a timestamp cursor would drop or repeat
/// events that share a millisecond — which, on a hot lot in the final seconds, several do.
/// </para>
/// <para>
/// <b>The payload is personalised at send time.</b> <c>highestBidderIsYou</c> and
/// <c>winnerIsYou</c> are different values for different subscribers on the same event, so the event
/// stores the bidder's id and <see cref="ForSubscriber"/> renders the boolean per connection. Sending
/// the id to everyone would deanonymise the room.
/// </para>
/// </remarks>
public sealed record AuctionRealtimeEvent
{
    /// <summary>The gate's monotonic sequence. The replay cursor.</summary>
    public required long Seq { get; init; }

    /// <summary>One of <see cref="AuctionEventTypes"/>.</summary>
    public required string Type { get; init; }

    public required Guid LotId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The event body, minus anything that has to be personalised.</summary>
    public required JsonObject Data { get; init; }

    /// <summary>
    /// The bidder this event is about, if any. Never serialised.
    /// </summary>
    /// <remarks>
    /// Held on the envelope rather than in <see cref="Data"/> precisely so it cannot be published by
    /// accident: <see cref="ForSubscriber"/> is the only thing that reads it, and it emits a boolean.
    /// </remarks>
    public Guid? SubjectBidderId { get; init; }

    /// <summary>
    /// Renders the wire payload for one subscriber.
    /// </summary>
    /// <remarks>
    /// The shape is exactly §11's: <c>{ "seq": 1042, "type": "bid.placed", "data": { … } }</c>. The
    /// contract is the seam the frontend is already built against, so the names here are the names
    /// there, and a C# rename must not be able to change them — which is why they are literals in
    /// this one method rather than a serializer naming policy applied to a record.
    /// </remarks>
    public JsonObject ForSubscriber(Guid? subscriberId)
    {
        var data = Data.DeepClone().AsObject();

        if (SubjectBidderId is { } bidderId)
        {
            var isYou = subscriberId is not null && subscriberId == bidderId;

            data[string.Equals(Type, AuctionEventTypes.LotClosed, StringComparison.Ordinal)
                ? "winnerIsYou"
                : "highestBidderIsYou"] = isYou;
        }

        return new JsonObject
        {
            ["seq"] = Seq,
            ["type"] = Type,
            ["data"] = data,
        };
    }

    public static AuctionRealtimeEvent BidPlaced(
        Guid lotId,
        long seq,
        long amountMinorUnits,
        Guid bidderId,
        DateTimeOffset placedAt,
        bool bidderIdentityVisible,
        bool isReserveMet)
        => new()
        {
            Seq = seq,
            Type = AuctionEventTypes.BidPlaced,
            LotId = lotId,
            OccurredAt = placedAt,
            SubjectBidderId = bidderId,
            Data = new JsonObject
            {
                ["lotId"] = lotId.ToString("D", CultureInfo.InvariantCulture),
                ["amountMinor"] = amountMinorUnits,
                ["bidderMask"] = BidderMask.For(bidderId, bidderIdentityVisible),
                ["placedAt"] = placedAt.ToString("O", CultureInfo.InvariantCulture),
                ["reserveMet"] = isReserveMet,
            },
        };

    public static AuctionRealtimeEvent LotExtended(
        Guid lotId,
        long seq,
        DateTimeOffset endsAt,
        DateTimeOffset occurredAt)
        => new()
        {
            Seq = seq,
            Type = AuctionEventTypes.LotExtended,
            LotId = lotId,
            OccurredAt = occurredAt,
            Data = new JsonObject
            {
                ["endsAt"] = endsAt.ToString("O", CultureInfo.InvariantCulture),
                ["reason"] = AntiSnipeReason,
            },
        };

    public static AuctionRealtimeEvent LotClosed(
        Guid lotId,
        long seq,
        string outcomeCode,
        Guid? winnerId,
        DateTimeOffset occurredAt)
        => new()
        {
            Seq = seq,
            Type = AuctionEventTypes.LotClosed,
            LotId = lotId,
            OccurredAt = occurredAt,
            SubjectBidderId = winnerId,
            Data = new JsonObject
            {
                ["outcome"] = outcomeCode,
            },
        };

    /// <summary>The contract's literal reason string for an anti-snipe extension.</summary>
    private const string AntiSnipeReason = "anti_snipe";
}

/// <summary>The event type names §11 publishes. Wire contract; never renamed.</summary>
public static class AuctionEventTypes
{
    public const string BidPlaced = "bid.placed";

    public const string LotExtended = "lot.extended";

    public const string LotClosed = "lot.closed";
}

/// <summary>
/// The pseudonym a bidder is shown as.
/// </summary>
/// <remarks>
/// <c>Dealer ••42</c> in the contract's own example. Stable per bidder within a lot so the room can
/// follow a contest between two bidders, and revealing nothing that identifies either — dealers bid
/// differently when they know who they are bidding against, and a platform that leaks it is a
/// platform whose auctions get gamed.
/// <para>
/// Derived from the id's own bytes rather than from a counter, because a counter would need shared
/// state on the broadcast path and would renumber on a restart.
/// </para>
/// </remarks>
public static class BidderMask
{
    public static string For(Guid bidderId, bool identityVisible)
    {
        var suffix = bidderId.ToString("N", CultureInfo.InvariantCulture)[^2..].ToUpperInvariant();

        return identityVisible
            ? string.Create(CultureInfo.InvariantCulture, $"Bidder {suffix}")
            : string.Create(CultureInfo.InvariantCulture, $"Bidder ••{suffix}");
    }
}

/// <summary>
/// Publishes to whoever is watching a lot.
/// </summary>
/// <remarks>
/// An interface because the transport is not this module's business, and because the auction module
/// must remain testable without a socket server. The host binds it to SignalR or to a Redis pub/sub
/// fan-out across nodes.
/// </remarks>
public interface IAuctionBroadcaster
{
    Task PublishAsync(AuctionRealtimeEvent realtimeEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Stores broadcast events so a reconnecting client can be given the gap.
/// </summary>
/// <remarks>
/// Separate from the broadcaster because the two have different durability requirements: a dropped
/// broadcast is recovered by replay, and a dropped journal entry is a gap that can never be
/// recovered. The journal is written first for that reason.
/// </remarks>
public interface IAuctionEventJournal
{
    Task AppendAsync(AuctionRealtimeEvent realtimeEvent, CancellationToken cancellationToken);

    /// <summary>Everything after <paramref name="sinceSeq"/>, in order. The <c>?sinceSeq=</c> query.</summary>
    Task<IReadOnlyList<AuctionRealtimeEvent>> ReplayAsync(
        Guid lotId,
        long sinceSeq,
        CancellationToken cancellationToken);
}
