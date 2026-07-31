import 'package:autohub_core/autohub_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app_providers.dart';
import 'provisional_models.dart';

/// The signed-in user's own listings.
///
/// Deliberately the same `SearchItem` projection a search hit uses, so the same `VehicleCard`
/// renders both. A seller looking at their own car in their garage and a buyer looking at it in
/// search are looking at the same object, and two card widgets is how the two views drift apart.
final FutureProvider<CursorPage<SearchItem>> myListingsProvider =
    FutureProvider<CursorPage<SearchItem>>((ref) async {
  try {
    return await ref.watch(customerApiProvider).myListings(const ListingListParams(limit: 20));
  } on Object catch (error) {
    throw ref.read(diagnosticsLogProvider.notifier).capture(error);
  }
});

/// Test drives, offers and deals in flight.
final FutureProvider<CursorPage<GarageBooking>> myBookingsProvider =
    FutureProvider<CursorPage<GarageBooking>>((ref) async {
  try {
    return await ref.watch(customerApiProvider).myBookings(limit: 20);
  } on Object catch (error) {
    throw ref.read(diagnosticsLogProvider.notifier).capture(error);
  }
});

/// Writes still on this phone, live.
///
/// Surfaced in the garage rather than hidden, because a seller whose listing is sitting in a queue
/// has a right to know it has not gone up yet — and because the alternative is a user who submitted
/// on a train, saw nothing appear, and submitted again from the office.
final StreamProvider<List<OutboxEntry>> pendingWritesProvider =
    StreamProvider<List<OutboxEntry>>((ref) => ref.watch(outboxProvider).watch());
