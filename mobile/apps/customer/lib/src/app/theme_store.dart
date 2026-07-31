import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// The store is `autohub_ui`'s [AhPreferencesThemeStore], which is where this app's own copy went.
/// It was identical to the partner app's, including the `'theme_mode'` key, and a storage key that
/// exists in two places is one typo away from stranding everyone who chose dark.
///
/// Held for the process, because `MaterialApp` listens to it.
///
/// Starts on [AhThemeMode.system] and is corrected from disk by `AhThemeController.load()` during
/// startup. A dealer who chose dark and gets a white flash at every cold launch stops trusting that
/// the setting stuck, which is why the load happens before the first frame rather than after it.
final Provider<AhThemeController> themeControllerProvider = Provider<AhThemeController>((ref) {
  final AhThemeController controller = AhThemeController(
    store: AhPreferencesThemeStore(ref.watch(devicePreferencesProvider)),
  );
  ref.onDispose(controller.dispose);
  return controller;
});
