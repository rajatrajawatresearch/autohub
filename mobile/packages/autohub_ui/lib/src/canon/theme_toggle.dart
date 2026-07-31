import 'dart:async';

import 'package:flutter/material.dart';

import '../theme/ah_density.dart';
import '../theme/ah_theme.dart';
import '../tokens/typography.dart';

/// Three answers, because "system" is a real one.
enum AhThemeMode {
  /// Follow the platform. The default, and correct for most people.
  system,
  light,
  dark;

  /// The wire form, for whatever the app persists into.
  String get storageKey => switch (this) {
        AhThemeMode.system => 'system',
        AhThemeMode.light => 'light',
        AhThemeMode.dark => 'dark',
      };

  static AhThemeMode? tryParse(String? value) => switch (value) {
        'system' => AhThemeMode.system,
        'light' => AhThemeMode.light,
        'dark' => AhThemeMode.dark,
        _ => null,
      };

  ThemeMode get materialMode => switch (this) {
        AhThemeMode.system => ThemeMode.system,
        AhThemeMode.light => ThemeMode.light,
        AhThemeMode.dark => ThemeMode.dark,
      };
}

/// Where the choice is persisted.
///
/// An interface rather than a concrete implementation because `autohub_ui` is the package that
/// knows what a colour is and nothing else — it has no business opening a keystore or a database.
/// The app supplies the implementation (`shared_preferences`, drift, whatever `autohub_core`
/// settles on) and this package stays testable with [AhInMemoryThemeStore].
abstract interface class AhThemeStore {
  /// `null` when nothing has been chosen yet, which is different from having chosen `system`.
  Future<AhThemeMode?> read();

  Future<void> write(AhThemeMode mode);
}

/// For tests and for the first run before an app wires real storage.
class AhInMemoryThemeStore implements AhThemeStore {
  AhThemeMode? _value;

  @override
  Future<AhThemeMode?> read() async => _value;

  @override
  Future<void> write(AhThemeMode mode) async => _value = mode;
}

/// Holds the choice and writes it through.
///
/// The write is fire-and-forget from the user's point of view but never from the controller's:
/// the in-memory value changes first and notifies immediately, so the theme flips on the same
/// frame as the tap. A toggle that waits on a disk write before repainting feels broken on a
/// cheap handset, and the failure mode of a lost write — the app opens in system theme once — is
/// mild enough to be worth that.
class AhThemeController extends ChangeNotifier {
  AhThemeController({required AhThemeStore store, AhThemeMode initial = AhThemeMode.system})
      : _store = store,
        _mode = initial;

  final AhThemeStore _store;
  AhThemeMode _mode;

  AhThemeMode get mode => _mode;

  /// Convenience for `MaterialApp.themeMode`.
  ThemeMode get materialMode => _mode.materialMode;

  /// Call once at startup, before the first frame if the app wants to avoid a flash of the wrong
  /// theme. A dealer who chose dark and gets a white flash at every cold launch stops trusting
  /// that the setting stuck.
  Future<void> load() async {
    final stored = await _store.read();
    if (stored == null || stored == _mode) return;
    _mode = stored;
    notifyListeners();
  }

  Future<void> select(AhThemeMode mode) async {
    if (mode == _mode) return;
    _mode = mode;
    notifyListeners();
    await _store.write(mode);
  }
}

@immutable
class AhThemeToggleLabels {
  const AhThemeToggleLabels({
    this.group = 'Theme',
    this.system = 'System',
    this.light = 'Light',
    this.dark = 'Dark',
  });

  final String group;
  final String system;
  final String light;
  final String dark;
}

/// A three-way segmented control: system, light, dark.
///
/// Dark is a real mode and not an inversion — dealers bid at night, inspectors work in basements —
/// so both themes ship at parity and the choice is explicit rather than guessed from the hour.
class AhThemeToggle extends StatelessWidget {
  const AhThemeToggle({
    required this.controller,
    this.labels = const AhThemeToggleLabels(),
    super.key,
  });

  final AhThemeController controller;

  /// en-IN defaults; pass resolved strings from the localisation registry in production.
  final AhThemeToggleLabels labels;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;

    return Semantics(
      label: labels.group,
      container: true,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: surfaces.card,
          borderRadius: const BorderRadius.all(AhRadius.control),
          border: Border.all(color: surfaces.borderHairline),
        ),
        child: Padding(
          padding: const EdgeInsets.all(2),
          child: ListenableBuilder(
            listenable: controller,
            builder: (context, _) => Row(
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                _Option(
                  controller: controller,
                  mode: AhThemeMode.system,
                  icon: Icons.brightness_auto_outlined,
                  label: labels.system,
                ),
                _Option(
                  controller: controller,
                  mode: AhThemeMode.light,
                  icon: Icons.light_mode_outlined,
                  label: labels.light,
                ),
                _Option(
                  controller: controller,
                  mode: AhThemeMode.dark,
                  icon: Icons.dark_mode_outlined,
                  label: labels.dark,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Option extends StatelessWidget {
  const _Option({
    required this.controller,
    required this.mode,
    required this.icon,
    required this.label,
  });

  final AhThemeController controller;
  final AhThemeMode mode;
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final selected = controller.mode == mode;

    return Semantics(
      button: true,
      selected: selected,
      label: label,
      excludeSemantics: true,
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          // `unawaited` rather than a bare call: `discarded_futures` is an error in this
          // workspace precisely so that a write nobody is watching is a deliberate act.
          onTap: () => unawaited(controller.select(mode)),
          splashFactory: NoSplash.splashFactory,
          hoverColor: Colors.transparent,
          focusColor: Colors.transparent,
          highlightColor: Colors.transparent,
          child: SizedBox(
            // Square and at the floor, in both densities. A theme switch is a small target that
            // people hit with a thumb while holding the phone in the same hand.
            width: ahMinTouchTarget,
            height: ahMinTouchTarget,
            child: Center(
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: selected ? surfaces.accentPrimary : Colors.transparent,
                  borderRadius: const BorderRadius.all(AhRadius.control),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(AhSpace.sm),
                  child: Icon(
                    icon,
                    size: 18,
                    color: selected ? surfaces.textOnPrimary : surfaces.textSecondary,
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
