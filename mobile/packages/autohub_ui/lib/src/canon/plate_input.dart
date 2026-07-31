import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/ah_density.dart';
import '../theme/ah_theme.dart';
import '../tokens/colors.dart';
import '../tokens/typography.dart';
import 'plate_block.dart';

enum PlateInputSize { sm, md, lg }

/// The customer homepage does not open with a photograph of a car. It opens with an empty plate,
/// waiting to be typed in.
///
/// This is the thesis rendered as an interaction: the most characteristic artifact in the
/// subject's world doing the most valuable job in the product. One input, one outcome, no funnel.
///
/// The field **is** the plate — the same hard border, the same [AhFonts.mono], the same tracking,
/// the same zero radius, the same plate-white fill. It does not flip in dark mode, for the same
/// reason [PlateBlock] does not: the user is frequently standing next to the car, and the screen
/// and the windscreen have to agree.
///
/// Behaviour, ported from `packages/ui/src/canon/plate-input.tsx`:
///
/// - the user types `mh12ab1234`, the field shows `MH 12 AB 1234`, the caller receives `MH12AB1234`
/// - separators are stripped as they are typed, so a paste from an RC photo, an SMS or a WhatsApp
///   forward all land in the same canonical place
/// - regrouping happens live through [groupRegistration] — the same function [PlateBlock] renders
///   with, so a half-typed plate and a finished one are grouped by one rule
///
/// The caret is the fiddly part and the reason this is not a bare [TextField]. Regrouping rewrites
/// the string on every keystroke, and a naively restored caret jumps to the end the moment a user
/// corrects a digit in the middle — which they do constantly, because they are reading a plate off
/// a car. So the caret is carried across the rewrite as "how many alphanumerics precede it", the
/// only position that survives a change in where the spaces are.
class PlateInput extends StatefulWidget {
  const PlateInput({
    required this.value,
    required this.onChanged,
    this.onSubmitted,
    this.label,
    this.hint,
    this.error,
    this.placeholder = 'MH 12 AB 1234',
    this.size = PlateInputSize.md,
    this.enabled = true,
    this.autofocus = false,
    this.maxChars = 11,
    this.focusNode,
    super.key,
  });

  /// Canonical — uppercase, no separators. This widget owns the display, the caller owns the state.
  final String value;

  /// Receives the canonical value on every keystroke.
  final ValueChanged<String> onChanged;

  /// Fired by the keyboard's Go key. The page owns the call to action; the field never navigates.
  final ValueChanged<String>? onSubmitted;

  final String? label;
  final String? hint;

  /// Resolved message from the form registry's validation rule.
  final String? error;

  final String placeholder;
  final PlateInputSize size;
  final bool enabled;
  final bool autofocus;

  /// Input guard only — 11 characters is the longest registration any Indian series produces,
  /// including BH. The actual validation rule and its message come from the form schema registry,
  /// because "what counts as a valid plate" is a business answer and this is a text box.
  final int maxChars;

  final FocusNode? focusNode;

  @override
  State<PlateInput> createState() => _PlateInputState();
}

class _PlateInputState extends State<PlateInput> {
  late final TextEditingController _controller;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: groupRegistration(widget.value));
  }

  @override
  void didUpdateWidget(PlateInput oldWidget) {
    super.didUpdateWidget(oldWidget);
    // Only rewrite when the parent genuinely disagrees with what is on screen. Comparing
    // canonical-to-canonical is what makes this safe: comparing display strings would rewrite the
    // field on every keystroke and destroy the caret work the formatter just did.
    if (widget.value != _canonical(_controller.text)) {
      _controller.value = TextEditingValue(
        text: groupRegistration(widget.value),
        selection: TextSelection.collapsed(offset: groupRegistration(widget.value).length),
      );
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final metrics = _metrics(widget.size);
    final invalid = widget.error != null;

    final field = TextField(
      controller: _controller,
      focusNode: widget.focusNode,
      enabled: widget.enabled,
      autofocus: widget.autofocus,
      onChanged: (display) => widget.onChanged(_canonical(display)),
      onSubmitted: (display) => widget.onSubmitted?.call(_canonical(display)),
      inputFormatters: <TextInputFormatter>[_PlateFormatter(widget.maxChars)],
      textAlign: TextAlign.center,
      textCapitalization: TextCapitalization.characters,
      // An Indian plate is mostly digits, and `visiblePassword` is the one keyboard type that
      // puts a number row in front of an alphanumeric layout on Android without switching the
      // user into a numeric-only pad they then cannot type "MH" on. It also suppresses the
      // autocorrect and suggestion strip, which on a registration number are actively harmful.
      keyboardType: TextInputType.visiblePassword,
      autocorrect: false,
      enableSuggestions: false,
      textInputAction: TextInputAction.go,
      style: AhText.plate.copyWith(
        fontSize: metrics.fontSize,
        letterSpacing: metrics.tracking,
        color: AhColors.tarmac,
      ),
      cursorColor: AhColors.highway,
      cursorWidth: 2,
      cursorRadius: Radius.zero,
      decoration: InputDecoration(
        isDense: true,
        filled: true,
        fillColor: AhColors.plate,
        hintText: widget.placeholder,
        hintStyle: AhText.plate.copyWith(
          fontSize: metrics.fontSize,
          letterSpacing: metrics.tracking,
          fontWeight: FontWeight.w400,
          color: AhColors.n400,
        ),
        contentPadding: EdgeInsets.symmetric(horizontal: metrics.padX, vertical: metrics.padY),
        border: _border(invalid),
        enabledBorder: _border(invalid),
        disabledBorder: _border(invalid),
        // A plate stays a plate when focused. The ring is the border going heavier, not a second
        // rectangle around it — a glowing outline would be the first non-printed thing on screen.
        focusedBorder: OutlineInputBorder(
          borderRadius: const BorderRadius.all(AhRadius.plate),
          borderSide: BorderSide(color: invalid ? AhColors.flag : AhColors.highway, width: 3),
        ),
      ),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        if (widget.label != null) ...<Widget>[
          Text(
            widget.label!.toUpperCase(),
            style: AhText.label.copyWith(color: surfaces.textSecondary),
          ),
          const SizedBox(height: AhSpace.sm),
        ],
        ConstrainedBox(
          constraints: BoxConstraints(minHeight: context.ahTapTarget(metrics.height)),
          child: field,
        ),
        if (invalid) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          Semantics(
            liveRegion: true,
            child: DecoratedBox(
              decoration: const BoxDecoration(
                color: AhColors.flagSoft,
                border: Border(left: BorderSide(color: AhColors.flag, width: 2)),
              ),
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: AhSpace.sm,
                  vertical: AhSpace.xs,
                ),
                child: Text(
                  widget.error!,
                  style: AhText.bodySm.copyWith(color: AhColors.flag),
                ),
              ),
            ),
          ),
        ] else if (widget.hint != null) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          Text(
            widget.hint!,
            style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
          ),
        ],
      ],
    );
  }

  static OutlineInputBorder _border(bool invalid) => OutlineInputBorder(
        borderRadius: const BorderRadius.all(AhRadius.plate),
        borderSide: BorderSide(color: invalid ? AhColors.flag : AhColors.tarmac, width: 2),
      );

  static _PlateInputMetrics _metrics(PlateInputSize size) => switch (size) {
        PlateInputSize.sm => const _PlateInputMetrics(40, 15, 1.6, 12, 8),
        PlateInputSize.md => const _PlateInputMetrics(52, 20, 2.8, 16, 12),
        // The hero. Big enough to read at arm's length while holding the phone up next to a
        // windscreen, which is the posture this input was designed for.
        PlateInputSize.lg => const _PlateInputMetrics(68, 28, 3.4, 20, 16),
      };
}

/// Strips separators, uppercases, clips to the series length, regroups, and puts the caret back
/// where the user left it.
///
/// Written as a [TextInputFormatter] rather than as an `onChanged` round-trip because the
/// formatter runs *inside* the edit — before the framework commits a value and before the
/// platform's text input plugin is told about it. Rewriting the controller afterwards works on
/// Android and produces a visible flicker plus a fight with the IME on iOS.
@immutable
class _PlateFormatter extends TextInputFormatter {
  const _PlateFormatter(this.maxChars);

  static final RegExp _separators = RegExp('[^0-9A-Za-z]');

  final int maxChars;

  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) {
    final raw = newValue.text;
    final canonical = raw.replaceAll(_separators, '').toUpperCase();
    final clipped =
        canonical.length > maxChars ? canonical.substring(0, maxChars) : canonical;
    final display = groupRegistration(clipped);

    // The caret, expressed as "how many alphanumerics precede it" — the only position that
    // survives regrouping, because the spaces move but the characters do not.
    final rawCaret = newValue.selection.baseOffset;
    final safeCaret = rawCaret < 0 ? raw.length : math.min(rawCaret, raw.length);
    final target = math.min(
      raw.substring(0, safeCaret).replaceAll(_separators, '').length,
      clipped.length,
    );

    var offset = target == 0 ? 0 : display.length;
    if (target > 0) {
      var seen = 0;
      for (var i = 0; i < display.length; i += 1) {
        if (display[i] == ' ') continue;
        seen += 1;
        if (seen == target) {
          offset = i + 1;
          break;
        }
      }
    }

    return TextEditingValue(
      text: display,
      selection: TextSelection.collapsed(offset: offset),
      // Cleared deliberately. A composing region left pointing into the pre-regrouping string is
      // how an IME ends up duplicating characters on the next keystroke.
      composing: TextRange.empty,
    );
  }
}

String _canonical(String display) =>
    display.replaceAll(RegExp('[^0-9A-Za-z]'), '').toUpperCase();

@immutable
class _PlateInputMetrics {
  const _PlateInputMetrics(this.height, this.fontSize, this.tracking, this.padX, this.padY);

  final double height;
  final double fontSize;
  final double tracking;
  final double padX;
  final double padY;
}
