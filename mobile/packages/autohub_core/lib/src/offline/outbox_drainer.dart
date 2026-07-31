import 'dart:async';
import 'dart:math';

import 'package:flutter/foundation.dart';

import 'backoff.dart';
import 'connectivity.dart';
import 'outbox.dart';
import 'write_intent.dart';
import 'write_outcome.dart';

/// A queued write that the server accepted, with whatever it answered.
@immutable
final class OutboxCompletion {
  const OutboxCompletion({required this.intent, required this.statusCode, this.body});

  final WriteIntent intent;
  final int statusCode;

  /// The response payload. A workflow action answers with the new snapshot, and the screen that
  /// was waiting replaces its state from this rather than assuming the transition it requested is
  /// the transition that happened.
  final Map<String, dynamic>? body;
}

/// What one drain pass did.
@immutable
final class OutboxDrainReport {
  const OutboxDrainReport({
    this.attempted = 0,
    this.accepted = 0,
    this.conflicted = 0,
    this.poisoned = 0,
    this.deferred = 0,
    this.stoppedEarly = false,
  });

  final int attempted;
  final int accepted;
  final int conflicted;
  final int poisoned;
  final int deferred;

  /// True when the pass abandoned the remaining rows because the network or the session was gone.
  final bool stoppedEarly;

  @override
  String toString() => 'OutboxDrainReport(attempted: $attempted, accepted: $accepted, '
      'conflicted: $conflicted, poisoned: $poisoned, deferred: $deferred, '
      'stoppedEarly: $stoppedEarly)';
}

/// Sends what the queue holds, when it is worth sending.
///
/// The interesting decisions are all about *not* sending:
///
/// - A `409` never retries. It is the only outcome that means the world changed rather than the
///   request failed, and re-applying a write against a state that has moved is last-writer-wins on
///   something with money in it.
/// - An outage does not spend an attempt. Being in a tunnel is not evidence about the request.
/// - A pass stops at the first outage instead of walking the rest of the queue into twenty
///   consecutive timeouts, each one costing the user the transport's full deadline.
/// - A `401` stops the pass and leaves the row pending. The write is fine; the session is not.
final class OutboxDrainer {
  OutboxDrainer({
    required Outbox outbox,
    required OutboxTransport transport,
    required ConnectivityMonitor connectivity,
    this.backoff = const BackoffPolicy(),
    DateTime Function()? clock,
    Random? random,
    this.sweepInterval = const Duration(minutes: 2),
    this.batchSize = 25,
  })  : _outbox = outbox,
        _transport = transport,
        _connectivity = connectivity,
        _clock = clock ?? _wallClock,
        _random = random;

  static DateTime _wallClock() => DateTime.now().toUtc();

  final Outbox _outbox;
  final OutboxTransport _transport;
  final ConnectivityMonitor _connectivity;
  final DateTime Function() _clock;
  final Random? _random;

  final BackoffPolicy backoff;

  /// Backstop poll.
  ///
  /// [ConnectivityMonitor] is a lagging signal — see `OutcomeDrivenConnectivity` — so something has
  /// to make the first attempt that turns "we think we are offline" back into evidence. Two
  /// minutes is chosen to be cheap on battery: the common case for a queue with work in it is that
  /// a connectivity edge fires long before this does.
  final Duration sweepInterval;

  /// Rows per pass. A dealer's bulk upload can leave hundreds queued, and sending all of them in
  /// one pass holds the transport busy for minutes with no way to interrupt it.
  final int batchSize;

  final StreamController<OutboxCompletion> _completions =
      StreamController<OutboxCompletion>.broadcast();

  StreamSubscription<bool>? _connectivitySubscription;
  Timer? _sweep;
  Future<OutboxDrainReport>? _pass;
  bool _disposed = false;

  /// Fires for every write the server accepted, in the order they were sent.
  Stream<OutboxCompletion> get completions => _completions.stream;

  /// Begins draining on connectivity edges and on the sweep timer.
  ///
  /// Called once, from the app shell. Not from a widget: two drainers over one queue would each
  /// hold their own reentrancy guard and happily send the same row twice.
  void start() {
    if (_disposed) return;
    _connectivitySubscription ??= _connectivity.changes.listen((online) {
      // Only the rising edge. Draining because we just went offline is a guaranteed timeout.
      if (online) unawaited(drain());
    });
    _sweep ??= Timer.periodic(sweepInterval, (_) => unawaited(drain()));
  }

  /// Sends everything currently due, oldest first.
  ///
  /// Concurrent calls share one pass. The connectivity edge, the sweep timer and a manual
  /// pull-to-refresh routinely land together, and three passes over one queue is three attempts at
  /// the same row — which the `Idempotency-Key` makes safe on the server but which still spends the
  /// user's data three times and triples the poison count on a genuinely bad write.
  Future<OutboxDrainReport> drain() {
    final running = _pass;
    if (running != null) return running;
    if (_disposed) return Future.value(const OutboxDrainReport());

    // A block body: `whenComplete` awaits anything Future-shaped its callback returns, and an arrow
    // over an assignment returns the assigned value.
    final pass = _runPass().whenComplete(() {
      _pass = null;
    });
    _pass = pass;
    return pass;
  }

  Future<void> dispose() async {
    _disposed = true;
    await _connectivitySubscription?.cancel();
    _connectivitySubscription = null;
    _sweep?.cancel();
    _sweep = null;
    await _pass;
    await _completions.close();
  }

  Future<OutboxDrainReport> _runPass() async {
    // Every caller of drain() is a moment that says "try now": the rising connectivity edge, the
    // sweep timer, a pull-to-refresh. An outage deferral is the one kind of backoff none of those
    // should have to wait out, because the thing it was waiting for is what just happened.
    if (_connectivity.isOnline) {
      await _outbox.releaseOutageDeferrals();
    }

    final now = _clock();
    final due = await _outbox.due(now: now, limit: batchSize);

    var attempted = 0;
    var accepted = 0;
    var conflicted = 0;
    var poisoned = 0;
    var deferred = 0;
    var stoppedEarly = false;

    for (final entry in due) {
      if (_disposed) {
        stoppedEarly = true;
        break;
      }

      attempted += 1;
      final outcome = await _transport.send(entry.intent);
      final at = _clock();

      switch (outcome) {
        case WriteAccepted(:final statusCode, :final body):
          _reportReachable();
          await _outbox.complete(entry.idempotencyKey);
          accepted += 1;
          if (!_completions.isClosed) {
            _completions.add(
              OutboxCompletion(intent: entry.intent, statusCode: statusCode, body: body),
            );
          }

        case WriteConflicted(:final code, :final currentState, :final detail):
          // Terminal by design. The queue's job ends here and a human's begins: the conflict screen
          // shows `currentState` and asks. Retrying would apply a decision the user made against a
          // world that no longer exists.
          _reportReachable();
          await _outbox.markConflicted(
            entry.idempotencyKey,
            code: code,
            currentState: currentState,
            detail: detail,
          );
          conflicted += 1;

        case WriteRejected(:final code, :final detail, :final statusCode):
          // A malformed request is malformed at every backoff step. Park it with its field errors
          // so the form can put them back on the fields that caused them.
          //
          // The pass continues rather than stopping. One bad row must not block unrelated writes
          // behind it — at the cost that a row which *depended* on this one will fail next with a
          // 404 and be parked too. Both then appear on the queue screen, which is the honest
          // outcome; silently stalling the whole queue is not.
          _reportReachable();
          await _outbox.poison(
            entry.idempotencyKey,
            code: code,
            error: detail ?? 'Rejected with HTTP $statusCode.',
          );
          poisoned += 1;

        case WriteUnauthenticated(:final code):
          // The write is valid and the session is not. Leave it pending, do not spend an attempt,
          // and stop — every remaining row would return the same 401 against the same dead token.
          _reportReachable();
          await _outbox.reschedule(
            entry.idempotencyKey,
            at: at.add(backoff.outageRetry),
            countsAsAttempt: false,
            errorCode: code,
            error: 'Waiting for sign-in.',
          );
          deferred += 1;
          stoppedEarly = true;

        case WriteFailedServer(:final statusCode, :final code, :final retryAfter):
          _reportReachable();
          final attempts = entry.attempts + 1;
          if (backoff.isPoisoned(attempts)) {
            await _outbox.poison(
              entry.idempotencyKey,
              code: code,
              error: 'Gave up after $attempts attempts (last: HTTP $statusCode).',
            );
            poisoned += 1;
          } else {
            // A server that names its own delay knows why it is shedding load; the client's curve
            // is a guess in the absence of that.
            final delay = retryAfter ?? backoff.delayFor(attempts, random: _random);
            await _outbox.reschedule(
              entry.idempotencyKey,
              at: at.add(delay),
              countsAsAttempt: true,
              errorCode: code,
              error: 'HTTP $statusCode. Retrying.',
            );
            deferred += 1;
          }

        case WriteUnreachable(:final reason):
          _reportUnreachable();
          await _outbox.reschedule(
            entry.idempotencyKey,
            at: at.add(backoff.outageRetry),
            countsAsAttempt: false,
            errorCode: 'common.network.unreachable',
            error: reason,
          );
          deferred += 1;
          stoppedEarly = true;
      }

      if (stoppedEarly) break;
    }

    return OutboxDrainReport(
      attempted: attempted,
      accepted: accepted,
      conflicted: conflicted,
      poisoned: poisoned,
      deferred: deferred,
      stoppedEarly: stoppedEarly,
    );
  }

  void _reportReachable() {
    if (_connectivity case final ReachabilityReporter reporter) reporter.recordReachable();
  }

  void _reportUnreachable() {
    if (_connectivity case final ReachabilityReporter reporter) reporter.recordUnreachable();
  }
}
