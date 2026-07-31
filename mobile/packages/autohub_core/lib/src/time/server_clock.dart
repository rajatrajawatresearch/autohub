import 'package:flutter/foundation.dart';

/// The clock every countdown in this product renders against.
///
/// A handset clock is not a source of truth. Phones drift, users set them by hand, and a device
/// two minutes fast shows a live auction lot as closed while it is still taking bids — the dealer
/// stops bidding, loses the truck, and nothing in the logs says why. Two minutes slow is worse: a
/// bid composed against a lot the server has already closed.
///
/// So the offset is measured once, at connect time, from a timestamp the server supplied, and
/// every subsequent "now" in the UI is derived from it. The handset clock is still used — it is
/// the only monotonic-ish source available — but only for *elapsed* time since the handshake,
/// which is the one thing it is reliable for.
///
/// This type is in `autohub_core` rather than in the widget that needs it because the measurement
/// happens where the connection is made. A widget cannot synthesise it, and that is the point:
/// `BidTicket` takes an [AhServerClock] and therefore cannot accidentally call `DateTime.now()`.
@immutable
class AhServerClock {
  const AhServerClock._(this.offset);

  /// Builds the offset from a server timestamp and the instant the client received it.
  ///
  /// [roundTrip] is the measured request duration, half of which is assumed to be the inbound leg.
  /// Halving is a coarse estimate and it is deliberately coarse: the alternative is NTP-style
  /// filtering over many samples, and the accuracy that buys is far below the one-second tick the
  /// countdown renders at. What matters is removing *minutes* of device drift, not milliseconds
  /// of network jitter.
  factory AhServerClock.fromHandshake({
    required DateTime serverTime,
    required DateTime receivedAt,
    Duration roundTrip = Duration.zero,
  }) {
    final serverAtReceipt = serverTime.toUtc().add(roundTrip ~/ 2);
    return AhServerClock._(serverAtReceipt.difference(receivedAt.toUtc()));
  }

  /// Zero offset — the handset clock, unmodified.
  ///
  /// Legitimate in exactly two places: a widget test, and the frames between app launch and the
  /// first response carrying a `Date` header. It is named rather than defaulted so that using it
  /// on an auction screen looks wrong in review, because it is.
  static const AhServerClock unsynchronised = AhServerClock._(Duration.zero);

  /// server − handset. Positive when the handset is running slow.
  final Duration offset;

  /// UTC, corrected. The only "now" a countdown may read.
  DateTime now() => DateTime.now().toUtc().add(offset);

  /// Time left until [deadline], floored at zero.
  ///
  /// Floored rather than allowed to go negative because every caller wants the same thing — a
  /// countdown that stops at `00:00:00` — and a negative `Duration` leaking into a formatter
  /// renders `-1:59:59`, which reads as time remaining.
  Duration remainingUntil(DateTime deadline) {
    final left = deadline.toUtc().difference(now());
    return left.isNegative ? Duration.zero : left;
  }

  @override
  bool operator ==(Object other) => other is AhServerClock && other.offset == offset;

  @override
  int get hashCode => offset.hashCode;

  @override
  String toString() => 'AhServerClock(offset: $offset)';
}
