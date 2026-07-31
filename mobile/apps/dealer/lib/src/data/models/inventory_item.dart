import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// One row of a dealer's stock list.
///
/// **PROVISIONAL**, in exactly the sense `ListingDetail` and `packages/api/src/schemas/deals.ts`
/// use the word: `contracts/registry-api.md` pins the *search* projection (§9) and the dealer
/// console needs two things that projection does not carry — when the vehicle entered stock, and
/// how much demand it has attracted. Both arrive on the same `/listings` object from the server
/// that the three web portals already run against.
///
/// It **wraps** [ListingSummary] rather than restating its fields. Restating them is how two
/// projections of one listing drift: someone adds `pricePeriod` to the contract shape, the copy
/// here does not get it, and a rental in a dealer's stock renders its monthly figure as an asking
/// price. One decode, one shape, one card.
///
/// Every dealer-console addition is nullable and decoded defensively. A server that does not send
/// `createdAt` renders an em dash in the age column; it does not fail the row, and it certainly
/// does not fail the page.
@immutable
final class InventoryItem {
  const InventoryItem({
    required this.listing,
    this.listedAt,
    this.leadCount,
    this.state,
  });

  /// The contract's card projection, decoded by `autohub_core`. Identical to a search hit, which
  /// is why the same card widget renders both.
  final ListingSummary listing;

  /// When the vehicle entered stock, per the server. Drives the age column and the ageing buckets.
  final DateTime? listedAt;

  /// Enquiries against this listing. Null means "the server did not say", which is a different
  /// fact from zero and renders differently.
  final int? leadCount;

  /// The listing's workflow state as the server resolved it — `code`, `name` and `category`.
  ///
  /// Kept as the registry's own [WorkflowState] rather than a string so the row's chip derives its
  /// tone from `category` and a state added to the listing workflow on a Tuesday renders correctly
  /// on a handset that shipped in March.
  final WorkflowState? state;

  String get listingId => listing.listingId;

  /// Whole days in stock, or null when the server did not date the row.
  ///
  /// Deliberately **not** measured against a server clock. The auction countdown is corrected for
  /// device drift because seconds decide who wins a truck; an ageing bucket is measured in days and
  /// a handset two minutes out lands in the same bucket either way. Paying for a clock handshake
  /// here would buy nothing and hide the one place the correction genuinely matters.
  int? ageInDaysAt(DateTime now) {
    final since = listedAt;
    if (since == null) return null;
    final days = now.toUtc().difference(since.toUtc()).inDays;
    return days < 0 ? 0 : days;
  }

  static InventoryItem fromJson(JsonMap json) {
    final listedAt = json['createdAt'];
    final leads = json['leadCount'];
    final state = json['state'];

    return InventoryItem(
      listing: SearchItem.fromJson(json),
      listedAt: listedAt is String ? DateTime.tryParse(listedAt) : null,
      leadCount: leads is int ? leads : null,
      // A malformed state object costs the chip, not the row. The alternative — letting it throw —
      // drops a vehicle out of a dealer's own stock list, which they will read as the platform
      // having lost their car.
      state: state is Map<String, Object?> ? _stateOrNull(state) : null,
    );
  }

  static WorkflowState? _stateOrNull(Map<String, Object?> json) {
    try {
      return WorkflowState.fromJson(json);
    } on Object {
      return null;
    }
  }
}

/// The ageing buckets a dealer's working capital is actually organised around.
///
/// These are a **reporting grouping**, not a business rule the platform enforces: nothing on the
/// server changes at day 61, and no fee, no permission and no workflow transition reads them. They
/// exist because "how much of my money is sitting in stock that is not moving" is the question a
/// used-car dealer asks every morning, and 30/60/90 is how the trade asks it.
///
/// If the platform ever publishes an ageing policy — a registry that says this dealer's floor plan
/// charges interest from day 45 — this list is the single line that goes away, replaced by a
/// registry read. It is written as data for exactly that reason.
const List<int> ahAgeingBucketEdges = <int>[30, 60, 90];

/// Counts stock into [ahAgeingBucketEdges] plus a final open-ended bucket.
///
/// Rows with no date land in [undated] rather than being silently counted as new. A vehicle whose
/// age the server did not report is not a fresh vehicle, and folding it into the 0–30 bucket would
/// make a stale lot look healthy on the one screen built to catch exactly that.
@immutable
final class AgeingProfile {
  const AgeingProfile({required this.counts, required this.undated});

  factory AgeingProfile.of(Iterable<InventoryItem> items, DateTime now) {
    final counts = List<int>.filled(ahAgeingBucketEdges.length + 1, 0);
    var undated = 0;

    for (final item in items) {
      final age = item.ageInDaysAt(now);
      if (age == null) {
        undated += 1;
        continue;
      }
      var bucket = ahAgeingBucketEdges.length;
      for (var index = 0; index < ahAgeingBucketEdges.length; index += 1) {
        if (age <= ahAgeingBucketEdges[index]) {
          bucket = index;
          break;
        }
      }
      counts[bucket] += 1;
    }

    return AgeingProfile(counts: List<int>.unmodifiable(counts), undated: undated);
  }

  /// One entry per bucket, in ascending age. Length is `edges.length + 1`.
  final List<int> counts;

  final int undated;

  /// Everything past the last edge — the stock that is costing the dealer money to hold.
  int get overOldestEdge => counts.isEmpty ? 0 : counts.last;

  /// `0–30`, `31–60`, `61–90`, `90+`. Digits and a dash are a locale invariant, not vocabulary:
  /// there is no i18n registry entry for what a numeric range looks like.
  static String labelFor(int bucket) {
    if (bucket >= ahAgeingBucketEdges.length) return '${ahAgeingBucketEdges.last}+';
    final low = bucket == 0 ? 0 : ahAgeingBucketEdges[bucket - 1] + 1;
    return '$low–${ahAgeingBucketEdges[bucket]}';
  }
}
