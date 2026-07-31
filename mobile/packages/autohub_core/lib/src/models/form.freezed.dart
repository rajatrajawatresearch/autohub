// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'form.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

ValidationRule _$ValidationRuleFromJson(Map<String, dynamic> json) {
  switch (json['rule']) {
    case 'required':
      return ValidationRuleMandatory.fromJson(json);
    case 'min':
      return ValidationRuleMin.fromJson(json);
    case 'max':
      return ValidationRuleMax.fromJson(json);
    case 'minLength':
      return ValidationRuleMinLength.fromJson(json);
    case 'maxLength':
      return ValidationRuleMaxLength.fromJson(json);
    case 'pattern':
      return ValidationRulePattern.fromJson(json);
    case 'email':
      return ValidationRuleEmail.fromJson(json);
    case 'phone':
      return ValidationRulePhone.fromJson(json);
    case 'oneOf':
      return ValidationRuleOneOf.fromJson(json);
    case 'custom':
      return ValidationRuleCustom.fromJson(json);

    default:
      return ValidationRuleUnsupported.fromJson(json);
  }
}

/// @nodoc
mixin _$ValidationRule {
  String get code => throw _privateConstructorUsedError;
  String get message => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) => throw _privateConstructorUsedError;

  /// Serializes this ValidationRule to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ValidationRuleCopyWith<ValidationRule> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ValidationRuleCopyWith<$Res> {
  factory $ValidationRuleCopyWith(
    ValidationRule value,
    $Res Function(ValidationRule) then,
  ) = _$ValidationRuleCopyWithImpl<$Res, ValidationRule>;
  @useResult
  $Res call({String code, String message});
}

/// @nodoc
class _$ValidationRuleCopyWithImpl<$Res, $Val extends ValidationRule>
    implements $ValidationRuleCopyWith<$Res> {
  _$ValidationRuleCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? code = null, Object? message = null}) {
    return _then(
      _value.copyWith(
            code: null == code
                ? _value.code
                : code // ignore: cast_nullable_to_non_nullable
                      as String,
            message: null == message
                ? _value.message
                : message // ignore: cast_nullable_to_non_nullable
                      as String,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$ValidationRuleMandatoryImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRuleMandatoryImplCopyWith(
    _$ValidationRuleMandatoryImpl value,
    $Res Function(_$ValidationRuleMandatoryImpl) then,
  ) = __$$ValidationRuleMandatoryImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String message});
}

/// @nodoc
class __$$ValidationRuleMandatoryImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRuleMandatoryImpl>
    implements _$$ValidationRuleMandatoryImplCopyWith<$Res> {
  __$$ValidationRuleMandatoryImplCopyWithImpl(
    _$ValidationRuleMandatoryImpl _value,
    $Res Function(_$ValidationRuleMandatoryImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? code = null, Object? message = null}) {
    return _then(
      _$ValidationRuleMandatoryImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRuleMandatoryImpl implements ValidationRuleMandatory {
  const _$ValidationRuleMandatoryImpl({
    required this.code,
    required this.message,
    final String? $type,
  }) : $type = $type ?? 'required';

  factory _$ValidationRuleMandatoryImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRuleMandatoryImplFromJson(json);

  @override
  final String code;
  @override
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.mandatory(code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRuleMandatoryImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, code, message);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRuleMandatoryImplCopyWith<_$ValidationRuleMandatoryImpl>
  get copyWith =>
      __$$ValidationRuleMandatoryImplCopyWithImpl<
        _$ValidationRuleMandatoryImpl
      >(this, _$identity);

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return mandatory(code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return mandatory?.call(code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (mandatory != null) {
      return mandatory(code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return mandatory(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return mandatory?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (mandatory != null) {
      return mandatory(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRuleMandatoryImplToJson(this);
  }
}

abstract class ValidationRuleMandatory implements ValidationRule {
  const factory ValidationRuleMandatory({
    required final String code,
    required final String message,
  }) = _$ValidationRuleMandatoryImpl;

  factory ValidationRuleMandatory.fromJson(Map<String, dynamic> json) =
      _$ValidationRuleMandatoryImpl.fromJson;

  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRuleMandatoryImplCopyWith<_$ValidationRuleMandatoryImpl>
  get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRuleMinImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRuleMinImplCopyWith(
    _$ValidationRuleMinImpl value,
    $Res Function(_$ValidationRuleMinImpl) then,
  ) = __$$ValidationRuleMinImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({Object value, String code, String message});
}

/// @nodoc
class __$$ValidationRuleMinImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRuleMinImpl>
    implements _$$ValidationRuleMinImplCopyWith<$Res> {
  __$$ValidationRuleMinImplCopyWithImpl(
    _$ValidationRuleMinImpl _value,
    $Res Function(_$ValidationRuleMinImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? value = null,
    Object? code = null,
    Object? message = null,
  }) {
    return _then(
      _$ValidationRuleMinImpl(
        value: null == value ? _value.value : value,
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRuleMinImpl implements ValidationRuleMin {
  const _$ValidationRuleMinImpl({
    required this.value,
    required this.code,
    required this.message,
    final String? $type,
  }) : $type = $type ?? 'min';

  factory _$ValidationRuleMinImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRuleMinImplFromJson(json);

  @override
  final Object value;
  @override
  final String code;
  @override
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.min(value: $value, code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRuleMinImpl &&
            const DeepCollectionEquality().equals(other.value, value) &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    const DeepCollectionEquality().hash(value),
    code,
    message,
  );

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRuleMinImplCopyWith<_$ValidationRuleMinImpl> get copyWith =>
      __$$ValidationRuleMinImplCopyWithImpl<_$ValidationRuleMinImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return min(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return min?.call(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (min != null) {
      return min(value, code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return min(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return min?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (min != null) {
      return min(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRuleMinImplToJson(this);
  }
}

abstract class ValidationRuleMin implements ValidationRule {
  const factory ValidationRuleMin({
    required final Object value,
    required final String code,
    required final String message,
  }) = _$ValidationRuleMinImpl;

  factory ValidationRuleMin.fromJson(Map<String, dynamic> json) =
      _$ValidationRuleMinImpl.fromJson;

  Object get value;
  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRuleMinImplCopyWith<_$ValidationRuleMinImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRuleMaxImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRuleMaxImplCopyWith(
    _$ValidationRuleMaxImpl value,
    $Res Function(_$ValidationRuleMaxImpl) then,
  ) = __$$ValidationRuleMaxImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({Object value, String code, String message});
}

/// @nodoc
class __$$ValidationRuleMaxImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRuleMaxImpl>
    implements _$$ValidationRuleMaxImplCopyWith<$Res> {
  __$$ValidationRuleMaxImplCopyWithImpl(
    _$ValidationRuleMaxImpl _value,
    $Res Function(_$ValidationRuleMaxImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? value = null,
    Object? code = null,
    Object? message = null,
  }) {
    return _then(
      _$ValidationRuleMaxImpl(
        value: null == value ? _value.value : value,
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRuleMaxImpl implements ValidationRuleMax {
  const _$ValidationRuleMaxImpl({
    required this.value,
    required this.code,
    required this.message,
    final String? $type,
  }) : $type = $type ?? 'max';

  factory _$ValidationRuleMaxImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRuleMaxImplFromJson(json);

  @override
  final Object value;
  @override
  final String code;
  @override
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.max(value: $value, code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRuleMaxImpl &&
            const DeepCollectionEquality().equals(other.value, value) &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    const DeepCollectionEquality().hash(value),
    code,
    message,
  );

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRuleMaxImplCopyWith<_$ValidationRuleMaxImpl> get copyWith =>
      __$$ValidationRuleMaxImplCopyWithImpl<_$ValidationRuleMaxImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return max(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return max?.call(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (max != null) {
      return max(value, code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return max(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return max?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (max != null) {
      return max(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRuleMaxImplToJson(this);
  }
}

abstract class ValidationRuleMax implements ValidationRule {
  const factory ValidationRuleMax({
    required final Object value,
    required final String code,
    required final String message,
  }) = _$ValidationRuleMaxImpl;

  factory ValidationRuleMax.fromJson(Map<String, dynamic> json) =
      _$ValidationRuleMaxImpl.fromJson;

  Object get value;
  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRuleMaxImplCopyWith<_$ValidationRuleMaxImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRuleMinLengthImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRuleMinLengthImplCopyWith(
    _$ValidationRuleMinLengthImpl value,
    $Res Function(_$ValidationRuleMinLengthImpl) then,
  ) = __$$ValidationRuleMinLengthImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int value, String code, String message});
}

/// @nodoc
class __$$ValidationRuleMinLengthImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRuleMinLengthImpl>
    implements _$$ValidationRuleMinLengthImplCopyWith<$Res> {
  __$$ValidationRuleMinLengthImplCopyWithImpl(
    _$ValidationRuleMinLengthImpl _value,
    $Res Function(_$ValidationRuleMinLengthImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? value = null,
    Object? code = null,
    Object? message = null,
  }) {
    return _then(
      _$ValidationRuleMinLengthImpl(
        value: null == value
            ? _value.value
            : value // ignore: cast_nullable_to_non_nullable
                  as int,
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRuleMinLengthImpl implements ValidationRuleMinLength {
  const _$ValidationRuleMinLengthImpl({
    required this.value,
    required this.code,
    required this.message,
    final String? $type,
  }) : $type = $type ?? 'minLength';

  factory _$ValidationRuleMinLengthImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRuleMinLengthImplFromJson(json);

  @override
  final int value;
  @override
  final String code;
  @override
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.minLength(value: $value, code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRuleMinLengthImpl &&
            (identical(other.value, value) || other.value == value) &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, value, code, message);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRuleMinLengthImplCopyWith<_$ValidationRuleMinLengthImpl>
  get copyWith =>
      __$$ValidationRuleMinLengthImplCopyWithImpl<
        _$ValidationRuleMinLengthImpl
      >(this, _$identity);

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return minLength(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return minLength?.call(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (minLength != null) {
      return minLength(value, code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return minLength(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return minLength?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (minLength != null) {
      return minLength(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRuleMinLengthImplToJson(this);
  }
}

abstract class ValidationRuleMinLength implements ValidationRule {
  const factory ValidationRuleMinLength({
    required final int value,
    required final String code,
    required final String message,
  }) = _$ValidationRuleMinLengthImpl;

  factory ValidationRuleMinLength.fromJson(Map<String, dynamic> json) =
      _$ValidationRuleMinLengthImpl.fromJson;

  int get value;
  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRuleMinLengthImplCopyWith<_$ValidationRuleMinLengthImpl>
  get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRuleMaxLengthImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRuleMaxLengthImplCopyWith(
    _$ValidationRuleMaxLengthImpl value,
    $Res Function(_$ValidationRuleMaxLengthImpl) then,
  ) = __$$ValidationRuleMaxLengthImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int value, String code, String message});
}

/// @nodoc
class __$$ValidationRuleMaxLengthImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRuleMaxLengthImpl>
    implements _$$ValidationRuleMaxLengthImplCopyWith<$Res> {
  __$$ValidationRuleMaxLengthImplCopyWithImpl(
    _$ValidationRuleMaxLengthImpl _value,
    $Res Function(_$ValidationRuleMaxLengthImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? value = null,
    Object? code = null,
    Object? message = null,
  }) {
    return _then(
      _$ValidationRuleMaxLengthImpl(
        value: null == value
            ? _value.value
            : value // ignore: cast_nullable_to_non_nullable
                  as int,
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRuleMaxLengthImpl implements ValidationRuleMaxLength {
  const _$ValidationRuleMaxLengthImpl({
    required this.value,
    required this.code,
    required this.message,
    final String? $type,
  }) : $type = $type ?? 'maxLength';

  factory _$ValidationRuleMaxLengthImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRuleMaxLengthImplFromJson(json);

  @override
  final int value;
  @override
  final String code;
  @override
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.maxLength(value: $value, code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRuleMaxLengthImpl &&
            (identical(other.value, value) || other.value == value) &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, value, code, message);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRuleMaxLengthImplCopyWith<_$ValidationRuleMaxLengthImpl>
  get copyWith =>
      __$$ValidationRuleMaxLengthImplCopyWithImpl<
        _$ValidationRuleMaxLengthImpl
      >(this, _$identity);

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return maxLength(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return maxLength?.call(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (maxLength != null) {
      return maxLength(value, code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return maxLength(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return maxLength?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (maxLength != null) {
      return maxLength(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRuleMaxLengthImplToJson(this);
  }
}

abstract class ValidationRuleMaxLength implements ValidationRule {
  const factory ValidationRuleMaxLength({
    required final int value,
    required final String code,
    required final String message,
  }) = _$ValidationRuleMaxLengthImpl;

  factory ValidationRuleMaxLength.fromJson(Map<String, dynamic> json) =
      _$ValidationRuleMaxLengthImpl.fromJson;

  int get value;
  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRuleMaxLengthImplCopyWith<_$ValidationRuleMaxLengthImpl>
  get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRulePatternImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRulePatternImplCopyWith(
    _$ValidationRulePatternImpl value,
    $Res Function(_$ValidationRulePatternImpl) then,
  ) = __$$ValidationRulePatternImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String value, String code, String message});
}

/// @nodoc
class __$$ValidationRulePatternImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRulePatternImpl>
    implements _$$ValidationRulePatternImplCopyWith<$Res> {
  __$$ValidationRulePatternImplCopyWithImpl(
    _$ValidationRulePatternImpl _value,
    $Res Function(_$ValidationRulePatternImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? value = null,
    Object? code = null,
    Object? message = null,
  }) {
    return _then(
      _$ValidationRulePatternImpl(
        value: null == value
            ? _value.value
            : value // ignore: cast_nullable_to_non_nullable
                  as String,
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRulePatternImpl implements ValidationRulePattern {
  const _$ValidationRulePatternImpl({
    required this.value,
    required this.code,
    required this.message,
    final String? $type,
  }) : $type = $type ?? 'pattern';

  factory _$ValidationRulePatternImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRulePatternImplFromJson(json);

  @override
  final String value;
  @override
  final String code;
  @override
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.pattern(value: $value, code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRulePatternImpl &&
            (identical(other.value, value) || other.value == value) &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, value, code, message);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRulePatternImplCopyWith<_$ValidationRulePatternImpl>
  get copyWith =>
      __$$ValidationRulePatternImplCopyWithImpl<_$ValidationRulePatternImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return pattern(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return pattern?.call(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (pattern != null) {
      return pattern(value, code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return pattern(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return pattern?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (pattern != null) {
      return pattern(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRulePatternImplToJson(this);
  }
}

abstract class ValidationRulePattern implements ValidationRule {
  const factory ValidationRulePattern({
    required final String value,
    required final String code,
    required final String message,
  }) = _$ValidationRulePatternImpl;

  factory ValidationRulePattern.fromJson(Map<String, dynamic> json) =
      _$ValidationRulePatternImpl.fromJson;

  String get value;
  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRulePatternImplCopyWith<_$ValidationRulePatternImpl>
  get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRuleEmailImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRuleEmailImplCopyWith(
    _$ValidationRuleEmailImpl value,
    $Res Function(_$ValidationRuleEmailImpl) then,
  ) = __$$ValidationRuleEmailImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String message});
}

/// @nodoc
class __$$ValidationRuleEmailImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRuleEmailImpl>
    implements _$$ValidationRuleEmailImplCopyWith<$Res> {
  __$$ValidationRuleEmailImplCopyWithImpl(
    _$ValidationRuleEmailImpl _value,
    $Res Function(_$ValidationRuleEmailImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? code = null, Object? message = null}) {
    return _then(
      _$ValidationRuleEmailImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRuleEmailImpl implements ValidationRuleEmail {
  const _$ValidationRuleEmailImpl({
    required this.code,
    required this.message,
    final String? $type,
  }) : $type = $type ?? 'email';

  factory _$ValidationRuleEmailImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRuleEmailImplFromJson(json);

  @override
  final String code;
  @override
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.email(code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRuleEmailImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, code, message);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRuleEmailImplCopyWith<_$ValidationRuleEmailImpl> get copyWith =>
      __$$ValidationRuleEmailImplCopyWithImpl<_$ValidationRuleEmailImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return email(code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return email?.call(code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (email != null) {
      return email(code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return email(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return email?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (email != null) {
      return email(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRuleEmailImplToJson(this);
  }
}

abstract class ValidationRuleEmail implements ValidationRule {
  const factory ValidationRuleEmail({
    required final String code,
    required final String message,
  }) = _$ValidationRuleEmailImpl;

  factory ValidationRuleEmail.fromJson(Map<String, dynamic> json) =
      _$ValidationRuleEmailImpl.fromJson;

  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRuleEmailImplCopyWith<_$ValidationRuleEmailImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRulePhoneImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRulePhoneImplCopyWith(
    _$ValidationRulePhoneImpl value,
    $Res Function(_$ValidationRulePhoneImpl) then,
  ) = __$$ValidationRulePhoneImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String message});
}

/// @nodoc
class __$$ValidationRulePhoneImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRulePhoneImpl>
    implements _$$ValidationRulePhoneImplCopyWith<$Res> {
  __$$ValidationRulePhoneImplCopyWithImpl(
    _$ValidationRulePhoneImpl _value,
    $Res Function(_$ValidationRulePhoneImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? code = null, Object? message = null}) {
    return _then(
      _$ValidationRulePhoneImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRulePhoneImpl implements ValidationRulePhone {
  const _$ValidationRulePhoneImpl({
    required this.code,
    required this.message,
    final String? $type,
  }) : $type = $type ?? 'phone';

  factory _$ValidationRulePhoneImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRulePhoneImplFromJson(json);

  @override
  final String code;
  @override
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.phone(code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRulePhoneImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, code, message);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRulePhoneImplCopyWith<_$ValidationRulePhoneImpl> get copyWith =>
      __$$ValidationRulePhoneImplCopyWithImpl<_$ValidationRulePhoneImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return phone(code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return phone?.call(code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (phone != null) {
      return phone(code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return phone(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return phone?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (phone != null) {
      return phone(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRulePhoneImplToJson(this);
  }
}

abstract class ValidationRulePhone implements ValidationRule {
  const factory ValidationRulePhone({
    required final String code,
    required final String message,
  }) = _$ValidationRulePhoneImpl;

  factory ValidationRulePhone.fromJson(Map<String, dynamic> json) =
      _$ValidationRulePhoneImpl.fromJson;

  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRulePhoneImplCopyWith<_$ValidationRulePhoneImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRuleOneOfImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRuleOneOfImplCopyWith(
    _$ValidationRuleOneOfImpl value,
    $Res Function(_$ValidationRuleOneOfImpl) then,
  ) = __$$ValidationRuleOneOfImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({List<String> value, String code, String message});
}

/// @nodoc
class __$$ValidationRuleOneOfImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRuleOneOfImpl>
    implements _$$ValidationRuleOneOfImplCopyWith<$Res> {
  __$$ValidationRuleOneOfImplCopyWithImpl(
    _$ValidationRuleOneOfImpl _value,
    $Res Function(_$ValidationRuleOneOfImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? value = null,
    Object? code = null,
    Object? message = null,
  }) {
    return _then(
      _$ValidationRuleOneOfImpl(
        value: null == value
            ? _value._value
            : value // ignore: cast_nullable_to_non_nullable
                  as List<String>,
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRuleOneOfImpl implements ValidationRuleOneOf {
  const _$ValidationRuleOneOfImpl({
    required final List<String> value,
    required this.code,
    required this.message,
    final String? $type,
  }) : _value = value,
       $type = $type ?? 'oneOf';

  factory _$ValidationRuleOneOfImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRuleOneOfImplFromJson(json);

  final List<String> _value;
  @override
  List<String> get value {
    if (_value is EqualUnmodifiableListView) return _value;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_value);
  }

  @override
  final String code;
  @override
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.oneOf(value: $value, code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRuleOneOfImpl &&
            const DeepCollectionEquality().equals(other._value, _value) &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    const DeepCollectionEquality().hash(_value),
    code,
    message,
  );

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRuleOneOfImplCopyWith<_$ValidationRuleOneOfImpl> get copyWith =>
      __$$ValidationRuleOneOfImplCopyWithImpl<_$ValidationRuleOneOfImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return oneOf(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return oneOf?.call(value, code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (oneOf != null) {
      return oneOf(value, code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return oneOf(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return oneOf?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (oneOf != null) {
      return oneOf(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRuleOneOfImplToJson(this);
  }
}

abstract class ValidationRuleOneOf implements ValidationRule {
  const factory ValidationRuleOneOf({
    required final List<String> value,
    required final String code,
    required final String message,
  }) = _$ValidationRuleOneOfImpl;

  factory ValidationRuleOneOf.fromJson(Map<String, dynamic> json) =
      _$ValidationRuleOneOfImpl.fromJson;

  List<String> get value;
  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRuleOneOfImplCopyWith<_$ValidationRuleOneOfImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRuleCustomImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRuleCustomImplCopyWith(
    _$ValidationRuleCustomImpl value,
    $Res Function(_$ValidationRuleCustomImpl) then,
  ) = __$$ValidationRuleCustomImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String message, String? value});
}

/// @nodoc
class __$$ValidationRuleCustomImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRuleCustomImpl>
    implements _$$ValidationRuleCustomImplCopyWith<$Res> {
  __$$ValidationRuleCustomImplCopyWithImpl(
    _$ValidationRuleCustomImpl _value,
    $Res Function(_$ValidationRuleCustomImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? message = null,
    Object? value = freezed,
  }) {
    return _then(
      _$ValidationRuleCustomImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
        value: freezed == value
            ? _value.value
            : value // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRuleCustomImpl implements ValidationRuleCustom {
  const _$ValidationRuleCustomImpl({
    required this.code,
    required this.message,
    this.value,
    final String? $type,
  }) : $type = $type ?? 'custom';

  factory _$ValidationRuleCustomImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRuleCustomImplFromJson(json);

  @override
  final String code;
  @override
  final String message;
  @override
  final String? value;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.custom(code: $code, message: $message, value: $value)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRuleCustomImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message) &&
            (identical(other.value, value) || other.value == value));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, code, message, value);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRuleCustomImplCopyWith<_$ValidationRuleCustomImpl>
  get copyWith =>
      __$$ValidationRuleCustomImplCopyWithImpl<_$ValidationRuleCustomImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return custom(code, message, value);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return custom?.call(code, message, value);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (custom != null) {
      return custom(code, message, value);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return custom(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return custom?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (custom != null) {
      return custom(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRuleCustomImplToJson(this);
  }
}

abstract class ValidationRuleCustom implements ValidationRule {
  const factory ValidationRuleCustom({
    required final String code,
    required final String message,
    final String? value,
  }) = _$ValidationRuleCustomImpl;

  factory ValidationRuleCustom.fromJson(Map<String, dynamic> json) =
      _$ValidationRuleCustomImpl.fromJson;

  @override
  String get code;
  @override
  String get message;
  String? get value;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRuleCustomImplCopyWith<_$ValidationRuleCustomImpl>
  get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$ValidationRuleUnsupportedImplCopyWith<$Res>
    implements $ValidationRuleCopyWith<$Res> {
  factory _$$ValidationRuleUnsupportedImplCopyWith(
    _$ValidationRuleUnsupportedImpl value,
    $Res Function(_$ValidationRuleUnsupportedImpl) then,
  ) = __$$ValidationRuleUnsupportedImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String message});
}

/// @nodoc
class __$$ValidationRuleUnsupportedImplCopyWithImpl<$Res>
    extends _$ValidationRuleCopyWithImpl<$Res, _$ValidationRuleUnsupportedImpl>
    implements _$$ValidationRuleUnsupportedImplCopyWith<$Res> {
  __$$ValidationRuleUnsupportedImplCopyWithImpl(
    _$ValidationRuleUnsupportedImpl _value,
    $Res Function(_$ValidationRuleUnsupportedImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? code = null, Object? message = null}) {
    return _then(
      _$ValidationRuleUnsupportedImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        message: null == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValidationRuleUnsupportedImpl implements ValidationRuleUnsupported {
  const _$ValidationRuleUnsupportedImpl({
    this.code = '',
    this.message = '',
    final String? $type,
  }) : $type = $type ?? 'unsupported';

  factory _$ValidationRuleUnsupportedImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValidationRuleUnsupportedImplFromJson(json);

  @override
  @JsonKey()
  final String code;
  @override
  @JsonKey()
  final String message;

  @JsonKey(name: 'rule')
  final String $type;

  @override
  String toString() {
    return 'ValidationRule.unsupported(code: $code, message: $message)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationRuleUnsupportedImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.message, message) || other.message == message));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, code, message);

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationRuleUnsupportedImplCopyWith<_$ValidationRuleUnsupportedImpl>
  get copyWith =>
      __$$ValidationRuleUnsupportedImplCopyWithImpl<
        _$ValidationRuleUnsupportedImpl
      >(this, _$identity);

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String message) mandatory,
    required TResult Function(Object value, String code, String message) min,
    required TResult Function(Object value, String code, String message) max,
    required TResult Function(int value, String code, String message) minLength,
    required TResult Function(int value, String code, String message) maxLength,
    required TResult Function(String value, String code, String message)
    pattern,
    required TResult Function(String code, String message) email,
    required TResult Function(String code, String message) phone,
    required TResult Function(List<String> value, String code, String message)
    oneOf,
    required TResult Function(String code, String message, String? value)
    custom,
    required TResult Function(String code, String message) unsupported,
  }) {
    return unsupported(code, message);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String message)? mandatory,
    TResult? Function(Object value, String code, String message)? min,
    TResult? Function(Object value, String code, String message)? max,
    TResult? Function(int value, String code, String message)? minLength,
    TResult? Function(int value, String code, String message)? maxLength,
    TResult? Function(String value, String code, String message)? pattern,
    TResult? Function(String code, String message)? email,
    TResult? Function(String code, String message)? phone,
    TResult? Function(List<String> value, String code, String message)? oneOf,
    TResult? Function(String code, String message, String? value)? custom,
    TResult? Function(String code, String message)? unsupported,
  }) {
    return unsupported?.call(code, message);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String message)? mandatory,
    TResult Function(Object value, String code, String message)? min,
    TResult Function(Object value, String code, String message)? max,
    TResult Function(int value, String code, String message)? minLength,
    TResult Function(int value, String code, String message)? maxLength,
    TResult Function(String value, String code, String message)? pattern,
    TResult Function(String code, String message)? email,
    TResult Function(String code, String message)? phone,
    TResult Function(List<String> value, String code, String message)? oneOf,
    TResult Function(String code, String message, String? value)? custom,
    TResult Function(String code, String message)? unsupported,
    required TResult orElse(),
  }) {
    if (unsupported != null) {
      return unsupported(code, message);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(ValidationRuleMandatory value) mandatory,
    required TResult Function(ValidationRuleMin value) min,
    required TResult Function(ValidationRuleMax value) max,
    required TResult Function(ValidationRuleMinLength value) minLength,
    required TResult Function(ValidationRuleMaxLength value) maxLength,
    required TResult Function(ValidationRulePattern value) pattern,
    required TResult Function(ValidationRuleEmail value) email,
    required TResult Function(ValidationRulePhone value) phone,
    required TResult Function(ValidationRuleOneOf value) oneOf,
    required TResult Function(ValidationRuleCustom value) custom,
    required TResult Function(ValidationRuleUnsupported value) unsupported,
  }) {
    return unsupported(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(ValidationRuleMandatory value)? mandatory,
    TResult? Function(ValidationRuleMin value)? min,
    TResult? Function(ValidationRuleMax value)? max,
    TResult? Function(ValidationRuleMinLength value)? minLength,
    TResult? Function(ValidationRuleMaxLength value)? maxLength,
    TResult? Function(ValidationRulePattern value)? pattern,
    TResult? Function(ValidationRuleEmail value)? email,
    TResult? Function(ValidationRulePhone value)? phone,
    TResult? Function(ValidationRuleOneOf value)? oneOf,
    TResult? Function(ValidationRuleCustom value)? custom,
    TResult? Function(ValidationRuleUnsupported value)? unsupported,
  }) {
    return unsupported?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(ValidationRuleMandatory value)? mandatory,
    TResult Function(ValidationRuleMin value)? min,
    TResult Function(ValidationRuleMax value)? max,
    TResult Function(ValidationRuleMinLength value)? minLength,
    TResult Function(ValidationRuleMaxLength value)? maxLength,
    TResult Function(ValidationRulePattern value)? pattern,
    TResult Function(ValidationRuleEmail value)? email,
    TResult Function(ValidationRulePhone value)? phone,
    TResult Function(ValidationRuleOneOf value)? oneOf,
    TResult Function(ValidationRuleCustom value)? custom,
    TResult Function(ValidationRuleUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (unsupported != null) {
      return unsupported(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$ValidationRuleUnsupportedImplToJson(this);
  }
}

abstract class ValidationRuleUnsupported implements ValidationRule {
  const factory ValidationRuleUnsupported({
    final String code,
    final String message,
  }) = _$ValidationRuleUnsupportedImpl;

  factory ValidationRuleUnsupported.fromJson(Map<String, dynamic> json) =
      _$ValidationRuleUnsupportedImpl.fromJson;

  @override
  String get code;
  @override
  String get message;

  /// Create a copy of ValidationRule
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationRuleUnsupportedImplCopyWith<_$ValidationRuleUnsupportedImpl>
  get copyWith => throw _privateConstructorUsedError;
}

FormField _$FormFieldFromJson(Map<String, dynamic> json) {
  return _FormField.fromJson(json);
}

/// @nodoc
mixin _$FormField {
  String get key => throw _privateConstructorUsedError;
  String get label => throw _privateConstructorUsedError;
  @JsonKey(unknownEnumValue: FormControl.unsupported)
  FormControl get control => throw _privateConstructorUsedError;
  @JsonKey(name: 'required')
  bool get isRequired => throw _privateConstructorUsedError;
  String? get placeholder => throw _privateConstructorUsedError;
  String? get helpText => throw _privateConstructorUsedError;

  /// `km`, `%`, `₹` — shown beside the input, never folded into the label.
  String? get unit => throw _privateConstructorUsedError;
  Object? get defaultValue => throw _privateConstructorUsedError;
  bool get readOnly => throw _privateConstructorUsedError;

  /// Present for `select` / `multiselect` / `radio`. Absent when the options come from taxonomy.
  List<Option>? get options => throw _privateConstructorUsedError;

  /// Attribute code to resolve options from the taxonomy registry instead of inlining them.
  String? get optionsFromAttribute => throw _privateConstructorUsedError;
  List<ValidationRule> get validation => throw _privateConstructorUsedError;

  /// JsonLogic over the form's own values. Evaluate with `JsonLogic`, never with a hand-rolled
  /// check — `{"==": [{"var": "fuel_type"}, "ev"]}` is why adding EVs needed no code, and it has
  /// to mean the same thing here as it does on the web and on the server.
  Object? get visibleWhen => throw _privateConstructorUsedError;

  /// Control-specific settings, deliberately an open map so the registry can extend a control
  /// without a client release. Read it with the matching config type at the point of use.
  Map<String, Object?>? get config => throw _privateConstructorUsedError;

  /// Serializes this FormField to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of FormField
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $FormFieldCopyWith<FormField> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $FormFieldCopyWith<$Res> {
  factory $FormFieldCopyWith(FormField value, $Res Function(FormField) then) =
      _$FormFieldCopyWithImpl<$Res, FormField>;
  @useResult
  $Res call({
    String key,
    String label,
    @JsonKey(unknownEnumValue: FormControl.unsupported) FormControl control,
    @JsonKey(name: 'required') bool isRequired,
    String? placeholder,
    String? helpText,
    String? unit,
    Object? defaultValue,
    bool readOnly,
    List<Option>? options,
    String? optionsFromAttribute,
    List<ValidationRule> validation,
    Object? visibleWhen,
    Map<String, Object?>? config,
  });
}

/// @nodoc
class _$FormFieldCopyWithImpl<$Res, $Val extends FormField>
    implements $FormFieldCopyWith<$Res> {
  _$FormFieldCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of FormField
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? key = null,
    Object? label = null,
    Object? control = null,
    Object? isRequired = null,
    Object? placeholder = freezed,
    Object? helpText = freezed,
    Object? unit = freezed,
    Object? defaultValue = freezed,
    Object? readOnly = null,
    Object? options = freezed,
    Object? optionsFromAttribute = freezed,
    Object? validation = null,
    Object? visibleWhen = freezed,
    Object? config = freezed,
  }) {
    return _then(
      _value.copyWith(
            key: null == key
                ? _value.key
                : key // ignore: cast_nullable_to_non_nullable
                      as String,
            label: null == label
                ? _value.label
                : label // ignore: cast_nullable_to_non_nullable
                      as String,
            control: null == control
                ? _value.control
                : control // ignore: cast_nullable_to_non_nullable
                      as FormControl,
            isRequired: null == isRequired
                ? _value.isRequired
                : isRequired // ignore: cast_nullable_to_non_nullable
                      as bool,
            placeholder: freezed == placeholder
                ? _value.placeholder
                : placeholder // ignore: cast_nullable_to_non_nullable
                      as String?,
            helpText: freezed == helpText
                ? _value.helpText
                : helpText // ignore: cast_nullable_to_non_nullable
                      as String?,
            unit: freezed == unit
                ? _value.unit
                : unit // ignore: cast_nullable_to_non_nullable
                      as String?,
            defaultValue: freezed == defaultValue
                ? _value.defaultValue
                : defaultValue,
            readOnly: null == readOnly
                ? _value.readOnly
                : readOnly // ignore: cast_nullable_to_non_nullable
                      as bool,
            options: freezed == options
                ? _value.options
                : options // ignore: cast_nullable_to_non_nullable
                      as List<Option>?,
            optionsFromAttribute: freezed == optionsFromAttribute
                ? _value.optionsFromAttribute
                : optionsFromAttribute // ignore: cast_nullable_to_non_nullable
                      as String?,
            validation: null == validation
                ? _value.validation
                : validation // ignore: cast_nullable_to_non_nullable
                      as List<ValidationRule>,
            visibleWhen: freezed == visibleWhen
                ? _value.visibleWhen
                : visibleWhen,
            config: freezed == config
                ? _value.config
                : config // ignore: cast_nullable_to_non_nullable
                      as Map<String, Object?>?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$FormFieldImplCopyWith<$Res>
    implements $FormFieldCopyWith<$Res> {
  factory _$$FormFieldImplCopyWith(
    _$FormFieldImpl value,
    $Res Function(_$FormFieldImpl) then,
  ) = __$$FormFieldImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String key,
    String label,
    @JsonKey(unknownEnumValue: FormControl.unsupported) FormControl control,
    @JsonKey(name: 'required') bool isRequired,
    String? placeholder,
    String? helpText,
    String? unit,
    Object? defaultValue,
    bool readOnly,
    List<Option>? options,
    String? optionsFromAttribute,
    List<ValidationRule> validation,
    Object? visibleWhen,
    Map<String, Object?>? config,
  });
}

/// @nodoc
class __$$FormFieldImplCopyWithImpl<$Res>
    extends _$FormFieldCopyWithImpl<$Res, _$FormFieldImpl>
    implements _$$FormFieldImplCopyWith<$Res> {
  __$$FormFieldImplCopyWithImpl(
    _$FormFieldImpl _value,
    $Res Function(_$FormFieldImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of FormField
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? key = null,
    Object? label = null,
    Object? control = null,
    Object? isRequired = null,
    Object? placeholder = freezed,
    Object? helpText = freezed,
    Object? unit = freezed,
    Object? defaultValue = freezed,
    Object? readOnly = null,
    Object? options = freezed,
    Object? optionsFromAttribute = freezed,
    Object? validation = null,
    Object? visibleWhen = freezed,
    Object? config = freezed,
  }) {
    return _then(
      _$FormFieldImpl(
        key: null == key
            ? _value.key
            : key // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        control: null == control
            ? _value.control
            : control // ignore: cast_nullable_to_non_nullable
                  as FormControl,
        isRequired: null == isRequired
            ? _value.isRequired
            : isRequired // ignore: cast_nullable_to_non_nullable
                  as bool,
        placeholder: freezed == placeholder
            ? _value.placeholder
            : placeholder // ignore: cast_nullable_to_non_nullable
                  as String?,
        helpText: freezed == helpText
            ? _value.helpText
            : helpText // ignore: cast_nullable_to_non_nullable
                  as String?,
        unit: freezed == unit
            ? _value.unit
            : unit // ignore: cast_nullable_to_non_nullable
                  as String?,
        defaultValue: freezed == defaultValue
            ? _value.defaultValue
            : defaultValue,
        readOnly: null == readOnly
            ? _value.readOnly
            : readOnly // ignore: cast_nullable_to_non_nullable
                  as bool,
        options: freezed == options
            ? _value._options
            : options // ignore: cast_nullable_to_non_nullable
                  as List<Option>?,
        optionsFromAttribute: freezed == optionsFromAttribute
            ? _value.optionsFromAttribute
            : optionsFromAttribute // ignore: cast_nullable_to_non_nullable
                  as String?,
        validation: null == validation
            ? _value._validation
            : validation // ignore: cast_nullable_to_non_nullable
                  as List<ValidationRule>,
        visibleWhen: freezed == visibleWhen ? _value.visibleWhen : visibleWhen,
        config: freezed == config
            ? _value._config
            : config // ignore: cast_nullable_to_non_nullable
                  as Map<String, Object?>?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$FormFieldImpl implements _FormField {
  const _$FormFieldImpl({
    required this.key,
    required this.label,
    @JsonKey(unknownEnumValue: FormControl.unsupported)
    this.control = FormControl.unsupported,
    @JsonKey(name: 'required') this.isRequired = false,
    this.placeholder,
    this.helpText,
    this.unit,
    this.defaultValue,
    this.readOnly = false,
    final List<Option>? options,
    this.optionsFromAttribute,
    final List<ValidationRule> validation = const <ValidationRule>[],
    this.visibleWhen,
    final Map<String, Object?>? config,
  }) : _options = options,
       _validation = validation,
       _config = config;

  factory _$FormFieldImpl.fromJson(Map<String, dynamic> json) =>
      _$$FormFieldImplFromJson(json);

  @override
  final String key;
  @override
  final String label;
  @override
  @JsonKey(unknownEnumValue: FormControl.unsupported)
  final FormControl control;
  @override
  @JsonKey(name: 'required')
  final bool isRequired;
  @override
  final String? placeholder;
  @override
  final String? helpText;

  /// `km`, `%`, `₹` — shown beside the input, never folded into the label.
  @override
  final String? unit;
  @override
  final Object? defaultValue;
  @override
  @JsonKey()
  final bool readOnly;

  /// Present for `select` / `multiselect` / `radio`. Absent when the options come from taxonomy.
  final List<Option>? _options;

  /// Present for `select` / `multiselect` / `radio`. Absent when the options come from taxonomy.
  @override
  List<Option>? get options {
    final value = _options;
    if (value == null) return null;
    if (_options is EqualUnmodifiableListView) return _options;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(value);
  }

  /// Attribute code to resolve options from the taxonomy registry instead of inlining them.
  @override
  final String? optionsFromAttribute;
  final List<ValidationRule> _validation;
  @override
  @JsonKey()
  List<ValidationRule> get validation {
    if (_validation is EqualUnmodifiableListView) return _validation;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_validation);
  }

  /// JsonLogic over the form's own values. Evaluate with `JsonLogic`, never with a hand-rolled
  /// check — `{"==": [{"var": "fuel_type"}, "ev"]}` is why adding EVs needed no code, and it has
  /// to mean the same thing here as it does on the web and on the server.
  @override
  final Object? visibleWhen;

  /// Control-specific settings, deliberately an open map so the registry can extend a control
  /// without a client release. Read it with the matching config type at the point of use.
  final Map<String, Object?>? _config;

  /// Control-specific settings, deliberately an open map so the registry can extend a control
  /// without a client release. Read it with the matching config type at the point of use.
  @override
  Map<String, Object?>? get config {
    final value = _config;
    if (value == null) return null;
    if (_config is EqualUnmodifiableMapView) return _config;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableMapView(value);
  }

  @override
  String toString() {
    return 'FormField(key: $key, label: $label, control: $control, isRequired: $isRequired, placeholder: $placeholder, helpText: $helpText, unit: $unit, defaultValue: $defaultValue, readOnly: $readOnly, options: $options, optionsFromAttribute: $optionsFromAttribute, validation: $validation, visibleWhen: $visibleWhen, config: $config)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$FormFieldImpl &&
            (identical(other.key, key) || other.key == key) &&
            (identical(other.label, label) || other.label == label) &&
            (identical(other.control, control) || other.control == control) &&
            (identical(other.isRequired, isRequired) ||
                other.isRequired == isRequired) &&
            (identical(other.placeholder, placeholder) ||
                other.placeholder == placeholder) &&
            (identical(other.helpText, helpText) ||
                other.helpText == helpText) &&
            (identical(other.unit, unit) || other.unit == unit) &&
            const DeepCollectionEquality().equals(
              other.defaultValue,
              defaultValue,
            ) &&
            (identical(other.readOnly, readOnly) ||
                other.readOnly == readOnly) &&
            const DeepCollectionEquality().equals(other._options, _options) &&
            (identical(other.optionsFromAttribute, optionsFromAttribute) ||
                other.optionsFromAttribute == optionsFromAttribute) &&
            const DeepCollectionEquality().equals(
              other._validation,
              _validation,
            ) &&
            const DeepCollectionEquality().equals(
              other.visibleWhen,
              visibleWhen,
            ) &&
            const DeepCollectionEquality().equals(other._config, _config));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    key,
    label,
    control,
    isRequired,
    placeholder,
    helpText,
    unit,
    const DeepCollectionEquality().hash(defaultValue),
    readOnly,
    const DeepCollectionEquality().hash(_options),
    optionsFromAttribute,
    const DeepCollectionEquality().hash(_validation),
    const DeepCollectionEquality().hash(visibleWhen),
    const DeepCollectionEquality().hash(_config),
  );

  /// Create a copy of FormField
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$FormFieldImplCopyWith<_$FormFieldImpl> get copyWith =>
      __$$FormFieldImplCopyWithImpl<_$FormFieldImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$FormFieldImplToJson(this);
  }
}

abstract class _FormField implements FormField {
  const factory _FormField({
    required final String key,
    required final String label,
    @JsonKey(unknownEnumValue: FormControl.unsupported)
    final FormControl control,
    @JsonKey(name: 'required') final bool isRequired,
    final String? placeholder,
    final String? helpText,
    final String? unit,
    final Object? defaultValue,
    final bool readOnly,
    final List<Option>? options,
    final String? optionsFromAttribute,
    final List<ValidationRule> validation,
    final Object? visibleWhen,
    final Map<String, Object?>? config,
  }) = _$FormFieldImpl;

  factory _FormField.fromJson(Map<String, dynamic> json) =
      _$FormFieldImpl.fromJson;

  @override
  String get key;
  @override
  String get label;
  @override
  @JsonKey(unknownEnumValue: FormControl.unsupported)
  FormControl get control;
  @override
  @JsonKey(name: 'required')
  bool get isRequired;
  @override
  String? get placeholder;
  @override
  String? get helpText;

  /// `km`, `%`, `₹` — shown beside the input, never folded into the label.
  @override
  String? get unit;
  @override
  Object? get defaultValue;
  @override
  bool get readOnly;

  /// Present for `select` / `multiselect` / `radio`. Absent when the options come from taxonomy.
  @override
  List<Option>? get options;

  /// Attribute code to resolve options from the taxonomy registry instead of inlining them.
  @override
  String? get optionsFromAttribute;
  @override
  List<ValidationRule> get validation;

  /// JsonLogic over the form's own values. Evaluate with `JsonLogic`, never with a hand-rolled
  /// check — `{"==": [{"var": "fuel_type"}, "ev"]}` is why adding EVs needed no code, and it has
  /// to mean the same thing here as it does on the web and on the server.
  @override
  Object? get visibleWhen;

  /// Control-specific settings, deliberately an open map so the registry can extend a control
  /// without a client release. Read it with the matching config type at the point of use.
  @override
  Map<String, Object?>? get config;

  /// Create a copy of FormField
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$FormFieldImplCopyWith<_$FormFieldImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

FormStep _$FormStepFromJson(Map<String, dynamic> json) {
  return _FormStep.fromJson(json);
}

/// @nodoc
mixin _$FormStep {
  String get key => throw _privateConstructorUsedError;
  String get title => throw _privateConstructorUsedError;
  String? get description => throw _privateConstructorUsedError;
  List<FormField> get fields => throw _privateConstructorUsedError;
  Object? get visibleWhen => throw _privateConstructorUsedError;

  /// Serializes this FormStep to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of FormStep
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $FormStepCopyWith<FormStep> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $FormStepCopyWith<$Res> {
  factory $FormStepCopyWith(FormStep value, $Res Function(FormStep) then) =
      _$FormStepCopyWithImpl<$Res, FormStep>;
  @useResult
  $Res call({
    String key,
    String title,
    String? description,
    List<FormField> fields,
    Object? visibleWhen,
  });
}

/// @nodoc
class _$FormStepCopyWithImpl<$Res, $Val extends FormStep>
    implements $FormStepCopyWith<$Res> {
  _$FormStepCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of FormStep
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? key = null,
    Object? title = null,
    Object? description = freezed,
    Object? fields = null,
    Object? visibleWhen = freezed,
  }) {
    return _then(
      _value.copyWith(
            key: null == key
                ? _value.key
                : key // ignore: cast_nullable_to_non_nullable
                      as String,
            title: null == title
                ? _value.title
                : title // ignore: cast_nullable_to_non_nullable
                      as String,
            description: freezed == description
                ? _value.description
                : description // ignore: cast_nullable_to_non_nullable
                      as String?,
            fields: null == fields
                ? _value.fields
                : fields // ignore: cast_nullable_to_non_nullable
                      as List<FormField>,
            visibleWhen: freezed == visibleWhen
                ? _value.visibleWhen
                : visibleWhen,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$FormStepImplCopyWith<$Res>
    implements $FormStepCopyWith<$Res> {
  factory _$$FormStepImplCopyWith(
    _$FormStepImpl value,
    $Res Function(_$FormStepImpl) then,
  ) = __$$FormStepImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String key,
    String title,
    String? description,
    List<FormField> fields,
    Object? visibleWhen,
  });
}

/// @nodoc
class __$$FormStepImplCopyWithImpl<$Res>
    extends _$FormStepCopyWithImpl<$Res, _$FormStepImpl>
    implements _$$FormStepImplCopyWith<$Res> {
  __$$FormStepImplCopyWithImpl(
    _$FormStepImpl _value,
    $Res Function(_$FormStepImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of FormStep
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? key = null,
    Object? title = null,
    Object? description = freezed,
    Object? fields = null,
    Object? visibleWhen = freezed,
  }) {
    return _then(
      _$FormStepImpl(
        key: null == key
            ? _value.key
            : key // ignore: cast_nullable_to_non_nullable
                  as String,
        title: null == title
            ? _value.title
            : title // ignore: cast_nullable_to_non_nullable
                  as String,
        description: freezed == description
            ? _value.description
            : description // ignore: cast_nullable_to_non_nullable
                  as String?,
        fields: null == fields
            ? _value._fields
            : fields // ignore: cast_nullable_to_non_nullable
                  as List<FormField>,
        visibleWhen: freezed == visibleWhen ? _value.visibleWhen : visibleWhen,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$FormStepImpl implements _FormStep {
  const _$FormStepImpl({
    required this.key,
    required this.title,
    this.description,
    final List<FormField> fields = const <FormField>[],
    this.visibleWhen,
  }) : _fields = fields;

  factory _$FormStepImpl.fromJson(Map<String, dynamic> json) =>
      _$$FormStepImplFromJson(json);

  @override
  final String key;
  @override
  final String title;
  @override
  final String? description;
  final List<FormField> _fields;
  @override
  @JsonKey()
  List<FormField> get fields {
    if (_fields is EqualUnmodifiableListView) return _fields;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_fields);
  }

  @override
  final Object? visibleWhen;

  @override
  String toString() {
    return 'FormStep(key: $key, title: $title, description: $description, fields: $fields, visibleWhen: $visibleWhen)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$FormStepImpl &&
            (identical(other.key, key) || other.key == key) &&
            (identical(other.title, title) || other.title == title) &&
            (identical(other.description, description) ||
                other.description == description) &&
            const DeepCollectionEquality().equals(other._fields, _fields) &&
            const DeepCollectionEquality().equals(
              other.visibleWhen,
              visibleWhen,
            ));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    key,
    title,
    description,
    const DeepCollectionEquality().hash(_fields),
    const DeepCollectionEquality().hash(visibleWhen),
  );

  /// Create a copy of FormStep
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$FormStepImplCopyWith<_$FormStepImpl> get copyWith =>
      __$$FormStepImplCopyWithImpl<_$FormStepImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$FormStepImplToJson(this);
  }
}

abstract class _FormStep implements FormStep {
  const factory _FormStep({
    required final String key,
    required final String title,
    final String? description,
    final List<FormField> fields,
    final Object? visibleWhen,
  }) = _$FormStepImpl;

  factory _FormStep.fromJson(Map<String, dynamic> json) =
      _$FormStepImpl.fromJson;

  @override
  String get key;
  @override
  String get title;
  @override
  String? get description;
  @override
  List<FormField> get fields;
  @override
  Object? get visibleWhen;

  /// Create a copy of FormStep
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$FormStepImplCopyWith<_$FormStepImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

FormDefinition _$FormDefinitionFromJson(Map<String, dynamic> json) {
  return _FormDefinition.fromJson(json);
}

/// @nodoc
mixin _$FormDefinition {
  String get key => throw _privateConstructorUsedError;
  int get version => throw _privateConstructorUsedError;
  String get title => throw _privateConstructorUsedError;
  String? get description => throw _privateConstructorUsedError;

  /// The action keeps its name through the flow: `Get my price` → `Getting your price`.
  String get submitLabel => throw _privateConstructorUsedError;
  List<FormStep> get steps => throw _privateConstructorUsedError;

  /// Serializes this FormDefinition to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of FormDefinition
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $FormDefinitionCopyWith<FormDefinition> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $FormDefinitionCopyWith<$Res> {
  factory $FormDefinitionCopyWith(
    FormDefinition value,
    $Res Function(FormDefinition) then,
  ) = _$FormDefinitionCopyWithImpl<$Res, FormDefinition>;
  @useResult
  $Res call({
    String key,
    int version,
    String title,
    String? description,
    String submitLabel,
    List<FormStep> steps,
  });
}

/// @nodoc
class _$FormDefinitionCopyWithImpl<$Res, $Val extends FormDefinition>
    implements $FormDefinitionCopyWith<$Res> {
  _$FormDefinitionCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of FormDefinition
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? key = null,
    Object? version = null,
    Object? title = null,
    Object? description = freezed,
    Object? submitLabel = null,
    Object? steps = null,
  }) {
    return _then(
      _value.copyWith(
            key: null == key
                ? _value.key
                : key // ignore: cast_nullable_to_non_nullable
                      as String,
            version: null == version
                ? _value.version
                : version // ignore: cast_nullable_to_non_nullable
                      as int,
            title: null == title
                ? _value.title
                : title // ignore: cast_nullable_to_non_nullable
                      as String,
            description: freezed == description
                ? _value.description
                : description // ignore: cast_nullable_to_non_nullable
                      as String?,
            submitLabel: null == submitLabel
                ? _value.submitLabel
                : submitLabel // ignore: cast_nullable_to_non_nullable
                      as String,
            steps: null == steps
                ? _value.steps
                : steps // ignore: cast_nullable_to_non_nullable
                      as List<FormStep>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$FormDefinitionImplCopyWith<$Res>
    implements $FormDefinitionCopyWith<$Res> {
  factory _$$FormDefinitionImplCopyWith(
    _$FormDefinitionImpl value,
    $Res Function(_$FormDefinitionImpl) then,
  ) = __$$FormDefinitionImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String key,
    int version,
    String title,
    String? description,
    String submitLabel,
    List<FormStep> steps,
  });
}

/// @nodoc
class __$$FormDefinitionImplCopyWithImpl<$Res>
    extends _$FormDefinitionCopyWithImpl<$Res, _$FormDefinitionImpl>
    implements _$$FormDefinitionImplCopyWith<$Res> {
  __$$FormDefinitionImplCopyWithImpl(
    _$FormDefinitionImpl _value,
    $Res Function(_$FormDefinitionImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of FormDefinition
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? key = null,
    Object? version = null,
    Object? title = null,
    Object? description = freezed,
    Object? submitLabel = null,
    Object? steps = null,
  }) {
    return _then(
      _$FormDefinitionImpl(
        key: null == key
            ? _value.key
            : key // ignore: cast_nullable_to_non_nullable
                  as String,
        version: null == version
            ? _value.version
            : version // ignore: cast_nullable_to_non_nullable
                  as int,
        title: null == title
            ? _value.title
            : title // ignore: cast_nullable_to_non_nullable
                  as String,
        description: freezed == description
            ? _value.description
            : description // ignore: cast_nullable_to_non_nullable
                  as String?,
        submitLabel: null == submitLabel
            ? _value.submitLabel
            : submitLabel // ignore: cast_nullable_to_non_nullable
                  as String,
        steps: null == steps
            ? _value._steps
            : steps // ignore: cast_nullable_to_non_nullable
                  as List<FormStep>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$FormDefinitionImpl implements _FormDefinition {
  const _$FormDefinitionImpl({
    required this.key,
    required this.version,
    required this.title,
    this.description,
    this.submitLabel = 'Continue',
    final List<FormStep> steps = const <FormStep>[],
  }) : _steps = steps;

  factory _$FormDefinitionImpl.fromJson(Map<String, dynamic> json) =>
      _$$FormDefinitionImplFromJson(json);

  @override
  final String key;
  @override
  final int version;
  @override
  final String title;
  @override
  final String? description;

  /// The action keeps its name through the flow: `Get my price` → `Getting your price`.
  @override
  @JsonKey()
  final String submitLabel;
  final List<FormStep> _steps;
  @override
  @JsonKey()
  List<FormStep> get steps {
    if (_steps is EqualUnmodifiableListView) return _steps;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_steps);
  }

  @override
  String toString() {
    return 'FormDefinition(key: $key, version: $version, title: $title, description: $description, submitLabel: $submitLabel, steps: $steps)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$FormDefinitionImpl &&
            (identical(other.key, key) || other.key == key) &&
            (identical(other.version, version) || other.version == version) &&
            (identical(other.title, title) || other.title == title) &&
            (identical(other.description, description) ||
                other.description == description) &&
            (identical(other.submitLabel, submitLabel) ||
                other.submitLabel == submitLabel) &&
            const DeepCollectionEquality().equals(other._steps, _steps));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    key,
    version,
    title,
    description,
    submitLabel,
    const DeepCollectionEquality().hash(_steps),
  );

  /// Create a copy of FormDefinition
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$FormDefinitionImplCopyWith<_$FormDefinitionImpl> get copyWith =>
      __$$FormDefinitionImplCopyWithImpl<_$FormDefinitionImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$FormDefinitionImplToJson(this);
  }
}

abstract class _FormDefinition implements FormDefinition {
  const factory _FormDefinition({
    required final String key,
    required final int version,
    required final String title,
    final String? description,
    final String submitLabel,
    final List<FormStep> steps,
  }) = _$FormDefinitionImpl;

  factory _FormDefinition.fromJson(Map<String, dynamic> json) =
      _$FormDefinitionImpl.fromJson;

  @override
  String get key;
  @override
  int get version;
  @override
  String get title;
  @override
  String? get description;

  /// The action keeps its name through the flow: `Get my price` → `Getting your price`.
  @override
  String get submitLabel;
  @override
  List<FormStep> get steps;

  /// Create a copy of FormDefinition
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$FormDefinitionImplCopyWith<_$FormDefinitionImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

FormValidateResponse _$FormValidateResponseFromJson(Map<String, dynamic> json) {
  return _FormValidateResponse.fromJson(json);
}

/// @nodoc
mixin _$FormValidateResponse {
  bool get valid => throw _privateConstructorUsedError;

  /// Serializes this FormValidateResponse to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of FormValidateResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $FormValidateResponseCopyWith<FormValidateResponse> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $FormValidateResponseCopyWith<$Res> {
  factory $FormValidateResponseCopyWith(
    FormValidateResponse value,
    $Res Function(FormValidateResponse) then,
  ) = _$FormValidateResponseCopyWithImpl<$Res, FormValidateResponse>;
  @useResult
  $Res call({bool valid});
}

/// @nodoc
class _$FormValidateResponseCopyWithImpl<
  $Res,
  $Val extends FormValidateResponse
>
    implements $FormValidateResponseCopyWith<$Res> {
  _$FormValidateResponseCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of FormValidateResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? valid = null}) {
    return _then(
      _value.copyWith(
            valid: null == valid
                ? _value.valid
                : valid // ignore: cast_nullable_to_non_nullable
                      as bool,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$FormValidateResponseImplCopyWith<$Res>
    implements $FormValidateResponseCopyWith<$Res> {
  factory _$$FormValidateResponseImplCopyWith(
    _$FormValidateResponseImpl value,
    $Res Function(_$FormValidateResponseImpl) then,
  ) = __$$FormValidateResponseImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({bool valid});
}

/// @nodoc
class __$$FormValidateResponseImplCopyWithImpl<$Res>
    extends _$FormValidateResponseCopyWithImpl<$Res, _$FormValidateResponseImpl>
    implements _$$FormValidateResponseImplCopyWith<$Res> {
  __$$FormValidateResponseImplCopyWithImpl(
    _$FormValidateResponseImpl _value,
    $Res Function(_$FormValidateResponseImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of FormValidateResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? valid = null}) {
    return _then(
      _$FormValidateResponseImpl(
        valid: null == valid
            ? _value.valid
            : valid // ignore: cast_nullable_to_non_nullable
                  as bool,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$FormValidateResponseImpl implements _FormValidateResponse {
  const _$FormValidateResponseImpl({required this.valid});

  factory _$FormValidateResponseImpl.fromJson(Map<String, dynamic> json) =>
      _$$FormValidateResponseImplFromJson(json);

  @override
  final bool valid;

  @override
  String toString() {
    return 'FormValidateResponse(valid: $valid)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$FormValidateResponseImpl &&
            (identical(other.valid, valid) || other.valid == valid));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, valid);

  /// Create a copy of FormValidateResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$FormValidateResponseImplCopyWith<_$FormValidateResponseImpl>
  get copyWith =>
      __$$FormValidateResponseImplCopyWithImpl<_$FormValidateResponseImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$FormValidateResponseImplToJson(this);
  }
}

abstract class _FormValidateResponse implements FormValidateResponse {
  const factory _FormValidateResponse({required final bool valid}) =
      _$FormValidateResponseImpl;

  factory _FormValidateResponse.fromJson(Map<String, dynamic> json) =
      _$FormValidateResponseImpl.fromJson;

  @override
  bool get valid;

  /// Create a copy of FormValidateResponse
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$FormValidateResponseImplCopyWith<_$FormValidateResponseImpl>
  get copyWith => throw _privateConstructorUsedError;
}

PhotoSetConfig _$PhotoSetConfigFromJson(Map<String, dynamic> json) {
  return _PhotoSetConfig.fromJson(json);
}

/// @nodoc
mixin _$PhotoSetConfig {
  int get min => throw _privateConstructorUsedError;
  int get max => throw _privateConstructorUsedError;

  /// "Front 3/4", "Odometer", "Tyres" — the shot list an inspector works through.
  List<String> get prompts => throw _privateConstructorUsedError;

  /// Serializes this PhotoSetConfig to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PhotoSetConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PhotoSetConfigCopyWith<PhotoSetConfig> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PhotoSetConfigCopyWith<$Res> {
  factory $PhotoSetConfigCopyWith(
    PhotoSetConfig value,
    $Res Function(PhotoSetConfig) then,
  ) = _$PhotoSetConfigCopyWithImpl<$Res, PhotoSetConfig>;
  @useResult
  $Res call({int min, int max, List<String> prompts});
}

/// @nodoc
class _$PhotoSetConfigCopyWithImpl<$Res, $Val extends PhotoSetConfig>
    implements $PhotoSetConfigCopyWith<$Res> {
  _$PhotoSetConfigCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PhotoSetConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? min = null, Object? max = null, Object? prompts = null}) {
    return _then(
      _value.copyWith(
            min: null == min
                ? _value.min
                : min // ignore: cast_nullable_to_non_nullable
                      as int,
            max: null == max
                ? _value.max
                : max // ignore: cast_nullable_to_non_nullable
                      as int,
            prompts: null == prompts
                ? _value.prompts
                : prompts // ignore: cast_nullable_to_non_nullable
                      as List<String>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$PhotoSetConfigImplCopyWith<$Res>
    implements $PhotoSetConfigCopyWith<$Res> {
  factory _$$PhotoSetConfigImplCopyWith(
    _$PhotoSetConfigImpl value,
    $Res Function(_$PhotoSetConfigImpl) then,
  ) = __$$PhotoSetConfigImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int min, int max, List<String> prompts});
}

/// @nodoc
class __$$PhotoSetConfigImplCopyWithImpl<$Res>
    extends _$PhotoSetConfigCopyWithImpl<$Res, _$PhotoSetConfigImpl>
    implements _$$PhotoSetConfigImplCopyWith<$Res> {
  __$$PhotoSetConfigImplCopyWithImpl(
    _$PhotoSetConfigImpl _value,
    $Res Function(_$PhotoSetConfigImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PhotoSetConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? min = null, Object? max = null, Object? prompts = null}) {
    return _then(
      _$PhotoSetConfigImpl(
        min: null == min
            ? _value.min
            : min // ignore: cast_nullable_to_non_nullable
                  as int,
        max: null == max
            ? _value.max
            : max // ignore: cast_nullable_to_non_nullable
                  as int,
        prompts: null == prompts
            ? _value._prompts
            : prompts // ignore: cast_nullable_to_non_nullable
                  as List<String>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PhotoSetConfigImpl implements _PhotoSetConfig {
  const _$PhotoSetConfigImpl({
    this.min = 1,
    this.max = 20,
    final List<String> prompts = const <String>[],
  }) : _prompts = prompts;

  factory _$PhotoSetConfigImpl.fromJson(Map<String, dynamic> json) =>
      _$$PhotoSetConfigImplFromJson(json);

  @override
  @JsonKey()
  final int min;
  @override
  @JsonKey()
  final int max;

  /// "Front 3/4", "Odometer", "Tyres" — the shot list an inspector works through.
  final List<String> _prompts;

  /// "Front 3/4", "Odometer", "Tyres" — the shot list an inspector works through.
  @override
  @JsonKey()
  List<String> get prompts {
    if (_prompts is EqualUnmodifiableListView) return _prompts;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_prompts);
  }

  @override
  String toString() {
    return 'PhotoSetConfig(min: $min, max: $max, prompts: $prompts)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PhotoSetConfigImpl &&
            (identical(other.min, min) || other.min == min) &&
            (identical(other.max, max) || other.max == max) &&
            const DeepCollectionEquality().equals(other._prompts, _prompts));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    min,
    max,
    const DeepCollectionEquality().hash(_prompts),
  );

  /// Create a copy of PhotoSetConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PhotoSetConfigImplCopyWith<_$PhotoSetConfigImpl> get copyWith =>
      __$$PhotoSetConfigImplCopyWithImpl<_$PhotoSetConfigImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$PhotoSetConfigImplToJson(this);
  }
}

abstract class _PhotoSetConfig implements PhotoSetConfig {
  const factory _PhotoSetConfig({
    final int min,
    final int max,
    final List<String> prompts,
  }) = _$PhotoSetConfigImpl;

  factory _PhotoSetConfig.fromJson(Map<String, dynamic> json) =
      _$PhotoSetConfigImpl.fromJson;

  @override
  int get min;
  @override
  int get max;

  /// "Front 3/4", "Odometer", "Tyres" — the shot list an inspector works through.
  @override
  List<String> get prompts;

  /// Create a copy of PhotoSetConfig
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PhotoSetConfigImplCopyWith<_$PhotoSetConfigImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

SliderConfig _$SliderConfigFromJson(Map<String, dynamic> json) {
  return _SliderConfig.fromJson(json);
}

/// @nodoc
mixin _$SliderConfig {
  double get min => throw _privateConstructorUsedError;
  double get max => throw _privateConstructorUsedError;
  double get step => throw _privateConstructorUsedError;

  /// Serializes this SliderConfig to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of SliderConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $SliderConfigCopyWith<SliderConfig> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $SliderConfigCopyWith<$Res> {
  factory $SliderConfigCopyWith(
    SliderConfig value,
    $Res Function(SliderConfig) then,
  ) = _$SliderConfigCopyWithImpl<$Res, SliderConfig>;
  @useResult
  $Res call({double min, double max, double step});
}

/// @nodoc
class _$SliderConfigCopyWithImpl<$Res, $Val extends SliderConfig>
    implements $SliderConfigCopyWith<$Res> {
  _$SliderConfigCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of SliderConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? min = null, Object? max = null, Object? step = null}) {
    return _then(
      _value.copyWith(
            min: null == min
                ? _value.min
                : min // ignore: cast_nullable_to_non_nullable
                      as double,
            max: null == max
                ? _value.max
                : max // ignore: cast_nullable_to_non_nullable
                      as double,
            step: null == step
                ? _value.step
                : step // ignore: cast_nullable_to_non_nullable
                      as double,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$SliderConfigImplCopyWith<$Res>
    implements $SliderConfigCopyWith<$Res> {
  factory _$$SliderConfigImplCopyWith(
    _$SliderConfigImpl value,
    $Res Function(_$SliderConfigImpl) then,
  ) = __$$SliderConfigImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({double min, double max, double step});
}

/// @nodoc
class __$$SliderConfigImplCopyWithImpl<$Res>
    extends _$SliderConfigCopyWithImpl<$Res, _$SliderConfigImpl>
    implements _$$SliderConfigImplCopyWith<$Res> {
  __$$SliderConfigImplCopyWithImpl(
    _$SliderConfigImpl _value,
    $Res Function(_$SliderConfigImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SliderConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? min = null, Object? max = null, Object? step = null}) {
    return _then(
      _$SliderConfigImpl(
        min: null == min
            ? _value.min
            : min // ignore: cast_nullable_to_non_nullable
                  as double,
        max: null == max
            ? _value.max
            : max // ignore: cast_nullable_to_non_nullable
                  as double,
        step: null == step
            ? _value.step
            : step // ignore: cast_nullable_to_non_nullable
                  as double,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SliderConfigImpl implements _SliderConfig {
  const _$SliderConfigImpl({this.min = 0, this.max = 100, this.step = 1});

  factory _$SliderConfigImpl.fromJson(Map<String, dynamic> json) =>
      _$$SliderConfigImplFromJson(json);

  @override
  @JsonKey()
  final double min;
  @override
  @JsonKey()
  final double max;
  @override
  @JsonKey()
  final double step;

  @override
  String toString() {
    return 'SliderConfig(min: $min, max: $max, step: $step)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SliderConfigImpl &&
            (identical(other.min, min) || other.min == min) &&
            (identical(other.max, max) || other.max == max) &&
            (identical(other.step, step) || other.step == step));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, min, max, step);

  /// Create a copy of SliderConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SliderConfigImplCopyWith<_$SliderConfigImpl> get copyWith =>
      __$$SliderConfigImplCopyWithImpl<_$SliderConfigImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$SliderConfigImplToJson(this);
  }
}

abstract class _SliderConfig implements SliderConfig {
  const factory _SliderConfig({
    final double min,
    final double max,
    final double step,
  }) = _$SliderConfigImpl;

  factory _SliderConfig.fromJson(Map<String, dynamic> json) =
      _$SliderConfigImpl.fromJson;

  @override
  double get min;
  @override
  double get max;
  @override
  double get step;

  /// Create a copy of SliderConfig
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SliderConfigImplCopyWith<_$SliderConfigImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

OtpConfig _$OtpConfigFromJson(Map<String, dynamic> json) {
  return _OtpConfig.fromJson(json);
}

/// @nodoc
mixin _$OtpConfig {
  int get length => throw _privateConstructorUsedError;
  int get resendAfterSeconds => throw _privateConstructorUsedError;

  /// Serializes this OtpConfig to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of OtpConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $OtpConfigCopyWith<OtpConfig> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $OtpConfigCopyWith<$Res> {
  factory $OtpConfigCopyWith(OtpConfig value, $Res Function(OtpConfig) then) =
      _$OtpConfigCopyWithImpl<$Res, OtpConfig>;
  @useResult
  $Res call({int length, int resendAfterSeconds});
}

/// @nodoc
class _$OtpConfigCopyWithImpl<$Res, $Val extends OtpConfig>
    implements $OtpConfigCopyWith<$Res> {
  _$OtpConfigCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of OtpConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? length = null, Object? resendAfterSeconds = null}) {
    return _then(
      _value.copyWith(
            length: null == length
                ? _value.length
                : length // ignore: cast_nullable_to_non_nullable
                      as int,
            resendAfterSeconds: null == resendAfterSeconds
                ? _value.resendAfterSeconds
                : resendAfterSeconds // ignore: cast_nullable_to_non_nullable
                      as int,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$OtpConfigImplCopyWith<$Res>
    implements $OtpConfigCopyWith<$Res> {
  factory _$$OtpConfigImplCopyWith(
    _$OtpConfigImpl value,
    $Res Function(_$OtpConfigImpl) then,
  ) = __$$OtpConfigImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int length, int resendAfterSeconds});
}

/// @nodoc
class __$$OtpConfigImplCopyWithImpl<$Res>
    extends _$OtpConfigCopyWithImpl<$Res, _$OtpConfigImpl>
    implements _$$OtpConfigImplCopyWith<$Res> {
  __$$OtpConfigImplCopyWithImpl(
    _$OtpConfigImpl _value,
    $Res Function(_$OtpConfigImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of OtpConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? length = null, Object? resendAfterSeconds = null}) {
    return _then(
      _$OtpConfigImpl(
        length: null == length
            ? _value.length
            : length // ignore: cast_nullable_to_non_nullable
                  as int,
        resendAfterSeconds: null == resendAfterSeconds
            ? _value.resendAfterSeconds
            : resendAfterSeconds // ignore: cast_nullable_to_non_nullable
                  as int,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$OtpConfigImpl implements _OtpConfig {
  const _$OtpConfigImpl({this.length = 6, this.resendAfterSeconds = 30});

  factory _$OtpConfigImpl.fromJson(Map<String, dynamic> json) =>
      _$$OtpConfigImplFromJson(json);

  @override
  @JsonKey()
  final int length;
  @override
  @JsonKey()
  final int resendAfterSeconds;

  @override
  String toString() {
    return 'OtpConfig(length: $length, resendAfterSeconds: $resendAfterSeconds)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$OtpConfigImpl &&
            (identical(other.length, length) || other.length == length) &&
            (identical(other.resendAfterSeconds, resendAfterSeconds) ||
                other.resendAfterSeconds == resendAfterSeconds));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, length, resendAfterSeconds);

  /// Create a copy of OtpConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$OtpConfigImplCopyWith<_$OtpConfigImpl> get copyWith =>
      __$$OtpConfigImplCopyWithImpl<_$OtpConfigImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$OtpConfigImplToJson(this);
  }
}

abstract class _OtpConfig implements OtpConfig {
  const factory _OtpConfig({final int length, final int resendAfterSeconds}) =
      _$OtpConfigImpl;

  factory _OtpConfig.fromJson(Map<String, dynamic> json) =
      _$OtpConfigImpl.fromJson;

  @override
  int get length;
  @override
  int get resendAfterSeconds;

  /// Create a copy of OtpConfig
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$OtpConfigImplCopyWith<_$OtpConfigImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

FileConfig _$FileConfigFromJson(Map<String, dynamic> json) {
  return _FileConfig.fromJson(json);
}

/// @nodoc
mixin _$FileConfig {
  List<String> get accept => throw _privateConstructorUsedError;
  double get maxSizeMb => throw _privateConstructorUsedError;
  bool get multiple => throw _privateConstructorUsedError;

  /// Serializes this FileConfig to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of FileConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $FileConfigCopyWith<FileConfig> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $FileConfigCopyWith<$Res> {
  factory $FileConfigCopyWith(
    FileConfig value,
    $Res Function(FileConfig) then,
  ) = _$FileConfigCopyWithImpl<$Res, FileConfig>;
  @useResult
  $Res call({List<String> accept, double maxSizeMb, bool multiple});
}

/// @nodoc
class _$FileConfigCopyWithImpl<$Res, $Val extends FileConfig>
    implements $FileConfigCopyWith<$Res> {
  _$FileConfigCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of FileConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? accept = null,
    Object? maxSizeMb = null,
    Object? multiple = null,
  }) {
    return _then(
      _value.copyWith(
            accept: null == accept
                ? _value.accept
                : accept // ignore: cast_nullable_to_non_nullable
                      as List<String>,
            maxSizeMb: null == maxSizeMb
                ? _value.maxSizeMb
                : maxSizeMb // ignore: cast_nullable_to_non_nullable
                      as double,
            multiple: null == multiple
                ? _value.multiple
                : multiple // ignore: cast_nullable_to_non_nullable
                      as bool,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$FileConfigImplCopyWith<$Res>
    implements $FileConfigCopyWith<$Res> {
  factory _$$FileConfigImplCopyWith(
    _$FileConfigImpl value,
    $Res Function(_$FileConfigImpl) then,
  ) = __$$FileConfigImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({List<String> accept, double maxSizeMb, bool multiple});
}

/// @nodoc
class __$$FileConfigImplCopyWithImpl<$Res>
    extends _$FileConfigCopyWithImpl<$Res, _$FileConfigImpl>
    implements _$$FileConfigImplCopyWith<$Res> {
  __$$FileConfigImplCopyWithImpl(
    _$FileConfigImpl _value,
    $Res Function(_$FileConfigImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of FileConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? accept = null,
    Object? maxSizeMb = null,
    Object? multiple = null,
  }) {
    return _then(
      _$FileConfigImpl(
        accept: null == accept
            ? _value._accept
            : accept // ignore: cast_nullable_to_non_nullable
                  as List<String>,
        maxSizeMb: null == maxSizeMb
            ? _value.maxSizeMb
            : maxSizeMb // ignore: cast_nullable_to_non_nullable
                  as double,
        multiple: null == multiple
            ? _value.multiple
            : multiple // ignore: cast_nullable_to_non_nullable
                  as bool,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$FileConfigImpl implements _FileConfig {
  const _$FileConfigImpl({
    final List<String> accept = const <String>[],
    this.maxSizeMb = 10,
    this.multiple = false,
  }) : _accept = accept;

  factory _$FileConfigImpl.fromJson(Map<String, dynamic> json) =>
      _$$FileConfigImplFromJson(json);

  final List<String> _accept;
  @override
  @JsonKey()
  List<String> get accept {
    if (_accept is EqualUnmodifiableListView) return _accept;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_accept);
  }

  @override
  @JsonKey()
  final double maxSizeMb;
  @override
  @JsonKey()
  final bool multiple;

  @override
  String toString() {
    return 'FileConfig(accept: $accept, maxSizeMb: $maxSizeMb, multiple: $multiple)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$FileConfigImpl &&
            const DeepCollectionEquality().equals(other._accept, _accept) &&
            (identical(other.maxSizeMb, maxSizeMb) ||
                other.maxSizeMb == maxSizeMb) &&
            (identical(other.multiple, multiple) ||
                other.multiple == multiple));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    const DeepCollectionEquality().hash(_accept),
    maxSizeMb,
    multiple,
  );

  /// Create a copy of FileConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$FileConfigImplCopyWith<_$FileConfigImpl> get copyWith =>
      __$$FileConfigImplCopyWithImpl<_$FileConfigImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$FileConfigImplToJson(this);
  }
}

abstract class _FileConfig implements FileConfig {
  const factory _FileConfig({
    final List<String> accept,
    final double maxSizeMb,
    final bool multiple,
  }) = _$FileConfigImpl;

  factory _FileConfig.fromJson(Map<String, dynamic> json) =
      _$FileConfigImpl.fromJson;

  @override
  List<String> get accept;
  @override
  double get maxSizeMb;
  @override
  bool get multiple;

  /// Create a copy of FileConfig
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$FileConfigImplCopyWith<_$FileConfigImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

MoneyConfig _$MoneyConfigFromJson(Map<String, dynamic> json) {
  return _MoneyConfig.fromJson(json);
}

/// @nodoc
mixin _$MoneyConfig {
  String get currency => throw _privateConstructorUsedError;
  int? get minMinor => throw _privateConstructorUsedError;
  int? get maxMinor => throw _privateConstructorUsedError;

  /// Serializes this MoneyConfig to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of MoneyConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $MoneyConfigCopyWith<MoneyConfig> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $MoneyConfigCopyWith<$Res> {
  factory $MoneyConfigCopyWith(
    MoneyConfig value,
    $Res Function(MoneyConfig) then,
  ) = _$MoneyConfigCopyWithImpl<$Res, MoneyConfig>;
  @useResult
  $Res call({String currency, int? minMinor, int? maxMinor});
}

/// @nodoc
class _$MoneyConfigCopyWithImpl<$Res, $Val extends MoneyConfig>
    implements $MoneyConfigCopyWith<$Res> {
  _$MoneyConfigCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of MoneyConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? currency = null,
    Object? minMinor = freezed,
    Object? maxMinor = freezed,
  }) {
    return _then(
      _value.copyWith(
            currency: null == currency
                ? _value.currency
                : currency // ignore: cast_nullable_to_non_nullable
                      as String,
            minMinor: freezed == minMinor
                ? _value.minMinor
                : minMinor // ignore: cast_nullable_to_non_nullable
                      as int?,
            maxMinor: freezed == maxMinor
                ? _value.maxMinor
                : maxMinor // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$MoneyConfigImplCopyWith<$Res>
    implements $MoneyConfigCopyWith<$Res> {
  factory _$$MoneyConfigImplCopyWith(
    _$MoneyConfigImpl value,
    $Res Function(_$MoneyConfigImpl) then,
  ) = __$$MoneyConfigImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String currency, int? minMinor, int? maxMinor});
}

/// @nodoc
class __$$MoneyConfigImplCopyWithImpl<$Res>
    extends _$MoneyConfigCopyWithImpl<$Res, _$MoneyConfigImpl>
    implements _$$MoneyConfigImplCopyWith<$Res> {
  __$$MoneyConfigImplCopyWithImpl(
    _$MoneyConfigImpl _value,
    $Res Function(_$MoneyConfigImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of MoneyConfig
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? currency = null,
    Object? minMinor = freezed,
    Object? maxMinor = freezed,
  }) {
    return _then(
      _$MoneyConfigImpl(
        currency: null == currency
            ? _value.currency
            : currency // ignore: cast_nullable_to_non_nullable
                  as String,
        minMinor: freezed == minMinor
            ? _value.minMinor
            : minMinor // ignore: cast_nullable_to_non_nullable
                  as int?,
        maxMinor: freezed == maxMinor
            ? _value.maxMinor
            : maxMinor // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$MoneyConfigImpl implements _MoneyConfig {
  const _$MoneyConfigImpl({
    this.currency = 'INR',
    this.minMinor,
    this.maxMinor,
  });

  factory _$MoneyConfigImpl.fromJson(Map<String, dynamic> json) =>
      _$$MoneyConfigImplFromJson(json);

  @override
  @JsonKey()
  final String currency;
  @override
  final int? minMinor;
  @override
  final int? maxMinor;

  @override
  String toString() {
    return 'MoneyConfig(currency: $currency, minMinor: $minMinor, maxMinor: $maxMinor)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$MoneyConfigImpl &&
            (identical(other.currency, currency) ||
                other.currency == currency) &&
            (identical(other.minMinor, minMinor) ||
                other.minMinor == minMinor) &&
            (identical(other.maxMinor, maxMinor) ||
                other.maxMinor == maxMinor));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, currency, minMinor, maxMinor);

  /// Create a copy of MoneyConfig
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$MoneyConfigImplCopyWith<_$MoneyConfigImpl> get copyWith =>
      __$$MoneyConfigImplCopyWithImpl<_$MoneyConfigImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$MoneyConfigImplToJson(this);
  }
}

abstract class _MoneyConfig implements MoneyConfig {
  const factory _MoneyConfig({
    final String currency,
    final int? minMinor,
    final int? maxMinor,
  }) = _$MoneyConfigImpl;

  factory _MoneyConfig.fromJson(Map<String, dynamic> json) =
      _$MoneyConfigImpl.fromJson;

  @override
  String get currency;
  @override
  int? get minMinor;
  @override
  int? get maxMinor;

  /// Create a copy of MoneyConfig
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$MoneyConfigImplCopyWith<_$MoneyConfigImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
