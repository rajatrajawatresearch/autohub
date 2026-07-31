import 'package:freezed_annotation/freezed_annotation.dart';

part 'problem.freezed.dart';
part 'problem.g.dart';

/// A field-level failure, addressed by JSON Pointer into the request body.
@freezed
class ProblemFieldError with _$ProblemFieldError {
  const factory ProblemFieldError({
    required String code,

    /// `/kms_driven`, or `/attributes/boom_length_m` for a dynamic attribute.
    String? pointer,
    String? message,
    String? messageKey,
    Map<String, Object?>? params,
  }) = _ProblemFieldError;

  factory ProblemFieldError.fromJson(Map<String, Object?> json) =>
      _$ProblemFieldErrorFromJson(json);
}

/// RFC 9457 Problem Details, plus the AutoHub extension members.
///
/// **`code` is the only member a client may branch on.** `title` and `detail` are human text
/// resolved through the i18n registry and are reworded whenever a content person decides they read
/// badly. A client that branches on the message is a client that breaks silently the day somebody
/// improves the copy — and on a phone that break ships to every handset at once and cannot be
/// rolled back.
///
/// Every field is optional because a proxy, a load balancer or a crash handler can produce a `502`
/// that is not this shape at all, and the parse of an error must not itself fail.
@freezed
class ProblemDetails with _$ProblemDetails {
  const factory ProblemDetails({
    String? type,
    String? title,
    int? status,

    /// Stable machine code, `{module}.{resource}.{condition}`.
    String? code,
    String? detail,
    String? instance,
    String? traceId,
    String? requestId,
    String? timestamp,

    /// Present on 400/403/409/422. The schema form attaches each entry to a field by [pointer].
    List<ProblemFieldError>? errors,

    /// Whatever the condition needs the client to know: `currentState` on a workflow conflict,
    /// `fieldErrors` on a form validation failure.
    Map<String, Object?>? metadata,
    bool? retryable,
    int? retryAfterSeconds,
    String? helpUrl,
    String? supportReference,
  }) = _ProblemDetails;

  factory ProblemDetails.fromJson(Map<String, Object?> json) => _$ProblemDetailsFromJson(json);

  const ProblemDetails._();

  /// Field errors keyed the way a form keys its fields.
  ///
  /// Two sources, merged, because the contract populates both: the RFC's `errors` array with JSON
  /// pointers, and `metadata.fieldErrors` from `POST /platform/forms/{key}/validate`. A renderer
  /// that read only one of them would apply half the server's `422` and leave the user staring at a
  /// field that looks fine.
  Map<String, List<String>> get fieldErrors {
    final result = <String, List<String>>{};

    for (final entry in errors ?? const <ProblemFieldError>[]) {
      final pointer = entry.pointer ?? '';
      final key = pointer.startsWith('/') ? pointer.substring(1).replaceAll('/', '.') : pointer;
      if (key.isEmpty) continue;
      (result[key] ??= <String>[]).add(entry.message ?? entry.code);
    }

    final fromMetadata = metadata?['fieldErrors'];
    if (fromMetadata is Map<Object?, Object?>) {
      fromMetadata.forEach((key, value) {
        if (key is! String) return;
        final codes = switch (value) {
          final List<Object?> list => list.map((item) => '$item').toList(),
          null => <String>[],
          _ => <String>['$value'],
        };
        (result[key] ??= <String>[]).addAll(codes);
      });
    }

    return result;
  }
}
