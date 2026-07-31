import 'dart:async';

import 'package:autohub_core/src/net/auth_tokens.dart';
import 'package:autohub_core/src/storage/session_store.dart';

/// The keychain-backed `AuthTokenStore` the transport actually runs on.
///
/// It exists because the two halves of the session live at different speeds. `SessionStore` is
/// asynchronous — the keychain is a platform channel — and the transport reads the access token
/// synchronously while building headers for every request in the app. Putting an `await` there
/// would make every HTTP call in the product wait on a channel hop to fetch a string that has not
/// changed in fourteen minutes.
///
/// So the record is hydrated once, at boot, and every write goes to both places: memory for the
/// next request, and the keychain for the next cold launch. A refresh token that only reached
/// memory is a user signed out by the next app restart, which they experience as the app randomly
/// forgetting them.
final class SessionTokenStore implements AuthTokenStore {
  SessionTokenStore._(this._sessions, this._tokens);

  /// Reads the persisted session once. Call from the app shell's bootstrap, before the first
  /// request — an `ApiClient` built against an unhydrated store sends its first call unauthenticated
  /// and takes a `401` for a session that was perfectly good.
  static Future<SessionTokenStore> open(SessionStore sessions) async {
    final stored = await sessions.read();
    return SessionTokenStore._(
      sessions,
      stored == null
          ? null
          : AuthTokens(
              accessToken: stored.accessToken,
              refreshToken: stored.refreshToken,
              expiresAt: stored.accessTokenExpiresAt,
            ),
    );
  }

  final SessionStore _sessions;
  final StreamController<AuthTokens?> _changes = StreamController<AuthTokens?>.broadcast();

  AuthTokens? _tokens;

  /// Preserved across writes so a rotation does not lose which user the cached registries belong
  /// to. Features and i18n are evaluated per user, and serving one user's flags to the next is a
  /// data leak with a friendly face.
  String? _userId;

  @override
  AuthTokens? get tokens => _tokens;

  @override
  Stream<AuthTokens?> get changes => _changes.stream;

  @override
  Future<void> write(AuthTokens? tokens) async {
    _tokens = tokens;

    if (tokens == null) {
      // The caller must also clear the registry cache and the outbox — see `SessionStore.clear`.
      // Forgetting either leaves one user's queued writes to drain under the next user's token.
      await _sessions.clear();
    } else {
      await _sessions.write(
        StoredSession(
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          accessTokenExpiresAt: tokens.expiresAt,
          userId: _userId,
        ),
      );
    }

    if (!_changes.isClosed) _changes.add(tokens);
  }

  /// Records who the session belongs to, once `GET /auth/me` has answered.
  Future<void> attachUser(String userId) async {
    _userId = userId;
    final current = _tokens;
    if (current != null) await write(current);
  }

  Future<void> dispose() => _changes.close();
}
