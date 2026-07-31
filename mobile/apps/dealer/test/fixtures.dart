import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_dealer/src/data/auction_feed.dart';
import 'package:autohub_dealer/src/features/auction/lot_controller.dart';

/// A lot with every figure named, so a test that changes one is obviously about that one.
AuctionLot testLot({
  String lotId = 'lot-1',
  int? currentBidMinor = 84500000,
  int minNextBidMinor = 85000000,
  int incrementMinor = 500000,
  int bidCount = 3,
  int latestSeq = 1042,
  bool highestBidderIsYou = false,
  int extensionCount = 0,
  String endsAt = '2026-07-31T10:00:00Z',
  String stateCode = 'live',
  String stateCategory = 'in_progress',
  bool isTerminal = false,
}) {
  return AuctionLot(
    lotId: lotId,
    listing: const SearchItem(
      listingId: 'listing-1',
      registration: Registration(
        canonical: 'MH12AB1234',
        display: 'MH 12 AB 1234',
        plateClass: 'commercial',
      ),
      title: '2021 Tata Ace Gold',
    ),
    state: WorkflowState.fromJson(<String, Object?>{
      'code': stateCode,
      'name': 'Live',
      'category': stateCategory,
      'isTerminal': isTerminal,
    }),
    startsAt: '2026-07-31T09:00:00Z',
    endsAt: endsAt,
    startPriceMinor: 80000000,
    incrementMinor: incrementMinor,
    currentBidMinor: currentBidMinor,
    minNextBidMinor: minNextBidMinor,
    bidCount: bidCount,
    latestSeq: latestSeq,
    highestBidderIsYou: highestBidderIsYou,
    extensionCount: extensionCount,
  );
}

/// A rendered view of [testLot], with `seenBidSeq` matching what is on screen — which is the
/// invariant every auction test is really about.
LotView testView({AuctionLot? lot, int? seenBidSeq, AhServerClock? clock}) {
  final resolved = lot ?? testLot();
  return LotView(
    lot: resolved,
    clock: clock ?? AhServerClock.unsynchronised,
    seenBidSeq: seenBidSeq ?? resolved.latestSeq,
    feed: AuctionFeedStatus.open,
  );
}

/// A deal summary, for the wallet's ledger arithmetic.
DealJson testDeal({
  required String category,
  required int amountMinor,
  String code = 'settled',
  bool isTerminal = false,
}) =>
    DealJson(<String, Object?>{
      'dealId': 'deal-$code-$amountMinor',
      'reference': 'AH-$amountMinor',
      'listing': <String, Object?>{
        'listingId': 'listing-1',
        'registration': <String, Object?>{'canonical': 'MH12AB1234', 'display': 'MH 12 AB 1234'},
        'title': '2021 Tata Ace Gold',
      },
      'amountMinor': amountMinor,
      'state': <String, Object?>{
        'code': code,
        'name': code,
        'category': category,
        'isTerminal': isTerminal,
      },
    });

/// A raw deal object, kept as JSON so the tests exercise the real decoder rather than a
/// hand-constructed model that could not have come off the wire.
extension type DealJson(Map<String, Object?> json) {}
