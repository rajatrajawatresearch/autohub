import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/ah_density.dart';
import '../theme/ah_theme.dart';
import '../tokens/colors.dart';
import '../tokens/typography.dart';

enum AhFieldSize { sm, md, lg }

/// The general-purpose field: label above, control, message below.
///
/// It is the widget `autohub_forms` will map most schema controls onto, so its parameters are
/// deliberately the vocabulary a form schema speaks — `unit`, `error`, `hint`, `mono` — rather
/// than Flutter's. A control registry that has to translate `InputDecoration` is a control
/// registry nobody will extend.
///
/// [error] carries a **resolved message**, never a code. The validation rule and its wording both
/// come from the form schema registry; this widget only knows that a non-null message means the
/// field is wrong and must say so where a screen reader will find it.
class AhTextField extends StatelessWidget {
  const AhTextField({
    required this.controller,
    this.label,
    this.hint,
    this.error,
    this.placeholder,
    this.unit,
    this.mono = false,
    this.size = AhFieldSize.md,
    this.enabled = true,
    this.readOnly = false,
    this.obscure = false,
    this.maxLines = 1,
    this.maxLength,
    this.keyboardType,
    this.textInputAction,
    this.textCapitalization = TextCapitalization.none,
    this.inputFormatters,
    this.onChanged,
    this.onSubmitted,
    this.autofocus = false,
    this.focusNode,
    super.key,
  });

  final TextEditingController controller;

  /// Rendered as the same uppercase letter-spaced label that sits above every value in the
  /// product, so a form and a read-only detail page look like one document.
  final String? label;

  /// A qualifier under the field. Replaced by [error] when the field is invalid — two messages
  /// under one control is two things to read at the moment the user is already stuck.
  final String? hint;

  /// Resolved message from the form registry's validation rule. Non-null marks the field invalid.
  final String? error;

  final String? placeholder;

  /// The unit the field is measured in — `km`, `%`, `bhp`.
  ///
  /// Comes from the taxonomy attribute or the form schema, never from this widget. Rendered
  /// inside the field rather than as placeholder text, because a placeholder disappears the
  /// moment the user starts typing and the unit is exactly what they need while typing.
  final String? unit;

  /// Data, not prose: registration, VIN, chassis, engine number, amounts, ids. Mono with tabular
  /// figures and a slashed zero, because someone is transcribing this off a stamping under a
  /// bonnet with no context to disambiguate `0` from `O`.
  final bool mono;

  final AhFieldSize size;
  final bool enabled;
  final bool readOnly;
  final bool obscure;
  final int maxLines;
  final int? maxLength;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final TextCapitalization textCapitalization;
  final List<TextInputFormatter>? inputFormatters;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;
  final bool autofocus;
  final FocusNode? focusNode;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final density = context.ahDensity;
    final invalid = error != null;
    final height = _height(size, density);
    final textStyle = (mono ? AhText.data : AhText.body).copyWith(color: surfaces.textPrimary);

    final border = OutlineInputBorder(
      borderRadius: const BorderRadius.all(AhRadius.control),
      borderSide: BorderSide(color: invalid ? AhColors.flag : surfaces.borderStrong),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        if (label != null) ...<Widget>[
          Text(
            label!.toUpperCase(),
            style: AhText.label.copyWith(color: surfaces.textSecondary),
          ),
          const SizedBox(height: AhSpace.sm),
        ],
        ConstrainedBox(
          // Single-line fields hold the density height; a textarea grows past it. Either way the
          // touch target floor applies, because a field is tapped before it is typed in.
          constraints: BoxConstraints(
            minHeight: maxLines == 1 ? context.ahTapTarget(height) : height,
          ),
          child: TextField(
            controller: controller,
            focusNode: focusNode,
            enabled: enabled,
            readOnly: readOnly,
            obscureText: obscure,
            maxLines: obscure ? 1 : maxLines,
            maxLength: maxLength,
            keyboardType: keyboardType,
            textInputAction: textInputAction,
            textCapitalization: textCapitalization,
            inputFormatters: inputFormatters,
            onChanged: onChanged,
            onSubmitted: onSubmitted,
            autofocus: autofocus,
            style: textStyle,
            cursorColor: surfaces.accentPrimary,
            cursorWidth: 2,
            cursorRadius: Radius.zero,
            decoration: InputDecoration(
              isDense: true,
              filled: true,
              fillColor: enabled && !readOnly ? surfaces.card : surfaces.sunken,
              hintText: placeholder,
              hintStyle: textStyle.copyWith(color: surfaces.textSecondary),
              counterText: '',
              contentPadding: EdgeInsets.symmetric(
                horizontal: density.padX,
                vertical: maxLines == 1 ? (height - 20) / 2 : density.padY + AhSpace.xs,
              ),
              border: border,
              enabledBorder: border,
              disabledBorder: border,
              focusedBorder: OutlineInputBorder(
                borderRadius: const BorderRadius.all(AhRadius.control),
                borderSide: BorderSide(
                  color: invalid ? AhColors.flag : surfaces.focusRing,
                  width: 2,
                ),
              ),
              suffixIcon: unit == null
                  ? null
                  : Padding(
                      padding: const EdgeInsets.only(right: AhSpace.md),
                      child: Text(
                        unit!.toUpperCase(),
                        style: AhText.label.copyWith(color: surfaces.textSecondary),
                      ),
                    ),
              suffixIconConstraints: const BoxConstraints(minWidth: 0, minHeight: 0),
            ),
          ),
        ),
        if (invalid) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          _Message(text: error!, tone: AhColors.flag, alert: true),
        ] else if (hint != null) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          _Message(text: hint!, tone: surfaces.textSecondary, alert: false),
        ],
      ],
    );
  }

  static double _height(AhFieldSize size, AhDensity density) => switch (size) {
        AhFieldSize.sm => density.controlHeight * 0.8,
        AhFieldSize.md => density.controlHeight,
        AhFieldSize.lg => density.controlHeight * 1.15,
      };
}

/// The message under a control.
///
/// An error gets a left rule in the flag colour rather than an icon: the rule survives being read
/// in sunlight on an inspection screen, where a 12dp glyph does not, and it costs no width on a
/// 360dp handset.
class _Message extends StatelessWidget {
  const _Message({required this.text, required this.tone, required this.alert});

  final String text;
  final Color tone;
  final bool alert;

  @override
  Widget build(BuildContext context) {
    final style = AhText.bodySm.copyWith(color: tone);

    if (!alert) return Text(text, style: style);

    return Semantics(
      liveRegion: true,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: AhColors.flagSoft,
          border: const Border(left: BorderSide(color: AhColors.flag, width: 2)),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AhSpace.sm, vertical: AhSpace.xs),
          child: Text(text, style: style),
        ),
      ),
    );
  }
}
