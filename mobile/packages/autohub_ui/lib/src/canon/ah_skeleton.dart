import 'package:flutter/widgets.dart';

import '../theme/ah_theme.dart';
import '../tokens/typography.dart';

/// A held slot, not a shimmer.
///
/// The only animation in AutoHub is the odometer roll on a valuation reveal. A pulsing skeleton
/// would be ambient motion on every list, every card and every cold launch, and it would cost
/// that one moment the weight it is supposed to carry. So loading reads as an unfilled field on
/// a form — which is exactly what it is.
///
/// Its job is holding layout. Give it the final element's dimensions and nothing shifts when the
/// data lands. A skeleton that is the wrong size is worse than no skeleton: it promises a shape
/// and then moves the thing the user was reaching for.
class AhSkeleton extends StatelessWidget {
  const AhSkeleton({this.width, this.height = 16, this.radius = AhRadius.control, super.key});

  /// A single line of body text at the default height.
  const AhSkeleton.line({this.width, super.key})
      : height = 16,
        radius = AhRadius.control;

  /// A media slot. Pair it with the card's real aspect ratio, not a guess.
  const AhSkeleton.block({required this.height, this.width, super.key})
      : radius = AhRadius.card;

  /// `null` fills the available width.
  final double? width;
  final double height;
  final Radius radius;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;

    return ExcludeSemantics(
      child: SizedBox(
        width: width,
        height: height,
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: surfaces.sunken,
            borderRadius: BorderRadius.all(radius),
            border: Border.all(color: surfaces.borderHairline),
          ),
        ),
      ),
    );
  }
}
