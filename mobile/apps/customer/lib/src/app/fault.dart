import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

import '../i18n/customer_text.dart';

/// A failure, split into the half a user reads and the half support needs.
///
/// The split is the whole point. A customer standing in a car park is not helped by
/// `catalog.vahan.record_not_found`; they are helped by a sentence that names the problem and the
/// fix. Support is not helped by "that did not go through"; they need the exact machine code and
/// the trace id, because those are what turn a screenshot into a log query.
///
/// So [message] is authored text and [reference] is the diagnostic, and `AppErrorView` renders the
/// first prominently and puts the second behind a copy action. Neither is derived from the other,
/// and nothing in this app branches on [message] — it is reworded by whoever owns the i18n
/// registry, on a Tuesday, without an app-store submission.
///
/// An `Exception` rather than a plain value so a provider can throw one: a `FutureProvider` that
/// resolves the message where the failure happened — with the strings and the diagnostics log in
/// reach — beats every screen re-deriving it during a build, which Riverpod forbids anyway.
@immutable
final class AppFault implements Exception {
  const AppFault({
    required this.code,
    required this.message,
    this.statusCode = 0,
    this.traceId,
    this.isOffline = false,
    this.fieldErrors = const <String, List<String>>{},
  });

  /// Resolves a machine code into the sentence a user should read.
  ///
  /// The chain, in order and for a reason at each step:
  ///
  /// 1. `error.{code}` from the localisation registry — the only source that can be both in the
  ///    user's language and specific to what went wrong, and the only one operations can fix.
  /// 2. The server's own `detail`, already resolved against `Accept-Language`, which is correct for
  ///    the failures only the server can describe ("this registration is already listed").
  /// 3. The authored fallback. Generic, and never a stack trace or an exception's `toString`.
  factory AppFault.coded({
    required String code,
    required AppStrings strings,
    String? detail,
    int statusCode = 0,
    String? traceId,
    bool isOffline = false,
    Map<String, List<String>> fieldErrors = const <String, List<String>>{},
  }) {
    final String localised = 'error.$code';
    final String trimmed = (detail ?? '').trim();
    return AppFault(
      code: code,
      message: strings.has(localised)
          ? strings(localised)
          : (trimmed.isNotEmpty ? trimmed : strings(CustomerText.errorBody)),
      statusCode: statusCode,
      traceId: traceId,
      isOffline: isOffline,
      fieldErrors: fieldErrors,
    );
  }

  /// Builds from anything thrown below the UI.
  factory AppFault.of(Object error, AppStrings strings) {
    if (error is AppFault) return error;

    if (error is ApiException) {
      return AppFault.coded(
        code: error.code,
        strings: strings,
        detail: error.message,
        statusCode: error.statusCode,
        traceId: error.traceId,
        isOffline: error.isOffline,
        fieldErrors: error.fieldErrors,
      );
    }

    if (error is RegistryUnavailableException) {
      // A registry document this device has never seen, on a network that is not answering. The
      // honest instruction is "get signal once" — a retry button pointed at the same dead network
      // is a button that teaches the user the app is broken.
      return AppFault(
        code: 'platform.registry.unavailable',
        message: strings(CustomerText.errorOfflineBody),
        isOffline: true,
      );
    }

    return AppFault(code: 'app.unexpected', message: strings(CustomerText.errorBody));
  }

  /// The stable machine code — `{module}.{resource}.{condition}`. Branch on this, never on text.
  final String code;

  /// Authored, localised, user-facing. Displayed and never parsed.
  final String message;

  /// HTTP status, or `0` when nothing reached a server.
  final int statusCode;

  /// `traceparent`'s id, when the server sent one. The single most useful thing support can have.
  final String? traceId;

  /// The request never reached a server, or gave up waiting. Drives a different title: an outage
  /// is not a rejection, and telling a user their input was wrong when the tunnel was wrong is how
  /// a perfectly good registration number gets retyped four times.
  final bool isOffline;

  /// From a `422`. Carried so a form can put each one back on the field that caused it.
  final Map<String, List<String>> fieldErrors;

  /// What the copy action puts on the clipboard.
  ///
  /// Deliberately compact and mono-friendly: code, status, trace id, in the order support reads
  /// them. No message — the message is whatever the registry said this week, and the code is what
  /// the logs are indexed by.
  String get reference {
    final StringBuffer buffer = StringBuffer(code);
    if (statusCode > 0) buffer.write(' · $statusCode');
    final String? trace = traceId;
    if (trace != null && trace.isNotEmpty) buffer.write(' · $trace');
    return buffer.toString();
  }

  /// The shape `SchemaForm` reads field errors out of — RFC 9457's `metadata.fieldErrors`.
  ///
  /// Rebuilt rather than carried verbatim so the form's resolver sees exactly the structure the
  /// contract describes, whether the failure arrived through the transport or through the outbox.
  Map<String, Object?> get asProblem => <String, Object?>{
        'code': code,
        'status': statusCode,
        'metadata': <String, Object?>{'fieldErrors': fieldErrors},
      };

  @override
  String toString() => 'AppFault($reference)';
}
