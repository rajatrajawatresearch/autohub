import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// The odometer is the only animation in AutoHub, which makes reduced motion the only place the
/// product has to honour that preference — and therefore the only place it can quietly fail to.
///
/// The discriminator used throughout: a rolling digit column contains all ten glyphs behind a clip
/// rect, so `find.text('7')` finds widgets while a roll is mounted and finds nothing when the
/// static path rendered. `₹8.4L` contains no `7`, so the two states are unambiguous.
void main() {
  const valuation = 84000000; // ₹8.4L

  group('MediaQuery.disableAnimations is honoured', () {
    testWidgets('renders the final value on the first frame, with nothing to roll', (tester) async {
      await tester.pumpWidget(
        ahHarness(
          const OdometerValue(minorUnits: valuation, compact: true),
          disableAnimations: true,
        ),
      );

      // One pump. No settle, no elapsed duration — the value must be correct on the frame the
      // widget first paints, because someone who turned animations off usually did it for a
      // vestibular reason and a 600ms vertical scroll is exactly what they turned off.
      expect(find.text('₹8.4L'), findsOneWidget);
      expect(find.text('7'), findsNothing);
    });

    testWidgets('schedules no frames — nothing is animating at all', (tester) async {
      await tester.pumpWidget(
        ahHarness(
          const OdometerValue(minorUnits: valuation, compact: true),
          disableAnimations: true,
        ),
      );

      // A zero-duration roll would still tick the scheduler. This asserts the widget took the
      // static branch rather than animating very fast.
      expect(tester.binding.hasScheduledFrame, isFalse);
    });

    testWidgets('reacts when the preference changes under it', (tester) async {
      await tester.pumpWidget(
        ahHarness(const OdometerValue(minorUnits: valuation, compact: true)),
      );
      await tester.pump();
      expect(find.text('7'), findsWidgets, reason: 'rolling before the preference changes');

      // A user can flip the accessibility setting while the app is backgrounded, so the value is
      // read on every dependency change rather than once in initState.
      await tester.pumpWidget(
        ahHarness(
          const OdometerValue(minorUnits: valuation, compact: true),
          disableAnimations: true,
        ),
      );
      await tester.pump();

      expect(find.text('₹8.4L'), findsOneWidget);
      expect(find.text('7'), findsNothing);
    });
  });

  group('the roll', () {
    testWidgets('mounts digit columns rather than a plain string', (tester) async {
      await tester.pumpWidget(
        ahHarness(const OdometerValue(minorUnits: valuation, compact: true)),
      );
      await tester.pump();

      expect(find.text('₹8.4L'), findsNothing);
      expect(find.text('7'), findsWidgets);

      await tester.pumpAndSettle();
    });

    testWidgets('settles, and leaves no pending frames behind', (tester) async {
      await tester.pumpWidget(
        ahHarness(const OdometerValue(minorUnits: valuation, compact: true)),
      );
      await tester.pumpAndSettle();

      expect(tester.binding.hasScheduledFrame, isFalse);
    });

    testWidgets('still announces the real figure while the digits are moving', (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(
        ahHarness(const OdometerValue(minorUnits: valuation, compact: true)),
      );
      await tester.pump();

      // Ten stacked glyphs per column would otherwise be read out as "zero one two three…".
      expect(find.bySemanticsLabel('₹8.4L'), findsOneWidget);

      await tester.pumpAndSettle();
      handle.dispose();
    });

    testWidgets('re-rolls when the revealed figure changes', (tester) async {
      await tester.pumpWidget(
        ahHarness(const OdometerValue(minorUnits: valuation, compact: true)),
      );
      await tester.pumpAndSettle();
      expect(tester.binding.hasScheduledFrame, isFalse);

      await tester.pumpWidget(
        ahHarness(const OdometerValue(minorUnits: 1200000000, compact: true)),
      );
      await tester.pump();
      expect(tester.binding.hasScheduledFrame, isTrue);

      await tester.pumpAndSettle();
    });
  });
}
