import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'option_picker.dart';
import 'shared.dart';

/// One choice from a registry list.
///
/// The list is never in this file. It is inline on the field definition, or it is fetched by the
/// app through the injected loader against `GET /platform/taxonomy/attributes?category=…` — which
/// is exactly what makes a new fuel type a database row rather than a release, on Android and iOS
/// at the same moment as on the web.
///
/// If you ever find yourself wanting to add a `const _fuelTypes = [...]` here, that is the bug this
/// whole architecture exists to prevent.
class SelectControl extends StatelessWidget {
  const SelectControl(this.props, {super.key});

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

        final bool empty = !state.loading && state.options.isEmpty;
        final String placeholder = state.loading
            ? t(FormLabelKeys.selectLoading)
            : empty
                ? t(FormLabelKeys.selectEmpty)
                : (props.field.placeholder ?? t(FormLabelKeys.selectPlaceholder));

        String? chosenLabel;
        for (final Option option in state.options) {
          if (option.value == current) {
            chosenLabel = option.label;
            break;
          }
        }
        // A value with no matching option is shown as its raw code rather than blanked. It happens
        // when a draft holds a taxonomy value that has since been retired, and blanking it would
        // silently discard the seller's answer.
        if (chosenLabel == null && current.isNotEmpty) chosenLabel = current;

        final bool enabled =
            !props.locked && !state.loading && !empty;

        return AhPickerField(
          label: chosenLabel,
          placeholder: placeholder,
          invalid: props.invalid,
          onTap: enabled ? () => _choose(context, state.options, current) : null,
        );
      },
    );
  }

  Future<void> _choose(
    BuildContext context,
    List<Option> options,
    String current,
  ) async {
    final Option? chosen = await showOptionPicker(
      context,
      title: props.field.label,
      options: options,
      selectedValue: current.isEmpty ? null : current,
    );
    // The sheet is a route; the field may have been removed by a visibility change while it was up.
    if (!context.mounted) return;
    props.onBlur();
    if (chosen == null) return;
    props.onChanged(chosen.value);
  }
}
