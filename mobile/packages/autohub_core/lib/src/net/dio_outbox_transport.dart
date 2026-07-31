import 'package:autohub_core/src/net/api_client.dart';
import 'package:autohub_core/src/net/api_config.dart';
import 'package:autohub_core/src/net/api_exception.dart';
import 'package:autohub_core/src/offline/write_intent.dart';
import 'package:autohub_core/src/offline/write_outcome.dart';

/// The `OutboxTransport` the queue in `offline/` declared and refused to implement itself.
///
/// Its whole job is to turn "what HTTP did" into "what the queue should do about it", and to do so
/// **without a single retry of its own**. Retry policy lives in `OutboxDrainer`, where the persisted
/// attempt count is; two independent retry loops stacked on each other is how a thirty-second
/// outage becomes a ten-minute one and how a battery goes flat on a train.
///
/// Note what it does not do: throw. Every failure — HTTP, socket, deadline — becomes one of the six
/// `WriteOutcome` cases, because the drain loop must make a *different* decision for each and a
/// `catch` that swallows a case it did not anticipate is how a `409` gets retried into a duplicate
/// deal.
final class DioOutboxTransport implements OutboxTransport {
  const DioOutboxTransport(this._client);

  final ApiClient _client;

  @override
  Future<WriteOutcome> send(WriteIntent intent) async {
    try {
      final response = await _client.sendRaw(
        _methodOf(intent.method),
        intent.endpoint,
        body: intent.body,
        context: RequestContext(
          // The key the user's gesture was born with. Substituting a fresh one here would defeat
          // the entire outbox: the replay after a tunnel would look like a second act to the
          // server, and one tap would become two listings.
          idempotencyKey: intent.idempotencyKey,
          // Headers frozen at gesture time — `X-Active-City` above all. A write drained after the
          // user switched cities still belongs to the city they were in when they made it.
          headers: intent.headers.isEmpty ? null : intent.headers,
        ),
      );

      return WriteAccepted(
        statusCode: response.statusCode ?? 200,
        body: _bodyOf(response.data),
      );
    } on ApiException catch (error) {
      return _outcomeOf(error);
    }
  }

  static WriteOutcome _outcomeOf(ApiException error) {
    if (error.isOffline) {
      // Never reached a server, or gave up waiting. Not the write's fault, and specifically not an
      // attempt the drain loop should spend.
      return WriteUnreachable(reason: error.code);
    }
    if (error.isCancelled) {
      return WriteUnreachable(reason: error.code);
    }

    final status = error.statusCode;

    if (status == 401) {
      // The transport already tried its one refresh. The write is fine; the session is not, so the
      // row stays pending and drains after the user signs back in.
      return WriteUnauthenticated(code: error.code);
    }

    if (status == 409) {
      // Never retried. Last-writer-wins is wrong for anything with money in it — the contract
      // carries `metadata.currentState` precisely so the UI can show what changed and ask.
      final currentState = error.metadata['currentState'];
      return WriteConflicted(
        code: error.code,
        currentState: currentState is Map<String, dynamic> ? currentState : null,
        detail: error.message,
      );
    }

    if (status == 429 || status >= 500) {
      // Might not be true in a minute. Backoff and try again — with the server's own delay when it
      // named one.
      return WriteFailedServer(
        statusCode: status,
        code: error.code,
        retryAfter: switch (error.retryAfterSeconds) {
          final int seconds when seconds > 0 => Duration(seconds: seconds),
          _ => null,
        },
      );
    }

    if (status >= 400) {
      // Wrong now and wrong forever. Retrying a `422` spends the user's battery to learn nothing,
      // so it goes straight to poisoned with its field errors intact and the sell form puts them
      // back on the fields that caused them.
      return WriteRejected(
        statusCode: status,
        code: error.code,
        detail: error.message,
        fieldErrors: error.fieldErrors,
      );
    }

    // A malformed `2xx`. The server acted — the queue is done with the row — but nothing usable
    // came back, so the body is dropped rather than guessed at.
    return WriteAccepted(statusCode: status == 0 ? 200 : status);
  }

  /// Exhaustive by construction: when the outbox learns a fifth verb, this stops compiling.
  static HttpMethod _methodOf(WriteMethod method) => switch (method) {
        WriteMethod.post => HttpMethod.post,
        WriteMethod.put => HttpMethod.put,
        WriteMethod.patch => HttpMethod.patch,
        WriteMethod.delete => HttpMethod.delete,
      };

  static Map<String, dynamic>? _bodyOf(Object? data) {
    if (data is Map<String, dynamic>) return data;
    if (data is Map<Object?, Object?>) {
      return <String, dynamic>{
        for (final entry in data.entries)
          if (entry.key is String) entry.key! as String: entry.value,
      };
    }
    // `204`, or an endpoint that answers with a bare array. Neither is something the drain loop
    // hands to a listener as new entity state.
    return null;
  }
}
