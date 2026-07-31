import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// The auction floor is the only place in the product where being slightly wrong is expensive and
/// irreversible, and the countdown is the part of it that a handset can get wrong on its own.
///
/// These tests exist for one failure in particular: a phone whose clock is two minutes off. It is
/// not hypothetical — device clocks drift, and users set them by hand. The bug it produces is
/// invisible in logs and reads to the dealer as "the app said the lot was closed".
void main() {
  /// A handset running two minutes FAST. The server's "now" is two minutes behind the device's.
  AhServerClock fastHandset() {
    final receivedAt = DateTime.now().toUtc();
    return AhServerClock.fromHandshake(
      serverTime: receivedAt.subtract(const Duration(minutes: 2)),
      receivedAt: receivedAt,
    );
  }

  String countdown(WidgetTester tester) {
    final strip = find.descendant(
      of: find.byType(BidTicket),
      matching: find.byWidgetPredicate(
        (w) => w is Text && RegExp(r'^\d{2}:\d{2}:\d{2}$').hasMatch(w.data ?? ''),
      ),
    );
    return tester.widget<Text>(strip).data!;
  }

  Future<void> unmount(WidgetTester tester) async {
    // The ticket owns a periodic timer. Unmounting explicitly proves `dispose` cancels it —
    // flutter_test fails the test if a timer outlives the tree.
    await tester.pumpWidget(const SizedBox.shrink());
  }

  group('the server clock, not the handset clock', () {
    testWidgets('a lot the device thinks has closed is still live', (tester) async {
      // Closes one minute from the *device's* now. A device two minutes fast is therefore looking
      // at a lot the server considers to have three minutes left.
      final endsAt = DateTime.now().toUtc().add(const Duration(minutes: 1));

      await tester.pumpWidget(
        ahHarness(BidTicket(endsAt: endsAt, clock: fastHandset())),
      );

      final left = countdown(tester);
      expect(left, isNot('00:00:00'), reason: 'the lot is still taking bids');
      // ~00:03:00, allowing a second of test execution.
      expect(left.startsWith('00:02:5') || left.startsWith('00:03:0'), isTrue, reason: left);

      await unmount(tester);
    });

    testWidgets('the same lot reads closed against the raw device clock', (tester) async {
      final endsAt = DateTime.now().toUtc().subtract(const Duration(minutes: 1));

      await tester.pumpWidget(
        ahHarness(
          BidTicket(endsAt: endsAt, clock: AhServerClock.unsynchronised),
        ),
      );

      // The control case: with no measured offset this is exactly the reading a drifted phone
      // would show, which is why `unsynchronised` is named rather than defaulted.
      expect(countdown(tester), '00:00:00');
      await unmount(tester);
    });

    testWidgets('a slow handset does not show time that has already run out', (tester) async {
      final receivedAt = DateTime.now().toUtc();
      // Handset two minutes SLOW: the server is ahead, so a lot closing in one device-minute has
      // already closed.
      final clock = AhServerClock.fromHandshake(
        serverTime: receivedAt.add(const Duration(minutes: 2)),
        receivedAt: receivedAt,
      );

      await tester.pumpWidget(
        ahHarness(
          BidTicket(
            endsAt: DateTime.now().toUtc().add(const Duration(minutes: 1)),
            clock: clock,
          ),
        ),
      );

      expect(countdown(tester), '00:00:00');
      await unmount(tester);
    });
  });

  group('closing', () {
    testWidgets('onEnd fires once, and the strip stops being yellow', (tester) async {
      var ended = 0;
      await tester.pumpWidget(
        ahHarness(
          BidTicket(
            endsAt: DateTime.now().toUtc().subtract(const Duration(seconds: 5)),
            clock: AhServerClock.unsynchronised,
            onEnd: () => ended += 1,
          ),
        ),
      );

      await tester.pump(const Duration(seconds: 1));
      expect(ended, 1);

      // Three more ticks. A callback that refires every second would refetch the lot every second.
      await tester.pump(const Duration(seconds: 1));
      await tester.pump(const Duration(seconds: 1));
      await tester.pump(const Duration(seconds: 1));
      expect(ended, 1);

      await unmount(tester);
    });

    testWidgets('a closed lot does not carry the rationed yellow', (tester) async {
      await tester.pumpWidget(
        ahHarness(
          BidTicket(
            endsAt: DateTime.now().toUtc().subtract(const Duration(minutes: 1)),
            clock: AhServerClock.unsynchronised,
          ),
        ),
      );

      final fills = tester
          .widgetList<DecoratedBox>(find.byType(DecoratedBox))
          .map((box) => (box.decoration as BoxDecoration).color)
          .toList();

      // Yellow means "this one, now". A settled lot is not that, and leaving it yellow spends the
      // screen's one signal element on something nobody can act on.
      expect(fills, isNot(contains(AhColors.signal)));
      await unmount(tester);
    });
  });

  group('what it shows', () {
    testWidgets('no bids reads as an instruction, not as zero rupees', (tester) async {
      await tester.pumpWidget(
        ahHarness(
          BidTicket(
            endsAt: DateTime.now().toUtc().add(const Duration(hours: 1)),
            clock: AhServerClock.unsynchronised,
            labels: const BidTicketLabels(noBids: 'No bids yet'),
          ),
        ),
      );

      // `₹0` would be a lie about a lot that simply has not been bid on.
      expect(find.text('No bids yet'), findsOneWidget);
      expect(find.byType(MoneyValue), findsNothing);
      await unmount(tester);
    });

    testWidgets('an extension explains itself while the lot is live', (tester) async {
      await tester.pumpWidget(
        ahHarness(
          BidTicket(
            endsAt: DateTime.now().toUtc().add(const Duration(minutes: 2)),
            clock: AhServerClock.unsynchronised,
            extended: true,
            labels: const BidTicketLabels(extended: 'Extended — a late bid moves the close'),
          ),
        ),
      );

      // Without the notice a clock that grows looks like a bug in the app.
      expect(find.text('Extended — a late bid moves the close'), findsOneWidget);
      await unmount(tester);
    });
  });

  group('formatRemaining', () {
    // A list rather than a map: `Duration` has no primitive equality, so it cannot key a const map.
    const cases = <(Duration, String)>[
      (Duration.zero, '00:00:00'),
      (Duration(seconds: 9), '00:00:09'),
      (Duration(minutes: 4, seconds: 5), '00:04:05'),
      (Duration(hours: 1, minutes: 14, seconds: 9), '01:14:09'),
      (Duration(hours: 23, minutes: 59, seconds: 59), '23:59:59'),
      // Days split out rather than rolled into hours: "51:14:09" is a figure a dealer has to do
      // arithmetic on, and the whole job of the strip is that they do not have to.
      (Duration(days: 2, hours: 3, minutes: 11, seconds: 57), '2d 03:11:57'),
    ];

    for (final (remaining, expected) in cases) {
      test('$remaining reads $expected', () {
        expect(formatRemaining(remaining), expected);
      });
    }
  });
}
