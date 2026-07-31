import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_partner/src/app/partner_side.dart';
import 'package:autohub_partner/src/app/partner_text.dart';
import 'package:autohub_partner/src/data/attachment_models.dart';
import 'package:autohub_partner/src/data/partner_providers.dart';
import 'package:autohub_partner/src/data/wire.dart';
import 'package:autohub_partner/src/screens/matches_screen.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// The commission must be on the table before a match can be accepted.
///
/// This is the product, not a nicety. The attachment market runs today on WhatsApp groups and a
/// broker's ₹5,000 line charge, and what AutoHub sells is that the cut is visible before you sign.
/// A build that let a partner accept a match whose commission the app could not state would ship
/// exactly the opacity this module exists to replace — and the owner would discover the number on
/// the first monthly statement, having already declined two other operators.
///
/// So the rule is enforced in one place (`AttachmentMatch.canAccept`), asserted here at the model
/// and at the widget, and it fails closed: no quote, no acceptance.
void main() {
  const WorkflowState proposed = WorkflowState(code: 'proposed', name: 'Proposed');

  const PricingQuote quote = PricingQuote(
    baseAmount: 5286000,
    lines: <PricingLine>[
      PricingLine(
        code: 'attach_platform_commission',
        label: 'Platform commission 6%',
        basis: PricingBasis.percent,
        rate: 0.06,
        amountMinor: 317200,
        ruleName: 'Corporate ETS — Bengaluru',
      ),
      PricingLine(
        code: 'gst_services_18',
        label: 'GST 18%',
        basis: PricingBasis.percentOfLines,
        rate: 0.18,
        amountMinor: 57100,
      ),
    ],
    totalFeesMinor: 374300,
    payableMinor: 374300,
    explanation: 'Charged to the operator every cycle for as long as the contract runs.',
  );

  AttachmentMatch match({PricingQuote? commission, String? proposalId = 'proposal-1'}) =>
      AttachmentMatch(
        id: 'match-1',
        title: 'Whitefield ETS — 40 sedans',
        state: proposed,
        isEligible: true,
        proposalId: proposalId,
        counterpartyName: 'Meridian Fleet Services',
        score: 87,
        rateMinor: 185000,
        rateLabel: 'Per duty',
        commission: commission,
      );

  Widget board(List<AttachmentMatch> items) => partnerHarness(
        const MatchesScreen(),
        overrides: <Override>[
          initialPartnerSideProvider.overrideWithValue(PartnerSide.supply),
          matchesProvider.overrideWith(
            (ref, postId) async => CursorPage<AttachmentMatch>(items: items),
          ),
        ],
      );

  group('AttachmentMatch.canAccept', () {
    test('is false while the commission has not been quoted', () {
      expect(match().canAccept, isFalse);
    });

    test('is true once there is a quote and a proposal to accept', () {
      expect(match(commission: quote).canAccept, isTrue);
    });

    test('is false without a proposal, quote or not', () {
      // A match is an opinion; a proposal is an offer with terms and an expiry. There is nothing to
      // accept until the second one exists.
      expect(match(commission: quote, proposalId: null).canAccept, isFalse);
    });

    test('is false for a candidate the hard filters rejected', () {
      const AttachmentMatch rejected = AttachmentMatch(
        id: 'match-2',
        title: 'Airport transfers — 12 sedans',
        state: proposed,
        isEligible: false,
        proposalId: 'proposal-2',
        commission: quote,
        denyReasonCode: 'permit_invalid',
        denyReasonLabel: 'Permit expires within 45 days',
      );

      expect(rejected.canAccept, isFalse);
    });

    test('eligibility defaults from the presence of a deny reason when the server omits it', () {
      // Belt and braces at the decode seam: a response that carries a deny reason but forgets the
      // flag must not produce an acceptable match.
      final AttachmentMatch decoded = AttachmentMatch.fromJson(<String, Object?>{
        'id': 'match-3',
        'title': 'Hotel desk — 4 sedans',
        'state': <String, Object?>{'code': 'ineligible', 'name': 'Not eligible'},
        'denyReasonCode': 'fitness_expiring',
      });

      expect(decoded.isEligible, isFalse);
      expect(decoded.canAccept, isFalse);
    });
  });

  group('the match board', () {
    testWidgets('disables acceptance and says why when there is no quote', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(board(<AttachmentMatch>[match()]));
      await tester.pumpAndSettle();

      final AhButton accept = tester.widget<AhButton>(
        find.widgetWithText(AhButton, defaultPartnerLabels[PartnerKeys.matchesAccept]!),
      );
      expect(accept.onPressed, isNull);

      expect(
        find.text(defaultPartnerLabels[PartnerKeys.matchesCommissionMissing]!),
        findsOneWidget,
      );
    });

    testWidgets('states the commission before acceptance is offered', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(board(<AttachmentMatch>[match(commission: quote)]));
      await tester.pumpAndSettle();

      final AhButton accept = tester.widget<AhButton>(
        find.widgetWithText(AhButton, defaultPartnerLabels[PartnerKeys.matchesAccept]!),
      );
      expect(accept.onPressed, isNotNull);

      // Every line, named, with the rule that produced it — and the total spelled in full rather
      // than compacted, because `₹3.7K` is not a figure anyone can be held to.
      expect(find.text('Platform commission 6%'), findsOneWidget);
      expect(find.text('Corporate ETS — Bengaluru'), findsOneWidget);
      expect(find.text('GST 18%'), findsOneWidget);
      expect(find.byType(MoneyValue), findsWidgets);
    });

    testWidgets('shows rejected candidates with the reason instead of hiding them', (
      WidgetTester tester,
    ) async {
      const AttachmentMatch rejected = AttachmentMatch(
        id: 'match-4',
        title: 'Corporate ETS — 40 sedans',
        postTitle: 'MH12AB1234',
        state: WorkflowState(code: 'ineligible', name: 'Not eligible'),
        isEligible: false,
        denyReasonCode: 'permit_invalid',
        denyReasonLabel: 'Permit expires within 45 days',
      );

      await tester.pumpWidget(board(<AttachmentMatch>[rejected]));
      await tester.pumpAndSettle();

      // "You matched 0 of 40 requirements, and 31 rejected you because your permit expires in 22
      // days" is the most useful sentence this module can say to an owner. A board that silently
      // returned nothing would teach them nothing.
      expect(find.text('Permit expires within 45 days'), findsOneWidget);
      expect(find.text('permit_invalid'), findsOneWidget);
      expect(
        find.text(defaultPartnerLabels[PartnerKeys.matchesAccept]!),
        findsNothing,
      );
    });
  });
}
