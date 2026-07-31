import 'dart:convert';
import 'dart:typed_data';

import 'package:autohub_core/src/net/api_client.dart';
import 'package:autohub_core/src/net/api_config.dart';
import 'package:autohub_core/src/net/api_exception.dart';
import 'package:autohub_core/src/net/auth_tokens.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// The transport, against a fake socket.
///
/// No server, no device, no plugin. What is being tested here is the behaviour that only shows up
/// when the network misbehaves — the refresh storm, the retry that must not happen, the `409` that
/// must not be replayed — and none of that is reproducible against a healthy backend.
void main() {
  group('headers', () {
    test('every request carries the identity the contract requires', () async {
      final recorder = _Recorder();
      final client = _client(recorder.respondJson(<String, Object?>{'ok': true}));

      await client.get<Object?>('/platform/features', read: (json) => json);

      final headers = recorder.last!.headers;
      expect(headers['X-Device-Id'], 'device-1');
      expect(headers['Accept-Language'], 'en-IN');
      expect(headers['X-Active-City'], 'pune');
      expect(headers['X-Client'], 'test/0.0.1');
      expect(headers['X-Request-Id'], isNotNull);
    });

    test('a write carries an Idempotency-Key and a read does not', () async {
      final recorder = _Recorder();
      final client = _client(recorder.respondJson(<String, Object?>{'ok': true}));

      await client.get<Object?>('/catalog/search', read: (json) => json);
      expect(recorder.last!.headers.containsKey('Idempotency-Key'), isFalse);

      await client.post<Object?>('/listings', read: (json) => json, body: <String, Object?>{});
      expect(recorder.last!.headers['Idempotency-Key'], isNotNull);
    });

    test('a caller-supplied key is never replaced', () async {
      // The outbox mints the key on the user's gesture. Overwriting it here would mean the replay
      // after a tunnel is a *different* act to the server — one tap, two listings.
      final recorder = _Recorder();
      final client = _client(recorder.respondJson(<String, Object?>{'ok': true}));

      await client.post<Object?>(
        '/listings',
        read: (json) => json,
        body: <String, Object?>{},
        context: const RequestContext(idempotencyKey: 'gesture-key'),
      );

      expect(recorder.last!.headers['Idempotency-Key'], 'gesture-key');
    });

    test('an unauthenticated call carries no bearer', () async {
      // The first valuation is deliberately anonymous: asking for a phone number before showing a
      // number is why users bounce.
      final recorder = _Recorder();
      final client = _client(
        recorder.respondJson(<String, Object?>{'ok': true}),
        tokens: InMemoryAuthTokenStore(
          const AuthTokens(accessToken: 'a1', refreshToken: 'r1'),
        ),
      );

      await client.post<Object?>(
        '/valuation/quotes',
        read: (json) => json,
        body: <String, Object?>{},
        context: const RequestContext(authenticated: false),
      );

      expect(recorder.last!.headers.containsKey('Authorization'), isFalse);
    });

    test('a per-request city overrides the default and is not overwritten when preset', () async {
      final recorder = _Recorder();
      final client = _client(recorder.respondJson(<String, Object?>{'ok': true}));

      await client.get<Object?>(
        '/catalog/search',
        read: (json) => json,
        context: const RequestContext(city: 'mumbai'),
      );
      expect(recorder.last!.headers['X-Active-City'], 'mumbai');

      // A frozen header from the outbox wins over everything: the write belongs to the city the
      // user was in when they made it.
      await client.post<Object?>(
        '/listings',
        read: (json) => json,
        body: <String, Object?>{},
        context: const RequestContext(headers: <String, String>{'X-Active-City': 'nagpur'}),
      );
      expect(recorder.last!.headers['X-Active-City'], 'nagpur');
    });
  });

  group('query serialisation', () {
    test('multi-value filters travel comma-joined and empties are dropped', () async {
      final recorder = _Recorder();
      final client = _client(recorder.respondJson(<String, Object?>{'ok': true}));

      await client.get<Object?>(
        '/catalog/search',
        read: (json) => json,
        query: <String, Object?>{
          'q': 'swift',
          'attr.fuel_type': <String>['petrol', 'cng'],
          'city': '',
          'cursor': null,
        },
      );

      expect(recorder.last!.queryParameters, <String, Object?>{
        'q': 'swift',
        'attr.fuel_type': 'petrol,cng',
      });
    });
  });

  group('problem details', () {
    test('a 409 becomes a typed exception carrying the code and the server`s current state', () async {
      final client = _client(
        (options) async => _json(
          409,
          <String, Object?>{
            'type': 'https://autohub.in/errors/workflow.transition_not_allowed',
            'title': 'That is no longer possible',
            'status': 409,
            'code': 'platform.workflow.transition_not_allowed',
            'detail': 'The listing was approved while you were looking at it.',
            'metadata': <String, Object?>{
              'currentState': <String, Object?>{'code': 'live'},
            },
          },
        ),
      );

      await expectLater(
        client.post<Object?>('/listings/1/workflow/actions',
            read: (json) => json, body: <String, Object?>{}),
        throwsA(
          isA<ApiException>()
              .having((e) => e.statusCode, 'statusCode', 409)
              .having((e) => e.code, 'code', 'platform.workflow.transition_not_allowed')
              .having((e) => e.hasCodePrefix('platform.workflow.'), 'prefix match', isTrue)
              .having((e) => e.metadata['currentState'], 'currentState',
                  <String, Object?>{'code': 'live'}),
        ),
      );
    });

    test('a 422 lands its field errors where a form can put them back on fields', () async {
      final client = _client(
        (options) async => _json(
          422,
          <String, Object?>{
            'code': 'common.validation.failed',
            'metadata': <String, Object?>{
              'fieldErrors': <String, Object?>{
                'kms_driven': <Object?>['kms.too_high'],
              },
            },
          },
        ),
      );

      try {
        await client.post<Object?>('/platform/forms/sell_vehicle/validate',
            read: (json) => json, body: <String, Object?>{});
        fail('expected an ApiException');
      } on ApiException catch (error) {
        expect(error.fieldErrors, <String, List<String>>{
          'kms_driven': <String>['kms.too_high'],
        });
      }
    });

    test('a body that is not a Problem still produces an actionable code', () async {
      // A load balancer answers with HTML. The parse of an error must not itself fail.
      final client = _client(
        (options) async => ResponseBody.fromString('<html>502</html>', 502, headers: _htmlHeaders),
      );

      await expectLater(
        client.get<Object?>('/catalog/search', read: (json) => json),
        throwsA(isA<ApiException>().having((e) => e.code, 'code', 'common.http.502')),
      );
    });

    test('a 2xx that does not match the contract is a named failure, not a TypeError', () async {
      final client = _client((options) async => _json(200, <String, Object?>{'unexpected': 1}));

      await expectLater(
        client.get<_StrictModel>('/platform/features', read: _StrictModel.fromJson),
        throwsA(
          isA<ApiException>().having((e) => e.code, 'code', ApiErrorCodes.malformedResponse),
        ),
      );
    });
  });

  group('network failures', () {
    test('an unreachable server and a timeout are distinguishable', () async {
      final unreachable = _client(
        (options) async => throw DioException.connectionError(
          requestOptions: options,
          reason: 'no route to host',
        ),
      );

      await expectLater(
        unreachable.post<Object?>('/listings', read: (json) => json, body: <String, Object?>{}),
        throwsA(
          isA<ApiException>()
              .having((e) => e.code, 'code', ApiErrorCodes.unreachable)
              .having((e) => e.isOffline, 'isOffline', isTrue),
        ),
      );

      final timedOut = _client(
        (options) async => throw DioException.receiveTimeout(
          timeout: const Duration(seconds: 1),
          requestOptions: options,
        ),
      );

      await expectLater(
        timedOut.post<Object?>('/listings', read: (json) => json, body: <String, Object?>{}),
        throwsA(isA<ApiException>().having((e) => e.code, 'code', ApiErrorCodes.timeout)),
      );
    });

    test('a cancelled request is not an error the user should see', () async {
      final token = CancelToken();
      final client = _client((options) async {
        token.cancel();
        await Future<void>.delayed(const Duration(milliseconds: 5));
        throw DioException.requestCancelled(
          requestOptions: options,
          reason: 'cancelled',
        );
      });

      await expectLater(
        client.get<Object?>(
          '/catalog/search',
          read: (json) => json,
          context: RequestContext(cancelToken: token),
        ),
        throwsA(isA<ApiException>().having((e) => e.isCancelled, 'isCancelled', isTrue)),
      );
    });
  });

  group('retry', () {
    test('a GET is retried on a 5xx, three attempts in total', () async {
      var attempts = 0;
      final client = _client((options) async {
        attempts += 1;
        return _json(503, <String, Object?>{'code': 'common.upstream.unavailable'});
      });

      await expectLater(
        client.get<Object?>('/catalog/search', read: (json) => json),
        throwsA(isA<ApiException>()),
      );
      expect(attempts, 3);
    });

    test('a GET is retried when the socket never opened', () async {
      var attempts = 0;
      final client = _client((options) async {
        attempts += 1;
        if (attempts < 3) {
          throw DioException.connectionError(requestOptions: options, reason: 'flaky');
        }
        return _json(200, <String, Object?>{'ok': true});
      });

      await client.get<Object?>('/catalog/search', read: (json) => json);
      expect(attempts, 3);
    });

    test('a write is NEVER retried, whatever the failure', () async {
      // A retried bid is a duplicated bid; a retried payout is a duplicated payout. The
      // Idempotency-Key protects the server from acting twice, and the client still refuses to
      // replay a write nobody asked it to replay.
      for (final failure in <String>['5xx', 'offline']) {
        var attempts = 0;
        final client = _client((options) async {
          attempts += 1;
          if (failure == 'offline') {
            throw DioException.connectionError(requestOptions: options, reason: 'flaky');
          }
          return _json(503, <String, Object?>{'code': 'common.upstream.unavailable'});
        });

        await expectLater(
          client.post<Object?>('/auction/lots/1/bids',
              read: (json) => json, body: <String, Object?>{'amountMinor': 51500000}),
          throwsA(isA<ApiException>()),
        );
        expect(attempts, 1, reason: 'a $failure write must be attempted exactly once');
      }
    });

    test('a 4xx is never retried, because it will be wrong forever', () async {
      var attempts = 0;
      final client = _client((options) async {
        attempts += 1;
        return _json(404, <String, Object?>{'code': 'listing.not_found'});
      });

      await expectLater(
        client.get<Object?>('/listings/nope', read: (json) => json),
        throwsA(isA<ApiException>()),
      );
      expect(attempts, 1);
    });
  });

  group('single-flight refresh — the test that keeps users signed in', () {
    test('a burst of five 401s triggers exactly ONE refresh', () async {
      // Refresh tokens rotate, are single-use, and carry reuse detection (contract §8). Five
      // parallel refreshes with the same token look like a replay attack: the server revokes the
      // whole family and signs the user out. A dashboard firing five requests the moment a
      // fifteen-minute access token expires is the normal shape of a screen, not an edge case.
      var refreshCalls = 0;
      var protectedCalls = 0;
      var refreshed = false;
      final store = InMemoryAuthTokenStore(
        const AuthTokens(accessToken: 'stale', refreshToken: 'r1'),
      );

      final client = _client(
        (options) async {
          if (options.path.contains('/auth/refresh')) {
            refreshCalls += 1;
            await Future<void>.delayed(const Duration(milliseconds: 10));
            refreshed = true;
            return _json(200, <String, Object?>{
              'accessToken': 'fresh',
              'refreshToken': 'r2',
              'expiresIn': 900,
            });
          }

          protectedCalls += 1;
          // Held open so all five requests are genuinely in flight when the first 401 lands.
          await Future<void>.delayed(const Duration(milliseconds: 25));
          if (!refreshed) {
            return _json(401, <String, Object?>{'code': 'identity.token.expired'});
          }
          return _json(200, <String, Object?>{
            'authorization': options.headers['Authorization'],
          });
        },
        tokens: store,
      );

      final results = await Future.wait<Object?>(<Future<Object?>>[
        for (var i = 0; i < 5; i++)
          client.get<Object?>('/listings/mine', read: (json) => json),
      ]);

      expect(refreshCalls, 1, reason: 'five 401s, one refresh');
      expect(protectedCalls, 10, reason: 'five originals plus five replays');
      expect(store.tokens?.accessToken, 'fresh');
      for (final result in results) {
        expect((result! as Map<String, Object?>)['authorization'], 'Bearer fresh');
      }
      await store.dispose();
    });

    test('a failed refresh does not loop, and clears the session', () async {
      var refreshCalls = 0;
      var protectedCalls = 0;
      final store = InMemoryAuthTokenStore(
        const AuthTokens(accessToken: 'stale', refreshToken: 'revoked'),
      );

      final client = _client(
        (options) async {
          if (options.path.contains('/auth/refresh')) {
            refreshCalls += 1;
            return _json(401, <String, Object?>{'code': 'identity.refresh.reused'});
          }
          protectedCalls += 1;
          return _json(401, <String, Object?>{'code': 'identity.token.expired'});
        },
        tokens: store,
      );

      await expectLater(
        client.get<Object?>('/listings/mine', read: (json) => json),
        throwsA(isA<ApiException>().having((e) => e.statusCode, 'statusCode', 401)),
      );

      expect(refreshCalls, 1);
      expect(protectedCalls, 1, reason: 'no replay when the refresh failed');
      expect(store.tokens, isNull, reason: 'a rejected refresh is a real sign-out');
      await store.dispose();
    });

    test('a refresh that fails on the network keeps the session', () async {
      // A tunnel is not a revoked session. Signing the user out here would demand an OTP from
      // somebody who never signed out, on a train, with no signal to receive it.
      final store = InMemoryAuthTokenStore(
        const AuthTokens(accessToken: 'stale', refreshToken: 'r1'),
      );

      final client = _client(
        (options) async {
          if (options.path.contains('/auth/refresh')) {
            throw DioException.connectionError(requestOptions: options, reason: 'tunnel');
          }
          return _json(401, <String, Object?>{'code': 'identity.token.expired'});
        },
        tokens: store,
      );

      await expectLater(
        client.get<Object?>('/listings/mine', read: (json) => json),
        throwsA(isA<ApiException>()),
      );

      expect(store.tokens?.refreshToken, 'r1');
      await store.dispose();
    });
  });

  group('conditional GET', () {
    test('a 200 surfaces the body, the ETag and the registry version', () async {
      final client = _client(
        (options) async => _json(
          200,
          <String, Object?>{'version': 41, 'categories': <Object?>[]},
          extraHeaders: <String, List<String>>{
            'etag': <String>['"taxonomy:v41"'],
            'x-registry-version': <String>['41'],
          },
        ),
      );

      final response = await client.getConditional('/platform/taxonomy/tree');

      expect(response.isNotModified, isFalse);
      expect(response.etag, '"taxonomy:v41"');
      expect(response.registryVersion, 41);
      expect(response.value?['version'], 41);
    });

    test('a 304 is a success with no body, not an error', () async {
      // Treating it as an error is the bug that makes an app blank its own taxonomy on a good
      // network.
      final recorder = _Recorder();
      final client = _client(
        recorder.wrap((options) async => ResponseBody.fromString('', 304, headers: _jsonHeaders)),
      );

      final response = await client.getConditional(
        '/platform/taxonomy/tree',
        ifNoneMatch: '"taxonomy:v41"',
      );

      expect(recorder.last!.headers['If-None-Match'], '"taxonomy:v41"');
      expect(response.isNotModified, isTrue);
      expect(response.value, isNull);
    });
  });
}

/* ── harness ──────────────────────────────────────────────────────────────── */

typedef _Handler = Future<ResponseBody> Function(RequestOptions options);

ApiClient _client(_Handler handler, {AuthTokenStore? tokens}) {
  final dio = Dio()..httpClientAdapter = _FakeAdapter(handler);
  final refreshDio = Dio()..httpClientAdapter = _FakeAdapter(handler);

  return ApiClient(
    deviceId: 'device-1',
    config: const ApiConfig(
      baseUrl: 'https://api.test/api/v1',
      defaultCity: 'pune',
      clientId: 'test/0.0.1',
    ),
    tokens: tokens,
    dio: dio,
    refreshDio: refreshDio,
  );
}

const Map<String, List<String>> _jsonHeaders = <String, List<String>>{
  Headers.contentTypeHeader: <String>['application/json'],
};

const Map<String, List<String>> _htmlHeaders = <String, List<String>>{
  Headers.contentTypeHeader: <String>['text/html'],
};

ResponseBody _json(
  int statusCode,
  Map<String, Object?> body, {
  Map<String, List<String>> extraHeaders = const <String, List<String>>{},
}) =>
    ResponseBody.fromString(
      jsonEncode(body),
      statusCode,
      headers: <String, List<String>>{..._jsonHeaders, ...extraHeaders},
    );

class _FakeAdapter implements HttpClientAdapter {
  _FakeAdapter(this.handler);

  final _Handler handler;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) =>
      handler(options);

  @override
  void close({bool force = false}) {}
}

/// Captures what actually went out on the wire.
class _Recorder {
  RequestOptions? last;

  _Handler respondJson(Map<String, Object?> body) =>
      wrap((options) async => _json(200, body));

  _Handler wrap(_Handler inner) => (RequestOptions options) {
        last = options;
        return inner(options);
      };
}

/// A model that insists on a field the fake response does not send.
class _StrictModel {
  const _StrictModel(this.version);

  factory _StrictModel.fromJson(Map<String, Object?> json) =>
      _StrictModel(json['version']! as int);

  final int version;
}
