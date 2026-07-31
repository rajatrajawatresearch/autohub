import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_partner/src/app/partner_router.dart';
import 'package:autohub_partner/src/app/partner_shell.dart';
import 'package:autohub_partner/src/app/partner_side.dart';
import 'package:autohub_partner/src/app/partner_text.dart';
import 'package:autohub_partner/src/data/partner_providers.dart';
import 'package:autohub_partner/src/screens/onboarding_screen.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// Onboarding and the shape of the app either side of it.
///
/// The two sides of this market share four of five tabs, and that is the evidence that attachment
/// is one module rather than two verticals sharing a name: a match, a contract and a settlement look
/// identical from both ends of the same deal, and only what a partner *publishes* differs.
void main() {
  group('partnerRedirect', () {
    test('sends a device with no side to onboarding, whatever the link said', () {
      // A push notification that arrives before the partner has ever chosen must not drop them into
      // a contract screen belonging to a market they have not identified with.
      expect(partnerRedirect(null, '/contracts/abc'), '/');
      expect(partnerRedirect(null, '/cycles/xyz'), '/');
      expect(partnerRedirect(null, '/'), isNull);
    });

    test('opens each side on its own board', () {
      expect(partnerRedirect(PartnerSide.supply, '/'), '/vehicles');
      expect(partnerRedirect(PartnerSide.demand, '/'), '/requirements');
    });

    test('rewrites a link belonging to the other side rather than showing an empty board', () {
      expect(partnerRedirect(PartnerSide.supply, '/requirements'), '/vehicles');
      expect(partnerRedirect(PartnerSide.supply, '/requirements/new'), '/vehicles');
      expect(partnerRedirect(PartnerSide.demand, '/vehicles'), '/requirements');
      expect(partnerRedirect(PartnerSide.demand, '/vehicles/new'), '/requirements');
    });

    test('leaves the shared surfaces deep-linkable from both sides', () {
      // A settlement statement is the same document from both ends of the same deal, so a
      // notification about a released payout opens it whoever tapped it. Whether they may *see* it
      // is the server's decision, not this function's.
      for (final PartnerSide side in PartnerSide.values) {
        expect(partnerRedirect(side, '/matches'), isNull);
        expect(partnerRedirect(side, '/matches?postId=abc'), isNull);
        expect(partnerRedirect(side, '/contracts/abc'), isNull);
        expect(partnerRedirect(side, '/cycles/abc'), isNull);
        expect(partnerRedirect(side, '/settings'), isNull);
      }
    });
  });

  group('tabsFor', () {
    test('differs in exactly one tab between the two sides', () {
      final List<PartnerTab> supply = tabsFor(PartnerSide.supply);
      final List<PartnerTab> demand = tabsFor(PartnerSide.demand);

      expect(supply.length, demand.length);
      expect(supply.first.location, '/vehicles');
      expect(demand.first.location, '/requirements');

      final List<String> sharedSupply =
          supply.skip(1).map((PartnerTab tab) => tab.location).toList();
      final List<String> sharedDemand =
          demand.skip(1).map((PartnerTab tab) => tab.location).toList();
      expect(sharedSupply, sharedDemand);
      expect(sharedSupply, <String>['/matches', '/contracts', '/settlement', '/settings']);
    });
  });

  group('OnboardingScreen', () {
    late AhDatabase database;

    setUp(() => database = openInMemoryAhDatabase());
    tearDown(() async => database.close());

    testWidgets('offers both sides and spends the rationed yellow on supply', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        partnerHarness(
          const OnboardingScreen(),
          overrides: <Override>[ahDatabaseProvider.overrideWithValue(database)],
        ),
      );

      expect(find.text(defaultPartnerLabels[PartnerKeys.sideSupplyCta]!), findsOneWidget);
      expect(find.text(defaultPartnerLabels[PartnerKeys.sideDemandCta]!), findsOneWidget);

      // One yellow control per screen. Supply is the side this marketplace is short of — a
      // requirement with no vehicles to match is a dead board — so it is the consequential action.
      final Iterable<AhButton> buttons = tester.widgetList<AhButton>(find.byType(AhButton));
      final Iterable<AhButton> yellow =
          buttons.where((AhButton button) => button.variant == AhButtonVariant.action);
      expect(yellow.length, 1);
      expect(yellow.single.label, defaultPartnerLabels[PartnerKeys.sideSupplyCta]);
    });

    testWidgets('remembers the side that was chosen', (WidgetTester tester) async {
      late ProviderContainer container;

      await tester.pumpWidget(
        partnerHarness(
          const OnboardingScreen(),
          overrides: <Override>[ahDatabaseProvider.overrideWithValue(database)],
        ),
      );

      container = ProviderScope.containerOf(
        tester.element(find.byType(OnboardingScreen)),
        listen: false,
      );
      expect(container.read(partnerSideProvider), isNull);

      await tester.tap(find.text(defaultPartnerLabels[PartnerKeys.sideDemandCta]!));
      await tester.pumpAndSettle();

      expect(container.read(partnerSideProvider), PartnerSide.demand);

      // Written through, so the next cold launch skips the chooser instead of asking again.
      final DevicePreferences preferences = container.read(devicePreferencesProvider);
      expect(await preferences.read(PartnerSide.storageKey), 'demand');
    });
  });
}
