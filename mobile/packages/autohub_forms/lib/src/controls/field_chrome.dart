/// The handful of shapes a form needs that the `autohub_ui` canon does not already publish.
///
/// It is a short file on purpose, and it should stay short. Inputs are [AhTextField], buttons are
/// [AhButton], the registration plate is `PlateInput`, the caption-over-value atom is `FieldBlock`.
/// Anything drawn here is drawn because a *form* needs it and a listing screen does not — a group
/// enclosure, a retry line for a taxonomy that did not load.
///
/// Nothing here names a colour. Every value comes from `context.ahSurfaces`, which is the Flutter
/// analogue of the web's `var(--accent-primary)`: this package names a role and `autohub_ui`
/// decides what the role looks like, in light and in dark.
library;

import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';


/// The uppercase letter-spaced caption that sits above every value.
///
/// This one repetition does more for coherence than any other decision in the design language, and
/// it is the first thing to copy when building a surface that does not exist yet.
class AhLabelText extends StatelessWidget {
  const AhLabelText(this.text, {this.tone, super.key});

  final String text;
  final Color? tone;

  @override
  Widget build(BuildContext context) => Text(
        text.toUpperCase(),
        style: AhText.label.copyWith(color: tone ?? context.ahSurfaces.textSecondary),
      );
}

/// A short sentence in the secondary tone — help text, "Loading options", "No options available".
class AhQuietText extends StatelessWidget {
  const AhQuietText(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) => Text(
        text,
        style: AhText.bodySm.copyWith(color: context.ahSurfaces.textSecondary),
      );
}

/// A bordered enclosure for controls that are a group rather than a single input — a radio list, a
/// tick list, an address block, a cascading picker.
///
/// Structure comes from hairlines, never from shadows. Without it a five-option radio list and the
/// help text under the field above it read as one undifferentiated column of prose.
class AhFieldBox extends StatelessWidget {
  const AhFieldBox({required this.child, this.invalid = false, this.padded = true, super.key});

  final Widget child;
  final bool invalid;

  /// `false` when the child draws its own row padding, so a list of 48dp rows can reach the border.
  final bool padded;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: invalid ? AhColors.flag : surfaces.borderHairline),
      ),
      child: padded ? Padding(padding: const EdgeInsets.all(AhSpace.md), child: child) : child,
    );
  }
}

/// The retry affordance a control shows when its option list did not arrive.
///
/// An empty dropdown and a failed fetch look identical to a user and are opposite problems. Saying
/// which one happened, and offering the one action that fixes it, is the difference between a
/// seller retrying and a seller closing the app on the screen that creates supply.
class AhOptionsFailure extends StatelessWidget {
  const AhOptionsFailure({
    required this.message,
    required this.retryLabel,
    required this.onRetry,
    super.key,
  });

  final String message;
  final String retryLabel;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Row(
        children: <Widget>[
          Expanded(child: AhQuietText(message)),
          const SizedBox(width: AhSpace.md),
          AhButton(
            label: retryLabel,
            onPressed: onRetry,
            variant: AhButtonVariant.outline,
            size: AhButtonSize.sm,
          ),
        ],
      );
}

/// A left-ruled block in the flag colour: the shape every fault in the product takes.
///
/// A rule rather than an icon, deliberately. The rule survives being read in sunlight on an
/// inspection screen where a 12dp glyph does not, and it costs no width on a 360dp handset.
class AhFaultBlock extends StatelessWidget {
  const AhFaultBlock({required this.message, this.code, this.emphasis = false, super.key});

  final String message;

  /// The registry's machine code, printed in mono beside the sentence.
  ///
  /// This is the entire point of the contract's `code` field: a seller sends a screenshot, support
  /// reads `kms.too_high` off it, and nobody has to guess which of four validation rules fired or
  /// ask a user to describe an error they have already dismissed.
  final String? code;

  final bool emphasis;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    return Semantics(
      liveRegion: true,
      child: Container(
        padding: const EdgeInsets.only(left: AhSpace.sm),
        decoration: const BoxDecoration(
          border: Border(left: BorderSide(color: AhColors.flag, width: 2)),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Expanded(
              child: Text(
                message,
                style: (emphasis ? AhText.body : AhText.bodySm)
                    .copyWith(color: surfaces.textPrimary),
              ),
            ),
            if (code != null) ...<Widget>[
              const SizedBox(width: AhSpace.sm),
              Text(code!, style: AhText.label.copyWith(color: surfaces.textSecondary)),
            ],
          ],
        ),
      ),
    );
  }
}
