import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_dealer/src/data/models/deal.dart';
import 'package:autohub_dealer/src/data/models/inventory_item.dart';
import 'package:flutter_test/flutter_test.dart';

Map<String, Object?> _goodListing(String id) => <String, Object?>{
      'listingId': id,
      'registration': <String, Object?>{'canonical': 'MH12AB1234', 'display': 'MH 12 AB 1234'},
      'title': '2021 Maruti Suzuki Swift VXi',
      'priceMinor': 54900000,
    };

/// Decoding is where a contract drift becomes a blank screen. The rule the web already paid for:
/// one bad row costs a row, never a page.
void main() {
  group('CursorPage', () {
    test('carries the cursor and keeps the estimate honest', () {
      final page = CursorPage.reader(InventoryItem.fromJson)(<String, Object?>{
        'items': <Object?>[_goodListing('a'), _goodListing('b')],
        'nextCursor': 'eyJpZCI6...',
        'totalEstimate': 1284,
      });

      expect(page.items, hasLength(2));
      expect(page.hasMore, isTrue);
      expect(page.totalEstimate, 1284);
    });

    test('an exhausted list has no cursor, and an empty string is not a cursor', () {
      final page = CursorPage.reader(InventoryItem.fromJson)(<String, Object?>{
        'items': <Object?>[_goodListing('a')],
        'nextCursor': '',
      });

      // An empty cursor sent back as a cursor is an infinite list that refetches page one forever.
      expect(page.nextCursor, isNull);
      expect(page.hasMore, isFalse);
    });

    test('a row that will not decode is dropped, not fatal', () {
      final page = CursorPage.reader(InventoryItem.fromJson)(<String, Object?>{
        'items': <Object?>[
          _goodListing('a'),
          <String, Object?>{'listingId': 'broken'}, // no registration, no title
          _goodListing('c'),
        ],
      });

      // On the web a single malformed listing once failed the parse for an entire search page and
      // the marketplace rendered empty. On a handset that cannot be fixed by a deploy.
      expect(page.items, hasLength(2));
      expect(page.items.map((item) => item.listingId), <String>['a', 'c']);
    });

    test('a missing items array yields an empty page rather than throwing', () {
      final page = CursorPage.reader(InventoryItem.fromJson)(<String, Object?>{});
      expect(page.items, isEmpty);
      expect(page.hasMore, isFalse);
    });

    test('concat keeps the newer cursor', () {
      final first = CursorPage.reader(InventoryItem.fromJson)(<String, Object?>{
        'items': <Object?>[_goodListing('a')],
        'nextCursor': 'page-2',
      });
      final second = CursorPage.reader(InventoryItem.fromJson)(<String, Object?>{
        'items': <Object?>[_goodListing('b')],
        'nextCursor': 'page-3',
      });

      final joined = first.concat(second);
      expect(joined.items, hasLength(2));
      // Keeping the *old* cursor is how a list silently repeats its second page forever.
      expect(joined.nextCursor, 'page-3');
    });
  });

  group('DealDetail', () {
    test('decodes the pricing lines the server produced, with their rules named', () {
      final detail = DealDetail.fromJson(<String, Object?>{
        'dealId': 'deal-1',
        'reference': 'AH-2026-0004',
        'listing': _goodListing('a'),
        'amountMinor': 84735000,
        'state': <String, Object?>{
          'code': 'settled',
          'name': 'Settled',
          'category': 'succeeded',
          'isTerminal': true,
        },
        'fees': <Object?>[
          <String, Object?>{
            'code': 'auction_commission',
            'label': 'Auction commission',
            'basis': 'percent',
            'rate': 0.02,
            'amountMinor': 1694700,
            'ruleName': 'Auction — gold dealer, MH',
          },
          <String, Object?>{
            'code': 'gst',
            'label': 'GST 18%',
            'basis': 'percent_of_lines',
            'rate': 0.18,
            'amountMinor': 305046,
            'appliesTo': <Object?>['auction_commission'],
          },
        ],
        'totalFeesMinor': 1999746,
        'payableMinor': 86734746,
      });

      expect(detail.fees, hasLength(2));
      // The rule that produced the number, because a dealer will ask and "the code says so" is not
      // an answer support can give.
      expect(detail.fees.first.ruleName, 'Auction — gold dealer, MH');
      expect(detail.fees.first.basis, PricingBasis.percent);
      expect(detail.fees[1].basis, PricingBasis.percentOfLines);
      // Integer paise, as the server sent them. Nothing here recomputed 2% of anything.
      expect(detail.totalFeesMinor, 1999746);
      expect(detail.payableMinor, 86734746);
    });

    test('a fee line this build cannot read costs the line, not the deal', () {
      final detail = DealDetail.fromJson(<String, Object?>{
        'dealId': 'deal-1',
        'reference': 'AH-2026-0004',
        'listing': _goodListing('a'),
        'amountMinor': 84735000,
        'state': <String, Object?>{'code': 'settled', 'name': 'Settled', 'category': 'succeeded'},
        'fees': <Object?>[
          <String, Object?>{'code': 'broken'}, // no label, no amountMinor
        ],
        'totalFeesMinor': 1999746,
      });

      expect(detail.fees, isEmpty);
      // The server's total still stands and is still rendered — it is the authoritative figure.
      expect(detail.totalFeesMinor, 1999746);
    });

    test('an unrecognised basis degrades rather than failing the deal', () {
      final detail = DealDetail.fromJson(<String, Object?>{
        'dealId': 'deal-1',
        'reference': 'AH-1',
        'listing': _goodListing('a'),
        'amountMinor': 1,
        'state': <String, Object?>{'code': 'x', 'name': 'X', 'category': 'in_progress'},
        'fees': <Object?>[
          <String, Object?>{
            'code': 'levy',
            'label': 'New levy',
            'basis': 'tiered_by_axle',
            'amountMinor': 1000,
          },
        ],
      });

      expect(detail.fees, hasLength(1));
      expect(detail.fees.single.basis, PricingBasis.flat);
      expect(detail.fees.single.amountMinor, 1000);
    });
  });

  group('DealSummary', () {
    test('an unknown workflow category degrades to draft, never to succeeded', () {
      final deal = DealSummary.fromJson(<String, Object?>{
        'dealId': 'deal-1',
        'reference': 'AH-1',
        'listing': _goodListing('a'),
        'amountMinor': 100,
        'state': <String, Object?>{'code': 'legal_hold', 'name': 'Legal hold', 'category': 'held'},
      });

      // A seventh category shipped by the server must never read as money already received.
      expect(deal.state.category, AhStateCategory.inProgress);
      expect(deal.state.name, 'Legal hold');
    });
  });
}
