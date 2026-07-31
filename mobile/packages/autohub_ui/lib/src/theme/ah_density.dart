import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../tokens/typography.dart';

/// The floor, in both densities, on every platform.
///
/// 44dp is Apple's HIG minimum and comfortably above Material's 40dp. It is not negotiable down
/// to `compact`: a dealer scanning an inventory list wants *shorter rows*, not smaller targets —
/// they are usually on a showroom floor, one-handed, often standing. `compact` earns its density
/// by cutting vertical padding and gap, not by making things harder to hit.
///
/// So a control smaller than this renders at its visual size and is *padded out* to 44dp of hit
/// area. The pixels shrink; the target does not.
const double ahMinTouchTarget = 44;

/// Density as a [ThemeExtension], so it travels with [ThemeData] and lerps with it.
///
/// Carrying it in the theme rather than in a global means the same widget tree is compact in the
/// dealer binary and comfortable in the customer binary with no `if (isDealer)` anywhere — which
/// is the entire reason the two apps can share a canon at all. It also makes density testable:
/// pump the same widget under two themes and diff the layout.
@immutable
class AhDensityTheme extends ThemeExtension<AhDensityTheme> {
  const AhDensityTheme(this.density);

  final AhDensity density;

  @override
  AhDensityTheme copyWith({AhDensity? density}) => AhDensityTheme(density ?? this.density);

  /// Density is a discrete choice, not a continuum — there is no half-compact row height that
  /// anyone designed. Snapping at the midpoint keeps a theme animation from parking every
  /// control on an untested size for 150ms.
  @override
  AhDensityTheme lerp(covariant AhDensityTheme? other, double t) {
    if (other == null) return this;
    return t < 0.5 ? this : other;
  }
}

/// A subtree override, for the one screen that legitimately mixes both.
///
/// The case this exists for: a customer surface (comfortable, thumb-driven) that embeds a
/// specification table which is genuinely a data grid and reads better compact. Wrapping the
/// table beats threading a `density:` parameter through six widgets, and beats the alternative
/// nobody should reach for — a second set of "dense" widgets.
///
/// It is an override and not the primary mechanism. If an app finds itself wrapping whole pages,
/// the app picked the wrong density in its [ThemeData].
class AhDensityScope extends InheritedWidget {
  const AhDensityScope({
    required this.density,
    required super.child,
    super.key,
  });

  final AhDensity density;

  /// The nearest override, or `null` when the theme's density should win.
  static AhDensity? maybeOf(BuildContext context) =>
      context.dependOnInheritedWidgetOfExactType<AhDensityScope>()?.density;

  @override
  bool updateShouldNotify(AhDensityScope oldWidget) => oldWidget.density != density;
}

/// How every control in the canon asks what size it is.
extension AhDensityContext on BuildContext {
  /// Resolution order: nearest [AhDensityScope], then the theme, then comfortable.
  ///
  /// Comfortable is the fallback rather than compact because getting it wrong in the comfortable
  /// direction costs a few dp of whitespace, and getting it wrong in the compact direction costs
  /// a mis-tap on a screen that moves money.
  AhDensity get ahDensity =>
      AhDensityScope.maybeOf(this) ??
      Theme.of(this).extension<AhDensityTheme>()?.density ??
      AhDensity.comfortable;

  /// The hit-target height for a control whose visual height is [visualHeight].
  double ahTapTarget(double visualHeight) => math.max(visualHeight, ahMinTouchTarget);
}
