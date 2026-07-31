import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'option_picker.dart';
import 'shared.dart';

/// The cascade: make → model → year → variant.
///
/// The levels are registry data, not a fixed four. A truck cascades make → model → axle config →
/// permit type; construction equipment will cascade something nobody has thought of yet. Each level
/// asks the injected loader for its options, scoped by the levels it depends on, so
/// `GET /platform/taxonomy/models?make=maruti-suzuki` is a query this file never has to know about.
///
/// Choosing a level clears everything downstream. A Swift is not a valid model of a Mahindra, and
/// silently keeping the stale value is how a listing ends up describing a car that does not exist —
/// which is then inspected, photographed and rejected.
class VehiclePickerControl extends StatelessWidget {
  const VehiclePickerControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final List<PickerLevel> levels = props.field.levels;
    if (levels.isEmpty) return AhQuietText(t(FormLabelKeys.selectEmpty));

    final Map<String, Object?> selection = asRecord(props.value);

    return AhFieldBox(
      invalid: props.invalid,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          for (int index = 0; index < levels.length; index += 1) ...<Widget>[
            _LevelField(
              key: ValueKey<String>('${props.field.key}.${levels[index].key}'),
              level: levels[index],
              levels: levels,
              props: props,
              selection: selection,
              onChoose: (String value) => _choose(levels, selection, index, value),
            ),
            if (index < levels.length - 1) const SizedBox(height: AhSpace.md),
          ],
        ],
      ),
    );
  }

  void _choose(
    List<PickerLevel> levels,
    Map<String, Object?> selection,
    int index,
    String value,
  ) {
    final Map<String, Object?> merged = Map<String, Object?>.of(selection);
    merged[levels[index].key] = value;
    for (final PickerLevel downstream in levels.sublist(index + 1)) {
      merged.remove(downstream.key);
    }
    props.onChanged(merged.isEmpty ? null : merged);
  }
}

class _LevelField extends StatelessWidget {
  const _LevelField({
    required this.level,
    required this.levels,
    required this.props,
    required this.selection,
    required this.onChoose,
    super.key,
  });

  final PickerLevel level;
  final List<PickerLevel> levels;
  final FieldProps props;
  final Map<String, Object?> selection;
  final ValueChanged<String> onChoose;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final String current = asString(selection[level.key]);

    String? missing;
    for (final String key in level.dependsOn) {
      if (asString(selection[key]).isEmpty) {
        missing = key;
        break;
      }
    }

    final Map<String, Object?> scope = <String, Object?>{
      for (final String key in level.dependsOn) key: selection[key],
    };

    String? blockedBy;
    if (missing != null) {
      for (final PickerLevel candidate in levels) {
        if (candidate.key == missing) blockedBy = candidate.label;
      }
      blockedBy ??= missing;
    }

    return FieldOptionsBuilder(
      field: props.field,
      inline: level.options,
      level: level.key,
      source: level.optionsSource,
      params: scope,
      enabled: missing == null,
      builder: (BuildContext context, FieldOptionsState state, VoidCallback reload) {
        String? label;
        for (final Option option in state.options) {
          if (option.value == current) label = option.label;
        }
        if (label == null && current.isNotEmpty) label = current;

        final String placeholder = blockedBy != null
            ? t(FormLabelKeys.selectLocked, <String, Object?>{'field': blockedBy})
            : state.loading
                ? t(FormLabelKeys.selectLoading)
                : state.failed
                    ? t(FormLabelKeys.selectError)
                    : state.options.isEmpty
                        ? t(FormLabelKeys.selectEmpty)
                        : (level.placeholder ?? t(FormLabelKeys.selectPlaceholder));

        final bool ready = missing == null &&
            !state.loading &&
            !state.failed &&
            state.options.isNotEmpty &&
            !props.disabled &&
            !props.readOnly;

        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            AhLabelText(level.label),
            const SizedBox(height: AhSpace.xs),
            if (state.failed && missing == null)
              AhOptionsFailure(
                message: t(FormLabelKeys.selectError),
                retryLabel: t(FormLabelKeys.fileRetry),
                onRetry: reload,
              )
            else
              AhPickerField(
                label: label,
                placeholder: placeholder,
                invalid: props.invalid && level.isRequired,
                onTap: ready
                    ? () async {
                        final Option? chosen = await showOptionPicker(
                          context,
                          title: level.label,
                          options: state.options,
                          selectedValue: current.isEmpty ? null : current,
                        );
                        if (!context.mounted) return;
                        props.onBlur();
                        if (chosen != null) onChoose(chosen.value);
                      }
                    : null,
              ),
          ],
        );
      },
    );
  }
}
