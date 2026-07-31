import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_dealer/src/app/console_shell.dart';
import 'package:autohub_dealer/src/app/router.dart';
import 'package:autohub_dealer/src/app/routes.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('visibleDestinations', () {
    test('shows everything before the feature registry has answered', () {
      // A cold launch in a basement. Hiding the auction floor from a dealer whose feature document
      // has not arrived costs them a truck; showing a tab that turns out to be off costs one tap.
      expect(visibleDestinations(null), hasLength(kConsoleDestinations.length));
    });

    test('hides a module the server turned off for this dealer', () {
      const features = Features(version: 12, modules: <String, bool>{'auction': false});
      final visible = visibleDestinations(features);

      expect(visible.map((d) => d.labelKey), isNot(contains('dealer.nav.auction')));
      expect(visible, hasLength(kConsoleDestinations.length - 1));
    });

    test('hiding a destination does not renumber the others', () {
      const features = Features(version: 12, modules: <String, bool>{'auction': false});
      final visible = visibleDestinations(features);

      // The branch index is a property of the destination, not its position in the visible list.
      // If filtering ever renumbered them, tapping "Wallet" would open the leads pipeline — and
      // only in the one city where the flag happens to be off.
      final wallet = visible.firstWhere((d) => d.labelKey == 'dealer.nav.wallet');
      expect(wallet.branch, 4);

      final leads = visible.firstWhere((d) => d.labelKey == 'dealer.nav.leads');
      expect(leads.branch, 3);
    });

    test('an unknown module reads as off, because the registry fails closed', () {
      // `Features.module` returns false for anything it has not been told about. A destination
      // gated on a module the server never mentioned is a flow that ends in a support call.
      const features = Features(version: 12);
      final visible = visibleDestinations(features);

      expect(visible.map((d) => d.labelKey), isNot(contains('dealer.nav.auction')));
    });
  });

  group('DealerRoutes', () {
    test('detail paths are built once and encoded, so a deep link cannot be spelled two ways', () {
      expect(DealerRoutes.stock('019fab0c'), '/inventory/019fab0c');
      expect(DealerRoutes.lot('019fab0c'), '/auction/019fab0c');
    });

    test('an id with reserved characters survives the round trip', () {
      // Ids come from the server. Nothing guarantees they stay opaque hex forever, and a raw `/`
      // in a path segment is a route that silently matches something else.
      expect(DealerRoutes.lot('a/b'), '/auction/a%2Fb');
      expect(Uri.decodeComponent('a%2Fb'), 'a/b');
    });

    test('every tab has a path', () {
      expect(DealerRoutes.tabs, hasLength(kConsoleDestinations.length));
    });
  });

  test('the route graph is well formed', () {
    // `GoRouter`'s constructor runs its own assertions over the tree — duplicate paths, a sub-route
    // spelled absolutely, a branch with no routes. Every one of those is a crash on a deep link,
    // which is a crash a dealer hits from a push notification rather than from anything they did.
    final router = buildDealerRouter();
    addTearDown(router.dispose);

    expect(router.configuration.routes, isNotEmpty);
  });
}
