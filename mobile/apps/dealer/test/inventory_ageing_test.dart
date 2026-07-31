import 'package:autohub_dealer/src/data/models/inventory_item.dart';
import 'package:flutter_test/flutter_test.dart';

Map<String, Object?> _listing({String? createdAt, int? leadCount, int? priceMinor}) =>
    <String, Object?>{
      'listingId': 'listing-$createdAt-$priceMinor',
      'registration': <String, Object?>{'canonical': 'MH12AB1234', 'display': 'MH 12 AB 1234'},
      'title': '2021 Maruti Suzuki Swift VXi',
      if (priceMinor != null) 'priceMinor': priceMinor,
      if (createdAt != null) 'createdAt': createdAt,
      if (leadCount != null) 'leadCount': leadCount,
    };

/// Ageing is the number a dealer's working capital is organised around, and the screen that shows
/// it is the one built to catch stock that is not moving. A row counted into the wrong bucket makes
/// a stale vehicle look healthy.
void main() {
  final now = DateTime.utc(2026, 7, 31);

  group('InventoryItem', () {
    test('decodes the contract projection and the dealer-console additions together', () {
      final item = InventoryItem.fromJson(
        _listing(createdAt: '2026-06-01T00:00:00Z', leadCount: 4, priceMinor: 54900000),
      );

      expect(item.listing.title, '2021 Maruti Suzuki Swift VXi');
      expect(item.listing.priceMinor, 54900000);
      expect(item.leadCount, 4);
      expect(item.ageInDaysAt(now), 60);
    });

    test('a row the server did not date still decodes and renders as unknown', () {
      // A missing field costs a column, never the row. One photoless listing once failed the parse
      // for an entire search page on the web; the same class of bug on a handset waits for an
      // app-store review.
      final item = InventoryItem.fromJson(_listing(priceMinor: 100));

      expect(item.listing.title, isNotEmpty);
      expect(item.listedAt, isNull);
      expect(item.leadCount, isNull);
      expect(item.ageInDaysAt(now), isNull);
    });

    test('a future date floors at zero rather than reading as negative', () {
      final item = InventoryItem.fromJson(_listing(createdAt: '2026-08-05T00:00:00Z'));
      expect(item.ageInDaysAt(now), 0);
    });
  });

  group('AgeingProfile', () {
    test('counts into the declared buckets, boundaries inclusive at the top', () {
      final profile = AgeingProfile.of(
        <InventoryItem>[
          InventoryItem.fromJson(_listing(createdAt: '2026-07-30T00:00:00Z')), // 1 day
          InventoryItem.fromJson(_listing(createdAt: '2026-07-01T00:00:00Z')), // 30 days
          InventoryItem.fromJson(_listing(createdAt: '2026-06-30T00:00:00Z')), // 31 days
          InventoryItem.fromJson(_listing(createdAt: '2026-06-01T00:00:00Z')), // 60 days
          InventoryItem.fromJson(_listing(createdAt: '2026-04-01T00:00:00Z')), // 121 days
        ],
        now,
      );

      expect(profile.counts, <int>[2, 2, 0, 1]);
      expect(profile.overOldestEdge, 1);
      expect(profile.undated, 0);
    });

    test('an undated row is not counted as new stock', () {
      final profile = AgeingProfile.of(
        <InventoryItem>[
          InventoryItem.fromJson(_listing()),
          InventoryItem.fromJson(_listing(createdAt: '2026-07-30T00:00:00Z')),
        ],
        now,
      );

      // Folding it into 0–30 would make a vehicle of unknown age look fresh on the one screen whose
      // job is to find the ones that are not.
      expect(profile.counts.first, 1);
      expect(profile.undated, 1);
    });

    test('labels read the way the trade says them', () {
      expect(AgeingProfile.labelFor(0), '0–30');
      expect(AgeingProfile.labelFor(1), '31–60');
      expect(AgeingProfile.labelFor(2), '61–90');
      expect(AgeingProfile.labelFor(ahAgeingBucketEdges.length), '90+');
    });
  });
}
