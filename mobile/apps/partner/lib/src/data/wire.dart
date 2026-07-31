/// Coercing readers for the hand-written models in this app.
///
/// Hand-written rather than generated for the reason `docs/10-mobile-architecture.md` §5 gives:
/// there is no OpenAPI document yet, so every model in this workspace is written against the
/// contract by hand and deleted the day the generator exists. What that costs is this file.
///
/// Every reader **coerces and never throws**. A field that arrives as `"26"` where the contract
/// says `26` is a server that widened a type, not a reason to blank a partner's settlement screen;
/// a field that arrives as an object where a string was promised yields `null`, and the widget
/// above renders an em dash. The one exception is a genuinely required identifier, which is left to
/// throw — a contract row with no id is not a row this app can navigate to, and failing at the
/// decode seam turns it into one diagnosable `common.response.malformed` instead of a null
/// dereference three screens later.
library;

import 'package:autohub_core/autohub_core.dart';

String? str(Object? value) {
  if (value is String) return value.isEmpty ? null : value;
  if (value is num || value is bool) return '$value';
  return null;
}

String requiredStr(Object? value, String field) {
  final String? parsed = str(value);
  if (parsed == null) throw FormatException('Missing "$field".', '$value');
  return parsed;
}

int? intOrNull(Object? value) {
  if (value is int) return value;
  if (value is num) return value.isFinite ? value.round() : null;
  if (value is String) return num.tryParse(value.trim())?.round();
  return null;
}

int intOr(Object? value, int fallback) => intOrNull(value) ?? fallback;

double? doubleOrNull(Object? value) {
  if (value is num) return value.isFinite ? value.toDouble() : null;
  if (value is String) return double.tryParse(value.trim());
  return null;
}

bool boolOr(Object? value, {required bool fallback}) {
  if (value is bool) return value;
  if (value is num) return value != 0;
  if (value is String) return value == 'true';
  return fallback;
}

Map<String, Object?> mapOf(Object? value) {
  if (value is Map<String, Object?>) return value;
  if (value is Map<Object?, Object?>) {
    return <String, Object?>{
      for (final MapEntry<Object?, Object?> entry in value.entries)
        if (entry.key is String) entry.key! as String: entry.value,
    };
  }
  return const <String, Object?>{};
}

List<Object?> listOf(Object? value) => value is List<Object?> ? value : const <Object?>[];

/// Decodes a list of models, dropping the entries that will not decode.
///
/// Dropping rather than failing is the lesson `autohub_core`'s `Media` model records in its own
/// doc comment: one malformed row must not take down the page that contains it. On this app that
/// page is the settlement list, and a partner who opens it to check a payout and finds it empty
/// concludes they have not been paid.
List<T> decodeAll<T>(Object? value, T Function(Map<String, Object?> json) decode) {
  final List<T> decoded = <T>[];
  for (final Object? entry in listOf(value)) {
    try {
      decoded.add(decode(mapOf(entry)));
    } on Object {
      continue;
    }
  }
  return decoded;
}

// The §0 cursor envelope used to be declared here. It is a pinned contract shape, so it now lives
// once in `autohub_core` as `CursorPage<T>` and arrives through the barrel this file imports —
// same `fromJson`, same `isEmpty`, same "drop the row that will not decode" trade.

/// The workflow state of an entity, as the registry declares it.
///
/// Reuses `autohub_core`'s [WorkflowState] rather than re-declaring `{code, name, category}` in
/// this app: one Dart definition of a contract shape, and the category decoding — including its
/// fallback to `in_progress`, matched to the web's — comes with it.
WorkflowState workflowStateOf(Object? value) => WorkflowState.fromJson(mapOf(value));
