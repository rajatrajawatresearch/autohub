import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'shared.dart';

/// The registration number — the primary key, made visible.
///
/// This is `PlateInput` from the canon, wired to a registry field. The control owns its caption and
/// its fault sentence because the plate block is not a field wearing a label: it is a stamped
/// rectangle with the label above it as part of its anatomy, and the shell stacking a second
/// uppercase caption on top would read as two different questions.
///
/// The plate earns more on a phone than it does on the web. The user is usually standing next to
/// the car, and the check that matters is whether the thing on the screen matches the thing on the
/// bumper — grouped, because ungrouped a ten-character plate is measurably easier to transpose two
/// digits in, and a transposed plate fetches somebody else's RTO record.
class PlateControl extends StatelessWidget {
  const PlateControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        PlateInput(
          value: asString(props.value),
          onChanged: (String canonical) => props.onChanged(canonical.isEmpty ? null : canonical),
          onSubmitted: (String _) => props.onBlur(),
          label: props.field.label,
          hint: props.field.helpText,
          error: props.error?.message,
          // A format example, not a business fact — and the same one `PlateInput` defaults to, so
          // a registry row that leaves `placeholder` unset still shows a seller what shape of
          // answer this field wants.
          placeholder: props.field.placeholder ?? 'MH 12 AB 1234',
          enabled: !props.locked,
        ),
        if (!props.field.isRequired) ...<Widget>[
          const SizedBox(height: AhSpace.xs),
          AhLabelText(t(FormLabelKeys.fieldOptional)),
        ],
      ],
    );
  }
}
