import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'shared.dart';

/// One choice, every option visible.
///
/// Used where the list is short and the answer matters enough to be read rather than opened —
/// fuel type, owner count, condition grade. Round, deliberately: round versus square is the only
/// grammar a user has for "pick one" versus "pick several", and spending it on decoration means
/// nothing distinguishes them at a glance.
class RadioControl extends StatelessWidget {
  const RadioControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final String current = asString(props.value);

    return FieldOptionsBuilder(
      field: props.field,
      inline: props.field.options,
      attribute: props.field.optionsFromAttribute,
      source: props.field.optionsSource,
      params: props.field.optionsParams,
      builder: (BuildContext context, FieldOptionsState state, VoidCallback reload) {
        if (state.failed) {
          return AhOptionsFailure(
            message: t(FormLabelKeys.selectError),
            retryLabel: t(FormLabelKeys.fileRetry),
            onRetry: reload,
          );
        }
        if (state.loading) return AhQuietText(t(FormLabelKeys.selectLoading));
        if (state.options.isEmpty) return AhQuietText(t(FormLabelKeys.selectEmpty));

        return AhFieldBox(
          invalid: props.invalid,
          padded: false,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              for (final Option option in state.options)
                _OptionRow(
                  option: option,
                  selected: option.value == current,
                  enabled: !props.disabled && !props.readOnly,
                  onTap: () {
                    props.onChanged(option.value);
                    props.onBlur();
                  },
                ),
            ],
          ),
        );
      },
    );
  }
}

class _OptionRow extends StatelessWidget {
  const _OptionRow({
    required this.option,
    required this.selected,
    required this.enabled,
    required this.onTap,
  });

  final Option option;
  final bool selected;
  final bool enabled;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    return Semantics(
      inMutuallyExclusiveGroup: true,
      checked: selected,
      child: InkWell(
        onTap: enabled ? onTap : null,
        child: Opacity(
          opacity: enabled ? 1 : 0.45,
          child: Container(
            // The whole row is the target, not the 20dp circle. A seller answering this outdoors
            // one-handed should not have to aim.
            constraints: const BoxConstraints(minHeight: 48),
            padding: const EdgeInsets.symmetric(horizontal: AhSpace.md, vertical: AhSpace.sm),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: <Widget>[
                Icon(
                  selected ? Icons.radio_button_checked : Icons.radio_button_unchecked,
                  size: 20,
                  color: selected ? surfaces.accentPrimary : surfaces.borderStrong,
                ),
                const SizedBox(width: AhSpace.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: <Widget>[
                      // Prose the user reads, not a caption above a value — so body, not the
                      // uppercase label atom.
                      Text(option.label, style: AhText.body.copyWith(color: surfaces.textPrimary)),
                      if (option.count != null)
                        Text(
                          '${option.count}',
                          style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                        ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
