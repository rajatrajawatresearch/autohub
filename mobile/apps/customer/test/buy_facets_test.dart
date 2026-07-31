import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_customer/src/app/router.dart';
import 'package:autohub_customer/src/features/buy/facet_sheet.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures/registry.dart';
import 'harness.dart';

/// Search, and the filters that must never be written in Dart.
///
/// Facets arrive inside the search response, generated from the taxonomy registry — their codes,
/// their labels, their options, their counts and the style they render as. The tests here check the
/// two failure modes that matter:
///
/// * a facet the registry publishes **appears**, without this binary knowing its name;
/// * a facet *style* this build has never shipped is **skipped**, not thrown on. That trade was
///   already paid for once on the web, where a strict union turned one unknown facet into an empty
///   marketplace for every user whose results happened to include it.
void main() {
  SearchResponse responseWith({List<Object?> items = const <Object?>[]}) {
    return SearchResponse.fromJson(<String, Object?>{
      'items': items,
      'facets': facetsJson,
      'totalEstimate': items.length,
    });
  }

  Future<void> openBuy(WidgetTester tester, TestRig rig) async {
    await pumpApp(tester, rig);
    rig.container.read(routerProvider).go(Routes.buy);
    await tester.pumpAndSettle();
  }

  appTest('the filter sheet is built from the response, and skips a style it does not know',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.searchResponse = responseWith(items: <Object?>[searchItemJson]);
    await openBuy(tester, rig);

    await tester.tap(find.text('Filters'));
    await tester.pumpAndSettle();

    expect(find.byType(FacetSheet), findsOneWidget);

    // Straight out of the taxonomy registry. Neither the label nor the option is written anywhere
    // in this binary.
    expect(find.text('FUEL'), findsOneWidget);
    expect(find.text('Petrol'), findsOneWidget);
    expect(find.text('Electric'), findsOneWidget);
    expect(find.text('KMS DRIVEN'), findsOneWidget);

    // `histogram` is a style this build has never shipped. One missing filter, not a dead screen.
    expect(find.textContaining('Price distribution'), findsNothing);
  });

  appTest('choosing a facet value reaches the query in the contract grammar',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.searchResponse = responseWith(items: <Object?>[searchItemJson]);
    await openBuy(tester, rig);

    await tester.tap(find.text('Filters'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Electric'));
    await tester.pumpAndSettle();

    // `attr.fuel_type=ev` — §9's grammar, and the same string the web builds, so a filtered link
    // means the same thing on both clients.
    expect(rig.api.searches.last.toQuery()['attr.fuel_type'], 'ev');
  });

  appTest('an empty result set is an instruction, not a mood',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.searchResponse = responseWith();
    await openBuy(tester, rig);

    expect(find.text('No vehicles match these filters'), findsOneWidget);
    // Unfiltered, the honest next move is to sell rather than to widen a filter that is not set.
    expect(
      find.text('Nothing is listed in this city yet. Sell yours, or check another city.'),
      findsOneWidget,
    );
  });

  appTest('a search that fails offers a retry and keeps the code off screen',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.searchError = const ApiException(
      statusCode: 0,
      code: ApiErrorCodes.unreachable,
      message: 'Cannot reach AutoHub. Check your connection and try again.',
    );
    await openBuy(tester, rig);

    // An outage is not a rejection, and the title says so.
    expect(find.text('No connection'), findsOneWidget);
    expect(find.text('Try again'), findsOneWidget);
    expect(find.textContaining(ApiErrorCodes.unreachable), findsNothing);

    rig.api
      ..searchError = null
      ..searchResponse = responseWith(items: <Object?>[searchItemJson]);
    await tester.tap(find.text('Try again'));
    await tester.pumpAndSettle();

    expect(find.text('2021 Maruti Suzuki Swift VXi'), findsOneWidget);
  });

  appTest('the next page is asked for with the cursor the last one returned',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.searchResponse = SearchResponse.fromJson(<String, Object?>{
      'items': <Object?>[searchItemJson],
      'facets': facetsJson,
      'nextCursor': 'eyJpZCI6IjAxOWZ',
      'totalEstimate': 40,
    });
    await openBuy(tester, rig);

    await tester.tap(find.text('Show more'));
    await tester.pumpAndSettle();

    // Dropping the cursor is the single easiest way to build an infinite list that re-fetches page
    // one forever.
    expect(rig.api.searches.last.cursor, 'eyJpZCI6IjAxOWZ');
  });
}
