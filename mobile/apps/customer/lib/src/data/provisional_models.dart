/// Read shapes the contract has not pinned yet.
///
/// **PROVISIONAL, and deliberately quarantined in the app rather than added to `autohub_core`.**
/// `docs/contracts/registry-api.md` pins the search projection (§9), the listing detail's
/// constituent shapes, the workflow snapshot and the auth surface. It does not yet pin the
/// cursor-page envelope for `GET /listings` or the deal read model behind `GET /deals/deals` —
/// `packages/api/src/schemas/deals.ts` carries the same note for the same reason.
///
/// Two rules keep this file from becoming a second contract:
///
/// 1. **Nothing here invents a field the contract does not already describe.** Every one of these
///    is composed from shapes `autohub_core` owns — `SearchItem`, `WorkflowState`, the cursor
///    envelope from §0 — so reconciling with the published endpoint is additive rather than a
///    rewrite.
/// 2. **Nothing here encodes a business fact.** No deal states, no side vocabulary, no fee names.
///    A deal's state arrives as a registry `WorkflowState` and is rendered through `StatusPill`
///    from its `category`, exactly as everything else is.
///
/// When the contract pins them, these move into `autohub_core` as freezed models and this file is
/// deleted. Hand-written decoders until then, because generating two models for one shape is how
/// two clients end up disagreeing about it.
library;

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

Map<String, Object?> _record(Object? value) {
  if (value is Map) {
    return <String, Object?>{
      for (final MapEntry<Object?, Object?> entry in value.entries)
        if (entry.key is String) entry.key! as String: entry.value,
    };
  }
  return const <String, Object?>{};
}

List<Object?> _list(Object? value) => value is List ? value : const <Object?>[];

String? _string(Object? value) {
  if (value is String) return value;
  if (value is num || value is bool) return '$value';
  return null;
}

int? _int(Object? value) {
  if (value is num) return value.isFinite ? value.round() : null;
  if (value is String) return num.tryParse(value.trim())?.round();
  return null;
}

/// The cursor-paginated envelope from §0 of the contract.
///
/// Generic because the envelope is, and because a page of listings and a page of bookings differ
/// only in what they carry. `totalEstimate` keeps its name: it is an estimate by contract and never
/// a `COUNT(*)`, so the UI says "about" and never promises precision the server did not offer.
@immutable
final class CursorPage<T> {
  const CursorPage({
    this.items = const <Never>[],
    this.nextCursor,
    this.totalEstimate,
  });

  /// Decodes an envelope, skipping any item that will not parse.
  ///
  /// Skipping rather than failing is the same trade `autohub_core` makes at every decode seam and
  /// it was learned expensively on the web: one malformed row must not blank a whole page. A
  /// listing missing from a list is a support question; an empty garage looks like data loss.
  factory CursorPage.fromJson(
    Map<String, Object?> json,
    T Function(Map<String, Object?> item) read,
  ) {
    final List<T> items = <T>[];
    for (final Object? entry in _list(json['items'])) {
      if (entry is! Map) continue;
      try {
        items.add(read(_record(entry)));
      } on Object {
        continue;
      }
    }
    return CursorPage<T>(
      items: items,
      nextCursor: _string(json['nextCursor']),
      totalEstimate: _int(json['totalEstimate']),
    );
  }

  final List<T> items;

  /// `null` when the list is exhausted. The infinite scroll stops on this, never on an item count.
  final String? nextCursor;

  final int? totalEstimate;

  bool get hasMore => (nextCursor ?? '').isNotEmpty;
}

/// One thing the customer has agreed to: a test drive, an accepted offer, a booked inspection.
///
/// Shown in the garage. The state is a registry [WorkflowState] rather than a string this app
/// recognises, so an operations user inserting "Awaiting documents" into the deal workflow renders
/// correctly on a handset that shipped months earlier.
@immutable
final class GarageBooking {
  const GarageBooking({
    required this.dealId,
    required this.reference,
    required this.listing,
    required this.state,
    this.amountMinor,
    this.currency = 'INR',
    this.counterpartyName,
    this.createdAt,
  });

  factory GarageBooking.fromJson(Map<String, Object?> json) => GarageBooking(
        dealId: _string(json['dealId']) ?? '',
        reference: _string(json['reference']) ?? '',
        listing: SearchItem.fromJson(_record(json['listing'])),
        state: WorkflowState.fromJson(_record(json['state'])),
        amountMinor: _int(json['amountMinor']),
        currency: _string(json['currency']) ?? 'INR',
        counterpartyName: _string(_record(json['counterparty'])['displayName']),
        createdAt: _string(json['createdAt']),
      );

  final String dealId;

  /// The human reference a customer reads out on the phone to support. Mono, always.
  final String reference;

  /// The vehicle, in the same projection a search hit uses — so the same card renders it.
  final SearchItem listing;

  final WorkflowState state;

  /// Integer minor units. Nullable: a booked test drive has no amount.
  final int? amountMinor;
  final String currency;

  final String? counterpartyName;
  final String? createdAt;
}

/// The body of `POST /deals/offers`, per `packages/api/src/schemas/deals.ts`.
@immutable
final class OfferRequest {
  const OfferRequest({
    required this.listingId,
    required this.amountMinor,
    this.message,
  });

  final String listingId;

  /// Integer paise. Never a `double` — an offer is a figure the customer will be held to.
  final int amountMinor;

  final String? message;

  Map<String, Object?> toJson() => <String, Object?>{
        'listingId': listingId,
        'amountMinor': amountMinor,
        if (message != null && message!.trim().isNotEmpty) 'message': message!.trim(),
      };
}
