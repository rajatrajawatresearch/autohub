import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// The plate input is the first thing a customer touches and the only input on the homepage.
/// Getting a character wrong here does not produce a validation error — it produces a *valuation
/// for a different car*, which the user has no way to detect.
void main() {
  /// The widget is controlled, so the test owns the canonical value the way a page would.
  Widget host({
    String initial = '',
    ValueChanged<String>? onChanged,
    int maxChars = 11,
  }) {
    return _PlateHost(initial: initial, onChanged: onChanged, maxChars: maxChars);
  }

  String displayed(WidgetTester tester) =>
      tester.widget<TextField>(find.byType(TextField)).controller!.text;

  TextSelection selection(WidgetTester tester) =>
      tester.widget<TextField>(find.byType(TextField)).controller!.selection;

  group('canonicalisation', () {
    testWidgets('lowercase becomes uppercase as it is typed', (tester) async {
      var canonical = '';
      await tester.pumpWidget(ahHarness(host(onChanged: (v) => canonical = v)));

      await tester.enterText(find.byType(TextField), 'mh12ab1234');
      await tester.pump();

      expect(canonical, 'MH12AB1234');
      expect(displayed(tester), 'MH 12 AB 1234');
    });

    testWidgets('separators are stripped, whatever the source used', (tester) async {
      // An RC photo, an SMS and a WhatsApp forward all spell it differently. They must all land
      // in the same canonical place.
      const spellings = <String>[
        'MH-12-AB-1234',
        'MH 12 AB 1234',
        'mh.12.ab.1234',
        'MH12AB1234',
      ];

      for (final spelling in spellings) {
        var canonical = '';
        // A fresh key per spelling. Without it the element is reused across iterations, the field
        // still holds the previous plate, and `onChanged` never fires for an identical value —
        // the loop would pass on iteration one and prove nothing after it.
        await tester.pumpWidget(
          ahHarness(
            _PlateHost(key: ValueKey<String>(spelling), initial: '', onChanged: (v) => canonical = v),
          ),
        );
        await tester.enterText(find.byType(TextField), spelling);
        await tester.pump();

        expect(canonical, 'MH12AB1234', reason: '"$spelling" must canonicalise');
        expect(displayed(tester), 'MH 12 AB 1234');
      }
    });

    testWidgets('the BH series groups by its own shape', (tester) async {
      var canonical = '';
      await tester.pumpWidget(ahHarness(host(onChanged: (v) => canonical = v)));

      await tester.enterText(find.byType(TextField), '22bh1234aa');
      await tester.pump();

      // Splitting on runs of letters and digits is what makes this work without a per-format
      // table — and what will make the next series work too.
      expect(canonical, '22BH1234AA');
      expect(displayed(tester), '22 BH 1234 AA');
    });

    testWidgets('clips at the longest registration any Indian series produces', (tester) async {
      var canonical = '';
      await tester.pumpWidget(ahHarness(host(onChanged: (v) => canonical = v)));

      await tester.enterText(find.byType(TextField), 'MH12AB1234567890');
      await tester.pump();

      expect(canonical.length, 11);
      expect(canonical, 'MH12AB12345');
    });
  });

  group('live regrouping', () {
    testWidgets('regroups on every keystroke, not on blur', (tester) async {
      await tester.pumpWidget(ahHarness(host()));

      const steps = <String, String>{
        'm': 'M',
        'mh': 'MH',
        'mh1': 'MH 1',
        'mh12': 'MH 12',
        'mh12a': 'MH 12 A',
        'mh12ab1': 'MH 12 AB 1',
        'mh12ab1234': 'MH 12 AB 1234',
      };

      for (final entry in steps.entries) {
        await tester.enterText(find.byType(TextField), entry.key);
        await tester.pump();
        expect(displayed(tester), entry.value, reason: 'after typing "${entry.key}"');
      }
    });

    testWidgets('a partial plate never shows a trailing separator', (tester) async {
      await tester.pumpWidget(ahHarness(host()));
      await tester.enterText(find.byType(TextField), 'mh12');
      await tester.pump();

      // "MH 12 " would put the caret past a space the user did not type and cannot delete
      // predictably.
      expect(displayed(tester).endsWith(' '), isFalse);
    });
  });

  group('the caret survives regrouping', () {
    testWidgets('stays at the end while typing forward', (tester) async {
      await tester.pumpWidget(ahHarness(host()));
      await tester.enterText(find.byType(TextField), 'mh12ab');
      await tester.pump();

      expect(selection(tester).baseOffset, displayed(tester).length);
    });

    testWidgets('stays put when a digit is corrected mid-string', (tester) async {
      await tester.pumpWidget(ahHarness(host(initial: 'MH12AB1234')));
      await tester.tap(find.byType(TextField));
      await tester.pump();
      expect(displayed(tester), 'MH 12 AB 1234');

      // The user reading a plate off a windscreen puts the caret after "MH 1" and types a 9.
      // The platform sends the whole new string; the formatter regroups it and must not throw
      // the caret to the end, which is what a naive implementation does on every correction.
      tester.testTextInput.updateEditingValue(
        const TextEditingValue(
          text: 'MH 192 AB 1234',
          selection: TextSelection.collapsed(offset: 5),
        ),
      );
      await tester.pump();

      expect(displayed(tester), 'MH 192 AB 1234');
      expect(selection(tester).baseOffset, 5, reason: 'caret sits just after the typed 9');
    });
  });

  group('the field is the plate', () {
    testWidgets('square corners and a hard tarmac border, in both themes', (tester) async {
      for (final brightness in Brightness.values) {
        await tester.pumpWidget(ahHarness(host(), brightness: brightness));

        final decoration =
            tester.widget<TextField>(find.byType(TextField)).decoration!;
        final border = decoration.enabledBorder! as OutlineInputBorder;

        expect(decoration.fillColor, AhColors.plate);
        expect(border.borderSide.color, AhColors.tarmac);
        expect(border.borderSide.width, 2);
        expect(border.borderRadius, const BorderRadius.all(Radius.zero));
      }
    });

    testWidgets('an invalid plate turns the border, not the fill', (tester) async {
      await tester.pumpWidget(
        ahHarness(
          const PlateInput(
            value: 'MH12',
            onChanged: _ignore,
            error: "This registration isn't in the RTO records.",
          ),
        ),
      );

      final decoration = tester.widget<TextField>(find.byType(TextField)).decoration!;
      expect((decoration.enabledBorder! as OutlineInputBorder).borderSide.color, AhColors.flag);
      expect(decoration.fillColor, AhColors.plate);
      expect(find.text("This registration isn't in the RTO records."), findsOneWidget);
    });

    testWidgets('the keyboard suppresses autocorrect and suggestions', (tester) async {
      await tester.pumpWidget(ahHarness(host()));
      final field = tester.widget<TextField>(find.byType(TextField));

      // A registration number is the one string on the phone that must never be "helpfully"
      // corrected into a word.
      expect(field.autocorrect, isFalse);
      expect(field.enableSuggestions, isFalse);
      expect(field.keyboardType, TextInputType.visiblePassword);
      expect(field.textInputAction, TextInputAction.go);
    });
  });

  group('submission', () {
    testWidgets('the Go key hands back the canonical value, not the display', (tester) async {
      String? submitted;
      await tester.pumpWidget(
        ahHarness(_PlateHost(initial: '', onSubmitted: (v) => submitted = v)),
      );

      await tester.enterText(find.byType(TextField), 'mh12ab1234');
      await tester.pump();
      await tester.testTextInput.receiveAction(TextInputAction.go);
      await tester.pump();

      expect(submitted, 'MH12AB1234');
    });
  });
}

void _ignore(String _) {}

/// Owns the canonical value the way a page does, so the controlled contract is exercised rather
/// than bypassed.
class _PlateHost extends StatefulWidget {
  const _PlateHost({
    required this.initial,
    this.onChanged,
    this.onSubmitted,
    this.maxChars = 11,
    super.key,
  });

  final String initial;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;
  final int maxChars;

  @override
  State<_PlateHost> createState() => _PlateHostState();
}

class _PlateHostState extends State<_PlateHost> {
  late String _value = widget.initial;

  @override
  Widget build(BuildContext context) {
    return PlateInput(
      value: _value,
      maxChars: widget.maxChars,
      onChanged: (canonical) {
        setState(() => _value = canonical);
        widget.onChanged?.call(canonical);
      },
      onSubmitted: widget.onSubmitted,
    );
  }
}
