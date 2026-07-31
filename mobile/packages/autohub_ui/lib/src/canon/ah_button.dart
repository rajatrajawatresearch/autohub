import 'package:flutter/material.dart';

import '../theme/ah_density.dart';
import '../theme/ah_theme.dart';
import '../tokens/colors.dart';
import '../tokens/typography.dart';

/// What a button means. Never what colour it is.
enum AhButtonVariant {
  /// Highway green, light text. Structure and trust — the default for almost everything.
  primary,

  /// Signal yellow, **dark text**.
  ///
  /// **Rationed to one per screen.** It marks the single most consequential action in the
  /// viewport — Place bid, Get valuation, Accept offer. A second yellow control in the same view
  /// is a bug, not a style choice, because the entire value of this colour is that it means
  /// "this one". If two actions on a screen both feel like they deserve it, the screen is doing
  /// two jobs and should be two screens.
  ///
  /// Note that an `AhBadgeTone.action` badge and a live [BidTicket] countdown strip also spend
  /// that budget. Count them.
  ///
  /// It never takes white text. White on [AhColors.signal] fails contrast at every size, which is
  /// why the polarity is fixed here rather than left to the caller.
  action,

  /// Bordered, transparent. The secondary action beside a primary.
  outline,

  /// No border, no fill. Toolbars, list-row affordances, "Cancel".
  ghost,

  /// Destructive and irreversible — withdraw a listing, cancel a deal.
  danger,
}

enum AhButtonSize { sm, md, lg, icon }

/// Flat, near-square, bordered. A control on an instrument, not a call to action on a shopfront.
///
/// Heights come from [AhDensity], so switching the dealer binary to `compact` retunes every
/// button in it without a single call site knowing. What density may **not** do is shrink the hit
/// area: the painted box gets smaller and the gesture target is padded back out to
/// [ahMinTouchTarget]. A dealer tapping "Place bid" one-handed on a showroom floor does not care
/// that the row is 8dp tighter; they care that they hit it.
///
/// There is no ripple and no loading spinner. Both are ambient motion, and this product spends
/// its whole motion budget on the odometer roll. A button that is working is a button whose
/// `onPressed` the caller has set to `null` and whose label the caller has changed.
class AhButton extends StatefulWidget {
  const AhButton({
    required this.label,
    required this.onPressed,
    this.variant = AhButtonVariant.primary,
    this.size = AhButtonSize.md,
    this.icon,
    this.block = false,
    super.key,
  });

  /// Already localised. Say what happens — "Place bid", never "Submit".
  ///
  /// Still required for [AhButtonSize.icon], where it is not painted but becomes the semantics
  /// label. An icon-only control with no accessible name is unusable with a screen reader, and
  /// making the parameter optional is how that ships.
  final String label;

  /// `null` disables the button. There is no separate `enabled` flag, so a button with no handler
  /// cannot look live.
  final VoidCallback? onPressed;

  final AhButtonVariant variant;
  final AhButtonSize size;
  final IconData? icon;

  /// Fills its row. The default in a form column and in a bottom sheet, where a control owns the
  /// full width and a centred pill would read as a suggestion.
  final bool block;

  @override
  State<AhButton> createState() => _AhButtonState();
}

class _AhButtonState extends State<AhButton> {
  bool _focused = false;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final density = context.ahDensity;
    final palette = _palette(widget.variant, surfaces);
    final visualHeight = _height(widget.size, density);
    final enabled = widget.onPressed != null;
    final isIcon = widget.size == AhButtonSize.icon;

    final painted = DecoratedBox(
      decoration: BoxDecoration(
        color: palette.fill,
        borderRadius: const BorderRadius.all(AhRadius.control),
        // The focus ring replaces the border rather than sitting outside it, so gaining focus
        // never changes the button's size and never nudges the row it sits in.
        border: Border.all(
          color: _focused ? surfaces.focusRing : palette.border,
          width: _focused ? 2 : 1,
        ),
      ),
      child: SizedBox(
        height: visualHeight,
        width: isIcon
            ? visualHeight
            : (widget.block ? double.infinity : null),
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: isIcon ? 0 : _padX(widget.size, density)),
          child: Row(
            mainAxisSize: widget.block ? MainAxisSize.max : MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            children: <Widget>[
              if (widget.icon != null) ...<Widget>[
                Icon(widget.icon, size: 16, color: palette.ink),
                if (!isIcon) const SizedBox(width: AhSpace.sm),
              ],
              if (!isIcon)
                Flexible(
                  child: Text(
                    widget.label,
                    style: _labelStyle(widget.size).copyWith(color: palette.ink),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    textAlign: TextAlign.center,
                  ),
                ),
            ],
          ),
        ),
      ),
    );

    return Semantics(
      button: true,
      enabled: enabled,
      label: widget.label,
      excludeSemantics: true,
      child: Opacity(
        opacity: enabled ? 1 : 0.45,
        child: Material(
          type: MaterialType.transparency,
          child: InkWell(
            onTap: widget.onPressed,
            onFocusChange: (value) => setState(() => _focused = value),
            // Every ink effect off. The ripple is the only animation Material insists on and it
            // is exactly the ambient motion this design system spent its budget avoiding.
            splashFactory: NoSplash.splashFactory,
            hoverColor: Colors.transparent,
            focusColor: Colors.transparent,
            highlightColor: Colors.transparent,
            child: ConstrainedBox(
              // The floor. A `sm` button in a compact toolbar paints at 35dp and is still hit at
              // 44dp: the InkWell owns this box, not the painted one, so the padding is real
              // touch area rather than decoration.
              constraints: BoxConstraints(minHeight: context.ahTapTarget(visualHeight)),
              child: Align(
                widthFactor: widget.block ? null : 1.0,
                heightFactor: 1.0,
                child: painted,
              ),
            ),
          ),
        ),
      ),
    );
  }

  static TextStyle _labelStyle(AhButtonSize size) {
    final base = size == AhButtonSize.lg ? AhText.body : AhText.bodySm;
    return base.copyWith(fontWeight: FontWeight.w600, height: 1);
  }

  static double _height(AhButtonSize size, AhDensity density) => switch (size) {
        AhButtonSize.sm => density.controlHeight * 0.8,
        AhButtonSize.md => density.controlHeight,
        AhButtonSize.lg => density.controlHeight * 1.15,
        // Square, and the same height as `md`, so it lines up in a toolbar without a wrapper.
        AhButtonSize.icon => density.controlHeight,
      };

  static double _padX(AhButtonSize size, AhDensity density) => switch (size) {
        AhButtonSize.sm => density.padX * 0.75,
        AhButtonSize.md => density.padX,
        AhButtonSize.lg => density.padX * 1.5,
        AhButtonSize.icon => 0,
      };

  static _ButtonPalette _palette(AhButtonVariant variant, AhSurfaces surfaces) => switch (variant) {
        AhButtonVariant.primary => _ButtonPalette(
            surfaces.accentPrimary,
            surfaces.textOnPrimary,
            surfaces.accentPrimary,
          ),
        AhButtonVariant.action => _ButtonPalette(
            surfaces.accentAction,
            surfaces.textOnAccent,
            surfaces.accentAction,
          ),
        AhButtonVariant.outline => _ButtonPalette(
            Colors.transparent,
            surfaces.textPrimary,
            surfaces.borderStrong,
          ),
        AhButtonVariant.ghost => _ButtonPalette(
            Colors.transparent,
            surfaces.textPrimary,
            Colors.transparent,
          ),
        AhButtonVariant.danger =>
          const _ButtonPalette(AhColors.flag, AhColors.plate, AhColors.flag),
      };
}

@immutable
class _ButtonPalette {
  const _ButtonPalette(this.fill, this.ink, this.border);

  final Color fill;
  final Color ink;
  final Color border;
}
