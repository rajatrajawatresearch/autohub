import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_dealer/src/data/bid_client.dart';
import 'package:flutter_test/flutter_test.dart';

/// Records what a bid actually put on the wire, and answers with whatever the test asked for.
final class _RecordingTransport implements OutboxTransport {
  _RecordingTransport(this.outcome);

  final WriteOutcome outcome;
  final List<WriteIntent> sent = <WriteIntent>[];

  @override
  Future<WriteOutcome> send(WriteIntent intent) async {
    sent.add(intent);
    return outcome;
  }
}

Map<String, Object?> _lotJson({int latestSeq = 1043, int currentBidMinor = 85000000}) =>
    <String, Object?>{
      'lotId': 'lot-1',
      'listing': <String, Object?>{
        'listingId': 'listing-1',
        'registration': <String, Object?>{'canonical': 'MH12AB1234', 'display': 'MH 12 AB 1234'},
        'title': '2021 Tata Ace Gold',
      },
      'state': <String, Object?>{'code': 'live', 'name': 'Live', 'category': 'in_progress'},
      'startsAt': '2026-07-31T09:00:00Z',
      'endsAt': '2026-07-31T10:00:00Z',
      'startPriceMinor': 80000000,
      'incrementMinor': 500000,
      'currentBidMinor': currentBidMinor,
      'minNextBidMinor': currentBidMinor + 500000,
      'latestSeq': latestSeq,
    };

void main() {
  group('BidClient', () {
    test('sends amount and seenBidSeq, and never queues', () async {
      final transport = _RecordingTransport(
        WriteAccepted(
          statusCode: 201,
          body: <String, dynamic>{
            'bid': <String, Object?>{
              'bidId': 'bid-1',
              'lotId': 'lot-1',
              'seq': 1043,
              'amountMinor': 85000000,
              'placedAt': '2026-07-31T09:30:00Z',
            },
            'lot': _lotJson(),
          },
        ),
      );

      final result = await BidClient(ImmediateWriter(transport)).place(
        lotId: 'lot-1',
        amountMinor: 85000000,
        seenBidSeq: 1042,
        activeCity: 'pune',
      );

      expect(result, isA<BidPlaced>());
      expect(transport.sent, hasLength(1));

      final intent = transport.sent.single;
      expect(intent.endpoint, '/auction/lots/lot-1/bids');
      expect(intent.method, WriteMethod.post);
      expect(intent.body?['amountMinor'], 85000000);
      // §6.2: the sequence the screen had rendered travels with every bid.
      expect(intent.body?['seenBidSeq'], 1042);
      // §4: a bid is never queued. `immediate` is what `ImmediateWriter.sendStrict` demands and
      // what the outbox refuses to accept.
      expect(intent.durability, WriteDurability.immediate);
      expect(intent.isQueueable, isFalse);
      // Minted on the gesture, so a bouncing thumb over showroom wifi replays into one bid.
      expect(intent.idempotencyKey, isNotEmpty);
      // Frozen at the gesture: the bid belongs to the city it was made from.
      expect(intent.headers['X-Active-City'], 'pune');
    });

    test('installs the server\'s lot rather than the amount that was sent', () async {
      // The server accepted a *different* figure from the one composed — a proxy bid stepped in.
      // Nothing about the response may be inferred from the request.
      final transport = _RecordingTransport(
        WriteAccepted(
          statusCode: 201,
          body: <String, dynamic>{
            'bid': <String, Object?>{
              'bidId': 'bid-1',
              'lotId': 'lot-1',
              'seq': 1044,
              'amountMinor': 86000000,
              'placedAt': '2026-07-31T09:30:00Z',
            },
            'lot': _lotJson(latestSeq: 1044, currentBidMinor: 86000000),
          },
        ),
      );

      final result = await BidClient(ImmediateWriter(transport)).place(
        lotId: 'lot-1',
        amountMinor: 85000000,
        seenBidSeq: 1042,
      );

      expect(result, isA<BidPlaced>());
      final placed = result as BidPlaced;
      expect(placed.response.lot.currentBidMinor, 86000000);
      expect(placed.response.lot.latestSeq, 1044);
    });

    test('offline fails loudly and immediately — nothing is held', () async {
      final transport = _RecordingTransport(
        const WriteUnreachable(reason: ApiErrorCodes.unreachable),
      );

      final result = await BidClient(ImmediateWriter(transport)).place(
        lotId: 'lot-1',
        amountMinor: 85000000,
        seenBidSeq: 1042,
      );

      // The dealer is told now, while they are still looking at the lot. A queued bid would drain
      // ninety seconds later onto a lot that has closed, at a price that has moved, after they had
      // stopped watching because as far as they knew they were in.
      expect(result, isA<BidUnsent>());
      expect((result as BidUnsent).code, ApiErrorCodes.unreachable);
      expect(transport.sent, hasLength(1), reason: 'exactly one attempt, and no retry');
    });

    test('an outbid race comes back as stale with its machine code', () async {
      final transport = _RecordingTransport(
        const WriteConflicted(
          code: 'auction.bid.outbid_race',
          detail: 'Someone bid while you were typing.',
          currentState: <String, dynamic>{'currentBidMinor': 86000000},
        ),
      );

      final result = await BidClient(ImmediateWriter(transport)).place(
        lotId: 'lot-1',
        amountMinor: 85000000,
        seenBidSeq: 1042,
      );

      expect(result, isA<BidStale>());
      final stale = result as BidStale;
      // Branch on the code, never on the message — the message is content a writer can reword.
      expect(stale.code, 'auction.bid.outbid_race');
      expect(stale.currentState?['currentBidMinor'], 86000000);
    });

    test('a rejection below the ladder is not retried and keeps its code', () async {
      final transport = _RecordingTransport(
        const WriteRejected(statusCode: 422, code: 'auction.bid.below_increment'),
      );

      final result = await BidClient(ImmediateWriter(transport)).place(
        lotId: 'lot-1',
        amountMinor: 1,
        seenBidSeq: 1042,
      );

      expect(result, isA<BidRefused>());
      expect((result as BidRefused).code, 'auction.bid.below_increment');
      expect(transport.sent, hasLength(1));
    });

    test('a 2xx whose body will not decode does not fabricate a placed bid', () async {
      final transport = _RecordingTransport(
        const WriteAccepted(statusCode: 200, body: <String, dynamic>{'unexpected': true}),
      );

      final result = await BidClient(ImmediateWriter(transport)).place(
        lotId: 'lot-1',
        amountMinor: 85000000,
        seenBidSeq: 1042,
      );

      // The server acted, but nothing usable came back. Reporting a placement this client cannot
      // substantiate would be the optimistic render the whole path exists to avoid; the screen
      // refetches instead.
      expect(result, isA<BidRefused>());
      expect((result as BidRefused).code, ApiErrorCodes.malformedResponse);
    });

    test('a queueable intent can never reach the immediate path', () async {
      final transport = _RecordingTransport(const WriteAccepted(statusCode: 200));
      final writer = ImmediateWriter(transport);

      // The mirror of the outbox's own refusal. If somebody ever rebuilds the bid intent as
      // queueable, this is where it fails — on a developer's machine, not in a showroom.
      expect(
        () => writer.sendStrict(
          WriteIntent.now(endpoint: '/auction/lots/lot-1/bids', method: WriteMethod.post),
        ),
        throwsA(isA<NotSendableImmediatelyError>()),
      );
      expect(transport.sent, isEmpty);
    });
  });
}
