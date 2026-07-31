import 'package:flutter/widgets.dart';

import '../theme/ah_theme.dart';
import '../tokens/typography.dart';

enum SectionHeadingSize { sm, md, lg }

/// A section header in the display face, carrying the same uppercase eyebrow that sits above
/// every value in the product, and a right-side slot for the one action the section owns.
///
/// The optional hairline rule is how sections separate. Structure comes from rules — not from
/// elevation, not from a coloured band, not from a card inside a card.
class SectionHeading extends StatelessWidget {
  const SectionHeading({
    required this.title,
    this.eyebrow,
    this.description,
    this.action,
    this.size = SectionHeadingSize.md,
    this.rule = false,
    super.key,
  });

  final String title;

  /// Rendered as the label eyebrow above the title: "Auction", "Inventory", "Step 2 of 4".
  final String? eyebrow;

  final String? description;

  /// One control, right-aligned. Two controls means the section is doing two jobs, and the fix is
  /// two sections rather than a wider slot — which is why this is a single [Widget] and not a
  /// list.
  final Widget? action;

  final SectionHeadingSize size;

  /// Hairline rule under the block.
  final bool rule;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;

    final block = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        if (eyebrow != null) ...<Widget>[
          Text(
            eyebrow!.toUpperCase(),
            style: AhText.label.copyWith(color: surfaces.textSecondary),
          ),
          const SizedBox(height: AhSpace.xs + 2),
        ],
        Text(title, style: _titleStyle(size).copyWith(color: surfaces.textPrimary)),
        if (description != null) ...<Widget>[
          const SizedBox(height: AhSpace.xs + 2),
          Text(
            description!,
            style: AhText.body.copyWith(color: surfaces.textSecondary),
          ),
        ],
      ],
    );

    final row = action == null
        ? block
        : Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: <Widget>[
              Expanded(child: block),
              const SizedBox(width: AhSpace.lg),
              action!,
            ],
          );

    if (!rule) return row;

    return DecoratedBox(
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: surfaces.borderHairline)),
      ),
      child: Padding(
        padding: const EdgeInsets.only(bottom: AhSpace.md),
        child: row,
      ),
    );
  }

  static TextStyle _titleStyle(SectionHeadingSize size) => switch (size) {
        SectionHeadingSize.sm => AhText.subheading,
        SectionHeadingSize.md => AhText.heading,
        SectionHeadingSize.lg => AhText.displayLg,
      };
}
