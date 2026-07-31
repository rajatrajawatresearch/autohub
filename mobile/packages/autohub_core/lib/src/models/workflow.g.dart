// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'workflow.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$WorkflowStateImpl _$$WorkflowStateImplFromJson(Map<String, dynamic> json) =>
    _$WorkflowStateImpl(
      code: json['code'] as String,
      name: json['name'] as String,
      category: json['category'] == null
          ? AhStateCategory.inProgress
          : _categoryFromJson(json['category'] as String),
      description: json['description'] as String?,
      isInitial: json['isInitial'] as bool? ?? false,
      isTerminal: json['isTerminal'] as bool? ?? false,
      slaMinutes: (json['slaMinutes'] as num?)?.toInt(),
    );

Map<String, dynamic> _$$WorkflowStateImplToJson(_$WorkflowStateImpl instance) =>
    <String, dynamic>{
      'code': instance.code,
      'name': instance.name,
      'category': _categoryToJson(instance.category),
      'description': instance.description,
      'isInitial': instance.isInitial,
      'isTerminal': instance.isTerminal,
      'slaMinutes': instance.slaMinutes,
    };

_$WorkflowTransitionImpl _$$WorkflowTransitionImplFromJson(
  Map<String, dynamic> json,
) => _$WorkflowTransitionImpl(
  actionCode: json['actionCode'] as String,
  label: json['label'] as String,
  from: json['from'] as String,
  to: json['to'] as String,
  requiredPermission: json['requiredPermission'] as String?,
  requiresReason: json['requiresReason'] as bool? ?? false,
  formKey: json['formKey'] as String?,
);

Map<String, dynamic> _$$WorkflowTransitionImplToJson(
  _$WorkflowTransitionImpl instance,
) => <String, dynamic>{
  'actionCode': instance.actionCode,
  'label': instance.label,
  'from': instance.from,
  'to': instance.to,
  'requiredPermission': instance.requiredPermission,
  'requiresReason': instance.requiresReason,
  'formKey': instance.formKey,
};

_$WorkflowDefinitionImpl _$$WorkflowDefinitionImplFromJson(
  Map<String, dynamic> json,
) => _$WorkflowDefinitionImpl(
  key: json['key'] as String,
  version: (json['version'] as num).toInt(),
  entityType: json['entityType'] as String,
  states:
      (json['states'] as List<dynamic>?)
          ?.map((e) => WorkflowState.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <WorkflowState>[],
  transitions:
      (json['transitions'] as List<dynamic>?)
          ?.map((e) => WorkflowTransition.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <WorkflowTransition>[],
);

Map<String, dynamic> _$$WorkflowDefinitionImplToJson(
  _$WorkflowDefinitionImpl instance,
) => <String, dynamic>{
  'key': instance.key,
  'version': instance.version,
  'entityType': instance.entityType,
  'states': instance.states,
  'transitions': instance.transitions,
};

_$AvailableActionImpl _$$AvailableActionImplFromJson(
  Map<String, dynamic> json,
) => _$AvailableActionImpl(
  actionCode: json['actionCode'] as String,
  label: json['label'] as String,
  requiresReason: json['requiresReason'] as bool? ?? false,
  requiresConfirmation: json['requiresConfirmation'] as bool? ?? false,
  formKey: json['formKey'] as String?,
  emphasis: $enumDecodeNullable(
    _$ActionEmphasisEnumMap,
    json['emphasis'],
    unknownValue: ActionEmphasis.standard,
  ),
);

Map<String, dynamic> _$$AvailableActionImplToJson(
  _$AvailableActionImpl instance,
) => <String, dynamic>{
  'actionCode': instance.actionCode,
  'label': instance.label,
  'requiresReason': instance.requiresReason,
  'requiresConfirmation': instance.requiresConfirmation,
  'formKey': instance.formKey,
  'emphasis': _$ActionEmphasisEnumMap[instance.emphasis],
};

const _$ActionEmphasisEnumMap = {
  ActionEmphasis.primary: 'primary',
  ActionEmphasis.standard: 'default',
  ActionEmphasis.destructive: 'destructive',
};

_$WorkflowHistoryEntryImpl _$$WorkflowHistoryEntryImplFromJson(
  Map<String, dynamic> json,
) => _$WorkflowHistoryEntryImpl(
  to: json['to'] as String,
  from: json['from'] as String?,
  actionCode: json['actionCode'] as String?,
  occurredAt: json['occurredAt'] as String,
  actor: json['actor'] as String?,
  reason: json['reason'] as String?,
  timeInPreviousState: json['timeInPreviousState'] as String?,
);

Map<String, dynamic> _$$WorkflowHistoryEntryImplToJson(
  _$WorkflowHistoryEntryImpl instance,
) => <String, dynamic>{
  'to': instance.to,
  'from': instance.from,
  'actionCode': instance.actionCode,
  'occurredAt': instance.occurredAt,
  'actor': instance.actor,
  'reason': instance.reason,
  'timeInPreviousState': instance.timeInPreviousState,
};

_$WorkflowSnapshotImpl _$$WorkflowSnapshotImplFromJson(
  Map<String, dynamic> json,
) => _$WorkflowSnapshotImpl(
  workflowKey: json['workflowKey'] as String,
  currentState: WorkflowState.fromJson(
    json['currentState'] as Map<String, dynamic>,
  ),
  enteredAt: json['enteredAt'] as String,
  dueAt: json['dueAt'] as String?,
  availableActions:
      (json['availableActions'] as List<dynamic>?)
          ?.map((e) => AvailableAction.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <AvailableAction>[],
  history:
      (json['history'] as List<dynamic>?)
          ?.map((e) => WorkflowHistoryEntry.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <WorkflowHistoryEntry>[],
);

Map<String, dynamic> _$$WorkflowSnapshotImplToJson(
  _$WorkflowSnapshotImpl instance,
) => <String, dynamic>{
  'workflowKey': instance.workflowKey,
  'currentState': instance.currentState,
  'enteredAt': instance.enteredAt,
  'dueAt': instance.dueAt,
  'availableActions': instance.availableActions,
  'history': instance.history,
};

_$WorkflowActionRequestImpl _$$WorkflowActionRequestImplFromJson(
  Map<String, dynamic> json,
) => _$WorkflowActionRequestImpl(
  actionCode: json['actionCode'] as String,
  reason: json['reason'] as String?,
  payload: json['payload'] as Map<String, dynamic>?,
);

Map<String, dynamic> _$$WorkflowActionRequestImplToJson(
  _$WorkflowActionRequestImpl instance,
) => <String, dynamic>{
  'actionCode': instance.actionCode,
  'reason': instance.reason,
  'payload': instance.payload,
};
