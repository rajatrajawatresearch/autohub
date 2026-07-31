import 'package:flutter/foundation.dart';

/// The verbs the API client can issue.
///
/// A legal enum: fixed by HTTP and by `contracts/registry-api.md` §12, not by a registry row.
/// `WriteMethod` in `offline/write_intent.dart` is the write-only subset the outbox replays — it
/// omits `get` deliberately, because a queued read answers with the state of the world twenty
/// minutes ago.
enum HttpMethod {
  get,
  post,
  put,
  patch,
  delete;

  String get wire => switch (this) {
        HttpMethod.get => 'GET',
        HttpMethod.post => 'POST',
        HttpMethod.put => 'PUT',
        HttpMethod.patch => 'PATCH',
        HttpMethod.delete => 'DELETE',
      };

  static HttpMethod? tryFromWire(String value) => switch (value.toUpperCase()) {
        'GET' => HttpMethod.get,
        'POST' => HttpMethod.post,
        'PUT' => HttpMethod.put,
        'PATCH' => HttpMethod.patch,
        'DELETE' => HttpMethod.delete,
        _ => null,
      };

  /// Writes carry an `Idempotency-Key`. Reads do not.
  bool get isWrite => this != HttpMethod.get;

  /// Whether the *client* may replay this request on its own initiative.
  ///
  /// `GET` only — and note that this is a narrower question than HTTP idempotence. `PUT` and
  /// `DELETE` are idempotent per RFC 9110 and are still not replayed here: the `Idempotency-Key`
  /// protects the *server* from acting twice, but a client that re-sends a write nobody asked it to
  /// re-send is a client making a decision on the user's behalf about their money. A retried bid is
  /// a duplicated bid; a retried payout is a duplicated payout.
  bool get isSafeToReplay => this == HttpMethod.get;
}

/// Transport configuration. Nothing here is a business constant.
///
/// Fuel types, statuses, fees and facets all arrive from the registry endpoints at runtime. What
/// lives in this file is hosts, deadlines and locale defaults — the things that genuinely differ
/// between a simulator, a QA build and a handset in Pune.
@immutable
final class ApiConfig {
  const ApiConfig({
    this.baseUrl = defaultBaseUrl,
    this.connectTimeout = const Duration(seconds: 10),
    this.receiveTimeout = const Duration(seconds: 15),
    this.sendTimeout = const Duration(seconds: 60),
    this.defaultLocale = 'en-IN',
    this.defaultCity,
    this.clientId = 'mobile/0.1.0',
  });

  /// The local API, which is what the three web portals were built against.
  static const String defaultBaseUrl = 'http://localhost:5180/api/v1';

  /// Absolute base including `/api/v1`, without a trailing slash.
  final String baseUrl;

  /// How long to wait for a socket. Short, because on a phone "no signal" is the common case and
  /// the honest answer is worth more than a long silence.
  final Duration connectTimeout;

  /// Matches the web's 15s deadline. Two clients that give up at different moments produce two
  /// different support stories about the same slow endpoint.
  final Duration receiveTimeout;

  /// Deliberately long: this is the deadline a 40-photo inspection upload runs against, and
  /// killing that at fifteen seconds is how an inspector loses an hour of work.
  final Duration sendTimeout;

  /// Sent as `Accept-Language`; the i18n registry resolves server-side strings with it.
  final String defaultLocale;

  /// Sent as `X-Active-City`; scopes search, pricing and feature evaluation.
  final String? defaultCity;

  /// Sent as `X-Client`; the server uses it for forced-upgrade and feature gating. Set it per app
  /// — `customer/1.4.0`, `dealer/1.4.0` — so a broken release can be identified without guessing.
  final String clientId;

  /// The websocket origin for `/realtime/*`, derived rather than configured separately so the two
  /// cannot drift apart and point at different environments.
  String get wsBaseUrl {
    if (baseUrl.startsWith('https://')) return 'wss://${baseUrl.substring('https://'.length)}';
    if (baseUrl.startsWith('http://')) return 'ws://${baseUrl.substring('http://'.length)}';
    return baseUrl;
  }

  ApiConfig copyWith({
    String? baseUrl,
    Duration? connectTimeout,
    Duration? receiveTimeout,
    Duration? sendTimeout,
    String? defaultLocale,
    String? defaultCity,
    String? clientId,
  }) =>
      ApiConfig(
        baseUrl: baseUrl ?? this.baseUrl,
        connectTimeout: connectTimeout ?? this.connectTimeout,
        receiveTimeout: receiveTimeout ?? this.receiveTimeout,
        sendTimeout: sendTimeout ?? this.sendTimeout,
        defaultLocale: defaultLocale ?? this.defaultLocale,
        defaultCity: defaultCity ?? this.defaultCity,
        clientId: clientId ?? this.clientId,
      );
}
