import 'dart:convert';
import 'dart:typed_data';

import 'package:autohub_core/src/net/api_client.dart';
import 'package:autohub_core/src/net/api_config.dart';
import 'package:autohub_core/src/net/auth_tokens.dart';
import 'package:autohub_core/src/net/dio_outbox_transport.dart';
import 'package:autohub_core/src/net/dio_registry_transport.dart';
import 'package:autohub_core/src/net/session_token_store.dart';
import 'package:autohub_core/src/offline/write_intent.dart';
import 'package:autohub_core/src/offline/write_outcome.dart';
import 'package:autohub_core/src/registry/registry_key.dart';
import 'package:autohub_core/src/registry/registry_transport.dart';
import 'package:autohub_core/src/storage/secure_store.dart';
import 'package:autohub_core/src/storage/session_store.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// The two adapters that let the registry cache and the offline queue stay free of dio.
///
/// The outbox cases are the expensive ones. Each `WriteOutcome` drives a different decision in the
/// drain loop, and collapsing two of them is how a `409` gets retried into a duplicate deal or a
/// tunnel gets counted as a server rejection and poisons a listing the seller finished composing.
void main() {
  group('DioOutboxTransport', () {
    test('a 2xx is accepted, and the server`s answer comes back with it', () async {
      // For a workflow action the body is the new snapshot, which the caller uses to replace state
      // rather than guessing what the transition did.
      final recorder = _Recorder();
      final transport = _outbox(
        recorder.wrap(
          (options) async => _json(200, <String, Object?>{
            'workflowKey': 'listing',
            'currentState': <String, Object?>{'code': 'live'},
          }),
        ),
      );

      final outcome = await transport.send(_intent());

      expect(outcome, isA<WriteAccepted>());
      final accepted = outcome as WriteAccepted;
      expect(accepted.statusCode, 200);
      expect(accepted.body?['workflowKey'], 'listing');
    });

    test('the key minted on the gesture is the key on the wire', () async {
      final recorder = _Recorder();
      final transport = _outbox(recorder.wrap((options) async => _json(202, <String, Object?>{})));

      final intent = _intent();
      await transport.send(intent);

      expect(recorder.last!.headers['Idempotency-Key'], intent.idempotencyKey);
    });

    test('headers frozen at gesture time survive the drain', () async {
      // The city the user was in when they acted is the city the write belongs to, even if the
      // queue does not drain until they have switched to another one.
      final recorder = _Recorder();
      final transport = _outbox(recorder.wrap((options) async => _json(202, <String, Object?>{})));

      await transport.send(
        WriteIntent.now(
          endpoint: '/listings',
          method: WriteMethod.post,
          headers: const <String, String>{'X-Active-City': 'nagpur'},
        ),
      );

      expect(recorder.last!.headers['X-Active-City'], 'nagpur');
    });

    test('a 409 is a conflict carrying the server`s current state, and is never a retry', () async {
      final transport = _outbox(
        (options) async => _json(409, <String, Object?>{
          'code': 'platform.workflow.transition_not_allowed',
          'detail': 'The seller withdrew the offer.',
          'metadata': <String, Object?>{
            'currentState': <String, Object?>{'code': 'withdrawn'},
          },
        }),
      );

      final outcome = await transport.send(_intent());

      expect(outcome, isA<WriteConflicted>());
      final conflict = outcome as WriteConflicted;
      expect(conflict.code, 'platform.workflow.transition_not_allowed');
      expect(conflict.currentState?['code'], 'withdrawn');
      expect(conflict.detail, 'The seller withdrew the offer.');
    });

    test('a 401 is kept apart from a rejection, because the write is fine', () async {
      final transport =
          _outbox((options) async => _json(401, <String, Object?>{'code': 'identity.token.expired'}));

      final outcome = await transport.send(_intent());

      expect(outcome, isA<WriteUnauthenticated>());
    });

    test('a 422 is poison, with its field errors intact', () async {
      final transport = _outbox(
        (options) async => _json(422, <String, Object?>{
          'code': 'common.validation.failed',
          'metadata': <String, Object?>{
            'fieldErrors': <String, Object?>{
              'kms_driven': <Object?>['kms.too_high'],
            },
          },
        }),
      );

      final outcome = await transport.send(_intent());

      expect(outcome, isA<WriteRejected>());
      final rejected = outcome as WriteRejected;
      expect(rejected.statusCode, 422);
      expect(rejected.fieldErrors['kms_driven'], <String>['kms.too_high']);
    });

    test('a 429 defers to the delay the server named', () async {
      // The server knows why it is shedding load; the client's backoff curve does not.
      final transport = _outbox(
        (options) async => _json(
          429,
          <String, Object?>{'code': 'common.rate_limited'},
          extraHeaders: <String, List<String>>{
            'retry-after': <String>['30'],
          },
        ),
      );

      final outcome = await transport.send(_intent());

      expect(outcome, isA<WriteFailedServer>());
      expect((outcome as WriteFailedServer).retryAfter, const Duration(seconds: 30));
    });

    test('a 5xx is worth trying again — once, by the drainer, not here', () async {
      var attempts = 0;
      final transport = _outbox((options) async {
        attempts += 1;
        return _json(503, <String, Object?>{'code': 'common.upstream.unavailable'});
      });

      final outcome = await transport.send(_intent());

      expect(outcome, isA<WriteFailedServer>());
      // Two retry loops stacked on each other is how a thirty-second outage becomes a ten-minute
      // one and a battery goes flat on a train.
      expect(attempts, 1);
    });

    test('a tunnel is unreachable, which the drainer must not charge as an attempt', () async {
      final transport = _outbox(
        (options) async =>
            throw DioException.connectionError(requestOptions: options, reason: 'tunnel'),
      );

      final outcome = await transport.send(_intent());

      expect(outcome, isA<WriteUnreachable>());
    });

    test('every outcome is one of the six — nothing throws out of this transport', () async {
      // The drain loop has exactly one shape and no `catch` that can swallow a case it did not
      // anticipate. That only holds if this method really never throws.
      final statuses = <int>[200, 204, 400, 401, 403, 409, 422, 429, 500, 503];
      for (final status in statuses) {
        final transport =
            _outbox((options) async => _json(status, <String, Object?>{'code': 'x.y.z'}));

        final outcome = await transport.send(_intent());

        expect(
          outcome,
          anyOf(
            isA<WriteAccepted>(),
            isA<WriteConflicted>(),
            isA<WriteRejected>(),
            isA<WriteUnauthenticated>(),
            isA<WriteFailedServer>(),
            isA<WriteUnreachable>(),
          ),
          reason: 'status $status',
        );
      }
    });
  });

  group('SessionTokenStore', () {
    test('hydrates once so the first request of a cold launch is authenticated', () async {
      // An unhydrated store sends that first call anonymously and takes a 401 for a session that
      // was perfectly good — which the user sees as the app forgetting them on every launch.
      final secure = InMemorySecureStore();
      final sessions = SessionStore(secure);
      await sessions.write(
        StoredSession(
          accessToken: 'a1',
          refreshToken: 'r1',
          accessTokenExpiresAt: DateTime.utc(2026, 8, 1),
        ),
      );

      final store = await SessionTokenStore.open(sessions);

      expect(store.tokens?.accessToken, 'a1');
      expect(store.tokens?.refreshToken, 'r1');
      await store.dispose();
    });

    test('a rotated pair reaches the keychain, not just memory', () async {
      final secure = InMemorySecureStore();
      final sessions = SessionStore(secure);
      final store = await SessionTokenStore.open(sessions);

      await store.write(
        const AuthTokens(accessToken: 'a2', refreshToken: 'r2'),
      );

      expect((await sessions.read())?.refreshToken, 'r2');
      // And it survives the restart the whole thing exists for.
      final reopened = await SessionTokenStore.open(SessionStore(secure));
      expect(reopened.tokens?.refreshToken, 'r2');

      await store.dispose();
      await reopened.dispose();
    });

    test('a sign-out clears the keychain entry as well as the memory copy', () async {
      final secure = InMemorySecureStore();
      final sessions = SessionStore(secure);
      final store = await SessionTokenStore.open(sessions);
      await store.write(const AuthTokens(accessToken: 'a1', refreshToken: 'r1'));

      await store.write(null);

      expect(store.tokens, isNull);
      expect(await sessions.read(), isNull);
      await store.dispose();
    });
  });

  group('DioRegistryTransport', () {
    test('a 200 carries the version that produced the body', () async {
      final transport = _registry(
        (options) async => _json(
          200,
          <String, Object?>{'version': 41, 'categories': <Object?>[]},
          extraHeaders: <String, List<String>>{
            'etag': <String>['"taxonomy:v41"'],
            'x-registry-version': <String>['41'],
          },
        ),
      );

      final fetch = await transport.fetch(RegistryDocuments.taxonomyTree);

      expect(fetch, isA<RegistryFresh>());
      final fresh = fetch as RegistryFresh;
      expect(fresh.version, 41);
      expect(fresh.etag, '"taxonomy:v41"');
      expect(fresh.body['version'], 41);
    });

    test('the version falls back to the body when the header is absent', () async {
      final transport = _registry(
        (options) async => _json(200, <String, Object?>{'version': 7, 'key': 'sell_vehicle'}),
      );

      final fetch = await transport.fetch(RegistryDocuments.form('sell_vehicle'));

      expect((fetch as RegistryFresh).version, 7);
    });

    test('a 304 keeps the validator so the next revalidation stays cheap', () async {
      final recorder = _Recorder();
      final transport = _registry(
        recorder.wrap((options) async => ResponseBody.fromString('', 304, headers: _jsonHeaders)),
      );

      final fetch = await transport.fetch(
        RegistryDocuments.taxonomyTree,
        ifNoneMatch: '"taxonomy:v41"',
      );

      expect(recorder.last!.headers['If-None-Match'], '"taxonomy:v41"');
      expect(fetch, isA<RegistryNotModified>());
      expect((fetch as RegistryNotModified).etag, '"taxonomy:v41"');
    });

    test('the query the cache key was derived from is the query that is sent', () async {
      // If the two ever differed, an SUV's attribute list could be cached under a truck's key.
      final recorder = _Recorder();
      final transport = _registry(
        recorder.wrap((options) async => _json(200, <String, Object?>{'version': 41})),
      );

      await transport.fetch(RegistryDocuments.attributes('suv'));

      expect(recorder.last!.path, '/platform/taxonomy/attributes');
      expect(recorder.last!.queryParameters, <String, Object?>{'category': 'suv'});
    });

    test('an unreachable network throws, so the cache keeps what it has', () async {
      // Fabricating an empty body here would read to the cache as "the taxonomy is now empty" and
      // blank a working app's category tree.
      final transport = _registry(
        (options) async =>
            throw DioException.connectionError(requestOptions: options, reason: 'basement'),
      );

      await expectLater(
        transport.fetch(RegistryDocuments.taxonomyTree),
        throwsA(anything),
      );
    });
  });
}

/* ── harness ──────────────────────────────────────────────────────────────── */

typedef _Handler = Future<ResponseBody> Function(RequestOptions options);

WriteIntent _intent() => WriteIntent.now(
      endpoint: '/listings/1/workflow/actions',
      method: WriteMethod.post,
      body: <String, dynamic>{'actionCode': 'submit'},
    );

ApiClient _apiClient(_Handler handler) {
  final dio = Dio()..httpClientAdapter = _FakeAdapter(handler);
  final refreshDio = Dio()..httpClientAdapter = _FakeAdapter(handler);
  return ApiClient(
    deviceId: 'device-1',
    config: const ApiConfig(baseUrl: 'https://api.test/api/v1', defaultCity: 'pune'),
    dio: dio,
    refreshDio: refreshDio,
  );
}

DioOutboxTransport _outbox(_Handler handler) => DioOutboxTransport(_apiClient(handler));

DioRegistryTransport _registry(_Handler handler) => DioRegistryTransport(_apiClient(handler));

const Map<String, List<String>> _jsonHeaders = <String, List<String>>{
  Headers.contentTypeHeader: <String>['application/json'],
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

class _Recorder {
  RequestOptions? last;

  _Handler wrap(_Handler inner) => (RequestOptions options) {
        last = options;
        return inner(options);
      };
}
