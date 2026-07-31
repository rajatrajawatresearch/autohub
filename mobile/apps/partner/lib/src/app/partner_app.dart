import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/partner_providers.dart';
import 'partner_router.dart';
import 'partner_text.dart';

/// The root widget.
///
/// Density is `compact`, matching the dealer app and the web partner portal. The reasoning from
/// `docs/01-design-system.md` applies exactly: this user is reconciling lists, not browsing — a
/// fleet operator scanning forty candidates, an owner checking eight documents and twelve months of
/// statements. Compact buys them rows they can see more of at once, and it buys them nothing at the
/// cost of the touch target, which stays pinned at 44dp in both densities because these people are
/// standing in a yard holding the phone in one hand.
///
/// Both themes ship at parity. Dark is authored rather than inverted, and on this app it earns its
/// keep for a reason the design system already anticipated: owner-drivers check a payout at the end
/// of a night shift.
class PartnerApp extends ConsumerStatefulWidget {
  const PartnerApp({super.key});

  @override
  ConsumerState<PartnerApp> createState() => _PartnerAppState();
}

class _PartnerAppState extends ConsumerState<PartnerApp> {
  @override
  void initState() {
    super.initState();
    // Once, from the shell, and never from a widget build. Reading a provider is not consent to
    // start making network calls — two drainers over one queue would each hold their own
    // reentrancy guard and cheerfully send the same supply post twice.
    ref.read(outboxDrainerProvider).start();
  }

  @override
  Widget build(BuildContext context) {
    final AhThemeController theme = ref.watch(themeControllerProvider);
    final PartnerText text = ref.watch(partnerTextProvider);

    return ListenableBuilder(
      listenable: theme,
      builder: (BuildContext context, Widget? _) => MaterialApp.router(
        // Shown in the OS task switcher, so it is a user-facing string and goes through the same
        // resolver as everything else.
        title: text(PartnerKeys.appTitle),
        debugShowCheckedModeBanner: false,
        routerConfig: ref.watch(routerProvider),
        theme: AhTheme.light(density: AhDensity.compact),
        darkTheme: AhTheme.dark(density: AhDensity.compact),
        themeMode: theme.materialMode,
      ),
    );
  }
}
