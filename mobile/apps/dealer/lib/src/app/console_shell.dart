import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'providers.dart';
import 'routes.dart';
import 'strings.dart';

/// One destination in the console.
@immutable
final class ConsoleDestination {
  const ConsoleDestination({
    required this.branch,
    required this.labelKey,
    required this.icon,
    this.module,
  });

  /// Index into the router's `StatefulShellRoute` branches. Fixed, so hiding a destination never
  /// renumbers the others.
  final int branch;

  final String labelKey;
  final IconData icon;

  /// The feature-registry module that gates this destination, or null when it is always present.
  final String? module;
}

/// Sections come from the feature registry.
///
/// A dealer in a city where auction has not launched simply does not see the tab — no empty page,
/// no "coming soon", and no code change on the day it does launch. The module names are contract
/// vocabulary from `GET /platform/features`; whether they are *on* is the server's answer,
/// evaluated for this user, and this list never decides it.
const List<ConsoleDestination> kConsoleDestinations = <ConsoleDestination>[
  ConsoleDestination(branch: 0, labelKey: DealerText.navDashboard, icon: Icons.dashboard_outlined),
  ConsoleDestination(branch: 1, labelKey: DealerText.navInventory, icon: Icons.inventory_2_outlined),
  ConsoleDestination(
    branch: 2,
    labelKey: DealerText.navAuction,
    icon: Icons.gavel_outlined,
    module: 'auction',
  ),
  ConsoleDestination(branch: 3, labelKey: DealerText.navLeads, icon: Icons.people_outline),
  ConsoleDestination(
    branch: 4,
    labelKey: DealerText.navWallet,
    icon: Icons.account_balance_wallet_outlined,
  ),
];

/// Which destinations this dealer sees.
///
/// Pure, so the claim it encodes can be tested without a router: a module the feature registry
/// turns off disappears, and the *others keep their branch indices*. A filtered list that
/// renumbered branches would send a dealer tapping "Wallet" to the leads pipeline, which is the
/// kind of bug that only shows up in the one city where the flag is off.
///
/// Until the registry answers, everything shows. Failing **open** is the right direction here: a
/// dealer briefly seeing a tab that turns out to be off costs one tap, and hiding the auction floor
/// from someone whose feature document has not arrived yet costs them a truck.
List<ConsoleDestination> visibleDestinations(Features? features) => <ConsoleDestination>[
      for (final destination in kConsoleDestinations)
        if (destination.module == null ||
            features == null ||
            features.module(destination.module!))
          destination,
    ];

/// The console frame: a branch navigator and a hairline tab bar.
///
/// A hand-built bar rather than Material's `NavigationBar`, for two reasons that are both about
/// this design language. Material animates a pill indicator under the active destination, and this
/// product spends its whole motion budget on the odometer roll. And a destination hidden by the
/// feature registry must not renumber the rest — a fixed `branch` per destination and a visible
/// list computed at build time makes that impossible to get wrong.
class ConsoleShell extends ConsumerWidget {
  const ConsoleShell({required this.shell, super.key});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = ref.watch(stringsProvider);
    final features = ref.watch(featuresProvider).valueOrNull;

    final visible = visibleDestinations(features);

    return Scaffold(
      body: shell,
      bottomNavigationBar: _TabBar(
        destinations: visible,
        activeBranch: shell.currentIndex,
        strings: t,
        onSelected: (branch) => shell.goBranch(
          branch,
          // Tapping the tab you are already on pops back to its root — the standard idiom, and the
          // fastest way out of a lot detail three levels deep on a showroom floor.
          initialLocation: branch == shell.currentIndex,
        ),
      ),
    );
  }
}

class _TabBar extends StatelessWidget {
  const _TabBar({
    required this.destinations,
    required this.activeBranch,
    required this.strings,
    required this.onSelected,
  });

  final List<ConsoleDestination> destinations;
  final int activeBranch;
  final DealerStrings strings;
  final ValueChanged<int> onSelected;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        border: Border(top: BorderSide(color: surfaces.borderHairline)),
      ),
      child: SafeArea(
        top: false,
        child: Row(
          children: <Widget>[
            for (final destination in destinations)
              Expanded(
                child: _Tab(
                  destination: destination,
                  label: strings(destination.labelKey),
                  active: destination.branch == activeBranch,
                  onTap: () => onSelected(destination.branch),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _Tab extends StatelessWidget {
  const _Tab({
    required this.destination,
    required this.label,
    required this.active,
    required this.onTap,
  });

  final ConsoleDestination destination;
  final String label;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final ink = active ? surfaces.accentPrimary : surfaces.textSecondary;

    return Semantics(
      button: true,
      selected: active,
      label: label,
      excludeSemantics: true,
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: onTap,
          splashFactory: NoSplash.splashFactory,
          hoverColor: Colors.transparent,
          focusColor: Colors.transparent,
          highlightColor: Colors.transparent,
          child: DecoratedBox(
            decoration: BoxDecoration(
              // A 2px rule rather than a moving pill. Structure comes from rules in this design
              // system, and a static rule reads at a glance on a showroom floor.
              border: Border(
                top: BorderSide(
                  color: active ? surfaces.accentPrimary : Colors.transparent,
                  width: 2,
                ),
              ),
            ),
            child: ConstrainedBox(
              // Compact everywhere else; never here. `ahMinTouchTarget` is the floor on both
              // densities and a tab bar is the most-tapped surface in the app.
              constraints: const BoxConstraints(minHeight: ahMinTouchTarget + AhSpace.md),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  Icon(destination.icon, size: 20, color: ink),
                  const SizedBox(height: 2),
                  Text(
                    label,
                    style: AhText.label.copyWith(color: ink, fontSize: 10),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// The router's last resort — a URL nothing matched.
///
/// Reached by a deep link this build does not know, which is a normal consequence of a push
/// notification that ships ahead of a release. It says so and offers the way home rather than
/// rendering go_router's stack trace at a dealer.
class ConsoleNotFound extends ConsumerWidget {
  const ConsoleNotFound({required this.location, super.key});

  final String location;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = ref.watch(stringsProvider);

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AhSpace.xl),
          child: Center(
            child: EmptyState(
              title: t(DealerText.somethingFailed),
              body: location,
              action: AhButton(
                label: t(DealerText.navDashboard),
                onPressed: () => context.go(DealerRoutes.dashboard),
                variant: AhButtonVariant.outline,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
