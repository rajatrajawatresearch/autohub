import 'dart:io' show HttpDate;

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// A value and the server clock that was measured while fetching it.
///
/// Paired rather than kept in a separate provider because the two must not drift apart: a lot
/// fetched now and a clock offset measured four hours ago is exactly the combination that renders
/// a live lot as closed. Handing them to the UI as one object makes the stale pairing
/// unrepresentable.
@immutable
final class Clocked<T> {
  const Clocked({required this.value, required this.clock});

  final T value;

  /// Offset measured on the request that produced [value]. See [clockFromHttpDate].
  final AhServerClock clock;

  Clocked<R> map<R>(R Function(T value) transform) =>
      Clocked<R>(value: transform(value), clock: clock);
}

/// Builds the countdown's clock from the `Date` header of the response that carried the lot.
///
/// **Why the `Date` header and not a bespoke handshake.** `contracts/registry-api.md` §11 does not
/// define a server-hello frame on the auction socket, and inventing one would put the client ahead
/// of the contract. Every HTTP response already carries an RFC 9110 `Date`, so the offset can be
/// measured on the very request that fetches the lot — which is precisely "at connect time" in the
/// sense §6.2 means it — with no extra round trip and nothing added to the seam.
///
/// **Why one second of resolution is enough.** `Date` is whole seconds, and the round trip is
/// halved rather than filtered NTP-style. Both are coarse on purpose: the failure this exists to
/// prevent is a handset that is *minutes* out, not milliseconds of network jitter. A phone two
/// minutes fast shows a live lot as closed and the dealer stops bidding on a truck they were
/// winning; nothing about that is fixed by a better estimator, and the countdown ticks once a
/// second regardless.
///
/// Returns `null` when the header is missing or unparseable, and the caller then keeps whatever
/// clock it already had. Falling back to [AhServerClock.unsynchronised] here would silently swap a
/// good offset for the device clock on one bad response — the one failure mode this whole
/// mechanism exists to make impossible.
AhServerClock? clockFromHttpDate(
  String? header, {
  required DateTime sentAt,
  required DateTime receivedAt,
}) {
  if (header == null || header.isEmpty) return null;

  final DateTime serverTime;
  try {
    serverTime = HttpDate.parse(header);
  } on Object {
    // Deliberately broad. `HttpDate.parse` signals a malformed header with an `HttpException`, and
    // a proxy that invents its own date format is a thing that happens on hotel wifi. Whatever it
    // throws, the answer is the same: keep the clock we already had rather than adopting the
    // handset's.
    return null;
  }

  final roundTrip = receivedAt.difference(sentAt);
  return AhServerClock.fromHandshake(
    serverTime: serverTime,
    receivedAt: receivedAt,
    // A negative round trip means the device clock moved under us mid-request. Discarding it is
    // right: the offset is still correct to within the true latency, and feeding a negative
    // duration into the halving would push the estimate the wrong way.
    roundTrip: roundTrip.isNegative ? Duration.zero : roundTrip,
  );
}
