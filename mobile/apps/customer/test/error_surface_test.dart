import 'package:autohub_customer/src/app/error_surface.dart';
import 'package:autohub_customer/src/app/fault.dart';
import 'package:autohub_customer/src/data/app_providers.dart';
import 'package:autohub_customer/src/i18n/customer_text.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// The error surface, and the line it draws.
///
/// A customer standing in a car park cannot act on `catalog.vahan.record_not_found`. Support cannot
/// act on "that did not go through". So the resting screen carries the sentence and the copy action
/// carries the code — and the assertion that the code is *absent* is the one that stops somebody
/// helpfully adding it back into the body text six months from now.
void main() {
  const AppFault fault = AppFault(
    code: 'catalog.vahan.record_not_found',
    message: "This registration number isn't in the RTO records. Check the digits.",
    statusCode: 404,
    traceId: '00-4bf92f-01',
  );

  Future<void> pumpSurface(WidgetTester tester, Widget child) {
    return tester.pumpWidget(
      ProviderScope(
        // The surface only needs words. Overriding the resolved strings keeps this file from
        // dragging a database, a registry cache and a locale stream in behind one widget.
        overrides: <Override>[appStringsProvider.overrideWithValue(const AppStrings())],
        child: MaterialApp(
          theme: AhTheme.light(),
          home: Scaffold(body: Center(child: child)),
        ),
      ),
    );
  }

  testWidgets('shows the sentence and never the code', (WidgetTester tester) async {
    await pumpSurface(tester, const AppErrorView(fault: fault));

    expect(
      find.text("This registration number isn't in the RTO records. Check the digits."),
      findsOneWidget,
    );
    expect(find.textContaining('catalog.vahan.record_not_found'), findsNothing);
    expect(find.textContaining('00-4bf92f-01'), findsNothing);
  });

  testWidgets('copies code, status and trace for support, and confirms what it copied',
      (WidgetTester tester) async {
    final List<String> clipboard = <String>[];
    tester.binding.defaultBinaryMessenger.setMockMethodCallHandler(
      SystemChannels.platform,
      (MethodCall call) async {
        if (call.method == 'Clipboard.setData') {
          final Object? arguments = call.arguments;
          if (arguments is Map) clipboard.add('${arguments['text']}');
        }
        return null;
      },
    );
    addTearDown(
      () => tester.binding.defaultBinaryMessenger
          .setMockMethodCallHandler(SystemChannels.platform, null),
    );

    await pumpSurface(tester, const AppErrorView(fault: fault));
    await tester.tap(find.text(defaultCustomerStrings[CustomerText.errorCopy]!));
    await tester.pumpAndSettle();

    // The order support reads them in. No message — that is whatever the registry said this week,
    // and the code is what the logs are indexed by.
    expect(clipboard, <String>['catalog.vahan.record_not_found · 404 · 00-4bf92f-01']);

    // Visible for exactly as long as it takes to screenshot, because a support agent asked for it.
    expect(find.textContaining('catalog.vahan.record_not_found'), findsOneWidget);
  });

  testWidgets('an outage gets its own title, because it is not the user being wrong',
      (WidgetTester tester) async {
    const AppFault offline = AppFault(
      code: 'common.network.unreachable',
      message: 'AutoHub cannot be reached.',
      isOffline: true,
    );

    await pumpSurface(tester, const AppErrorView(fault: offline));

    expect(find.text(defaultCustomerStrings[CustomerText.errorOfflineTitle]!), findsOneWidget);
    expect(find.text(defaultCustomerStrings[CustomerText.errorTitle]!), findsNothing);
  });

  test('the reference degrades to whatever the failure actually carried', () {
    const AppFault bare = AppFault(code: 'app.unexpected', message: 'Try again.');
    expect(bare.reference, 'app.unexpected');
    expect(fault.reference, 'catalog.vahan.record_not_found · 404 · 00-4bf92f-01');
  });

  test('a registry-authored sentence outranks the server detail, and both outrank the fallback',
      () {
    const AppStrings authored = AppStrings();

    // Nothing authored for this code, so the server's own resolved detail is used — it is the only
    // source that can describe a failure only the server can see.
    final AppFault withDetail = AppFault.coded(
      code: 'listing.already_listed',
      strings: authored,
      detail: 'This registration is already listed by another seller.',
    );
    expect(withDetail.message, 'This registration is already listed by another seller.');

    // Nothing authored and nothing said: the generic sentence, never a stack trace and never empty.
    final AppFault bare = AppFault.coded(code: 'listing.already_listed', strings: authored);
    expect(bare.message, defaultCustomerStrings[CustomerText.errorBody]);
    expect(bare.message, isNotEmpty);
  });
}
