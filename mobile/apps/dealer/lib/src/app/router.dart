import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/auction/auction_floor_screen.dart';
import '../features/auction/lot_screen.dart';
import '../features/dashboard/dashboard_screen.dart';
import '../features/inventory/inventory_screen.dart';
import '../features/inventory/stock_detail_screen.dart';
import '../features/leads/leads_screen.dart';
import '../features/wallet/wallet_screen.dart';
import 'console_shell.dart';
import 'routes.dart';

final GlobalKey<NavigatorState> _rootNavigator = GlobalKey<NavigatorState>();

/// The console's navigation graph.
///
/// A `StatefulShellRoute` rather than five independent stacks, because each tab keeps its own
/// history: a dealer three levels into a lot who checks their wallet and comes back must land on
/// that lot. Losing that is the difference between an app they work in and one they restart.
///
/// The detail screens are **sub-routes of their branch**, not root-level pushes. That is what makes
/// `/auction/{lotId}` a real deep link: an outbid push notification opens the lot *with the auction
/// tab selected and the floor behind it*, so the back gesture goes somewhere sensible instead of
/// dropping the dealer out of the app.
GoRouter buildDealerRouter() {
  return GoRouter(
    navigatorKey: _rootNavigator,
    initialLocation: DealerRoutes.dashboard,
    routes: <RouteBase>[
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) => ConsoleShell(shell: navigationShell),
        branches: <StatefulShellBranch>[
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: DealerRoutes.dashboard,
                builder: (context, state) => const DealerDashboard(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: DealerRoutes.inventory,
                builder: (context, state) => const InventoryScreen(),
                routes: <RouteBase>[
                  GoRoute(
                    path: ':listingId',
                    builder: (context, state) => StockDetailScreen(
                      // Non-null by construction: the pattern only matches when the segment is
                      // present. The fallback exists so a malformed link renders an empty detail
                      // page rather than throwing during a route build, which go_router surfaces as
                      // a red screen the dealer cannot leave.
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
                path: DealerRoutes.auction,
                builder: (context, state) => const AuctionFloorScreen(),
                routes: <RouteBase>[
                  GoRoute(
                    path: ':lotId',
                    builder: (context, state) => LotScreen(
                      lotId: state.pathParameters['lotId'] ?? '',
                    ),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: DealerRoutes.leads,
                builder: (context, state) => const LeadsScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: DealerRoutes.wallet,
                builder: (context, state) => const WalletScreen(),
              ),
            ],
          ),
        ],
      ),
    ],
    errorBuilder: (context, state) => ConsoleNotFound(location: state.uri.toString()),
  );
}

/// The router, held for the life of the app.
///
/// A provider rather than a field on the widget so it survives a hot reload of the app shell and so
/// a test can override it with a router pointed at one screen. Rebuilding a `GoRouter` throws away
/// every branch's history, which a dealer experiences as the app forgetting where they were.
final Provider<GoRouter> dealerRouterProvider = Provider<GoRouter>((ref) {
  final router = buildDealerRouter();
  ref.onDispose(router.dispose);
  return router;
});
