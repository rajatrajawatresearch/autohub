import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_dealer/src/features/auction/lot_controller.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures.dart';

/// `seenBidSeq` is the number that decides whether a dealer's bid is accepted at the price they
/// read or rejected as a race. Every case below is a way of getting it wrong that would cost money.
void main() {
  group('foldAuctionEvent', () {
    test('a bid moves the figure and seenBidSeq together', () {
      final before = testView(lot: testLot(latestSeq: 1042, currentBidMinor: 84500000));

      final after = foldAuctionEvent(
        before,
        const AuctionEvent.bidPlaced(
          seq: 1043,
          data: BidPlacedEvent(
            lotId: 'lot-1',
            amountMinor: 85000000,
            placedAt: '2026-07-31T09:30:00Z',
            minNextBidMinor: 85500000,
            bidCount: 4,
          ),
        ),
      );

      expect(after, isNotNull);
      expect(after!.lot.currentBidMinor, 85000000);
      expect(after.lot.minNextBidMinor, 85500000);
      expect(after.lot.bidCount, 4);
      // The guard and the figure it describes are one state transition. If these two could ever
      // disagree, a dealer could bid against a price they had not seen.
      expect(after.seenBidSeq, 1043);
      expect(after.lot.latestSeq, 1043);
    });

    test('an event this build cannot interpret does NOT advance seenBidSeq', () {
      final before = testView(lot: testLot(latestSeq: 1042));

      // A frame from a newer server: the type is unknown, but freezed still yields its sequence so
      // the reconnect anchor can advance.
      final event = AuctionEvent.fromJson(<String, Object?>{
        'seq': 1043,
        'type': 'bid.retracted',
        'data': <String, Object?>{'lotId': 'lot-1'},
      });
      expect(event, isA<AuctionUnknownEvent>());
      expect(event.seq, 1043);

      final after = foldAuctionEvent(before, event);

      // Nothing was rendered, so the dealer has seen nothing new. The next bid therefore carries
      // 1042, the server reads it as stale, and it comes back as an outbid race — which is exactly
      // right. Advancing to 1043 here would let them commit money against a change they never saw.
      expect(after, isNull);
      expect(before.seenBidSeq, 1042);
    });

    test('a replayed frame after a reconnect is ignored', () {
      final before = testView(lot: testLot(latestSeq: 1042), seenBidSeq: 1042);

      final after = foldAuctionEvent(
        before,
        const AuctionEvent.bidPlaced(
          seq: 1042,
          data: BidPlacedEvent(
            lotId: 'lot-1',
            amountMinor: 1,
            placedAt: '2026-07-31T09:29:00Z',
          ),
        ),
      );

      expect(after, isNull, reason: 'a replay must not move seenBidSeq backwards or re-render');
    });

    test('a bid without minNextBidMinor falls back to the lot\'s own increment', () {
      final before = testView(
        lot: testLot(latestSeq: 1042, incrementMinor: 500000, bidCount: 3),
      );

      final after = foldAuctionEvent(
        before,
        const AuctionEvent.bidPlaced(
          seq: 1043,
          data: BidPlacedEvent(
            lotId: 'lot-1',
            amountMinor: 85000000,
            placedAt: '2026-07-31T09:30:00Z',
          ),
        ),
      );

      expect(after!.lot.minNextBidMinor, 85000000 + 500000);
      expect(after.lot.bidCount, 4, reason: 'an omitted count increments by exactly one');
    });

    test('an anti-snipe extension moves the close time and counts itself', () {
      final before = testView(lot: testLot(latestSeq: 1042, extensionCount: 0));

      final after = foldAuctionEvent(
        before,
        const AuctionEvent.lotExtended(
          seq: 1043,
          data: LotExtendedEvent(endsAt: '2026-07-31T10:02:00Z', reason: 'anti_snipe'),
        ),
      );

      expect(after!.lot.endsAt, '2026-07-31T10:02:00Z');
      expect(after.lot.extensionCount, 1);
      expect(after.seenBidSeq, 1043);
    });

    test('a close stops bidding without inventing a workflow state', () {
      final before = testView(lot: testLot(latestSeq: 1042));

      final after = foldAuctionEvent(
        before,
        const AuctionEvent.lotClosed(
          seq: 1043,
          data: LotClosedEvent(outcome: 'sold', winnerIsYou: true),
        ),
      );

      expect(after!.isClosed, isTrue);
      // The words belong to the workflow registry. A console that fabricated "Sold" here would
      // disagree with the deals screen about the same lot the moment operations renamed the state.
      expect(after.lot.state.code, 'live');
      expect(after.lot.state.name, 'Live');
    });
  });
}
