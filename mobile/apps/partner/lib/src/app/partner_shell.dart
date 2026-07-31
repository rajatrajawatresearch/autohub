import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../data/partner_providers.dart';
import 'partner_side.dart';
import 'partner_text.dart';

/// One tab of the shell.
@immutable
class PartnerTab {
  const PartnerTab({required this.location, required this.labelKey, required this.icon});

  final String location;
  final String labelKey;
  final IconData icon;
}

/// The tabs each side gets.
///
/// The two sides differ in exactly one tab — a supply partner has vehicles where a demand partner
/// has requirements — and share the other four, because a match, a contract and a settlement look
/// the same from both ends of the same deal. That is the shape of the market, and it is why one
/// binary serves both rather than a role switcher on a login screen.
List<PartnerTab> tabsFor(PartnerSide side) => <PartnerTab>[
      switch (side) {
        PartnerSide.supply => const PartnerTab(
            location: '/vehicles',
            labelKey: PartnerKeys.navVehicles,
            icon: Icons.local_taxi_outlined,
          ),
        PartnerSide.demand => const PartnerTab(
            location: '/requirements',
            labelKey: PartnerKeys.navRequirements,
            icon: Icons.assignment_outlined,
          ),
      },
      const PartnerTab(
        location: '/matches',
        labelKey: PartnerKeys.navMatches,
        icon: Icons.compare_arrows_outlined,
      ),
      const PartnerTab(
        location: '/contracts',
        labelKey: PartnerKeys.navContracts,
        icon: Icons.description_outlined,
      ),
      const PartnerTab(
        location: '/settlement',
        labelKey: PartnerKeys.navSettlement,
        icon: Icons.account_balance_outlined,
      ),
      const PartnerTab(
        location: '/settings',
        labelKey: PartnerKeys.navSettings,
        icon: Icons.tune_outlined,
      ),
    ];

/// The tabbed frame, plus the one banner that is genuinely app-wide.
///
/// Read failures belong to the section that issued them and are rendered there by `AsyncSection`.
/// **Write** failures do not: a queued supply post that came back `409` has no screen of its own
/// once the partner has navigated away, and silently leaving it in SQLite is how "where did my
/// listing go" becomes a support ticket. So anything in the outbox that needs a person surfaces
/// here, on every tab, until they deal with it.
class PartnerShell extends ConsumerWidget {
  const PartnerShell({required this.child, super.key});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerSide? side = ref.watch(partnerSideProvider);
    if (side == null) return child;

    final List<PartnerTab> tabs = tabsFor(side);
    final String location = GoRouterState.of(context).uri.path;

    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: Column(
          children: <Widget>[
            const _StuckWritesBanner(),
            Expanded(child: child),
          ],
        ),
      ),
      bottomNavigationBar: _TabBar(tabs: tabs, location: location),
    );
  }
}

class _TabBar extends ConsumerWidget {
  const _TabBar({required this.tabs, required this.location});

  final List<PartnerTab> tabs;
  final String location;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final PartnerText text = ref.watch(partnerTextProvider);

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        // Structure comes from a hairline, never from elevation. The bar is a rule with controls
        // under it, not a floating card.
        border: Border(top: BorderSide(color: surfaces.borderHairline)),
      ),
      child: SafeArea(
        top: false,
        child: Row(
          children: <Widget>[
            for (final PartnerTab tab in tabs)
              Expanded(
                child: _TabButton(
                  tab: tab,
                  label: text(tab.labelKey),
                  selected: location == tab.location || location.startsWith('${tab.location}/'),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _TabButton extends StatelessWidget {
  const _TabButton({required this.tab, required this.label, required this.selected});

  final PartnerTab tab;
  final String label;
  final bool selected;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final Color tint = selected ? surfaces.accentPrimary : surfaces.textSecondary;

    return Semantics(
      button: true,
      selected: selected,
      label: label,
      excludeSemantics: true,
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: selected ? null : () => context.go(tab.location),
          splashFactory: NoSplash.splashFactory,
          hoverColor: Colors.transparent,
          focusColor: Colors.transparent,
          highlightColor: Colors.transparent,
          child: ConstrainedBox(
            // Compact density shortens rows; it never shrinks a target. A fleet operator taps this
            // one-handed while walking a yard.
            constraints: const BoxConstraints(minHeight: ahMinTouchTarget + AhSpace.md),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                Icon(tab.icon, size: 20, color: tint),
                const SizedBox(height: 2),
                Text(
                  label,
                  style: AhText.label.copyWith(fontSize: 9, color: tint),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Writes the queue has stopped retrying, surfaced wherever the partner is.
///
/// Deliberately a count and a route, not a modal. A `409` on a contract transition is not urgent to
/// the second — the entity moved and the server's answer is waiting — but it is something only a
/// person can resolve, so it must not be discoverable solely by opening a settings screen nobody
/// opens.
class _StuckWritesBanner extends ConsumerWidget {
  const _StuckWritesBanner();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final List<OutboxEntry> entries =
        ref.watch(outboxEntriesProvider).valueOrNull ?? const <OutboxEntry>[];
    final int stuck = entries.where((OutboxEntry entry) => entry.needsAttention).length;
    if (stuck == 0) return const SizedBox.shrink();

    final PartnerText text = ref.watch(partnerTextProvider);

    return Material(
      type: MaterialType.transparency,
      child: InkWell(
        onTap: () => context.go('/settings'),
        splashFactory: NoSplash.splashFactory,
        child: ColoredBox(
          color: AhColors.cautionSoft,
          child: Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AhSpace.lg,
              vertical: AhSpace.sm,
            ),
            child: Row(
              children: <Widget>[
                const Icon(Icons.pending_actions_outlined, size: 16, color: AhColors.caution),
                const SizedBox(width: AhSpace.sm),
                Expanded(
                  child: Text(
                    text(PartnerKeys.settingsQueue),
                    style: AhText.bodySm.copyWith(color: AhColors.tarmac),
                  ),
                ),
                // The count is a number, not a sentence — no language to invent, and it is the one
                // thing the partner needs to decide whether to look now.
                Text('$stuck', style: AhText.data.copyWith(color: AhColors.tarmac)),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
