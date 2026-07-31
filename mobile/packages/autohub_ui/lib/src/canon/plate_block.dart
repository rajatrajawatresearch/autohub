import 'package:flutter/widgets.dart';

import '../tokens/colors.dart';
import '../tokens/typography.dart';

/// The five plate classes that exist on Indian roads, coloured the way they actually are.
///
/// | class        | plate  | lettering |
/// |--------------|--------|-----------|
/// | [private]    | white  | black     |
/// | [commercial] | yellow | black     |
/// | [ev]         | green  | white     |
/// | [government] | white  | black     |
/// | [defence]    | black  | white     |
///
/// These are not theme decisions and they do not flip in dark mode. A user matching a plate on a
/// screen against the car standing in front of them needs the two to agree, and on a phone they are
/// usually doing exactly that.
enum PlateClass { private, commercial, ev, government, defence }

/// How large the plate reads.
enum PlateSize { xs, sm, md, lg }

/// The signature.
///
/// Every vehicle in AutoHub is identified by a rendering of its actual registration plate — in
/// search, in an auction lot, in a dealer's inventory, on an inspection sheet. It is not decoration:
/// it is the primary key made visible, and it is the one thing a user can check against the object
/// in front of them.
///
/// Ported from `packages/ui/src/canon/plate-block.tsx`, deliberately down to the grouping rule, so
/// the same vehicle looks the same on a phone and on a laptop.
class PlateBlock extends StatelessWidget {
  const PlateBlock({
    required this.registration,
    this.display,
    this.plateClass = PlateClass.private,
    this.size = PlateSize.md,
    super.key,
  });

  /// Canonical registration from the API — `MH12AB1234`.
  final String registration;

  /// The server's pre-grouped form when there is one.
  ///
  /// Trusting it keeps the client from re-deriving a formatting decision the registry already made,
  /// and keeps a future series the regex does not know about rendering correctly.
  final String? display;

  final PlateClass plateClass;
  final PlateSize size;

  @override
  Widget build(BuildContext context) {
    final grouped = (display != null && display!.trim().isNotEmpty)
        ? display!.trim().toUpperCase()
        : groupRegistration(registration);

    final metrics = _metrics(size);
    final palette = _palette(plateClass);

    return Semantics(
      label: 'Registration $grouped',
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: palette.fill,
          // A plate has square corners and a hard border. Rounding it turns it into a chip.
          borderRadius: const BorderRadius.all(AhRadius.plate),
          border: Border.all(color: palette.border, width: metrics.borderWidth),
        ),
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: metrics.padX, vertical: metrics.padY),
          child: Text(
            grouped,
            style: AhText.plate.copyWith(
              fontSize: metrics.fontSize,
              letterSpacing: metrics.tracking,
              color: palette.ink,
            ),
            maxLines: 1,
            softWrap: false,
          ),
        ),
      ),
    );
  }

  static _PlateMetrics _metrics(PlateSize size) => switch (size) {
        PlateSize.xs => const _PlateMetrics(11, 1.1, 6, 2, 1),
        PlateSize.sm => const _PlateMetrics(13, 1.6, 8, 4, 1.5),
        PlateSize.md => const _PlateMetrics(16, 2.2, 10, 6, 2),
        PlateSize.lg => const _PlateMetrics(20, 2.8, 16, 10, 2),
      };

  static _PlatePalette _palette(PlateClass plateClass) => switch (plateClass) {
        PlateClass.private =>
          const _PlatePalette(AhColors.plate, AhColors.tarmac, AhColors.tarmac),
        PlateClass.commercial =>
          const _PlatePalette(AhColors.signal, AhColors.tarmac, AhColors.tarmac),
        PlateClass.ev =>
          const _PlatePalette(AhColors.highway, AhColors.plate, AhColors.highway),
        PlateClass.government =>
          const _PlatePalette(AhColors.plate, AhColors.tarmac, AhColors.tarmac),
        PlateClass.defence =>
          const _PlatePalette(AhColors.tarmac, AhColors.plate, AhColors.tarmac),
      };
}

/// Groups a canonical registration the way a person proofreads it off a windscreen.
///
/// `MH12AB1234` becomes `MH 12 AB 1234`. Splitting on runs of letters and digits covers every series
/// without a per-format table — the state series, the BH series (`22BH1234AA`), and a partial string
/// typed one character at a time, which is why the plate input shares this function.
///
/// Grouping is not decoration. Ungrouped, a ten-character plate is measurably easier to transpose
/// two digits in, and the user is usually copying it from a car rather than reading it back.
String groupRegistration(String value) {
  final canonical = value.replaceAll(RegExp('[^0-9A-Za-z]'), '').toUpperCase();
  final runs = RegExp('[0-9]+|[A-Z]+').allMatches(canonical).map((m) => m.group(0)!);
  return runs.join(' ');
}

@immutable
class _PlateMetrics {
  const _PlateMetrics(this.fontSize, this.tracking, this.padX, this.padY, this.borderWidth);

  final double fontSize;
  final double tracking;
  final double padX;
  final double padY;
  final double borderWidth;
}

@immutable
class _PlatePalette {
  const _PlatePalette(this.fill, this.ink, this.border);

  final Color fill;
  final Color ink;
  final Color border;
}
