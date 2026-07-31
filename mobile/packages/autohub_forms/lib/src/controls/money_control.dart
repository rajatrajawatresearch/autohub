import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'number_control.dart';
import 'shared.dart';
import 'text_control.dart';

/// Rupees in, paise out.
///
/// The user types a rupee figure; the form holds an **integer** number of paise, which is the only
/// representation the API, the ledger and the commission engine accept. The conversion is string
/// arithmetic and not `(rupees * 100).round()`, because `8.4 * 100` is `840.0000000000001` in
/// binary floating point, and a listing price one paise out is a settlement that does not
/// reconcile — three weeks later, in a different system, for somebody who cannot see this screen.
///
/// The live `₹8.4L` readout underneath is how India *says* the number. It is the fastest way for a
/// seller to notice they typed one zero too many, and it costs one line.
class MoneyControl extends StatelessWidget {
  const MoneyControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final int? minor = asMinorUnits(props.value);
    final String currency = props.field.currency ?? 'INR';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            // A fixed block, not a prefix inside the text. A symbol the user can select, delete or
            // type twice is a symbol that ends up inside the parsed value.
            FixedAffix(text: t(FormLabelKeys.moneySymbol)),
            Expanded(
              child: AhManagedTextField(
                text: minorToRupees(minor),
                mono: true,
                enabled: !props.disabled,
                readOnly: props.field.readOnly,
                error: props.error?.message,
                placeholder: props.field.placeholder,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                onBlur: props.onBlur,
                sameValue: (String incoming, String current) =>
                    rupeesToMinor(incoming) != null &&
                    rupeesToMinor(incoming) == rupeesToMinor(current),
                onTextChanged: (String next) {
                  final String cleaned = next.replaceAll(RegExp('[^0-9.]'), '');
                  props.onChanged(cleaned.trim().isEmpty ? null : rupeesToMinor(cleaned));
                },
              ),
            ),
          ],
        ),
        if (minor != null) ...<Widget>[
          const SizedBox(height: AhSpace.xs),
          AhValueReadout(formatIndianMoney(minor, compact: true, currency: currency)),
        ],
      ],
    );
  }
}

/// An immovable block welded to the left of a field — the rupee sign here, the dial code on
/// `phone`. Shared because both exist for the same reason: an affix the user can select, delete or
/// paste over is an affix that ends up inside the value.
class FixedAffix extends StatelessWidget {
  const FixedAffix({required this.text, super.key});

  final String text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    return Container(
      alignment: Alignment.center,
      height: context.ahTapTarget(context.ahDensity.controlHeight),
      padding: const EdgeInsets.symmetric(horizontal: AhSpace.md),
      decoration: BoxDecoration(
        color: surfaces.sunken,
        border: Border.all(color: surfaces.borderStrong),
        borderRadius: const BorderRadius.horizontal(left: AhRadius.control),
      ),
      child: Text(text, style: AhText.data.copyWith(color: surfaces.textSecondary)),
    );
  }
}
