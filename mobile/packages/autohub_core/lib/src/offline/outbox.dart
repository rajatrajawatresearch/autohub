import 'dart:convert';

import 'package:drift/drift.dart';
import 'package:flutter/foundation.dart';

import '../net/api_exception.dart';
import '../storage/ah_database.dart';
import 'write_intent.dart';

/// Where a queued write stands.
///
/// Three states, and note the one that is missing: there is no `inFlight`. A row that was being
/// sent when the process died would sit in that state forever, and the write the seller believes
/// they submitted would never be retried. Re-sending a row that did reach the server is harmless —
/// that is what `WriteIntent.idempotencyKey` is for — so the queue is designed to risk a duplicate
/// request rather than risk a stranded one.
enum OutboxState {
  /// Waiting for its next attempt window.
  pending,

  /// Stopped by a `409`. Needs a human decision, and will never retry on its own.
  conflicted,

  /// Rejected past the point of usefully asking again. Needs a human decision.
  poisoned;

  static OutboxState? tryParse(String value) => switch (value) {
        'pending' => OutboxState.pending,
        'conflicted' => OutboxState.conflicted,
        'poisoned' => OutboxState.poisoned,
        _ => null,
      };
}

/// A queued write plus its retry bookkeeping.
@immutable
final class OutboxEntry {
  const OutboxEntry({
    required this.intent,
    required this.state,
    required this.attempts,
    required this.nextAttemptAt,
    this.lastError,
    this.lastErrorCode,
    this.conflict,
  });

  final WriteIntent intent;
  final OutboxState state;

  /// Server rejections so far. Outages are not counted — see `BackoffPolicy.outageRetry`.
  final int attempts;

  final DateTime nextAttemptAt;

  /// Human text from the last failure, for the queue screen.
  final String? lastError;

  /// Stable machine code from the last failure. The thing code branches on.
  final String? lastErrorCode;

  /// The server's state at the moment of a `409`.
  final Map<String, dynamic>? conflict;

  String get idempotencyKey => intent.idempotencyKey;

  bool isDueAt(DateTime now) =>
      state == OutboxState.pending && !nextAttemptAt.isAfter(now.toUtc());

  /// True when this write needs a person to look at it. Drives the badge on the queue screen.
  bool get needsAttention =>
      state == OutboxState.conflicted || state == OutboxState.poisoned;

  @override
  String toString() => 'OutboxEntry(${intent.method.wire} ${intent.endpoint}, '
      '${state.name}, attempts: $attempts)';
}

/// Thrown when something tries to queue a write that must never be queued.
///
/// The only writes with `WriteDurability.immediate` are bids. Reaching this is not a network
/// condition to be handled — it is a wiring mistake in the auction client, and it should fail on
/// the developer's first run rather than in a showroom.
final class NotQueueableError extends Error {
  NotQueueableError(this.intent);

  final WriteIntent intent;

  @override
  String toString() =>
      'NotQueueableError: ${intent.method.wire} ${intent.endpoint} was captured as '
      'WriteDurability.immediate and must not be queued. Bids go through ImmediateWriter — a bid '
      'delivered after the lot closes is worse than a bid that visibly failed.';
}

/// The durable write queue.
///
/// Persistence only. Every decision about *when* to retry and *whether* to give up belongs to
/// `OutboxDrainer`, which is the part with a transport and a clock; keeping them apart is what
/// lets the retry policy be tested without SQLite and the storage be tested without a network.
final class Outbox {
  Outbox(this._db, {DateTime Function()? clock}) : _clock = clock ?? _wallClock;

  static DateTime _wallClock() => DateTime.now().toUtc();

  final AhDatabase _db;
  final DateTime Function() _clock;

  /// Persists a write for later delivery.
  ///
  /// Refuses `WriteDurability.immediate` outright. The check is here, at the storage boundary,
  /// rather than in the auction client, because it is the boundary that every path must cross: a
  /// future retry helper, a "resend all" button, or a well-meaning generic `submit()` wrapper
  /// cannot route a bid into the queue without going through this line.
  ///
  /// Enqueuing an intent whose key is already present is a no-op that returns the stored row. A
  /// double-tap is one act, and the primary key says so.
  Future<OutboxEntry> enqueue(WriteIntent intent) async {
    if (!intent.isQueueable) throw NotQueueableError(intent);

    final existing = await find(intent.idempotencyKey);
    if (existing != null) return existing;

    final entry = OutboxEntry(
      intent: intent,
      state: OutboxState.pending,
      attempts: 0,
      // Due immediately. Backoff starts after the first failure, not before the first try.
      nextAttemptAt: intent.createdAt,
    );

    await _db.into(_db.outboxRows).insert(
          OutboxRowsCompanion.insert(
            idempotencyKey: intent.idempotencyKey,
            endpoint: intent.endpoint,
            method: intent.method.wire,
            body: Value(intent.body == null ? null : jsonEncode(intent.body)),
            headers: Value(intent.headers.isEmpty ? null : jsonEncode(intent.headers)),
            createdAt: intent.createdAt,
            nextAttemptAt: entry.nextAttemptAt,
            state: OutboxState.pending.name,
          ),
          mode: InsertMode.insertOrIgnore,
        );

    return entry;
  }

  /// Rows ready to send, oldest first.
  ///
  /// Order is load-bearing and not cosmetic. *Create the listing* must reach the server before
  /// *submit it for review*, and a queue that drains newest-first turns a two-step flow into a
  /// `404` followed by a poisoned write. `createdAt` is stored as ISO text precisely so this
  /// ordering survives two taps in the same second; the key breaks any remaining tie so the order
  /// is total rather than merely mostly-defined.
  Future<List<OutboxEntry>> due({DateTime? now, int limit = 50}) async {
    final at = (now ?? _clock()).toUtc();
    final query = _db.select(_db.outboxRows)
      ..where((row) => row.state.equals(OutboxState.pending.name))
      ..where((row) => row.nextAttemptAt.isSmallerOrEqualValue(at))
      ..orderBy([
        (row) => OrderingTerm.asc(row.createdAt),
        (row) => OrderingTerm.asc(row.idempotencyKey),
      ])
      ..limit(limit);
    return _entriesFrom(await query.get());
  }

  Future<List<OutboxEntry>> inState(OutboxState state) async {
    final query = _db.select(_db.outboxRows)
      ..where((row) => row.state.equals(state.name))
      ..orderBy([(row) => OrderingTerm.asc(row.createdAt)]);
    return _entriesFrom(await query.get());
  }

  Future<List<OutboxEntry>> all() async {
    final query = _db.select(_db.outboxRows)
      ..orderBy([(row) => OrderingTerm.asc(row.createdAt)]);
    return _entriesFrom(await query.get());
  }

  Future<OutboxEntry?> find(String idempotencyKey) async {
    final query = _db.select(_db.outboxRows)
      ..where((row) => row.idempotencyKey.equals(idempotencyKey));
    final row = await query.getSingleOrNull();
    return row == null ? null : _toEntry(row);
  }

  /// Everything in the queue, live. Powers the "3 pending" badge in the app shell.
  Stream<List<OutboxEntry>> watch() {
    final query = _db.select(_db.outboxRows)
      ..orderBy([(row) => OrderingTerm.asc(row.createdAt)]);
    return query.watch().map(_entriesFrom);
  }

  Future<int> countPending() async {
    final rows = await inState(OutboxState.pending);
    return rows.length;
  }

  /// The server accepted it. The row's job is over.
  Future<void> complete(String idempotencyKey) => discard(idempotencyKey);

  /// Pushes the next attempt out.
  ///
  /// [countsAsAttempt] is false for an outage, which is the whole reason this is one method with a
  /// flag rather than two: the caller must make that judgement explicitly at every call site, and
  /// getting it wrong silently is what burns a user's poison budget on a tunnel.
  Future<void> reschedule(
    String idempotencyKey, {
    required DateTime at,
    required bool countsAsAttempt,
    String? errorCode,
    String? error,
  }) async {
    final attempts = countsAsAttempt
        ? Value(await _attemptsOf(idempotencyKey) + 1)
        : const Value<int>.absent();
    final update = _db.update(_db.outboxRows)
      ..where((row) => row.idempotencyKey.equals(idempotencyKey));
    await update.write(
      OutboxRowsCompanion(
        nextAttemptAt: Value(at.toUtc()),
        state: Value(OutboxState.pending.name),
        attempts: attempts,
        lastErrorCode: Value(errorCode),
        lastError: Value(error),
      ),
    );
  }

  /// Stops a write on a `409` and records what the server says is true instead.
  Future<void> markConflicted(
    String idempotencyKey, {
    required String code,
    Map<String, dynamic>? currentState,
    String? detail,
  }) async {
    final update = _db.update(_db.outboxRows)
      ..where((row) => row.idempotencyKey.equals(idempotencyKey));
    await update.write(
      OutboxRowsCompanion(
        state: Value(OutboxState.conflicted.name),
        lastErrorCode: Value(code),
        lastError: Value(detail),
        conflict: Value(currentState == null ? null : jsonEncode(currentState)),
      ),
    );
  }

  /// Parks a write for a human.
  Future<void> poison(
    String idempotencyKey, {
    required String code,
    String? error,
    bool countsAsAttempt = true,
  }) async {
    final attempts = countsAsAttempt
        ? Value(await _attemptsOf(idempotencyKey) + 1)
        : const Value<int>.absent();
    final update = _db.update(_db.outboxRows)
      ..where((row) => row.idempotencyKey.equals(idempotencyKey));
    await update.write(
      OutboxRowsCompanion(
        state: Value(OutboxState.poisoned.name),
        attempts: attempts,
        lastErrorCode: Value(code),
        lastError: Value(error),
      ),
    );
  }

  /// Makes every write that is only waiting out an outage due immediately.
  ///
  /// An outage deferral is a guess about the future — "the network was down fifteen seconds ago, so
  /// give it fifteen more". Connectivity returning is not a better guess, it is evidence, and it
  /// makes the deferral stale.
  ///
  /// Without this a seller who comes out of a tunnel watches "saved on your phone" for the rest of a
  /// backoff that exists for a reason that has already stopped being true — and on a later attempt,
  /// with the interval grown, that is minutes of a user believing their listing did not go.
  ///
  /// Deliberately narrow: only rows whose last failure was [ApiErrorCodes.unreachable] are released.
  /// A write backing off because the *server* returned a 5xx is waiting on something connectivity
  /// says nothing about, and releasing those would turn a sick server into a hammered one.
  /// Attempt counts are untouched — an outage never counted as an attempt in the first place.
  Future<int> releaseOutageDeferrals() async {
    final update = _db.update(_db.outboxRows)
      ..where(
        (row) =>
            row.state.equals(OutboxState.pending.name) &
            row.lastErrorCode.equals(ApiErrorCodes.unreachable),
      );

    return update.write(OutboxRowsCompanion(nextAttemptAt: Value(_clock())));
  }

  /// Puts a conflicted or poisoned write back in the queue after the user resolved it.
  ///
  /// The attempt count resets because the user has changed something — accepted the server's
  /// state, edited the payload — and carrying the old count forward would poison the corrected
  /// write after one more failure.
  Future<void> retryNow(String idempotencyKey) async {
    final update = _db.update(_db.outboxRows)
      ..where((row) => row.idempotencyKey.equals(idempotencyKey));
    await update.write(
      OutboxRowsCompanion(
        state: Value(OutboxState.pending.name),
        attempts: const Value(0),
        nextAttemptAt: Value(_clock()),
        lastError: const Value(null),
        lastErrorCode: const Value(null),
        conflict: const Value(null),
      ),
    );
  }

  /// The user abandoned the write. Removes it without sending.
  Future<void> discard(String idempotencyKey) async {
    final delete = _db.delete(_db.outboxRows)
      ..where((row) => row.idempotencyKey.equals(idempotencyKey));
    await delete.go();
  }

  /// Empties the queue. Sign-out only — a queue is scoped to the user who created it, and draining
  /// one person's pending listing under the next person's token is a different bug in every module.
  Future<int> clear() => _db.delete(_db.outboxRows).go();

  Future<int> _attemptsOf(String idempotencyKey) async {
    final query = _db.select(_db.outboxRows)
      ..where((row) => row.idempotencyKey.equals(idempotencyKey));
    final row = await query.getSingleOrNull();
    return row?.attempts ?? 0;
  }

  List<OutboxEntry> _entriesFrom(List<OutboxRow> rows) {
    return [
      for (final row in rows)
        if (_toEntry(row) case final OutboxEntry entry) entry,
    ];
  }

  /// Rebuilds an entry, or null for a row this build cannot replay.
  ///
  /// Only reachable after a downgrade — a newer app queuing a verb or state this one has never
  /// heard of. Skipping is deliberate: sending a write whose method we had to guess is worse than
  /// leaving it in the table until the user updates again.
  static OutboxEntry? _toEntry(OutboxRow row) {
    final method = WriteMethod.tryParse(row.method);
    final state = OutboxState.tryParse(row.state);
    if (method == null || state == null) return null;

    return OutboxEntry(
      intent: WriteIntent.restored(
        idempotencyKey: row.idempotencyKey,
        endpoint: row.endpoint,
        method: method,
        createdAt: row.createdAt,
        durability: WriteDurability.queued,
        body: _decodeMap(row.body),
        headers: _decodeHeaders(row.headers),
      ),
      state: state,
      attempts: row.attempts,
      nextAttemptAt: row.nextAttemptAt,
      lastError: row.lastError,
      lastErrorCode: row.lastErrorCode,
      conflict: _decodeMap(row.conflict),
    );
  }

  static Map<String, dynamic>? _decodeMap(String? raw) {
    if (raw == null) return null;
    try {
      final decoded = jsonDecode(raw);
      return decoded is Map<String, dynamic> ? decoded : null;
    } on FormatException {
      return null;
    }
  }

  static Map<String, String> _decodeHeaders(String? raw) {
    final decoded = _decodeMap(raw);
    if (decoded == null) return const {};
    return {
      for (final entry in decoded.entries)
        if (entry.value case final String value) entry.key: value,
    };
  }
}
