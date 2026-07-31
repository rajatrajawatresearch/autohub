import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// Everything that can go wrong, as one thing a screen can render.
///
/// The app deals with three unrelated failure vocabularies — `ApiException` from the transport,
/// `RegistryUnavailableException` from the cache, and the six `WriteOutcome` cases from the offline
/// queue — and a screen that branches on all three grows three error paths, one of which is always
/// the one nobody tested. This normalises them at the seam, and it keeps exactly two facts:
///
/// * [message], which the user reads. Server prose, resolved through the i18n registry.
/// * [code], which the user does **not** read but support does. See [supportReference].
///
/// The one thing it never does is compose a sentence out of a code. `attachment.vehicle.already_
/// committed` is a machine string; turning it into "Your vehicle is already committed" in Dart puts
/// a translation in the binary and guarantees it disagrees with the server's own wording the first
/// time a content person edits it.
@immutable
final class PartnerFailure {
  const PartnerFailure({
    required this.message,
    required this.code,
    this.traceId,
    this.isOffline = false,
    this.fieldErrors = const <String, List<String>>{},
  });

  /// Normalises whatever was thrown.
  ///
  /// The `Object` parameter is deliberate: this sits under `AsyncValue.error`, which carries
  /// whatever a provider threw, and a provider can throw a `TypeError` from a decode as easily as
  /// an `ApiException`. Narrowing the parameter would only move the `is` checks to every call site.
  factory PartnerFailure.from(Object error) {
    if (error is PartnerFailure) return error;

    if (error is ApiException) {
      return PartnerFailure(
        message: error.message,
        code: error.code,
        traceId: error.traceId,
        isOffline: error.isOffline,
        fieldErrors: error.fieldErrors,
      );
    }

    if (error is RegistryUnavailableException) {
      // Deliberately reported as offline. The only way to reach this is "this device has never
      // cached the document and cannot fetch it now", which for the user is indistinguishable from
      // — and is usually caused by — having no signal. The right affordance is a retry.
      return const PartnerFailure(
        message: 'Some of the app has not downloaded yet. Reconnect and try again.',
        code: 'platform.registry.unavailable',
        isOffline: true,
      );
    }

    // Anything else is a defect in this build rather than a condition the server reported. It still
    // gets a code, because a screenshot with `partner.client.unexpected` on it is a bug report and
    // a screenshot of a bare sentence is a guess.
    return const PartnerFailure(
      message: 'Something went wrong in the app.',
      code: 'partner.client.unexpected',
    );
  }

  /// Turns a queued write's outcome into a failure, or `null` when there is nothing to report.
  ///
  /// Exhaustive over `WriteOutcome` on purpose — `non_exhaustive_switch_expression` is an error in
  /// this workspace precisely so that a seventh outcome cannot slip through as a silent success.
  static PartnerFailure? fromOutcome(WriteOutcome outcome) => switch (outcome) {
        WriteAccepted() => null,
        // A 409 is not a retry. The entity moved, the server said what it is now, and the screen
        // has to ask the user rather than re-apply an intent formed against a state that is gone.
        final WriteConflicted conflict => PartnerFailure(
            message: conflict.detail ?? 'This changed while you were away. Reload and try again.',
            code: conflict.code,
          ),
        final WriteRejected rejected => PartnerFailure(
            message: rejected.detail ?? 'The details need fixing before this can be sent.',
            code: rejected.code,
            fieldErrors: rejected.fieldErrors,
          ),
        final WriteUnauthenticated auth => PartnerFailure(
            message: 'Sign in again to send this. Nothing was lost.',
            code: auth.code,
          ),
        final WriteFailedServer server => PartnerFailure(
            message: 'AutoHub could not take that just now. It will be retried.',
            code: server.code,
          ),
        final WriteUnreachable unreachable => PartnerFailure(
            message: 'No connection. It is queued and will be sent when there is signal.',
            code: unreachable.reason,
            isOffline: true,
          ),
      };

  /// Human text. Displayed, never branched on.
  final String message;

  /// The stable machine code, `{module}.{resource}.{condition}`.
  final String code;

  final String? traceId;

  /// The request never reached a server. The distinction the UI needs: a retry is worth offering,
  /// and nothing the user typed is at fault.
  final bool isOffline;

  /// Keyed the way a schema form keys its fields, so a `422` lands back on the field that caused it.
  final Map<String, List<String>> fieldErrors;

  /// The one string support asks for, and the only place [code] is meant to be seen.
  ///
  /// It is rendered — small, mono, secondary — rather than hidden behind a "details" disclosure,
  /// because the realistic support path in this market is a WhatsApp screenshot. A code behind a
  /// tap is a code that is never in the screenshot, and the ticket becomes "it said something went
  /// wrong". Present but quiet costs the user nothing and saves the round trip.
  String get supportReference => traceId == null ? code : '$code · $traceId';

  @override
  String toString() => 'PartnerFailure($supportReference): $message';
}
