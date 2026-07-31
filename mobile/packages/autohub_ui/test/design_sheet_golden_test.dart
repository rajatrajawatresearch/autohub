import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Renders the mobile design language to a PNG that a person can look at.
///
/// This exists because the usual ways of seeing a Flutter app are both closed here: there is no
/// Android SDK or Xcode on the build machine, and the web target cannot compile the app at all —
/// drift's SQLite reaches `dart:ffi`, which does not exist on web. A golden is the remaining honest
/// answer, and it is arguably the better one for a design review: it is deterministic, it is diffed
/// on every run, and a change to the plate's tracking shows up as a failing test rather than as
/// something nobody noticed.
///
/// Regenerate after an intentional design change:
///   cd mobile/packages/autohub_ui && flutter test --update-goldens
void main() {
  testWidgets('the canon, at handset width', (WidgetTester tester) async {
    tester.view
      ..physicalSize = const Size(390 * 3, 1180 * 3)
      ..devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(const _Sheet());
    await tester.pumpAndSettle();

    await expectLater(
      find.byType(_Sheet),
      matchesGoldenFile('goldens/design_sheet.png'),
    );
  });
}

class _Sheet extends StatelessWidget {
  const _Sheet();

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      theme: AhTheme.light(),
      home: Builder(
        builder: (BuildContext context) {
          final AhSurfaces surfaces = Theme.of(context).extension<AhSurfaces>()!;

          return Material(
            color: surfaces.page,
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(AhSpace.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  const Text('What is your car\nworth today?', style: AhText.displayLg),
                  const SizedBox(height: AhSpace.xl),

                  // The signature, in every class that exists on an Indian road. The colours are
                  // information, not decoration — a commercial plate is yellow in a car park too.
                  const _Label('Plate classes'),
                  const Wrap(
                    spacing: AhSpace.sm,
                    runSpacing: AhSpace.sm,
                    children: <Widget>[
                      PlateBlock(registration: 'MH12AB1234'),
                      PlateBlock(registration: 'MH12WZ4410', plateClass: PlateClass.commercial),
                      PlateBlock(registration: 'MH14QR2091', plateClass: PlateClass.ev),
                      PlateBlock(registration: 'DL01CG0001', plateClass: PlateClass.government),
                      PlateBlock(registration: '12A123456', plateClass: PlateClass.defence),
                    ],
                  ),

                  const SizedBox(height: AhSpace.xl),
                  const _Label('Sizes'),
                  const Wrap(
                    spacing: AhSpace.sm,
                    runSpacing: AhSpace.sm,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: <Widget>[
                      PlateBlock(registration: 'MH12AB1234', size: PlateSize.xs),
                      PlateBlock(registration: 'MH12AB1234', size: PlateSize.sm),
                      PlateBlock(registration: 'MH12AB1234', size: PlateSize.md),
                      PlateBlock(registration: 'MH12AB1234', size: PlateSize.lg),
                    ],
                  ),

                  const SizedBox(height: AhSpace.xl),
                  const _Label('Money — lakh and crore, never thousands'),
                  const Row(
                    children: <Widget>[
                      MoneyValue(minorUnits: 9450000, compact: true),
                      SizedBox(width: AhSpace.lg),
                      MoneyValue(minorUnits: 84735000, compact: true),
                      SizedBox(width: AhSpace.lg),
                      MoneyValue(minorUnits: 12000000000, compact: true),
                    ],
                  ),
                  const SizedBox(height: AhSpace.sm),
                  const MoneyValue(minorUnits: 84735000),

                  const SizedBox(height: AhSpace.xl),
                  const _Label('Status — the category decides the tone, never the server'),
                  const Wrap(
                    spacing: AhSpace.sm,
                    runSpacing: AhSpace.sm,
                    children: <Widget>[
                      StatusPill(category: AhStateCategory.draft, label: 'Draft'),
                      StatusPill(category: AhStateCategory.awaitingAction, label: 'Under review'),
                      StatusPill(category: AhStateCategory.inProgress, label: 'Live'),
                      StatusPill(category: AhStateCategory.succeeded, label: 'Sold'),
                      StatusPill(category: AhStateCategory.failed, label: 'Rejected'),
                      StatusPill(category: AhStateCategory.cancelled, label: 'Withdrawn'),
                    ],
                  ),

                  const SizedBox(height: AhSpace.xl),
                  const _Label('Facts — the RC-card atom'),
                  const SpecStrip(
                    items: <SpecItem>[
                      SpecItem(label: 'Year', value: '2021'),
                      SpecItem(label: 'Kms', value: '38,400'),
                      SpecItem(label: 'Fuel', value: 'Petrol'),
                      SpecItem(label: 'Owners', value: '1st'),
                    ],
                  ),

                  const SizedBox(height: AhSpace.xl),
                  const _Label('Actions — yellow is rationed to one'),
                  Row(
                    children: <Widget>[
                      Expanded(
                        child: AhButton(
                          label: 'Get valuation',
                          variant: AhButtonVariant.action,
                          onPressed: () {},
                        ),
                      ),
                      const SizedBox(width: AhSpace.sm),
                      Expanded(
                        child: AhButton(label: 'Browse', variant: AhButtonVariant.outline, onPressed: () {}),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

class _Label extends StatelessWidget {
  const _Label(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = Theme.of(context).extension<AhSurfaces>()!;
    return Padding(
      padding: const EdgeInsets.only(bottom: AhSpace.sm),
      child: Text(
        text.toUpperCase(),
        style: AhText.label.copyWith(color: surfaces.textSecondary),
      ),
    );
  }
}
