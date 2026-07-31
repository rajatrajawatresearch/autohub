import 'package:flutter/material.dart';

/// The Registry palette, ported from `packages/ui/src/styles/tokens.css`.
///
/// The hex values are identical to the web's on purpose. A customer who checks a valuation on the
/// phone and opens the same listing on a laptop is looking at one product, and "close enough" green
/// is how two surfaces drift into looking like two companies.
///
/// Two colours carry meaning and everything else is neutral: [highway] is structure and trust,
/// [signal] is action and liveness. Nothing else gets a colour unless it is a status.
abstract final class AhColors {
  /// Near-black, faintly green. Dark surfaces, and the lettering on a private plate.
  static const tarmac = Color(0xFF0F1512);

  /// Deep signage green. The primary: headers, active state, verified marks, primary actions.
  static const highway = Color(0xFF0B3D2E);

  /// Hover and raised primary, and the primary itself on dark surfaces.
  static const highwayLit = Color(0xFF145A44);

  /// Commercial-plate yellow.
  ///
  /// **Rationed to one element per screen.** It marks the single most consequential action in view
  /// — place bid, get valuation, accept offer. A second yellow control on the same screen is a bug,
  /// not a style choice, because the whole value of this colour is that it means "this one".
  ///
  /// It always takes dark text. White on this yellow fails contrast at every size.
  static const signal = Color(0xFFF2C230);
  static const signalLit = Color(0xFFFFD84D);

  /// Plate off-white. The light page background.
  static const plate = Color(0xFFF6F5F1);

  /// Card surface on light.
  static const chalk = Color(0xFFFFFFFF);

  /// Neutral ramp — cool-green tinted, never pure grey.
  static const n950 = Color(0xFF0F1512);
  static const n900 = Color(0xFF151D19);
  static const n800 = Color(0xFF1D2622);
  static const n700 = Color(0xFF2F3B35);
  static const n600 = Color(0xFF47544D);

  /// Secondary text. Passes AA on both [chalk] and [plate].
  static const n500 = Color(0xFF64726A);
  static const n400 = Color(0xFF94A09A);
  static const n300 = Color(0xFFC3CBC5);

  /// Hairline rules. Structure comes from these, not from shadows.
  static const n200 = Color(0xFFDEE3DF);
  static const n100 = Color(0xFFEDF0EC);
  static const n50 = Color(0xFFF6F8F5);

  /// Status. The only other colours permitted anywhere in the product.
  static const flag = Color(0xFFC1402B); // expired, rejected, overdue
  static const flagSoft = Color(0xFFFBEAE7);
  static const caution = Color(0xFFB5771A); // pending, expiring soon
  static const cautionSoft = Color(0xFFFDF3E2);
  static const clear = Color(0xFF1E7A54); // verified, settled, passed
  static const clearSoft = Color(0xFFE6F4ED);
  static const info = Color(0xFF2C5A8C); // informational, under review
  static const infoSoft = Color(0xFFE9F0F7);
}

/// Semantic surfaces, resolved per theme.
///
/// Widgets read these and never the raw ramp — which is what lets dark mode be an authored mode
/// rather than an inversion, and what lets a token change once instead of in ninety files.
@immutable
class AhSurfaces extends ThemeExtension<AhSurfaces> {
  const AhSurfaces({
    required this.page,
    required this.card,
    required this.sunken,
    required this.inverse,
    required this.textPrimary,
    required this.textSecondary,
    required this.textOnPrimary,
    required this.textOnAccent,
    required this.borderHairline,
    required this.borderStrong,
    required this.borderPlate,
    required this.accentPrimary,
    required this.accentPrimaryHover,
    required this.accentAction,
    required this.focusRing,
  });

  /// Light. The plate-white page a customer sees.
  factory AhSurfaces.light() => const AhSurfaces(
        page: AhColors.plate,
        card: AhColors.chalk,
        sunken: AhColors.n100,
        inverse: AhColors.tarmac,
        textPrimary: AhColors.n950,
        textSecondary: AhColors.n500,
        textOnPrimary: Color(0xFFF2F7F4),
        textOnAccent: AhColors.tarmac,
        borderHairline: AhColors.n200,
        borderStrong: AhColors.n300,
        borderPlate: AhColors.tarmac,
        accentPrimary: AhColors.highway,
        accentPrimaryHover: AhColors.highwayLit,
        accentAction: AhColors.signal,
        focusRing: AhColors.highway,
      );

  /// Dark, authored rather than inverted.
  ///
  /// Dealers bid at night and inspectors work in basements, so this is a real mode with its own
  /// values. [AhColors.signal] is unchanged — it is already a dark-surface colour — while green
  /// lifts, because [AhColors.highway] on near-black is a rectangle nobody can see.
  factory AhSurfaces.dark() => const AhSurfaces(
        page: AhColors.tarmac,
        card: AhColors.n900,
        sunken: Color(0xFF0B0F0D),
        inverse: AhColors.plate,
        textPrimary: Color(0xFFEEF2EF),
        textSecondary: AhColors.n400,
        textOnPrimary: Color(0xFFF2F7F4),
        textOnAccent: AhColors.tarmac,
        borderHairline: Color(0xFF26302B),
        borderStrong: Color(0xFF35413B),
        borderPlate: Color(0xFFE8ECE9),
        accentPrimary: AhColors.highwayLit,
        accentPrimaryHover: Color(0xFF1C7256),
        accentAction: AhColors.signal,
        // Green on a dark surface is not visible enough to be a focus ring, so the ring is the
        // one place yellow appears without being an action.
        focusRing: AhColors.signal,
      );

  final Color page;
  final Color card;
  final Color sunken;
  final Color inverse;

  final Color textPrimary;
  final Color textSecondary;
  final Color textOnPrimary;

  /// Dark text, always. Yellow never takes white.
  final Color textOnAccent;

  final Color borderHairline;
  final Color borderStrong;
  final Color borderPlate;

  final Color accentPrimary;
  final Color accentPrimaryHover;
  final Color accentAction;

  final Color focusRing;

  @override
  AhSurfaces copyWith({
    Color? page,
    Color? card,
    Color? sunken,
    Color? inverse,
    Color? textPrimary,
    Color? textSecondary,
    Color? textOnPrimary,
    Color? textOnAccent,
    Color? borderHairline,
    Color? borderStrong,
    Color? borderPlate,
    Color? accentPrimary,
    Color? accentPrimaryHover,
    Color? accentAction,
    Color? focusRing,
  }) {
    return AhSurfaces(
      page: page ?? this.page,
      card: card ?? this.card,
      sunken: sunken ?? this.sunken,
      inverse: inverse ?? this.inverse,
      textPrimary: textPrimary ?? this.textPrimary,
      textSecondary: textSecondary ?? this.textSecondary,
      textOnPrimary: textOnPrimary ?? this.textOnPrimary,
      textOnAccent: textOnAccent ?? this.textOnAccent,
      borderHairline: borderHairline ?? this.borderHairline,
      borderStrong: borderStrong ?? this.borderStrong,
      borderPlate: borderPlate ?? this.borderPlate,
      accentPrimary: accentPrimary ?? this.accentPrimary,
      accentPrimaryHover: accentPrimaryHover ?? this.accentPrimaryHover,
      accentAction: accentAction ?? this.accentAction,
      focusRing: focusRing ?? this.focusRing,
    );
  }

  @override
  AhSurfaces lerp(covariant AhSurfaces? other, double t) {
    if (other == null) return this;
    return AhSurfaces(
      page: Color.lerp(page, other.page, t)!,
      card: Color.lerp(card, other.card, t)!,
      sunken: Color.lerp(sunken, other.sunken, t)!,
      inverse: Color.lerp(inverse, other.inverse, t)!,
      textPrimary: Color.lerp(textPrimary, other.textPrimary, t)!,
      textSecondary: Color.lerp(textSecondary, other.textSecondary, t)!,
      textOnPrimary: Color.lerp(textOnPrimary, other.textOnPrimary, t)!,
      textOnAccent: Color.lerp(textOnAccent, other.textOnAccent, t)!,
      borderHairline: Color.lerp(borderHairline, other.borderHairline, t)!,
      borderStrong: Color.lerp(borderStrong, other.borderStrong, t)!,
      borderPlate: Color.lerp(borderPlate, other.borderPlate, t)!,
      accentPrimary: Color.lerp(accentPrimary, other.accentPrimary, t)!,
      accentPrimaryHover: Color.lerp(accentPrimaryHover, other.accentPrimaryHover, t)!,
      accentAction: Color.lerp(accentAction, other.accentAction, t)!,
      focusRing: Color.lerp(focusRing, other.focusRing, t)!,
    );
  }
}
