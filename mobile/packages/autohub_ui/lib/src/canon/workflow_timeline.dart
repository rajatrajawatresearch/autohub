import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/widgets.dart';
import 'package:intl/intl.dart';

import '../theme/ah_theme.dart';
import '../tokens/typography.dart';
import 'status_pill.dart';

// This file used to declare its own `WorkflowStateDefinition` and `WorkflowHistoryEntry` — render
// shapes that mirrored `autohub_core`'s `WorkflowState` and `WorkflowHistoryEntry` field for field,
// differing only in that `occurredAt` had already been parsed to a `DateTime`.
//
// That second definition cost more than it bought. Both packages export their version, so the name
// was ambiguous in every app that imported both barrels, and all three apps independently worked
// around it with a `hide` clause and a hand-written remapping loop. Worse, those three loops did
// not agree: two dropped a row whose timestamp would not parse, and the third substituted
// `DateTime.now()` — which renders a fabricated instant in an audit trail as though it were fact.
//
// So the widget now takes the contract types straight from `autohub_core`, a caller can pass
// `definition.states` and `snapshot.history` through untouched, and the one judgement about an
// unparseable timestamp is made once, here, where it can be tested.

@immutable
class WorkflowTimelineLabels {
  const WorkflowTimelineLabels({this.actor = 'By', this.dwell = 'Held'});

  final String actor;
  final String dwell;
}

/// Any entity's state history, rendered from the workflow registry.
///
/// This widget knows nothing about listings, deals, loans or claims. It receives the workflow's
/// declared [states] and the entity's [entries] exactly as the API returns them, and the only
/// decision it makes is which [StatusPill] tone a category wears. Add a state to the registry on
/// a Tuesday and it appears here, correctly toned, on handsets that shipped in March.
///
/// A vertical hairline rail, one chip per entry, mono timestamps, and the dwell time in the
/// previous state — because the question a dealer actually asks about a stuck listing is not
/// "what happened" but "how long did it sit there".
class WorkflowTimeline extends StatelessWidget {
  const WorkflowTimeline({
    required this.entries,
    required this.states,
    this.newestFirst = true,
    this.labels = const WorkflowTimelineLabels(),
    this.empty,
    super.key,
  });

  /// `history` from `GET /{module}/{resource}/{id}/workflow`, in the order the server sent it.
  final List<WorkflowHistoryEntry> entries;

  /// `states` from `GET /platform/workflows/{key}`.
  final List<WorkflowState> states;

  final bool newestFirst;
  final WorkflowTimelineLabels labels;

  /// Shown when an entity has no history yet. Supplied by the caller, because the instruction
  /// depends on the entity — "Nothing has happened to this listing yet" is not the right sentence
  /// for a settlement.
  final Widget? empty;

  @override
  Widget build(BuildContext context) {
    final rows = _renderable();
    if (rows.isEmpty) return empty ?? const SizedBox.shrink();

    final surfaces = context.ahSurfaces;
    final byCode = <String, WorkflowState>{
      for (final state in states) state.code: state,
    };
    final ordered = newestFirst ? rows.reversed.toList() : rows;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        for (var index = 0; index < ordered.length; index += 1)
          _Entry(
            entry: ordered[index].entry,
            occurredAt: ordered[index].occurredAt,
            // A code with no definition still renders, as its own raw code in a neutral chip.
            // Falling back rather than throwing matters here: a paginated or migrated history can
            // legitimately reference a state the current registry version no longer declares, and
            // an audit trail that crashes is worse than one with an unfamiliar word in it.
            state: byCode[ordered[index].entry.to],
            labels: labels,
            isLatest: newestFirst ? index == 0 : index == ordered.length - 1,
            isLast: index == ordered.length - 1,
            rail: surfaces.borderHairline,
          ),
      ],
    );
  }

  /// The entries this widget is willing to draw, each paired with its parsed instant.
  ///
  /// A row whose `occurredAt` will not parse is dropped rather than rendered at the epoch or at
  /// `DateTime.now()`. Both of those substitute a timestamp the server never sent into a record
  /// people argue from: the epoch reorders the whole timeline and reads as a 1970 transition, and
  /// "now" is worse still, because it is plausible. A trail that is one row short announces less
  /// than a trail that is confidently wrong.
  List<({WorkflowHistoryEntry entry, DateTime occurredAt})> _renderable() {
    final rows = <({WorkflowHistoryEntry entry, DateTime occurredAt})>[];
    for (final entry in entries) {
      final parsed = DateTime.tryParse(entry.occurredAt);
      if (parsed == null) continue;
      rows.add((entry: entry, occurredAt: parsed.toUtc()));
    }
    return rows;
  }
}

class _Entry extends StatelessWidget {
  const _Entry({
    required this.entry,
    required this.occurredAt,
    required this.state,
    required this.labels,
    required this.isLatest,
    required this.isLast,
    required this.rail,
  });

  final WorkflowHistoryEntry entry;

  /// [entry]'s `occurredAt`, already parsed and known good — the caller filtered the rows that
  /// were not, so there is no failure to re-handle here.
  final DateTime occurredAt;

  final WorkflowState? state;
  final WorkflowTimelineLabels labels;
  final bool isLatest;
  final bool isLast;
  final Color rail;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final dwell = entry.timeInPreviousState == null
        ? null
        : formatIso8601Dwell(entry.timeInPreviousState!);

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          SizedBox(
            width: 17,
            child: Stack(
              children: <Widget>[
                Positioned(
                  left: 3,
                  top: 0,
                  bottom: isLast ? null : 0,
                  height: isLast ? 8 : null,
                  child: Container(width: 1, color: rail),
                ),
                Positioned(
                  left: 0,
                  top: 5,
                  child: Container(
                    width: 8,
                    height: 8,
                    decoration: BoxDecoration(
                      color: isLatest ? surfaces.accentPrimary : surfaces.page,
                      border: Border.all(
                        color: isLatest ? surfaces.accentPrimary : surfaces.borderStrong,
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(left: AhSpace.sm, bottom: isLast ? 0 : AhSpace.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  Wrap(
                    spacing: AhSpace.sm,
                    runSpacing: AhSpace.xs,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: <Widget>[
                      StatusPill(
                        category: state?.category ?? AhStateCategory.draft,
                        label: state?.name ?? entry.to,
                        size: StatusPillSize.sm,
                      ),
                      if (dwell != null)
                        Text(
                          '${labels.dwell} $dwell',
                          style: AhText.data.copyWith(
                            fontSize: 13,
                            color: surfaces.textSecondary,
                          ),
                        ),
                    ],
                  ),
                  const SizedBox(height: AhSpace.xs),
                  Wrap(
                    spacing: AhSpace.md,
                    runSpacing: 2,
                    children: <Widget>[
                      Text(
                        formatIstStamp(occurredAt),
                        style: AhText.data.copyWith(
                          fontSize: 13,
                          color: surfaces.textPrimary,
                        ),
                      ),
                      if (entry.actor != null)
                        Text(
                          '${labels.actor} ${entry.actor}',
                          style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// `PT17H10M` → `17h 10m`.
///
/// Units are a technical invariant, not business vocabulary — there is no registry for what "h"
/// means — so this is the one place in the canon that emits characters the caller did not supply.
/// Returns `null` on anything it does not recognise, so an unparseable duration renders nothing
/// rather than a raw `PT17H10M` in the middle of an audit trail.
String? formatIso8601Dwell(String value) {
  final match = RegExp(r'^P(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:([\d.]+)S)?)?$')
      .firstMatch(value.trim().toUpperCase());
  if (match == null) return null;

  final parts = <String>[];
  if (match.group(1) != null) parts.add('${int.parse(match.group(1)!)}d');
  if (match.group(2) != null) parts.add('${int.parse(match.group(2)!)}h');
  if (match.group(3) != null) parts.add('${int.parse(match.group(3)!)}m');
  if (parts.isEmpty && match.group(4) != null) {
    parts.add('${double.parse(match.group(4)!).round()}s');
  }

  return parts.isEmpty ? null : parts.join(' ');
}

/// A UTC instant, rendered in IST.
///
/// A fixed +05:30 rather than a timezone database, because India has one zone and has never
/// observed daylight saving — so the database would be 900KB to encode a constant. If this
/// product ever lists outside IST, that is the moment to add `package:timezone`, not before.
///
/// It renders in IST rather than device-local on purpose: an audit trail is compared against what
/// a colleague in the Pune office saw, and two people reading the same transition at different
/// times is the sort of thing that turns into a dispute.
String formatIstStamp(DateTime instant) {
  final ist = instant.toUtc().add(const Duration(hours: 5, minutes: 30));
  return '${DateFormat('d MMM yyyy, h:mm').format(ist)}${ist.hour < 12 ? 'am' : 'pm'} IST';
}
