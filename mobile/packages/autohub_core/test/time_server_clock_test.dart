import 'package:autohub_core/src/time/server_clock.dart';
import 'package:flutter_test/flutter_test.dart';

/// The offset that keeps a drifted handset from mispricing an auction.
///
/// A phone two minutes fast shows a live lot as closed and the dealer walks away from a truck
/// they were winning; two minutes slow and they compose a bid on a lot that has already settled.
/// Neither leaves anything in a log, so the arithmetic is pinned here.
void main() {
  final receivedAt = DateTime.utc(2026, 7, 31, 10, 0, 0);

  group('fromHandshake', () {
    test('a handset running fast gets a negative offset', () {
      // The device says 10:00:00; the server says 09:58:00. The device is two minutes ahead.
      final clock = AhServerClock.fromHandshake(
        serverTime: receivedAt.subtract(const Duration(minutes: 2)),
        receivedAt: receivedAt,
      );

      expect(clock.offset, const Duration(minutes: -2));
    });

    test('a handset running slow gets a positive offset', () {
      final clock = AhServerClock.fromHandshake(
        serverTime: receivedAt.add(const Duration(minutes: 2)),
        receivedAt: receivedAt,
      );

      expect(clock.offset, const Duration(minutes: 2));
    });

    test('half the round trip is credited to the inbound leg', () {
      // The server stamped its time before sending; by the time the client read it, roughly half
      // the round trip had elapsed. Ignoring that biases every offset toward "the server is
      // behind" by however slow the network is.
      final clock = AhServerClock.fromHandshake(
        serverTime: receivedAt,
        receivedAt: receivedAt,
        roundTrip: const Duration(milliseconds: 400),
      );

      expect(clock.offset, const Duration(milliseconds: 200));
    });

    test('a local-time server stamp is normalised to UTC before subtracting', () {
      final local = DateTime.utc(2026, 7, 31, 10, 0, 0).toLocal();
      final clock = AhServerClock.fromHandshake(serverTime: local, receivedAt: receivedAt);

      // A stamp that skipped `toUtc()` would produce an offset equal to the device's timezone —
      // +05:30 in India, which would read as a five-and-a-half-hour clock drift.
      expect(clock.offset, Duration.zero);
    });
  });

  group('now', () {
    test('applies the offset to the device clock', () {
      final clock = AhServerClock.fromHandshake(
        serverTime: DateTime.now().toUtc().subtract(const Duration(minutes: 2)),
        receivedAt: DateTime.now().toUtc(),
      );

      final drift = DateTime.now().toUtc().difference(clock.now());
      expect(drift.inSeconds, closeTo(120, 2));
    });

    test('unsynchronised is the raw device clock, and is named so it is visible', () {
      final drift = DateTime.now().toUtc().difference(AhServerClock.unsynchronised.now());
      expect(drift.inSeconds.abs(), lessThanOrEqualTo(1));
      expect(AhServerClock.unsynchronised.offset, Duration.zero);
    });
  });

  group('remainingUntil', () {
    test('a lot the device thinks has closed still has time on the server', () {
      final now = DateTime.now().toUtc();
      // Device two minutes fast.
      final clock = AhServerClock.fromHandshake(
        serverTime: now.subtract(const Duration(minutes: 2)),
        receivedAt: now,
      );

      final remaining = clock.remainingUntil(now.add(const Duration(minutes: 1)));
      expect(remaining.inSeconds, closeTo(180, 2));
    });

    test('floors at zero rather than counting backwards', () {
      final past = DateTime.now().toUtc().subtract(const Duration(hours: 3));

      // A negative Duration reaching a formatter renders "-2:59:59", which reads as time left.
      expect(AhServerClock.unsynchronised.remainingUntil(past), Duration.zero);
    });

    test('normalises a local deadline before comparing', () {
      final deadline = DateTime.now().add(const Duration(minutes: 10));
      final remaining = AhServerClock.unsynchronised.remainingUntil(deadline);

      expect(remaining.inSeconds, closeTo(600, 2));
    });
  });

  group('value semantics', () {
    test('two clocks with the same offset are equal', () {
      // BidTicket re-arms its close callback when the clock changes; identity comparison there
      // would re-arm on every rebuild.
      final a = AhServerClock.fromHandshake(
        serverTime: receivedAt,
        receivedAt: receivedAt,
      );
      final b = AhServerClock.fromHandshake(
        serverTime: receivedAt.add(const Duration(days: 1)),
        receivedAt: receivedAt.add(const Duration(days: 1)),
      );

      expect(a, b);
      expect(a.hashCode, b.hashCode);
      expect(a, AhServerClock.unsynchronised);
    });
  });
}
