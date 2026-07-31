import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import 'field_chrome.dart';
import 'shared.dart';

/// A bounded quantity — battery health, condition score, tread depth.
///
/// Bounds and step come from `config`, which comes from the taxonomy attribute's `range`. Nothing
/// here decides that a battery runs from 40% to 100%; the registry does, and the EV category can
/// change its mind without a release.
///
/// The reading sits above the track in mono with its unit, because a slider without a number is a
/// mood and this product does not deal in moods — a valuation is computed from this figure.
class SliderControl extends StatelessWidget {
  const SliderControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final double min = (props.field.configMin ?? 0).toDouble();
    final double max = (props.field.configMax ?? 100).toDouble();
    final double step = (props.field.configStep ?? 1).toDouble();

    // A registry row with max <= min would make Flutter's Slider assert. Widening rather than
    // asserting keeps a bad row costing one confusing field instead of the whole step.
    final double upper = max > min ? max : min + 1;
    final num? stored = asNumber(props.value);
    final double current = (stored?.toDouble() ?? min).clamp(min, upper);

    final int divisions = step > 0 ? ((upper - min) / step).round().clamp(1, 1000) : 1;
    final String unit = props.field.unit == null ? '' : ' ${props.field.unit}';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: <Widget>[
            Expanded(
              child: Text(
                '${formatIndianNumber(current)}$unit',
                style: AhText.data.copyWith(fontSize: 18, color: surfaces.textPrimary),
              ),
            ),
            AhLabelText(
              '${formatIndianNumber(min)}–${formatIndianNumber(upper)}$unit',
            ),
          ],
        ),
        Slider(
          value: current,
          min: min,
          max: upper,
          divisions: divisions,
          activeColor: surfaces.accentPrimary,
          inactiveColor: surfaces.borderHairline,
          label: '${formatIndianNumber(current)}$unit',
          onChanged: props.locked
              ? null
              : (double next) => props.onChanged(step >= 1 ? next.round() : next),
          // The value settles on every drag frame; the *fault* is only allowed to appear once the
          // user has let go. Otherwise a required slider shouts at somebody mid-drag.
          onChangeEnd: (double _) => props.onBlur(),
        ),
        if (props.field.marks.isNotEmpty)
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              for (final SliderMark mark in props.field.marks) AhLabelText(mark.label),
            ],
          ),
      ],
    );
  }
}
