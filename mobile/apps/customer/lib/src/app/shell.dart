import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../data/app_providers.dart';
import '../i18n/customer_text.dart';

/// The four places a customer goes, and the bar that gets them there.
///
/// A `StatefulNavigationShell`, so each tab keeps its own navigator: a seller who is four steps
/// into the sell form and taps *Buy* to check what a comparable car is listed at comes back to step
/// four, not to step one. On a twenty-minute form that is the difference between a listing and an
/// abandoned draft.
///
/// The bar itself is not Material's `NavigationBar`. That widget is a pill indicator, a ripple and
/// an elevation — three things this design language does not have. Structure here comes from a
/// hairline, the selected tab is marked by the same highway green that marks structure everywhere
/// else, and nothing animates.
class CustomerShell extends ConsumerWidget {
  const CustomerShell({required this.navigationShell, super.key});

  final StatefulNavigationShell navigationShell;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);

    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: DecoratedBox(
        decoration: BoxDecoration(
          color: surfaces.card,
          border: Border(top: BorderSide(color: surfaces.borderHairline)),
        ),
        child: SafeArea(
          top: false,
          child: Row(
            children: <Widget>[
              _Tab(
                index: 0,
                icon: Icons.grid_view_outlined,
                label: t(CustomerText.navHome),
                shell: navigationShell,
              ),
              _Tab(
                index: 1,
                icon: Icons.search_outlined,
                label: t(CustomerText.navBuy),
                shell: navigationShell,
              ),
              _Tab(
                index: 2,
                icon: Icons.sell_outlined,
                label: t(CustomerText.navSell),
                shell: navigationShell,
              ),
              _Tab(
                index: 3,
                icon: Icons.garage_outlined,
                label: t(CustomerText.navGarage),
                shell: navigationShell,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Tab extends StatelessWidget {
  const _Tab({
    required this.index,
    required this.icon,
    required this.label,
    required this.shell,
  });

  final int index;
  final IconData icon;
  final String label;
  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final bool selected = shell.currentIndex == index;
    final Color tint = selected ? surfaces.accentPrimary : surfaces.textSecondary;

    return Expanded(
      child: Semantics(
        button: true,
        selected: selected,
        label: label,
        excludeSemantics: true,
        child: Material(
          type: MaterialType.transparency,
          child: InkWell(
            // `initialLocation: true` only when re-tapping the current tab: that is the gesture
            // that means "take me back to the top of this section", and it is the only way out of a
            // deep stack without hunting for the back arrow.
            onTap: () => shell.goBranch(index, initialLocation: selected),
            splashFactory: NoSplash.splashFactory,
            hoverColor: Colors.transparent,
            focusColor: Colors.transparent,
            highlightColor: Colors.transparent,
            child: Container(
              // Comfortably past the 44dp floor. This bar is hit one-handed, with a thumb, by
              // somebody walking around a car.
              constraints: const BoxConstraints(minHeight: 56),
              padding: const EdgeInsets.symmetric(vertical: AhSpace.sm),
              decoration: BoxDecoration(
                border: Border(
                  top: BorderSide(
                    color: selected ? surfaces.accentPrimary : Colors.transparent,
                    width: 2,
                  ),
                ),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  Icon(icon, size: 20, color: tint),
                  const SizedBox(height: AhSpace.xs),
                  Text(
                    label.toUpperCase(),
                    style: AhText.label.copyWith(color: tint),
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
