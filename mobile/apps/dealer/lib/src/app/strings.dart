import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// Every key this app renders a word for.
///
/// Keys, not sentences. A screen names `DealerText.placeBid` and never types "Place bid", so a
/// content person rewording the confirmation flow — or a translator publishing `mr-IN` — changes a
/// registry row and every handset picks it up on the next launch. `01-design-system.md` §7 owns the
/// register these are written in: a knowledgeable dealer, not a salesperson. Say what happens.
abstract final class DealerText {
  /* ── Shell ─────────────────────────────────────────────────────────────── */
  static const String navDashboard = 'dealer.nav.dashboard';
  static const String navInventory = 'dealer.nav.inventory';
  static const String navAuction = 'dealer.nav.auction';
  static const String navLeads = 'dealer.nav.leads';
  static const String navWallet = 'dealer.nav.wallet';

  /* ── Common ────────────────────────────────────────────────────────────── */
  static const String retry = 'common.action.retry';
  static const String refresh = 'common.action.refresh';
  static const String cancel = 'common.action.cancel';
  static const String loadMore = 'common.action.load_more';
  static const String close = 'common.action.close';
  static const String copyCode = 'common.action.copy_code';
  static const String copied = 'common.status.copied';
  static const String supportCode = 'common.label.support_code';
  static const String showDetails = 'common.action.show_details';
  static const String somethingFailed = 'common.error.generic';
  static const String offlineTitle = 'common.error.offline';

  /* ── Dashboard ─────────────────────────────────────────────────────────── */
  static const String dashboardEyebrow = 'dealer.dashboard.eyebrow';
  static const String dashboardTitle = 'dealer.dashboard.title';
  static const String dashboardDescription = 'dealer.dashboard.description';
  static const String stockValue = 'dealer.dashboard.stock_value';
  static const String vehiclesInStock = 'dealer.dashboard.vehicles_in_stock';
  static const String ageing = 'dealer.dashboard.ageing';
  static const String ageingHint = 'dealer.dashboard.ageing_hint';
  static const String liveLots = 'dealer.dashboard.live_lots';
  static const String payoutDue = 'dealer.dashboard.payout_due';
  static const String payoutDueHint = 'dealer.dashboard.payout_due_hint';
  static const String needsYouToday = 'dealer.dashboard.needs_you';
  static const String settlement = 'dealer.dashboard.settlement';
  static const String settlementCycle = 'dealer.settlement.cycle';
  static const String settlementCycleValue = 'dealer.settlement.cycle_value';
  static const String settlementCutOff = 'dealer.settlement.cut_off';
  static const String settlementCutOffValue = 'dealer.settlement.cut_off_value';
  static const String commissionTerms = 'dealer.settlement.commission';
  static const String commissionTermsValue = 'dealer.settlement.commission_value';
  static const String commissionTermsHint = 'dealer.settlement.commission_hint';
  static const String noStockTitle = 'dealer.inventory.empty_title';
  static const String noStockBody = 'dealer.inventory.empty_body';
  static const String noLotsTitle = 'dealer.auction.empty_title';
  static const String noLotsBody = 'dealer.auction.empty_body';

  /* ── Inventory ─────────────────────────────────────────────────────────── */
  static const String inventoryEyebrow = 'dealer.inventory.eyebrow';
  static const String inventoryTitle = 'dealer.inventory.title';
  static const String inventoryDescription = 'dealer.inventory.description';
  static const String filterPlaceholder = 'dealer.inventory.filter_placeholder';
  static const String sortNewest = 'dealer.inventory.sort_newest';
  static const String sortOldest = 'dealer.inventory.sort_oldest';
  static const String sortAskHigh = 'dealer.inventory.sort_ask_high';
  static const String sortAskLow = 'dealer.inventory.sort_ask_low';
  static const String sortLeads = 'dealer.inventory.sort_leads';
  static const String colAge = 'dealer.inventory.col_age';
  static const String colAsk = 'dealer.inventory.col_ask';
  static const String colLeads = 'dealer.inventory.col_leads';
  static const String onRequest = 'listing.price.on_request';
  static const String daysInStock = 'dealer.inventory.days_unit';
  static const String noMatchTitle = 'dealer.inventory.no_match_title';
  static const String noMatchBody = 'dealer.inventory.no_match_body';
  static const String inventoryFailedTitle = 'dealer.inventory.failed_title';
  static const String inventoryFailedBody = 'dealer.inventory.failed_body';

  /* ── Stock detail ──────────────────────────────────────────────────────── */
  static const String askingPrice = 'listing.label.asking_price';
  static const String views = 'listing.label.views';
  static const String city = 'listing.label.city';
  static const String listedOn = 'listing.label.listed_on';
  static const String history = 'dealer.stock.history';
  static const String historyDescription = 'dealer.stock.history_description';
  static const String historyEmpty = 'dealer.stock.history_empty';
  static const String availableNow = 'dealer.stock.available_now';
  static const String availableNowHint = 'dealer.stock.available_now_hint';
  static const String noActions = 'dealer.stock.no_actions';
  static const String actionReason = 'dealer.stock.action_reason';
  static const String actionConfirm = 'dealer.stock.action_confirm';
  static const String actionQueued = 'dealer.stock.action_queued';
  static const String actionBlocked = 'dealer.stock.action_blocked';
  static const String stockFailedTitle = 'dealer.stock.failed_title';

  /* ── Auction ───────────────────────────────────────────────────────────── */
  static const String auctionEyebrow = 'dealer.auction.eyebrow';
  static const String auctionTitle = 'dealer.auction.title';
  static const String auctionDescription = 'dealer.auction.description';
  static const String openLot = 'dealer.auction.open_lot';
  static const String reserveMet = 'dealer.auction.reserve_met';
  static const String reserveNotMet = 'dealer.auction.reserve_not_met';
  static const String lotNumber = 'dealer.auction.lot_number';
  static const String lotTerms = 'dealer.auction.lot_terms';
  static const String startPrice = 'dealer.auction.start_price';
  static const String increment = 'dealer.auction.increment';
  static const String incrementHint = 'dealer.auction.increment_hint';
  static const String reserve = 'dealer.auction.reserve';
  static const String reserveHint = 'dealer.auction.reserve_hint';
  static const String bidsSoFar = 'dealer.auction.bids_so_far';
  static const String extensions = 'dealer.auction.extensions';
  static const String extensionsHint = 'dealer.auction.extensions_hint';
  static const String inspection = 'dealer.auction.inspection';
  static const String inspectionOpen = 'dealer.auction.inspection_open';
  static const String inspectionNone = 'dealer.auction.inspection_none';
  static const String bidLadder = 'dealer.auction.bid_ladder';
  static const String noBidsTitle = 'dealer.auction.no_bids_title';
  static const String noBidsBody = 'dealer.auction.no_bids_body';
  static const String bindingNotice = 'dealer.auction.binding_notice';
  static const String feedLive = 'dealer.auction.feed_live';
  static const String feedConnecting = 'dealer.auction.feed_connecting';
  static const String feedOffline = 'dealer.auction.feed_offline';
  static const String lotFailedTitle = 'dealer.auction.lot_failed_title';
  static const String lotFailedBody = 'dealer.auction.lot_failed_body';

  /* ── Bidding ───────────────────────────────────────────────────────────── */
  static const String currentBid = 'auction.bid.current';
  static const String yourMax = 'auction.bid.your_max';
  static const String leading = 'auction.bid.leading';
  static const String outbid = 'auction.bid.outbid';
  static const String timeLeft = 'auction.lot.time_left';
  static const String lotClosedLabel = 'auction.lot.closed';
  static const String lotExtended = 'auction.lot.extended';
  static const String yourBid = 'auction.bid.your_bid';
  static const String minimumNextBid = 'auction.bid.minimum_next';
  static const String ladderStep = 'auction.bid.ladder_step';
  static const String belowLadder = 'auction.bid.below_ladder';
  static const String placeBid = 'auction.bid.place';
  static const String confirmBid = 'auction.bid.confirm';
  static const String confirmBidQuestion = 'auction.bid.confirm_question';
  static const String confirmBidTerms = 'auction.bid.confirm_terms';
  static const String bidPlaced = 'auction.bid.placed';
  static const String bidClosed = 'auction.bid.lot_closed';
  static const String bidSending = 'auction.bid.sending';

  /* ── Bid failures, keyed by the server's machine code ──────────────────── */
  static const String errOutbidRace = 'error.auction.bid.outbid_race';
  static const String errBelowIncrement = 'error.auction.bid.below_increment';
  static const String errLotClosed = 'error.auction.lot.closed';
  static const String errNotVerified = 'error.auction.bidder.not_verified';
  static const String errBidUnsent = 'error.auction.bid.unsent';
  static const String errBidGeneric = 'error.auction.bid.generic';
  static const String errSignedOut = 'error.identity.token.expired';

  /* ── Leads ─────────────────────────────────────────────────────────────── */
  static const String leadsEyebrow = 'dealer.leads.eyebrow';
  static const String leadsTitle = 'dealer.leads.title';
  static const String leadsDescription = 'dealer.leads.description';
  static const String noPipelineTitle = 'dealer.leads.no_pipeline_title';
  static const String noPipelineBody = 'dealer.leads.no_pipeline_body';
  static const String stageEmpty = 'dealer.leads.stage_empty';

  /* ── Wallet ────────────────────────────────────────────────────────────── */
  static const String walletEyebrow = 'dealer.wallet.eyebrow';
  static const String walletTitle = 'dealer.wallet.title';
  static const String walletDescription = 'dealer.wallet.description';
  static const String settled = 'dealer.wallet.settled';
  static const String inFlight = 'dealer.wallet.in_flight';
  static const String dealCount = 'dealer.wallet.deal_count';
  static const String recentDeals = 'dealer.wallet.recent_deals';
  static const String commissionLines = 'dealer.wallet.commission_lines';
  static const String commissionLinesHint = 'dealer.wallet.commission_lines_hint';
  static const String totalFees = 'dealer.wallet.total_fees';
  static const String payable = 'dealer.wallet.payable';
  static const String escrowHeld = 'dealer.wallet.escrow_held';
  static const String noDealsTitle = 'dealer.wallet.empty_title';
  static const String noDealsBody = 'dealer.wallet.empty_body';
  static const String noFeeLines = 'dealer.wallet.no_fee_lines';
}

/// The `en-IN` copy this binary ships with, so a first launch on a dead network still reads as
/// English rather than as a list of dotted keys.
///
/// `AhStrings` on its own falls back to the key, and its reasoning is sound for a *missing*
/// translation — `listing.cta.get_valuation` on a button is ugly and immediately diagnosable, which
/// beats a blank button. It is the wrong answer for a cold start, though: a dealer who opens the
/// app in a basement before any registry has ever been fetched would get a screen of identifiers.
///
/// So the resolution order is registry → this map → the key, and every default lives here rather
/// than at a call site. A translator publishing `dealer.*` in the i18n registry replaces all of it
/// at once, with no screen edited and nothing rebuilt — which is the same claim the sell form makes
/// about fields, applied to words.
const Map<String, String> kDealerEnIn = <String, String>{
  DealerText.navDashboard: 'Dashboard',
  DealerText.navInventory: 'Inventory',
  DealerText.navAuction: 'Auction',
  DealerText.navLeads: 'Leads',
  DealerText.navWallet: 'Wallet',

  DealerText.retry: 'Retry',
  DealerText.refresh: 'Refresh',
  DealerText.cancel: 'Cancel',
  DealerText.loadMore: 'Load more',
  DealerText.close: 'Close',
  DealerText.copyCode: 'Copy code',
  DealerText.copied: 'Copied',
  DealerText.supportCode: 'Give this to support',
  DealerText.showDetails: 'Details',
  DealerText.somethingFailed: 'That did not go through.',
  DealerText.offlineTitle: 'No connection',

  DealerText.dashboardEyebrow: 'Today',
  DealerText.dashboardTitle: 'Dashboard',
  DealerText.dashboardDescription: 'Stock, demand and money for this branch.',
  DealerText.stockValue: 'Stock value',
  DealerText.vehiclesInStock: 'In stock',
  DealerText.ageing: 'Ageing',
  DealerText.ageingHint: 'Days in stock',
  DealerText.liveLots: 'Live lots',
  DealerText.payoutDue: 'Payout due',
  DealerText.payoutDueHint: 'Deals sold and not yet settled',
  DealerText.needsYouToday: 'Needs you today',
  DealerText.settlement: 'Settlement',
  DealerText.settlementCycle: 'Cycle',
  DealerText.settlementCycleValue: 'T+2 working days',
  DealerText.settlementCutOff: 'Cut-off',
  DealerText.settlementCutOffValue: '18:00 IST',
  DealerText.commissionTerms: 'Commission',
  DealerText.commissionTermsValue: 'From the pricing registry',
  DealerText.commissionTermsHint: 'Quoted on every deal before you accept it, never applied after.',
  DealerText.noStockTitle: 'No stock in this branch',
  DealerText.noStockBody: 'Add a vehicle, or bulk upload your existing stock.',
  DealerText.noLotsTitle: 'No lots are open',
  DealerText.noLotsBody: 'The next session opens when stock is released for auction.',

  DealerText.inventoryEyebrow: 'Stock',
  DealerText.inventoryTitle: 'Inventory',
  DealerText.inventoryDescription: 'Every vehicle here, and what it costs you to hold it.',
  DealerText.filterPlaceholder: 'Plate or model',
  DealerText.sortNewest: 'Newest in',
  DealerText.sortOldest: 'Oldest in',
  DealerText.sortAskHigh: 'Ask high to low',
  DealerText.sortAskLow: 'Ask low to high',
  DealerText.sortLeads: 'Most leads',
  DealerText.colAge: 'Age',
  DealerText.colAsk: 'Ask',
  DealerText.colLeads: 'Leads',
  DealerText.onRequest: 'On request',
  DealerText.daysInStock: 'd',
  DealerText.noMatchTitle: 'Nothing matches that',
  DealerText.noMatchBody: 'Check the plate, or try part of the model name.',
  DealerText.inventoryFailedTitle: 'Inventory did not load',
  DealerText.inventoryFailedBody: 'Nothing has changed on your stock. Retry.',

  DealerText.askingPrice: 'Asking price',
  DealerText.views: 'Views',
  DealerText.city: 'City',
  DealerText.listedOn: 'Listed',
  DealerText.history: 'History',
  DealerText.historyDescription: 'Every state change, who made it, and how long it sat.',
  DealerText.historyEmpty: 'Nothing has happened to this vehicle yet.',
  DealerText.availableNow: 'Available now',
  DealerText.availableNowHint: 'What you can do to this vehicle right now.',
  DealerText.noActions: 'Nothing to do here right now.',
  DealerText.actionReason: 'Reason',
  DealerText.actionConfirm: 'Confirm',
  DealerText.actionQueued: 'Queued. It will land when you are back on signal.',
  DealerText.actionBlocked: 'This vehicle moved on. Refreshed to what the server has.',
  DealerText.stockFailedTitle: 'That vehicle did not load',

  DealerText.auctionEyebrow: 'Trade',
  DealerText.auctionTitle: 'Auction floor',
  DealerText.auctionDescription:
      'Bids are binding. A bid in the last minutes moves the close time.',
  DealerText.openLot: 'Open lot',
  DealerText.reserveMet: 'Reserve met',
  DealerText.reserveNotMet: 'Reserve not met',
  DealerText.lotNumber: 'Lot',
  DealerText.lotTerms: 'Lot terms',
  DealerText.startPrice: 'Start price',
  DealerText.increment: 'Increment',
  DealerText.incrementHint: 'A bid must clear the current high by at least this.',
  DealerText.reserve: 'Reserve',
  DealerText.reserveHint: 'The reserve amount itself is never shown to bidders.',
  DealerText.bidsSoFar: 'Bids',
  DealerText.extensions: 'Extensions',
  DealerText.extensionsHint: 'A bid inside the final window pushes the close time out.',
  DealerText.inspection: 'Inspection',
  DealerText.inspectionOpen: 'Report available',
  DealerText.inspectionNone: 'Not available',
  DealerText.bidLadder: 'Bid ladder',
  DealerText.noBidsTitle: 'No bids yet',
  DealerText.noBidsBody: 'The ladder starts at the start price.',
  DealerText.bindingNotice:
      'Winning opens a deal with a payment window. Missing it forfeits the deposit and the lot is '
          'relisted. The server clock is authoritative — your screen may be a second behind it.',
  DealerText.feedLive: 'Live',
  DealerText.feedConnecting: 'Connecting',
  DealerText.feedOffline: 'Not live — pull to refresh',
  DealerText.lotFailedTitle: 'This lot did not load',
  DealerText.lotFailedBody: 'Do not bid from a stale screen. Retry and check the clock.',

  DealerText.currentBid: 'Current bid',
  DealerText.yourMax: 'Your maximum',
  DealerText.leading: 'You lead',
  DealerText.outbid: 'Outbid',
  DealerText.timeLeft: 'Time left',
  DealerText.lotClosedLabel: 'Closed',
  DealerText.lotExtended: 'Extended — a bid in the last minutes moves the close',
  DealerText.yourBid: 'Your bid',
  DealerText.minimumNextBid: 'Minimum next bid',
  DealerText.ladderStep: 'Ladder step',
  DealerText.belowLadder: 'Below the ladder. Raise it to the minimum shown.',
  DealerText.placeBid: 'Place bid',
  DealerText.confirmBid: 'Confirm bid',
  DealerText.confirmBidQuestion: 'Place a binding bid of',
  DealerText.confirmBidTerms:
      'Winning opens a deal with a payment window. Missing it forfeits your deposit.',
  DealerText.bidPlaced: 'Bid placed',
  DealerText.bidClosed: 'This lot has closed. Results are on the deals page.',
  DealerText.bidSending: 'Placing…',

  DealerText.errOutbidRace:
      'Someone bid while you were typing. The figure above is now current — bid again if you still '
          'want it.',
  DealerText.errBelowIncrement: 'That is under the ladder step. Raise it to the minimum shown.',
  DealerText.errLotClosed: 'This lot closed before the bid landed.',
  DealerText.errNotVerified: 'Your dealer KYC is not complete, so bidding is off.',
  DealerText.errBidUnsent: 'No signal — the bid was not placed. The lot is still open.',
  DealerText.errBidGeneric: 'The bid did not go through. Check the current figure before trying '
      'again.',
  DealerText.errSignedOut: 'Your session expired. Sign in and place the bid again.',

  DealerText.leadsEyebrow: 'Trade',
  DealerText.leadsTitle: 'Leads',
  DealerText.leadsDescription: 'Stages come from the deal workflow, so this matches how you sell.',
  DealerText.noPipelineTitle: 'No pipeline configured',
  DealerText.noPipelineBody: 'The deal workflow has not been published yet.',
  DealerText.stageEmpty: 'Nothing here',

  DealerText.walletEyebrow: 'Money',
  DealerText.walletTitle: 'Wallet',
  DealerText.walletDescription: 'Balances, commission lines and payouts for this branch.',
  DealerText.settled: 'Settled',
  DealerText.inFlight: 'In flight',
  DealerText.dealCount: 'Deals',
  DealerText.recentDeals: 'Recent deals',
  DealerText.commissionLines: 'Commission lines',
  DealerText.commissionLinesHint: 'Every rupee, with the rule that produced it.',
  DealerText.totalFees: 'Total fees',
  DealerText.payable: 'Payable',
  DealerText.escrowHeld: 'Held in escrow',
  DealerText.noDealsTitle: 'No deals yet',
  DealerText.noDealsBody: 'They appear the moment an offer is accepted or a lot is won.',
  DealerText.noFeeLines: 'No fee lines on this deal.',
};

/// Resolves a key to the word a dealer reads.
///
/// Deliberately callable, so a screen reads `t(DealerText.placeBid)` rather than
/// `strings.resolve(...)` — short enough that nobody is tempted to inline a literal instead.
@immutable
final class DealerStrings {
  const DealerStrings(this.registry);

  /// The bundle from `GET /platform/i18n/{locale}`, with its `en-IN` fallback already composed by
  /// `RegistryReads.strings`. Null before the first fetch on a device that has never been online.
  final AhStrings? registry;

  String get locale => registry?.locale ?? AhStrings.fallbackLocale;

  String call(String key) {
    final bundle = registry;
    if (bundle != null && bundle.has(key)) return bundle(key);
    return kDealerEnIn[key] ?? key;
  }

  /// For keys this binary cannot enumerate — `listing.badge.{code}`, where the code is a registry
  /// row and the label belongs to the localisation registry.
  ///
  /// The caller supplies something readable rather than letting the key itself reach the screen: a
  /// badge reading `listing.badge.verified_rc` is diagnosable but useless on a card, and the code
  /// with its underscores replaced is at least the word the server was trying to say.
  String or(String key, String fallback) {
    final bundle = registry;
    if (bundle != null && bundle.has(key)) return bundle(key);
    return kDealerEnIn[key] ?? fallback;
  }
}
