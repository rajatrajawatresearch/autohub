// See the note in `taxonomy.dart` — the workspace root already settled this diagnostic.
// ignore_for_file: invalid_annotation_target

import 'package:autohub_core/src/contract/state_category.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'workflow.freezed.dart';
part 'workflow.g.dart';

/// Decodes the state category, falling back the way the web does.
///
/// `packages/api/src/schemas/workflow.ts` catches an unrecognised category to `in_progress`, and
/// matching it is not pedantry: `category` is what the design system turns into a `StatusPill`
/// tone, so a phone that fell back to `draft` while the web fell back to `in_progress` would show
/// the same listing in two different colours to the same user on two screens.
AhStateCategory _categoryFromJson(String value) =>
    AhStateCategory.parseOr(value, AhStateCategory.inProgress);

String _categoryToJson(AhStateCategory value) => value.wire;

/// How consequential an action is. Presentation vocabulary, closed by the contract.
///
/// `primary` marks the single action that may wear the rationed signal yellow. Two primaries on one
/// screen is a registry bug, not a style choice.
enum ActionEmphasis {
  @JsonValue('primary')
  primary,
  @JsonValue('default')
  standard,
  @JsonValue('destructive')
  destructive,
}

@freezed
class WorkflowState with _$WorkflowState {
  const factory WorkflowState({
    /// A registry row. Never switch on this — switch on [category].
    required String code,
    required String name,
    @JsonKey(fromJson: _categoryFromJson, toJson: _categoryToJson)
    @Default(AhStateCategory.inProgress)
    AhStateCategory category,
    String? description,
    @Default(false) bool isInitial,
    @Default(false) bool isTerminal,

    /// Minutes allowed in this state before it is overdue. Drives the "due in 3h" line.
    int? slaMinutes,
  }) = _WorkflowState;

  factory WorkflowState.fromJson(Map<String, Object?> json) => _$WorkflowStateFromJson(json);
}

@freezed
class WorkflowTransition with _$WorkflowTransition {
  const factory WorkflowTransition({
    required String actionCode,
    required String label,
    required String from,
    required String to,
    String? requiredPermission,
    @Default(false) bool requiresReason,

    /// When set, the client renders this form from the form registry before firing the action.
    String? formKey,
  }) = _WorkflowTransition;

  factory WorkflowTransition.fromJson(Map<String, Object?> json) =>
      _$WorkflowTransitionFromJson(json);
}

@freezed
class WorkflowDefinition with _$WorkflowDefinition {
  const factory WorkflowDefinition({
    required String key,
    required int version,

    /// `listings.listing`, `deals.deal`, `auction.lot`.
    required String entityType,
    @Default(<WorkflowState>[]) List<WorkflowState> states,
    @Default(<WorkflowTransition>[]) List<WorkflowTransition> transitions,
  }) = _WorkflowDefinition;

  factory WorkflowDefinition.fromJson(Map<String, Object?> json) =>
      _$WorkflowDefinitionFromJson(json);
}

/// An action *this* user may fire on *this* entity right now.
///
/// Guards and permissions were evaluated server-side. The client renders buttons from this list and
/// never re-derives authorisation — a client that decides what a user may do is a client an attacker
/// can convince.
@freezed
class AvailableAction with _$AvailableAction {
  const factory AvailableAction({
    required String actionCode,
    required String label,
    @Default(false) bool requiresReason,
    @Default(false) bool requiresConfirmation,
    String? formKey,
    @JsonKey(unknownEnumValue: ActionEmphasis.standard) ActionEmphasis? emphasis,
  }) = _AvailableAction;

  factory AvailableAction.fromJson(Map<String, Object?> json) => _$AvailableActionFromJson(json);
}

@freezed
class WorkflowHistoryEntry with _$WorkflowHistoryEntry {
  const factory WorkflowHistoryEntry({
    required String to,
    String? from,
    String? actionCode,
    required String occurredAt,
    String? actor,
    String? reason,

    /// ISO-8601 duration, `PT17H10M`. Rendered as "17h 10m in Draft".
    String? timeInPreviousState,
  }) = _WorkflowHistoryEntry;

  factory WorkflowHistoryEntry.fromJson(Map<String, Object?> json) =>
      _$WorkflowHistoryEntryFromJson(json);
}

/// The live state of one entity. Returned by both the GET and the action POST — same shape, always,
/// so a screen replaces its state from the server's answer rather than guessing what a transition
/// did.
@freezed
class WorkflowSnapshot with _$WorkflowSnapshot {
  const factory WorkflowSnapshot({
    required String workflowKey,
    required WorkflowState currentState,
    required String enteredAt,
    String? dueAt,
    @Default(<AvailableAction>[]) List<AvailableAction> availableActions,
    @Default(<WorkflowHistoryEntry>[]) List<WorkflowHistoryEntry> history,
  }) = _WorkflowSnapshot;

  factory WorkflowSnapshot.fromJson(Map<String, Object?> json) => _$WorkflowSnapshotFromJson(json);
}

@freezed
class WorkflowActionRequest with _$WorkflowActionRequest {
  const factory WorkflowActionRequest({
    required String actionCode,
    String? reason,

    /// Payload for the transition's `formKey`, when it has one.
    Map<String, Object?>? payload,
  }) = _WorkflowActionRequest;

  factory WorkflowActionRequest.fromJson(Map<String, Object?> json) =>
      _$WorkflowActionRequestFromJson(json);
}
