import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

/// The smallest tree a canon widget needs.
///
/// Deliberately not a `MaterialApp`: `MaterialApp` installs its own [MediaQuery] from the test
/// window, which would silently win over the one these tests set to exercise
/// `MediaQuery.disableAnimationsOf`. Building the ancestors by hand keeps the reduced-motion test
/// honest rather than accidentally passing against the platform default.
Widget ahHarness(
  Widget child, {
  Brightness brightness = Brightness.light,
  AhDensity density = AhDensity.comfortable,
  bool disableAnimations = false,
}) {
  return MediaQuery(
    data: MediaQueryData(disableAnimations: disableAnimations),
    child: Directionality(
      textDirection: TextDirection.ltr,
      child: Localizations(
        locale: const Locale('en', 'IN'),
        // `TextField` asserts on these. Supplying the defaults directly keeps the tree free of
        // `MaterialApp`, which is the whole point of this harness.
        delegates: const <LocalizationsDelegate<dynamic>>[
          DefaultMaterialLocalizations.delegate,
          DefaultWidgetsLocalizations.delegate,
        ],
        child: Theme(
          data: brightness == Brightness.dark
              ? AhTheme.dark(density: density)
              : AhTheme.light(density: density),
          child: Material(
            color: Colors.transparent,
            // `EditableText` asserts on an Overlay ancestor — it is where the selection toolbar
            // and the magnifier mount. `Overlay.wrap` rather than `Overlay(initialEntries: …)`:
            // `initialEntries` is read once and ignored on rebuild, so a second `pumpWidget` with
            // a changed child would silently keep showing the first one and every "it reacts to
            // a new value" test would pass without testing anything.
            child: Overlay.wrap(
              // Centred and shrink-wrapped on both axes: a widget under test must lay out against
              // its own intrinsic size, not against a full-screen box that would hide an
              // overflow. 360dp is the narrow handset the design system targets.
              child: Align(
                widthFactor: 1,
                heightFactor: 1,
                child: SizedBox(width: 360, child: child),
              ),
            ),
          ),
        ),
      ),
    ),
  );
}
