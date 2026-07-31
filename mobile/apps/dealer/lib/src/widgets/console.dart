import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

/// One figure and what it is.
///
/// Deliberately a [FieldBlock] in a hairline cell and nothing else. No donut, no sparkline, no
/// gradient card, no coloured tile: `01-design-system.md` calls the language instrumentation rather
/// than merchandising, and a dealer reconciling stock at 9am is reading numbers, not admiring a
/// dashboard. A chart here would occupy the space of four figures to say less than one of them.
@immutable
final class ConsoleMetric {
  const ConsoleMetric({required this.label, required this.value, this.hint});

  final String label;

  /// Usually a [MoneyValue] or a mono numeral. A widget rather than a string because money must go
  /// through the one formatter and a count must be tabular.
  final Widget value;

  /// A qualifier under the figure — a unit, a denominator, an "as of". A number without one is a
  /// rumour.
  ///
  /// This is where a figure earns its colour, or rather where it does not: a metric that needs to
  /// shout is a metric with a hint saying why. Colour is reserved for status, and an ageing count
  /// is a fact, not a status.
  final String? hint;
}

/// The metric strip: a hairline grid, two columns on a handset.
///
/// A grid of rules rather than a row of cards. Structure comes from hairlines in this design
/// system, and cards would put five borders and five shadows on the densest surface in the product.
class ConsoleMetrics extends StatelessWidget {
  const ConsoleMetrics({required this.metrics, this.columns = 2, super.key});

  final List<ConsoleMetric> metrics;

  /// Two on a phone. The strip is meant to be read at a glance while standing, and three columns
  /// of `₹1.24Cr` at 360dp truncates the one thing the dealer came for.
  final int columns;

  @override
  Widget build(BuildContext context) {
    if (metrics.isEmpty) return const SizedBox.shrink();

    final surfaces = context.ahSurfaces;
    final density = context.ahDensity;
    final rows = (metrics.length + columns - 1) ~/ columns;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          for (var row = 0; row < rows; row += 1)
            DecoratedBox(
              decoration: BoxDecoration(
                border: row == 0
                    ? null
                    : Border(top: BorderSide(color: surfaces.borderHairline)),
              ),
              child: IntrinsicHeight(
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: <Widget>[
                    for (var column = 0; column < columns; column += 1)
                      Expanded(
                        child: DecoratedBox(
                          decoration: BoxDecoration(
                            border: column == 0
                                ? null
                                : Border(
                                    left: BorderSide(color: surfaces.borderHairline),
                                  ),
                          ),
                          child: Padding(
                            padding: EdgeInsets.symmetric(
                              horizontal: density.padX,
                              vertical: density.padY + AhSpace.sm,
                            ),
                            child: _cellAt(row * columns + column),
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }

  /// An empty trailing cell still draws its rule, so the grid stays a grid. A short last row that
  /// loses its divider reads as a rendering fault rather than as five metrics.
  Widget _cellAt(int index) {
    if (index >= metrics.length) return const SizedBox.shrink();
    final metric = metrics[index];
    return FieldBlock(
      label: metric.label,
      valueWidget: metric.value,
      hint: metric.hint,
    );
  }
}

/// A dense list row, divided by a hairline from the next.
///
/// The console's atom. Everything a dealer scans — a stock line, a deal, a bid on the ladder — is
/// this: an identifier on the left, the thing on the right, and the numbers in mono so a column of
/// them lines up. Row height comes from `AhDensity`, so the same widget is 48dp here and 56dp in
/// the customer binary without a call site knowing which app it is in.
class ConsoleRow extends StatelessWidget {
  const ConsoleRow({
    required this.child,
    this.onTap,
    this.divider = true,
    super.key,
  });

  final Widget child;
  final VoidCallback? onTap;

  /// Off on the last row of a list, so the group does not end in a floating rule.
  final bool divider;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final density = context.ahDensity;

    final body = DecoratedBox(
      decoration: BoxDecoration(
        border: divider
            ? Border(bottom: BorderSide(color: surfaces.borderHairline))
            : null,
      ),
      child: ConstrainedBox(
        // The floor, from `ahMinTouchTarget`. Compact earns its density by cutting padding, never
        // by making a row harder to hit — the dealer is one-handed on a showroom floor.
        constraints: BoxConstraints(minHeight: context.ahTapTarget(density.rowHeight)),
        child: Padding(
          padding: EdgeInsets.symmetric(vertical: density.padY),
          child: child,
        ),
      ),
    );

    if (onTap == null) return body;

    return Material(
      type: MaterialType.transparency,
      child: InkWell(
        onTap: onTap,
        splashFactory: NoSplash.splashFactory,
        hoverColor: Colors.transparent,
        focusColor: Colors.transparent,
        highlightColor: Colors.transparent,
        child: body,
      ),
    );
  }
}

/// The page header every console screen opens with.
///
/// A thin wrapper over [SectionHeading] that pins the eyebrow/title/description shape, so the five
/// screens cannot each invent their own spacing above the fold.
class ConsolePageHeader extends StatelessWidget {
  const ConsolePageHeader({
    required this.eyebrow,
    required this.title,
    this.description,
    this.action,
    super.key,
  });

  final String eyebrow;
  final String title;
  final String? description;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return SectionHeading(
      eyebrow: eyebrow,
      title: title,
      description: description,
      action: action,
    );
  }
}

/// The gap between blocks on a console page. One constant so five screens agree.
const double kConsoleBlockGap = AhSpace.xl;

/// Page padding. Horizontal is tight on purpose — the dealer paid for the pixels.
EdgeInsets consolePagePadding(BuildContext context) => EdgeInsets.symmetric(
      horizontal: context.ahDensity.padX,
      vertical: AhSpace.lg,
    );
