import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../form_scope.dart';
import 'shared.dart';

/// A setting, not an answer.
///
/// The label sits on the left and the rocker on the right across a hairline row — the shape of a
/// settings list, which is what a toggle always belongs to. It owns its label for that reason: a
/// caption stacked above a switch reads as a question, and a question deserves a radio pair where
/// "no" is something the user chose rather than something they left alone.
class ToggleControl extends StatelessWidget {
  const ToggleControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final bool value = asBoolean(props.value);
    final bool enabled = !props.locked;

    return Container(
      constraints: const BoxConstraints(minHeight: 48),
      decoration: BoxDecoration(
        border: Border.symmetric(
          horizontal: BorderSide(color: surfaces.borderHairline),
        ),
      ),
      padding: const EdgeInsets.symmetric(vertical: AhSpace.sm),
      child: Row(
        children: <Widget>[
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                Text(props.field.label, style: AhText.body.copyWith(color: surfaces.textPrimary)),
                if (props.field.helpText != null)
                  Text(
                    props.field.helpText!,
                    style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                  ),
              ],
            ),
          ),
          const SizedBox(width: AhSpace.lg),
          Switch(
            value: value,
            activeThumbColor: surfaces.accentPrimary,
            onChanged: enabled
                ? (bool next) {
                    // Stored as a real boolean and not as `next ? true : null`: a toggle the user
                    // deliberately turned off is an answer, and a conditional field keyed on
                    // `{"==":[{"var":"accident_history"}, false]}` has to be able to see it.
                    props.onChanged(next);
                    props.onBlur();
                  }
                : null,
          ),
        ],
      ),
    );
  }
}
