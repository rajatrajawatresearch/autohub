import 'package:autohub_core/autohub_core.dart';
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
      // The wire shape, straight through. The timeline drops a row whose timestamp will not parse
      // rather than dating it to the epoch, which is the answer this file used to give on its own.
      entries: snapshot.history,
      empty: Text(
        emptyMessage,
        style: AhText.bodySm.copyWith(color: context.ahSurfaces.textSecondary),
      ),
    );
  }

  List<WorkflowState> _states() {
    final List<WorkflowState> states = <WorkflowState>[
      ...?definition?.states,
    ];

    // The snapshot always knows its own current state, definition or not. Appending it costs one
    // row and buys a correctly-named, correctly-toned chip for the entry a partner is most likely
    // to be looking at.
    final WorkflowState current = snapshot.currentState;
    if (!states.any((WorkflowState known) => known.code == current.code)) {
      states.add(current);
    }

    return states;
  }
}
