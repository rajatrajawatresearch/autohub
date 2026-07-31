import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/fault.dart';
import '../i18n/customer_text.dart';
import 'app_providers.dart';
import 'gesture_writer.dart';
import 'provisional_models.dart';

/// One listing, by id.
///
/// The failure is resolved into an [AppFault] here rather than in the screen, because resolving it
/// needs the localisation registry and the diagnostics log — and reading a provider's notifier
/// during a widget build to record a fault is exactly the mutation Riverpod refuses.
final FutureProviderFamily<ListingDetail, String> listingProvider =
    FutureProvider.family<ListingDetail, String>((ref, listingId) async {
  try {
    return await ref.watch(customerApiProvider).listing(listingId);
  } on Object catch (error) {
    throw ref.read(diagnosticsLogProvider.notifier).capture(error);
  }
});

/* ── The sticky action bar ─────────────────────────────────────────────────── */

/// What the bar at the bottom of a listing is doing.
@immutable
sealed class ListingActionState {
  const ListingActionState();
}

final class ListingActionIdle extends ListingActionState {
  const ListingActionIdle();
}

final class ListingActionBusy extends ListingActionState {
  const ListingActionBusy();
}

/// The server accepted it. [message] is authored text the bar shows in place of the controls.
final class ListingActionDone extends ListingActionState {
  const ListingActionDone(this.message);

  final String message;
}

/// On the phone until there is a network. Distinct from done, because the seller has not been told
/// anything yet and saying otherwise is a lie the user finds out about later.
final class ListingActionQueued extends ListingActionState {
  const ListingActionQueued(this.message);

  final String message;
}

final class ListingActionRefused extends ListingActionState {
  const ListingActionRefused(this.fault, {this.needsSignIn = false});

  final AppFault fault;
  final bool needsSignIn;
}

final NotifierProviderFamily<ListingActionController, ListingActionState, String>
    listingActionProvider =
    NotifierProvider.family<ListingActionController, ListingActionState, String>(
  ListingActionController.new,
);

/// The two things a buyer can do to a listing, and both are writes that must survive a tunnel.
///
/// Note what this controller does **not** do: decide which buttons exist. The primary action on a
/// listing is whatever `workflow.availableActions` says it is, labelled by the server, with its
/// emphasis set by the registry — see `listing_screen.dart`. A client that decided "live listings
/// get a Book test drive button" would be a client that disagrees with the server the first time
/// operations adds a step, on handsets that cannot be corrected without an app-store release.
class ListingActionController extends FamilyNotifier<ListingActionState, String> {
  @override
  ListingActionState build(String listingId) => const ListingActionIdle();

  /// Fires a workflow transition the server offered.
  ///
  /// The body is exactly §3's: an `actionCode` and, when the transition demands one, a reason.
  /// Nothing is derived from the state code — the app never re-derives authorisation, because
  /// guards and permissions were evaluated server-side before this action was offered at all.
  Future<void> fire(AvailableAction action, {String? reason}) {
    return _submit(
      WriteIntent.now(
        endpoint: '/listings/${Uri.encodeComponent(arg)}/workflow/actions',
        method: WriteMethod.post,
        body: <String, dynamic>{
          'actionCode': action.actionCode,
          if (reason != null && reason.trim().isNotEmpty) 'reason': reason.trim(),
        },
      ),
      doneKey: CustomerText.listingActionDone,
      queuedKey: CustomerText.listingOfferQueued,
    );
  }

  /// Makes an offer on the listing.
  ///
  /// [amountMinor] is integer paise. Never a `double` and never rupees — an offer is a figure the
  /// buyer will be held to, and a marketplace that accumulates offers in binary floating point
  /// reconciles short at the end of the month.
  Future<void> makeOffer({required int amountMinor, String? message}) {
    return _submit(
      WriteIntent.now(
        endpoint: '/deals/offers',
        method: WriteMethod.post,
        body: OfferRequest(
          listingId: arg,
          amountMinor: amountMinor,
          message: message,
        ).toJson(),
      ),
      doneKey: CustomerText.listingOfferSent,
      queuedKey: CustomerText.listingOfferQueued,
    );
  }

  void reset() => state = const ListingActionIdle();

  Future<void> _submit(
    WriteIntent intent, {
    required String doneKey,
    required String queuedKey,
  }) async {
    if (state is ListingActionBusy) return;
    state = const ListingActionBusy();

    final AppStrings t = ref.read(appStringsProvider);
    final SubmitResult result = await ref.read(gestureWriterProvider).submit(intent);

    switch (result) {
      case SubmitAccepted():
        // The server's answer to a workflow action is a fresh snapshot. Rather than fold it in
        // here, the listing is invalidated so the whole page — status pill, timeline, remaining
        // actions — comes back from the server rather than being guessed at locally.
        ref.invalidate(listingProvider(arg));
        state = ListingActionDone(t(doneKey));
      case SubmitQueued():
        state = ListingActionQueued(t(queuedKey));
      case SubmitRefused(:final AppFault fault, :final bool needsSignIn):
        state = ListingActionRefused(fault, needsSignIn: needsSignIn);
    }
  }
}
