/// The registry's validation vocabulary, evaluated on the handset.
///
/// The point is **parity**. `POST /platform/forms/{key}/validate` runs these same rules on the
/// server and answers with machine codes (`metadata.fieldErrors: { "kms_driven": ["kms.too_high"] }`).
/// A payload that passes here must pass there. Two validators that disagree are worse than one
/// validator, because the disagreement is only ever discovered by a user — either a seller blocked
/// by a rule the server would have accepted, or a seller who fills in six screens and is rejected
/// at the end for something the phone should have caught.
///
/// So nothing in this file invents a message and nothing invents a code. Every fault carries the
/// registry's own `code` and the registry's own authored sentence, and the fallbacks below are used
/// only for the rules a registry row implied without spelling out (`required`, a `config.min`).
///
/// The rule union is `autohub_core`'s sealed [ValidationRule], which means the switch below is
/// exhaustive by compiler enforcement. When the contract grows a tenth rule, this file stops
/// compiling — which is the entire reason a sealed union was worth the ceremony, and much better
/// than a `default:` arm quietly passing everything a future registry sends.
///
/// No widgets here, on purpose: the same functions run in a unit test, in the step gate, and behind
/// the submit button.
library;

import 'package:autohub_core/autohub_core.dart';

import 'field_config.dart';
import 'types.dart';
import 'visibility.dart';

/// Used only when a fault reaches the surface without a registry code. Technical, never authored.
const String fallbackFaultCode = 'field.invalid';

/// "Empty" for validation purposes.
///
/// `0` is a real odometer reading and a real price, so it is not empty. `false` **is**: a required
/// consent toggle that is off has not been answered, and treating it as answered is how a loan
/// application ships without a bureau consent.
bool isEmptyValue(Object? value) {
  if (value == null) return true;
  if (value is String) return value.trim().isEmpty;
  if (value is bool) return !value;
  if (value is List) return value.isEmpty;
  if (value is Map) return value.values.every(isEmptyValue);
  return false;
}

/// Length for the things that have one — a list of photographs, a string of characters.
int? _countOf(Object? value) {
  if (value is List) return value.length;
  if (value is String) return value.length;
  return null;
}

num? _numberOf(Object? value) {
  if (value is num) return value.isFinite ? value : null;
  if (value is String && value.trim().isNotEmpty) return num.tryParse(value.trim());
  if (value is DateTime) return value.millisecondsSinceEpoch;
  return null;
}

final RegExp _emailPattern = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]{2,}$');
final RegExp _nonDigits = RegExp(r'\D');

RegExp? _safePattern(String pattern) {
  try {
    return RegExp(pattern);
  } on FormatException {
    // A malformed pattern is a bad registry row. Skipping the rule leaves the server to enforce it;
    // failing every value would lock every seller out of the field until the row was fixed.
    return null;
  }
}

/// `true` when the rule is satisfied.
///
/// [ValidationRuleUnsupported] and [ValidationRuleCustom] both pass. A registry that publishes a
/// rule this binary has not learned yet is a normal Tuesday, and a `custom` rule is server-side by
/// contract — the phone has neither the data nor the authority to answer "is this registration
/// already listed". Failing closed on either would mean a schema change bricks the sell flow on
/// every handset that has not updated.
bool _passes(ValidationRule rule, Object? input, FormField field) => switch (rule) {
      ValidationRuleMandatory() => !isEmptyValue(input),
      ValidationRuleMin(:final Object value) => _atLeast(input, value),
      ValidationRuleMax(:final Object value) => _atMost(input, value),
      ValidationRuleMinLength(:final int value) => (_countOf(input) ?? value) >= value,
      ValidationRuleMaxLength(:final int value) => (_countOf(input) ?? value) <= value,
      ValidationRulePattern(:final String value) =>
        _safePattern(value)?.hasMatch('$input') ?? true,
      ValidationRuleEmail() => _emailPattern.hasMatch('$input'.trim()),
      ValidationRulePhone() => _isPhone('$input', field),
      ValidationRuleOneOf(:final List<String> value) => _isOneOf(input, value),
      ValidationRuleCustom() => true,
      ValidationRuleUnsupported() => true,
    };

/// `min` means a floor on a number *or* a count on a list. Which one is decided by the value's
/// shape, never by a hardcoded list of which controls hold lists — six is a count of photographs
/// and forty is a battery percentage, and the registry writes both the same way.
bool _atLeast(Object? value, Object? bound) {
  final num? floor = _numberOf(bound);
  if (floor == null) return true;
  if (value is List) return value.length >= floor;
  final num? numeric = _numberOf(value);
  return numeric == null || numeric >= floor;
}

bool _atMost(Object? value, Object? bound) {
  final num? ceiling = _numberOf(bound);
  if (ceiling == null) return true;
  if (value is List) return value.length <= ceiling;
  final num? numeric = _numberOf(value);
  return numeric == null || numeric <= ceiling;
}

bool _isPhone(String value, FormField field) {
  final String digits = value.replaceAll(_nonDigits, '');
  final int expected = field.nationalLength ?? 10;
  // Accept the national number with or without its dial code. The phone control stores E.164, and a
  // draft written by an older build may hold the bare ten digits.
  return digits.length == expected ||
      (digits.length > expected && digits.length <= expected + 4);
}

bool _isOneOf(Object? value, List<String> allowed) {
  final Set<String> permitted = allowed.toSet();
  if (value is List) return value.every((Object? entry) => permitted.contains('$entry'));
  return permitted.contains('$value');
}

/// One rule ready to run, with a message that may depend on the value.
///
/// The message is a function because "at least 6" reads differently for a list of photographs and
/// for a battery percentage, and only the value knows which it is.
class _CompiledRule {
  const _CompiledRule({required this.rule, required this.code, required this.message});

  final ValidationRule rule;
  final String code;
  final String Function(Object? value) message;
}

/// Assembles the rules for one field.
///
/// Registry rules first — they carry authored messages and stable codes — then anything implied by
/// `required` and `config` that the registry left to the client. A registry rule always wins: if
/// the row spells out `min` with its own sentence, the synthesised one is never added, so a
/// `config.min` can never shadow a message an operations user wrote on purpose.
List<_CompiledRule> _compileRules(FormField field, FormLabels t) {
  // `is` rather than `runtimeType`: freezed's concrete classes are the private `_$…Impl` types, so
  // comparing runtime types would silently never match and every authored rule would be shadowed by
  // a synthesised one carrying the generic sentence instead of the operations team's.
  bool declares<T extends ValidationRule>() =>
      field.validation.any((ValidationRule rule) => rule is T);

  final List<_CompiledRule> compiled = <_CompiledRule>[];

  if (field.isRequired && !declares<ValidationRuleMandatory>()) {
    compiled.add(
      _CompiledRule(
        rule: ValidationRule.mandatory(code: '${field.key}.required', message: ''),
        code: '${field.key}.required',
        message: (Object? _) => t(FormLabelKeys.validationRequired),
      ),
    );
  }

  for (final ValidationRule rule in field.validation) {
    compiled.add(
      _CompiledRule(
        rule: rule,
        code: rule.code.isEmpty ? fallbackFaultCode : rule.code,
        message: (Object? _) => rule.message,
      ),
    );
  }

  final num? configMin = field.configMin;
  if (configMin != null && !declares<ValidationRuleMin>()) {
    compiled.add(
      _CompiledRule(
        rule: ValidationRule.min(value: configMin, code: '${field.key}.min', message: ''),
        code: '${field.key}.min',
        message: (Object? value) => value is List
            ? t(FormLabelKeys.validationMinItems, <String, Object?>{'value': configMin})
            : t(FormLabelKeys.validationMin, <String, Object?>{'value': configMin}),
      ),
    );
  }

  final num? configMax = field.configMax;
  if (configMax != null && !declares<ValidationRuleMax>()) {
    compiled.add(
      _CompiledRule(
        rule: ValidationRule.max(value: configMax, code: '${field.key}.max', message: ''),
        code: '${field.key}.max',
        message: (Object? value) => value is List
            ? t(FormLabelKeys.validationMaxItems, <String, Object?>{'value': configMax})
            : t(FormLabelKeys.validationMax, <String, Object?>{'value': configMax}),
      ),
    );
  }

  return compiled;
}

/// The first fault on one field, or `null`.
///
/// One fault per field, deliberately. Naming the next thing wrong before the first is fixed is
/// noise, and on a phone it is noise that pushes the input off screen.
FieldFault? validateField(
  FormField field,
  Object? value, {
  FormLabels labels = const FormLabels(),
}) {
  final bool empty = isEmptyValue(value);
  for (final _CompiledRule compiled in _compileRules(field, labels)) {
    // An untouched optional field is not "wrong". Only `required` speaks about emptiness; a `max`
    // of 500000 has nothing to say about a blank odometer.
    if (empty && compiled.rule is! ValidationRuleMandatory) continue;
    if (_passes(compiled.rule, value, field)) continue;
    return FieldFault(code: compiled.code, message: compiled.message(value));
  }
  return null;
}

/// The outcome of one validation run, keyed by field key.
///
/// Deliberately the same shape the server sends back in `metadata.fieldErrors`, so the local path
/// and the 422 path feed the same rendering code and a fault looks identical whichever raised it.
class FormValidation {
  const FormValidation(this.fieldErrors);

  static const FormValidation valid = FormValidation(<String, FieldFault>{});

  final Map<String, FieldFault> fieldErrors;

  bool get isValid => fieldErrors.isEmpty;
}

/// Validates the values as they currently stand.
///
/// The rule set is rebuilt on every run rather than cached, because **visibility is part of the
/// schema**: the shape of a sell form for an EV is not the shape of a sell form for a diesel, and
/// the difference has to appear the instant `fuel_type` changes. A validator captured at mount
/// would never learn that `battery_health_pct` had become required.
///
/// [fieldKeys] restricts the run to a subset — how a step gate validates without blocking the user
/// on a field two screens ahead that they have not been shown yet.
FormValidation validateForm(
  FormDefinition definition,
  FormValues values, {
  Set<String>? fieldKeys,
  FormLabels labels = const FormLabels(),
}) {
  final Map<String, FieldFault> faults = <String, FieldFault>{};
  for (final FormField field in visibleFieldsOf(definition, values)) {
    if (fieldKeys != null && !fieldKeys.contains(field.key)) continue;
    final FieldFault? fault = validateField(field, values[field.key], labels: labels);
    if (fault != null) faults[field.key] = fault;
  }
  return FormValidation(faults);
}
