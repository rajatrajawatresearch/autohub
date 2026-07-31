import 'dart:convert';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/fault.dart';
import 'app_providers.dart';
import 'customer_api.dart';

/* ── Where a quote lives after it has been shown ───────────────────────────── */

/// Quotes the user has taken, kept on the phone that took them.
///
/// There is no `GET /valuation/quotes/{id}` in `docs/contracts/registry-api.md`, and inventing one
/// would be this client deciding a piece of the contract on its own. It is also the wrong answer:
/// a valuation is *this person's* question about *their* car, taken without signing in, and asking
/// a server to hand it back requires knowing who is asking. Keeping it locally makes `/valuation/{id}`
/// deep-linkable, instant, and readable in a basement — and it is what lets the garage show a
/// signed-out user the vehicles they have asked about.
///
/// Stored in the preferences table rather than the keychain: a valuation is not a secret, and a
/// keychain read is a platform channel hop on a path the garage hits on every open.
final class ValuationVault {
  const ValuationVault(this._preferences);

  /// A JSON array of quote ids, newest first. The preferences table is a flat key-value store, so
  /// the index is what makes "the last few" answerable without a scan.
  static const String indexKey = 'valuation.index';

  /// Enough to cover a person comparing a few cars, small enough that the index stays one small
  /// row. Anything older has an `expiresAt` in the past and is a stale number, not history.
  static const int limit = 12;

  final DevicePreferences _preferences;

  static String quoteKey(String quoteId) => 'valuation.quote.$quoteId';

  Future<void> save(ValuationQuote quote) async {
    await _preferences.write(quoteKey(quote.quoteId), jsonEncode(quote.toJson()));

    final List<String> index = await _readIndex();
    final List<String> next = <String>[
      quote.quoteId,
      ...index.where((String id) => id != quote.quoteId),
    ];

    // Evict past the cap here rather than lazily, so a phone that valued forty cars does not carry
    // forty JSON blobs it will never read again.
    for (final String stale in next.skip(limit)) {
      await _preferences.remove(quoteKey(stale));
    }

    await _preferences.write(indexKey, jsonEncode(next.take(limit).toList()));
  }

  Future<ValuationQuote?> read(String quoteId) async {
    final String? raw = await _preferences.read(quoteKey(quoteId));
    return _decode(raw);
  }

  /// Newest first. Skips anything that will not decode — a quote written by an older build is a
  /// missing card, never a garage that fails to open.
  Future<List<ValuationQuote>> recent() async {
    final List<ValuationQuote> quotes = <ValuationQuote>[];
    for (final String id in await _readIndex()) {
      final ValuationQuote? quote = await read(id);
      if (quote != null) quotes.add(quote);
    }
    return quotes;
  }

  /// Fires whenever the index changes, so the garage updates the moment a quote is taken on the
  /// home screen without either screen knowing about the other.
  Stream<String?> watchIndex() => _preferences.watch(indexKey);

  Future<List<String>> _readIndex() async {
    final String? raw = await _preferences.read(indexKey);
    if (raw == null || raw.isEmpty) return const <String>[];
    try {
      final Object? decoded = jsonDecode(raw);
      if (decoded is! List) return const <String>[];
      return <String>[
        for (final Object? entry in decoded)
          if (entry is String) entry,
      ];
    } on FormatException {
      return const <String>[];
    }
  }

  static ValuationQuote? _decode(String? raw) {
    if (raw == null || raw.isEmpty) return null;
    try {
      final Object? decoded = jsonDecode(raw);
      if (decoded is! Map<String, Object?>) return null;
      return ValuationQuote.fromJson(decoded);
    } on Object {
      // Broad, and the cost of being wrong is one missing card. A `FormatException` covers a
      // truncated write; a `TypeError` covers a body shaped by a contract this build cannot read.
      return null;
    }
  }
}

final Provider<ValuationVault> valuationVaultProvider =
    Provider<ValuationVault>((ref) => ValuationVault(ref.watch(devicePreferencesProvider)));

/// One saved quote, for `/valuation/{quoteId}` reached cold from a deep link.
final FutureProviderFamily<ValuationQuote?, String> savedValuationProvider =
    FutureProvider.family<ValuationQuote?, String>(
  (ref, quoteId) => ref.watch(valuationVaultProvider).read(quoteId),
);

/// Every saved quote, newest first. The garage's "your vehicles".
final FutureProvider<List<ValuationQuote>> savedValuationsProvider =
    FutureProvider<List<ValuationQuote>>((ref) async {
  final ValuationVault vault = ref.watch(valuationVaultProvider);
  // Re-reads whenever a quote is saved anywhere in the app.
  ref.watch(valuationVaultIndexProvider);
  return vault.recent();
});

final StreamProvider<String?> valuationVaultIndexProvider =
    StreamProvider<String?>((ref) => ref.watch(valuationVaultProvider).watchIndex());

/* ── The hero's state machine ──────────────────────────────────────────────── */

/// Where the home screen's one interaction stands.
///
/// Sealed, because the four cases render four different things in the *same place* on the page and
/// the compiler refusing a switch that forgets one is what keeps the reveal from ever being a
/// route of its own.
@immutable
sealed class ValuationHeroState {
  const ValuationHeroState();
}

final class ValuationIdle extends ValuationHeroState {
  const ValuationIdle();
}

final class ValuationWorking extends ValuationHeroState {
  const ValuationWorking();
}

final class ValuationRevealed extends ValuationHeroState {
  const ValuationRevealed(this.quote);

  final ValuationQuote quote;
}

final class ValuationFailed extends ValuationHeroState {
  const ValuationFailed(this.fault);

  final AppFault fault;
}

final NotifierProvider<ValuationHeroController, ValuationHeroState> valuationHeroProvider =
    NotifierProvider<ValuationHeroController, ValuationHeroState>(ValuationHeroController.new);

/// The one call the home screen makes, and the reason the answer never navigates.
class ValuationHeroController extends Notifier<ValuationHeroState> {
  @override
  ValuationHeroState build() => const ValuationIdle();

  /// Asks for a quote and reveals it in place.
  ///
  /// **Not queued, and deliberately so.** The offline queue's promise — *your write will land
  /// eventually* — is worthless here: a valuation delivered twenty minutes later describes a market
  /// that has moved to a user who has closed the app, and the whole product claim is "thirty
  /// seconds". So it meets the network now or it fails in front of the person who tapped, which is
  /// the same reasoning `ImmediateWriter` documents for bids, one notch less severe.
  ///
  /// The idempotency key is still minted **on the gesture**, and that is what the throwaway
  /// [WriteIntent] is for. A dead cell mid-request means the client cannot tell "the server never
  /// saw it" from "the quote was created and the response was lost"; a retry carrying the same key
  /// replays the original answer instead of creating a second quote record and a second follow-up
  /// call to the seller.
  Future<void> submit({required String registrationNumber, int? kmsDriven}) async {
    if (state is ValuationWorking) return;

    final String? city = ref.read(activeCityProvider).valueOrNull;
    final ValuationQuoteRequest request = ValuationQuoteRequest(
      registrationNumber: registrationNumber,
      kmsDriven: kmsDriven,
      city: city,
    );

    final WriteIntent gesture = WriteIntent.immediateNow(
      endpoint: '/valuation/quotes',
      method: WriteMethod.post,
      body: request.toJson(),
    );

    state = const ValuationWorking();

    try {
      final CustomerApi api = ref.read(customerApiProvider);
      final ValuationQuote quote = await api.valuationQuote(
        request,
        idempotencyKey: gesture.idempotencyKey,
      );
      // Saved before it is shown. A user who backgrounds the app on the reveal and comes back an
      // hour later finds the number in their garage rather than having to type the plate again.
      await ref.read(valuationVaultProvider).save(quote);
      state = ValuationRevealed(quote);
    } on Object catch (error) {
      state = ValuationFailed(ref.read(diagnosticsLogProvider.notifier).capture(error));
    }
  }

  /// Back to the empty plate. The label says "Check another vehicle" because that is what it does.
  void reset() => state = const ValuationIdle();

  /// Puts a quote that came from somewhere else — the garage, a deep link — into the hero.
  void reveal(ValuationQuote quote) => state = ValuationRevealed(quote);
}
