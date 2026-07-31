import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_partner/src/app/partner_text.dart';
import 'package:autohub_partner/src/data/attachment_models.dart';
import 'package:autohub_partner/src/data/wire_dates.dart';
import 'package:autohub_partner/src/widgets/compliance_view.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// Compliance is the screen that protects the partner's income, and the failure it must not have is
/// a client-side threshold.
///
/// An expired permit, fitness certificate or driver document lapses the assignment, suspends the
/// contract after a registry-configured grace period, and blocks the payout while earnings keep
/// accruing. The thresholds that decide "expiring" are registry rows and they differ per contract
/// type — a school-transport police verification goes stale at 180 days where an aggregator's goes
/// stale at 365. So the verdict is the server's, and the client contributes calendar arithmetic and
/// nothing else. These tests pin both halves of that split.
void main() {
  // 12 August 2026, mid-morning IST. Deliberately not midnight: the whole reason `wholeDaysUntil`
  // exists is that subtracting instants and reading `.inDays` truncates, and truncation is wrong
  // for half of every day.
  final DateTime now = DateTime.utc(2026, 8, 12, 4, 30);

  ComplianceDocument document({
    required String validTill,
    String code = 'contract_carriage_permit',
    WorkflowState status = const WorkflowState(
      code: 'valid',
      name: 'Valid',
      category: AhStateCategory.succeeded,
    ),
  }) =>
      ComplianceDocument(
        code: code,
        label: 'Contract carriage permit',
        status: status,
        validTill: validTill,
      );

  group('calendar arithmetic', () {
    test('counts whole IST days, not 24-hour periods', () {
      // 3 September is 22 calendar days after 12 August. A naive instant subtraction from 04:30 UTC
      // would give 21, and a permit shown as lapsing a day later than it does is a paused contract
      // the owner did not see coming.
      expect(wholeDaysUntil('2026-09-03', now), 22);
    });

    test('is zero on the last valid day and negative after it', () {
      expect(wholeDaysUntil('2026-08-12', now), 0);
      expect(wholeDaysUntil('2026-08-09', now), -3);
    });

    test('handles a full instant as well as a bare date', () {
      // The contract sends dates; some projections send timestamps. Both name a day in IST.
      expect(wholeDaysUntil('2026-09-03T00:00:00Z', now), 22);
    });

    test('returns null rather than throwing on an unparseable or absent date', () {
      // One malformed row must cost that row its countdown, never the screen.
      expect(wholeDaysUntil(null, now), isNull);
      expect(wholeDaysUntil('', now), isNull);
      expect(wholeDaysUntil('soon', now), isNull);
    });
  });

  group('soonestToLapse', () {
    test('picks the document that lapses first and ignores those with no expiry', () {
      final List<ComplianceDocument> documents = <ComplianceDocument>[
        document(validTill: '2027-01-31', code: 'fitness_certificate'),
        document(validTill: '2026-09-03', code: 'contract_carriage_permit'),
        // A one-time KYC. It cannot lapse, so it cannot be the answer.
        const ComplianceDocument(
          code: 'driver_aadhaar_kyc',
          label: 'Aadhaar KYC',
          status: WorkflowState(code: 'verified', name: 'Verified'),
        ),
      ];

      expect(soonestToLapse(documents, now)?.code, 'contract_carriage_permit');
    });

    test('is null when nothing carries an expiry', () {
      expect(soonestToLapse(const <ComplianceDocument>[], now), isNull);
    });
  });

  group('ComplianceRow', () {
    testWidgets('takes its tone from the registry, never from a day count', (
      WidgetTester tester,
    ) async {
      // 200 days of validity left, and the server has still marked it as needing attention —
      // because the police-verification window for this contract type is 180 days, a number that
      // lives in `attach.pvc_max_age_days` and not in this binary. A client with an `if (days < 30)`
      // would paint this green while the server is already counting down to a suspension.
      const ComplianceDocument document = ComplianceDocument(
        code: 'driver_police_verification',
        label: 'Police verification',
        status: WorkflowState(
          code: 'expiring',
          name: 'Expiring',
          category: AhStateCategory.awaitingAction,
        ),
        validTill: '2027-02-28',
      );

      await tester.pumpWidget(
        partnerHarness(
          ComplianceRow(document: document, now: now, text: const PartnerText(null)),
        ),
      );

      final StatusPill pill = tester.widget<StatusPill>(find.byType(StatusPill));
      expect(pill.category, AhStateCategory.awaitingAction);
      expect(pill.label, 'Expiring');

      // The date renders verbatim, in the form the server sent it, so it can be checked against the
      // document itself in any of the eight languages this product serves.
      expect(find.text('2027-02-28'), findsOneWidget);
      expect(find.text('200d'), findsOneWidget);
    });

    testWidgets('does not render a negative countdown beside a lapsed document', (
      WidgetTester tester,
    ) async {
      final ComplianceDocument lapsed = document(
        validTill: '2026-08-09',
        status: const WorkflowState(
          code: 'lapsed',
          name: 'Lapsed',
          category: AhStateCategory.failed,
        ),
      );

      await tester.pumpWidget(
        partnerHarness(
          ComplianceRow(document: lapsed, now: now, text: const PartnerText(null)),
        ),
      );

      // The pill already carries the registry's own word for it. `-3d` would be the client
      // volunteering a second opinion in a vocabulary nobody authored.
      expect(find.text('-3d'), findsNothing);
      expect(find.text('3d'), findsNothing);
      expect(tester.widget<StatusPill>(find.byType(StatusPill)).category, AhStateCategory.failed);
    });
  });
}
