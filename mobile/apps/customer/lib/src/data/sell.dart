import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/fault.dart';
import 'gesture_writer.dart';

/// The registry key of the form that creates supply. The only product knowledge in the sell flow.
const String sellFormKey = 'sell_vehicle';

/// Where the sell flow stands after the seller has tapped the submit button.
@immutable
sealed class SellState {
  const SellState();
}

/// Filling it in. Everything about what "it" contains is in the form definition, not here.
final class SellComposing extends SellState {
  const SellComposing();
}

/// The server took it. [listingId] is `null` only if the response did not name one.
final class SellSubmitted extends SellState {
  const SellSubmitted(this.listingId);

  final String? listingId;
}

/// On the phone, waiting for a network. **The draft is still on disk.**
final class SellQueued extends SellState {
  const SellQueued(this.idempotencyKey);

  final String idempotencyKey;
}

final NotifierProvider<SellController, SellState> sellProvider =
    NotifierProvider<SellController, SellState>(SellController.new);

/// The sell flow's write path, and nothing else.
///
/// This controller knows three facts about vehicles: the registry key of the form, the intent it
/// creates, and where the payload's category lives if the form asked for one. It knows no fields —
/// `SchemaForm` renders whatever `GET /platform/forms/sell_vehicle` returns, and this receives the
/// payload with hidden fields already stripped.
class SellController extends Notifier<SellState> {
  @override
  SellState build() {
    // A queued submission lands minutes or hours later, usually with the app in the background.
    // Watching the drain is what turns "saved on your phone" into "your vehicle is with our team"
    // without the seller having to come back and resubmit — and it is the only place the draft is
    // safe to discard, because until the server has it the draft is the only copy.
    final StreamSubscription<OutboxCompletion> subscription =
        ref.watch(outboxDrainerProvider).completions.listen(_onDrained);
    ref.onDispose(subscription.cancel);

    return const SellComposing();
  }

  /// Hands the payload to the server, or to the queue.
  ///
  /// Throws [FormSubmissionRejected] when the server refused it, which is the only way to get a
  /// `422`'s field errors back onto the fields that caused them and the user back to the step that
  /// holds the first one. Anything else — accepted, queued — resolves normally and the screen reads
  /// [state].
  ///
  /// The `Idempotency-Key` is minted **in this method**, on the tap, by [WriteIntent.now]. That is
  /// the line that stops a seller on a train from ending up with two listings for one car: the
  /// request that died in the tunnel and the row that drains on the other side carry the same key,
  /// so the server replays the original response instead of acting twice.
  Future<void> submit(FormValues payload) async {
    final String? city = ref.read(activeCityProvider).valueOrNull;

    final WriteIntent intent = WriteIntent.now(
      endpoint: '/listings',
      method: WriteMethod.post,
      body: <String, dynamic>{
        'intent': ListingIntent.sale.wire,
        // The category comes out of the payload because the form asked the seller for it. When the
        // form does not ask, the field is absent and the server applies the default for that form
        // key — guessing `car` here would list somebody's truck as a car.
        ...?_categoryOf(payload),
        ...?_registrationOf(payload),
        'values': payload,
      },
      headers: <String, String>{
        // Frozen at the gesture. A listing composed in Pune and drained after the user switched to
        // Mumbai belongs to Pune.
        if (city != null && city.isNotEmpty) 'X-Active-City': city,
      },
    );

    final SubmitResult result = await ref.read(gestureWriterProvider).submit(intent);

    switch (result) {
      case SubmitAccepted(:final Map<String, dynamic>? body):
        await _accepted(body);
      case SubmitQueued(:final String idempotencyKey):
        state = SellQueued(idempotencyKey);
      case SubmitRefused(:final AppFault fault):
        // Back to the form, which puts each field error under its own field. The values are
        // untouched: a seller told their registration is already listed must not also lose the six
        // photographs they just took.
        throw FormSubmissionRejected(
          problem: fault.asProblem,
          message: fault.fieldErrors.isEmpty ? fault.message : null,
        );
    }
  }

  /// Back to an empty form for the next vehicle.
  void reset() => state = const SellComposing();

  Future<void> _accepted(Map<String, dynamic>? body) async {
    final Object? id = body?['listingId'];
    // The draft has served its purpose the moment the server owns the data — and not one step
    // earlier. Discarding on *enqueue* would mean a poisoned write with nothing behind it.
    await ref.read(draftStoreProvider).discard(formKey: sellFormKey);
    state = SellSubmitted(id is String && id.isNotEmpty ? id : null);
  }

  void _onDrained(OutboxCompletion completion) {
    final SellState current = state;
    if (current is! SellQueued) return;
    if (completion.intent.idempotencyKey != current.idempotencyKey) return;
    unawaited(_accepted(completion.body));
  }

  static Map<String, dynamic>? _categoryOf(FormValues payload) {
    final Object? category = payload['category'] ?? payload['category_code'];
    if (category is! String || category.isEmpty) return null;
    return <String, dynamic>{'categoryCode': category};
  }

  static Map<String, dynamic>? _registrationOf(FormValues payload) {
    final Object? registration = payload['registration_number'];
    if (registration is! String || registration.isEmpty) return null;
    return <String, dynamic>{'registrationNumber': registration};
  }
}
