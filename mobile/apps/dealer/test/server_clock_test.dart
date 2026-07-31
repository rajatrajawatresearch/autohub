import 'dart:io' show HttpDate;

import 'package:autohub_dealer/src/data/server_time.dart';
import 'package:flutter_test/flutter_test.dart';

/// The same RFC 9110 spelling a server sends. Formatted rather than hand-written so the test
/// exercises the real parse rather than a string that happens to be in the right shape.
String _httpDate(DateTime instant) => HttpDate.format(instant);

/// The countdown is the one thing on the auction screen that cannot be rendered against the handset
/// clock. A phone two minutes fast shows a live lot as closed and the dealer stops bidding on a
/// truck they were winning; two minutes slow is worse, because they compose a bid on a lot the
/// server has already settled. Neither leaves a trace anyone can debug from a support ticket.
void main() {
  group('clockFromHttpDate', () {
    test('corrects a handset running two minutes fast', () {
      // The device believes it is 09:32; the server says 09:30.
      final at = DateTime.utc(2026, 7, 31, 9, 32);

      final clock = clockFromHttpDate(
        'Fri, 31 Jul 2026 09:30:00 GMT',
        sentAt: at,
        receivedAt: at,
      );

      expect(clock, isNotNull);
      expect(clock!.offset, const Duration(minutes: -2));
    });

    test('a corrected countdown keeps the two minutes a fast phone would have eaten', () {
      // Built against the real wall clock, because `AhServerClock.now()` derives from it — that is
      // the whole design: measure the offset once, then use the handset only for elapsed time.
      final now = DateTime.now().toUtc();
      final serverTime = now.subtract(const Duration(minutes: 2));

      final clock = clockFromHttpDate(
        _httpDate(serverTime),
        sentAt: now,
        receivedAt: now,
      )!;

      // A lot the server closes five minutes from *its* now. An uncorrected handset would render
      // three minutes and the dealer would stop bidding on a truck they were winning.
      final deadline = serverTime.add(const Duration(minutes: 5));
      final drift = (clock.remainingUntil(deadline) - const Duration(minutes: 5)).abs();

      expect(drift, lessThan(const Duration(seconds: 2)));
      expect(deadline.difference(now), lessThan(const Duration(minutes: 4)));
    });

    test('attributes half the round trip to the inbound leg', () {
      final sentAt = DateTime.utc(2026, 7, 31, 9, 30);
      final receivedAt = sentAt.add(const Duration(milliseconds: 400));

      final clock = clockFromHttpDate(
        'Fri, 31 Jul 2026 09:30:00 GMT',
        sentAt: sentAt,
        receivedAt: receivedAt,
      );

      // Server time at receipt is 09:30:00 + 200ms; the device said 09:30:00.400. Offset −200ms.
      expect(clock!.offset, const Duration(milliseconds: -200));
    });

    test('a missing or unreadable Date yields null so the previous clock survives', () {
      final at = DateTime.utc(2026, 7, 31, 9, 30);

      expect(clockFromHttpDate(null, sentAt: at, receivedAt: at), isNull);
      expect(clockFromHttpDate('', sentAt: at, receivedAt: at), isNull);
      expect(clockFromHttpDate('yesterday', sentAt: at, receivedAt: at), isNull);
      // Returning an unsynchronised clock here would silently swap a good offset for the device
      // clock on one bad response — the exact failure the mechanism exists to prevent.
    });

    test('a device clock that jumps backwards mid-request does not skew the estimate', () {
      final sentAt = DateTime.utc(2026, 7, 31, 9, 30, 5);
      // The user set the clock back while the request was in flight.
      final receivedAt = DateTime.utc(2026, 7, 31, 9, 30);

      final clock = clockFromHttpDate(
        'Fri, 31 Jul 2026 09:30:00 GMT',
        sentAt: sentAt,
        receivedAt: receivedAt,
      );

      // The negative round trip is discarded rather than halved into the offset.
      expect(clock!.offset, Duration.zero);
    });
  });
}
