import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'providers.dart';
import 'router.dart';

/// The dealer console.
///
/// Density is **compact**, set once here in the theme. Every control in `autohub_ui` reads
/// `context.ahDensity`, so one line retunes every row height, every button and every field in the
/// binary — and the same widgets are comfortable in the customer app with no `if (isDealer)`
/// anywhere. That is the entire reason three apps can share one canon.
///
/// Light and dark ship at parity because dealers bid at night. Dark is authored rather than
/// inverted — see `AhSurfaces.dark`.
class DealerApp extends ConsumerWidget {
  const DealerApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return _Runtime(
      child: MaterialApp.router(
        title: 'AutoHub Dealer',
        debugShowCheckedModeBanner: false,
        routerConfig: ref.watch(dealerRouterProvider),
        theme: AhTheme.light(density: AhDensity.compact),
        darkTheme: AhTheme.dark(density: AhDensity.compact),
        // The handset decides. A dealer who has their phone in dark mode at 9pm on a showroom floor
        // has already told us what they want.
        themeMode: ThemeMode.system,
      ),
    );
  }
}

/// Starts the things that must run exactly once, after the graph exists and before the first
/// screen needs them.
///
/// Two of them, and both are deliberately *not* side effects of reading a provider:
///
/// **The outbox drain loop.** `outboxDrainerProvider` does not start itself, because reading a
/// provider is not consent to begin sending the user's queued writes — a widget that happened to
/// `watch` it during a build would otherwise fire off a dealer's pending workflow actions as a
/// consequence of rendering.
///
/// **Registry warming.** The console cannot paint its own tab bar without the feature document or
/// its pipeline without the deal workflow, and every one of those reads is served from disk first.
/// Warming is best-effort and never awaited by anything on screen: a launch that blocks on the
/// network is a launch that hangs in a basement.
class _Runtime extends ConsumerStatefulWidget {
  const _Runtime({required this.child});

  final Widget child;

  @override
  ConsumerState<_Runtime> createState() => _RuntimeState();
}

class _RuntimeState extends ConsumerState<_Runtime> {
  @override
  void initState() {
    super.initState();
    // Deferred to after the first frame so the console paints from cache immediately and the
    // network work happens behind it.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      ref.read(outboxDrainerProvider).start();
      unawaited(_warm());
    });
  }

  Future<void> _warm() async {
    final client = ref.read(registryClientProvider);
    final locale = ref.read(activeLocaleProvider);

    // `warm` swallows its own failures by design — whatever lands, lands, and whatever does not is
    // served from the previous session's cache.
    await client.warm(<RegistryKey>[
      RegistryDocuments.features,
      RegistryDocuments.workflow('listing'),
      RegistryDocuments.workflow('deal'),
      RegistryDocuments.i18n(locale),
      RegistryDocuments.i18n(AhStrings.fallbackLocale),
    ]);
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
