namespace AutoHub.Modules.Auction.Domain;

/// <summary>
/// The workflow registry key a lot's lifecycle is defined under, and the verbs this module fires.
/// </summary>
/// <remarks>
/// As in the deals module: no state code appears here or anywhere else in the auction module. Which
/// states a lot passes through — open, closed, awaiting payment, paid, forfeited, relisted, and any
/// legal-hold or dispute state an operator inserts later — is rows in
/// <c>platform.workflow_definitions</c>.
/// <para>
/// <b>The timers are the reason this matters more here than elsewhere.</b> A winner's payment window
/// and the forfeiture that follows it are a transition with <c>automaticAfterMinutes</c> set, fired
/// by the workflow scheduler. Written as a background job with a hardcoded duration, "give
/// commercial-vehicle winners 72 hours instead of 24" is a deploy; written as registry data it is a
/// field. The duration itself lives on the auction's settings and is copied onto the lot, so a change
/// never alters the terms of a sale already in flight.
/// </para>
/// </remarks>
public static class AuctionWorkflow
{
    public const string LotKey = "auction_lot";

    public const string LotEntityType = "auction.lot";

    /// <summary>Bidding opens. Fired by the scheduler at the lot's opening time.</summary>
    public const string Open = "open";

    /// <summary>Bidding ends. Fired by the scheduler at the close, or immediately on an instant award.</summary>
    public const string Close = "close";

    /// <summary>The lot met its reserve and has a winner.</summary>
    public const string Award = "award";

    /// <summary>The lot closed without a winner, or below its reserve.</summary>
    public const string PassIn = "pass_in";

    /// <summary>The winner paid inside their window.</summary>
    public const string ConfirmPayment = "confirm_payment";

    /// <summary>The payment window expired. Wired to an automatic transition, never to a button.</summary>
    public const string Forfeit = "forfeit";

    /// <summary>The lot goes round again in a later auction.</summary>
    public const string Relist = "relist";
}
