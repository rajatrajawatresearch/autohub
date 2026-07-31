import 'package:flutter/widgets.dart';

import '../theme/ah_theme.dart';
import '../tokens/colors.dart';
import '../tokens/typography.dart';

/// A badge's meaning, never its colour.
///
/// Callers pick a tone, not a hex. `AhBadgeTone.flag` on both themes resolves to the one red this
/// product has, and changing that red is one edit in `colors.dart` rather than a grep.
enum AhBadgeTone {
  neutral,
  outline,
  primary,

  /// Live, urgent, money-moving. Shares the rationed yellow — **count it against the screen's
  /// one**, together with any [AhButtonVariant.action] button in view.
  action,

  /// Expired, rejected, overdue.
  flag,

  /// Pending, expiring soon.
  caution,

  /// Verified, settled, passed.
  clear,

  /// Informational, under review.
  info,
}

enum AhBadgeSize { sm, md }

/// A stamp on a document. Near-square, uppercase, letter-spaced.
///
/// Status badges are a solid fill with a fixed text polarity rather than a tinted chip, for two
/// reasons: a solid fill needs no dark-mode override, and every pair clears AA. A light chip with
/// [AhColors.caution] text does not — that amber is a fill colour and never a text colour, which
/// is the same reason the signal yellow always takes dark text.
///
/// A badge is a caller-supplied label. It is not [StatusPill]: a workflow state has a registry
/// category and must go through that widget so the tone is derived rather than chosen. Use this
/// for things that are genuinely not workflow — "RC verified", "Finance available", "Trending".
class AhBadge extends StatelessWidget {
  const AhBadge({
    required this.label,
    this.tone = AhBadgeTone.neutral,
    this.size = AhBadgeSize.md,
    this.icon,
    super.key,
  });

  /// Already localised by the caller. This widget never invents a word.
  final String label;

  final AhBadgeTone tone;
  final AhBadgeSize size;

  /// One hairline glyph, optional. Two glyphs is a toolbar, not a badge.
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final palette = _palette(context, tone, surfaces);
    final small = size == AhBadgeSize.sm;
    final fontSize = small ? 10.0 : 11.0;

    return Semantics(
      label: label,
      // As in StatusPill: the painted text is uppercased for typographic reasons and must not
      // reach a screen reader, which would otherwise announce the badge twice.
      excludeSemantics: true,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: palette.fill,
          borderRadius: const BorderRadius.all(AhRadius.control),
          border: Border.all(color: palette.border),
        ),
        child: Padding(
          padding: EdgeInsets.symmetric(
            horizontal: small ? AhSpace.xs + 2 : AhSpace.sm,
            vertical: small ? 1 : 3,
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              if (icon != null) ...<Widget>[
                Icon(icon, size: fontSize + 1, color: palette.ink),
                const SizedBox(width: AhSpace.xs),
              ],
              Text(
                label.toUpperCase(),
                style: AhText.label.copyWith(fontSize: fontSize, color: palette.ink),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      ),
    );
  }

  static _BadgePalette _palette(
    BuildContext context,
    AhBadgeTone tone,
    AhSurfaces surfaces,
  ) =>
      switch (tone) {
        AhBadgeTone.neutral =>
          _BadgePalette(surfaces.sunken, surfaces.textPrimary, surfaces.borderHairline),
        AhBadgeTone.outline => _BadgePalette(
            const Color(0x00000000),
            surfaces.textSecondary,
            surfaces.borderStrong,
          ),
        AhBadgeTone.primary => _BadgePalette(
            surfaces.accentPrimary,
            surfaces.textOnPrimary,
            surfaces.accentPrimary,
          ),
        AhBadgeTone.action => _BadgePalette(
            surfaces.accentAction,
            surfaces.textOnAccent,
            surfaces.accentAction,
          ),
        AhBadgeTone.flag =>
          const _BadgePalette(AhColors.flag, AhColors.plate, AhColors.flag),
        // Amber takes dark text, always.
        AhBadgeTone.caution =>
          const _BadgePalette(AhColors.caution, AhColors.tarmac, AhColors.caution),
        AhBadgeTone.clear =>
          const _BadgePalette(AhColors.clear, AhColors.plate, AhColors.clear),
        AhBadgeTone.info =>
          const _BadgePalette(AhColors.info, AhColors.plate, AhColors.info),
      };
}

@immutable
class _BadgePalette {
  const _BadgePalette(this.fill, this.ink, this.border);

  final Color fill;
  final Color ink;
  final Color border;
}
