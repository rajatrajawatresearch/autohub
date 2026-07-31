// See the note in `taxonomy.dart` — the workspace root already settled this diagnostic.
// ignore_for_file: invalid_annotation_target

import 'package:autohub_core/src/models/common.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'form.freezed.dart';
part 'form.g.dart';

/// The control vocabulary this client implements.
///
/// A legal enum, because it is a statement about *this build's capabilities*, not about the
/// registry's contents: each value is a widget somebody wrote. [unsupported] is what a control name
/// this build has never heard of decodes to, and the renderer draws a visible placeholder for it —
/// `packages/forms/src/controls/unsupported.tsx` does the same on the web.
///
/// That degradation is not politeness, it is the difference between a sell flow that collects a
/// sale and a blank step. The registries are edited by people who are not shipping this binary; a
/// schema running ahead of a deployed client is a normal Tuesday, and it must cost one field rather
/// than the one screen that creates supply.
enum FormControl {
  @JsonValue('plate')
  plate,
  @JsonValue('text')
  text,
  @JsonValue('number')
  number,
  @JsonValue('select')
  select,
  @JsonValue('multiselect')
  multiselect,
  @JsonValue('radio')
  radio,
  @JsonValue('checkbox')
  checkbox,
  @JsonValue('date')
  date,
  @JsonValue('money')
  money,
  @JsonValue('phone')
  phone,
  @JsonValue('otp')
  otp,
  @JsonValue('file')
  file,
  @JsonValue('photo_set')
  photoSet,
  @JsonValue('slider')
  slider,
  @JsonValue('toggle')
  toggle,
  @JsonValue('address')
  address,
  @JsonValue('vehicle_picker')
  vehiclePicker,
  @JsonValue('rich_text')
  richText,

  /// Not a wire value. What an unrecognised control decodes to.
  unsupported,
}

/// One validation rule, carrying its own machine code and its own pre-resolved message.
///
/// Sealed and discriminated on `rule`, so a renderer that grows a tenth rule cannot forget to
/// handle it. The `code` and `message` travel *with* the rule rather than being derived from it:
/// that is what makes web, Flutter and the server reject the same input for the same stated reason,
/// in the user's language, without three copies of the sentence.
///
/// An unrecognised rule decodes to [ValidationRuleUnsupported] rather than failing the parse. The
/// consequence is chosen deliberately: the client stops enforcing that one rule and the server —
/// which is authoritative anyway — rejects the submission with a `422` that the form puts back on
/// the field. The alternative is that one unknown rule kills the entire form definition, and the
/// user cannot sell their car at all.
@Freezed(unionKey: 'rule', fallbackUnion: 'unsupported')
sealed class ValidationRule with _$ValidationRule {
  @FreezedUnionValue('required')
  const factory ValidationRule.mandatory({
    required String code,
    required String message,
  }) = ValidationRuleMandatory;

  @FreezedUnionValue('min')
  const factory ValidationRule.min({
    required Object value,
    required String code,
    required String message,
  }) = ValidationRuleMin;

  @FreezedUnionValue('max')
  const factory ValidationRule.max({
    required Object value,
    required String code,
    required String message,
  }) = ValidationRuleMax;

  @FreezedUnionValue('minLength')
  const factory ValidationRule.minLength({
    required int value,
    required String code,
    required String message,
  }) = ValidationRuleMinLength;

  @FreezedUnionValue('maxLength')
  const factory ValidationRule.maxLength({
    required int value,
    required String code,
    required String message,
  }) = ValidationRuleMaxLength;

  @FreezedUnionValue('pattern')
  const factory ValidationRule.pattern({
    required String value,
    required String code,
    required String message,
  }) = ValidationRulePattern;

  @FreezedUnionValue('email')
  const factory ValidationRule.email({
    required String code,
    required String message,
  }) = ValidationRuleEmail;

  @FreezedUnionValue('phone')
  const factory ValidationRule.phone({
    required String code,
    required String message,
  }) = ValidationRulePhone;

  @FreezedUnionValue('oneOf')
  const factory ValidationRule.oneOf({
    required List<String> value,
    required String code,
    required String message,
  }) = ValidationRuleOneOf;

  /// Server-side only. The client shows the returned message and does not re-derive the check —
  /// half a custom rule is worse than none, because it disagrees.
  @FreezedUnionValue('custom')
  const factory ValidationRule.custom({
    required String code,
    required String message,
    String? value,
  }) = ValidationRuleCustom;

  /// A rule this build does not implement. Left to the server.
  const factory ValidationRule.unsupported({
    @Default('') String code,
    @Default('') String message,
  }) = ValidationRuleUnsupported;

  factory ValidationRule.fromJson(Map<String, Object?> json) => _$ValidationRuleFromJson(json);
}

@freezed
class FormField with _$FormField {
  const factory FormField({
    required String key,
    required String label,
    @JsonKey(unknownEnumValue: FormControl.unsupported)
    @Default(FormControl.unsupported)
    FormControl control,
    @JsonKey(name: 'required') @Default(false) bool isRequired,
    String? placeholder,
    String? helpText,

    /// `km`, `%`, `₹` — shown beside the input, never folded into the label.
    String? unit,
    Object? defaultValue,
    @Default(false) bool readOnly,

    /// Present for `select` / `multiselect` / `radio`. Absent when the options come from taxonomy.
    List<Option>? options,

    /// Attribute code to resolve options from the taxonomy registry instead of inlining them.
    String? optionsFromAttribute,
    @Default(<ValidationRule>[]) List<ValidationRule> validation,

    /// JsonLogic over the form's own values. Evaluate with `JsonLogic`, never with a hand-rolled
    /// check — `{"==": [{"var": "fuel_type"}, "ev"]}` is why adding EVs needed no code, and it has
    /// to mean the same thing here as it does on the web and on the server.
    Object? visibleWhen,

    /// Control-specific settings, deliberately an open map so the registry can extend a control
    /// without a client release. Read it with the matching config type at the point of use.
    Map<String, Object?>? config,
  }) = _FormField;

  factory FormField.fromJson(Map<String, Object?> json) => _$FormFieldFromJson(json);
}

@freezed
class FormStep with _$FormStep {
  const factory FormStep({
    required String key,
    required String title,
    String? description,
    @Default(<FormField>[]) List<FormField> fields,
    Object? visibleWhen,
  }) = _FormStep;

  factory FormStep.fromJson(Map<String, Object?> json) => _$FormStepFromJson(json);
}

@freezed
class FormDefinition with _$FormDefinition {
  const factory FormDefinition({
    required String key,
    required int version,
    required String title,
    String? description,

    /// The action keeps its name through the flow: `Get my price` → `Getting your price`.
    @Default('Continue') String submitLabel,
    @Default(<FormStep>[]) List<FormStep> steps,
  }) = _FormDefinition;

  factory FormDefinition.fromJson(Map<String, Object?> json) => _$FormDefinitionFromJson(json);
}

@freezed
class FormValidateResponse with _$FormValidateResponse {
  const factory FormValidateResponse({required bool valid}) = _FormValidateResponse;

  factory FormValidateResponse.fromJson(Map<String, Object?> json) =>
      _$FormValidateResponseFromJson(json);
}

/* ── Typed `config` readers ──────────────────────────────────────────────────
   `config` is an open record on the wire so the registry can extend a control without a client
   release. These parse the slice a given control needs, at the point of use, and never throw — a
   malformed config yields defaults, because a bad row must cost a setting rather than a screen. */

@freezed
class PhotoSetConfig with _$PhotoSetConfig {
  const factory PhotoSetConfig({
    @Default(1) int min,
    @Default(20) int max,

    /// "Front 3/4", "Odometer", "Tyres" — the shot list an inspector works through.
    @Default(<String>[]) List<String> prompts,
  }) = _PhotoSetConfig;

  factory PhotoSetConfig.fromJson(Map<String, Object?> json) => _$PhotoSetConfigFromJson(json);
}

@freezed
class SliderConfig with _$SliderConfig {
  const factory SliderConfig({
    @Default(0) double min,
    @Default(100) double max,
    @Default(1) double step,
  }) = _SliderConfig;

  factory SliderConfig.fromJson(Map<String, Object?> json) => _$SliderConfigFromJson(json);
}

@freezed
class OtpConfig with _$OtpConfig {
  const factory OtpConfig({
    @Default(6) int length,
    @Default(30) int resendAfterSeconds,
  }) = _OtpConfig;

  factory OtpConfig.fromJson(Map<String, Object?> json) => _$OtpConfigFromJson(json);
}

@freezed
class FileConfig with _$FileConfig {
  const factory FileConfig({
    @Default(<String>[]) List<String> accept,
    @Default(10) double maxSizeMb,
    @Default(false) bool multiple,
  }) = _FileConfig;

  factory FileConfig.fromJson(Map<String, Object?> json) => _$FileConfigFromJson(json);
}

@freezed
class MoneyConfig with _$MoneyConfig {
  const factory MoneyConfig({
    @Default('INR') String currency,
    int? minMinor,
    int? maxMinor,
  }) = _MoneyConfig;

  factory MoneyConfig.fromJson(Map<String, Object?> json) => _$MoneyConfigFromJson(json);
}
