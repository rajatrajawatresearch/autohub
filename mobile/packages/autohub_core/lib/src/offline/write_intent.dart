import 'package:flutter/foundation.dart';
import 'package:uuid/uuid.dart';

const Uuid _uuid = Uuid();

/// The verbs a write can use.
///
/// A legal enum under the prime directive: these are fixed by HTTP and by
/// `contracts/registry-api.md` §12, not by a registry row. `GET` is deliberately absent — the
/// outbox replays writes, and a queued read is a read that answers with the state of the world
/// twenty minutes ago. Leaving it out of the type makes that unrepresentable rather than
/// documented.
enum WriteMethod {
  post,
  put,
  patch,
  delete;

  /// The wire spelling.
  String get wire => switch (this) {
        WriteMethod.post => 'POST',
        WriteMethod.put => 'PUT',
        WriteMethod.patch => 'PATCH',
        WriteMethod.delete => 'DELETE',
      };

  /// Reads a persisted row back. Returns null for anything this build does not recognise, so a
  /// queue written by a newer app version cannot crash an older one on downgrade.
  static WriteMethod? tryParse(String value) => switch (value.toUpperCase()) {
        'POST' => WriteMethod.post,
        'PUT' => WriteMethod.put,
        'PATCH' => WriteMethod.patch,
        'DELETE' => WriteMethod.delete,
        _ => null,
      };
}

/// Whether a write may wait for the network, or must meet it now.
///
/// See [WriteIntent.immediateNow] for why bids are the second kind.
enum WriteDurability { queued, immediate }

/// One mutation the user asked for, captured at the instant they asked for it.
///
/// A `WriteIntent` is created on the gesture — the tap on *Submit for review*, the confirmation
/// sheet's *Place bid* — and not at the moment some drain loop decides to make an HTTP call. That
/// distinction is the entire correctness argument of this package, and it lives in one field.
@immutable
final class WriteIntent {
  /// Captures a write at the moment the user acted.
  ///
  /// ─────────────────────────────────────────────────────────────────────────────────────────
  /// **The `Idempotency-Key` is minted here, in this constructor, on the user's gesture — never
  /// at send time.**
  ///
  /// This is the single most important line in the package. The failure it prevents is concrete:
  /// a seller on a train taps *Submit*, the request leaves as the carriage enters a tunnel, and
  /// the socket dies somewhere between the phone and the server. The client cannot distinguish
  /// "the server never saw it" from "the server created the listing and the response was lost".
  /// Neither can the user.
  ///
  /// If the key were generated at send time, the retry that follows would carry a *different* key
  /// and the server would have no way to recognise it as the same act. The seller ends up with two
  /// listings for one car, two valuation requests, or — on the payment paths — two debits. If the
  /// key is generated here, the retry is byte-identical to the original, the server's idempotency
  /// table recognises it, and it replays the original response instead of acting twice.
  ///
  /// Which is why [idempotencyKey] is also the outbox's primary key: a double-tap that produces
  /// two enqueues of the same captured intent writes one row. The uniqueness that protects the
  /// server protects the queue with the same guarantee.
  /// ─────────────────────────────────────────────────────────────────────────────────────────
  WriteIntent.now({
    required this.endpoint,
    required this.method,
    Map<String, dynamic>? body,
    Map<String, String> headers = const {},
    this.durability = WriteDurability.queued,
    DateTime? occurredAt,
  })  : idempotencyKey = _uuid.v4(),
        createdAt = (occurredAt ?? DateTime.now()).toUtc(),
        body = body == null ? null : Map.unmodifiable(body),
        headers = Map.unmodifiable(headers);

  /// A write that must not be queued.
  ///
  /// Bids, and effectively nothing else. `docs/10-mobile-architecture.md` §4 states the reason
  /// plainly: a bid that lands ninety seconds late is a bid on a lot that has closed, at a price
  /// that has moved. Delivering it late is not a partial success — it is a dealer committing money
  /// to a truck somebody else already bought, against a figure that stopped being true a minute
  /// ago. The honest failure is loud, immediate, and in front of the dealer while they are still
  /// looking at the lot.
  ///
  /// The key is still minted at gesture time. Not queueing is not the same as not deduplicating: a
  /// dealer whose thumb bounces on *Place bid* over patchy showroom wifi must place one bid, and
  /// this is what makes the second POST a replay rather than a second bid.
  WriteIntent.immediateNow({
    required String endpoint,
    required WriteMethod method,
    Map<String, dynamic>? body,
    Map<String, String> headers = const {},
    DateTime? occurredAt,
  }) : this.now(
          endpoint: endpoint,
          method: method,
          body: body,
          headers: headers,
          durability: WriteDurability.immediate,
          occurredAt: occurredAt,
        );

  /// Rebuilds an intent from a persisted outbox row.
  ///
  /// The only constructor that accepts an existing [idempotencyKey], and it exists precisely so
  /// that a row surviving an app restart replays with the key it was born with. Nothing outside
  /// the outbox store should call it.
  WriteIntent.restored({
    required this.idempotencyKey,
    required this.endpoint,
    required this.method,
    required this.createdAt,
    required this.durability,
    Map<String, dynamic>? body,
    Map<String, String> headers = const {},
  })  : body = body == null ? null : Map.unmodifiable(body),
        headers = Map.unmodifiable(headers);

  /// The `Idempotency-Key` header value, and the identity of the user's act.
  final String idempotencyKey;

  /// Path relative to the API base — never an absolute URL. See `OutboxRows.endpoint`.
  final String endpoint;

  final WriteMethod method;

  /// JSON body, already shaped for the wire. Hidden schema-form fields are stripped before this
  /// point, because the payload the server sees must be the payload the client validated.
  final Map<String, dynamic>? body;

  /// Headers frozen at gesture time — `X-Active-City` above all. The city the user was in when
  /// they acted is the city the write belongs to, even if the queue does not drain until they have
  /// switched to another one.
  final Map<String, String> headers;

  /// When the user acted. Not when the request was sent, and not when the row was written.
  final DateTime createdAt;

  final WriteDurability durability;

  bool get isQueueable => durability == WriteDurability.queued;

  Duration ageAt(DateTime now) => now.toUtc().difference(createdAt);

  @override
  String toString() =>
      'WriteIntent(${method.wire} $endpoint, key: $idempotencyKey, ${durability.name})';
}
