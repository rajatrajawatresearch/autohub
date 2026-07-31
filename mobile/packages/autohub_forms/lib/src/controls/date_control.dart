import 'package:flutter/material.dart';

import '../field_config.dart';
import '../form_scope.dart';
import '../types.dart';
import 'option_picker.dart';
import 'shared.dart';

/// A date, stored as a plain ISO `YYYY-MM-DD` string.
///
/// Never a `DateTime` in the value. An RC expiry is a calendar date, not an instant: the moment it
/// becomes a timestamp it acquires a timezone, and an inspector in Pune entering `2026-03-31` will
/// see the server record `2026-03-30` for reasons nobody on the call can explain.
///
/// The OS picker, not a custom calendar. An inspector standing outdoors wants the wheel they
/// already know how to use, and a bespoke popover is a worse tool in a worse place.
class DateControl extends StatelessWidget {
  const DateControl(this.props, {super.key});

  final FieldProps props;

  @override
  Widget build(BuildContext context) {
    final FormLabels t = SchemaFormScope.of(context).labels;
    final String iso = asString(props.value);

    return AhPickerField(
      label: iso.isEmpty ? null : iso,
      mono: true,
      trailing: Icons.calendar_today_outlined,
      invalid: props.invalid,
      placeholder: props.field.placeholder ?? t(FormLabelKeys.selectPlaceholder),
      onTap: props.locked ? null : () => _pick(context),
    );
  }

  Future<void> _pick(BuildContext context) async {
    final DateTime today = DateTime.now();
    final DateTime first = _parseIso(props.field.minDate) ?? DateTime(today.year - 60);
    final DateTime last = _parseIso(props.field.maxDate) ?? DateTime(today.year + 30);
    final DateTime initial = _parseIso(asString(props.value)) ?? today;

    final DateTime? chosen = await showDatePicker(
      context: context,
      initialDate: _clamp(initial, first, last),
      firstDate: first,
      lastDate: last,
    );

    // The picker is a route. By the time it pops this element may be gone — a step can disappear
    // underneath a user while a modal is open, because visibility is data.
    if (!context.mounted) return;
    props.onBlur();
    if (chosen == null) return;
    props.onChanged(_toIso(chosen));
  }
}

DateTime _clamp(DateTime value, DateTime first, DateTime last) {
  if (value.isBefore(first)) return first;
  if (value.isAfter(last)) return last;
  return value;
}

DateTime? _parseIso(String? value) {
  if (value == null || value.length < 10) return null;
  return DateTime.tryParse(value.substring(0, 10));
}

String _toIso(DateTime value) =>
    '${value.year.toString().padLeft(4, '0')}-'
    '${value.month.toString().padLeft(2, '0')}-'
    '${value.day.toString().padLeft(2, '0')}';
