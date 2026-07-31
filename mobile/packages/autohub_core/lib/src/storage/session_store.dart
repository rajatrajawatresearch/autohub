import 'dart:convert';

import 'package:flutter/foundation.dart';

import 'secure_store.dart';

/// The part of a signed-in session that survives the process.
///
/// Named `StoredSession` and not `AuthTokens` because that is what distinguishes it: this is the
/// on-disk record, not the live session object the transport passes around. The access token is
/// kept alongside the refresh token so a cold launch can make its first request without a refresh
/// round trip — fifteen minutes of validity is often still on the clock, and spending a network
/// round trip to discover that is the difference between an app that opens and an app that spins.
@immutable
final class StoredSession {
  const StoredSession({
    required this.accessToken,
    required this.refreshToken,
    this.accessTokenExpiresAt,
    this.userId,
  });

  factory StoredSession.fromJson(Map<String, dynamic> json) {
    final expiry = json['accessTokenExpiresAt'];
    return StoredSession(
      accessToken: json['accessToken'] as String? ?? '',
      refreshToken: json['refreshToken'] as String? ?? '',
      accessTokenExpiresAt: expiry is String ? DateTime.tryParse(expiry) : null,
      userId: json['userId'] as String?,
    );
  }

  /// 15 minutes, per `contracts/registry-api.md` §8.
  final String accessToken;

  /// 30 days, rotating, single-use. The one genuinely dangerous string on the device.
  final String refreshToken;

  final DateTime? accessTokenExpiresAt;

  /// Kept so a launch can decide whether the cached registries belong to this user before any
  /// network call resolves who they are. Features and i18n are evaluated per user.
  final String? userId;

  Map<String, dynamic> toJson() => {
        'accessToken': accessToken,
        'refreshToken': refreshToken,
        if (accessTokenExpiresAt != null)
          'accessTokenExpiresAt': accessTokenExpiresAt!.toUtc().toIso8601String(),
        if (userId != null) 'userId': userId,
      };

  /// True when the access token should be refreshed before use.
  ///
  /// [skew] exists because the alternative is a race the user experiences as a random sign-out: a
  /// token with four seconds left passes this check, spends three of them in transit, and arrives
  /// expired. Refreshing slightly early costs one extra call; refreshing slightly late costs a 401
  /// on a write the user just made.
  ///
  /// A missing expiry returns false — the transport's 401-then-refresh path handles it, and
  /// guessing "expired" would refresh a perfectly good token on every single launch.
  bool needsRefreshAt(DateTime now, {Duration skew = const Duration(seconds: 30)}) {
    final expiry = accessTokenExpiresAt;
    if (expiry == null) return false;
    return !expiry.toUtc().isAfter(now.toUtc().add(skew));
  }

  StoredSession copyWith({
    String? accessToken,
    String? refreshToken,
    DateTime? accessTokenExpiresAt,
    String? userId,
  }) =>
      StoredSession(
        accessToken: accessToken ?? this.accessToken,
        refreshToken: refreshToken ?? this.refreshToken,
        accessTokenExpiresAt: accessTokenExpiresAt ?? this.accessTokenExpiresAt,
        userId: userId ?? this.userId,
      );

  /// Never prints either token. A session that lands in a crash report is a session an aggregator
  /// has indexed.
  @override
  String toString() => 'StoredSession(user: ${userId ?? 'unknown'}, '
      'expires: ${accessTokenExpiresAt?.toIso8601String() ?? 'unknown'})';
}

/// Reads and writes the session in the keychain.
final class SessionStore {
  const SessionStore(this._secure);

  static const String _key = 'ah.session.v1';

  final SecureKeyValueStore _secure;

  /// The stored session, or null when there is none or it cannot be read.
  ///
  /// A corrupt entry is treated as "signed out" rather than thrown from. The only recovery a user
  /// has for a launch that crashes on a bad keychain entry is reinstalling the app, and signing in
  /// again is a far cheaper way to reach the same place.
  Future<StoredSession?> read() async {
    final raw = await _secure.read(_key);
    if (raw == null || raw.isEmpty) return null;
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! Map<String, dynamic>) return null;
      final session = StoredSession.fromJson(decoded);
      if (session.refreshToken.isEmpty) return null;
      return session;
    } on FormatException {
      return null;
    }
  }

  Future<void> write(StoredSession session) =>
      _secure.write(_key, jsonEncode(session.toJson()));

  /// Sign-out, and every path that discovers the session is dead.
  ///
  /// The caller must also clear the registry cache and the outbox. Neither is this class's job, but
  /// forgetting either leaves one user's queued writes to drain under the next user's token.
  Future<void> clear() => _secure.delete(_key);
}
