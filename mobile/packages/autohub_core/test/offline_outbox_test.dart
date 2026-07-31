import 'dart:math';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter_test/flutter_test.dart';

/// The queue is where being wrong costs money — a duplicated listing, a re-applied acceptance, a
/// bid on a closed lot. Every test here is a specific way that has happened to somebody.
void main() {
  late AhDatabase database;
  late _TestClock clock;
  late Outbox outbox;
  late _FakeOutboxTransport transport;

  setUp(() {
    database = openInMemoryAhDatabase();
    clock = _TestClock(DateTime.utc(2026, 7, 29, 9));
    outbox = Outbox(database, clock: clock.now);
    transport = _FakeOutboxTransport();
  });

  tearDown(() => database.close());

  OutboxDrainer drainerWith({BackoffPolicy? backoff, ConnectivityMonitor? connectivity}) {
    return OutboxDrainer(
      outbox: outbox,
      transport: transport,
      connectivity: connectivity ?? OutcomeDrivenConnectivity(),
      // Jitter off and a seeded generator: production wants the delay unpredictable, a test wants
      // the same curve twice.
      backoff: backoff ??
          const BackoffPolicy(
            base: Duration(seconds: 4),
            jitterFraction: 0,
            poisonAfter: 8,
          ),
      clock: clock.now,
      random: Random(1),
    );
  }

  WriteIntent submitListing({DateTime? at}) => WriteIntent.now(
        endpoint: '/listings',
        method: WriteMethod.post,
        body: const {'registrationNumber': 'MH12AB1234', 'kmsDriven': 38400},
        headers: const {'X-Active-City': 'pune'},
        occurredAt: at ?? clock.now(),
      );

  group('the idempotency key', () {
    test('is the same on the retry as on the attempt that failed in the tunnel', () async {
      // This is the whole package in one test. One tap, two HTTP requests, one listing.
      final intent = submitListing();
      final keyAtGestureTime = intent.idempotencyKey;
      await outbox.enqueue(intent);

      transport.script([
        const WriteFailedServer(statusCode: 503, code: 'common.server.unavailable'),
        const WriteAccepted(statusCode: 201, body: {'id': '019fab0c'}),
      ]);

      final drainer = drainerWith();
      final firstPass = await drainer.drain();
      expect(firstPass.deferred, 1);

      // The user is through the tunnel and the backoff window has passed.
      clock.advance(const Duration(minutes: 1));
      final secondPass = await drainer.drain();
      expect(secondPass.accepted, 1);

      expect(transport.sent, hasLength(2));
      expect(
        transport.sent.map((intent) => intent.idempotencyKey).toSet(),
        {keyAtGestureTime},
        reason: 'a fresh key on the retry is a second listing for one car',
      );
      expect(await outbox.all(), isEmpty);
      await drainer.dispose();
    });

    test('survives the process, because it is what the row is keyed by', () async {
      final intent = submitListing();
      await outbox.enqueue(intent);

      // A different Outbox over the same file — the app was killed and relaunched.
      final afterRelaunch = await Outbox(database, clock: clock.now).all();
      expect(afterRelaunch.single.idempotencyKey, intent.idempotencyKey);
      expect(afterRelaunch.single.intent.body, intent.body);
      expect(afterRelaunch.single.intent.headers['X-Active-City'], 'pune');
      expect(
        afterRelaunch.single.intent.createdAt,
        intent.createdAt,
        reason: 'createdAt is when the user acted, not when the row was replayed',
      );
    });

    test('two gestures are two keys', () {
      expect(submitListing().idempotencyKey, isNot(submitListing().idempotencyKey));
    });

    test('enqueuing one captured intent twice writes one row', () async {
      final intent = submitListing();
      await outbox.enqueue(intent);
      await outbox.enqueue(intent);
      expect(await outbox.all(), hasLength(1));
    });
  });

  group('a 409', () {
    test('does not retry, and keeps what the server says is true instead', () async {
      await outbox.enqueue(
        WriteIntent.now(
          endpoint: '/listings/019fab0c/workflow/actions',
          method: WriteMethod.post,
          body: const {'actionCode': 'approve'},
          occurredAt: clock.now(),
        ),
      );

      transport.script([
        const WriteConflicted(
          code: 'platform.workflow.transition_not_allowed',
          currentState: {'code': 'sold', 'name': 'Sold', 'category': 'succeeded'},
          detail: 'This listing has already been sold.',
        ),
      ]);

      final drainer = drainerWith();
      expect((await drainer.drain()).conflicted, 1);

      final parked = (await outbox.inState(OutboxState.conflicted)).single;
      expect(parked.lastErrorCode, 'platform.workflow.transition_not_allowed');
      expect(parked.conflict!['code'], 'sold');

      // An hour later, and a hundred drain passes later, it has still not been resent. Retrying
      // would re-apply a decision made against a world that has moved.
      clock.advance(const Duration(hours: 1));
      for (var pass = 0; pass < 3; pass++) {
        expect((await drainer.drain()).attempted, 0);
      }
      expect(transport.sent, hasLength(1));
      await drainer.dispose();
    });

    test('goes back into the queue only when a human resolves it', () async {
      await outbox.enqueue(submitListing());
      transport.script([
        const WriteConflicted(code: 'listings.already_live'),
        const WriteAccepted(statusCode: 200),
      ]);
      final drainer = drainerWith();
      await drainer.drain();

      final conflicted = (await outbox.inState(OutboxState.conflicted)).single;
      await outbox.retryNow(conflicted.idempotencyKey);

      expect((await drainer.drain()).accepted, 1);
      expect(await outbox.all(), isEmpty);
      await drainer.dispose();
    });
  });

  group('bids', () {
    test('the queue refuses to accept one', () async {
      final bid = WriteIntent.immediateNow(
        endpoint: '/auction/lots/019fab0c/bids',
        method: WriteMethod.post,
        body: const {'amountMinor': 51500000, 'seenBidSeq': 1042},
        occurredAt: clock.now(),
      );

      await expectLater(outbox.enqueue(bid), throwsA(isA<NotQueueableError>()));
      expect(
        await outbox.all(),
        isEmpty,
        reason: 'a bid delivered ninety seconds late is a bid on a lot that has closed',
      );
    });

    test('a bid that fails fails loudly and leaves nothing behind', () async {
      final connectivity = OutcomeDrivenConnectivity();
      final writer = ImmediateWriter(transport, reachability: connectivity);
      transport.script([const WriteUnreachable(reason: 'no route to host')]);

      final outcome = await writer.send(
        WriteIntent.immediateNow(
          endpoint: '/auction/lots/019fab0c/bids',
          method: WriteMethod.post,
          body: const {'amountMinor': 51500000},
          occurredAt: clock.now(),
        ),
      );

      expect(outcome, isA<WriteUnreachable>());
      expect(await outbox.all(), isEmpty, reason: 'no silent fallback into the queue');
      expect(connectivity.isOnline, isFalse);
      await connectivity.dispose();
    });

    test('a bid still carries one key across a double-tap replay', () async {
      // Not queueing is not the same as not deduplicating.
      final bid = WriteIntent.immediateNow(
        endpoint: '/auction/lots/019fab0c/bids',
        method: WriteMethod.post,
        occurredAt: clock.now(),
      );
      final writer = ImmediateWriter(transport);
      transport.script([
        const WriteFailedServer(statusCode: 502, code: 'common.server.bad_gateway'),
        const WriteAccepted(statusCode: 201),
      ]);

      await writer.send(bid);
      await writer.send(bid);

      expect(transport.sent.map((intent) => intent.idempotencyKey).toSet(), {bid.idempotencyKey});
    });

    test('sendStrict refuses a write that was captured as queueable', () {
      expect(
        () => ImmediateWriter(transport).sendStrict(submitListing()),
        throwsA(isA<NotSendableImmediatelyError>()),
      );
    });
  });

  group('backoff and poisoning', () {
    test('an outage does not spend an attempt', () async {
      await outbox.enqueue(submitListing());
      transport.always(const WriteUnreachable(reason: 'no signal'));

      final drainer = drainerWith();
      for (var pass = 0; pass < 5; pass++) {
        await drainer.drain();
        clock.advance(const Duration(minutes: 1));
      }

      final entry = (await outbox.inState(OutboxState.pending)).single;
      expect(
        entry.attempts,
        0,
        reason: 'a commuter passes through more dead spots than any poison limit tolerates',
      );
      await drainer.dispose();
    });

    test('a server failure spends an attempt, and the last one parks the write', () async {
      await outbox.enqueue(submitListing());
      transport.always(const WriteFailedServer(statusCode: 500, code: 'common.server.error'));

      final drainer = drainerWith(
        backoff: const BackoffPolicy(
          base: Duration(seconds: 4),
          jitterFraction: 0,
          poisonAfter: 3,
        ),
      );

      await drainer.drain();
      expect((await outbox.inState(OutboxState.pending)).single.attempts, 1);

      clock.advance(const Duration(minutes: 5));
      await drainer.drain();
      expect((await outbox.inState(OutboxState.pending)).single.attempts, 2);

      clock.advance(const Duration(minutes: 5));
      await drainer.drain();
      expect(await outbox.inState(OutboxState.pending), isEmpty);
      expect((await outbox.inState(OutboxState.poisoned)).single.attempts, 3);
      await drainer.dispose();
    });

    test('the backoff window is respected between passes', () async {
      await outbox.enqueue(submitListing());
      transport.always(const WriteFailedServer(statusCode: 500, code: 'common.server.error'));
      final drainer = drainerWith();

      await drainer.drain();
      // One second later the row is not due, so a sweep that fires early sends nothing.
      clock.advance(const Duration(seconds: 1));
      expect((await drainer.drain()).attempted, 0);

      clock.advance(const Duration(seconds: 10));
      expect((await drainer.drain()).attempted, 1);
      await drainer.dispose();
    });

    test('a server-named Retry-After wins over the client curve', () async {
      await outbox.enqueue(submitListing());
      transport.script([
        const WriteFailedServer(
          statusCode: 429,
          code: 'common.rate_limited',
          retryAfter: Duration(minutes: 30),
        ),
      ]);
      final drainer = drainerWith();
      await drainer.drain();

      clock.advance(const Duration(minutes: 10));
      expect((await drainer.drain()).attempted, 0);

      clock.advance(const Duration(minutes: 21));
      expect((await drainer.drain()).attempted, 1);
      await drainer.dispose();
    });

    test('a 422 is parked at once rather than retried into the same 422', () async {
      await outbox.enqueue(submitListing());
      transport.always(
        const WriteRejected(
          statusCode: 422,
          code: 'listings.validation_failed',
          detail: 'Enter the odometer reading in kilometres.',
          fieldErrors: {
            'kms_driven': ['kms.too_high'],
          },
        ),
      );

      final drainer = drainerWith();
      expect((await drainer.drain()).poisoned, 1);
      expect(transport.sent, hasLength(1));
      expect(
        (await outbox.inState(OutboxState.poisoned)).single.lastErrorCode,
        'listings.validation_failed',
      );
      await drainer.dispose();
    });

    test('a 401 leaves the write pending and stops the pass', () async {
      await outbox.enqueue(submitListing(at: clock.now().subtract(const Duration(seconds: 2))));
      await outbox.enqueue(submitListing(at: clock.now().subtract(const Duration(seconds: 1))));
      transport.always(const WriteUnauthenticated(code: 'identity.token.expired'));

      final drainer = drainerWith();
      final report = await drainer.drain();

      expect(report.attempted, 1, reason: 'the second row would fail on the same dead token');
      expect(report.stoppedEarly, isTrue);
      expect(await outbox.inState(OutboxState.pending), hasLength(2));
      expect((await outbox.inState(OutboxState.pending)).first.attempts, 0);
      await drainer.dispose();
    });
  });

  group('drain ordering and concurrency', () {
    test('sends oldest first, because step two depends on step one', () async {
      final create = WriteIntent.now(
        endpoint: '/listings',
        method: WriteMethod.post,
        occurredAt: clock.now().subtract(const Duration(seconds: 2)),
      );
      // 900ms later: close enough that a second-granularity timestamp would tie, which is exactly
      // the case `storeDateTimeAsText` exists to keep deterministic.
      final submit = WriteIntent.now(
        endpoint: '/listings/019fab0c/workflow/actions',
        method: WriteMethod.post,
        occurredAt: clock.now().subtract(const Duration(milliseconds: 1100)),
      );
      // Enqueued out of order on purpose.
      await outbox.enqueue(submit);
      await outbox.enqueue(create);

      transport.always(const WriteAccepted(statusCode: 200));
      final drainer = drainerWith();
      await drainer.drain();

      expect(transport.sent.map((intent) => intent.endpoint), [
        '/listings',
        '/listings/019fab0c/workflow/actions',
      ]);
      await drainer.dispose();
    });

    test('stops the pass at the first outage instead of collecting twenty timeouts', () async {
      for (var i = 4; i > 0; i--) {
        await outbox.enqueue(submitListing(at: clock.now().subtract(Duration(seconds: i))));
      }
      transport.always(const WriteUnreachable(reason: 'no signal'));

      final drainer = drainerWith();
      final report = await drainer.drain();

      expect(report.attempted, 1);
      expect(report.stoppedEarly, isTrue);
      expect(transport.sent, hasLength(1));
      await drainer.dispose();
    });

    test('overlapping drains share one pass', () async {
      await outbox.enqueue(submitListing());
      transport.always(const WriteAccepted(statusCode: 201));
      final drainer = drainerWith();

      // The connectivity edge, the sweep timer and a pull-to-refresh routinely land together.
      final reports = await Future.wait([drainer.drain(), drainer.drain(), drainer.drain()]);

      expect(transport.sent, hasLength(1));
      expect(reports.map((report) => report.accepted), everyElement(1));
      await drainer.dispose();
    });

    test('drains on the rising edge of connectivity, and only the rising edge', () async {
      await outbox.enqueue(submitListing());
      transport.always(const WriteAccepted(statusCode: 201));

      final connectivity = OutcomeDrivenConnectivity();
      final drainer = drainerWith(connectivity: connectivity)..start();
      final accepted = drainer.completions.first;

      // Going offline must not trigger a pass: every request in it would be a guaranteed timeout.
      connectivity.recordUnreachable();
      await Future<void>.delayed(const Duration(milliseconds: 20));
      expect(transport.sent, isEmpty);

      // The train leaves the tunnel. This is the moment the listing is supposed to appear.
      connectivity.recordReachable();
      await accepted;

      expect(transport.sent, hasLength(1));
      expect(await outbox.all(), isEmpty);

      await drainer.dispose();
      await connectivity.dispose();
    });

    test('a completed write is announced with the server response', () async {
      await outbox.enqueue(submitListing());
      transport.always(const WriteAccepted(statusCode: 201, body: {'id': '019fab0c'}));
      final drainer = drainerWith();

      final completions = <OutboxCompletion>[];
      final subscription = drainer.completions.listen(completions.add);
      await drainer.drain();
      await Future<void>.delayed(Duration.zero);

      expect(completions.single.body!['id'], '019fab0c');
      await subscription.cancel();
      await drainer.dispose();
    });
  });

  group('BackoffPolicy', () {
    test('doubles and then flattens at the ceiling', () {
      const policy = BackoffPolicy(
        base: Duration(seconds: 2),
        ceiling: Duration(seconds: 30),
        jitterFraction: 0,
      );
      expect(policy.delayFor(1), const Duration(seconds: 2));
      expect(policy.delayFor(2), const Duration(seconds: 4));
      expect(policy.delayFor(3), const Duration(seconds: 8));
      expect(policy.delayFor(10), const Duration(seconds: 30));
      // A corrupt attempt count must not overflow the shift into a negative delay.
      expect(policy.delayFor(100000), const Duration(seconds: 30));
    });

    test('jitter stays inside its band', () {
      const policy = BackoffPolicy(base: Duration(seconds: 10), jitterFraction: 0.25);
      final random = Random(7);
      for (var i = 0; i < 200; i++) {
        final delay = policy.delayFor(1, random: random);
        expect(delay.inMilliseconds, inInclusiveRange(7500, 12500));
      }
    });
  });
}

/// A transport the test drives, one outcome at a time.
final class _FakeOutboxTransport implements OutboxTransport {
  final List<WriteIntent> sent = [];
  final List<WriteOutcome> _script = [];
  WriteOutcome _fallback = const WriteAccepted(statusCode: 200);

  void script(List<WriteOutcome> outcomes) => _script
    ..clear()
    ..addAll(outcomes);

  void always(WriteOutcome outcome) {
    _script.clear();
    _fallback = outcome;
  }

  @override
  Future<WriteOutcome> send(WriteIntent intent) async {
    sent.add(intent);
    return _script.isEmpty ? _fallback : _script.removeAt(0);
  }
}

/// Wall clock under the test's control, so backoff windows are asserted rather than slept through.
final class _TestClock {
  _TestClock(this._now);

  DateTime _now;

  DateTime now() => _now;

  void advance(Duration by) => _now = _now.add(by);
}
