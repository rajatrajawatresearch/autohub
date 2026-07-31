// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'problem.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

ProblemFieldError _$ProblemFieldErrorFromJson(Map<String, dynamic> json) {
  return _ProblemFieldError.fromJson(json);
}

/// @nodoc
mixin _$ProblemFieldError {
  String get code => throw _privateConstructorUsedError;

  /// `/kms_driven`, or `/attributes/boom_length_m` for a dynamic attribute.
  String? get pointer => throw _privateConstructorUsedError;
  String? get message => throw _privateConstructorUsedError;
  String? get messageKey => throw _privateConstructorUsedError;
  Map<String, Object?>? get params => throw _privateConstructorUsedError;

  /// Serializes this ProblemFieldError to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ProblemFieldError
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ProblemFieldErrorCopyWith<ProblemFieldError> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ProblemFieldErrorCopyWith<$Res> {
  factory $ProblemFieldErrorCopyWith(
    ProblemFieldError value,
    $Res Function(ProblemFieldError) then,
  ) = _$ProblemFieldErrorCopyWithImpl<$Res, ProblemFieldError>;
  @useResult
  $Res call({
    String code,
    String? pointer,
    String? message,
    String? messageKey,
    Map<String, Object?>? params,
  });
}

/// @nodoc
class _$ProblemFieldErrorCopyWithImpl<$Res, $Val extends ProblemFieldError>
    implements $ProblemFieldErrorCopyWith<$Res> {
  _$ProblemFieldErrorCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ProblemFieldError
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? pointer = freezed,
    Object? message = freezed,
    Object? messageKey = freezed,
    Object? params = freezed,
  }) {
    return _then(
      _value.copyWith(
            code: null == code
                ? _value.code
                : code // ignore: cast_nullable_to_non_nullable
                      as String,
            pointer: freezed == pointer
                ? _value.pointer
                : pointer // ignore: cast_nullable_to_non_nullable
                      as String?,
            message: freezed == message
                ? _value.message
                : message // ignore: cast_nullable_to_non_nullable
                      as String?,
            messageKey: freezed == messageKey
                ? _value.messageKey
                : messageKey // ignore: cast_nullable_to_non_nullable
                      as String?,
            params: freezed == params
                ? _value.params
                : params // ignore: cast_nullable_to_non_nullable
                      as Map<String, Object?>?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$ProblemFieldErrorImplCopyWith<$Res>
    implements $ProblemFieldErrorCopyWith<$Res> {
  factory _$$ProblemFieldErrorImplCopyWith(
    _$ProblemFieldErrorImpl value,
    $Res Function(_$ProblemFieldErrorImpl) then,
  ) = __$$ProblemFieldErrorImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String code,
    String? pointer,
    String? message,
    String? messageKey,
    Map<String, Object?>? params,
  });
}

/// @nodoc
class __$$ProblemFieldErrorImplCopyWithImpl<$Res>
    extends _$ProblemFieldErrorCopyWithImpl<$Res, _$ProblemFieldErrorImpl>
    implements _$$ProblemFieldErrorImplCopyWith<$Res> {
  __$$ProblemFieldErrorImplCopyWithImpl(
    _$ProblemFieldErrorImpl _value,
    $Res Function(_$ProblemFieldErrorImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ProblemFieldError
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? pointer = freezed,
    Object? message = freezed,
    Object? messageKey = freezed,
    Object? params = freezed,
  }) {
    return _then(
      _$ProblemFieldErrorImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        pointer: freezed == pointer
            ? _value.pointer
            : pointer // ignore: cast_nullable_to_non_nullable
                  as String?,
        message: freezed == message
            ? _value.message
            : message // ignore: cast_nullable_to_non_nullable
                  as String?,
        messageKey: freezed == messageKey
            ? _value.messageKey
            : messageKey // ignore: cast_nullable_to_non_nullable
                  as String?,
        params: freezed == params
            ? _value._params
            : params // ignore: cast_nullable_to_non_nullable
                  as Map<String, Object?>?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ProblemFieldErrorImpl implements _ProblemFieldError {
  const _$ProblemFieldErrorImpl({
    required this.code,
    this.pointer,
    this.message,
    this.messageKey,
    final Map<String, Object?>? params,
  }) : _params = params;

  factory _$ProblemFieldErrorImpl.fromJson(Map<String, dynamic> json) =>
      _$$ProblemFieldErrorImplFromJson(json);

  @override
  final String code;

  /// `/kms_driven`, or `/attributes/boom_length_m` for a dynamic attribute.
  @override
  final String? pointer;
  @override
  final String? message;
  @override
  final String? messageKey;
  final Map<String, Object?>? _params;
  @override
  Map<String, Object?>? get params {
    final value = _params;
    if (value == null) return null;
    if (_params is EqualUnmodifiableMapView) return _params;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableMapView(value);
  }

  @override
  String toString() {
    return 'ProblemFieldError(code: $code, pointer: $pointer, message: $message, messageKey: $messageKey, params: $params)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ProblemFieldErrorImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.pointer, pointer) || other.pointer == pointer) &&
            (identical(other.message, message) || other.message == message) &&
            (identical(other.messageKey, messageKey) ||
                other.messageKey == messageKey) &&
            const DeepCollectionEquality().equals(other._params, _params));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    pointer,
    message,
    messageKey,
    const DeepCollectionEquality().hash(_params),
  );

  /// Create a copy of ProblemFieldError
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ProblemFieldErrorImplCopyWith<_$ProblemFieldErrorImpl> get copyWith =>
      __$$ProblemFieldErrorImplCopyWithImpl<_$ProblemFieldErrorImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$ProblemFieldErrorImplToJson(this);
  }
}

abstract class _ProblemFieldError implements ProblemFieldError {
  const factory _ProblemFieldError({
    required final String code,
    final String? pointer,
    final String? message,
    final String? messageKey,
    final Map<String, Object?>? params,
  }) = _$ProblemFieldErrorImpl;

  factory _ProblemFieldError.fromJson(Map<String, dynamic> json) =
      _$ProblemFieldErrorImpl.fromJson;

  @override
  String get code;

  /// `/kms_driven`, or `/attributes/boom_length_m` for a dynamic attribute.
  @override
  String? get pointer;
  @override
  String? get message;
  @override
  String? get messageKey;
  @override
  Map<String, Object?>? get params;

  /// Create a copy of ProblemFieldError
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ProblemFieldErrorImplCopyWith<_$ProblemFieldErrorImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

ProblemDetails _$ProblemDetailsFromJson(Map<String, dynamic> json) {
  return _ProblemDetails.fromJson(json);
}

/// @nodoc
mixin _$ProblemDetails {
  String? get type => throw _privateConstructorUsedError;
  String? get title => throw _privateConstructorUsedError;
  int? get status => throw _privateConstructorUsedError;

  /// Stable machine code, `{module}.{resource}.{condition}`.
  String? get code => throw _privateConstructorUsedError;
  String? get detail => throw _privateConstructorUsedError;
  String? get instance => throw _privateConstructorUsedError;
  String? get traceId => throw _privateConstructorUsedError;
  String? get requestId => throw _privateConstructorUsedError;
  String? get timestamp => throw _privateConstructorUsedError;

  /// Present on 400/403/409/422. The schema form attaches each entry to a field by [pointer].
  List<ProblemFieldError>? get errors => throw _privateConstructorUsedError;

  /// Whatever the condition needs the client to know: `currentState` on a workflow conflict,
  /// `fieldErrors` on a form validation failure.
  Map<String, Object?>? get metadata => throw _privateConstructorUsedError;
  bool? get retryable => throw _privateConstructorUsedError;
  int? get retryAfterSeconds => throw _privateConstructorUsedError;
  String? get helpUrl => throw _privateConstructorUsedError;
  String? get supportReference => throw _privateConstructorUsedError;

  /// Serializes this ProblemDetails to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ProblemDetails
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ProblemDetailsCopyWith<ProblemDetails> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ProblemDetailsCopyWith<$Res> {
  factory $ProblemDetailsCopyWith(
    ProblemDetails value,
    $Res Function(ProblemDetails) then,
  ) = _$ProblemDetailsCopyWithImpl<$Res, ProblemDetails>;
  @useResult
  $Res call({
    String? type,
    String? title,
    int? status,
    String? code,
    String? detail,
    String? instance,
    String? traceId,
    String? requestId,
    String? timestamp,
    List<ProblemFieldError>? errors,
    Map<String, Object?>? metadata,
    bool? retryable,
    int? retryAfterSeconds,
    String? helpUrl,
    String? supportReference,
  });
}

/// @nodoc
class _$ProblemDetailsCopyWithImpl<$Res, $Val extends ProblemDetails>
    implements $ProblemDetailsCopyWith<$Res> {
  _$ProblemDetailsCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ProblemDetails
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? type = freezed,
    Object? title = freezed,
    Object? status = freezed,
    Object? code = freezed,
    Object? detail = freezed,
    Object? instance = freezed,
    Object? traceId = freezed,
    Object? requestId = freezed,
    Object? timestamp = freezed,
    Object? errors = freezed,
    Object? metadata = freezed,
    Object? retryable = freezed,
    Object? retryAfterSeconds = freezed,
    Object? helpUrl = freezed,
    Object? supportReference = freezed,
  }) {
    return _then(
      _value.copyWith(
            type: freezed == type
                ? _value.type
                : type // ignore: cast_nullable_to_non_nullable
                      as String?,
            title: freezed == title
                ? _value.title
                : title // ignore: cast_nullable_to_non_nullable
                      as String?,
            status: freezed == status
                ? _value.status
                : status // ignore: cast_nullable_to_non_nullable
                      as int?,
            code: freezed == code
                ? _value.code
                : code // ignore: cast_nullable_to_non_nullable
                      as String?,
            detail: freezed == detail
                ? _value.detail
                : detail // ignore: cast_nullable_to_non_nullable
                      as String?,
            instance: freezed == instance
                ? _value.instance
                : instance // ignore: cast_nullable_to_non_nullable
                      as String?,
            traceId: freezed == traceId
                ? _value.traceId
                : traceId // ignore: cast_nullable_to_non_nullable
                      as String?,
            requestId: freezed == requestId
                ? _value.requestId
                : requestId // ignore: cast_nullable_to_non_nullable
                      as String?,
            timestamp: freezed == timestamp
                ? _value.timestamp
                : timestamp // ignore: cast_nullable_to_non_nullable
                      as String?,
            errors: freezed == errors
                ? _value.errors
                : errors // ignore: cast_nullable_to_non_nullable
                      as List<ProblemFieldError>?,
            metadata: freezed == metadata
                ? _value.metadata
                : metadata // ignore: cast_nullable_to_non_nullable
                      as Map<String, Object?>?,
            retryable: freezed == retryable
                ? _value.retryable
                : retryable // ignore: cast_nullable_to_non_nullable
                      as bool?,
            retryAfterSeconds: freezed == retryAfterSeconds
                ? _value.retryAfterSeconds
                : retryAfterSeconds // ignore: cast_nullable_to_non_nullable
                      as int?,
            helpUrl: freezed == helpUrl
                ? _value.helpUrl
                : helpUrl // ignore: cast_nullable_to_non_nullable
                      as String?,
            supportReference: freezed == supportReference
                ? _value.supportReference
                : supportReference // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$ProblemDetailsImplCopyWith<$Res>
    implements $ProblemDetailsCopyWith<$Res> {
  factory _$$ProblemDetailsImplCopyWith(
    _$ProblemDetailsImpl value,
    $Res Function(_$ProblemDetailsImpl) then,
  ) = __$$ProblemDetailsImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String? type,
    String? title,
    int? status,
    String? code,
    String? detail,
    String? instance,
    String? traceId,
    String? requestId,
    String? timestamp,
    List<ProblemFieldError>? errors,
    Map<String, Object?>? metadata,
    bool? retryable,
    int? retryAfterSeconds,
    String? helpUrl,
    String? supportReference,
  });
}

/// @nodoc
class __$$ProblemDetailsImplCopyWithImpl<$Res>
    extends _$ProblemDetailsCopyWithImpl<$Res, _$ProblemDetailsImpl>
    implements _$$ProblemDetailsImplCopyWith<$Res> {
  __$$ProblemDetailsImplCopyWithImpl(
    _$ProblemDetailsImpl _value,
    $Res Function(_$ProblemDetailsImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ProblemDetails
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? type = freezed,
    Object? title = freezed,
    Object? status = freezed,
    Object? code = freezed,
    Object? detail = freezed,
    Object? instance = freezed,
    Object? traceId = freezed,
    Object? requestId = freezed,
    Object? timestamp = freezed,
    Object? errors = freezed,
    Object? metadata = freezed,
    Object? retryable = freezed,
    Object? retryAfterSeconds = freezed,
    Object? helpUrl = freezed,
    Object? supportReference = freezed,
  }) {
    return _then(
      _$ProblemDetailsImpl(
        type: freezed == type
            ? _value.type
            : type // ignore: cast_nullable_to_non_nullable
                  as String?,
        title: freezed == title
            ? _value.title
            : title // ignore: cast_nullable_to_non_nullable
                  as String?,
        status: freezed == status
            ? _value.status
            : status // ignore: cast_nullable_to_non_nullable
                  as int?,
        code: freezed == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String?,
        detail: freezed == detail
            ? _value.detail
            : detail // ignore: cast_nullable_to_non_nullable
                  as String?,
        instance: freezed == instance
            ? _value.instance
            : instance // ignore: cast_nullable_to_non_nullable
                  as String?,
        traceId: freezed == traceId
            ? _value.traceId
            : traceId // ignore: cast_nullable_to_non_nullable
                  as String?,
        requestId: freezed == requestId
            ? _value.requestId
            : requestId // ignore: cast_nullable_to_non_nullable
                  as String?,
        timestamp: freezed == timestamp
            ? _value.timestamp
            : timestamp // ignore: cast_nullable_to_non_nullable
                  as String?,
        errors: freezed == errors
            ? _value._errors
            : errors // ignore: cast_nullable_to_non_nullable
                  as List<ProblemFieldError>?,
        metadata: freezed == metadata
            ? _value._metadata
            : metadata // ignore: cast_nullable_to_non_nullable
                  as Map<String, Object?>?,
        retryable: freezed == retryable
            ? _value.retryable
            : retryable // ignore: cast_nullable_to_non_nullable
                  as bool?,
        retryAfterSeconds: freezed == retryAfterSeconds
            ? _value.retryAfterSeconds
            : retryAfterSeconds // ignore: cast_nullable_to_non_nullable
                  as int?,
        helpUrl: freezed == helpUrl
            ? _value.helpUrl
            : helpUrl // ignore: cast_nullable_to_non_nullable
                  as String?,
        supportReference: freezed == supportReference
            ? _value.supportReference
            : supportReference // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ProblemDetailsImpl extends _ProblemDetails {
  const _$ProblemDetailsImpl({
    this.type,
    this.title,
    this.status,
    this.code,
    this.detail,
    this.instance,
    this.traceId,
    this.requestId,
    this.timestamp,
    final List<ProblemFieldError>? errors,
    final Map<String, Object?>? metadata,
    this.retryable,
    this.retryAfterSeconds,
    this.helpUrl,
    this.supportReference,
  }) : _errors = errors,
       _metadata = metadata,
       super._();

  factory _$ProblemDetailsImpl.fromJson(Map<String, dynamic> json) =>
      _$$ProblemDetailsImplFromJson(json);

  @override
  final String? type;
  @override
  final String? title;
  @override
  final int? status;

  /// Stable machine code, `{module}.{resource}.{condition}`.
  @override
  final String? code;
  @override
  final String? detail;
  @override
  final String? instance;
  @override
  final String? traceId;
  @override
  final String? requestId;
  @override
  final String? timestamp;

  /// Present on 400/403/409/422. The schema form attaches each entry to a field by [pointer].
  final List<ProblemFieldError>? _errors;

  /// Present on 400/403/409/422. The schema form attaches each entry to a field by [pointer].
  @override
  List<ProblemFieldError>? get errors {
    final value = _errors;
    if (value == null) return null;
    if (_errors is EqualUnmodifiableListView) return _errors;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(value);
  }

  /// Whatever the condition needs the client to know: `currentState` on a workflow conflict,
  /// `fieldErrors` on a form validation failure.
  final Map<String, Object?>? _metadata;

  /// Whatever the condition needs the client to know: `currentState` on a workflow conflict,
  /// `fieldErrors` on a form validation failure.
  @override
  Map<String, Object?>? get metadata {
    final value = _metadata;
    if (value == null) return null;
    if (_metadata is EqualUnmodifiableMapView) return _metadata;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableMapView(value);
  }

  @override
  final bool? retryable;
  @override
  final int? retryAfterSeconds;
  @override
  final String? helpUrl;
  @override
  final String? supportReference;

  @override
  String toString() {
    return 'ProblemDetails(type: $type, title: $title, status: $status, code: $code, detail: $detail, instance: $instance, traceId: $traceId, requestId: $requestId, timestamp: $timestamp, errors: $errors, metadata: $metadata, retryable: $retryable, retryAfterSeconds: $retryAfterSeconds, helpUrl: $helpUrl, supportReference: $supportReference)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ProblemDetailsImpl &&
            (identical(other.type, type) || other.type == type) &&
            (identical(other.title, title) || other.title == title) &&
            (identical(other.status, status) || other.status == status) &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.detail, detail) || other.detail == detail) &&
            (identical(other.instance, instance) ||
                other.instance == instance) &&
            (identical(other.traceId, traceId) || other.traceId == traceId) &&
            (identical(other.requestId, requestId) ||
                other.requestId == requestId) &&
            (identical(other.timestamp, timestamp) ||
                other.timestamp == timestamp) &&
            const DeepCollectionEquality().equals(other._errors, _errors) &&
            const DeepCollectionEquality().equals(other._metadata, _metadata) &&
            (identical(other.retryable, retryable) ||
                other.retryable == retryable) &&
            (identical(other.retryAfterSeconds, retryAfterSeconds) ||
                other.retryAfterSeconds == retryAfterSeconds) &&
            (identical(other.helpUrl, helpUrl) || other.helpUrl == helpUrl) &&
            (identical(other.supportReference, supportReference) ||
                other.supportReference == supportReference));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    type,
    title,
    status,
    code,
    detail,
    instance,
    traceId,
    requestId,
    timestamp,
    const DeepCollectionEquality().hash(_errors),
    const DeepCollectionEquality().hash(_metadata),
    retryable,
    retryAfterSeconds,
    helpUrl,
    supportReference,
  );

  /// Create a copy of ProblemDetails
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ProblemDetailsImplCopyWith<_$ProblemDetailsImpl> get copyWith =>
      __$$ProblemDetailsImplCopyWithImpl<_$ProblemDetailsImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$ProblemDetailsImplToJson(this);
  }
}

abstract class _ProblemDetails extends ProblemDetails {
  const factory _ProblemDetails({
    final String? type,
    final String? title,
    final int? status,
    final String? code,
    final String? detail,
    final String? instance,
    final String? traceId,
    final String? requestId,
    final String? timestamp,
    final List<ProblemFieldError>? errors,
    final Map<String, Object?>? metadata,
    final bool? retryable,
    final int? retryAfterSeconds,
    final String? helpUrl,
    final String? supportReference,
  }) = _$ProblemDetailsImpl;
  const _ProblemDetails._() : super._();

  factory _ProblemDetails.fromJson(Map<String, dynamic> json) =
      _$ProblemDetailsImpl.fromJson;

  @override
  String? get type;
  @override
  String? get title;
  @override
  int? get status;

  /// Stable machine code, `{module}.{resource}.{condition}`.
  @override
  String? get code;
  @override
  String? get detail;
  @override
  String? get instance;
  @override
  String? get traceId;
  @override
  String? get requestId;
  @override
  String? get timestamp;

  /// Present on 400/403/409/422. The schema form attaches each entry to a field by [pointer].
  @override
  List<ProblemFieldError>? get errors;

  /// Whatever the condition needs the client to know: `currentState` on a workflow conflict,
  /// `fieldErrors` on a form validation failure.
  @override
  Map<String, Object?>? get metadata;
  @override
  bool? get retryable;
  @override
  int? get retryAfterSeconds;
  @override
  String? get helpUrl;
  @override
  String? get supportReference;

  /// Create a copy of ProblemDetails
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ProblemDetailsImplCopyWith<_$ProblemDetailsImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
