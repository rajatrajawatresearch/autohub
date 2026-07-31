/// Turning a `422` back into something a user can act on.
///
/// The contract says the server answers a failed validation with codes and nothing else:
///
/// ```jsonc
/// { "status": 422, "code": "validation.failed",
///   "metadata": { "fieldErrors": { "kms_driven": ["kms.too_high"] } } }
/// ```
///
/// That is the right division of authority and it is worth stating why. The **server** is the
/// authority on *what* is wrong — it can see the duplicate registration, the dealer's tier, the
/// rate limit. The **registry** is the authority on *how we say it*, in the user's language, in the
/// product's voice. So a fault raised by the server is rendered from the same authored sentence a
/// fault raised on the phone would have used, and a user cannot tell which side caught it.
///
/// If the client invented a sentence here — "Server rejected kms_driven" — the same rule would read
/// two different ways depending on which validator happened to fire first, and support would be
/// fielding screenshots of a message nobody wrote.
library;

import 'package:autohub_core/autohub_core.dart';

import 'field_config.dart';
import 'types.dart';

/// Pulls `metadata.fieldErrors` out of an RFC 9457 Problem Details body.
///
/// Tolerant on the way in: the body may be missing, `metadata` may be absent, and a field's entry
/// may be a single string rather than a list. None of that is worth an exception on a screen whose
/// job at that moment is to explain a failure.
Map<String, Object?> fieldErrorsFromProblem(Object? problem) {
  if (problem is! Map) return const <String, Object?>{};
  final Object? metadata = problem['metadata'];
  if (metadata is! Map) return const <String, Object?>{};
  final Object? fieldErrors = metadata['fieldErrors'];
  if (fieldErrors is! Map) return const <String, Object?>{};
  return <String, Object?>{
    for (final MapEntry<Object?, Object?> entry in fieldErrors.entries)
      if (entry.key is String) entry.key! as String: entry.value,
  };
}

/// Resolves the server's code-only answer into displayable faults.
///
/// Each code is looked up against the field's own authored rules. A code the definition does not
/// know — a rule that is `custom` and server-side by contract, or a rule newer than this build —
/// still surfaces, carrying its code and the generic authored fallback, because a fault the user
/// can see and quote is worth far more than a fault silently dropped on the floor.
///
/// Only the first code per field is kept: one fault per field is the same rule the local validator
/// follows, and a field wearing three messages at once is a field nobody reads.
Map<String, FieldFault> resolveServerFieldErrors(
  FormDefinition definition,
  Map<String, Object?> fieldErrors, {
  FormLabels labels = const FormLabels(),
}) {
  final Map<String, FieldFault> resolved = <String, FieldFault>{};

  for (final MapEntry<String, Object?> entry in fieldErrors.entries) {
    final Object? raw = entry.value;
    final List<Object?> codes = raw is List ? raw : <Object?>[raw];
    if (codes.isEmpty) continue;

    final Object? first = codes.first;
    if (first == null) continue;
    final String code = first is String ? first : '$first';
    if (code.isEmpty) continue;

    final FormField? field = definition.fieldFor(entry.key);
    String? authored;
    if (field != null) {
      for (final ValidationRule rule in field.validation) {
        if (rule.code == code) {
          authored = rule.message;
          break;
        }
      }
    }

    resolved[entry.key] = FieldFault(
      code: code,
      message: authored ?? labels(FormLabelKeys.validationInvalid),
    );
  }

  return resolved;
}

/// The index of the first step holding any of [keys], or `-1`.
///
/// A 422 routinely names a field on a step the user has already walked past — the phone number they
/// typed three screens ago, rejected because it belongs to a blocked account. Leaving them on the
/// last step with an error about a field they cannot see is how a submission is abandoned. This is
/// what lets the form take them back to it.
int stepIndexForFields(List<FormStep> steps, Iterable<String> keys) {
  final Set<String> wanted = keys.toSet();
  if (wanted.isEmpty) return -1;
  for (int index = 0; index < steps.length; index += 1) {
    for (final FormField field in steps[index].fields) {
      if (wanted.contains(field.key)) return index;
    }
  }
  return -1;
}

/// Convenience for the common call: a `422` body straight from the transport, applied to a form.
///
/// Returns an empty map when the body carries no field errors — which is the case for a `409`
/// or a gateway timeout, and the caller should then show [FormLabelKeys.validationInvalid]'s
/// sibling: a form-level message, not a field-level one.
Map<String, FieldFault> faultsFromProblem(
  FormDefinition definition,
  Object? problem, {
  FormLabels labels = const FormLabels(),
}) =>
    resolveServerFieldErrors(
      definition,
      fieldErrorsFromProblem(problem),
      labels: labels,
    );
