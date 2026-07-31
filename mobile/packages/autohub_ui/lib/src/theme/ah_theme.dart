import 'package:flutter/material.dart';

import '../tokens/colors.dart';
import '../tokens/typography.dart';
import 'ah_density.dart';

/// The instrument easing curve, ported from `--ease-instrument`.
///
/// It decelerates hard and does not overshoot. A spring would be playful and this product is a
/// measuring tool; the only thing it is ever applied to is the odometer roll.
const Cubic ahEaseInstrument = Cubic(0.2, 0.8, 0.2, 1);

/// [ThemeData] for the Registry language.
///
/// Two things this deliberately does *not* do. It does not call `ColorScheme.fromSeed`: seeding
/// generates a tonal palette, and a generated `#0C4034` is not `--ah-highway` — the whole reason
/// the hex values were ported by hand is that the phone and the laptop must render one product.
/// And it does not enable Material's ink splash: the one animation in AutoHub is the odometer
/// roll, and a ripple under every tap is ambient motion that would cost that moment its weight.
abstract final class AhTheme {
  /// The plate-white page a customer sees.
  static ThemeData light({AhDensity density = AhDensity.comfortable}) =>
      _build(Brightness.light, AhSurfaces.light(), density);

  /// Authored, not inverted.
  ///
  /// Dealers bid at night and inspectors work in basements with a torch, so dark carries its own
  /// values from [AhSurfaces.dark] rather than flipping the neutral ramp — an inverted ramp puts
  /// hairlines at the wrong contrast and turns [AhColors.highway] into an invisible rectangle.
  static ThemeData dark({AhDensity density = AhDensity.comfortable}) =>
      _build(Brightness.dark, AhSurfaces.dark(), density);

  static ThemeData _build(Brightness brightness, AhSurfaces surfaces, AhDensity density) {
    final scheme = ColorScheme(
      brightness: brightness,
      primary: surfaces.accentPrimary,
      onPrimary: surfaces.textOnPrimary,
      // Material's "secondary" is the closest slot to the rationed yellow. Nothing in the canon
      // reads it — controls take their colour from AhSurfaces — but a stray Material widget that
      // does should at least land on a token rather than on Material's purple.
      secondary: surfaces.accentAction,
      onSecondary: surfaces.textOnAccent,
      error: AhColors.flag,
      onError: AhColors.plate,
      surface: surfaces.card,
      onSurface: surfaces.textPrimary,
      surfaceContainerHighest: surfaces.sunken,
      onSurfaceVariant: surfaces.textSecondary,
      outline: surfaces.borderStrong,
      outlineVariant: surfaces.borderHairline,
      inverseSurface: surfaces.inverse,
      onInverseSurface: surfaces.page,
      // Elevation overlays are a Material 3 tint that lifts a surface toward the primary hue.
      // Structure here comes from hairlines, so the tint is switched off by pinning it to the
      // surface it would be tinting.
      surfaceTint: surfaces.card,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: scheme,
      scaffoldBackgroundColor: surfaces.page,
      canvasColor: surfaces.page,
      dividerColor: surfaces.borderHairline,
      splashFactory: NoSplash.splashFactory,
      highlightColor: Colors.transparent,
      // Material pads every tap target to 48dp, which fights `compact` row heights. The canon
      // enforces its own floor (`ahMinTouchTarget`) at the widget that owns the gesture, where it
      // can pad the hit area without also padding the visible box.
      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
      fontFamily: AhFonts.sans,
      textTheme: _textTheme(surfaces),
      dividerTheme: DividerThemeData(
        color: surfaces.borderHairline,
        thickness: 1,
        space: 1,
      ),
      extensions: <ThemeExtension<dynamic>>[surfaces, AhDensityTheme(density)],
    );
  }

  /// Maps the signage scale onto Material's slots.
  ///
  /// The mapping exists so that a Material widget the canon has not replaced yet — a dialog, a
  /// snackbar — still speaks in the right faces. Canon widgets name [AhText] styles directly and
  /// never read `Theme.of(context).textTheme`, because the signage scale is chosen by reading
  /// distance and Material's is chosen by a type ramp, and the two do not line up.
  static TextTheme _textTheme(AhSurfaces surfaces) {
    final primary = surfaces.textPrimary;
    final secondary = surfaces.textSecondary;

    return TextTheme(
      displayLarge: AhText.displayXl.copyWith(color: primary),
      displayMedium: AhText.displayLg.copyWith(color: primary),
      displaySmall: AhText.displayLg.copyWith(color: primary),
      headlineMedium: AhText.heading.copyWith(color: primary),
      headlineSmall: AhText.heading.copyWith(color: primary),
      titleLarge: AhText.subheading.copyWith(color: primary),
      titleMedium: AhText.subheading.copyWith(color: primary),
      titleSmall: AhText.bodySm.copyWith(color: primary, fontWeight: FontWeight.w600),
      bodyLarge: AhText.body.copyWith(color: primary),
      bodyMedium: AhText.body.copyWith(color: primary),
      bodySmall: AhText.bodySm.copyWith(color: secondary),
      labelLarge: AhText.bodySm.copyWith(color: primary, fontWeight: FontWeight.w600),
      labelMedium: AhText.label.copyWith(color: secondary),
      labelSmall: AhText.label.copyWith(color: secondary),
    );
  }
}

/// How every widget in the canon asks for a colour.
extension AhSurfacesContext on BuildContext {
  /// The resolved surfaces for this subtree.
  ///
  /// Falling back on brightness rather than throwing keeps a canon widget usable inside a bare
  /// `MaterialApp` — a golden test, a package example, an app mid-migration. It will look
  /// slightly off and it will not crash, which is the right trade for a widget layer.
  AhSurfaces get ahSurfaces {
    final theme = Theme.of(this);
    return theme.extension<AhSurfaces>() ??
        (theme.brightness == Brightness.dark ? AhSurfaces.dark() : AhSurfaces.light());
  }
}
