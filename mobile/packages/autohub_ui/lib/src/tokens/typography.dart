import 'package:flutter/widgets.dart';

/// The three faces, and the jobs they do.
///
/// Bundled as assets rather than fetched, because a font that arrives over the network is a font
/// that has not arrived when an inspector opens a checklist in a basement.
abstract final class AhFonts {
  /// Display. A grotesque with a real width axis — signage at headline sizes, ordinary UI at body.
  static const display = 'Archivo';

  /// Body. Warmer than Inter and still readable at 13sp inside a dealer's inventory list.
  static const sans = 'InstrumentSans';

  /// Data. Plates, VIN, chassis, odometer, money, ids.
  ///
  /// Unambiguous `0`/`O` and `1`/`l` is not a preference here: a user transcribing a chassis number
  /// off a stamped plate under a bonnet has no context to disambiguate from.
  static const mono = 'JetBrainsMono';
}

/// The signage scale, ported from the web.
///
/// Sizes are chosen for the distance they are read from rather than from a modular ratio — the same
/// reasoning as `packages/ui`, and the same numbers, so the two clients agree about what a heading is.
abstract final class AhText {
  static const displayXl = TextStyle(
    fontFamily: AhFonts.display,
    fontSize: 40,
    height: 0.95,
    letterSpacing: -1.2,
    fontWeight: FontWeight.w700,
  );

  static const displayLg = TextStyle(
    fontFamily: AhFonts.display,
    fontSize: 30,
    height: 1.02,
    letterSpacing: -0.75,
    fontWeight: FontWeight.w700,
  );

  static const heading = TextStyle(
    fontFamily: AhFonts.display,
    fontSize: 22,
    height: 1.25,
    letterSpacing: -0.33,
    fontWeight: FontWeight.w600,
  );

  static const subheading = TextStyle(
    fontFamily: AhFonts.display,
    fontSize: 17,
    height: 1.35,
    fontWeight: FontWeight.w600,
  );

  static const body = TextStyle(
    fontFamily: AhFonts.sans,
    fontSize: 15,
    height: 1.6,
  );

  static const bodySm = TextStyle(
    fontFamily: AhFonts.sans,
    fontSize: 13,
    height: 1.55,
  );

  /// The RC-card atom: a small uppercase letter-spaced label above every value.
  ///
  /// This one repetition does more for coherence than any other single decision in the design, and
  /// it is the thing to copy first when building a screen that does not exist yet.
  static const label = TextStyle(
    fontFamily: AhFonts.sans,
    fontSize: 11,
    height: 1.2,
    letterSpacing: 0.88,
    fontWeight: FontWeight.w600,
  );

  /// Anything read, compared or transcribed.
  static const data = TextStyle(
    fontFamily: AhFonts.mono,
    fontSize: 14,
    height: 1.4,
    fontWeight: FontWeight.w500,
    fontFeatures: [FontFeature.tabularFigures(), FontFeature.slashedZero()],
  );

  /// The plate itself.
  static const plate = TextStyle(
    fontFamily: AhFonts.mono,
    fontSize: 20,
    height: 1,
    letterSpacing: 2.8,
    fontWeight: FontWeight.w700,
    fontFeatures: [FontFeature.tabularFigures(), FontFeature.slashedZero()],
  );
}

/// Geometry. Near-square throughout — rounded corners read as consumer retail, and this is an
/// instrument.
abstract final class AhRadius {
  static const control = Radius.circular(2);
  static const card = Radius.circular(4);

  /// A plate has square corners. It is a stamped rectangle, not a chip.
  static const plate = Radius.zero;
}

/// The 4dp spacing scale. Nothing between these values.
abstract final class AhSpace {
  static const xs = 4.0;
  static const sm = 8.0;
  static const md = 12.0;
  static const lg = 16.0;
  static const xl = 24.0;
  static const xxl = 32.0;
  static const xxxl = 48.0;
}

/// Density, matching the web's two modes.
///
/// Customer surfaces are `comfortable` and thumb-driven; dealer and partner are `compact` because
/// their users are reconciling lists, not browsing. Touch targets never drop below 44dp on either —
/// a compact row is shorter, not harder to hit.
enum AhDensity {
  comfortable(controlHeight: 48, rowHeight: 56, padX: 16, padY: 10, gap: 16),
  compact(controlHeight: 44, rowHeight: 48, padX: 12, padY: 6, gap: 12);

  const AhDensity({
    required this.controlHeight,
    required this.rowHeight,
    required this.padX,
    required this.padY,
    required this.gap,
  });

  final double controlHeight;
  final double rowHeight;
  final double padX;
  final double padY;
  final double gap;
}
