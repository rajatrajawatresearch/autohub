import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import 'shared.dart';
import 'text_control.dart';

/// Whole and decimal quantities — kms driven, seats, payload, tread depth, years in business.
///
/// The value the form holds is always a real number or absent; the text on screen is allowed to be
/// halfway to one. `1`, `12`, `12.` all have to survive keystroke by keystroke, and `12.` is not a
/// number yet — which is why the re-sync test compares parsed meanings rather than strings.
///
/// The grouped readout underneath is not decoration. `38400` is unreadable at a glance and `38,400`
/// is the number the user meant to type; a seller who typed one zero too many finds out here rather
/// than after a valuation comes back wrong by a factor of ten.
class NumberControl extends StatelessWidget {
  const NumberControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final num? numeric = asNumber(props.value);
    final num? step = props.field.configStep;
    final bool decimal = step != null && step < 1;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        AhManagedTextField(
          text: numeric == null ? '' : '$numeric',
          mono: true,
          enabled: !props.disabled,
          readOnly: props.field.readOnly,
          error: props.error?.message,
          placeholder: props.field.placeholder,
          unit: props.field.unit,
          keyboardType: TextInputType.numberWithOptions(decimal: decimal),
          onBlur: props.onBlur,
          sameValue: (String incoming, String current) =>
              num.tryParse(incoming) != null && num.tryParse(incoming) == num.tryParse(current),
          onTextChanged: (String next) {
            final String cleaned = next.replaceAll(RegExp('[^0-9.-]'), '');
            if (cleaned.trim().isEmpty) {
              props.onChanged(null);
              return;
            }
            final num? parsed = num.tryParse(cleaned);
            // A half-typed `12.` leaves the value at its last parseable state rather than clearing
            // it. Clearing would fire `required` at somebody mid-keystroke.
            if (parsed != null) props.onChanged(parsed);
          },
        ),
        if (numeric != null && numeric.abs() >= 10000) ...<Widget>[
          const SizedBox(height: AhSpace.xs),
          AhValueReadout(
            '${formatIndianNumber(numeric)}${props.field.unit == null ? '' : ' ${props.field.unit}'}',
          ),
        ],
      ],
    );
  }
}

/// A mono, secondary-tone line under an input: the same number said back the way a person would
/// actually say it. Never focusable, never the value — a confirmation, not a second field, and
/// excluded from semantics so a screen reader does not read every figure twice.
class AhValueReadout extends StatelessWidget {
  const AhValueReadout(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) => ExcludeSemantics(
        child: Text(
          text,
          style: AhText.data.copyWith(fontSize: 12, color: context.ahSurfaces.textSecondary),
        ),
      );
}
