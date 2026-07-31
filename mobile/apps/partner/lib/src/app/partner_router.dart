import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../data/attachment_api.dart';
import '../data/attachment_writes.dart';
import '../data/partner_providers.dart';
import '../screens/contract_screen.dart';
import '../screens/contracts_screen.dart';
import '../screens/cycle_screen.dart';
import '../screens/matches_screen.dart';
import '../screens/my_vehicles_screen.dart';
import '../screens/onboarding_screen.dart';
import '../screens/requirements_screen.dart';
import '../screens/schema_post_screen.dart';
import '../screens/settings_screen.dart';
import '../screens/settlement_screen.dart';
import 'partner_shell.dart';
import 'partner_side.dart';
import 'partner_text.dart';

/// Where a location resolves to, given the side this device is on.
///
/// Pulled out of the `GoRouter` closure and left pure so it can be tested without a widget tree,
/// because it is the piece of navigation with actual consequences: it is what a push notification's
/// deep link and a cold launch both run through, and getting it wrong strands a partner on a board
/// belonging to somebody else's half of the market.
///
/// Returns `null` to mean "no redirect", which is go_router's own convention.
String? partnerRedirect(PartnerSide? side, String location) {
  // No side, no app. Onboarding is one tap and it is the only screen that makes sense before the
  // market knows which end of it you are on. A deep link that arrives first is deliberately
  // dropped rather than queued — the alternative is showing a stranger a contract.
  if (side == null) return location == '/' ? null : '/';

  if (location == '/') {
    return side == PartnerSide.supply ? '/vehicles' : '/requirements';
  }

  // Land a partner on their own board rather than on an empty one belonging to the other side.
  // This is what makes a link from the wrong side degrade gracefully instead of showing a fleet
  // operator a "no vehicles listed" screen they can do nothing about.
  if (side == PartnerSide.supply && location.startsWith('/requirements')) return '/vehicles';
  if (side == PartnerSide.demand && location.startsWith('/vehicles')) return '/requirements';

  // Matches, contracts, settlements and settings are shared. A settlement statement is the same
  // document from both ends of the deal, so a link to one is valid whoever opens it — the server
  // decides whether they may see it, not this function.
  return null;
}

/// Routes, and the one redirect that makes the side choice real.
///
/// Every location is deep-linkable, because the notifications this app will eventually carry are
/// all about one row: a new match, a document about to expire, a payout released. Opening the app
/// on the home screen and asking the partner to find it again is how a notification becomes a
/// notification people turn off.
///
/// Detail routes sit **outside** the shell so they push over it with a back affordance, while the
/// five tab roots sit inside it. A settlement statement opened from a notification should not have
/// a tab bar under it suggesting the partner is browsing.
final Provider<GoRouter> routerProvider = Provider<GoRouter>((ref) {
  // go_router refreshes on a `Listenable`, and Riverpod speaks in providers. This is the adapter,
  // and it is the whole reason the router is a provider rather than a global: switching sides in
  // settings must re-evaluate the redirect immediately, not on the next navigation.
  final ValueNotifier<int> refresh = ValueNotifier<int>(0);
  ref.listen<PartnerSide?>(partnerSideProvider, (PartnerSide? _, PartnerSide? __) {
    refresh.value += 1;
  });
  ref.onDispose(refresh.dispose);

  return GoRouter(
    initialLocation: '/',
    refreshListenable: refresh,
    redirect: (BuildContext context, GoRouterState state) =>
        partnerRedirect(ref.read(partnerSideProvider), state.matchedLocation),
    routes: <RouteBase>[
      GoRoute(
        path: '/',
        builder: (BuildContext context, GoRouterState state) => const OnboardingScreen(),
      ),
      ShellRoute(
        builder: (BuildContext context, GoRouterState state, Widget child) =>
            PartnerShell(child: child),
        routes: <RouteBase>[
          GoRoute(
            path: '/vehicles',
            builder: (BuildContext context, GoRouterState state) => const MyVehiclesScreen(),
          ),
          GoRoute(
            path: '/requirements',
            builder: (BuildContext context, GoRouterState state) => const RequirementsScreen(),
          ),
          GoRoute(
            path: '/matches',
            builder: (BuildContext context, GoRouterState state) =>
                MatchesScreen(postId: state.uri.queryParameters['postId']),
          ),
          GoRoute(
            path: '/contracts',
            builder: (BuildContext context, GoRouterState state) => const ContractsScreen(),
          ),
          GoRoute(
            path: '/settlement',
            builder: (BuildContext context, GoRouterState state) => const SettlementScreen(),
          ),
          GoRoute(
            path: '/settings',
            builder: (BuildContext context, GoRouterState state) => const SettingsScreen(),
          ),
        ],
      ),
      GoRoute(
        path: '/vehicles/new',
        builder: (BuildContext context, GoRouterState state) => SchemaPostScreen(
          formKey: AttachmentApi.supplyFormKey,
          titleKey: PartnerKeys.vehiclesAddTitle,
          buildIntent: (Map<String, Object?> payload, String? city) =>
              publishSupplyPost(payload: payload, activeCity: city),
          doneLocation: '/vehicles',
        ),
      ),
      GoRoute(
        path: '/requirements/new',
        builder: (BuildContext context, GoRouterState state) => SchemaPostScreen(
          formKey: AttachmentApi.demandFormKey,
          titleKey: PartnerKeys.requirementsAddTitle,
          buildIntent: (Map<String, Object?> payload, String? city) =>
              publishDemandPost(payload: payload, activeCity: city),
          doneLocation: '/requirements',
        ),
      ),
      GoRoute(
        path: '/contracts/:id',
        builder: (BuildContext context, GoRouterState state) =>
            ContractScreen(contractId: state.pathParameters['id'] ?? ''),
      ),
      GoRoute(
        path: '/cycles/:id',
        builder: (BuildContext context, GoRouterState state) =>
            CycleScreen(cycleId: state.pathParameters['id'] ?? ''),
      ),
    ],
  );
});
