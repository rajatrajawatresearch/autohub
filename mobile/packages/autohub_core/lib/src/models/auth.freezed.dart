// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'auth.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

OtpRequest _$OtpRequestFromJson(Map<String, dynamic> json) {
  return _OtpRequest.fromJson(json);
}

/// @nodoc
mixin _$OtpRequest {
  /// E.164 — `+919812345678`.
  String get phone => throw _privateConstructorUsedError;
  OtpPurpose? get purpose => throw _privateConstructorUsedError;
  OtpChannel? get channel => throw _privateConstructorUsedError;

  /// Serializes this OtpRequest to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of OtpRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $OtpRequestCopyWith<OtpRequest> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $OtpRequestCopyWith<$Res> {
  factory $OtpRequestCopyWith(
    OtpRequest value,
    $Res Function(OtpRequest) then,
  ) = _$OtpRequestCopyWithImpl<$Res, OtpRequest>;
  @useResult
  $Res call({String phone, OtpPurpose? purpose, OtpChannel? channel});
}

/// @nodoc
class _$OtpRequestCopyWithImpl<$Res, $Val extends OtpRequest>
    implements $OtpRequestCopyWith<$Res> {
  _$OtpRequestCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of OtpRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? phone = null,
    Object? purpose = freezed,
    Object? channel = freezed,
  }) {
    return _then(
      _value.copyWith(
            phone: null == phone
                ? _value.phone
                : phone // ignore: cast_nullable_to_non_nullable
                      as String,
            purpose: freezed == purpose
                ? _value.purpose
                : purpose // ignore: cast_nullable_to_non_nullable
                      as OtpPurpose?,
            channel: freezed == channel
                ? _value.channel
                : channel // ignore: cast_nullable_to_non_nullable
                      as OtpChannel?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$OtpRequestImplCopyWith<$Res>
    implements $OtpRequestCopyWith<$Res> {
  factory _$$OtpRequestImplCopyWith(
    _$OtpRequestImpl value,
    $Res Function(_$OtpRequestImpl) then,
  ) = __$$OtpRequestImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String phone, OtpPurpose? purpose, OtpChannel? channel});
}

/// @nodoc
class __$$OtpRequestImplCopyWithImpl<$Res>
    extends _$OtpRequestCopyWithImpl<$Res, _$OtpRequestImpl>
    implements _$$OtpRequestImplCopyWith<$Res> {
  __$$OtpRequestImplCopyWithImpl(
    _$OtpRequestImpl _value,
    $Res Function(_$OtpRequestImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of OtpRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? phone = null,
    Object? purpose = freezed,
    Object? channel = freezed,
  }) {
    return _then(
      _$OtpRequestImpl(
        phone: null == phone
            ? _value.phone
            : phone // ignore: cast_nullable_to_non_nullable
                  as String,
        purpose: freezed == purpose
            ? _value.purpose
            : purpose // ignore: cast_nullable_to_non_nullable
                  as OtpPurpose?,
        channel: freezed == channel
            ? _value.channel
            : channel // ignore: cast_nullable_to_non_nullable
                  as OtpChannel?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$OtpRequestImpl implements _OtpRequest {
  const _$OtpRequestImpl({required this.phone, this.purpose, this.channel});

  factory _$OtpRequestImpl.fromJson(Map<String, dynamic> json) =>
      _$$OtpRequestImplFromJson(json);

  /// E.164 — `+919812345678`.
  @override
  final String phone;
  @override
  final OtpPurpose? purpose;
  @override
  final OtpChannel? channel;

  @override
  String toString() {
    return 'OtpRequest(phone: $phone, purpose: $purpose, channel: $channel)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$OtpRequestImpl &&
            (identical(other.phone, phone) || other.phone == phone) &&
            (identical(other.purpose, purpose) || other.purpose == purpose) &&
            (identical(other.channel, channel) || other.channel == channel));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, phone, purpose, channel);

  /// Create a copy of OtpRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$OtpRequestImplCopyWith<_$OtpRequestImpl> get copyWith =>
      __$$OtpRequestImplCopyWithImpl<_$OtpRequestImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$OtpRequestImplToJson(this);
  }
}

abstract class _OtpRequest implements OtpRequest {
  const factory _OtpRequest({
    required final String phone,
    final OtpPurpose? purpose,
    final OtpChannel? channel,
  }) = _$OtpRequestImpl;

  factory _OtpRequest.fromJson(Map<String, dynamic> json) =
      _$OtpRequestImpl.fromJson;

  /// E.164 — `+919812345678`.
  @override
  String get phone;
  @override
  OtpPurpose? get purpose;
  @override
  OtpChannel? get channel;

  /// Create a copy of OtpRequest
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$OtpRequestImplCopyWith<_$OtpRequestImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

OtpChallenge _$OtpChallengeFromJson(Map<String, dynamic> json) {
  return _OtpChallenge.fromJson(json);
}

/// @nodoc
mixin _$OtpChallenge {
  String get challengeId => throw _privateConstructorUsedError;
  int get expiresInSeconds => throw _privateConstructorUsedError;
  int get resendAfterSeconds => throw _privateConstructorUsedError;

  /// `+91 98200 1****5` — shown so the user can confirm the number before waiting on it.
  String? get maskedTo => throw _privateConstructorUsedError;

  /// Four characters echoed in the SMS body. If it does not match the screen, it is a scam call.
  String? get hint => throw _privateConstructorUsedError;
  @JsonKey(unknownEnumValue: OtpChannel.sms)
  OtpChannel? get deliveryChannel => throw _privateConstructorUsedError;
  int get attemptsAllowed => throw _privateConstructorUsedError;

  /// Serializes this OtpChallenge to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of OtpChallenge
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $OtpChallengeCopyWith<OtpChallenge> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $OtpChallengeCopyWith<$Res> {
  factory $OtpChallengeCopyWith(
    OtpChallenge value,
    $Res Function(OtpChallenge) then,
  ) = _$OtpChallengeCopyWithImpl<$Res, OtpChallenge>;
  @useResult
  $Res call({
    String challengeId,
    int expiresInSeconds,
    int resendAfterSeconds,
    String? maskedTo,
    String? hint,
    @JsonKey(unknownEnumValue: OtpChannel.sms) OtpChannel? deliveryChannel,
    int attemptsAllowed,
  });
}

/// @nodoc
class _$OtpChallengeCopyWithImpl<$Res, $Val extends OtpChallenge>
    implements $OtpChallengeCopyWith<$Res> {
  _$OtpChallengeCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of OtpChallenge
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? challengeId = null,
    Object? expiresInSeconds = null,
    Object? resendAfterSeconds = null,
    Object? maskedTo = freezed,
    Object? hint = freezed,
    Object? deliveryChannel = freezed,
    Object? attemptsAllowed = null,
  }) {
    return _then(
      _value.copyWith(
            challengeId: null == challengeId
                ? _value.challengeId
                : challengeId // ignore: cast_nullable_to_non_nullable
                      as String,
            expiresInSeconds: null == expiresInSeconds
                ? _value.expiresInSeconds
                : expiresInSeconds // ignore: cast_nullable_to_non_nullable
                      as int,
            resendAfterSeconds: null == resendAfterSeconds
                ? _value.resendAfterSeconds
                : resendAfterSeconds // ignore: cast_nullable_to_non_nullable
                      as int,
            maskedTo: freezed == maskedTo
                ? _value.maskedTo
                : maskedTo // ignore: cast_nullable_to_non_nullable
                      as String?,
            hint: freezed == hint
                ? _value.hint
                : hint // ignore: cast_nullable_to_non_nullable
                      as String?,
            deliveryChannel: freezed == deliveryChannel
                ? _value.deliveryChannel
                : deliveryChannel // ignore: cast_nullable_to_non_nullable
                      as OtpChannel?,
            attemptsAllowed: null == attemptsAllowed
                ? _value.attemptsAllowed
                : attemptsAllowed // ignore: cast_nullable_to_non_nullable
                      as int,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$OtpChallengeImplCopyWith<$Res>
    implements $OtpChallengeCopyWith<$Res> {
  factory _$$OtpChallengeImplCopyWith(
    _$OtpChallengeImpl value,
    $Res Function(_$OtpChallengeImpl) then,
  ) = __$$OtpChallengeImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String challengeId,
    int expiresInSeconds,
    int resendAfterSeconds,
    String? maskedTo,
    String? hint,
    @JsonKey(unknownEnumValue: OtpChannel.sms) OtpChannel? deliveryChannel,
    int attemptsAllowed,
  });
}

/// @nodoc
class __$$OtpChallengeImplCopyWithImpl<$Res>
    extends _$OtpChallengeCopyWithImpl<$Res, _$OtpChallengeImpl>
    implements _$$OtpChallengeImplCopyWith<$Res> {
  __$$OtpChallengeImplCopyWithImpl(
    _$OtpChallengeImpl _value,
    $Res Function(_$OtpChallengeImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of OtpChallenge
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? challengeId = null,
    Object? expiresInSeconds = null,
    Object? resendAfterSeconds = null,
    Object? maskedTo = freezed,
    Object? hint = freezed,
    Object? deliveryChannel = freezed,
    Object? attemptsAllowed = null,
  }) {
    return _then(
      _$OtpChallengeImpl(
        challengeId: null == challengeId
            ? _value.challengeId
            : challengeId // ignore: cast_nullable_to_non_nullable
                  as String,
        expiresInSeconds: null == expiresInSeconds
            ? _value.expiresInSeconds
            : expiresInSeconds // ignore: cast_nullable_to_non_nullable
                  as int,
        resendAfterSeconds: null == resendAfterSeconds
            ? _value.resendAfterSeconds
            : resendAfterSeconds // ignore: cast_nullable_to_non_nullable
                  as int,
        maskedTo: freezed == maskedTo
            ? _value.maskedTo
            : maskedTo // ignore: cast_nullable_to_non_nullable
                  as String?,
        hint: freezed == hint
            ? _value.hint
            : hint // ignore: cast_nullable_to_non_nullable
                  as String?,
        deliveryChannel: freezed == deliveryChannel
            ? _value.deliveryChannel
            : deliveryChannel // ignore: cast_nullable_to_non_nullable
                  as OtpChannel?,
        attemptsAllowed: null == attemptsAllowed
            ? _value.attemptsAllowed
            : attemptsAllowed // ignore: cast_nullable_to_non_nullable
                  as int,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$OtpChallengeImpl implements _OtpChallenge {
  const _$OtpChallengeImpl({
    required this.challengeId,
    this.expiresInSeconds = 300,
    this.resendAfterSeconds = 30,
    this.maskedTo,
    this.hint,
    @JsonKey(unknownEnumValue: OtpChannel.sms) this.deliveryChannel,
    this.attemptsAllowed = 5,
  });

  factory _$OtpChallengeImpl.fromJson(Map<String, dynamic> json) =>
      _$$OtpChallengeImplFromJson(json);

  @override
  final String challengeId;
  @override
  @JsonKey()
  final int expiresInSeconds;
  @override
  @JsonKey()
  final int resendAfterSeconds;

  /// `+91 98200 1****5` — shown so the user can confirm the number before waiting on it.
  @override
  final String? maskedTo;

  /// Four characters echoed in the SMS body. If it does not match the screen, it is a scam call.
  @override
  final String? hint;
  @override
  @JsonKey(unknownEnumValue: OtpChannel.sms)
  final OtpChannel? deliveryChannel;
  @override
  @JsonKey()
  final int attemptsAllowed;

  @override
  String toString() {
    return 'OtpChallenge(challengeId: $challengeId, expiresInSeconds: $expiresInSeconds, resendAfterSeconds: $resendAfterSeconds, maskedTo: $maskedTo, hint: $hint, deliveryChannel: $deliveryChannel, attemptsAllowed: $attemptsAllowed)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$OtpChallengeImpl &&
            (identical(other.challengeId, challengeId) ||
                other.challengeId == challengeId) &&
            (identical(other.expiresInSeconds, expiresInSeconds) ||
                other.expiresInSeconds == expiresInSeconds) &&
            (identical(other.resendAfterSeconds, resendAfterSeconds) ||
                other.resendAfterSeconds == resendAfterSeconds) &&
            (identical(other.maskedTo, maskedTo) ||
                other.maskedTo == maskedTo) &&
            (identical(other.hint, hint) || other.hint == hint) &&
            (identical(other.deliveryChannel, deliveryChannel) ||
                other.deliveryChannel == deliveryChannel) &&
            (identical(other.attemptsAllowed, attemptsAllowed) ||
                other.attemptsAllowed == attemptsAllowed));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    challengeId,
    expiresInSeconds,
    resendAfterSeconds,
    maskedTo,
    hint,
    deliveryChannel,
    attemptsAllowed,
  );

  /// Create a copy of OtpChallenge
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$OtpChallengeImplCopyWith<_$OtpChallengeImpl> get copyWith =>
      __$$OtpChallengeImplCopyWithImpl<_$OtpChallengeImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$OtpChallengeImplToJson(this);
  }
}

abstract class _OtpChallenge implements OtpChallenge {
  const factory _OtpChallenge({
    required final String challengeId,
    final int expiresInSeconds,
    final int resendAfterSeconds,
    final String? maskedTo,
    final String? hint,
    @JsonKey(unknownEnumValue: OtpChannel.sms)
    final OtpChannel? deliveryChannel,
    final int attemptsAllowed,
  }) = _$OtpChallengeImpl;

  factory _OtpChallenge.fromJson(Map<String, dynamic> json) =
      _$OtpChallengeImpl.fromJson;

  @override
  String get challengeId;
  @override
  int get expiresInSeconds;
  @override
  int get resendAfterSeconds;

  /// `+91 98200 1****5` — shown so the user can confirm the number before waiting on it.
  @override
  String? get maskedTo;

  /// Four characters echoed in the SMS body. If it does not match the screen, it is a scam call.
  @override
  String? get hint;
  @override
  @JsonKey(unknownEnumValue: OtpChannel.sms)
  OtpChannel? get deliveryChannel;
  @override
  int get attemptsAllowed;

  /// Create a copy of OtpChallenge
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$OtpChallengeImplCopyWith<_$OtpChallengeImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

OtpVerifyRequest _$OtpVerifyRequestFromJson(Map<String, dynamic> json) {
  return _OtpVerifyRequest.fromJson(json);
}

/// @nodoc
mixin _$OtpVerifyRequest {
  String get challengeId => throw _privateConstructorUsedError;
  String get code => throw _privateConstructorUsedError;

  /// Binds the resulting refresh token to this install.
  String? get deviceId => throw _privateConstructorUsedError;

  /// Serializes this OtpVerifyRequest to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of OtpVerifyRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $OtpVerifyRequestCopyWith<OtpVerifyRequest> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $OtpVerifyRequestCopyWith<$Res> {
  factory $OtpVerifyRequestCopyWith(
    OtpVerifyRequest value,
    $Res Function(OtpVerifyRequest) then,
  ) = _$OtpVerifyRequestCopyWithImpl<$Res, OtpVerifyRequest>;
  @useResult
  $Res call({String challengeId, String code, String? deviceId});
}

/// @nodoc
class _$OtpVerifyRequestCopyWithImpl<$Res, $Val extends OtpVerifyRequest>
    implements $OtpVerifyRequestCopyWith<$Res> {
  _$OtpVerifyRequestCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of OtpVerifyRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? challengeId = null,
    Object? code = null,
    Object? deviceId = freezed,
  }) {
    return _then(
      _value.copyWith(
            challengeId: null == challengeId
                ? _value.challengeId
                : challengeId // ignore: cast_nullable_to_non_nullable
                      as String,
            code: null == code
                ? _value.code
                : code // ignore: cast_nullable_to_non_nullable
                      as String,
            deviceId: freezed == deviceId
                ? _value.deviceId
                : deviceId // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$OtpVerifyRequestImplCopyWith<$Res>
    implements $OtpVerifyRequestCopyWith<$Res> {
  factory _$$OtpVerifyRequestImplCopyWith(
    _$OtpVerifyRequestImpl value,
    $Res Function(_$OtpVerifyRequestImpl) then,
  ) = __$$OtpVerifyRequestImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String challengeId, String code, String? deviceId});
}

/// @nodoc
class __$$OtpVerifyRequestImplCopyWithImpl<$Res>
    extends _$OtpVerifyRequestCopyWithImpl<$Res, _$OtpVerifyRequestImpl>
    implements _$$OtpVerifyRequestImplCopyWith<$Res> {
  __$$OtpVerifyRequestImplCopyWithImpl(
    _$OtpVerifyRequestImpl _value,
    $Res Function(_$OtpVerifyRequestImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of OtpVerifyRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? challengeId = null,
    Object? code = null,
    Object? deviceId = freezed,
  }) {
    return _then(
      _$OtpVerifyRequestImpl(
        challengeId: null == challengeId
            ? _value.challengeId
            : challengeId // ignore: cast_nullable_to_non_nullable
                  as String,
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        deviceId: freezed == deviceId
            ? _value.deviceId
            : deviceId // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$OtpVerifyRequestImpl implements _OtpVerifyRequest {
  const _$OtpVerifyRequestImpl({
    required this.challengeId,
    required this.code,
    this.deviceId,
  });

  factory _$OtpVerifyRequestImpl.fromJson(Map<String, dynamic> json) =>
      _$$OtpVerifyRequestImplFromJson(json);

  @override
  final String challengeId;
  @override
  final String code;

  /// Binds the resulting refresh token to this install.
  @override
  final String? deviceId;

  @override
  String toString() {
    return 'OtpVerifyRequest(challengeId: $challengeId, code: $code, deviceId: $deviceId)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$OtpVerifyRequestImpl &&
            (identical(other.challengeId, challengeId) ||
                other.challengeId == challengeId) &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.deviceId, deviceId) ||
                other.deviceId == deviceId));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, challengeId, code, deviceId);

  /// Create a copy of OtpVerifyRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$OtpVerifyRequestImplCopyWith<_$OtpVerifyRequestImpl> get copyWith =>
      __$$OtpVerifyRequestImplCopyWithImpl<_$OtpVerifyRequestImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$OtpVerifyRequestImplToJson(this);
  }
}

abstract class _OtpVerifyRequest implements OtpVerifyRequest {
  const factory _OtpVerifyRequest({
    required final String challengeId,
    required final String code,
    final String? deviceId,
  }) = _$OtpVerifyRequestImpl;

  factory _OtpVerifyRequest.fromJson(Map<String, dynamic> json) =
      _$OtpVerifyRequestImpl.fromJson;

  @override
  String get challengeId;
  @override
  String get code;

  /// Binds the resulting refresh token to this install.
  @override
  String? get deviceId;

  /// Create a copy of OtpVerifyRequest
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$OtpVerifyRequestImplCopyWith<_$OtpVerifyRequestImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

AuthUser _$AuthUserFromJson(Map<String, dynamic> json) {
  return _AuthUser.fromJson(json);
}

/// @nodoc
mixin _$AuthUser {
  String get id => throw _privateConstructorUsedError;
  String get displayName => throw _privateConstructorUsedError;
  String? get phone => throw _privateConstructorUsedError;
  String? get email => throw _privateConstructorUsedError;

  /// Grouping only — for display and audit. **Never** the basis of a client-side gate.
  List<String> get roles => throw _privateConstructorUsedError;

  /// `listings.listing.create`, `auction.bid.place`. Show and hide on these.
  List<String> get permissions => throw _privateConstructorUsedError;
  String? get activeCity => throw _privateConstructorUsedError;
  String? get dealerId => throw _privateConstructorUsedError;
  String? get avatarUrl => throw _privateConstructorUsedError;
  int get kycLevel => throw _privateConstructorUsedError;

  /// Serializes this AuthUser to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of AuthUser
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $AuthUserCopyWith<AuthUser> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $AuthUserCopyWith<$Res> {
  factory $AuthUserCopyWith(AuthUser value, $Res Function(AuthUser) then) =
      _$AuthUserCopyWithImpl<$Res, AuthUser>;
  @useResult
  $Res call({
    String id,
    String displayName,
    String? phone,
    String? email,
    List<String> roles,
    List<String> permissions,
    String? activeCity,
    String? dealerId,
    String? avatarUrl,
    int kycLevel,
  });
}

/// @nodoc
class _$AuthUserCopyWithImpl<$Res, $Val extends AuthUser>
    implements $AuthUserCopyWith<$Res> {
  _$AuthUserCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of AuthUser
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? displayName = null,
    Object? phone = freezed,
    Object? email = freezed,
    Object? roles = null,
    Object? permissions = null,
    Object? activeCity = freezed,
    Object? dealerId = freezed,
    Object? avatarUrl = freezed,
    Object? kycLevel = null,
  }) {
    return _then(
      _value.copyWith(
            id: null == id
                ? _value.id
                : id // ignore: cast_nullable_to_non_nullable
                      as String,
            displayName: null == displayName
                ? _value.displayName
                : displayName // ignore: cast_nullable_to_non_nullable
                      as String,
            phone: freezed == phone
                ? _value.phone
                : phone // ignore: cast_nullable_to_non_nullable
                      as String?,
            email: freezed == email
                ? _value.email
                : email // ignore: cast_nullable_to_non_nullable
                      as String?,
            roles: null == roles
                ? _value.roles
                : roles // ignore: cast_nullable_to_non_nullable
                      as List<String>,
            permissions: null == permissions
                ? _value.permissions
                : permissions // ignore: cast_nullable_to_non_nullable
                      as List<String>,
            activeCity: freezed == activeCity
                ? _value.activeCity
                : activeCity // ignore: cast_nullable_to_non_nullable
                      as String?,
            dealerId: freezed == dealerId
                ? _value.dealerId
                : dealerId // ignore: cast_nullable_to_non_nullable
                      as String?,
            avatarUrl: freezed == avatarUrl
                ? _value.avatarUrl
                : avatarUrl // ignore: cast_nullable_to_non_nullable
                      as String?,
            kycLevel: null == kycLevel
                ? _value.kycLevel
                : kycLevel // ignore: cast_nullable_to_non_nullable
                      as int,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$AuthUserImplCopyWith<$Res>
    implements $AuthUserCopyWith<$Res> {
  factory _$$AuthUserImplCopyWith(
    _$AuthUserImpl value,
    $Res Function(_$AuthUserImpl) then,
  ) = __$$AuthUserImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String id,
    String displayName,
    String? phone,
    String? email,
    List<String> roles,
    List<String> permissions,
    String? activeCity,
    String? dealerId,
    String? avatarUrl,
    int kycLevel,
  });
}

/// @nodoc
class __$$AuthUserImplCopyWithImpl<$Res>
    extends _$AuthUserCopyWithImpl<$Res, _$AuthUserImpl>
    implements _$$AuthUserImplCopyWith<$Res> {
  __$$AuthUserImplCopyWithImpl(
    _$AuthUserImpl _value,
    $Res Function(_$AuthUserImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AuthUser
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? displayName = null,
    Object? phone = freezed,
    Object? email = freezed,
    Object? roles = null,
    Object? permissions = null,
    Object? activeCity = freezed,
    Object? dealerId = freezed,
    Object? avatarUrl = freezed,
    Object? kycLevel = null,
  }) {
    return _then(
      _$AuthUserImpl(
        id: null == id
            ? _value.id
            : id // ignore: cast_nullable_to_non_nullable
                  as String,
        displayName: null == displayName
            ? _value.displayName
            : displayName // ignore: cast_nullable_to_non_nullable
                  as String,
        phone: freezed == phone
            ? _value.phone
            : phone // ignore: cast_nullable_to_non_nullable
                  as String?,
        email: freezed == email
            ? _value.email
            : email // ignore: cast_nullable_to_non_nullable
                  as String?,
        roles: null == roles
            ? _value._roles
            : roles // ignore: cast_nullable_to_non_nullable
                  as List<String>,
        permissions: null == permissions
            ? _value._permissions
            : permissions // ignore: cast_nullable_to_non_nullable
                  as List<String>,
        activeCity: freezed == activeCity
            ? _value.activeCity
            : activeCity // ignore: cast_nullable_to_non_nullable
                  as String?,
        dealerId: freezed == dealerId
            ? _value.dealerId
            : dealerId // ignore: cast_nullable_to_non_nullable
                  as String?,
        avatarUrl: freezed == avatarUrl
            ? _value.avatarUrl
            : avatarUrl // ignore: cast_nullable_to_non_nullable
                  as String?,
        kycLevel: null == kycLevel
            ? _value.kycLevel
            : kycLevel // ignore: cast_nullable_to_non_nullable
                  as int,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$AuthUserImpl extends _AuthUser {
  const _$AuthUserImpl({
    required this.id,
    required this.displayName,
    this.phone,
    this.email,
    final List<String> roles = const <String>[],
    final List<String> permissions = const <String>[],
    this.activeCity,
    this.dealerId,
    this.avatarUrl,
    this.kycLevel = 0,
  }) : _roles = roles,
       _permissions = permissions,
       super._();

  factory _$AuthUserImpl.fromJson(Map<String, dynamic> json) =>
      _$$AuthUserImplFromJson(json);

  @override
  final String id;
  @override
  final String displayName;
  @override
  final String? phone;
  @override
  final String? email;

  /// Grouping only — for display and audit. **Never** the basis of a client-side gate.
  final List<String> _roles;

  /// Grouping only — for display and audit. **Never** the basis of a client-side gate.
  @override
  @JsonKey()
  List<String> get roles {
    if (_roles is EqualUnmodifiableListView) return _roles;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_roles);
  }

  /// `listings.listing.create`, `auction.bid.place`. Show and hide on these.
  final List<String> _permissions;

  /// `listings.listing.create`, `auction.bid.place`. Show and hide on these.
  @override
  @JsonKey()
  List<String> get permissions {
    if (_permissions is EqualUnmodifiableListView) return _permissions;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_permissions);
  }

  @override
  final String? activeCity;
  @override
  final String? dealerId;
  @override
  final String? avatarUrl;
  @override
  @JsonKey()
  final int kycLevel;

  @override
  String toString() {
    return 'AuthUser(id: $id, displayName: $displayName, phone: $phone, email: $email, roles: $roles, permissions: $permissions, activeCity: $activeCity, dealerId: $dealerId, avatarUrl: $avatarUrl, kycLevel: $kycLevel)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AuthUserImpl &&
            (identical(other.id, id) || other.id == id) &&
            (identical(other.displayName, displayName) ||
                other.displayName == displayName) &&
            (identical(other.phone, phone) || other.phone == phone) &&
            (identical(other.email, email) || other.email == email) &&
            const DeepCollectionEquality().equals(other._roles, _roles) &&
            const DeepCollectionEquality().equals(
              other._permissions,
              _permissions,
            ) &&
            (identical(other.activeCity, activeCity) ||
                other.activeCity == activeCity) &&
            (identical(other.dealerId, dealerId) ||
                other.dealerId == dealerId) &&
            (identical(other.avatarUrl, avatarUrl) ||
                other.avatarUrl == avatarUrl) &&
            (identical(other.kycLevel, kycLevel) ||
                other.kycLevel == kycLevel));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    id,
    displayName,
    phone,
    email,
    const DeepCollectionEquality().hash(_roles),
    const DeepCollectionEquality().hash(_permissions),
    activeCity,
    dealerId,
    avatarUrl,
    kycLevel,
  );

  /// Create a copy of AuthUser
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AuthUserImplCopyWith<_$AuthUserImpl> get copyWith =>
      __$$AuthUserImplCopyWithImpl<_$AuthUserImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$AuthUserImplToJson(this);
  }
}

abstract class _AuthUser extends AuthUser {
  const factory _AuthUser({
    required final String id,
    required final String displayName,
    final String? phone,
    final String? email,
    final List<String> roles,
    final List<String> permissions,
    final String? activeCity,
    final String? dealerId,
    final String? avatarUrl,
    final int kycLevel,
  }) = _$AuthUserImpl;
  const _AuthUser._() : super._();

  factory _AuthUser.fromJson(Map<String, dynamic> json) =
      _$AuthUserImpl.fromJson;

  @override
  String get id;
  @override
  String get displayName;
  @override
  String? get phone;
  @override
  String? get email;

  /// Grouping only — for display and audit. **Never** the basis of a client-side gate.
  @override
  List<String> get roles;

  /// `listings.listing.create`, `auction.bid.place`. Show and hide on these.
  @override
  List<String> get permissions;
  @override
  String? get activeCity;
  @override
  String? get dealerId;
  @override
  String? get avatarUrl;
  @override
  int get kycLevel;

  /// Create a copy of AuthUser
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AuthUserImplCopyWith<_$AuthUserImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

TokenPair _$TokenPairFromJson(Map<String, dynamic> json) {
  return _TokenPair.fromJson(json);
}

/// @nodoc
mixin _$TokenPair {
  String get accessToken => throw _privateConstructorUsedError;
  String get refreshToken => throw _privateConstructorUsedError;

  /// Seconds. 900 — fifteen minutes.
  int get expiresIn => throw _privateConstructorUsedError;
  String get tokenType => throw _privateConstructorUsedError;

  /// Serializes this TokenPair to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of TokenPair
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $TokenPairCopyWith<TokenPair> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $TokenPairCopyWith<$Res> {
  factory $TokenPairCopyWith(TokenPair value, $Res Function(TokenPair) then) =
      _$TokenPairCopyWithImpl<$Res, TokenPair>;
  @useResult
  $Res call({
    String accessToken,
    String refreshToken,
    int expiresIn,
    String tokenType,
  });
}

/// @nodoc
class _$TokenPairCopyWithImpl<$Res, $Val extends TokenPair>
    implements $TokenPairCopyWith<$Res> {
  _$TokenPairCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of TokenPair
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? accessToken = null,
    Object? refreshToken = null,
    Object? expiresIn = null,
    Object? tokenType = null,
  }) {
    return _then(
      _value.copyWith(
            accessToken: null == accessToken
                ? _value.accessToken
                : accessToken // ignore: cast_nullable_to_non_nullable
                      as String,
            refreshToken: null == refreshToken
                ? _value.refreshToken
                : refreshToken // ignore: cast_nullable_to_non_nullable
                      as String,
            expiresIn: null == expiresIn
                ? _value.expiresIn
                : expiresIn // ignore: cast_nullable_to_non_nullable
                      as int,
            tokenType: null == tokenType
                ? _value.tokenType
                : tokenType // ignore: cast_nullable_to_non_nullable
                      as String,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$TokenPairImplCopyWith<$Res>
    implements $TokenPairCopyWith<$Res> {
  factory _$$TokenPairImplCopyWith(
    _$TokenPairImpl value,
    $Res Function(_$TokenPairImpl) then,
  ) = __$$TokenPairImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String accessToken,
    String refreshToken,
    int expiresIn,
    String tokenType,
  });
}

/// @nodoc
class __$$TokenPairImplCopyWithImpl<$Res>
    extends _$TokenPairCopyWithImpl<$Res, _$TokenPairImpl>
    implements _$$TokenPairImplCopyWith<$Res> {
  __$$TokenPairImplCopyWithImpl(
    _$TokenPairImpl _value,
    $Res Function(_$TokenPairImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of TokenPair
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? accessToken = null,
    Object? refreshToken = null,
    Object? expiresIn = null,
    Object? tokenType = null,
  }) {
    return _then(
      _$TokenPairImpl(
        accessToken: null == accessToken
            ? _value.accessToken
            : accessToken // ignore: cast_nullable_to_non_nullable
                  as String,
        refreshToken: null == refreshToken
            ? _value.refreshToken
            : refreshToken // ignore: cast_nullable_to_non_nullable
                  as String,
        expiresIn: null == expiresIn
            ? _value.expiresIn
            : expiresIn // ignore: cast_nullable_to_non_nullable
                  as int,
        tokenType: null == tokenType
            ? _value.tokenType
            : tokenType // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$TokenPairImpl implements _TokenPair {
  const _$TokenPairImpl({
    required this.accessToken,
    required this.refreshToken,
    this.expiresIn = 900,
    this.tokenType = 'Bearer',
  });

  factory _$TokenPairImpl.fromJson(Map<String, dynamic> json) =>
      _$$TokenPairImplFromJson(json);

  @override
  final String accessToken;
  @override
  final String refreshToken;

  /// Seconds. 900 — fifteen minutes.
  @override
  @JsonKey()
  final int expiresIn;
  @override
  @JsonKey()
  final String tokenType;

  @override
  String toString() {
    return 'TokenPair(accessToken: $accessToken, refreshToken: $refreshToken, expiresIn: $expiresIn, tokenType: $tokenType)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$TokenPairImpl &&
            (identical(other.accessToken, accessToken) ||
                other.accessToken == accessToken) &&
            (identical(other.refreshToken, refreshToken) ||
                other.refreshToken == refreshToken) &&
            (identical(other.expiresIn, expiresIn) ||
                other.expiresIn == expiresIn) &&
            (identical(other.tokenType, tokenType) ||
                other.tokenType == tokenType));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, accessToken, refreshToken, expiresIn, tokenType);

  /// Create a copy of TokenPair
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$TokenPairImplCopyWith<_$TokenPairImpl> get copyWith =>
      __$$TokenPairImplCopyWithImpl<_$TokenPairImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$TokenPairImplToJson(this);
  }
}

abstract class _TokenPair implements TokenPair {
  const factory _TokenPair({
    required final String accessToken,
    required final String refreshToken,
    final int expiresIn,
    final String tokenType,
  }) = _$TokenPairImpl;

  factory _TokenPair.fromJson(Map<String, dynamic> json) =
      _$TokenPairImpl.fromJson;

  @override
  String get accessToken;
  @override
  String get refreshToken;

  /// Seconds. 900 — fifteen minutes.
  @override
  int get expiresIn;
  @override
  String get tokenType;

  /// Create a copy of TokenPair
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$TokenPairImplCopyWith<_$TokenPairImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

AuthSession _$AuthSessionFromJson(Map<String, dynamic> json) {
  return _AuthSession.fromJson(json);
}

/// @nodoc
mixin _$AuthSession {
  String get accessToken => throw _privateConstructorUsedError;
  String get refreshToken => throw _privateConstructorUsedError;
  int get expiresIn => throw _privateConstructorUsedError;
  String get tokenType => throw _privateConstructorUsedError;
  AuthUser get user => throw _privateConstructorUsedError;
  bool get isNewUser => throw _privateConstructorUsedError;

  /// `complete_profile`, `kyc_required` — the shell routes on this instead of guessing.
  String? get nextStep => throw _privateConstructorUsedError;

  /// Serializes this AuthSession to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of AuthSession
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $AuthSessionCopyWith<AuthSession> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $AuthSessionCopyWith<$Res> {
  factory $AuthSessionCopyWith(
    AuthSession value,
    $Res Function(AuthSession) then,
  ) = _$AuthSessionCopyWithImpl<$Res, AuthSession>;
  @useResult
  $Res call({
    String accessToken,
    String refreshToken,
    int expiresIn,
    String tokenType,
    AuthUser user,
    bool isNewUser,
    String? nextStep,
  });

  $AuthUserCopyWith<$Res> get user;
}

/// @nodoc
class _$AuthSessionCopyWithImpl<$Res, $Val extends AuthSession>
    implements $AuthSessionCopyWith<$Res> {
  _$AuthSessionCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of AuthSession
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? accessToken = null,
    Object? refreshToken = null,
    Object? expiresIn = null,
    Object? tokenType = null,
    Object? user = null,
    Object? isNewUser = null,
    Object? nextStep = freezed,
  }) {
    return _then(
      _value.copyWith(
            accessToken: null == accessToken
                ? _value.accessToken
                : accessToken // ignore: cast_nullable_to_non_nullable
                      as String,
            refreshToken: null == refreshToken
                ? _value.refreshToken
                : refreshToken // ignore: cast_nullable_to_non_nullable
                      as String,
            expiresIn: null == expiresIn
                ? _value.expiresIn
                : expiresIn // ignore: cast_nullable_to_non_nullable
                      as int,
            tokenType: null == tokenType
                ? _value.tokenType
                : tokenType // ignore: cast_nullable_to_non_nullable
                      as String,
            user: null == user
                ? _value.user
                : user // ignore: cast_nullable_to_non_nullable
                      as AuthUser,
            isNewUser: null == isNewUser
                ? _value.isNewUser
                : isNewUser // ignore: cast_nullable_to_non_nullable
                      as bool,
            nextStep: freezed == nextStep
                ? _value.nextStep
                : nextStep // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }

  /// Create a copy of AuthSession
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $AuthUserCopyWith<$Res> get user {
    return $AuthUserCopyWith<$Res>(_value.user, (value) {
      return _then(_value.copyWith(user: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$AuthSessionImplCopyWith<$Res>
    implements $AuthSessionCopyWith<$Res> {
  factory _$$AuthSessionImplCopyWith(
    _$AuthSessionImpl value,
    $Res Function(_$AuthSessionImpl) then,
  ) = __$$AuthSessionImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String accessToken,
    String refreshToken,
    int expiresIn,
    String tokenType,
    AuthUser user,
    bool isNewUser,
    String? nextStep,
  });

  @override
  $AuthUserCopyWith<$Res> get user;
}

/// @nodoc
class __$$AuthSessionImplCopyWithImpl<$Res>
    extends _$AuthSessionCopyWithImpl<$Res, _$AuthSessionImpl>
    implements _$$AuthSessionImplCopyWith<$Res> {
  __$$AuthSessionImplCopyWithImpl(
    _$AuthSessionImpl _value,
    $Res Function(_$AuthSessionImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AuthSession
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? accessToken = null,
    Object? refreshToken = null,
    Object? expiresIn = null,
    Object? tokenType = null,
    Object? user = null,
    Object? isNewUser = null,
    Object? nextStep = freezed,
  }) {
    return _then(
      _$AuthSessionImpl(
        accessToken: null == accessToken
            ? _value.accessToken
            : accessToken // ignore: cast_nullable_to_non_nullable
                  as String,
        refreshToken: null == refreshToken
            ? _value.refreshToken
            : refreshToken // ignore: cast_nullable_to_non_nullable
                  as String,
        expiresIn: null == expiresIn
            ? _value.expiresIn
            : expiresIn // ignore: cast_nullable_to_non_nullable
                  as int,
        tokenType: null == tokenType
            ? _value.tokenType
            : tokenType // ignore: cast_nullable_to_non_nullable
                  as String,
        user: null == user
            ? _value.user
            : user // ignore: cast_nullable_to_non_nullable
                  as AuthUser,
        isNewUser: null == isNewUser
            ? _value.isNewUser
            : isNewUser // ignore: cast_nullable_to_non_nullable
                  as bool,
        nextStep: freezed == nextStep
            ? _value.nextStep
            : nextStep // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$AuthSessionImpl implements _AuthSession {
  const _$AuthSessionImpl({
    required this.accessToken,
    required this.refreshToken,
    this.expiresIn = 900,
    this.tokenType = 'Bearer',
    required this.user,
    this.isNewUser = false,
    this.nextStep,
  });

  factory _$AuthSessionImpl.fromJson(Map<String, dynamic> json) =>
      _$$AuthSessionImplFromJson(json);

  @override
  final String accessToken;
  @override
  final String refreshToken;
  @override
  @JsonKey()
  final int expiresIn;
  @override
  @JsonKey()
  final String tokenType;
  @override
  final AuthUser user;
  @override
  @JsonKey()
  final bool isNewUser;

  /// `complete_profile`, `kyc_required` — the shell routes on this instead of guessing.
  @override
  final String? nextStep;

  @override
  String toString() {
    return 'AuthSession(accessToken: $accessToken, refreshToken: $refreshToken, expiresIn: $expiresIn, tokenType: $tokenType, user: $user, isNewUser: $isNewUser, nextStep: $nextStep)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AuthSessionImpl &&
            (identical(other.accessToken, accessToken) ||
                other.accessToken == accessToken) &&
            (identical(other.refreshToken, refreshToken) ||
                other.refreshToken == refreshToken) &&
            (identical(other.expiresIn, expiresIn) ||
                other.expiresIn == expiresIn) &&
            (identical(other.tokenType, tokenType) ||
                other.tokenType == tokenType) &&
            (identical(other.user, user) || other.user == user) &&
            (identical(other.isNewUser, isNewUser) ||
                other.isNewUser == isNewUser) &&
            (identical(other.nextStep, nextStep) ||
                other.nextStep == nextStep));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    accessToken,
    refreshToken,
    expiresIn,
    tokenType,
    user,
    isNewUser,
    nextStep,
  );

  /// Create a copy of AuthSession
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AuthSessionImplCopyWith<_$AuthSessionImpl> get copyWith =>
      __$$AuthSessionImplCopyWithImpl<_$AuthSessionImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$AuthSessionImplToJson(this);
  }
}

abstract class _AuthSession implements AuthSession {
  const factory _AuthSession({
    required final String accessToken,
    required final String refreshToken,
    final int expiresIn,
    final String tokenType,
    required final AuthUser user,
    final bool isNewUser,
    final String? nextStep,
  }) = _$AuthSessionImpl;

  factory _AuthSession.fromJson(Map<String, dynamic> json) =
      _$AuthSessionImpl.fromJson;

  @override
  String get accessToken;
  @override
  String get refreshToken;
  @override
  int get expiresIn;
  @override
  String get tokenType;
  @override
  AuthUser get user;
  @override
  bool get isNewUser;

  /// `complete_profile`, `kyc_required` — the shell routes on this instead of guessing.
  @override
  String? get nextStep;

  /// Create a copy of AuthSession
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AuthSessionImplCopyWith<_$AuthSessionImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

RefreshRequest _$RefreshRequestFromJson(Map<String, dynamic> json) {
  return _RefreshRequest.fromJson(json);
}

/// @nodoc
mixin _$RefreshRequest {
  String get refreshToken => throw _privateConstructorUsedError;
  String? get deviceId => throw _privateConstructorUsedError;

  /// Serializes this RefreshRequest to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of RefreshRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $RefreshRequestCopyWith<RefreshRequest> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $RefreshRequestCopyWith<$Res> {
  factory $RefreshRequestCopyWith(
    RefreshRequest value,
    $Res Function(RefreshRequest) then,
  ) = _$RefreshRequestCopyWithImpl<$Res, RefreshRequest>;
  @useResult
  $Res call({String refreshToken, String? deviceId});
}

/// @nodoc
class _$RefreshRequestCopyWithImpl<$Res, $Val extends RefreshRequest>
    implements $RefreshRequestCopyWith<$Res> {
  _$RefreshRequestCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of RefreshRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? refreshToken = null, Object? deviceId = freezed}) {
    return _then(
      _value.copyWith(
            refreshToken: null == refreshToken
                ? _value.refreshToken
                : refreshToken // ignore: cast_nullable_to_non_nullable
                      as String,
            deviceId: freezed == deviceId
                ? _value.deviceId
                : deviceId // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$RefreshRequestImplCopyWith<$Res>
    implements $RefreshRequestCopyWith<$Res> {
  factory _$$RefreshRequestImplCopyWith(
    _$RefreshRequestImpl value,
    $Res Function(_$RefreshRequestImpl) then,
  ) = __$$RefreshRequestImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String refreshToken, String? deviceId});
}

/// @nodoc
class __$$RefreshRequestImplCopyWithImpl<$Res>
    extends _$RefreshRequestCopyWithImpl<$Res, _$RefreshRequestImpl>
    implements _$$RefreshRequestImplCopyWith<$Res> {
  __$$RefreshRequestImplCopyWithImpl(
    _$RefreshRequestImpl _value,
    $Res Function(_$RefreshRequestImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of RefreshRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? refreshToken = null, Object? deviceId = freezed}) {
    return _then(
      _$RefreshRequestImpl(
        refreshToken: null == refreshToken
            ? _value.refreshToken
            : refreshToken // ignore: cast_nullable_to_non_nullable
                  as String,
        deviceId: freezed == deviceId
            ? _value.deviceId
            : deviceId // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$RefreshRequestImpl implements _RefreshRequest {
  const _$RefreshRequestImpl({required this.refreshToken, this.deviceId});

  factory _$RefreshRequestImpl.fromJson(Map<String, dynamic> json) =>
      _$$RefreshRequestImplFromJson(json);

  @override
  final String refreshToken;
  @override
  final String? deviceId;

  @override
  String toString() {
    return 'RefreshRequest(refreshToken: $refreshToken, deviceId: $deviceId)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$RefreshRequestImpl &&
            (identical(other.refreshToken, refreshToken) ||
                other.refreshToken == refreshToken) &&
            (identical(other.deviceId, deviceId) ||
                other.deviceId == deviceId));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, refreshToken, deviceId);

  /// Create a copy of RefreshRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$RefreshRequestImplCopyWith<_$RefreshRequestImpl> get copyWith =>
      __$$RefreshRequestImplCopyWithImpl<_$RefreshRequestImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$RefreshRequestImplToJson(this);
  }
}

abstract class _RefreshRequest implements RefreshRequest {
  const factory _RefreshRequest({
    required final String refreshToken,
    final String? deviceId,
  }) = _$RefreshRequestImpl;

  factory _RefreshRequest.fromJson(Map<String, dynamic> json) =
      _$RefreshRequestImpl.fromJson;

  @override
  String get refreshToken;
  @override
  String? get deviceId;

  /// Create a copy of RefreshRequest
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$RefreshRequestImplCopyWith<_$RefreshRequestImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

SessionInfo _$SessionInfoFromJson(Map<String, dynamic> json) {
  return _SessionInfo.fromJson(json);
}

/// @nodoc
mixin _$SessionInfo {
  String get id => throw _privateConstructorUsedError;
  String get deviceId => throw _privateConstructorUsedError;
  String? get platform => throw _privateConstructorUsedError;
  String? get model => throw _privateConstructorUsedError;
  String? get city => throw _privateConstructorUsedError;
  String get lastSeenAt => throw _privateConstructorUsedError;
  bool get isCurrent => throw _privateConstructorUsedError;

  /// Serializes this SessionInfo to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of SessionInfo
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $SessionInfoCopyWith<SessionInfo> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $SessionInfoCopyWith<$Res> {
  factory $SessionInfoCopyWith(
    SessionInfo value,
    $Res Function(SessionInfo) then,
  ) = _$SessionInfoCopyWithImpl<$Res, SessionInfo>;
  @useResult
  $Res call({
    String id,
    String deviceId,
    String? platform,
    String? model,
    String? city,
    String lastSeenAt,
    bool isCurrent,
  });
}

/// @nodoc
class _$SessionInfoCopyWithImpl<$Res, $Val extends SessionInfo>
    implements $SessionInfoCopyWith<$Res> {
  _$SessionInfoCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of SessionInfo
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? deviceId = null,
    Object? platform = freezed,
    Object? model = freezed,
    Object? city = freezed,
    Object? lastSeenAt = null,
    Object? isCurrent = null,
  }) {
    return _then(
      _value.copyWith(
            id: null == id
                ? _value.id
                : id // ignore: cast_nullable_to_non_nullable
                      as String,
            deviceId: null == deviceId
                ? _value.deviceId
                : deviceId // ignore: cast_nullable_to_non_nullable
                      as String,
            platform: freezed == platform
                ? _value.platform
                : platform // ignore: cast_nullable_to_non_nullable
                      as String?,
            model: freezed == model
                ? _value.model
                : model // ignore: cast_nullable_to_non_nullable
                      as String?,
            city: freezed == city
                ? _value.city
                : city // ignore: cast_nullable_to_non_nullable
                      as String?,
            lastSeenAt: null == lastSeenAt
                ? _value.lastSeenAt
                : lastSeenAt // ignore: cast_nullable_to_non_nullable
                      as String,
            isCurrent: null == isCurrent
                ? _value.isCurrent
                : isCurrent // ignore: cast_nullable_to_non_nullable
                      as bool,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$SessionInfoImplCopyWith<$Res>
    implements $SessionInfoCopyWith<$Res> {
  factory _$$SessionInfoImplCopyWith(
    _$SessionInfoImpl value,
    $Res Function(_$SessionInfoImpl) then,
  ) = __$$SessionInfoImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String id,
    String deviceId,
    String? platform,
    String? model,
    String? city,
    String lastSeenAt,
    bool isCurrent,
  });
}

/// @nodoc
class __$$SessionInfoImplCopyWithImpl<$Res>
    extends _$SessionInfoCopyWithImpl<$Res, _$SessionInfoImpl>
    implements _$$SessionInfoImplCopyWith<$Res> {
  __$$SessionInfoImplCopyWithImpl(
    _$SessionInfoImpl _value,
    $Res Function(_$SessionInfoImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SessionInfo
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? deviceId = null,
    Object? platform = freezed,
    Object? model = freezed,
    Object? city = freezed,
    Object? lastSeenAt = null,
    Object? isCurrent = null,
  }) {
    return _then(
      _$SessionInfoImpl(
        id: null == id
            ? _value.id
            : id // ignore: cast_nullable_to_non_nullable
                  as String,
        deviceId: null == deviceId
            ? _value.deviceId
            : deviceId // ignore: cast_nullable_to_non_nullable
                  as String,
        platform: freezed == platform
            ? _value.platform
            : platform // ignore: cast_nullable_to_non_nullable
                  as String?,
        model: freezed == model
            ? _value.model
            : model // ignore: cast_nullable_to_non_nullable
                  as String?,
        city: freezed == city
            ? _value.city
            : city // ignore: cast_nullable_to_non_nullable
                  as String?,
        lastSeenAt: null == lastSeenAt
            ? _value.lastSeenAt
            : lastSeenAt // ignore: cast_nullable_to_non_nullable
                  as String,
        isCurrent: null == isCurrent
            ? _value.isCurrent
            : isCurrent // ignore: cast_nullable_to_non_nullable
                  as bool,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SessionInfoImpl implements _SessionInfo {
  const _$SessionInfoImpl({
    required this.id,
    required this.deviceId,
    this.platform,
    this.model,
    this.city,
    required this.lastSeenAt,
    this.isCurrent = false,
  });

  factory _$SessionInfoImpl.fromJson(Map<String, dynamic> json) =>
      _$$SessionInfoImplFromJson(json);

  @override
  final String id;
  @override
  final String deviceId;
  @override
  final String? platform;
  @override
  final String? model;
  @override
  final String? city;
  @override
  final String lastSeenAt;
  @override
  @JsonKey()
  final bool isCurrent;

  @override
  String toString() {
    return 'SessionInfo(id: $id, deviceId: $deviceId, platform: $platform, model: $model, city: $city, lastSeenAt: $lastSeenAt, isCurrent: $isCurrent)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SessionInfoImpl &&
            (identical(other.id, id) || other.id == id) &&
            (identical(other.deviceId, deviceId) ||
                other.deviceId == deviceId) &&
            (identical(other.platform, platform) ||
                other.platform == platform) &&
            (identical(other.model, model) || other.model == model) &&
            (identical(other.city, city) || other.city == city) &&
            (identical(other.lastSeenAt, lastSeenAt) ||
                other.lastSeenAt == lastSeenAt) &&
            (identical(other.isCurrent, isCurrent) ||
                other.isCurrent == isCurrent));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    id,
    deviceId,
    platform,
    model,
    city,
    lastSeenAt,
    isCurrent,
  );

  /// Create a copy of SessionInfo
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SessionInfoImplCopyWith<_$SessionInfoImpl> get copyWith =>
      __$$SessionInfoImplCopyWithImpl<_$SessionInfoImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$SessionInfoImplToJson(this);
  }
}

abstract class _SessionInfo implements SessionInfo {
  const factory _SessionInfo({
    required final String id,
    required final String deviceId,
    final String? platform,
    final String? model,
    final String? city,
    required final String lastSeenAt,
    final bool isCurrent,
  }) = _$SessionInfoImpl;

  factory _SessionInfo.fromJson(Map<String, dynamic> json) =
      _$SessionInfoImpl.fromJson;

  @override
  String get id;
  @override
  String get deviceId;
  @override
  String? get platform;
  @override
  String? get model;
  @override
  String? get city;
  @override
  String get lastSeenAt;
  @override
  bool get isCurrent;

  /// Create a copy of SessionInfo
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SessionInfoImplCopyWith<_$SessionInfoImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
