// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'form.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$ValidationRuleMandatoryImpl _$$ValidationRuleMandatoryImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRuleMandatoryImpl(
  code: json['code'] as String,
  message: json['message'] as String,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRuleMandatoryImplToJson(
  _$ValidationRuleMandatoryImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$ValidationRuleMinImpl _$$ValidationRuleMinImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRuleMinImpl(
  value: json['value'] as Object,
  code: json['code'] as String,
  message: json['message'] as String,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRuleMinImplToJson(
  _$ValidationRuleMinImpl instance,
) => <String, dynamic>{
  'value': instance.value,
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$ValidationRuleMaxImpl _$$ValidationRuleMaxImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRuleMaxImpl(
  value: json['value'] as Object,
  code: json['code'] as String,
  message: json['message'] as String,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRuleMaxImplToJson(
  _$ValidationRuleMaxImpl instance,
) => <String, dynamic>{
  'value': instance.value,
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$ValidationRuleMinLengthImpl _$$ValidationRuleMinLengthImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRuleMinLengthImpl(
  value: (json['value'] as num).toInt(),
  code: json['code'] as String,
  message: json['message'] as String,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRuleMinLengthImplToJson(
  _$ValidationRuleMinLengthImpl instance,
) => <String, dynamic>{
  'value': instance.value,
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$ValidationRuleMaxLengthImpl _$$ValidationRuleMaxLengthImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRuleMaxLengthImpl(
  value: (json['value'] as num).toInt(),
  code: json['code'] as String,
  message: json['message'] as String,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRuleMaxLengthImplToJson(
  _$ValidationRuleMaxLengthImpl instance,
) => <String, dynamic>{
  'value': instance.value,
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$ValidationRulePatternImpl _$$ValidationRulePatternImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRulePatternImpl(
  value: json['value'] as String,
  code: json['code'] as String,
  message: json['message'] as String,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRulePatternImplToJson(
  _$ValidationRulePatternImpl instance,
) => <String, dynamic>{
  'value': instance.value,
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$ValidationRuleEmailImpl _$$ValidationRuleEmailImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRuleEmailImpl(
  code: json['code'] as String,
  message: json['message'] as String,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRuleEmailImplToJson(
  _$ValidationRuleEmailImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$ValidationRulePhoneImpl _$$ValidationRulePhoneImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRulePhoneImpl(
  code: json['code'] as String,
  message: json['message'] as String,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRulePhoneImplToJson(
  _$ValidationRulePhoneImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$ValidationRuleOneOfImpl _$$ValidationRuleOneOfImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRuleOneOfImpl(
  value: (json['value'] as List<dynamic>).map((e) => e as String).toList(),
  code: json['code'] as String,
  message: json['message'] as String,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRuleOneOfImplToJson(
  _$ValidationRuleOneOfImpl instance,
) => <String, dynamic>{
  'value': instance.value,
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$ValidationRuleCustomImpl _$$ValidationRuleCustomImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRuleCustomImpl(
  code: json['code'] as String,
  message: json['message'] as String,
  value: json['value'] as String?,
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRuleCustomImplToJson(
  _$ValidationRuleCustomImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'message': instance.message,
  'value': instance.value,
  'rule': instance.$type,
};

_$ValidationRuleUnsupportedImpl _$$ValidationRuleUnsupportedImplFromJson(
  Map<String, dynamic> json,
) => _$ValidationRuleUnsupportedImpl(
  code: json['code'] as String? ?? '',
  message: json['message'] as String? ?? '',
  $type: json['rule'] as String?,
);

Map<String, dynamic> _$$ValidationRuleUnsupportedImplToJson(
  _$ValidationRuleUnsupportedImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'message': instance.message,
  'rule': instance.$type,
};

_$FormFieldImpl _$$FormFieldImplFromJson(Map<String, dynamic> json) =>
    _$FormFieldImpl(
      key: json['key'] as String,
      label: json['label'] as String,
      control:
          $enumDecodeNullable(
            _$FormControlEnumMap,
            json['control'],
            unknownValue: FormControl.unsupported,
          ) ??
          FormControl.unsupported,
      isRequired: json['required'] as bool? ?? false,
      placeholder: json['placeholder'] as String?,
      helpText: json['helpText'] as String?,
      unit: json['unit'] as String?,
      defaultValue: json['defaultValue'],
      readOnly: json['readOnly'] as bool? ?? false,
      options: (json['options'] as List<dynamic>?)
          ?.map((e) => Option.fromJson(e as Map<String, dynamic>))
          .toList(),
      optionsFromAttribute: json['optionsFromAttribute'] as String?,
      validation:
          (json['validation'] as List<dynamic>?)
              ?.map((e) => ValidationRule.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <ValidationRule>[],
      visibleWhen: json['visibleWhen'],
      config: json['config'] as Map<String, dynamic>?,
    );

Map<String, dynamic> _$$FormFieldImplToJson(_$FormFieldImpl instance) =>
    <String, dynamic>{
      'key': instance.key,
      'label': instance.label,
      'control': _$FormControlEnumMap[instance.control]!,
      'required': instance.isRequired,
      'placeholder': instance.placeholder,
      'helpText': instance.helpText,
      'unit': instance.unit,
      'defaultValue': instance.defaultValue,
      'readOnly': instance.readOnly,
      'options': instance.options,
      'optionsFromAttribute': instance.optionsFromAttribute,
      'validation': instance.validation,
      'visibleWhen': instance.visibleWhen,
      'config': instance.config,
    };

const _$FormControlEnumMap = {
  FormControl.plate: 'plate',
  FormControl.text: 'text',
  FormControl.number: 'number',
  FormControl.select: 'select',
  FormControl.multiselect: 'multiselect',
  FormControl.radio: 'radio',
  FormControl.checkbox: 'checkbox',
  FormControl.date: 'date',
  FormControl.money: 'money',
  FormControl.phone: 'phone',
  FormControl.otp: 'otp',
  FormControl.file: 'file',
  FormControl.photoSet: 'photo_set',
  FormControl.slider: 'slider',
  FormControl.toggle: 'toggle',
  FormControl.address: 'address',
  FormControl.vehiclePicker: 'vehicle_picker',
  FormControl.richText: 'rich_text',
  FormControl.unsupported: 'unsupported',
};

_$FormStepImpl _$$FormStepImplFromJson(Map<String, dynamic> json) =>
    _$FormStepImpl(
      key: json['key'] as String,
      title: json['title'] as String,
      description: json['description'] as String?,
      fields:
          (json['fields'] as List<dynamic>?)
              ?.map((e) => FormField.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <FormField>[],
      visibleWhen: json['visibleWhen'],
    );

Map<String, dynamic> _$$FormStepImplToJson(_$FormStepImpl instance) =>
    <String, dynamic>{
      'key': instance.key,
      'title': instance.title,
      'description': instance.description,
      'fields': instance.fields,
      'visibleWhen': instance.visibleWhen,
    };

_$FormDefinitionImpl _$$FormDefinitionImplFromJson(Map<String, dynamic> json) =>
    _$FormDefinitionImpl(
      key: json['key'] as String,
      version: (json['version'] as num).toInt(),
      title: json['title'] as String,
      description: json['description'] as String?,
      submitLabel: json['submitLabel'] as String? ?? 'Continue',
      steps:
          (json['steps'] as List<dynamic>?)
              ?.map((e) => FormStep.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <FormStep>[],
    );

Map<String, dynamic> _$$FormDefinitionImplToJson(
  _$FormDefinitionImpl instance,
) => <String, dynamic>{
  'key': instance.key,
  'version': instance.version,
  'title': instance.title,
  'description': instance.description,
  'submitLabel': instance.submitLabel,
  'steps': instance.steps,
};

_$FormValidateResponseImpl _$$FormValidateResponseImplFromJson(
  Map<String, dynamic> json,
) => _$FormValidateResponseImpl(valid: json['valid'] as bool);

Map<String, dynamic> _$$FormValidateResponseImplToJson(
  _$FormValidateResponseImpl instance,
) => <String, dynamic>{'valid': instance.valid};

_$PhotoSetConfigImpl _$$PhotoSetConfigImplFromJson(Map<String, dynamic> json) =>
    _$PhotoSetConfigImpl(
      min: (json['min'] as num?)?.toInt() ?? 1,
      max: (json['max'] as num?)?.toInt() ?? 20,
      prompts:
          (json['prompts'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          const <String>[],
    );

Map<String, dynamic> _$$PhotoSetConfigImplToJson(
  _$PhotoSetConfigImpl instance,
) => <String, dynamic>{
  'min': instance.min,
  'max': instance.max,
  'prompts': instance.prompts,
};

_$SliderConfigImpl _$$SliderConfigImplFromJson(Map<String, dynamic> json) =>
    _$SliderConfigImpl(
      min: (json['min'] as num?)?.toDouble() ?? 0,
      max: (json['max'] as num?)?.toDouble() ?? 100,
      step: (json['step'] as num?)?.toDouble() ?? 1,
    );

Map<String, dynamic> _$$SliderConfigImplToJson(_$SliderConfigImpl instance) =>
    <String, dynamic>{
      'min': instance.min,
      'max': instance.max,
      'step': instance.step,
    };

_$OtpConfigImpl _$$OtpConfigImplFromJson(Map<String, dynamic> json) =>
    _$OtpConfigImpl(
      length: (json['length'] as num?)?.toInt() ?? 6,
      resendAfterSeconds: (json['resendAfterSeconds'] as num?)?.toInt() ?? 30,
    );

Map<String, dynamic> _$$OtpConfigImplToJson(_$OtpConfigImpl instance) =>
    <String, dynamic>{
      'length': instance.length,
      'resendAfterSeconds': instance.resendAfterSeconds,
    };

_$FileConfigImpl _$$FileConfigImplFromJson(Map<String, dynamic> json) =>
    _$FileConfigImpl(
      accept:
          (json['accept'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          const <String>[],
      maxSizeMb: (json['maxSizeMb'] as num?)?.toDouble() ?? 10,
      multiple: json['multiple'] as bool? ?? false,
    );

Map<String, dynamic> _$$FileConfigImplToJson(_$FileConfigImpl instance) =>
    <String, dynamic>{
      'accept': instance.accept,
      'maxSizeMb': instance.maxSizeMb,
      'multiple': instance.multiple,
    };

_$MoneyConfigImpl _$$MoneyConfigImplFromJson(Map<String, dynamic> json) =>
    _$MoneyConfigImpl(
      currency: json['currency'] as String? ?? 'INR',
      minMinor: (json['minMinor'] as num?)?.toInt(),
      maxMinor: (json['maxMinor'] as num?)?.toInt(),
    );

Map<String, dynamic> _$$MoneyConfigImplToJson(_$MoneyConfigImpl instance) =>
    <String, dynamic>{
      'currency': instance.currency,
      'minMinor': instance.minMinor,
      'maxMinor': instance.maxMinor,
    };
