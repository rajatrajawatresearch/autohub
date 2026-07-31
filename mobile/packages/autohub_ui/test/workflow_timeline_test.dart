import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// The timeline renders an audit trail, and an audit trail is a thing people argue from.
///
/// These tests exist because the workspace briefly had three answers to "what do I do with a
/// timestamp that will not parse": two apps dropped the row and one substituted `DateTime.now()`.
/// The third is the dangerous one — a fabricated instant is indistinguishable from a real one, so
/// it does not read as a bug, it reads as a fact about when someone rejected a listing. The
/// behaviour now lives in one widget, which is what makes it testable at all.
void main() {
  WorkflowHistoryEntry entry({
    required String to,
    required String occurredAt,
    String? actor,
  }) =>
      WorkflowHistoryEntry(to: to, occurredAt: occurredAt, actor: actor);

  const WorkflowState draft = WorkflowState(
    code: 'draft',
    name: 'Draft',
  );
  const WorkflowState live = WorkflowState(
    code: 'live',
    name: 'Live',
    category: AhStateCategory.succeeded,
  );

  Future<void> pump(WidgetTester tester, Widget child) =>
      tester.pumpWidget(ahHarness(child));

  group('a timestamp that will not parse', () {
    testWidgets('drops the row rather than dating it to now or to the epoch', (tester) async {
      await pump(
        tester,
        WorkflowTimeline(
          entries: <WorkflowHistoryEntry>[
            entry(to: 'draft', occurredAt: '2026-07-28T11:00:00Z'),
            entry(to: 'live', occurredAt: 'whenever'),
          ],
          states: const <WorkflowState>[draft, live],
        ),
      );

      expect(find.text('DRAFT'), findsOneWidget);
      expect(find.text('LIVE'), findsNothing);
      // The surviving row keeps its real stamp; nothing was invented to stand in for the other.
      expect(find.textContaining('28 Jul 2026'), findsOneWidget);
      expect(find.textContaining('1970'), findsNothing);
    });

    testWidgets('shows the empty message when it takes the last renderable row with it',
        (tester) async {
      await pump(
        tester,
        WorkflowTimeline(
          entries: <WorkflowHistoryEntry>[entry(to: 'draft', occurredAt: '')],
          states: const <WorkflowState>[draft],
          empty: const Text('Nothing yet'),
        ),
      );

      // Not a bare rail with no rows on it: a screen that renders an empty timeline widget looks
      // like a loading state that finished wrong.
      expect(find.text('Nothing yet'), findsOneWidget);
      expect(find.byType(StatusPill), findsNothing);
    });
  });

  group('the vocabulary is the registry\'s', () {
    testWidgets('renders the state name the server sent, not the code', (tester) async {
      await pump(
        tester,
        WorkflowTimeline(
          entries: <WorkflowHistoryEntry>[
            entry(to: 'live', occurredAt: '2026-07-29T04:10:00Z'),
          ],
          states: const <WorkflowState>[live],
        ),
      );

      expect(find.text('LIVE'), findsOneWidget);
      expect(find.text('Live'), findsNothing);
    });

    testWidgets('a code the definition no longer declares still renders, as its own code',
        (tester) async {
      await pump(
        tester,
        WorkflowTimeline(
          entries: <WorkflowHistoryEntry>[
            entry(to: 'legal_hold', occurredAt: '2026-07-29T04:10:00Z'),
          ],
          // The definition this device cached predates the state — a real case after a registry
          // migration, and one that must not blank the panel.
          states: const <WorkflowState>[draft],
        ),
      );

      expect(find.text('LEGAL_HOLD'), findsOneWidget);
      expect(
        tester.widget<StatusPill>(find.byType(StatusPill)).category,
        AhStateCategory.draft,
        reason: 'an undeclared code wears the one category that promises nothing — guessing '
            'succeeded or failed would state an outcome the registry never claimed',
      );
    });

    testWidgets('takes the contract types straight through, with no remapping at the call site',
        (tester) async {
      // The signature is the assertion: `snapshot.history` and `definition.states` are passed
      // exactly as decoded. When this stops compiling, an app has been handed a second shape to
      // convert to — which is how the duplicate arose in the first place.
      const WorkflowSnapshot snapshot = WorkflowSnapshot(
        workflowKey: 'listings.listing',
        currentState: live,
        enteredAt: '2026-07-29T04:10:00Z',
        history: <WorkflowHistoryEntry>[
          WorkflowHistoryEntry(to: 'draft', occurredAt: '2026-07-28T11:00:00Z'),
        ],
      );
      const WorkflowDefinition definition = WorkflowDefinition(
        key: 'listings.listing',
        version: 3,
        entityType: 'listings.listing',
        states: <WorkflowState>[draft, live],
      );

      await pump(
        tester,
        WorkflowTimeline(entries: snapshot.history, states: definition.states),
      );

      expect(find.text('DRAFT'), findsOneWidget);
    });
  });

  group('ordering', () {
    testWidgets('newestFirst puts the latest transition at the top', (tester) async {
      final List<WorkflowHistoryEntry> history = <WorkflowHistoryEntry>[
        entry(to: 'draft', occurredAt: '2026-07-28T11:00:00Z', actor: 'Seller'),
        entry(to: 'live', occurredAt: '2026-07-29T04:10:00Z', actor: 'Ops'),
      ];

      await pump(
        tester,
        WorkflowTimeline(entries: history, states: const <WorkflowState>[draft, live]),
      );

      final double liveY = tester.getTopLeft(find.text('LIVE')).dy;
      final double draftY = tester.getTopLeft(find.text('DRAFT')).dy;
      expect(liveY, lessThan(draftY));
    });

    testWidgets('newestFirst: false keeps the server order', (tester) async {
      final List<WorkflowHistoryEntry> history = <WorkflowHistoryEntry>[
        entry(to: 'draft', occurredAt: '2026-07-28T11:00:00Z'),
        entry(to: 'live', occurredAt: '2026-07-29T04:10:00Z'),
      ];

      await pump(
        tester,
        WorkflowTimeline(
          entries: history,
          states: const <WorkflowState>[draft, live],
          newestFirst: false,
        ),
      );

      expect(
        tester.getTopLeft(find.text('DRAFT')).dy,
        lessThan(tester.getTopLeft(find.text('LIVE')).dy),
      );
    });
  });

  group('the IST stamp', () {
    test('renders a UTC instant in IST, because the office it is compared against is', () {
      expect(
        formatIstStamp(DateTime.utc(2026, 7, 29, 4, 10)),
        '29 Jul 2026, 9:40am IST',
      );
    });

    test('crosses the date line the +05:30 offset puts it on', () {
      // 20:00 UTC is 01:30 the next morning in IST. A handset rendering this in UTC would file the
      // transition on the wrong day, and the wrong day is what a dispute turns on.
      expect(
        formatIstStamp(DateTime.utc(2026, 7, 29, 20)),
        '30 Jul 2026, 1:30am IST',
      );
    });
  });

  group('the dwell duration', () {
    test('reads the way a person says it', () {
      expect(formatIso8601Dwell('PT17H10M'), '17h 10m');
      expect(formatIso8601Dwell('P2DT3H'), '2d 3h');
      expect(formatIso8601Dwell('PT45S'), '45s');
    });

    test('renders nothing rather than a raw PT17H10M in the middle of an audit trail', () {
      expect(formatIso8601Dwell('17 hours'), isNull);
      expect(formatIso8601Dwell(''), isNull);
    });
  });
}
