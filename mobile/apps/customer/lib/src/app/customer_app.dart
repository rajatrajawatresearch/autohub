import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../data/app_providers.dart';
import '../data/sell.dart';
import 'router.dart';
import 'theme_store.dart';

/// The customer binary's root.
///
/// Density is `comfortable` here and `compact` in the dealer and partner binaries, set once in the
/// theme so that not one widget in the shared canon has to know which app it is running inside.
/// Both brightnesses ship: dark is an authored mode rather than an inversion, and a customer
/// checking a valuation at eleven at night is not an edge case.
class CustomerApp extends ConsumerStatefulWidget {
  const CustomerApp({super.key});

  @override
  ConsumerState<CustomerApp> createState() => _CustomerAppState();
}

class _CustomerAppState extends ConsumerState<CustomerApp> {
  @override
  void initState() {
    super.initState();
    unawaited(_startup());
  }

  /// Everything that must happen once, and none of it blocking the first frame.
  ///
  /// A launch that waits on the network is a launch that hangs in a basement. The registry warm is
  /// a set of conditional GETs whose unchanged answer is a couple of hundred bytes; whatever lands
  /// lands, and whatever does not is served from the previous session's cache.
  Future<void> _startup() async {
    // Read before the first paint so a user who chose dark does not get a white flash.
    await ref.read(themeControllerProvider).load();

    final RegistryClient registry = ref.read(registryClientProvider);
    final String locale = ref.read(appLocaleProvider).valueOrNull ?? AhStrings.fallbackLocale;

    await registry.warm(<RegistryKey>[
      RegistryDocuments.features,
      RegistryDocuments.cities,
      RegistryDocuments.taxonomyTree,
      // The one document that must survive offline above all others: a seller who opened the sell
      // flow on a train and lost signal keeps typing only because this landed on an earlier launch.
      RegistryDocuments.form(sellFormKey),
      RegistryDocuments.i18n(locale),
      if (locale != AhStrings.fallbackLocale) RegistryDocuments.i18n(AhStrings.fallbackLocale),
    ]);

    // Reading a provider is not consent to start making network calls, so the drain loop does not
    // start itself. This is the one place that says go — after the session has been hydrated, so a
    // queued listing drains under the token it was made with.
    ref.read(outboxDrainerProvider).start();

    // A month is not a guess about attention span. It is long enough that anything older is a form
    // the user has forgotten exists, and short enough that an abandoned 40-photo draft stops
    // occupying a budget handset's storage forever.
    await ref.read(draftStoreProvider).purgeOlderThan(const Duration(days: 30));
  }

  @override
  Widget build(BuildContext context) {
    final AhThemeController theme = ref.watch(themeControllerProvider);
    final GoRouter router = ref.watch(routerProvider);

    return ListenableBuilder(
      listenable: theme,
      builder: (BuildContext context, Widget? _) => MaterialApp.router(
        title: 'AutoHub',
        debugShowCheckedModeBanner: false,
        routerConfig: router,
        // Density is set here and nowhere else. `comfortable` on customer, `compact` on dealer and
        // partner — the same canon, two sizes, no `if (isDealer)` in a single widget.
        theme: AhTheme.light(),
        darkTheme: AhTheme.dark(),
        themeMode: theme.materialMode,
      ),
    );
  }
}
