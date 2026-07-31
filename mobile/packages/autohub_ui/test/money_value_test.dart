import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// `MoneyValue` renders the single most-seen number in the product, and it is the widget where a
/// wrong answer costs money directly — a buyer reads `₹8.4L` and a seller reads `₹84L` off the
/// same paise value and one of them makes a decision on it.
///
/// The formatting rules themselves are pinned in `autohub_core`'s
/// `values_indian_format_test.dart`, against the same expectations the web's `format.ts` produces.
/// What is tested here is that the widget renders exactly what the formatter returned, without
/// losing a character to the two-span typographic split.
void main() {
  /// The rendered string, reassembled from the spans. This is the assertion that matters: the
  /// widget splits the amount into a small symbol and full-size digits, and a split that dropped
  /// or duplicated a character would still *look* plausible on screen.
  String rendered(WidgetTester tester) {
    final text = tester.widget<Text>(
      find.descendant(of: find.byType(MoneyValue), matching: find.byType(Text)),
    );
    return text.textSpan!.toPlainText();
  }

  group('the lakh and crore boundaries, spoken', () {
    const cases = <int, String>{
      // Below a lakh: written in full, because that is how it is said.
      9450000: '₹94,500',
      // The documented pair from the design system.
      84000000: '₹8.4L',
      1200000000: '₹1.2Cr',
      // Exactly on each boundary.
      10000000: '₹1L',
      1000000000: '₹1Cr',
      // ₹99,999.60 rounds up across the lakh boundary. Saying it as a lakh is the whole point of
      // the promotion — the compact renderer never emits six digits.
      9999960: '₹1L',
      // 99.9995L would round to "100L", which nobody says.
      999995000: '₹1Cr',
      // Two decimals is the ceiling for a spoken figure.
      54900000: '₹5.49L',
      0: '₹0',
      -9450000: '-₹94,500',
    };

    cases.forEach((minorUnits, expected) {
      testWidgets('$minorUnits paise renders $expected', (tester) async {
        await tester.pumpWidget(
          ahHarness(MoneyValue(minorUnits: minorUnits, compact: true)),
        );
        expect(rendered(tester), expected);
      });
    });
  });

  group('the written form', () {
    const cases = <int, String>{
      // Indian grouping: the last three digits, then pairs. Western `₹847,350` would mark the
      // product as foreign on the first screen.
      84735000: '₹8,47,350',
      100000000: '₹10,00,000',
      1000000000: '₹1,00,00,000',
      9450000: '₹94,500',
      // Paise appear only when they exist. A vehicle price never carries them; a ledger line does.
      123456: '₹1,234.56',
      100050: '₹1,000.50',
      0: '₹0',
    };

    cases.forEach((minorUnits, expected) {
      testWidgets('$minorUnits paise renders $expected', (tester) async {
        await tester.pumpWidget(ahHarness(MoneyValue(minorUnits: minorUnits)));
        expect(rendered(tester), expected);
      });
    });
  });

  group('typography', () {
    testWidgets('the rupee sign sits at 0.85em so the digits hold the line', (tester) async {
      await tester.pumpWidget(
        ahHarness(const MoneyValue(minorUnits: 84000000, compact: true)),
      );

      final span = tester
          .widget<Text>(
            find.descendant(of: find.byType(MoneyValue), matching: find.byType(Text)),
          )
          .textSpan! as TextSpan;
      final symbol = span.children![0] as TextSpan;
      final digits = span.children![1] as TextSpan;

      expect(symbol.text, '₹');
      expect(digits.text, '8.4L');
      expect(symbol.style!.fontSize, closeTo(digits.style!.fontSize! * 0.85, 0.001));
    });

    testWidgets('the sign travels with the symbol, not with the digits', (tester) async {
      await tester.pumpWidget(
        ahHarness(const MoneyValue(minorUnits: -9450000, compact: true)),
      );

      final span = tester
          .widget<Text>(
            find.descendant(of: find.byType(MoneyValue), matching: find.byType(Text)),
          )
          .textSpan! as TextSpan;

      expect((span.children![0] as TextSpan).text, '-₹');
      expect((span.children![1] as TextSpan).text, '94,500');
    });

    testWidgets('figures are tabular and the zero is slashed', (tester) async {
      await tester.pumpWidget(ahHarness(const MoneyValue(minorUnits: 100000000)));

      final span = tester
          .widget<Text>(
            find.descendant(of: find.byType(MoneyValue), matching: find.byType(Text)),
          )
          .textSpan! as TextSpan;
      final features = (span.children![1] as TextSpan).style!.fontFeatures!;

      // A column of prices must align on the decimal, and a dealer transcribing an amount must
      // not have to decide whether a glyph is a zero or a letter O.
      expect(features.map((f) => f.feature), containsAll(<String>['tnum', 'zero']));
    });

    testWidgets('never wraps — a price that reflows is a price that moved', (tester) async {
      await tester.pumpWidget(ahHarness(const MoneyValue(minorUnits: 1000000000)));

      final text = tester.widget<Text>(
        find.descendant(of: find.byType(MoneyValue), matching: find.byType(Text)),
      );
      expect(text.maxLines, 1);
      expect(text.softWrap, isFalse);
    });
  });

  group('accessibility', () {
    testWidgets('announces the whole amount as one node', (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(
        ahHarness(const MoneyValue(minorUnits: 84000000, compact: true)),
      );

      // The two-span split is a typographic device. A screen reader must not hear "rupee" and
      // "8.4L" as two separate things.
      expect(find.bySemanticsLabel('₹8.4L'), findsOneWidget);
      handle.dispose();
    });
  });
}
