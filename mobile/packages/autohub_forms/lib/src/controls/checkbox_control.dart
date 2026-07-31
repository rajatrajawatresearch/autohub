import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart' hide FormField;

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'shared.dart';

/// A tick.
///
/// With options on the field definition it is a set of ticks and the value is a list of codes —
/// accessories fitted, documents on hand, defects found on an inspection line. Without options it
/// is a single yes/no and the value is a boolean; `required` then means "must be ticked", which is
/// what a consent or a declaration needs and why [isEmptyValue] treats `false` as unanswered.
class CheckboxControl extends StatelessWidget {
  const CheckboxControl(this.props, {super.key});

  final FieldProps props;

  /// A lone tick carries its own label beside the box; a group of ticks needs a caption above it.
  static bool ownsLabel(FormField field) =>
      (field.options?.isEmpty ?? true) &&
      field.optionsFromAttribute == null &&
      field.optionsSource == null;

  @override
  Widget build(BuildContext context) {
    if (ownsLabel(props.field)) return _SingleTick(props: props);

    final FormLabels t = SchemaFormScope.of(context).labels;
    final List<String> selected = asStringList(props.value);

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
                _TickRow(
                  label: option.label,
                  hint: option.count?.toString(),
                  checked: selected.contains(option.value),
                  enabled: !props.disabled && !props.readOnly,
                  onTap: () {
                    final List<String> next = List<String>.of(selected);
                    if (!next.remove(option.value)) next.add(option.value);
                    props.onChanged(next.isEmpty ? null : next);
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

class _SingleTick extends StatelessWidget {
  const _SingleTick({required this.props});

  final FieldProps props;

  @override
  Widget build(BuildContext context) => _TickRow(
        label: props.field.label,
        hint: props.field.helpText,
        checked: asBoolean(props.value),
        enabled: !props.disabled && !props.readOnly,
        invalid: props.invalid,
        onTap: () {
          props.onChanged(!asBoolean(props.value));
          props.onBlur();
        },
      );
}

class _TickRow extends StatelessWidget {
  const _TickRow({
    required this.label,
    required this.checked,
    required this.enabled,
    required this.onTap,
    this.hint,
    this.invalid = false,
  });

  final String label;
  final String? hint;
  final bool checked;
  final bool enabled;
  final bool invalid;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    return Semantics(
      checked: checked,
      child: InkWell(
        onTap: enabled ? onTap : null,
        child: Opacity(
          opacity: enabled ? 1 : 0.45,
          child: Container(
            constraints: const BoxConstraints(minHeight: 48),
            padding: const EdgeInsets.symmetric(horizontal: AhSpace.md, vertical: AhSpace.sm),
            child: Row(
              children: <Widget>[
                Icon(
                  checked ? Icons.check_box_outlined : Icons.check_box_outline_blank,
                  size: 20,
                  color: checked
                      ? surfaces.accentPrimary
                      : (invalid ? AhColors.flag : surfaces.borderStrong),
                ),
                const SizedBox(width: AhSpace.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: <Widget>[
                      Text(label, style: AhText.body.copyWith(color: surfaces.textPrimary)),
                      if (hint != null)
                        Text(
                          hint!,
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
