/// Reading `config` off a registry field.
///
/// `FormField.config` is an open `Map<String, Object?>` on the wire, and it stays open on purpose:
/// it is the extension point that lets a registry give a control a new setting without a client
/// release. `autohub_core` publishes typed readers for the five configs a *consumer* cares about
/// (`PhotoSetConfig`, `SliderConfig`, `OtpConfig`, `FileConfig`, `MoneyConfig`); this file covers
/// the rest, which are the settings only a renderer has any use for — how many rows a text box has,
/// which parts an address is made of, which levels a vehicle cascade walks.
///
/// Every reader coerces rather than casts. The registry is authored by humans through an admin
/// console, and a `6` that arrived as `"6"` is a data-entry variation, not a reason to blank a
/// field on somebody's phone.
///
/// `min` and `max` are deliberately untyped as `num`. They mean whatever the *value's* shape says
/// they mean — a bound on a battery percentage, a count on a list of photographs — and that is
/// decided at validation time from the value itself, never from a hardcoded list of which controls
/// hold lists.
library;

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

String? _str(Object? value) {
  if (value is String) return value;
  if (value is num || value is bool) return '$value';
  return null;
}

bool _flag(Object? value) {
  if (value is bool) return value;
  if (value is String) return value == 'true';
  return false;
}

num? _num(Object? value) {
  if (value is num) return value.isFinite ? value : null;
  if (value is String) return num.tryParse(value.trim());
  return null;
}

int? _int(Object? value) => _num(value)?.round();

Map<String, Object?> _map(Object? value) {
  if (value is Map) {
    return <String, Object?>{
      for (final MapEntry<Object?, Object?> entry in value.entries)
        if (entry.key is String) entry.key! as String: entry.value,
    };
  }
  return const <String, Object?>{};
}

List<Object?> _list(Object? value) => value is List ? value : const <Object?>[];

List<String> _strings(Object? value) =>
    <String>[for (final Object? entry in _list(value)) ?_str(entry)];

/// A cascading level for `vehicle_picker` — make → model → year → variant, defined by the registry.
///
/// Not a fixed four. A truck cascades make → model → axle config → permit type, and construction
/// equipment will cascade something nobody has thought of yet.
@immutable
class PickerLevel {
  const PickerLevel({
    required this.key,
    required this.label,
    this.placeholder,
    this.dependsOn = const <String>[],
    this.optionsSource,
    this.options,
    this.isRequired = false,
  });

  factory PickerLevel.fromJson(Map<String, Object?> json) => PickerLevel(
        key: _str(json['key']) ?? '',
        label: _str(json['label']) ?? '',
        placeholder: _str(json['placeholder']),
        dependsOn: _strings(json['dependsOn']),
        optionsSource: _str(json['optionsSource']),
        options: json['options'] == null
            ? null
            : <Option>[for (final Object? entry in _list(json['options'])) Option.fromJson(_map(entry))],
        isRequired: _flag(json['required']),
      );

  final String key;
  final String label;
  final String? placeholder;

  /// Level keys whose values must be chosen before this one can load. A model list without a make
  /// is a meaningless query.
  final List<String> dependsOn;
  final String? optionsSource;
  final List<Option>? options;
  final bool isRequired;
}

/// A sub-input inside `address`. The registry decides whether a market needs a landmark line.
@immutable
class AddressPart {
  const AddressPart({
    required this.key,
    required this.label,
    this.placeholder,
    this.control,
    this.optionsSource,
    this.options,
    this.isRequired = false,
    this.dependsOn = const <String>[],
    this.mono = false,
  });

  factory AddressPart.fromJson(Map<String, Object?> json) => AddressPart(
        key: _str(json['key']) ?? '',
        label: _str(json['label']) ?? '',
        placeholder: _str(json['placeholder']),
        control: _str(json['control']),
        optionsSource: _str(json['optionsSource']),
        options: json['options'] == null
            ? null
            : <Option>[for (final Object? entry in _list(json['options'])) Option.fromJson(_map(entry))],
        isRequired: _flag(json['required']),
        dependsOn: _strings(json['dependsOn']),
        mono: _flag(json['mono']),
      );

  final String key;
  final String label;
  final String? placeholder;
  final String? control;
  final String? optionsSource;
  final List<Option>? options;
  final bool isRequired;

  /// Other part keys this one's option list is scoped by — a city list narrowed by state.
  final List<String> dependsOn;

  /// Render as mono. Pincodes and plot numbers are transcribed off documents, not read as prose.
  final bool mono;
}

/// A labelled tick on a `slider` track.
@immutable
class SliderMark {
  const SliderMark({required this.value, required this.label});

  factory SliderMark.fromJson(Map<String, Object?> json) => SliderMark(
        value: _num(json['value']) ?? 0,
        label: _str(json['label']) ?? '',
      );

  final num value;
  final String label;
}

/// The renderer's view of a field's `config`.
///
/// An extension rather than a parallel class, so a call site reads `field.prompts` and there is no
/// second object to keep in step with the one `autohub_core` decoded.
extension FormFieldConfig on FormField {
  Map<String, Object?> get _config => config ?? const <String, Object?>{};

  /// A numeric bound on the value, or a count bound on a list. Which one is decided by the value.
  num? get configMin => _num(_config['min']);
  num? get configMax => _num(_config['max']);
  num? get configStep => _num(_config['step']);

  /// Prompted capture slots for `photo_set` — "Front 3/4", "Odometer".
  List<String> get prompts => _strings(_config['prompts']);

  /// Accepted MIME types and/or extensions for `file` and `photo_set`.
  List<String> get accept => _strings(_config['accept']);
  num? get maxSizeMb => _num(_config['maxSizeMb']);
  bool get multiple => _flag(_config['multiple']);

  int? get rows => _int(_config['rows']);
  bool get multiline => _flag(_config['multiline']);

  /// Digit count for `otp`.
  int? get otpLength => _int(_config['length']);

  /// ISO-4217 for `money`. Amounts are always integer minor units.
  String? get currency => _str(_config['currency']);

  /// Dial code for `phone`, e.g. `+91`.
  String? get dialCode => _str(_config['dialCode']);

  /// National significant number length for `phone`.
  int? get nationalLength => _int(_config['nationalLength']);

  /// Where `select` / `multiselect` / `radio` fetch options when they are not inline and not named
  /// by `optionsFromAttribute`.
  String? get optionsSource => _str(_config['optionsSource']);

  /// Extra params handed to the options loader — e.g. `{ "category": "suv" }`.
  Map<String, Object?> get optionsParams => _map(_config['optionsParams']);

  List<PickerLevel> get levels => <PickerLevel>[
        for (final Object? entry in _list(_config['levels'])) PickerLevel.fromJson(_map(entry)),
      ];

  List<AddressPart> get parts => <AddressPart>[
        for (final Object? entry in _list(_config['parts'])) AddressPart.fromJson(_map(entry)),
      ];

  /// `date` bounds, ISO `YYYY-MM-DD`.
  String? get minDate => _str(_config['minDate']);
  String? get maxDate => _str(_config['maxDate']);

  List<SliderMark> get marks => <SliderMark>[
        for (final Object? entry in _list(_config['marks'])) SliderMark.fromJson(_map(entry)),
      ];
}

/// Field lookup across a whole document, ignoring visibility.
///
/// Server-error resolution needs it: a `422` routinely names a field the current values happen to
/// hide, and the message for it still has to come from the registry row.
extension FormDefinitionFields on FormDefinition {
  Iterable<FormField> get allFields => steps.expand((FormStep step) => step.fields);

  FormField? fieldFor(String key) {
    for (final FormStep step in steps) {
      for (final FormField field in step.fields) {
        if (field.key == key) return field;
      }
    }
    return null;
  }
}

/// The machine code a rule carries, whatever kind of rule it is.
///
/// Sealed unions are the right shape for the *rules* and the wrong shape for "give me the code" —
/// without this every call site would repeat an eleven-arm switch to reach two fields that every
/// arm has.
extension ValidationRuleMeta on ValidationRule {
  String get code => switch (this) {
        ValidationRuleMandatory(:final String code) => code,
        ValidationRuleMin(:final String code) => code,
        ValidationRuleMax(:final String code) => code,
        ValidationRuleMinLength(:final String code) => code,
        ValidationRuleMaxLength(:final String code) => code,
        ValidationRulePattern(:final String code) => code,
        ValidationRuleEmail(:final String code) => code,
        ValidationRulePhone(:final String code) => code,
        ValidationRuleOneOf(:final String code) => code,
        ValidationRuleCustom(:final String code) => code,
        ValidationRuleUnsupported(:final String code) => code,
      };

  String get message => switch (this) {
        ValidationRuleMandatory(:final String message) => message,
        ValidationRuleMin(:final String message) => message,
        ValidationRuleMax(:final String message) => message,
        ValidationRuleMinLength(:final String message) => message,
        ValidationRuleMaxLength(:final String message) => message,
        ValidationRulePattern(:final String message) => message,
        ValidationRuleEmail(:final String message) => message,
        ValidationRulePhone(:final String message) => message,
        ValidationRuleOneOf(:final String message) => message,
        ValidationRuleCustom(:final String message) => message,
        ValidationRuleUnsupported(:final String message) => message,
      };
}
