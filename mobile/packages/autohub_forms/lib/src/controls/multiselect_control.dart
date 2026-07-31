import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'field_chrome.dart';
import 'shared.dart';

/// Several choices from a registry list, as chips.
///
/// Chips rather than a multi-select menu because that is how the search facets read
/// (`facetStyle: "chips"` in the taxonomy), and a dealer filtering inventory and a seller
/// describing a car should not be operating two different grammars for the same act.
class MultiselectControl extends StatelessWidget {
  const MultiselectControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
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

        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Wrap(
              spacing: AhSpace.sm,
              runSpacing: AhSpace.sm,
              children: <Widget>[
                for (final Option option in state.options)
                  _Chip(
                    option: option,
                    selected: selected.contains(option.value),
                    enabled: !props.disabled && !props.readOnly,
                    onTap: () => _toggle(selected, option.value),
                  ),
              ],
            ),
            if (selected.isNotEmpty) ...<Widget>[
              const SizedBox(height: AhSpace.sm),
              AhLabelText(
                t(FormLabelKeys.multiselectCount, <String, Object?>{'count': selected.length}),
              ),
            ],
          ],
        );
      },
    );
  }

  void _toggle(List<String> selected, String value) {
    final List<String> next = List<String>.of(selected);
    if (!next.remove(value)) next.add(value);
    props.onChanged(next.isEmpty ? null : next);
    props.onBlur();
  }
}

class _Chip extends StatelessWidget {
  const _Chip({
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
    return Opacity(
      opacity: enabled ? 1 : 0.45,
      child: Material(
        color: selected ? surfaces.accentPrimary : surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.control),
        child: InkWell(
          onTap: enabled ? onTap : null,
          borderRadius: const BorderRadius.all(AhRadius.control),
          child: Container(
            // The 44dp floor holds on chips too. A facet a dealer cannot reliably hit while
            // standing is a facet they stop using.
            constraints: const BoxConstraints(minHeight: 44),
            alignment: Alignment.center,
            padding: const EdgeInsets.symmetric(horizontal: AhSpace.md),
            decoration: BoxDecoration(
              borderRadius: const BorderRadius.all(AhRadius.control),
              border: Border.all(
                color: selected ? Colors.transparent : surfaces.borderStrong,
              ),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                Text(
                  option.label,
                  style: AhText.bodySm.copyWith(
                    color: selected ? surfaces.textOnPrimary : surfaces.textPrimary,
                    fontWeight: selected ? FontWeight.w600 : FontWeight.w400,
                  ),
                ),
                if (option.count != null) ...<Widget>[
                  const SizedBox(width: AhSpace.sm),
                  Text(
                    '${option.count}',
                    style: AhText.label.copyWith(
                      color: selected ? surfaces.textOnPrimary : surfaces.textSecondary,
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
