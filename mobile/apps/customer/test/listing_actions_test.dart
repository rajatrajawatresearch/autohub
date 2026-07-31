import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_customer/src/app/router.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures/registry.dart';
import 'harness.dart';

/// The sticky action bar, and the one thing it must never do.
///
/// The primary control on a listing is whatever `workflow.availableActions` says it is, labelled by
/// the server and permitted by the server. On a live listing that action reads *Book test drive* —
/// and this binary does not know that, must not know that, and is checked here for not knowing it:
/// the fixture calls the same action something else, and the button says what the fixture says.
///
/// A client that decided "live listings get a Book test drive button" would disagree with the server
/// the first time operations adds a step to the flow, on handsets that cannot be corrected without
/// an app-store release.
void main() {
  ListingDetail detailWith(List<Object?> actions) {
    return ListingDetail.fromJson(<String, Object?>{
      ...searchItemJson,
      'listingId': '019fab0c-listing',
      'currency': 'INR',
      'gallery': <Object?>[],
      'specGroups': <Object?>[
        <String, Object?>{
          'code': 'engine',
          'label': 'Engine',
          'items': <Object?>[
            <String, Object?>{'label': 'Fuel', 'value': 'Petrol'},
          ],
        },
      ],
      'seller': <String, Object?>{
        'id': 'seller-1',
        'displayName': 'Deccan Motors',
        'type': 'dealer',
        'city': 'Pune',
        'isVerified': true,
      },
      'workflow': <String, Object?>{
        'workflowKey': 'listing',
        'currentState': <String, Object?>{
          'code': 'live',
          'name': 'Live',
          'category': 'in_progress',
        },
        'enteredAt': '2026-07-29T04:10:00Z',
        'availableActions': actions,
        'history': <Object?>[
          <String, Object?>{
            'from': 'draft',
            'to': 'live',
            'actionCode': 'approve',
            'occurredAt': '2026-07-29T04:10:00Z',
            'actor': 'Moderation',
          },
        ],
      },
    });
  }

  Future<void> openListing(WidgetTester tester, TestRig rig) async {
    await pumpApp(tester, rig);
    rig.container.read(routerProvider).push(Routes.listing('019fab0c-listing'));
    await tester.pumpAndSettle();
  }

  appTest('the primary action is the server\'s, with the server\'s word on it',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.listingDetail = detailWith(<Object?>[
      <String, Object?>{
        'actionCode': 'book_test_drive',
        // Deliberately not "Book test drive". If the button reads this, the label came off the
        // wire; if it reads anything else, somebody hardcoded it.
        'label': 'Arrange a viewing',
        'emphasis': 'primary',
      },
      <String, Object?>{
        'actionCode': 'report',
        'label': 'Report this listing',
        'emphasis': 'default',
      },
    ]);

    await openListing(tester, rig);

    expect(find.text('Arrange a viewing'), findsOneWidget);
    // The state's own name and its registry category, rendered as the one status chip.
    expect(find.byType(StatusPill), findsWidgets);
    expect(find.text('LIVE'), findsWidgets);
    // The spec group came from the registry's `displayGroup`, not from a list of vehicle fields.
    expect(find.text('Engine'), findsOneWidget);
  });

  appTest('firing it is a queued write with the key minted on the tap',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.listingDetail = detailWith(<Object?>[
      <String, Object?>{
        'actionCode': 'book_test_drive',
        'label': 'Arrange a viewing',
        'emphasis': 'primary',
      },
    ]);

    await openListing(tester, rig);
    await tester.tap(find.text('Arrange a viewing'));
    await tester.pumpAndSettle();

    final WriteIntent sent = rig.outbox.sent.single;
    expect(sent.endpoint, '/listings/019fab0c-listing/workflow/actions');
    expect(sent.method, WriteMethod.post);
    expect(sent.body?['actionCode'], 'book_test_drive');
    expect(sent.idempotencyKey, isNotEmpty);
  });

  appTest('with no action offered, the bar shows only what is always true',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.listingDetail = detailWith(<Object?>[]);

    await openListing(tester, rig);

    // Making an offer is not a workflow transition — it creates a deal — so it survives a listing
    // the server is currently offering no transitions on.
    expect(find.text('Make an offer'), findsOneWidget);
    // And the price is written in full above it. `₹5.49L` is how India *says* a price and the
    // wrong thing to show next to the control that commits to one — so the accessible name of the
    // figure is the grouped rupee amount, not the spoken short form.
    expect(find.bySemanticsLabel('₹5,49,000'), findsOneWidget);
  });

  appTest('a listing that is gone says so, and offers the way back',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    // `FakeCustomerApi` answers a missing listing with the contract's own `listing.not_found`.
    await openListing(tester, rig);

    expect(find.text('That did not go through'), findsOneWidget);
    expect(find.textContaining('listing.not_found'), findsNothing);
    expect(find.text('Buy'), findsWidgets);
  });
}
