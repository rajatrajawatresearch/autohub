import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_customer/src/app/router.dart';
import 'package:autohub_customer/src/data/sell.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// The registry's proof, on a handset.
///
/// The claim in `docs/10-mobile-architecture.md` §1 is that a field added in an admin console shows
/// up in the Flutter sell flow on the next launch with nothing rebuilt. The fixture this runs
/// against carries a `trailer_axles` field precisely because no vehicle marketplace UI would ever
/// hand-write one: if it renders, the screen is drawing the document rather than a form somebody
/// typed in Dart.
///
/// The second half is survival. A sell form is twenty minutes of somebody's day plus photographs of
/// their car, and §4 is unambiguous: it autosaves on change, flushes when the app is backgrounded,
/// and is recovered on relaunch **without asking**.
void main() {
  Future<void> openSell(WidgetTester tester, TestRig rig) async {
    await pumpApp(tester, rig);
    rig.container.read(routerProvider).go(Routes.sell);
    await tester.pumpAndSettle();
  }

  appTest('renders whatever the form registry returned, including a field nobody wrote',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    await openSell(tester, rig);

    // The document's own step title, and its identity in mono at the foot of the form — when a
    // seller reports a field behaving oddly, the first question is which version they were on.
    expect(find.text('Your vehicle'), findsOneWidget);
    expect(find.text('sell_vehicle · v7'), findsOneWidget);

    // The proof. Nothing in this binary knows what a trailer axle is.
    expect(find.text('TRAILER AXLES'), findsOneWidget);
    expect(find.text('Only for goods carriers.'), findsOneWidget);

    // And it was fetched by key rather than constructed.
    expect(
      rig.registry.requested,
      contains(RegistryDocuments.form(sellFormKey).name),
    );
  });

  appTest('what the seller typed survives the app being backgrounded',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    await openSell(tester, rig);

    await tester.enterText(find.byType(TextField).first, 'MH14CD5678');
    await tester.pump();

    // Android kills a backgrounded process without further warning, and a debounce timer does not
    // fire in a process that no longer exists. This is the lifecycle edge that has to write.
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.inactive);
    await tester.pumpAndSettle();

    final FormDraft? draft =
        await rig.container.read(draftStoreProvider).recover(formKey: sellFormKey);
    expect(draft, isNotNull);
    expect(draft!.values['registration_number'], 'MH14CD5678');
    expect(
      draft.schemaVersion,
      7,
      reason: 'the draft records which document it was composed against',
    );
  });

  appTest('and is back in the form on the next launch, without being asked',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    await openSell(tester, rig);

    await tester.enterText(find.byType(TextField).first, 'MH14CD5678');
    await tester.pump();
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.inactive);
    await tester.pumpAndSettle();

    // A new process, the same phone: the widget tree goes, the disk stays.
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pumpAndSettle();
    await openSell(tester, rig);

    // No dialogue offering to restore anything — the user has no idea what is in a draft until
    // they see it, and answering wrong loses the twenty minutes.
    expect(find.byType(Dialog), findsNothing);
    expect(find.text('MH 14 CD 5678'), findsOneWidget);
  });
}
