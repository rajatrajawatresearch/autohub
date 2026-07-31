import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_dealer/src/app/strings.dart';
import 'package:autohub_dealer/src/features/auction/bid_panel.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures.dart';
import 'harness.dart';

/// The bid panel is the one surface in the product where being slightly wrong is expensive and
/// irreversible. These are the guarantees §6.2 asks for, asserted through the widget a dealer
/// actually touches rather than through the layer underneath it.
void main() {
  const t = DealerStrings(null);

  testWidgets('prefills the server\'s next legal bid, never a locally derived one', (tester) async {
    // A ladder with a slab step: the next legal bid is *not* current + increment.
    final lot = testLot(
      currentBidMinor: 84500000,
      incrementMinor: 500000,
      minNextBidMinor: 90000000,
    );

    await tester.pumpWidget(dealerHarness(BidPanel(view: testView(lot: lot))));

    final field = tester.widget<TextField>(find.byType(TextField));
    expect(field.controller?.text, '900000');
    expect(
      field.controller?.text,
      isNot('850000'),
      reason: 'a client-derived minimum is a bid the server rejects at the moment it matters',
    );
  });

  testWidgets('a bid under the ladder cannot be placed', (tester) async {
    await tester.pumpWidget(
      dealerHarness(BidPanel(view: testView(lot: testLot(minNextBidMinor: 85000000)))),
    );

    await tester.enterText(find.byType(TextField), '840000');
    await tester.pump();

    expect(find.text(t(DealerText.belowLadder)), findsOneWidget);

    // Tapping it must not open the confirmation sheet at all.
    await tester.tap(find.text(t(DealerText.placeBid)));
    await tester.pumpAndSettle();
    expect(find.text(t(DealerText.confirmBid)), findsNothing);
  });

  testWidgets('the confirmation sheet restates the exact amount before anything is sent',
      (tester) async {
    final sent = <WriteIntent>[];

    await tester.pumpWidget(
      dealerHarness(
        BidPanel(view: testView(lot: testLot(minNextBidMinor: 85000000))),
        overrides: <Override>[
          immediateWriterOverride(
            const WriteUnreachable(reason: ApiErrorCodes.unreachable),
            sent,
          ),
        ],
      ),
    );

    await tester.tap(find.text(t(DealerText.placeBid)));
    await tester.pumpAndSettle();

    // Uncompacted, because `₹8.5L` is not a figure anyone can be held to — and beside the plate of
    // the vehicle it buys, which is what a dealer checks against the truck in front of them.
    // Scoped to the sheet's own heading: the panel behind it also renders the minimum next bid,
    // and an assertion that cannot tell them apart would pass with no sheet at all.
    expect(
      find.descendant(
        of: find.byType(SectionHeading),
        matching: find.text(formatIndianMoney(85000000)),
      ),
      findsOneWidget,
    );
    expect(find.text('MH 12 AB 1234'), findsOneWidget);
    expect(find.text(t(DealerText.confirmBidTerms)), findsOneWidget);

    // Nothing has left yet. The sheet is a gate, not a receipt.
    expect(sent, isEmpty);
  });

  testWidgets('cancelling the sheet sends nothing', (tester) async {
    final sent = <WriteIntent>[];

    await tester.pumpWidget(
      dealerHarness(
        BidPanel(view: testView(lot: testLot(minNextBidMinor: 85000000))),
        overrides: <Override>[
          immediateWriterOverride(
            const WriteUnreachable(reason: ApiErrorCodes.unreachable),
            sent,
          ),
        ],
      ),
    );

    await tester.tap(find.text(t(DealerText.placeBid)));
    await tester.pumpAndSettle();
    await tester.tap(find.text(t(DealerText.cancel)));
    await tester.pumpAndSettle();

    expect(sent, isEmpty);
    expect(find.text(t(DealerText.confirmBid)), findsNothing);
  });

  testWidgets('offline surfaces a loud failure and carries seenBidSeq on the attempt',
      (tester) async {
    final sent = <WriteIntent>[];

    await tester.pumpWidget(
      dealerHarness(
        BidPanel(
          view: testView(lot: testLot(minNextBidMinor: 85000000, latestSeq: 1042)),
        ),
        overrides: <Override>[
          immediateWriterOverride(
            const WriteUnreachable(reason: ApiErrorCodes.unreachable),
            sent,
          ),
        ],
      ),
    );

    await tester.tap(find.text(t(DealerText.placeBid)));
    await tester.pumpAndSettle();
    await tester.tap(find.text(t(DealerText.confirmBid)));
    await tester.pumpAndSettle();

    expect(sent, hasLength(1));
    expect(sent.single.body?['seenBidSeq'], 1042);
    expect(sent.single.durability, WriteDurability.immediate);

    // A sheet rather than a snackbar: a snackbar times out, and a dealer who looked away for four
    // seconds would be left with a screen showing no error and no bid.
    expect(find.text(t(DealerText.errBidUnsent)), findsOneWidget);
    expect(find.text(t(DealerText.offlineTitle)), findsOneWidget);
  });

  testWidgets('the machine code is available to support but not shown to the dealer',
      (tester) async {
    await tester.pumpWidget(
      dealerHarness(
        BidPanel(view: testView(lot: testLot(minNextBidMinor: 85000000))),
        overrides: <Override>[
          immediateWriterOverride(
            const WriteRejected(statusCode: 422, code: 'auction.bid.below_increment'),
            <WriteIntent>[],
          ),
        ],
      ),
    );

    await tester.tap(find.text(t(DealerText.placeBid)));
    await tester.pumpAndSettle();
    await tester.tap(find.text(t(DealerText.confirmBid)));
    await tester.pumpAndSettle();

    // The sentence a dealer reads never contains the code.
    expect(find.text(t(DealerText.errBelowIncrement)), findsOneWidget);
    expect(find.textContaining('auction.bid.below_increment'), findsNothing);

    // One tap away for whoever picks up the phone.
    await tester.tap(find.text(t(DealerText.showDetails)));
    await tester.pumpAndSettle();
    expect(find.textContaining('auction.bid.below_increment'), findsOneWidget);
  });

  testWidgets('a closed lot offers no way to bid', (tester) async {
    await tester.pumpWidget(
      dealerHarness(
        BidPanel(
          view: testView(
            lot: testLot(stateCode: 'sold', stateCategory: 'succeeded', isTerminal: true),
          ),
        ),
      ),
    );

    expect(find.text(t(DealerText.bidClosed)), findsOneWidget);
    expect(find.text(t(DealerText.placeBid)), findsNothing);
    expect(find.byType(TextField), findsNothing);
  });
}
