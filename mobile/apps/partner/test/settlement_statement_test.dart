import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_partner/src/app/partner_text.dart';
import 'package:autohub_partner/src/data/attachment_models.dart';
import 'package:autohub_partner/src/data/partner_providers.dart';
import 'package:autohub_partner/src/screens/cycle_screen.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// The settlement statement is the one screen in this app where being wrong costs the partner
/// money, so it is the one with arithmetic under test.
///
/// The numbers below are the worked cycle from `docs/modules/12-rental-lease-attachment.md` §5.9.2
/// — a corporate ETS contract in Bengaluru, one Dzire CNG, 26 duties at ₹1,850 plus 340 extra km at
/// ₹14, against 6% commission, 18% GST on that commission, TDS 194-O, one vehicle no-show penalty,
/// FASTag tolls and a challan. Using the spec's own figures rather than round numbers is the point:
/// if the client's idea of what a statement is made of ever drifts from the module spec's, this
/// fails.
void main() {
  const WorkflowState settled = WorkflowState(code: 'settled', name: 'Settled');

  /// Every amount in paise, signed exactly as the server sends it. Nothing in the app flips a sign.
  SettlementCycle workedCycle({int? netPayableOverride}) => SettlementCycle(
        id: 'cycle-1',
        publicRef: 'ACY-2607-0012',
        state: settled,
        agreedGrossMinor: 5286000, // ₹52,860
        netPayableMinor: netPayableOverride ?? 4570400, // ₹45,704
        lines: const <SettlementLine>[
          SettlementLine(code: 'tds_194o_ecommerce', label: 'TDS 194-O', amountMinor: -5300),
          SettlementLine(
            code: 'attach_platform_commission',
            label: 'Platform commission 6%',
            amountMinor: -317200,
          ),
          SettlementLine(code: 'gst_services_18', label: 'GST 18%', amountMinor: -57100),
          SettlementLine(
            code: 'attach_penalty_vehicle_noshow',
            label: 'Vehicle no-show',
            amountMinor: -150000,
          ),
          SettlementLine(code: 'attach_toll_recovery', label: 'Tolls', amountMinor: -86000),
          SettlementLine(code: 'attach_challan_recovery', label: 'Challan', amountMinor: -100000),
        ],
      );

  group('SettlementCycle arithmetic', () {
    test('the spec\'s worked cycle foots to the paisa', () {
      final SettlementCycle cycle = workedCycle();

      expect(cycle.derivedNetPayableMinor, 4570400);
      expect(cycle.foots, isTrue);
      expect(cycle.reconciliationDeltaMinor, 0);
    });

    test('a statement whose lines disagree with the payable figure does not foot', () {
      // One paisa. The comparison is exact and not a tolerance, because a tolerance is a decision
      // to accept being wrong by an amount nobody chose — and because integer paise make an exact
      // comparison free.
      final SettlementCycle cycle = workedCycle(netPayableOverride: 4570401);

      expect(cycle.foots, isFalse);
      expect(cycle.reconciliationDeltaMinor, 1);
    });

    test('a cycle with no lines yet is not treated as unreconciled', () {
      // `awaiting_earnings` is a real state: the cycle is open, nothing has been priced, and there
      // is nothing to reconcile. Flagging it would cry wolf on every open cycle and train the
      // partner to ignore the one warning on this screen that matters.
      const SettlementCycle open = SettlementCycle(
        id: 'cycle-2',
        state: WorkflowState(code: 'open', name: 'Open'),
        agreedGrossMinor: 0,
        netPayableMinor: 0,
      );

      expect(open.foots, isTrue);
    });

    test('the payable figure is the server\'s, never a total the client added up', () {
      // Deliberately inconsistent input: the app must keep rendering what the server said and
      // report the disagreement, not quietly substitute its own sum. Silently preferring the
      // derived total would mean a partner is shown a number no invoice or ledger contains.
      final SettlementCycle cycle = workedCycle(netPayableOverride: 9900000);

      expect(cycle.netPayableMinor, 9900000);
      expect(cycle.derivedNetPayableMinor, 4570400);
      expect(cycle.foots, isFalse);
    });
  });

  group('the unreconciled warning', () {
    testWidgets('names a client-side code so support knows which side noticed', (
      WidgetTester tester,
    ) async {
      final SettlementCycle cycle = workedCycle(netPayableOverride: 4570401);

      await tester.pumpWidget(
        partnerHarness(
          UnreconciledStatement(cycle: cycle, text: const PartnerText(null)),
        ),
      );

      // The sentence the partner reads.
      expect(
        find.textContaining(
          defaultPartnerLabels[PartnerKeys.settlementUnreconciled]!.substring(0, 30),
        ),
        findsOneWidget,
      );

      // The reference support reads off the screenshot: the client-side code, and the cycle's
      // public reference so the row can be found without asking the partner to describe it.
      expect(
        find.textContaining('partner.settlement.statement_unreconciled · ACY-2607-0012'),
        findsOneWidget,
      );
    });

    /// Pumps the real statement screen against a fixed cycle.
    ///
    /// Each case gets its own tree rather than re-pumping one: the warning's whole job is to be
    /// present or absent on a first render, and reusing an element tree would let a stale
    /// `AsyncValue` answer the question instead of the cycle under test.
    Future<void> pumpCycle(WidgetTester tester, SettlementCycle cycle) async {
      await tester.pumpWidget(
        partnerHarness(
          const CycleScreen(cycleId: 'cycle-1'),
          overrides: <Override>[
            cycleProvider.overrideWith((ref, id) async => cycle),
          ],
        ),
      );
      await tester.pumpAndSettle();
    }

    testWidgets('stays out of the way when the statement foots', (WidgetTester tester) async {
      await pumpCycle(tester, workedCycle());

      expect(find.byType(UnreconciledStatement), findsNothing);
      // The payable figure still renders — in the header, and again at the foot of the lines.
      expect(find.byType(MoneyValue), findsWidgets);
    });

    testWidgets('warns before the partner reads a figure that does not add up', (
      WidgetTester tester,
    ) async {
      await pumpCycle(tester, workedCycle(netPayableOverride: 4570401));

      expect(find.byType(UnreconciledStatement), findsOneWidget);
    });
  });
}
