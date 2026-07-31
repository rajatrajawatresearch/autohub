import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// Density is the mechanism that lets the customer binary and the dealer binary share one widget
/// canon. The rule that keeps it from becoming an accessibility regression is that density shrinks
/// ink, never reach — so these tests pin the 44dp floor at the tightest combination the product
/// ships: a `compact` dealer surface with a `sm` control.
void main() {
  Size sizeOf(WidgetTester tester, Finder finder) => tester.getSize(finder);

  group('the 44dp touch target floor', () {
    for (final density in AhDensity.values) {
      for (final size in AhButtonSize.values) {
        testWidgets('${density.name}/${size.name} button is at least 44dp tall', (tester) async {
          await tester.pumpWidget(
            ahHarness(
              AhButton(label: 'Place bid', onPressed: () {}, size: size),
              density: density,
            ),
          );

          final height = sizeOf(tester, find.byType(AhButton)).height;
          expect(
            height,
            greaterThanOrEqualTo(ahMinTouchTarget),
            reason: '${density.name}/${size.name} dropped below the floor at $height',
          );
        });
      }
    }

    testWidgets('a compact sm button paints smaller than it is tapped', (tester) async {
      await tester.pumpWidget(
        ahHarness(
          AhButton(label: 'Reject', onPressed: () {}, size: AhButtonSize.sm),
          density: AhDensity.compact,
        ),
      );

      final tapHeight = sizeOf(tester, find.byType(AhButton)).height;
      final paintedHeight = sizeOf(
        tester,
        find.descendant(of: find.byType(AhButton), matching: find.byType(DecoratedBox)),
      ).height;

      // 44 × 35.2. The visible control is denser and the gesture target is not — which is the
      // whole bargain, and the thing that silently breaks if someone "simplifies" the wrapper.
      expect(paintedHeight, lessThan(tapHeight));
      expect(tapHeight, greaterThanOrEqualTo(ahMinTouchTarget));
    });

    testWidgets('the theme toggle is square and at the floor in both densities', (tester) async {
      for (final density in AhDensity.values) {
        final controller = AhThemeController(store: AhInMemoryThemeStore());
        addTearDown(controller.dispose);

        await tester.pumpWidget(
          ahHarness(AhThemeToggle(controller: controller), density: density),
        );

        final option = find
            .descendant(of: find.byType(AhThemeToggle), matching: find.byType(SizedBox))
            .first;
        final size = sizeOf(tester, option);

        expect(size.height, ahMinTouchTarget);
        expect(size.width, ahMinTouchTarget);
      }
    });
  });

  group('density resolution', () {
    testWidgets('comfortable is the fallback when nothing declares one', (tester) async {
      late AhDensity resolved;
      await tester.pumpWidget(
        Directionality(
          textDirection: TextDirection.ltr,
          child: Builder(
            builder: (context) {
              resolved = context.ahDensity;
              return const SizedBox.shrink();
            },
          ),
        ),
      );

      // Getting this wrong toward comfortable costs whitespace; getting it wrong toward compact
      // costs a mis-tap on a screen that moves money.
      expect(resolved, AhDensity.comfortable);
    });

    testWidgets('the theme sets it for the whole binary', (tester) async {
      late AhDensity resolved;
      await tester.pumpWidget(
        ahHarness(
          Builder(
            builder: (context) {
              resolved = context.ahDensity;
              return const SizedBox.shrink();
            },
          ),
          density: AhDensity.compact,
        ),
      );

      expect(resolved, AhDensity.compact);
    });

    testWidgets('a scope overrides the theme for one subtree', (tester) async {
      late AhDensity outer;
      late AhDensity inner;

      await tester.pumpWidget(
        ahHarness(
          Column(
            children: <Widget>[
              Builder(
                builder: (context) {
                  outer = context.ahDensity;
                  return const SizedBox.shrink();
                },
              ),
              AhDensityScope(
                density: AhDensity.compact,
                child: Builder(
                  builder: (context) {
                    inner = context.ahDensity;
                    return const SizedBox.shrink();
                  },
                ),
              ),
            ],
          ),
        ),
      );

      // The case this exists for: a comfortable customer screen embedding a spec table that is
      // genuinely a data grid.
      expect(outer, AhDensity.comfortable);
      expect(inner, AhDensity.compact);
    });

    testWidgets('a compact row is shorter than a comfortable one', (tester) async {
      double heightAt(AhDensity density) => density.controlHeight;

      expect(heightAt(AhDensity.compact), lessThan(heightAt(AhDensity.comfortable)));

      for (final density in AhDensity.values) {
        await tester.pumpWidget(
          ahHarness(
            AhButton(label: 'Submit', onPressed: () {}),
            density: density,
          ),
        );
        final painted = sizeOf(
          tester,
          find.descendant(of: find.byType(AhButton), matching: find.byType(DecoratedBox)),
        ).height;
        expect(painted, density.controlHeight);
      }
    });
  });

  group('the button variants', () {
    testWidgets('yellow always takes dark text — white fails contrast at every size',
        (tester) async {
      for (final brightness in Brightness.values) {
        await tester.pumpWidget(
          ahHarness(
            AhButton(
              label: 'Place bid',
              onPressed: () {},
              variant: AhButtonVariant.action,
            ),
            brightness: brightness,
          ),
        );

        final decoration = tester
            .widget<DecoratedBox>(
              find.descendant(of: find.byType(AhButton), matching: find.byType(DecoratedBox)),
            )
            .decoration as BoxDecoration;
        final label = tester.widget<Text>(
          find.descendant(of: find.byType(AhButton), matching: find.byType(Text)),
        );

        expect(decoration.color, AhColors.signal);
        expect(label.style!.color, AhColors.tarmac);
      }
    });

    testWidgets('a null handler disables rather than silently doing nothing', (tester) async {
      await tester.pumpWidget(
        ahHarness(const AhButton(label: 'Submit', onPressed: null)),
      );

      await tester.tap(find.byType(AhButton), warnIfMissed: false);
      await tester.pump();
      expect(tester.takeException(), isNull);

      final opacity = tester.widget<Opacity>(
        find.descendant(of: find.byType(AhButton), matching: find.byType(Opacity)),
      );
      expect(opacity.opacity, lessThan(1));
    });

    testWidgets('an icon-only button keeps an accessible name', (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(
        ahHarness(
          AhButton(
            label: 'Filter results',
            onPressed: () {},
            size: AhButtonSize.icon,
            icon: Icons.tune,
          ),
        ),
      );

      // The label is not painted at this size, which is exactly why it is still required.
      expect(find.text('Filter results'), findsNothing);
      expect(find.bySemanticsLabel('Filter results'), findsOneWidget);
      handle.dispose();
    });
  });
}
