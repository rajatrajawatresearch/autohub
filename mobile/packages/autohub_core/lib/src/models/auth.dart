// See the note in `taxonomy.dart` — the workspace root already settled this diagnostic.
// ignore_for_file: invalid_annotation_target

import 'package:freezed_annotation/freezed_annotation.dart';

part 'auth.freezed.dart';
part 'auth.g.dart';

/// Why the OTP is being sent. Chooses the SMS template and the throttle bucket server-side.
enum OtpPurpose {
  @JsonValue('login')
  login,
  @JsonValue('signup')
  signup,
  @JsonValue('verify_phone')
  verifyPhone,
  @JsonValue('step_up')
  stepUp,
  @JsonValue('reset')
  reset,
}

/// Delivery channel. A transport vocabulary, closed by the contract.
enum OtpChannel {
  @JsonValue('sms')
  sms,
  @JsonValue('whatsapp')
  whatsapp,
  @JsonValue('voice')
  voice,
}

@freezed
class OtpRequest with _$OtpRequest {
  const factory OtpRequest({
    /// E.164 — `+919812345678`.
    required String phone,
    OtpPurpose? purpose,
    OtpChannel? channel,
  }) = _OtpRequest;

  factory OtpRequest.fromJson(Map<String, Object?> json) => _$OtpRequestFromJson(json);
}

@freezed
class OtpChallenge with _$OtpChallenge {
  const factory OtpChallenge({
    required String challengeId,
    @Default(300) int expiresInSeconds,
    @Default(30) int resendAfterSeconds,

    /// `+91 98200 1****5` — shown so the user can confirm the number before waiting on it.
    String? maskedTo,

    /// Four characters echoed in the SMS body. If it does not match the screen, it is a scam call.
    String? hint,
    @JsonKey(unknownEnumValue: OtpChannel.sms) OtpChannel? deliveryChannel,
    @Default(5) int attemptsAllowed,
  }) = _OtpChallenge;

  factory OtpChallenge.fromJson(Map<String, Object?> json) => _$OtpChallengeFromJson(json);
}

@freezed
class OtpVerifyRequest with _$OtpVerifyRequest {
  const factory OtpVerifyRequest({
    required String challengeId,
    required String code,

    /// Binds the resulting refresh token to this install.
    String? deviceId,
  }) = _OtpVerifyRequest;

  factory OtpVerifyRequest.fromJson(Map<String, Object?> json) => _$OtpVerifyRequestFromJson(json);
}

@freezed
class AuthUser with _$AuthUser {
  const factory AuthUser({
    required String id,
    required String displayName,
    String? phone,
    String? email,

    /// Grouping only — for display and audit. **Never** the basis of a client-side gate.
    @Default(<String>[]) List<String> roles,

    /// `listings.listing.create`, `auction.bid.place`. Show and hide on these.
    @Default(<String>[]) List<String> permissions,
    String? activeCity,
    String? dealerId,
    String? avatarUrl,
    @Default(0) int kycLevel,
  }) = _AuthUser;

  factory AuthUser.fromJson(Map<String, Object?> json) => _$AuthUserFromJson(json);

  const AuthUser._();

  /// The client shows and hides on permissions; the server enforces on permissions. Role is a
  /// grouping, not a check — a client that gates on `roles.contains('dealer')` is a client that
  /// disagrees with the server about what a dealer may do the first time an operations user edits
  /// a role.
  bool can(String permission) => permissions.contains(permission);
}

@freezed
class TokenPair with _$TokenPair {
  const factory TokenPair({
    required String accessToken,
    required String refreshToken,

    /// Seconds. 900 — fifteen minutes.
    @Default(900) int expiresIn,
    @Default('Bearer') String tokenType,
  }) = _TokenPair;

  factory TokenPair.fromJson(Map<String, Object?> json) => _$TokenPairFromJson(json);
}

@freezed
class AuthSession with _$AuthSession {
  const factory AuthSession({
    required String accessToken,
    required String refreshToken,
    @Default(900) int expiresIn,
    @Default('Bearer') String tokenType,
    required AuthUser user,
    @Default(false) bool isNewUser,

    /// `complete_profile`, `kyc_required` — the shell routes on this instead of guessing.
    String? nextStep,
  }) = _AuthSession;

  factory AuthSession.fromJson(Map<String, Object?> json) => _$AuthSessionFromJson(json);
}

@freezed
class RefreshRequest with _$RefreshRequest {
  const factory RefreshRequest({
    required String refreshToken,
    String? deviceId,
  }) = _RefreshRequest;

  factory RefreshRequest.fromJson(Map<String, Object?> json) => _$RefreshRequestFromJson(json);
}

@freezed
class SessionInfo with _$SessionInfo {
  const factory SessionInfo({
    required String id,
    required String deviceId,
    String? platform,
    String? model,
    String? city,
    required String lastSeenAt,
    @Default(false) bool isCurrent,
  }) = _SessionInfo;

  factory SessionInfo.fromJson(Map<String, Object?> json) => _$SessionInfoFromJson(json);
}
