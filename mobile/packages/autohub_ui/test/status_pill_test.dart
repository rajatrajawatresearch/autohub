import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// The status chip is where the prime directive is easiest to break: a `switch` on a state code
/// compiles, reviews cleanly, ships, and then renders every state added afterwards as grey on a
/// million handsets that cannot be hot-fixed.
///
/// So the mapping is pinned here for all six categories. The expectation below is written as an
/// exhaustive switch on purpose — if a seventh category is added to the contract, this file stops
/// compiling before anything can ship undecided.
void main() {
  StatusTone expected(AhStateCategory category) => switch (category) {
        AhStateCategory.draft =>
          const StatusTone(AhColors.n100, AhColors.n600, AhColors.n300),
        AhStateCategory.awaitingAction =>
          const StatusTone(AhColors.cautionSoft, AhColors.caution, AhColors.caution),
        AhStateCategory.inProgress =>
          const StatusTone(AhColors.infoSoft, AhColors.info, AhColors.info),
        AhStateCategory.succeeded =>
          const StatusTone(AhColors.clearSoft, AhColors.clear, AhColors.clear),
        AhStateCategory.failed =>
          const StatusTone(AhColors.flagSoft, AhColors.flag, AhColors.flag),
        AhStateCategory.cancelled =>
          const StatusTone(AhColors.n100, AhColors.n600, AhColors.n400),
      };

  BoxDecoration decorationOf(WidgetTester tester) => tester
      .widget<DecoratedBox>(
        find.descendant(of: find.byType(StatusPill), matching: find.byType(DecoratedBox)),
      )
      .decoration as BoxDecoration;

  group('every category maps to a tone', () {
    for (final category in AhStateCategory.values) {
      test('${category.wire} resolves without a state code', () {
        final tone = StatusPill.toneFor(category);
        expect(tone.fill, expected(category).fill);
        expect(tone.ink, expected(category).ink);
        expect(tone.border, expected(category).border);
      });

      testWidgets('${category.wire} paints its tone', (tester) async {
        await tester.pumpWidget(
          ahHarness(StatusPill(category: category, label: 'Under review')),
        );

        final decoration = decorationOf(tester);
        final text = tester.widget<Text>(
          find.descendant(of: find.byType(StatusPill), matching: find.byType(Text)),
        );

        expect(decoration.color, expected(category).fill);
        expect(decoration.border!.top.color, expected(category).border);
        expect(text.style!.color, expected(category).ink);
      });

      testWidgets('${category.wire} keeps its tone in dark mode', (tester) async {
        await tester.pumpWidget(
          ahHarness(
            StatusPill(category: category, label: 'Under review'),
            brightness: Brightness.dark,
          ),
        );

        // A status chip is a sticker on a file. Its colours clear AA against their own fill, not
        // against the page, so the same amber means the same thing at midnight in a basement.
        expect(decorationOf(tester).color, expected(category).fill);
      });
    }

    test('the contract has exactly six categories', () {
      // A guard on the enum itself, so a category added without touching the widget still trips
      // something in CI even if the tone happened to compile.
      expect(AhStateCategory.values, hasLength(6));
    });
  });

  group('the label comes from the registry, never from the category', () {
    testWidgets('renders whatever the server named the state', (tester) async {
      // "Legal hold" is a state nobody wrote code for. It arrives as a registry row with a
      // category, and it must render correctly on a build that predates it.
      await tester.pumpWidget(
        ahHarness(
          const StatusPill(category: AhStateCategory.awaitingAction, label: 'Legal hold'),
        ),
      );

      expect(find.text('LEGAL HOLD'), findsOneWidget);
      expect(decorationOf(tester).color, AhColors.cautionSoft);
    });

    testWidgets('announces the label as the registry wrote it, not uppercased', (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(
        ahHarness(
          const StatusPill(category: AhStateCategory.succeeded, label: 'Sold'),
        ),
      );

      // Uppercase is a typographic treatment. A screen reader spelling out "S-O-L-D" is not.
      expect(find.bySemanticsLabel('Sold'), findsOneWidget);
      handle.dispose();
    });

    testWidgets('a long label truncates rather than breaking the row', (tester) async {
      await tester.pumpWidget(
        ahHarness(
          const SizedBox(
            width: 90,
            child: StatusPill(
              category: AhStateCategory.inProgress,
              label: 'Awaiting seller documents',
            ),
          ),
        ),
      );

      final text = tester.widget<Text>(
        find.descendant(of: find.byType(StatusPill), matching: find.byType(Text)),
      );
      expect(text.maxLines, 1);
      expect(text.overflow, TextOverflow.ellipsis);
      expect(tester.takeException(), isNull);
    });
  });

  group('wire parsing degrades rather than crashes', () {
    test('a known category round-trips', () {
      for (final category in AhStateCategory.values) {
        expect(AhStateCategory.tryParse(category.wire), category);
      }
    });

    test('an unknown category is null, not an exception', () {
      // A seventh category shipped by the server must not take down the screen that renders it.
      expect(AhStateCategory.tryParse('legal_hold'), isNull);
    });

    test('parseOr falls back to draft — the category that promises nothing', () {
      expect(AhStateCategory.parseOr('legal_hold'), AhStateCategory.draft);
      expect(AhStateCategory.parseOr('succeeded'), AhStateCategory.succeeded);
    });
  });
}
