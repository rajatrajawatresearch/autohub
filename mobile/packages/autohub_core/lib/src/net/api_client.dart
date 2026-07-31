import 'package:autohub_core/src/net/api_config.dart';
import 'package:autohub_core/src/net/api_exception.dart';
import 'package:autohub_core/src/net/auth_tokens.dart';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:uuid/uuid.dart';

/// A decoded JSON object.
typedef JsonMap = Map<String, Object?>;

/// Turns a decoded body into a model. `SearchResponse.fromJson` and friends satisfy it directly.
typedef JsonReader<T> = T Function(JsonMap json);

const Uuid _uuid = Uuid();

/// Keys on `RequestOptions.extra`, so per-request intent survives into the interceptors.
const String _kAuthenticated = 'ah.authenticated';
const String _kAccessToken = 'ah.accessToken';
const String _kLocale = 'ah.locale';
const String _kCity = 'ah.city';
const String _kRefreshed = 'ah.refreshed';

/// Per-call overrides.
@immutable
final class RequestContext {
  const RequestContext({
    this.locale,
    this.city,
    this.accessToken,
    this.timeout,
    this.cancelToken,
    this.headers,
    this.idempotencyKey,
    this.authenticated = true,
  });

  /// Overrides `Accept-Language` for this call.
  final String? locale;

  /// Overrides `X-Active-City`.
  final String? city;

  /// An explicit bearer, for the rare call that must not use the ambient session.
  ///
  /// A request that supplies its own token is also excluded from the refresh-and-retry path: the
  /// caller owns that token's lifecycle and silently swapping it for the session's would send a
  /// request as the wrong user.
  final String? accessToken;

  final Duration? timeout;

  final CancelToken? cancelToken;

  final Map<String, String>? headers;

  /// Supply your own when one user gesture must stay one operation across a retry.
  ///
  /// The outbox always does. See `offline/write_intent.dart` for why the key is minted on the
  /// gesture rather than at send time.
  final String? idempotencyKey;

  /// Set false for the endpoints that must not carry a bearer — the first valuation is deliberately
  /// unauthenticated, because asking for a phone number before showing a number is why users
  /// bounce.
  final bool authenticated;
}

/// A response the caller may need more than the body of.
///
/// Registry documents are the case: the cache stores the body *against* its `ETag` and
/// `X-Registry-Version`, and a `304` carries no body at all. Everything else uses the typed helpers
/// and never sees this.
@immutable
final class ApiResponse<T> {
  const ApiResponse({
    required this.statusCode,
    required this.value,
    this.etag,
    this.registryVersion,
  });

  final int statusCode;

  /// Null on `204` and on `304`.
  final T? value;

  final String? etag;

  /// `X-Registry-Version` — the registry snapshot that served this response.
  final int? registryVersion;

  bool get isNotModified => statusCode == 304;
}

/// The one place in this workspace that knows a backend exists.
///
/// Responsibilities, and nothing beyond them:
///   · build the request — headers, query grammar, idempotency key, deadline
///   · parse the response — models on success, RFC 9457 Problem Details into a typed
///     [ApiException]
///   · recover once — a single, single-flight refresh on `401`
///
/// **Retries happen on `GET` only.** A retried bid is a duplicated bid; a retried payout is a
/// duplicated payout. The `Idempotency-Key` protects the server from acting twice, but the client
/// still refuses to replay a write on its own initiative — because replaying is a decision about
/// somebody else's money that nobody asked it to make.
///
/// Endpoint modules are the intended callers. An app that reaches for this directly is an app that
/// will grow a second copy of the header logic.
final class ApiClient {
  ApiClient({
    required String deviceId,
    ApiConfig config = const ApiConfig(),
    AuthTokenStore? tokens,
    Dio? dio,
    Dio? refreshDio,
  })  : _config = config,
        _deviceId = deviceId,
        _tokens = tokens ?? InMemoryAuthTokenStore() {
    _dio = dio ?? Dio();
    _refreshDio = refreshDio ?? Dio();

    _applyBaseOptions(_dio);
    _applyBaseOptions(_refreshDio);

    final headers = _HeaderInterceptor(
      config: () => _config,
      deviceId: _deviceId,
    );

    // The refresh client carries the standard headers but **not** the auth interceptor. Sending
    // `/auth/refresh` through a chain that reacts to `401` by refreshing is an infinite loop that
    // only shows up when a session has genuinely expired — which is to say, in production.
    _refreshDio.interceptors.add(headers);

    _refresher = _TokenRefresher(
      store: _tokens,
      dio: _refreshDio,
      deviceId: _deviceId,
    );

    _auth = _AuthInterceptor(store: _tokens, refresher: _refresher, retryWith: _dio);

    _dio.interceptors
      ..add(headers)
      ..add(_auth);
  }

  /// Backoff before a retried `GET`. 200ms then 600ms — long enough for a handover between cells,
  /// short enough that a user watching a list does not conclude the app is dead.
  static const List<Duration> _getRetryBackoff = <Duration>[
    Duration(milliseconds: 200),
    Duration(milliseconds: 600),
  ];

  ApiConfig _config;
  final String _deviceId;
  final AuthTokenStore _tokens;

  late final Dio _dio;
  late final Dio _refreshDio;
  late final _TokenRefresher _refresher;
  late final _AuthInterceptor _auth;

  ApiConfig get config => _config;

  AuthTokenStore get tokens => _tokens;

  /// Stable per install, from the keychain. Binds refresh tokens and feeds fraud signals.
  ///
  /// Passed in rather than generated here because a device id minted per launch is not a device id:
  /// it would unbind every refresh token on every cold start and log the user out weekly.
  String get deviceId => _deviceId;

  /// Replaces the transport configuration at runtime — a city switch, a base URL from a QA build
  /// menu. The header interceptor reads it through a closure, so in-flight requests keep the config
  /// they started with and the next one picks up the change.
  void configure(ApiConfig config) {
    _config = config;
    _applyBaseOptions(_dio);
    _applyBaseOptions(_refreshDio);
  }

  void _applyBaseOptions(Dio dio) {
    dio.options
      ..baseUrl = _config.baseUrl
      ..connectTimeout = _config.connectTimeout
      ..receiveTimeout = _config.receiveTimeout
      ..sendTimeout = _config.sendTimeout
      ..responseType = ResponseType.json
      // 3xx counts as success so a conditional GET's `304` reaches the caller as a response rather
      // than as an error. Everything from 400 up throws, and becomes an ApiException.
      ..validateStatus = (int? status) => status != null && status >= 200 && status < 400;
  }

  Future<T> get<T>(
    String path, {
    required JsonReader<T> read,
    Map<String, Object?>? query,
    RequestContext context = const RequestContext(),
  }) async {
    final response = await _send(HttpMethod.get, path, query: query, context: context);
    return _read(response, read, path);
  }

  /// A conditional `GET`, for registry documents.
  ///
  /// Sends `If-None-Match` when [ifNoneMatch] is given and surfaces the `304` rather than treating
  /// it as an error. That `304` is the response this whole caching design optimises for: a couple
  /// of hundred bytes, cheap enough to spend on every cold launch.
  Future<ApiResponse<JsonMap>> getConditional(
    String path, {
    String? ifNoneMatch,
    Map<String, Object?>? query,
    RequestContext context = const RequestContext(),
  }) async {
    final response = await _send(
      HttpMethod.get,
      path,
      query: query,
      context: context,
      extraHeaders: ifNoneMatch == null ? null : <String, String>{'If-None-Match': ifNoneMatch},
    );

    final etag = _firstHeader(response, 'etag');
    final version = int.tryParse(_firstHeader(response, 'x-registry-version') ?? '');

    if (response.statusCode == 304) {
      return ApiResponse<JsonMap>(
        statusCode: 304,
        value: null,
        etag: etag,
        registryVersion: version,
      );
    }

    return ApiResponse<JsonMap>(
      statusCode: response.statusCode ?? 200,
      value: _asJsonMap(response.data, path),
      etag: etag,
      registryVersion: version,
    );
  }

  Future<T> post<T>(
    String path, {
    required JsonReader<T> read,
    Object? body,
    Map<String, Object?>? query,
    RequestContext context = const RequestContext(),
  }) async {
    final response =
        await _send(HttpMethod.post, path, body: body, query: query, context: context);
    return _read(response, read, path);
  }

  Future<T> patch<T>(
    String path, {
    required JsonReader<T> read,
    Object? body,
    Map<String, Object?>? query,
    RequestContext context = const RequestContext(),
  }) async {
    final response =
        await _send(HttpMethod.patch, path, body: body, query: query, context: context);
    return _read(response, read, path);
  }

  /// For endpoints that answer `204`.
  Future<void> send(
    HttpMethod method,
    String path, {
    Object? body,
    Map<String, Object?>? query,
    RequestContext context = const RequestContext(),
  }) async {
    await _send(method, path, body: body, query: query, context: context);
  }

  /// The transport-level escape hatch, for the two adapters in this directory that need the raw
  /// status, headers and body — the registry cache and the outbox drainer. Endpoint modules use the
  /// typed helpers above; a screen that reaches for this is a screen re-implementing decoding.
  Future<Response<Object?>> sendRaw(
    HttpMethod method,
    String path, {
    Object? body,
    Map<String, Object?>? query,
    Map<String, String>? extraHeaders,
    RequestContext context = const RequestContext(),
  }) =>
      _send(
        method,
        path,
        body: body,
        query: query,
        context: context,
        extraHeaders: extraHeaders,
      );

  /// Closes the underlying sockets. Called when the app shell tears down, not per screen.
  void close() {
    _dio.close(force: true);
    _refreshDio.close(force: true);
  }

  Future<Response<Object?>> _send(
    HttpMethod method,
    String path, {
    Object? body,
    Map<String, Object?>? query,
    Map<String, String>? extraHeaders,
    RequestContext context = const RequestContext(),
  }) async {
    final headers = <String, String>{
      ...?extraHeaders,
      ...?context.headers,
      if (context.idempotencyKey != null) 'Idempotency-Key': context.idempotencyKey!,
    };

    final options = Options(
      method: method.wire,
      headers: headers,
      receiveTimeout: context.timeout,
      extra: <String, Object?>{
        _kAuthenticated: context.authenticated,
        if (context.accessToken != null) _kAccessToken: context.accessToken,
        if (context.locale != null) _kLocale: context.locale,
        if (context.city != null) _kCity: context.city,
      },
    );

    var attempt = 0;
    while (true) {
      try {
        return await _dio.request<Object?>(
          path,
          data: body,
          queryParameters: _serialiseQuery(query),
          options: options,
          cancelToken: context.cancelToken,
        );
      } on DioException catch (error) {
        final failure = _toApiException(error);

        // Only a GET, only on a failure that might not be true in a second, and only twice.
        final retryable = method.isSafeToReplay &&
            attempt < _getRetryBackoff.length &&
            (failure.isOffline || failure.statusCode >= 500);
        if (!retryable) throw failure;

        await Future<void>.delayed(_getRetryBackoff[attempt]);
        attempt += 1;
      }
    }
  }

  T _read<T>(Response<Object?> response, JsonReader<T> read, String path) {
    final json = _asJsonMap(response.data, path);
    try {
      return read(json);
    } on Object catch (error) {
      // Deliberately broad. `json_serializable` signals a shape mismatch with a `TypeError`, which
      // is an `Error` and not an `Exception`, and letting that escape means a contract drift
      // surfaces three frames inside a widget build with no mention of which endpoint produced it.
      // This is the boundary; catching everything here is what keeps defensive casts out of the
      // rest of the app.
      //
      // The cause is logged rather than folded into the message: `message` is user-facing text
      // resolved from the i18n registry, and a `TypeError` in a dialog helps nobody.
      assert(() {
        debugPrint('[autohub_core] decode failed for $path: $error');
        return true;
      }(), 'debug-only log of the underlying decode failure');

      throw ApiException(
        statusCode: response.statusCode ?? 200,
        code: ApiErrorCodes.malformedResponse,
        message: 'The response from $path did not match the contract.',
      );
    }
  }

  JsonMap _asJsonMap(Object? data, String path) {
    if (data is Map<String, Object?>) return data;
    if (data is Map<Object?, Object?>) {
      return <String, Object?>{
        for (final entry in data.entries)
          if (entry.key is String) entry.key! as String: entry.value,
      };
    }
    throw ApiException(
      statusCode: 200,
      code: ApiErrorCodes.malformedResponse,
      message: 'The response from $path was not a JSON object.',
    );
  }

  static String? _firstHeader(Response<Object?> response, String name) =>
      response.headers.value(name);

  /// `Retry-After` in its delta-seconds form. The HTTP-date form is deliberately not parsed: it
  /// would be measured against the handset clock, and a phone two minutes fast would either hammer
  /// a server that asked for room or sit idle long after it recovered.
  static int? _retryAfterHeader(Response<Object?>? response) {
    if (response == null) return null;
    final raw = response.headers.value('retry-after');
    if (raw == null) return null;
    return int.tryParse(raw.trim());
  }

  /// Multi-value filters are OR-within-the-attribute and travel comma-joined, per §9. Empty values
  /// are dropped rather than sent blank — `?city=` means something different from omitting it.
  static Map<String, Object?>? _serialiseQuery(Map<String, Object?>? query) {
    if (query == null || query.isEmpty) return null;
    final serialised = <String, Object?>{};
    query.forEach((key, value) {
      if (value == null) return;
      if (value is Iterable<Object?>) {
        final joined = value.map((item) => '$item').where((item) => item.isNotEmpty).join(',');
        if (joined.isNotEmpty) serialised[key] = joined;
        return;
      }
      final text = '$value';
      if (text.isEmpty) return;
      serialised[key] = text;
    });
    return serialised.isEmpty ? null : serialised;
  }

  static ApiException _toApiException(DioException error) => switch (error.type) {
        DioExceptionType.connectionTimeout ||
        DioExceptionType.sendTimeout ||
        DioExceptionType.receiveTimeout ||
        DioExceptionType.transformTimeout =>
          const ApiException(
            statusCode: 408,
            code: ApiErrorCodes.timeout,
            message: 'That took longer than expected. Try again.',
          ),
        DioExceptionType.cancel => const ApiException(
            statusCode: 0,
            code: ApiErrorCodes.cancelled,
            message: 'Request cancelled.',
          ),
        DioExceptionType.badCertificate ||
        DioExceptionType.connectionError ||
        DioExceptionType.unknown =>
          const ApiException(
            statusCode: 0,
            code: ApiErrorCodes.unreachable,
            message: 'Cannot reach AutoHub. Check your connection and try again.',
          ),
        DioExceptionType.badResponse => ApiException.fromPayload(
            error.response?.statusCode ?? 500,
            error.response?.data,
            'The request could not be completed.',
            retryAfterSeconds: _retryAfterHeader(error.response),
          ),
      };
}

/// Headers every request carries, per `contracts/registry-api.md` §12.
class _HeaderInterceptor extends Interceptor {
  _HeaderInterceptor({required this.config, required this.deviceId});

  final ApiConfig Function() config;
  final String deviceId;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    final current = config();
    final headers = options.headers;

    headers['Accept'] = 'application/json';
    headers['Accept-Language'] =
        (options.extra[_kLocale] as String?) ?? current.defaultLocale;
    headers['X-Device-Id'] = deviceId;
    headers['X-Client'] = current.clientId;
    headers['X-Request-Id'] = _uuid.v4();

    final city = (options.extra[_kCity] as String?) ?? current.defaultCity;
    // Never overwrite a city the caller already set: the outbox freezes `X-Active-City` at the
    // moment the user acted, and a write drained after they switched cities belongs to the city
    // they were in when they made it.
    if (city != null && city.isNotEmpty && !headers.containsKey('X-Active-City')) {
      headers['X-Active-City'] = city;
    }

    final method = HttpMethod.tryFromWire(options.method);
    if (method != null && method.isWrite && !headers.containsKey('Idempotency-Key')) {
      // A last resort. Every write that a user initiated should arrive here with its key already
      // set, minted on the gesture — a key generated at send time is a *different* key on the
      // retry after a tunnel, and the server has no way to recognise the second POST as the same
      // act. That is how one tap becomes two listings, or two debits.
      headers['Idempotency-Key'] = _uuid.v4();
    }

    handler.next(options);
  }
}

/// Attaches the bearer, and recovers from exactly one `401`.
class _AuthInterceptor extends Interceptor {
  _AuthInterceptor({
    required this.store,
    required this.refresher,
    required this.retryWith,
  });

  final AuthTokenStore store;
  final _TokenRefresher refresher;
  final Dio retryWith;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    if (options.extra[_kAuthenticated] == false) return handler.next(options);

    final explicit = options.extra[_kAccessToken] as String?;
    final token = explicit ?? store.tokens?.accessToken;
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    final options = err.requestOptions;

    final recoverable = err.response?.statusCode == 401 &&
        options.extra[_kAuthenticated] != false &&
        options.extra[_kAccessToken] == null &&
        options.extra[_kRefreshed] != true;

    if (!recoverable) return handler.next(err);

    final refreshed = await refresher.refreshOnce();
    if (!refreshed) {
      // One refresh, then stop. A refresh loop on a revoked token is a sign-out that never
      // happens, and a user stuck on a spinner they cannot escape.
      return handler.next(err);
    }

    // Marked before the replay so the retried request cannot start its own refresh cycle.
    options.extra[_kRefreshed] = true;
    try {
      handler.resolve(await retryWith.fetch<Object?>(options));
    } on DioException catch (retryFailure) {
      handler.next(retryFailure);
    }
  }
}

/// Refreshes the session at most once at a time.
class _TokenRefresher {
  _TokenRefresher({required this.store, required this.dio, required this.deviceId});

  final AuthTokenStore store;
  final Dio dio;
  final String deviceId;

  Future<bool>? _inFlight;

  /// A burst of `401`s must produce **one** refresh, not five.
  ///
  /// This is not an optimisation. Refresh tokens are rotating, single-use and carry reuse
  /// detection (`contracts/registry-api.md` §8): a replayed refresh token revokes the whole family
  /// and forces re-authentication. A dashboard that fires five parallel requests the moment its
  /// fifteen-minute access token expires — which is the normal shape of a screen, not an edge case
  /// — would send five refreshes with the same token, and the server would correctly read that as
  /// an attack and sign the user out.
  ///
  /// So the first caller performs the refresh and every other caller awaits the same future.
  Future<bool> refreshOnce() =>
      _inFlight ??= _perform().whenComplete(() => _inFlight = null);

  Future<bool> _perform() async {
    final current = store.tokens;
    if (current == null || current.refreshToken.isEmpty) return false;

    try {
      final response = await dio.post<Object?>(
        '/auth/refresh',
        data: <String, Object?>{
          'refreshToken': current.refreshToken,
          'deviceId': deviceId,
        },
        options: Options(extra: <String, Object?>{_kAuthenticated: false}),
      );

      final body = response.data;
      if (body is! Map<Object?, Object?>) return false;

      final access = body['accessToken'];
      final refresh = body['refreshToken'];
      if (access is! String || refresh is! String) return false;

      final expiresIn = body['expiresIn'];
      await store.write(
        AuthTokens.fromExpiresIn(
          accessToken: access,
          refreshToken: refresh,
          expiresInSeconds: expiresIn is int ? expiresIn : 900,
        ),
      );
      return true;
    } on DioException catch (error) {
      if (error.response == null) {
        // A tunnel is not a revoked session. Keeping the tokens is what lets the app come back to
        // life when signal returns, instead of demanding an OTP from a user who never signed out.
        return false;
      }
      await store.write(null);
      return false;
    }
  }
}
