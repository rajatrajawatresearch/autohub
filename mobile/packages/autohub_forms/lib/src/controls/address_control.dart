import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'option_picker.dart';
import 'shared.dart';
import 'text_control.dart';

/// A composite value — the parts are whatever the registry says they are.
///
/// A dealer's premises, a pickup location and a loan applicant's permanent address are not the same
/// shape, and a market that needs a landmark line gets one by editing `config.parts`, not by
/// editing this file. The fallback below is only what an Indian postal address looks like when the
/// registry has not spoken, and even its captions come from the localisation bundle.
class AddressControl extends StatelessWidget {
  const AddressControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final Map<String, Object?> record = asRecord(props.value);
    final List<AddressPart> parts =
        props.field.parts.isEmpty ? _defaultParts(t) : props.field.parts;

    return AhFieldBox(
      invalid: props.invalid,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          for (final AddressPart part in parts) ...<Widget>[
            _AddressPartField(
              // Keyed so a registry that changes its parts list rebuilds rather than reusing the
              // wrong text controller under a renamed caption.
              key: ValueKey<String>('${props.field.key}.${part.key}'),
              part: part,
              props: props,
              record: record,
              onChanged: (String next) => _setPart(record, part.key, next),
            ),
            const SizedBox(height: AhSpace.md),
          ],
        ],
      ),
    );
  }

  void _setPart(Map<String, Object?> record, String key, String next) {
    final Map<String, Object?> merged = Map<String, Object?>.of(record);
    if (next.isEmpty) {
      merged.remove(key);
    } else {
      merged[key] = next;
    }
    props.onChanged(merged.isEmpty ? null : merged);
  }
}

class _AddressPartField extends StatelessWidget {
  const _AddressPartField({
    required this.part,
    required this.props,
    required this.record,
    required this.onChanged,
    super.key,
  });

  final AddressPart part;
  final FieldProps props;
  final Map<String, Object?> record;
  final ValueChanged<String> onChanged;

  bool get _isSelect =>
      part.control == 'select' || part.options != null || part.optionsSource != null;

  @override
  Widget build(BuildContext context) {
    final String value = asString(record[part.key]);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        AhLabelText(part.label),
        const SizedBox(height: AhSpace.xs),
        if (_isSelect) _PartPicker(part: part, props: props, record: record, value: value, onChanged: onChanged)
        else
          AhManagedTextField(
            text: value,
            mono: part.mono,
            enabled: !props.locked,
            // No per-part error string. The registry validates the address as one value, so the
            // fault belongs to the block and the shell draws it once underneath — a red border on
            // four of six lines would claim a precision the schema does not have.
            placeholder: part.placeholder,
            keyboardType: part.mono ? TextInputType.number : TextInputType.streetAddress,
            onBlur: props.onBlur,
            onTextChanged: onChanged,
          ),
      ],
    );
  }
}

class _PartPicker extends StatelessWidget {
  const _PartPicker({
    required this.part,
    required this.props,
    required this.record,
    required this.value,
    required this.onChanged,
  });

  final AddressPart part;
  final FieldProps props;
  final Map<String, Object?> record;
  final String value;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;

    // Only the parts this one is scoped by. Handing the whole address to the loader would refetch
    // the city list on every keystroke in the street line.
    final Map<String, Object?> scope = <String, Object?>{
      for (final String key in part.dependsOn) key: record[key],
    };
    final bool blocked = part.dependsOn.any((String key) => asString(record[key]).isEmpty);

    return FieldOptionsBuilder(
      field: props.field,
      inline: part.options,
      level: part.key,
      source: part.optionsSource,
      params: scope,
      enabled: !blocked,
      builder: (BuildContext context, FieldOptionsState state, VoidCallback reload) {
        if (state.failed) {
          return AhOptionsFailure(
            message: t(FormLabelKeys.selectError),
            retryLabel: t(FormLabelKeys.fileRetry),
            onRetry: reload,
          );
        }

        String? label;
        for (final Option option in state.options) {
          if (option.value == value) label = option.label;
        }
        if (label == null && value.isNotEmpty) label = value;

        final bool ready =
            !blocked && !state.loading && state.options.isNotEmpty && !props.disabled && !props.readOnly;

        return AhPickerField(
          label: label,
          invalid: props.invalid && part.isRequired,
          placeholder: blocked
              ? t(FormLabelKeys.selectLocked, <String, Object?>{'field': part.dependsOn.first})
              : state.loading
                  ? t(FormLabelKeys.selectLoading)
                  : state.options.isEmpty
                      ? t(FormLabelKeys.selectEmpty)
                      : (part.placeholder ?? t(FormLabelKeys.selectPlaceholder)),
          onTap: ready
              ? () async {
                  final Option? chosen = await showOptionPicker(
                    context,
                    title: part.label,
                    options: state.options,
                    selectedValue: value.isEmpty ? null : value,
                  );
                  if (!context.mounted) return;
                  props.onBlur();
                  if (chosen != null) onChanged(chosen.value);
                }
              : null,
        );
      },
    );
  }
}

/// Only used when the registry sends no `parts`. Every caption still comes from the localisation
/// bundle, so a Marathi build does not fall back into English addresses.
List<AddressPart> _defaultParts(FormLabels t) => <AddressPart>[
      AddressPart(key: 'line1', label: t(FormLabelKeys.addressLine1), isRequired: true),
      AddressPart(key: 'line2', label: t(FormLabelKeys.addressLine2)),
      AddressPart(key: 'city', label: t(FormLabelKeys.addressCity), isRequired: true),
      AddressPart(key: 'state', label: t(FormLabelKeys.addressState), isRequired: true),
      AddressPart(
        key: 'pincode',
        label: t(FormLabelKeys.addressPincode),
        isRequired: true,
        mono: true,
      ),
      AddressPart(key: 'landmark', label: t(FormLabelKeys.addressLandmark)),
    ];
