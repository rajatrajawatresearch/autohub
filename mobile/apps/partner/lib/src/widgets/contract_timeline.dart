// `autohub_core` and `autohub_ui` both publish a `WorkflowHistoryEntry`: the core one is the wire
// shape with an ISO string, the UI one is the render shape with a `DateTime`. Hiding the core name
// here rather than aliasing a whole package keeps the conversion visible in one file — which is
// exactly where a timestamp parse belongs.
import 'package:autohub_core/autohub_core.dart' hide WorkflowHistoryEntry;
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/widgets.dart';

/// A contract's lifecycle, rendered from the workflow registry.
///
/// proposal → trial → active → renewal → termination is **not** written anywhere in this app. The
/// states arrive from `GET /platform/workflows/{key}` with their names and their categories, and
/// the entity's own history arrives from `GET /attachment/contracts/{id}/workflow`. Adding
/// `legal_hold` to the definition on a Tuesday renders here, correctly toned, on a handset that
/// shipped in March.
///
/// The one judgement this widget makes is what to do when the *definition* has not arrived — a
/// first launch on a dead network, or a registry document this device has never cached. It renders
/// the history anyway, with the current state's own name folded into the vocabulary, because an
/// audit trail with one unfamiliar word in it is strictly better than a blank panel on the screen
/// a partner opened to find out what happened to their contract.
class ContractTimeline extends StatelessWidget {
  const ContractTimeline({
    required this.snapshot,
    required this.definition,
    required this.emptyMessage,
    super.key,
  });

  final WorkflowSnapshot snapshot;

  /// `null` while the registry document is still loading, or forever on a device that has never
  /// seen it.
  final WorkflowDefinition? definition;

  /// What to say when nothing has happened yet. Supplied by the caller because the sentence depends
  /// on the entity, and this widget does not know it is looking at a contract.
  final String emptyMessage;

  @override
  Widget build(BuildContext context) {
    return WorkflowTimeline(
      states: _states(),
      entries: _entries(),
      empty: Text(
        emptyMessage,
        style: AhText.bodySm.copyWith(color: context.ahSurfaces.textSecondary),
      ),
    );
  }

  List<WorkflowStateDefinition> _states() {
    final List<WorkflowStateDefinition> states = <WorkflowStateDefinition>[
      for (final WorkflowState state in definition?.states ?? const <WorkflowState>[])
        WorkflowStateDefinition(
          code: state.code,
          name: state.name,
          category: state.category,
        ),
    ];

    // The snapshot always knows its own current state, definition or not. Appending it costs one
    // row and buys a correctly-named, correctly-toned chip for the entry a partner is most likely
    // to be looking at.
    final WorkflowState current = snapshot.currentState;
    if (!states.any((WorkflowStateDefinition known) => known.code == current.code)) {
      states.add(
        WorkflowStateDefinition(
          code: current.code,
          name: current.name,
          category: current.category,
        ),
      );
    }

    return states;
  }

  List<WorkflowHistoryEntry> _entries() {
    final List<WorkflowHistoryEntry> entries = <WorkflowHistoryEntry>[];
    // The loop variable is deliberately left to inference: its type is `autohub_core`'s
    // `WorkflowHistoryEntry`, which this library has hidden so that the name below refers to
    // `autohub_ui`'s. Naming it is not possible here; needing to is the signal that the conversion
    // belongs in exactly one file, which is this one.
    for (final entry in snapshot.history) {
      final DateTime? occurredAt = DateTime.tryParse(entry.occurredAt);
      // A history row whose timestamp will not parse is dropped rather than rendered at the epoch.
      // An audit trail that claims a termination happened in 1970 is worse than one that is a row
      // short, because the first is read as fact.
      if (occurredAt == null) continue;
      entries.add(
        WorkflowHistoryEntry(
          to: entry.to,
          occurredAt: occurredAt,
          from: entry.from,
          actionCode: entry.actionCode,
          actor: entry.actor,
          timeInPreviousState: entry.timeInPreviousState,
        ),
      );
    }
    return entries;
  }
}
