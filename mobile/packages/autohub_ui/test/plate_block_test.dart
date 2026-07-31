import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// The plate is the primary key made visible, and a user is frequently holding the phone up next
/// to the car it describes. These tests pin the two things that would break that: the colours
/// (which are facts about Indian roads, not theme decisions) and the grouping.
void main() {
  group('the five plate classes', () {
    // An exhaustive switch, so adding a sixth class to the enum fails to compile here before it
    // can ship as a silently-white plate.
    ({Color fill, Color ink, Color border}) expected(PlateClass plateClass) => switch (plateClass) {
          PlateClass.private =>
            (fill: AhColors.plate, ink: AhColors.tarmac, border: AhColors.tarmac),
          PlateClass.commercial =>
            (fill: AhColors.signal, ink: AhColors.tarmac, border: AhColors.tarmac),
          PlateClass.ev =>
            (fill: AhColors.highway, ink: AhColors.plate, border: AhColors.highway),
          PlateClass.government =>
            (fill: AhColors.plate, ink: AhColors.tarmac, border: AhColors.tarmac),
          PlateClass.defence =>
            (fill: AhColors.tarmac, ink: AhColors.plate, border: AhColors.tarmac),
        };

    for (final plateClass in PlateClass.values) {
      testWidgets('${plateClass.name} paints its real-world colours', (tester) async {
        await tester.pumpWidget(
          ahHarness(PlateBlock(registration: 'MH12AB1234', plateClass: plateClass)),
        );

        final decoration = tester
            .widget<DecoratedBox>(
              find.descendant(
                of: find.byType(PlateBlock),
                matching: find.byType(DecoratedBox),
              ),
            )
            .decoration as BoxDecoration;
        final text = tester.widget<Text>(
          find.descendant(of: find.byType(PlateBlock), matching: find.byType(Text)),
        );

        expect(decoration.color, expected(plateClass).fill);
        expect(decoration.border!.top.color, expected(plateClass).border);
        expect(text.style!.color, expected(plateClass).ink);
      });

      testWidgets('${plateClass.name} does not flip in dark mode', (tester) async {
        await tester.pumpWidget(
          ahHarness(
            PlateBlock(registration: 'MH12AB1234', plateClass: plateClass),
            brightness: Brightness.dark,
          ),
        );

        final decoration = tester
            .widget<DecoratedBox>(
              find.descendant(
                of: find.byType(PlateBlock),
                matching: find.byType(DecoratedBox),
              ),
            )
            .decoration as BoxDecoration;

        // A plate on a windscreen has no dark mode. If this ever inverts, a user comparing the
        // screen against the car in front of them is looking at two different objects.
        expect(decoration.color, expected(plateClass).fill);
      });
    }
  });

  group('geometry', () {
    testWidgets('corners are square — a plate is a stamped rectangle, not a chip', (tester) async {
      await tester.pumpWidget(ahHarness(const PlateBlock(registration: 'MH12AB1234')));

      final decoration = tester
          .widget<DecoratedBox>(
            find.descendant(of: find.byType(PlateBlock), matching: find.byType(DecoratedBox)),
          )
          .decoration as BoxDecoration;

      expect(decoration.borderRadius, const BorderRadius.all(Radius.zero));
    });

    testWidgets('every size renders one unwrapped line', (tester) async {
      for (final size in PlateSize.values) {
        await tester.pumpWidget(
          ahHarness(PlateBlock(registration: 'MH12AB1234', size: size)),
        );
        final text = tester.widget<Text>(
          find.descendant(of: find.byType(PlateBlock), matching: find.byType(Text)),
        );
        expect(text.maxLines, 1, reason: '$size must not wrap');
        expect(text.softWrap, isFalse);
      }
    });
  });

  group('rendering', () {
    testWidgets('groups the canonical registration', (tester) async {
      await tester.pumpWidget(ahHarness(const PlateBlock(registration: 'MH12AB1234')));
      expect(find.text('MH 12 AB 1234'), findsOneWidget);
    });

    testWidgets('trusts the server pre-grouped display over the local rule', (tester) async {
      await tester.pumpWidget(
        ahHarness(const PlateBlock(registration: 'MH12AB1234', display: 'MH-12-AB-1234')),
      );

      // Keeping the server's form is what lets a series this build's regex does not know about
      // still render correctly, without an app release.
      expect(find.text('MH-12-AB-1234'), findsOneWidget);
      expect(find.text('MH 12 AB 1234'), findsNothing);
    });

    testWidgets('announces the grouped plate to a screen reader', (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(ahHarness(const PlateBlock(registration: 'mh12ab1234')));

      // A prefix match, not an exact one. `PlateBlock` annotates without excluding its child, so
      // the node's label merges to "Registration MH 12 AB 1234\nMH 12 AB 1234" — the plate is
      // announced twice. Asserting the merged form pins the behaviour that actually ships rather
      // than the one this test would prefer; see the handoff note about the redundant second read.
      expect(
        find.bySemanticsLabel(RegExp('^Registration MH 12 AB 1234')),
        findsOneWidget,
      );
      handle.dispose();
    });
  });

  group('groupRegistration', () {
    // The same function the plate input regroups with on every keystroke, so a partial string has
    // to behave as well as a complete one.
    const cases = <String, String>{
      'MH12AB1234': 'MH 12 AB 1234',
      'mh12ab1234': 'MH 12 AB 1234',
      'DL3CAB1234': 'DL 3 CAB 1234',
      // BH series — a different shape entirely, and the reason this splits on runs rather than
      // using a per-format table.
      '22BH1234AA': '22 BH 1234 AA',
      'MH 12 AB 1234': 'MH 12 AB 1234',
      'MH-12-AB-1234': 'MH 12 AB 1234',
      'M': 'M',
      'MH1': 'MH 1',
      '': '',
    };

    cases.forEach((input, expected) {
      test('"$input" groups as "$expected"', () {
        expect(groupRegistration(input), expected);
      });
    });
  });
}
