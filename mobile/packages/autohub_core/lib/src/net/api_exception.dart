import 'package:autohub_core/src/models/problem.dart';

/// Codes this package raises itself.
///
/// They exist so that a caller branches on one vocabulary whether the failure came from the server,
/// from the socket, or from the client's own deadline. A screen that has to check `error is
/// SocketException` in one place and `problem.code` in another grows two error paths, and one of
/// them is always the one nobody tested.
abstract final class ApiErrorCodes {
  /// The client's deadline expired. Distinct from [unreachable]: the server may well have acted.
  static const String timeout = 'common.network.timeout';

  /// Nothing was sent or nothing came back — no signal, DNS, TLS. The server did not act.
  static const String unreachable = 'common.network.unreachable';

  /// The caller cancelled, usually because the screen was popped.
  static const String cancelled = 'common.network.aborted';

  /// A `2xx` whose body does not match the contract.
  static const String malformedResponse = 'common.response.malformed';

  /// A `401` that survived the one refresh attempt.
  static const String unauthenticated = 'identity.token.expired';
}

/// Every failure this transport can produce, as one type.
///
/// **Thrown, not returned.** The alternative — a `Result` at every call site — was considered and
/// rejected for a specific reason: Riverpod's `AsyncValue` already models error-or-data at the seam
/// where the UI actually needs the distinction, and it carries the thrown object. A `Result` would
/// mean every provider unwraps twice and re-wraps once, and the compiler would stop objecting to
/// the unwrap that forgets the error case. Throwing puts the failure exactly where an `AsyncValue`
/// picks it up.
///
/// The one rule that goes with that choice: **no raw `DioException` ever escapes this package.** A
/// caller catching `ApiException` has caught everything.
///
/// Branch on [code]. Never on [message] — that is human text resolved through the i18n registry and
/// reworded whenever a content person decides it reads badly, which on a phone ships to every
/// handset at once and cannot be rolled back.
final class ApiException implements Exception {
  const ApiException({
    required this.statusCode,
    required this.code,
    required this.message,
    this.problem,
    int? retryAfterSeconds,
  }) : _retryAfterOverride = retryAfterSeconds;

  /// Builds from a response body that may or may not be RFC 9457.
  ///
  /// Never throws. A `502` from a load balancer is an HTML page, a crash handler emits something
  /// else again, and the parse of an error must not itself fail — that turns a diagnosable outage
  /// into an unhandled exception on a screen the user is standing in.
  factory ApiException.fromPayload(
    int statusCode,
    Object? payload,
    String fallbackMessage, {
    int? retryAfterSeconds,
  }) {
    ProblemDetails? problem;
    if (payload is Map<Object?, Object?>) {
      final typed = <String, Object?>{
        for (final entry in payload.entries)
          if (entry.key is String) entry.key! as String: entry.value,
      };
      try {
        problem = ProblemDetails.fromJson(typed);
      } on Object {
        // Shaped like JSON but not like a Problem. Fall through to the HTTP-status code below,
        // which is still something support can act on.
        problem = null;
      }
    }

    return ApiException(
      statusCode: statusCode,
      code: problem?.code ?? 'common.http.$statusCode',
      message: problem?.detail ?? problem?.title ?? fallbackMessage,
      problem: problem,
      retryAfterSeconds: retryAfterSeconds,
    );
  }

  /// HTTP status, or `0` when nothing reached a server.
  final int statusCode;

  /// The stable machine code. `{module}.{resource}.{condition}`.
  final String code;

  /// Human text. Displayed, never branched on.
  final String message;

  final ProblemDetails? problem;

  final int? _retryAfterOverride;

  /// Whatever the condition needs the caller to know — `currentState` on a workflow `409`.
  Map<String, Object?> get metadata => problem?.metadata ?? const <String, Object?>{};

  /// Keyed the way a schema form keys its fields, so a `422` lands back on the field that caused
  /// it rather than as a banner the user cannot act on.
  Map<String, List<String>> get fieldErrors =>
      problem?.fieldErrors ?? const <String, List<String>>{};

  /// The delay the server asked for, from the `Retry-After` header or from
  /// `problem.retryAfterSeconds`. When the server names a delay it wins over any client backoff
  /// curve — it knows why it is shedding load and the client does not.
  int? get retryAfterSeconds => _retryAfterOverride ?? problem?.retryAfterSeconds;

  String? get traceId => problem?.traceId;

  /// `error.hasCode('auction.bid.below_increment')` reads the way the check is meant to be made.
  bool hasCode(String value) => code == value;

  /// True for a whole module or resource: `hasCodePrefix('auction.bid.')`.
  bool hasCodePrefix(String prefix) => code.startsWith(prefix);

  /// The request never reached a server, or gave up waiting.
  ///
  /// The distinction the offline queue cares about: this is "not the write's fault", and it must
  /// not consume an attempt. A commuter on the Mumbai local passes through enough dead spots in
  /// one journey to exhaust any sane poison limit.
  bool get isOffline =>
      code == ApiErrorCodes.unreachable || code == ApiErrorCodes.timeout;

  bool get isCancelled => code == ApiErrorCodes.cancelled;

  @override
  String toString() => 'ApiException($statusCode $code): $message';
}
