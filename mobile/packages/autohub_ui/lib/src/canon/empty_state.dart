import 'package:flutter/widgets.dart';

import '../theme/ah_theme.dart';
import '../tokens/typography.dart';

/// An empty state is an instruction, never a mood.
///
/// *"No bids yet. Bidding opens when the reserve is set."* tells a dealer what is true and what
/// makes it stop being true. An illustration of a sad car tells them nothing and costs a download.
/// So there is no illustration slot: [icon] exists for a single hairline glyph where a surface
/// genuinely needs one, and the words do the work.
///
/// Both strings are supplied by the caller and both should come from the localisation registry.
/// A sentence hardcoded here is a sentence that cannot ship in Marathi without an app release —
/// which is the one thing the i18n registry exists to prevent.
class EmptyState extends StatelessWidget {
  const EmptyState({
    required this.title,
    this.body,
    this.action,
    this.icon,
    this.align = CrossAxisAlignment.center,
    super.key,
  });

  /// What is true now. "No bids yet."
  final String title;

  /// What makes it stop being true. "Bidding opens when the reserve is set."
  final String? body;

  /// The control that does the thing the body just described, when there is one the user can
  /// press. An empty state that only reports is fine; most of them are.
  final Widget? action;

  final IconData? icon;

  final CrossAxisAlignment align;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final centred = align == CrossAxisAlignment.center;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.sunken,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AhSpace.xl, vertical: AhSpace.xxl),
        child: Column(
          crossAxisAlignment: align,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            if (icon != null) ...<Widget>[
              Icon(icon, size: 24, color: surfaces.textSecondary),
              const SizedBox(height: AhSpace.md),
            ],
            Text(
              title,
              style: AhText.subheading.copyWith(color: surfaces.textPrimary),
              textAlign: centred ? TextAlign.center : TextAlign.start,
            ),
            if (body != null) ...<Widget>[
              const SizedBox(height: AhSpace.sm),
              ConstrainedBox(
                // Roughly 42 characters at body size. A measure this narrow is not a style
                // preference — an instruction the user has to act on is read once, and a
                // full-width line on a tablet loses its place between lines.
                constraints: const BoxConstraints(maxWidth: 320),
                child: Text(
                  body!,
                  style: AhText.body.copyWith(color: surfaces.textSecondary),
                  textAlign: centred ? TextAlign.center : TextAlign.start,
                ),
              ),
            ],
            if (action != null) ...<Widget>[
              const SizedBox(height: AhSpace.lg),
              action!,
            ],
          ],
        ),
      ),
    );
  }
}
