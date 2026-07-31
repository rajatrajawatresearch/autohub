import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/fault.dart';
import '../i18n/customer_text.dart';
import 'app_providers.dart';

/// What happened to a write the user just made.
///
/// Three cases and not two, because "queued" is a real outcome that the user has to be told about
/// in different words: their listing is not live, it is on their phone, and it will go up by
/// itself. Collapsing it into success is how a seller closes the app believing they have listed a
/// car; collapsing it into failure is how they fill the form in twice.
@immutable
sealed class SubmitResult {
  const SubmitResult();
}

/// The server acted, or replayed an earlier identical act. Either way it is done.
@immutable
final class SubmitAccepted extends SubmitResult {
  const SubmitAccepted({required this.statusCode, this.body});

  final int statusCode;

  /// The response payload — a created listing, a workflow snapshot. `null` on a `204`.
  final Map<String, dynamic>? body;
}

/// On this phone, waiting for a network. The draft is **not** discarded.
@immutable
final class SubmitQueued extends SubmitResult {
  const SubmitQueued(this.idempotencyKey);

  /// The key the gesture was born with, so a screen can watch the drain for its own write.
  final String idempotencyKey;
}

/// The server said no, and will say no again to the same request.
@immutable
final class SubmitRefused extends SubmitResult {
  const SubmitRefused(this.fault, {this.needsSignIn = false});

  final AppFault fault;

  /// A `401` that survived the transport's one refresh. The write is fine; the session is not.
  final bool needsSignIn;
}

final Provider<GestureWriter> gestureWriterProvider = Provider<GestureWriter>(
  (ref) => GestureWriter(
    immediate: ref.watch(immediateWriterProvider),
    outbox: ref.watch(outboxProvider),
    drainer: ref.watch(outboxDrainerProvider),
    diagnostics: ref.watch(diagnosticsLogProvider.notifier),
    strings: ref.watch(appStringsProvider),
  ),
);

/// Every mutation the customer app makes, in one shape.
///
/// ## Try now, queue if the network is not there
///
/// The two write paths in `autohub_core` answer different questions, and neither on its own is
/// right for a customer gesture:
///
/// * The **outbox alone** would be silent. A seller taps *Get my price*, the row lands in SQLite,
///   the screen says "submitted" — and a `422` about a registration number that is already listed
///   arrives twenty minutes later as a poisoned row nobody is looking at, with its field errors
///   thrown away, on a form the user has closed.
/// * The **immediate writer alone** would lose work. A seller in a tunnel finishes twenty minutes
///   of typing and six photographs and gets "no connection", and the only recovery is to do it
///   again when they surface.
///
/// So: send it now, and queue it only for the failures that might not be true in a minute. That
/// gives a `422` back to the form while the user is still standing in front of it, and gives a
/// tunnel the queue's guarantee — and it costs nothing, because
/// **[WriteIntent.now] mints the `Idempotency-Key` at the gesture**. The request that failed and
/// the row that drains an hour later are byte-identical, so the server recognises the replay
/// instead of creating a second listing.
///
/// A `409` and a `4xx` are never queued, for the reason `WriteOutcome` states plainly:
/// last-writer-wins is wrong for anything with money in it, and retrying a `422` spends a
/// commuter's battery to learn nothing.
final class GestureWriter {
  const GestureWriter({
    required ImmediateWriter immediate,
    required Outbox outbox,
    required OutboxDrainer drainer,
    required DiagnosticsLog diagnostics,
    required AppStrings strings,
  })  : _immediate = immediate,
        _outbox = outbox,
        _drainer = drainer,
        _diagnostics = diagnostics,
        _strings = strings;

  final ImmediateWriter _immediate;
  final Outbox _outbox;
  final OutboxDrainer _drainer;
  final DiagnosticsLog _diagnostics;
  final AppStrings _strings;

  /// Sends [intent] once, and queues it if — and only if — the failure was about the network.
  ///
  /// [intent] must have been captured with [WriteIntent.now] **at the moment of the tap**. Building
  /// it here would defeat the whole design: the key would be minted at send time and the retry
  /// after a tunnel would be a different act as far as the server is concerned.
  Future<SubmitResult> submit(WriteIntent intent) async {
    assert(
      intent.isQueueable,
      'Capture customer gestures with WriteIntent.now. WriteDurability.immediate is for writes '
      'that expire faster than a queue — bids — and this path deliberately falls back to the queue.',
    );

    final WriteOutcome outcome = await _immediate.send(intent);

    switch (outcome) {
      case WriteAccepted(:final int statusCode, :final Map<String, dynamic>? body):
        return SubmitAccepted(statusCode: statusCode, body: body);

      case WriteRejected(
          :final int statusCode,
          :final String code,
          :final String? detail,
          :final Map<String, List<String>> fieldErrors,
        ):
        // Wrong now and wrong forever. Straight back to the user with its field errors intact.
        return SubmitRefused(
          _record(
            code: code,
            detail: detail,
            statusCode: statusCode,
            fieldErrors: fieldErrors,
          ),
        );

      case WriteConflicted(:final String code, :final String? detail):
        // The entity moved underneath the write. Never retried — the UI shows what is true now and
        // asks, rather than re-applying an acceptance to a state that no longer exists.
        return SubmitRefused(_record(code: code, detail: detail, statusCode: 409));

      case WriteUnauthenticated(:final String code):
        // The transport already spent its one refresh. The write is good; the session is not.
        return SubmitRefused(
          _record(code: code, statusCode: 401),
          needsSignIn: true,
        );

      case WriteUnreachable(:final String reason):
        return _enqueue(intent, reason);

      case WriteFailedServer(:final String code):
        // A `5xx` or a `429`. Might well be true again in thirty seconds, which is exactly what the
        // queue's backoff exists for — and unlike a `422` there is nothing for the user to fix.
        return _enqueue(intent, code);
    }
  }

  Future<SubmitResult> _enqueue(WriteIntent intent, String reason) async {
    await _outbox.enqueue(intent);
    // Nudge the drain loop rather than waiting for its sweep timer. The common case is a signal
    // that came back thirty seconds ago and nothing has asked since.
    unawaited(_drainer.drain());
    _diagnostics.record(
      AppFault.coded(code: reason, strings: _strings, isOffline: true),
    );
    return SubmitQueued(intent.idempotencyKey);
  }

  AppFault _record({
    required String code,
    required int statusCode,
    String? detail,
    Map<String, List<String>> fieldErrors = const <String, List<String>>{},
  }) {
    final AppFault fault = AppFault.coded(
      code: code,
      strings: _strings,
      detail: detail,
      statusCode: statusCode,
      fieldErrors: fieldErrors,
    );
    _diagnostics.record(fault);
    return fault;
  }
}
