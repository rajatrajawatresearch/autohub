import 'dart:async';

import 'package:flutter/foundation.dart';

/// The pair the transport needs to make an authenticated call.
@immutable
final class AuthTokens {
  const AuthTokens({
    required this.accessToken,
    required this.refreshToken,
    this.expiresAt,
  });

  /// Builds from `POST /auth/otp/verify` or `POST /auth/refresh`, which state a *duration*.
  ///
  /// Converted to an absolute instant at the moment of receipt rather than kept as a duration,
  /// because "900 seconds from when?" has no answer once the value has been persisted across an
  /// app restart.
  factory AuthTokens.fromExpiresIn({
    required String accessToken,
    required String refreshToken,
    required int expiresInSeconds,
    DateTime? issuedAt,
  }) =>
      AuthTokens(
        accessToken: accessToken,
        refreshToken: refreshToken,
        expiresAt: (issuedAt ?? DateTime.now().toUtc())
            .toUtc()
            .add(Duration(seconds: expiresInSeconds)),
      );

  final String accessToken;

  /// Rotating, single-use, thirty days, **with reuse detection**. Replaying one revokes the whole
  /// family — which is exactly why the auth interceptor refreshes single-flight.
  final String refreshToken;

  /// Absolute expiry of [accessToken], UTC. Null when the server did not say.
  final DateTime? expiresAt;

  /// Deliberately pessimistic by [skew].
  ///
  /// A token that expires while a request is in flight produces a `401` the user pays for in
  /// latency; refreshing thirty seconds early costs nothing. The handset clock is not trusted for
  /// anything else, but it is trusted for this: being wrong here is a wasted refresh, not a wrong
  /// number on a screen.
  bool isExpired(DateTime now, {Duration skew = const Duration(seconds: 30)}) {
    final expiry = expiresAt;
    if (expiry == null) return false;
    return !now.toUtc().add(skew).isBefore(expiry);
  }
}

/// Where the transport reads and writes the session.
///
/// An interface, not a class, so that the production implementation can put tokens in the
/// keychain — `flutter_secure_storage`, per `docs/10-mobile-architecture.md` §5 — while tests and
/// the first boot use something synchronous. A refresh token in shared preferences is a refresh
/// token any process that can read the app's sandbox can steal, and on a rooted handset that is a
/// realistic threat rather than a theoretical one.
///
/// [tokens] is synchronous on purpose: it is read on the request path, and an `await` there means
/// the header interceptor becomes asynchronous for every call in the app to serve a value that is
/// almost always already in memory. Implementations hydrate once at boot and keep a cached copy.
abstract interface class AuthTokenStore {
  AuthTokens? get tokens;

  /// `null` signs the user out.
  Future<void> write(AuthTokens? tokens);

  /// Fires on every change, including the sign-out. The shell listens and routes.
  Stream<AuthTokens?> get changes;
}

/// The bootstrap and test implementation.
///
/// Holds the pair in memory only, which is the correct behaviour for a test and the wrong one for a
/// shipped app — a user signed out by every cold start will not stay a user. The secure-storage
/// implementation that replaces it belongs beside the rest of the storage layer, not here, because
/// this file must stay free of platform channels so it can run under `flutter test` without a
/// device.
final class InMemoryAuthTokenStore implements AuthTokenStore {
  InMemoryAuthTokenStore([this._tokens]);

  AuthTokens? _tokens;
  final StreamController<AuthTokens?> _changes = StreamController<AuthTokens?>.broadcast();

  @override
  AuthTokens? get tokens => _tokens;

  @override
  Stream<AuthTokens?> get changes => _changes.stream;

  @override
  Future<void> write(AuthTokens? tokens) async {
    _tokens = tokens;
    if (!_changes.isClosed) _changes.add(tokens);
  }

  Future<void> dispose() => _changes.close();
}
