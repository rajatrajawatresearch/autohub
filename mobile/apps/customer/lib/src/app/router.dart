import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../data/session.dart';
import '../features/buy/buy_screen.dart';
import '../features/garage/garage_screen.dart';
import '../features/home/home_screen.dart';
import '../features/listing/listing_screen.dart';
import '../features/login/login_screen.dart';
import '../features/sell/sell_screen.dart';
import '../features/valuation/valuation_screen.dart';
import 'shell.dart';

/// Every address in the customer app.
///
/// Named rather than typed as a `String` at each call site, because these are the strings a push
/// notification, a WhatsApp share and an email campaign all point at — and a typo in one of them is
/// a link that opens the home screen instead of the lot somebody just outbid you on.
abstract final class Routes {
  static const String home = '/';
  static const String buy = '/buy';
  static const String sell = '/sell';
  static const String garage = '/garage';
  static const String login = '/login';

  /// `/buy/{listingId}` — the same address the web uses, so one link works on both.
  static String listing(String listingId) => '$buy/${Uri.encodeComponent(listingId)}';

  /// `/valuation/{quoteId}` — a reveal reached from a saved quote rather than from the hero.
  static String valuation(String quoteId) => '/valuation/${Uri.encodeComponent(quoteId)}';

  /// `/buy?intent={intent}` — rent and attach are the same search screen with a different intent,
  /// which is why there is no third results implementation.
  static String buyWithIntent(ListingIntent intent) =>
      intent == ListingIntent.sale ? buy : '$buy?intent=${intent.wire}';
}

final GlobalKey<NavigatorState> _rootNavigatorKey = GlobalKey<NavigatorState>();

/// The router.
///
/// Deep-linkable throughout, which is not a nicety on a phone: a push notification about a listing
/// must open *that listing*, and a search a customer sends a friend on WhatsApp must open the same
/// results. Every screen that has an identity has a URL, and the two that do not — the facet sheet,
/// the offer sheet — are modals over a screen that does.
///
/// Detail pages sit above the shell rather than inside a branch. A listing owns the full height of
/// the phone: its sticky action bar is the most consequential thing on screen and a tab bar under
/// it would compete with it for the thumb.
final Provider<GoRouter> routerProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    navigatorKey: _rootNavigatorKey,
    initialLocation: Routes.home,
    routes: <RouteBase>[
      StatefulShellRoute.indexedStack(
        builder: (BuildContext context, GoRouterState state, StatefulNavigationShell shell) =>
            CustomerShell(navigationShell: shell),
        branches: <StatefulShellBranch>[
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: Routes.home,
                builder: (BuildContext context, GoRouterState state) => const HomeScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: Routes.buy,
                builder: (BuildContext context, GoRouterState state) => BuyScreen(
                  // The query grammar is the contract's, so a link built by the web opens here
                  // unchanged. An intent this build has no screen for degrades to `sale` rather
                  // than failing the route — see `ListingIntent.fromWire`.
                  intent: ListingIntent.fromWire(state.uri.queryParameters['intent']),
                ),
                routes: <RouteBase>[
                  GoRoute(
                    path: ':listingId',
                    parentNavigatorKey: _rootNavigatorKey,
                    builder: (BuildContext context, GoRouterState state) => ListingScreen(
                      listingId: state.pathParameters['listingId'] ?? '',
                    ),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: Routes.sell,
                builder: (BuildContext context, GoRouterState state) => const SellScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: Routes.garage,
                builder: (BuildContext context, GoRouterState state) => const GarageScreen(),
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: '/valuation/:quoteId',
        parentNavigatorKey: _rootNavigatorKey,
        builder: (BuildContext context, GoRouterState state) {
          // The quote travels in `extra` when the reveal was reached in-app, so returning to it
          // costs no network and works in a lift. A cold deep link has no `extra` and falls back to
          // the copy this phone saved when it took the quote.
          final Object? extra = state.extra;
          return ValuationScreen(
            quoteId: state.pathParameters['quoteId'] ?? '',
            quote: extra is ValuationQuote ? extra : null,
          );
        },
      ),
      GoRoute(
        path: Routes.login,
        parentNavigatorKey: _rootNavigatorKey,
        redirect: (BuildContext context, GoRouterState state) =>
            // Signing in twice is not a state. A deep link to the login screen from a signed-in
            // device is somebody who wanted their account, so send them there.
            ref.read(sessionProvider) is SignedIn ? Routes.garage : null,
        builder: (BuildContext context, GoRouterState state) => const LoginScreen(),
      ),
    ],
  );
});
