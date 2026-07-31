import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_customer/src/features/home/home_screen.dart';
import 'package:autohub_customer/src/features/valuation/valuation_screen.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures/registry.dart';
import 'harness.dart';

/// The screen the product is judged on.
///
/// Two claims are checked here and both are structural rather than cosmetic:
///
/// 1. **The page opens with an empty plate**, not a photograph and a headline. If somebody ever
///    "improves" this into a hero image with a call to action, this file fails.
/// 2. **The answer is revealed in place.** No route is pushed between the question and the answer,
///    because a page load in the middle of that moment is the difference between a product that
///    feels instant and one that feels like a form. The assertion is deliberately about the
///    *absence* of `ValuationScreen`, which is the only way to tell "revealed here" from "navigated
///    somewhere that looks the same".
void main() {
  appTest('opens with an empty plate under the question', (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    await pumpApp(tester, rig);

    expect(find.byType(PlateInput), findsOneWidget);
    expect(find.text('What is your car worth today?'), findsOneWidget);
    // The promise that makes the plate worth typing into. Removing it is a conversion decision, not
    // a copy edit.
    expect(find.text('Free · 30 seconds · no phone number needed'), findsOneWidget);
    expect(find.text('Get valuation'), findsOneWidget);

    // Nothing competes with the input. A hero photograph or a testimonial carousel would show up
    // as an Image on this screen; the live stock strip is empty in this rig.
    expect(find.byType(Image), findsNothing);
  });

  appTest('the button does nothing until a plate could be a plate',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.quote = ValuationQuote.fromJson(valuationQuoteJson);
    await pumpApp(tester, rig);

    await tester.enterText(find.byType(TextField).first, 'MH1');
    await tester.pump();
    await tester.tap(find.text('Get valuation'));
    await tester.pumpAndSettle();

    expect(rig.api.idempotencyKeys, isEmpty, reason: 'three characters is not a registration');
    expect(find.byType(OdometerValue), findsNothing);
  });

  appTest('submitting reveals the valuation in place, without navigating',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.quote = ValuationQuote.fromJson(valuationQuoteJson);
    await pumpApp(tester, rig);

    await tester.enterText(find.byType(TextField).first, 'MH12AB1234');
    await tester.pump();
    await tester.tap(find.text('Get valuation'));
    await tester.pumpAndSettle();

    // The answer, on the primary figure, with the one animation in the product.
    expect(find.byType(OdometerValue), findsOneWidget);
    expect(find.text('2021 Maruti Suzuki Swift VXi'), findsOneWidget);

    // In place: the hero's own subtree was replaced, and no route was pushed.
    expect(find.byType(HomeScreen), findsOneWidget);
    expect(find.byType(ValuationScreen), findsNothing);
    expect(find.byType(PlateInput), findsNothing);

    // The plate the user typed is echoed on the answer, so they can see it is their car.
    expect(find.byType(PlateBlock), findsWidgets);
  });

  appTest('the quote is saved to the phone as it is revealed',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.quote = ValuationQuote.fromJson(valuationQuoteJson);
    await pumpApp(tester, rig);

    await tester.enterText(find.byType(TextField).first, 'MH12AB1234');
    await tester.pump();
    await tester.tap(find.text('Get valuation'));
    await tester.pumpAndSettle();

    // A user who backgrounds the app on the reveal and comes back an hour later must find the
    // number in their garage rather than typing the plate again — and `/valuation/{id}` has to
    // resolve from this, because the contract pins no endpoint that hands a quote back.
    final DevicePreferences preferences = rig.container.read(devicePreferencesProvider);
    expect(await preferences.read('valuation.index'), contains('019fab0c-quote'));
  });

  appTest('a rejected plate says so inside the plate, and keeps the machine code off screen',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.quoteError = const ApiException(
      statusCode: 404,
      code: 'catalog.vahan.record_not_found',
      message: "This registration number isn't in the RTO records. Check the digits.",
    );

    await pumpApp(tester, rig);
    await tester.enterText(find.byType(TextField).first, 'MH12AB9999');
    await tester.pump();
    await tester.tap(find.text('Get valuation'));
    await tester.pumpAndSettle();

    // Still on the question, with the fix four characters away.
    expect(find.byType(PlateInput), findsOneWidget);
    expect(
      find.text("This registration number isn't in the RTO records. Check the digits."),
      findsOneWidget,
    );
    // The code is for support, not for the person standing in a car park.
    expect(find.textContaining('catalog.vahan.record_not_found'), findsNothing);
  });
}
