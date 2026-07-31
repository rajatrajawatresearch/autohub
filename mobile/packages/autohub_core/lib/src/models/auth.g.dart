// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'auth.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$OtpRequestImpl _$$OtpRequestImplFromJson(Map<String, dynamic> json) =>
    _$OtpRequestImpl(
      phone: json['phone'] as String,
      purpose: $enumDecodeNullable(_$OtpPurposeEnumMap, json['purpose']),
      channel: $enumDecodeNullable(_$OtpChannelEnumMap, json['channel']),
    );

Map<String, dynamic> _$$OtpRequestImplToJson(_$OtpRequestImpl instance) =>
    <String, dynamic>{
      'phone': instance.phone,
      'purpose': _$OtpPurposeEnumMap[instance.purpose],
      'channel': _$OtpChannelEnumMap[instance.channel],
    };

const _$OtpPurposeEnumMap = {
  OtpPurpose.login: 'login',
  OtpPurpose.signup: 'signup',
  OtpPurpose.verifyPhone: 'verify_phone',
  OtpPurpose.stepUp: 'step_up',
  OtpPurpose.reset: 'reset',
};

const _$OtpChannelEnumMap = {
  OtpChannel.sms: 'sms',
  OtpChannel.whatsapp: 'whatsapp',
  OtpChannel.voice: 'voice',
};

_$OtpChallengeImpl _$$OtpChallengeImplFromJson(Map<String, dynamic> json) =>
    _$OtpChallengeImpl(
      challengeId: json['challengeId'] as String,
      expiresInSeconds: (json['expiresInSeconds'] as num?)?.toInt() ?? 300,
      resendAfterSeconds: (json['resendAfterSeconds'] as num?)?.toInt() ?? 30,
      maskedTo: json['maskedTo'] as String?,
      hint: json['hint'] as String?,
      deliveryChannel: $enumDecodeNullable(
        _$OtpChannelEnumMap,
        json['deliveryChannel'],
        unknownValue: OtpChannel.sms,
      ),
      attemptsAllowed: (json['attemptsAllowed'] as num?)?.toInt() ?? 5,
    );

Map<String, dynamic> _$$OtpChallengeImplToJson(_$OtpChallengeImpl instance) =>
    <String, dynamic>{
      'challengeId': instance.challengeId,
      'expiresInSeconds': instance.expiresInSeconds,
      'resendAfterSeconds': instance.resendAfterSeconds,
      'maskedTo': instance.maskedTo,
      'hint': instance.hint,
      'deliveryChannel': _$OtpChannelEnumMap[instance.deliveryChannel],
      'attemptsAllowed': instance.attemptsAllowed,
    };

_$OtpVerifyRequestImpl _$$OtpVerifyRequestImplFromJson(
  Map<String, dynamic> json,
) => _$OtpVerifyRequestImpl(
  challengeId: json['challengeId'] as String,
  code: json['code'] as String,
  deviceId: json['deviceId'] as String?,
);

Map<String, dynamic> _$$OtpVerifyRequestImplToJson(
  _$OtpVerifyRequestImpl instance,
) => <String, dynamic>{
  'challengeId': instance.challengeId,
  'code': instance.code,
  'deviceId': instance.deviceId,
};

_$AuthUserImpl _$$AuthUserImplFromJson(Map<String, dynamic> json) =>
    _$AuthUserImpl(
      id: json['id'] as String,
      displayName: json['displayName'] as String,
      phone: json['phone'] as String?,
      email: json['email'] as String?,
      roles:
          (json['roles'] as List<dynamic>?)?.map((e) => e as String).toList() ??
          const <String>[],
      permissions:
          (json['permissions'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          const <String>[],
      activeCity: json['activeCity'] as String?,
      dealerId: json['dealerId'] as String?,
      avatarUrl: json['avatarUrl'] as String?,
      kycLevel: (json['kycLevel'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$$AuthUserImplToJson(_$AuthUserImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'displayName': instance.displayName,
      'phone': instance.phone,
      'email': instance.email,
      'roles': instance.roles,
      'permissions': instance.permissions,
      'activeCity': instance.activeCity,
      'dealerId': instance.dealerId,
      'avatarUrl': instance.avatarUrl,
      'kycLevel': instance.kycLevel,
    };

_$TokenPairImpl _$$TokenPairImplFromJson(Map<String, dynamic> json) =>
    _$TokenPairImpl(
      accessToken: json['accessToken'] as String,
      refreshToken: json['refreshToken'] as String,
      expiresIn: (json['expiresIn'] as num?)?.toInt() ?? 900,
      tokenType: json['tokenType'] as String? ?? 'Bearer',
    );

Map<String, dynamic> _$$TokenPairImplToJson(_$TokenPairImpl instance) =>
    <String, dynamic>{
      'accessToken': instance.accessToken,
      'refreshToken': instance.refreshToken,
      'expiresIn': instance.expiresIn,
      'tokenType': instance.tokenType,
    };

_$AuthSessionImpl _$$AuthSessionImplFromJson(Map<String, dynamic> json) =>
    _$AuthSessionImpl(
      accessToken: json['accessToken'] as String,
      refreshToken: json['refreshToken'] as String,
      expiresIn: (json['expiresIn'] as num?)?.toInt() ?? 900,
      tokenType: json['tokenType'] as String? ?? 'Bearer',
      user: AuthUser.fromJson(json['user'] as Map<String, dynamic>),
      isNewUser: json['isNewUser'] as bool? ?? false,
      nextStep: json['nextStep'] as String?,
    );

Map<String, dynamic> _$$AuthSessionImplToJson(_$AuthSessionImpl instance) =>
    <String, dynamic>{
      'accessToken': instance.accessToken,
      'refreshToken': instance.refreshToken,
      'expiresIn': instance.expiresIn,
      'tokenType': instance.tokenType,
      'user': instance.user,
      'isNewUser': instance.isNewUser,
      'nextStep': instance.nextStep,
    };

_$RefreshRequestImpl _$$RefreshRequestImplFromJson(Map<String, dynamic> json) =>
    _$RefreshRequestImpl(
      refreshToken: json['refreshToken'] as String,
      deviceId: json['deviceId'] as String?,
    );

Map<String, dynamic> _$$RefreshRequestImplToJson(
  _$RefreshRequestImpl instance,
) => <String, dynamic>{
  'refreshToken': instance.refreshToken,
  'deviceId': instance.deviceId,
};

_$SessionInfoImpl _$$SessionInfoImplFromJson(Map<String, dynamic> json) =>
    _$SessionInfoImpl(
      id: json['id'] as String,
      deviceId: json['deviceId'] as String,
      platform: json['platform'] as String?,
      model: json['model'] as String?,
      city: json['city'] as String?,
      lastSeenAt: json['lastSeenAt'] as String,
      isCurrent: json['isCurrent'] as bool? ?? false,
    );

Map<String, dynamic> _$$SessionInfoImplToJson(_$SessionInfoImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'deviceId': instance.deviceId,
      'platform': instance.platform,
      'model': instance.model,
      'city': instance.city,
      'lastSeenAt': instance.lastSeenAt,
      'isCurrent': instance.isCurrent,
    };
