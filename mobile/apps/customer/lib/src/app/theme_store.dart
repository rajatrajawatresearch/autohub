import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Persists the light/dark choice in the same table as the active city.
///
/// `autohub_ui` declares the port and refuses to implement it — it is the package that knows what a
/// colour is and nothing else, and a widget library that opened a database would be a widget library
/// nobody could test. This is the app filling it in, against `autohub_core`'s preferences table
/// rather than the keychain: a theme is not a secret, and it is read on the very first frame.
final class PreferencesThemeStore implements AhThemeStore {
  const PreferencesThemeStore(this._preferences);

  static const String key = 'theme_mode';

  final DevicePreferences _preferences;

  @override
  Future<AhThemeMode?> read() async => AhThemeMode.tryParse(await _preferences.read(key));

  @override
  Future<void> write(AhThemeMode mode) => _preferences.write(key, mode.storageKey);
}

/// Held for the process, because `MaterialApp` listens to it.
///
/// Starts on [AhThemeMode.system] and is corrected from disk by `AhThemeController.load()` during
/// startup. A dealer who chose dark and gets a white flash at every cold launch stops trusting that
/// the setting stuck, which is why the load happens before the first frame rather than after it.
final Provider<AhThemeController> themeControllerProvider = Provider<AhThemeController>((ref) {
  final AhThemeController controller = AhThemeController(
    store: PreferencesThemeStore(ref.watch(devicePreferencesProvider)),
  );
  ref.onDispose(controller.dispose);
  return controller;
});
