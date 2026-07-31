import 'package:flutter/foundation.dart';

import 'write_intent.dart';

/// What happened when a write was sent.
///
/// Six outcomes, sealed, because the drain loop must make a *different* decision for each and the
/// analyzer must refuse to compile a loop that forgets one. Collapsing these into "success or
/// failure" is how a `409` gets retried into a duplicate deal and a tunnel gets counted as a
/// server rejection.
///
/// Note what is not here: an exception. The transport converts every failure — HTTP, socket,
/// deadline — into one of these, so the drain loop has exactly one shape and no `catch` that can
/// swallow a case it did not anticipate.
sealed class WriteOutcome {
  const WriteOutcome();
}

/// `2xx`. The server acted, or replayed an earlier identical act. Either way the queue is done.
@immutable
final class WriteAccepted extends WriteOutcome {
  const WriteAccepted({required this.statusCode, this.body});

  final int statusCode;

  /// The response payload. For a workflow action this is the new `WorkflowSnapshot`, which is why
  /// the drain loop hands it to listeners rather than discarding it — the caller replaces state
  /// from the server's answer instead of guessing what the transition did.
  final Map<String, dynamic>? body;
}

/// `409`. The entity moved underneath this write.
///
/// **Never retried.** Last-writer-wins is wrong for anything with money in it: the dealer whose
/// queued *accept offer* drains an hour after the seller withdrew must be told the offer is gone,
/// not have the platform quietly re-apply their acceptance to a state that no longer exists. The
/// contract supports doing better — a `409` carries `metadata.currentState` — so the queue keeps
/// that and the UI asks.
@immutable
final class WriteConflicted extends WriteOutcome {
  const WriteConflicted({required this.code, this.currentState, this.detail});

  /// The stable machine code, e.g. `platform.workflow.transition_not_allowed`.
  final String code;

  /// The server's view of the entity right now, verbatim from the problem's `metadata`.
  final Map<String, dynamic>? currentState;

  /// Human text from the problem detail. Displayed, never branched on.
  final String? detail;
}

/// `4xx` other than `401` and `409`. The request is wrong and will be wrong forever.
///
/// Retrying a `422` produces the same `422` at every backoff step until the poison limit, having
/// spent the user's battery to learn nothing. It goes straight to poisoned with its field errors
/// intact so the sell form can put them back on the fields that caused them.
@immutable
final class WriteRejected extends WriteOutcome {
  const WriteRejected({
    required this.statusCode,
    required this.code,
    this.detail,
    this.fieldErrors = const {},
  });

  final int statusCode;
  final String code;
  final String? detail;

  /// Keyed by field key, per `packages/api`'s `ApiError.fieldErrors`.
  final Map<String, List<String>> fieldErrors;
}

/// `401` after the transport already tried its one refresh.
///
/// Kept separate from [WriteRejected] because the queue's response is the opposite: the write is
/// perfectly valid, the *session* is not. The row stays pending and drains after the user signs
/// back in. Poisoning it would throw away a listing the seller finished composing merely because
/// their access token expired inside a tunnel.
@immutable
final class WriteUnauthenticated extends WriteOutcome {
  const WriteUnauthenticated({required this.code});

  final String code;
}

/// `5xx`, `429`, or anything else the server said that might not be true in a minute.
@immutable
final class WriteFailedServer extends WriteOutcome {
  const WriteFailedServer({required this.statusCode, required this.code, this.retryAfter});

  final int statusCode;
  final String code;

  /// From `Retry-After` / `problem.retryAfterSeconds`. When the server names a delay, that delay
  /// wins over the client's backoff curve — the server knows why it is shedding load and the
  /// client does not.
  final Duration? retryAfter;
}

/// The request never reached a server: no signal, DNS failure, TLS failure, client deadline.
///
/// The drain loop treats this as "not the write's fault" and does not spend an attempt on it. A
/// commuter on the Mumbai local passes through enough dead spots in one journey to exhaust any
/// sane poison limit, and arriving at the office to find a perfectly good listing marked as failed
/// is a defect the user experiences as data loss.
@immutable
final class WriteUnreachable extends WriteOutcome {
  const WriteUnreachable({required this.reason});

  final String reason;
}

/// The seam between the queue and whatever is doing HTTP.
///
/// Declared here, implemented in `net/`. The outbox's whole job is deciding what to do when the
/// network misbehaves, and a test for that must be able to make the network misbehave on demand —
/// which means the queue may not know that dio exists.
abstract interface class OutboxTransport {
  /// Sends [intent] exactly as captured.
  ///
  /// Implementations **must** send `intent.idempotencyKey` as the `Idempotency-Key` header and
  /// must not substitute a fresh one on a retry. They must also not retry internally: retry policy
  /// lives in `OutboxDrainer`, where the persisted attempt count is, and two independent retry
  /// loops stacked on each other is how a 30-second outage becomes a ten-minute one.
  Future<WriteOutcome> send(WriteIntent intent);
}
