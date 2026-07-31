// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'workflow.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

WorkflowState _$WorkflowStateFromJson(Map<String, dynamic> json) {
  return _WorkflowState.fromJson(json);
}

/// @nodoc
mixin _$WorkflowState {
  /// A registry row. Never switch on this — switch on [category].
  String get code => throw _privateConstructorUsedError;
  String get name => throw _privateConstructorUsedError;
  @JsonKey(fromJson: _categoryFromJson, toJson: _categoryToJson)
  AhStateCategory get category => throw _privateConstructorUsedError;
  String? get description => throw _privateConstructorUsedError;
  bool get isInitial => throw _privateConstructorUsedError;
  bool get isTerminal => throw _privateConstructorUsedError;

  /// Minutes allowed in this state before it is overdue. Drives the "due in 3h" line.
  int? get slaMinutes => throw _privateConstructorUsedError;

  /// Serializes this WorkflowState to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of WorkflowState
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $WorkflowStateCopyWith<WorkflowState> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $WorkflowStateCopyWith<$Res> {
  factory $WorkflowStateCopyWith(
    WorkflowState value,
    $Res Function(WorkflowState) then,
  ) = _$WorkflowStateCopyWithImpl<$Res, WorkflowState>;
  @useResult
  $Res call({
    String code,
    String name,
    @JsonKey(fromJson: _categoryFromJson, toJson: _categoryToJson)
    AhStateCategory category,
    String? description,
    bool isInitial,
    bool isTerminal,
    int? slaMinutes,
  });
}

/// @nodoc
class _$WorkflowStateCopyWithImpl<$Res, $Val extends WorkflowState>
    implements $WorkflowStateCopyWith<$Res> {
  _$WorkflowStateCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of WorkflowState
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? name = null,
    Object? category = null,
    Object? description = freezed,
    Object? isInitial = null,
    Object? isTerminal = null,
    Object? slaMinutes = freezed,
  }) {
    return _then(
      _value.copyWith(
            code: null == code
                ? _value.code
                : code // ignore: cast_nullable_to_non_nullable
                      as String,
            name: null == name
                ? _value.name
                : name // ignore: cast_nullable_to_non_nullable
                      as String,
            category: null == category
                ? _value.category
                : category // ignore: cast_nullable_to_non_nullable
                      as AhStateCategory,
            description: freezed == description
                ? _value.description
                : description // ignore: cast_nullable_to_non_nullable
                      as String?,
            isInitial: null == isInitial
                ? _value.isInitial
                : isInitial // ignore: cast_nullable_to_non_nullable
                      as bool,
            isTerminal: null == isTerminal
                ? _value.isTerminal
                : isTerminal // ignore: cast_nullable_to_non_nullable
                      as bool,
            slaMinutes: freezed == slaMinutes
                ? _value.slaMinutes
                : slaMinutes // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$WorkflowStateImplCopyWith<$Res>
    implements $WorkflowStateCopyWith<$Res> {
  factory _$$WorkflowStateImplCopyWith(
    _$WorkflowStateImpl value,
    $Res Function(_$WorkflowStateImpl) then,
  ) = __$$WorkflowStateImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String code,
    String name,
    @JsonKey(fromJson: _categoryFromJson, toJson: _categoryToJson)
    AhStateCategory category,
    String? description,
    bool isInitial,
    bool isTerminal,
    int? slaMinutes,
  });
}

/// @nodoc
class __$$WorkflowStateImplCopyWithImpl<$Res>
    extends _$WorkflowStateCopyWithImpl<$Res, _$WorkflowStateImpl>
    implements _$$WorkflowStateImplCopyWith<$Res> {
  __$$WorkflowStateImplCopyWithImpl(
    _$WorkflowStateImpl _value,
    $Res Function(_$WorkflowStateImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of WorkflowState
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? name = null,
    Object? category = null,
    Object? description = freezed,
    Object? isInitial = null,
    Object? isTerminal = null,
    Object? slaMinutes = freezed,
  }) {
    return _then(
      _$WorkflowStateImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        name: null == name
            ? _value.name
            : name // ignore: cast_nullable_to_non_nullable
                  as String,
        category: null == category
            ? _value.category
            : category // ignore: cast_nullable_to_non_nullable
                  as AhStateCategory,
        description: freezed == description
            ? _value.description
            : description // ignore: cast_nullable_to_non_nullable
                  as String?,
        isInitial: null == isInitial
            ? _value.isInitial
            : isInitial // ignore: cast_nullable_to_non_nullable
                  as bool,
        isTerminal: null == isTerminal
            ? _value.isTerminal
            : isTerminal // ignore: cast_nullable_to_non_nullable
                  as bool,
        slaMinutes: freezed == slaMinutes
            ? _value.slaMinutes
            : slaMinutes // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$WorkflowStateImpl implements _WorkflowState {
  const _$WorkflowStateImpl({
    required this.code,
    required this.name,
    @JsonKey(fromJson: _categoryFromJson, toJson: _categoryToJson)
    this.category = AhStateCategory.inProgress,
    this.description,
    this.isInitial = false,
    this.isTerminal = false,
    this.slaMinutes,
  });

  factory _$WorkflowStateImpl.fromJson(Map<String, dynamic> json) =>
      _$$WorkflowStateImplFromJson(json);

  /// A registry row. Never switch on this — switch on [category].
  @override
  final String code;
  @override
  final String name;
  @override
  @JsonKey(fromJson: _categoryFromJson, toJson: _categoryToJson)
  final AhStateCategory category;
  @override
  final String? description;
  @override
  @JsonKey()
  final bool isInitial;
  @override
  @JsonKey()
  final bool isTerminal;

  /// Minutes allowed in this state before it is overdue. Drives the "due in 3h" line.
  @override
  final int? slaMinutes;

  @override
  String toString() {
    return 'WorkflowState(code: $code, name: $name, category: $category, description: $description, isInitial: $isInitial, isTerminal: $isTerminal, slaMinutes: $slaMinutes)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$WorkflowStateImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.name, name) || other.name == name) &&
            (identical(other.category, category) ||
                other.category == category) &&
            (identical(other.description, description) ||
                other.description == description) &&
            (identical(other.isInitial, isInitial) ||
                other.isInitial == isInitial) &&
            (identical(other.isTerminal, isTerminal) ||
                other.isTerminal == isTerminal) &&
            (identical(other.slaMinutes, slaMinutes) ||
                other.slaMinutes == slaMinutes));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    name,
    category,
    description,
    isInitial,
    isTerminal,
    slaMinutes,
  );

  /// Create a copy of WorkflowState
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$WorkflowStateImplCopyWith<_$WorkflowStateImpl> get copyWith =>
      __$$WorkflowStateImplCopyWithImpl<_$WorkflowStateImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$WorkflowStateImplToJson(this);
  }
}

abstract class _WorkflowState implements WorkflowState {
  const factory _WorkflowState({
    required final String code,
    required final String name,
    @JsonKey(fromJson: _categoryFromJson, toJson: _categoryToJson)
    final AhStateCategory category,
    final String? description,
    final bool isInitial,
    final bool isTerminal,
    final int? slaMinutes,
  }) = _$WorkflowStateImpl;

  factory _WorkflowState.fromJson(Map<String, dynamic> json) =
      _$WorkflowStateImpl.fromJson;

  /// A registry row. Never switch on this — switch on [category].
  @override
  String get code;
  @override
  String get name;
  @override
  @JsonKey(fromJson: _categoryFromJson, toJson: _categoryToJson)
  AhStateCategory get category;
  @override
  String? get description;
  @override
  bool get isInitial;
  @override
  bool get isTerminal;

  /// Minutes allowed in this state before it is overdue. Drives the "due in 3h" line.
  @override
  int? get slaMinutes;

  /// Create a copy of WorkflowState
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$WorkflowStateImplCopyWith<_$WorkflowStateImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

WorkflowTransition _$WorkflowTransitionFromJson(Map<String, dynamic> json) {
  return _WorkflowTransition.fromJson(json);
}

/// @nodoc
mixin _$WorkflowTransition {
  String get actionCode => throw _privateConstructorUsedError;
  String get label => throw _privateConstructorUsedError;
  String get from => throw _privateConstructorUsedError;
  String get to => throw _privateConstructorUsedError;
  String? get requiredPermission => throw _privateConstructorUsedError;
  bool get requiresReason => throw _privateConstructorUsedError;

  /// When set, the client renders this form from the form registry before firing the action.
  String? get formKey => throw _privateConstructorUsedError;

  /// Serializes this WorkflowTransition to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of WorkflowTransition
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $WorkflowTransitionCopyWith<WorkflowTransition> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $WorkflowTransitionCopyWith<$Res> {
  factory $WorkflowTransitionCopyWith(
    WorkflowTransition value,
    $Res Function(WorkflowTransition) then,
  ) = _$WorkflowTransitionCopyWithImpl<$Res, WorkflowTransition>;
  @useResult
  $Res call({
    String actionCode,
    String label,
    String from,
    String to,
    String? requiredPermission,
    bool requiresReason,
    String? formKey,
  });
}

/// @nodoc
class _$WorkflowTransitionCopyWithImpl<$Res, $Val extends WorkflowTransition>
    implements $WorkflowTransitionCopyWith<$Res> {
  _$WorkflowTransitionCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of WorkflowTransition
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? actionCode = null,
    Object? label = null,
    Object? from = null,
    Object? to = null,
    Object? requiredPermission = freezed,
    Object? requiresReason = null,
    Object? formKey = freezed,
  }) {
    return _then(
      _value.copyWith(
            actionCode: null == actionCode
                ? _value.actionCode
                : actionCode // ignore: cast_nullable_to_non_nullable
                      as String,
            label: null == label
                ? _value.label
                : label // ignore: cast_nullable_to_non_nullable
                      as String,
            from: null == from
                ? _value.from
                : from // ignore: cast_nullable_to_non_nullable
                      as String,
            to: null == to
                ? _value.to
                : to // ignore: cast_nullable_to_non_nullable
                      as String,
            requiredPermission: freezed == requiredPermission
                ? _value.requiredPermission
                : requiredPermission // ignore: cast_nullable_to_non_nullable
                      as String?,
            requiresReason: null == requiresReason
                ? _value.requiresReason
                : requiresReason // ignore: cast_nullable_to_non_nullable
                      as bool,
            formKey: freezed == formKey
                ? _value.formKey
                : formKey // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$WorkflowTransitionImplCopyWith<$Res>
    implements $WorkflowTransitionCopyWith<$Res> {
  factory _$$WorkflowTransitionImplCopyWith(
    _$WorkflowTransitionImpl value,
    $Res Function(_$WorkflowTransitionImpl) then,
  ) = __$$WorkflowTransitionImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String actionCode,
    String label,
    String from,
    String to,
    String? requiredPermission,
    bool requiresReason,
    String? formKey,
  });
}

/// @nodoc
class __$$WorkflowTransitionImplCopyWithImpl<$Res>
    extends _$WorkflowTransitionCopyWithImpl<$Res, _$WorkflowTransitionImpl>
    implements _$$WorkflowTransitionImplCopyWith<$Res> {
  __$$WorkflowTransitionImplCopyWithImpl(
    _$WorkflowTransitionImpl _value,
    $Res Function(_$WorkflowTransitionImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of WorkflowTransition
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? actionCode = null,
    Object? label = null,
    Object? from = null,
    Object? to = null,
    Object? requiredPermission = freezed,
    Object? requiresReason = null,
    Object? formKey = freezed,
  }) {
    return _then(
      _$WorkflowTransitionImpl(
        actionCode: null == actionCode
            ? _value.actionCode
            : actionCode // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        from: null == from
            ? _value.from
            : from // ignore: cast_nullable_to_non_nullable
                  as String,
        to: null == to
            ? _value.to
            : to // ignore: cast_nullable_to_non_nullable
                  as String,
        requiredPermission: freezed == requiredPermission
            ? _value.requiredPermission
            : requiredPermission // ignore: cast_nullable_to_non_nullable
                  as String?,
        requiresReason: null == requiresReason
            ? _value.requiresReason
            : requiresReason // ignore: cast_nullable_to_non_nullable
                  as bool,
        formKey: freezed == formKey
            ? _value.formKey
            : formKey // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$WorkflowTransitionImpl implements _WorkflowTransition {
  const _$WorkflowTransitionImpl({
    required this.actionCode,
    required this.label,
    required this.from,
    required this.to,
    this.requiredPermission,
    this.requiresReason = false,
    this.formKey,
  });

  factory _$WorkflowTransitionImpl.fromJson(Map<String, dynamic> json) =>
      _$$WorkflowTransitionImplFromJson(json);

  @override
  final String actionCode;
  @override
  final String label;
  @override
  final String from;
  @override
  final String to;
  @override
  final String? requiredPermission;
  @override
  @JsonKey()
  final bool requiresReason;

  /// When set, the client renders this form from the form registry before firing the action.
  @override
  final String? formKey;

  @override
  String toString() {
    return 'WorkflowTransition(actionCode: $actionCode, label: $label, from: $from, to: $to, requiredPermission: $requiredPermission, requiresReason: $requiresReason, formKey: $formKey)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$WorkflowTransitionImpl &&
            (identical(other.actionCode, actionCode) ||
                other.actionCode == actionCode) &&
            (identical(other.label, label) || other.label == label) &&
            (identical(other.from, from) || other.from == from) &&
            (identical(other.to, to) || other.to == to) &&
            (identical(other.requiredPermission, requiredPermission) ||
                other.requiredPermission == requiredPermission) &&
            (identical(other.requiresReason, requiresReason) ||
                other.requiresReason == requiresReason) &&
            (identical(other.formKey, formKey) || other.formKey == formKey));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    actionCode,
    label,
    from,
    to,
    requiredPermission,
    requiresReason,
    formKey,
  );

  /// Create a copy of WorkflowTransition
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$WorkflowTransitionImplCopyWith<_$WorkflowTransitionImpl> get copyWith =>
      __$$WorkflowTransitionImplCopyWithImpl<_$WorkflowTransitionImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$WorkflowTransitionImplToJson(this);
  }
}

abstract class _WorkflowTransition implements WorkflowTransition {
  const factory _WorkflowTransition({
    required final String actionCode,
    required final String label,
    required final String from,
    required final String to,
    final String? requiredPermission,
    final bool requiresReason,
    final String? formKey,
  }) = _$WorkflowTransitionImpl;

  factory _WorkflowTransition.fromJson(Map<String, dynamic> json) =
      _$WorkflowTransitionImpl.fromJson;

  @override
  String get actionCode;
  @override
  String get label;
  @override
  String get from;
  @override
  String get to;
  @override
  String? get requiredPermission;
  @override
  bool get requiresReason;

  /// When set, the client renders this form from the form registry before firing the action.
  @override
  String? get formKey;

  /// Create a copy of WorkflowTransition
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$WorkflowTransitionImplCopyWith<_$WorkflowTransitionImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

WorkflowDefinition _$WorkflowDefinitionFromJson(Map<String, dynamic> json) {
  return _WorkflowDefinition.fromJson(json);
}

/// @nodoc
mixin _$WorkflowDefinition {
  String get key => throw _privateConstructorUsedError;
  int get version => throw _privateConstructorUsedError;

  /// `listings.listing`, `deals.deal`, `auction.lot`.
  String get entityType => throw _privateConstructorUsedError;
  List<WorkflowState> get states => throw _privateConstructorUsedError;
  List<WorkflowTransition> get transitions =>
      throw _privateConstructorUsedError;

  /// Serializes this WorkflowDefinition to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of WorkflowDefinition
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $WorkflowDefinitionCopyWith<WorkflowDefinition> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $WorkflowDefinitionCopyWith<$Res> {
  factory $WorkflowDefinitionCopyWith(
    WorkflowDefinition value,
    $Res Function(WorkflowDefinition) then,
  ) = _$WorkflowDefinitionCopyWithImpl<$Res, WorkflowDefinition>;
  @useResult
  $Res call({
    String key,
    int version,
    String entityType,
    List<WorkflowState> states,
    List<WorkflowTransition> transitions,
  });
}

/// @nodoc
class _$WorkflowDefinitionCopyWithImpl<$Res, $Val extends WorkflowDefinition>
    implements $WorkflowDefinitionCopyWith<$Res> {
  _$WorkflowDefinitionCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of WorkflowDefinition
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? key = null,
    Object? version = null,
    Object? entityType = null,
    Object? states = null,
    Object? transitions = null,
  }) {
    return _then(
      _value.copyWith(
            key: null == key
                ? _value.key
                : key // ignore: cast_nullable_to_non_nullable
                      as String,
            version: null == version
                ? _value.version
                : version // ignore: cast_nullable_to_non_nullable
                      as int,
            entityType: null == entityType
                ? _value.entityType
                : entityType // ignore: cast_nullable_to_non_nullable
                      as String,
            states: null == states
                ? _value.states
                : states // ignore: cast_nullable_to_non_nullable
                      as List<WorkflowState>,
            transitions: null == transitions
                ? _value.transitions
                : transitions // ignore: cast_nullable_to_non_nullable
                      as List<WorkflowTransition>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$WorkflowDefinitionImplCopyWith<$Res>
    implements $WorkflowDefinitionCopyWith<$Res> {
  factory _$$WorkflowDefinitionImplCopyWith(
    _$WorkflowDefinitionImpl value,
    $Res Function(_$WorkflowDefinitionImpl) then,
  ) = __$$WorkflowDefinitionImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String key,
    int version,
    String entityType,
    List<WorkflowState> states,
    List<WorkflowTransition> transitions,
  });
}

/// @nodoc
class __$$WorkflowDefinitionImplCopyWithImpl<$Res>
    extends _$WorkflowDefinitionCopyWithImpl<$Res, _$WorkflowDefinitionImpl>
    implements _$$WorkflowDefinitionImplCopyWith<$Res> {
  __$$WorkflowDefinitionImplCopyWithImpl(
    _$WorkflowDefinitionImpl _value,
    $Res Function(_$WorkflowDefinitionImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of WorkflowDefinition
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? key = null,
    Object? version = null,
    Object? entityType = null,
    Object? states = null,
    Object? transitions = null,
  }) {
    return _then(
      _$WorkflowDefinitionImpl(
        key: null == key
            ? _value.key
            : key // ignore: cast_nullable_to_non_nullable
                  as String,
        version: null == version
            ? _value.version
            : version // ignore: cast_nullable_to_non_nullable
                  as int,
        entityType: null == entityType
            ? _value.entityType
            : entityType // ignore: cast_nullable_to_non_nullable
                  as String,
        states: null == states
            ? _value._states
            : states // ignore: cast_nullable_to_non_nullable
                  as List<WorkflowState>,
        transitions: null == transitions
            ? _value._transitions
            : transitions // ignore: cast_nullable_to_non_nullable
                  as List<WorkflowTransition>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$WorkflowDefinitionImpl implements _WorkflowDefinition {
  const _$WorkflowDefinitionImpl({
    required this.key,
    required this.version,
    required this.entityType,
    final List<WorkflowState> states = const <WorkflowState>[],
    final List<WorkflowTransition> transitions = const <WorkflowTransition>[],
  }) : _states = states,
       _transitions = transitions;

  factory _$WorkflowDefinitionImpl.fromJson(Map<String, dynamic> json) =>
      _$$WorkflowDefinitionImplFromJson(json);

  @override
  final String key;
  @override
  final int version;

  /// `listings.listing`, `deals.deal`, `auction.lot`.
  @override
  final String entityType;
  final List<WorkflowState> _states;
  @override
  @JsonKey()
  List<WorkflowState> get states {
    if (_states is EqualUnmodifiableListView) return _states;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_states);
  }

  final List<WorkflowTransition> _transitions;
  @override
  @JsonKey()
  List<WorkflowTransition> get transitions {
    if (_transitions is EqualUnmodifiableListView) return _transitions;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_transitions);
  }

  @override
  String toString() {
    return 'WorkflowDefinition(key: $key, version: $version, entityType: $entityType, states: $states, transitions: $transitions)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$WorkflowDefinitionImpl &&
            (identical(other.key, key) || other.key == key) &&
            (identical(other.version, version) || other.version == version) &&
            (identical(other.entityType, entityType) ||
                other.entityType == entityType) &&
            const DeepCollectionEquality().equals(other._states, _states) &&
            const DeepCollectionEquality().equals(
              other._transitions,
              _transitions,
            ));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    key,
    version,
    entityType,
    const DeepCollectionEquality().hash(_states),
    const DeepCollectionEquality().hash(_transitions),
  );

  /// Create a copy of WorkflowDefinition
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$WorkflowDefinitionImplCopyWith<_$WorkflowDefinitionImpl> get copyWith =>
      __$$WorkflowDefinitionImplCopyWithImpl<_$WorkflowDefinitionImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$WorkflowDefinitionImplToJson(this);
  }
}

abstract class _WorkflowDefinition implements WorkflowDefinition {
  const factory _WorkflowDefinition({
    required final String key,
    required final int version,
    required final String entityType,
    final List<WorkflowState> states,
    final List<WorkflowTransition> transitions,
  }) = _$WorkflowDefinitionImpl;

  factory _WorkflowDefinition.fromJson(Map<String, dynamic> json) =
      _$WorkflowDefinitionImpl.fromJson;

  @override
  String get key;
  @override
  int get version;

  /// `listings.listing`, `deals.deal`, `auction.lot`.
  @override
  String get entityType;
  @override
  List<WorkflowState> get states;
  @override
  List<WorkflowTransition> get transitions;

  /// Create a copy of WorkflowDefinition
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$WorkflowDefinitionImplCopyWith<_$WorkflowDefinitionImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

AvailableAction _$AvailableActionFromJson(Map<String, dynamic> json) {
  return _AvailableAction.fromJson(json);
}

/// @nodoc
mixin _$AvailableAction {
  String get actionCode => throw _privateConstructorUsedError;
  String get label => throw _privateConstructorUsedError;
  bool get requiresReason => throw _privateConstructorUsedError;
  bool get requiresConfirmation => throw _privateConstructorUsedError;
  String? get formKey => throw _privateConstructorUsedError;
  @JsonKey(unknownEnumValue: ActionEmphasis.standard)
  ActionEmphasis? get emphasis => throw _privateConstructorUsedError;

  /// Serializes this AvailableAction to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of AvailableAction
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $AvailableActionCopyWith<AvailableAction> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $AvailableActionCopyWith<$Res> {
  factory $AvailableActionCopyWith(
    AvailableAction value,
    $Res Function(AvailableAction) then,
  ) = _$AvailableActionCopyWithImpl<$Res, AvailableAction>;
  @useResult
  $Res call({
    String actionCode,
    String label,
    bool requiresReason,
    bool requiresConfirmation,
    String? formKey,
    @JsonKey(unknownEnumValue: ActionEmphasis.standard)
    ActionEmphasis? emphasis,
  });
}

/// @nodoc
class _$AvailableActionCopyWithImpl<$Res, $Val extends AvailableAction>
    implements $AvailableActionCopyWith<$Res> {
  _$AvailableActionCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of AvailableAction
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? actionCode = null,
    Object? label = null,
    Object? requiresReason = null,
    Object? requiresConfirmation = null,
    Object? formKey = freezed,
    Object? emphasis = freezed,
  }) {
    return _then(
      _value.copyWith(
            actionCode: null == actionCode
                ? _value.actionCode
                : actionCode // ignore: cast_nullable_to_non_nullable
                      as String,
            label: null == label
                ? _value.label
                : label // ignore: cast_nullable_to_non_nullable
                      as String,
            requiresReason: null == requiresReason
                ? _value.requiresReason
                : requiresReason // ignore: cast_nullable_to_non_nullable
                      as bool,
            requiresConfirmation: null == requiresConfirmation
                ? _value.requiresConfirmation
                : requiresConfirmation // ignore: cast_nullable_to_non_nullable
                      as bool,
            formKey: freezed == formKey
                ? _value.formKey
                : formKey // ignore: cast_nullable_to_non_nullable
                      as String?,
            emphasis: freezed == emphasis
                ? _value.emphasis
                : emphasis // ignore: cast_nullable_to_non_nullable
                      as ActionEmphasis?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$AvailableActionImplCopyWith<$Res>
    implements $AvailableActionCopyWith<$Res> {
  factory _$$AvailableActionImplCopyWith(
    _$AvailableActionImpl value,
    $Res Function(_$AvailableActionImpl) then,
  ) = __$$AvailableActionImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String actionCode,
    String label,
    bool requiresReason,
    bool requiresConfirmation,
    String? formKey,
    @JsonKey(unknownEnumValue: ActionEmphasis.standard)
    ActionEmphasis? emphasis,
  });
}

/// @nodoc
class __$$AvailableActionImplCopyWithImpl<$Res>
    extends _$AvailableActionCopyWithImpl<$Res, _$AvailableActionImpl>
    implements _$$AvailableActionImplCopyWith<$Res> {
  __$$AvailableActionImplCopyWithImpl(
    _$AvailableActionImpl _value,
    $Res Function(_$AvailableActionImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AvailableAction
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? actionCode = null,
    Object? label = null,
    Object? requiresReason = null,
    Object? requiresConfirmation = null,
    Object? formKey = freezed,
    Object? emphasis = freezed,
  }) {
    return _then(
      _$AvailableActionImpl(
        actionCode: null == actionCode
            ? _value.actionCode
            : actionCode // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        requiresReason: null == requiresReason
            ? _value.requiresReason
            : requiresReason // ignore: cast_nullable_to_non_nullable
                  as bool,
        requiresConfirmation: null == requiresConfirmation
            ? _value.requiresConfirmation
            : requiresConfirmation // ignore: cast_nullable_to_non_nullable
                  as bool,
        formKey: freezed == formKey
            ? _value.formKey
            : formKey // ignore: cast_nullable_to_non_nullable
                  as String?,
        emphasis: freezed == emphasis
            ? _value.emphasis
            : emphasis // ignore: cast_nullable_to_non_nullable
                  as ActionEmphasis?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$AvailableActionImpl implements _AvailableAction {
  const _$AvailableActionImpl({
    required this.actionCode,
    required this.label,
    this.requiresReason = false,
    this.requiresConfirmation = false,
    this.formKey,
    @JsonKey(unknownEnumValue: ActionEmphasis.standard) this.emphasis,
  });

  factory _$AvailableActionImpl.fromJson(Map<String, dynamic> json) =>
      _$$AvailableActionImplFromJson(json);

  @override
  final String actionCode;
  @override
  final String label;
  @override
  @JsonKey()
  final bool requiresReason;
  @override
  @JsonKey()
  final bool requiresConfirmation;
  @override
  final String? formKey;
  @override
  @JsonKey(unknownEnumValue: ActionEmphasis.standard)
  final ActionEmphasis? emphasis;

  @override
  String toString() {
    return 'AvailableAction(actionCode: $actionCode, label: $label, requiresReason: $requiresReason, requiresConfirmation: $requiresConfirmation, formKey: $formKey, emphasis: $emphasis)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AvailableActionImpl &&
            (identical(other.actionCode, actionCode) ||
                other.actionCode == actionCode) &&
            (identical(other.label, label) || other.label == label) &&
            (identical(other.requiresReason, requiresReason) ||
                other.requiresReason == requiresReason) &&
            (identical(other.requiresConfirmation, requiresConfirmation) ||
                other.requiresConfirmation == requiresConfirmation) &&
            (identical(other.formKey, formKey) || other.formKey == formKey) &&
            (identical(other.emphasis, emphasis) ||
                other.emphasis == emphasis));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    actionCode,
    label,
    requiresReason,
    requiresConfirmation,
    formKey,
    emphasis,
  );

  /// Create a copy of AvailableAction
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AvailableActionImplCopyWith<_$AvailableActionImpl> get copyWith =>
      __$$AvailableActionImplCopyWithImpl<_$AvailableActionImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$AvailableActionImplToJson(this);
  }
}

abstract class _AvailableAction implements AvailableAction {
  const factory _AvailableAction({
    required final String actionCode,
    required final String label,
    final bool requiresReason,
    final bool requiresConfirmation,
    final String? formKey,
    @JsonKey(unknownEnumValue: ActionEmphasis.standard)
    final ActionEmphasis? emphasis,
  }) = _$AvailableActionImpl;

  factory _AvailableAction.fromJson(Map<String, dynamic> json) =
      _$AvailableActionImpl.fromJson;

  @override
  String get actionCode;
  @override
  String get label;
  @override
  bool get requiresReason;
  @override
  bool get requiresConfirmation;
  @override
  String? get formKey;
  @override
  @JsonKey(unknownEnumValue: ActionEmphasis.standard)
  ActionEmphasis? get emphasis;

  /// Create a copy of AvailableAction
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AvailableActionImplCopyWith<_$AvailableActionImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

WorkflowHistoryEntry _$WorkflowHistoryEntryFromJson(Map<String, dynamic> json) {
  return _WorkflowHistoryEntry.fromJson(json);
}

/// @nodoc
mixin _$WorkflowHistoryEntry {
  String get to => throw _privateConstructorUsedError;
  String? get from => throw _privateConstructorUsedError;
  String? get actionCode => throw _privateConstructorUsedError;
  String get occurredAt => throw _privateConstructorUsedError;
  String? get actor => throw _privateConstructorUsedError;
  String? get reason => throw _privateConstructorUsedError;

  /// ISO-8601 duration, `PT17H10M`. Rendered as "17h 10m in Draft".
  String? get timeInPreviousState => throw _privateConstructorUsedError;

  /// Serializes this WorkflowHistoryEntry to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of WorkflowHistoryEntry
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $WorkflowHistoryEntryCopyWith<WorkflowHistoryEntry> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $WorkflowHistoryEntryCopyWith<$Res> {
  factory $WorkflowHistoryEntryCopyWith(
    WorkflowHistoryEntry value,
    $Res Function(WorkflowHistoryEntry) then,
  ) = _$WorkflowHistoryEntryCopyWithImpl<$Res, WorkflowHistoryEntry>;
  @useResult
  $Res call({
    String to,
    String? from,
    String? actionCode,
    String occurredAt,
    String? actor,
    String? reason,
    String? timeInPreviousState,
  });
}

/// @nodoc
class _$WorkflowHistoryEntryCopyWithImpl<
  $Res,
  $Val extends WorkflowHistoryEntry
>
    implements $WorkflowHistoryEntryCopyWith<$Res> {
  _$WorkflowHistoryEntryCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of WorkflowHistoryEntry
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? to = null,
    Object? from = freezed,
    Object? actionCode = freezed,
    Object? occurredAt = null,
    Object? actor = freezed,
    Object? reason = freezed,
    Object? timeInPreviousState = freezed,
  }) {
    return _then(
      _value.copyWith(
            to: null == to
                ? _value.to
                : to // ignore: cast_nullable_to_non_nullable
                      as String,
            from: freezed == from
                ? _value.from
                : from // ignore: cast_nullable_to_non_nullable
                      as String?,
            actionCode: freezed == actionCode
                ? _value.actionCode
                : actionCode // ignore: cast_nullable_to_non_nullable
                      as String?,
            occurredAt: null == occurredAt
                ? _value.occurredAt
                : occurredAt // ignore: cast_nullable_to_non_nullable
                      as String,
            actor: freezed == actor
                ? _value.actor
                : actor // ignore: cast_nullable_to_non_nullable
                      as String?,
            reason: freezed == reason
                ? _value.reason
                : reason // ignore: cast_nullable_to_non_nullable
                      as String?,
            timeInPreviousState: freezed == timeInPreviousState
                ? _value.timeInPreviousState
                : timeInPreviousState // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$WorkflowHistoryEntryImplCopyWith<$Res>
    implements $WorkflowHistoryEntryCopyWith<$Res> {
  factory _$$WorkflowHistoryEntryImplCopyWith(
    _$WorkflowHistoryEntryImpl value,
    $Res Function(_$WorkflowHistoryEntryImpl) then,
  ) = __$$WorkflowHistoryEntryImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String to,
    String? from,
    String? actionCode,
    String occurredAt,
    String? actor,
    String? reason,
    String? timeInPreviousState,
  });
}

/// @nodoc
class __$$WorkflowHistoryEntryImplCopyWithImpl<$Res>
    extends _$WorkflowHistoryEntryCopyWithImpl<$Res, _$WorkflowHistoryEntryImpl>
    implements _$$WorkflowHistoryEntryImplCopyWith<$Res> {
  __$$WorkflowHistoryEntryImplCopyWithImpl(
    _$WorkflowHistoryEntryImpl _value,
    $Res Function(_$WorkflowHistoryEntryImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of WorkflowHistoryEntry
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? to = null,
    Object? from = freezed,
    Object? actionCode = freezed,
    Object? occurredAt = null,
    Object? actor = freezed,
    Object? reason = freezed,
    Object? timeInPreviousState = freezed,
  }) {
    return _then(
      _$WorkflowHistoryEntryImpl(
        to: null == to
            ? _value.to
            : to // ignore: cast_nullable_to_non_nullable
                  as String,
        from: freezed == from
            ? _value.from
            : from // ignore: cast_nullable_to_non_nullable
                  as String?,
        actionCode: freezed == actionCode
            ? _value.actionCode
            : actionCode // ignore: cast_nullable_to_non_nullable
                  as String?,
        occurredAt: null == occurredAt
            ? _value.occurredAt
            : occurredAt // ignore: cast_nullable_to_non_nullable
                  as String,
        actor: freezed == actor
            ? _value.actor
            : actor // ignore: cast_nullable_to_non_nullable
                  as String?,
        reason: freezed == reason
            ? _value.reason
            : reason // ignore: cast_nullable_to_non_nullable
                  as String?,
        timeInPreviousState: freezed == timeInPreviousState
            ? _value.timeInPreviousState
            : timeInPreviousState // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$WorkflowHistoryEntryImpl implements _WorkflowHistoryEntry {
  const _$WorkflowHistoryEntryImpl({
    required this.to,
    this.from,
    this.actionCode,
    required this.occurredAt,
    this.actor,
    this.reason,
    this.timeInPreviousState,
  });

  factory _$WorkflowHistoryEntryImpl.fromJson(Map<String, dynamic> json) =>
      _$$WorkflowHistoryEntryImplFromJson(json);

  @override
  final String to;
  @override
  final String? from;
  @override
  final String? actionCode;
  @override
  final String occurredAt;
  @override
  final String? actor;
  @override
  final String? reason;

  /// ISO-8601 duration, `PT17H10M`. Rendered as "17h 10m in Draft".
  @override
  final String? timeInPreviousState;

  @override
  String toString() {
    return 'WorkflowHistoryEntry(to: $to, from: $from, actionCode: $actionCode, occurredAt: $occurredAt, actor: $actor, reason: $reason, timeInPreviousState: $timeInPreviousState)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$WorkflowHistoryEntryImpl &&
            (identical(other.to, to) || other.to == to) &&
            (identical(other.from, from) || other.from == from) &&
            (identical(other.actionCode, actionCode) ||
                other.actionCode == actionCode) &&
            (identical(other.occurredAt, occurredAt) ||
                other.occurredAt == occurredAt) &&
            (identical(other.actor, actor) || other.actor == actor) &&
            (identical(other.reason, reason) || other.reason == reason) &&
            (identical(other.timeInPreviousState, timeInPreviousState) ||
                other.timeInPreviousState == timeInPreviousState));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    to,
    from,
    actionCode,
    occurredAt,
    actor,
    reason,
    timeInPreviousState,
  );

  /// Create a copy of WorkflowHistoryEntry
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$WorkflowHistoryEntryImplCopyWith<_$WorkflowHistoryEntryImpl>
  get copyWith =>
      __$$WorkflowHistoryEntryImplCopyWithImpl<_$WorkflowHistoryEntryImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$WorkflowHistoryEntryImplToJson(this);
  }
}

abstract class _WorkflowHistoryEntry implements WorkflowHistoryEntry {
  const factory _WorkflowHistoryEntry({
    required final String to,
    final String? from,
    final String? actionCode,
    required final String occurredAt,
    final String? actor,
    final String? reason,
    final String? timeInPreviousState,
  }) = _$WorkflowHistoryEntryImpl;

  factory _WorkflowHistoryEntry.fromJson(Map<String, dynamic> json) =
      _$WorkflowHistoryEntryImpl.fromJson;

  @override
  String get to;
  @override
  String? get from;
  @override
  String? get actionCode;
  @override
  String get occurredAt;
  @override
  String? get actor;
  @override
  String? get reason;

  /// ISO-8601 duration, `PT17H10M`. Rendered as "17h 10m in Draft".
  @override
  String? get timeInPreviousState;

  /// Create a copy of WorkflowHistoryEntry
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$WorkflowHistoryEntryImplCopyWith<_$WorkflowHistoryEntryImpl>
  get copyWith => throw _privateConstructorUsedError;
}

WorkflowSnapshot _$WorkflowSnapshotFromJson(Map<String, dynamic> json) {
  return _WorkflowSnapshot.fromJson(json);
}

/// @nodoc
mixin _$WorkflowSnapshot {
  String get workflowKey => throw _privateConstructorUsedError;
  WorkflowState get currentState => throw _privateConstructorUsedError;
  String get enteredAt => throw _privateConstructorUsedError;
  String? get dueAt => throw _privateConstructorUsedError;
  List<AvailableAction> get availableActions =>
      throw _privateConstructorUsedError;
  List<WorkflowHistoryEntry> get history => throw _privateConstructorUsedError;

  /// Serializes this WorkflowSnapshot to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of WorkflowSnapshot
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $WorkflowSnapshotCopyWith<WorkflowSnapshot> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $WorkflowSnapshotCopyWith<$Res> {
  factory $WorkflowSnapshotCopyWith(
    WorkflowSnapshot value,
    $Res Function(WorkflowSnapshot) then,
  ) = _$WorkflowSnapshotCopyWithImpl<$Res, WorkflowSnapshot>;
  @useResult
  $Res call({
    String workflowKey,
    WorkflowState currentState,
    String enteredAt,
    String? dueAt,
    List<AvailableAction> availableActions,
    List<WorkflowHistoryEntry> history,
  });

  $WorkflowStateCopyWith<$Res> get currentState;
}

/// @nodoc
class _$WorkflowSnapshotCopyWithImpl<$Res, $Val extends WorkflowSnapshot>
    implements $WorkflowSnapshotCopyWith<$Res> {
  _$WorkflowSnapshotCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of WorkflowSnapshot
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? workflowKey = null,
    Object? currentState = null,
    Object? enteredAt = null,
    Object? dueAt = freezed,
    Object? availableActions = null,
    Object? history = null,
  }) {
    return _then(
      _value.copyWith(
            workflowKey: null == workflowKey
                ? _value.workflowKey
                : workflowKey // ignore: cast_nullable_to_non_nullable
                      as String,
            currentState: null == currentState
                ? _value.currentState
                : currentState // ignore: cast_nullable_to_non_nullable
                      as WorkflowState,
            enteredAt: null == enteredAt
                ? _value.enteredAt
                : enteredAt // ignore: cast_nullable_to_non_nullable
                      as String,
            dueAt: freezed == dueAt
                ? _value.dueAt
                : dueAt // ignore: cast_nullable_to_non_nullable
                      as String?,
            availableActions: null == availableActions
                ? _value.availableActions
                : availableActions // ignore: cast_nullable_to_non_nullable
                      as List<AvailableAction>,
            history: null == history
                ? _value.history
                : history // ignore: cast_nullable_to_non_nullable
                      as List<WorkflowHistoryEntry>,
          )
          as $Val,
    );
  }

  /// Create a copy of WorkflowSnapshot
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $WorkflowStateCopyWith<$Res> get currentState {
    return $WorkflowStateCopyWith<$Res>(_value.currentState, (value) {
      return _then(_value.copyWith(currentState: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$WorkflowSnapshotImplCopyWith<$Res>
    implements $WorkflowSnapshotCopyWith<$Res> {
  factory _$$WorkflowSnapshotImplCopyWith(
    _$WorkflowSnapshotImpl value,
    $Res Function(_$WorkflowSnapshotImpl) then,
  ) = __$$WorkflowSnapshotImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String workflowKey,
    WorkflowState currentState,
    String enteredAt,
    String? dueAt,
    List<AvailableAction> availableActions,
    List<WorkflowHistoryEntry> history,
  });

  @override
  $WorkflowStateCopyWith<$Res> get currentState;
}

/// @nodoc
class __$$WorkflowSnapshotImplCopyWithImpl<$Res>
    extends _$WorkflowSnapshotCopyWithImpl<$Res, _$WorkflowSnapshotImpl>
    implements _$$WorkflowSnapshotImplCopyWith<$Res> {
  __$$WorkflowSnapshotImplCopyWithImpl(
    _$WorkflowSnapshotImpl _value,
    $Res Function(_$WorkflowSnapshotImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of WorkflowSnapshot
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? workflowKey = null,
    Object? currentState = null,
    Object? enteredAt = null,
    Object? dueAt = freezed,
    Object? availableActions = null,
    Object? history = null,
  }) {
    return _then(
      _$WorkflowSnapshotImpl(
        workflowKey: null == workflowKey
            ? _value.workflowKey
            : workflowKey // ignore: cast_nullable_to_non_nullable
                  as String,
        currentState: null == currentState
            ? _value.currentState
            : currentState // ignore: cast_nullable_to_non_nullable
                  as WorkflowState,
        enteredAt: null == enteredAt
            ? _value.enteredAt
            : enteredAt // ignore: cast_nullable_to_non_nullable
                  as String,
        dueAt: freezed == dueAt
            ? _value.dueAt
            : dueAt // ignore: cast_nullable_to_non_nullable
                  as String?,
        availableActions: null == availableActions
            ? _value._availableActions
            : availableActions // ignore: cast_nullable_to_non_nullable
                  as List<AvailableAction>,
        history: null == history
            ? _value._history
            : history // ignore: cast_nullable_to_non_nullable
                  as List<WorkflowHistoryEntry>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$WorkflowSnapshotImpl implements _WorkflowSnapshot {
  const _$WorkflowSnapshotImpl({
    required this.workflowKey,
    required this.currentState,
    required this.enteredAt,
    this.dueAt,
    final List<AvailableAction> availableActions = const <AvailableAction>[],
    final List<WorkflowHistoryEntry> history = const <WorkflowHistoryEntry>[],
  }) : _availableActions = availableActions,
       _history = history;

  factory _$WorkflowSnapshotImpl.fromJson(Map<String, dynamic> json) =>
      _$$WorkflowSnapshotImplFromJson(json);

  @override
  final String workflowKey;
  @override
  final WorkflowState currentState;
  @override
  final String enteredAt;
  @override
  final String? dueAt;
  final List<AvailableAction> _availableActions;
  @override
  @JsonKey()
  List<AvailableAction> get availableActions {
    if (_availableActions is EqualUnmodifiableListView)
      return _availableActions;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_availableActions);
  }

  final List<WorkflowHistoryEntry> _history;
  @override
  @JsonKey()
  List<WorkflowHistoryEntry> get history {
    if (_history is EqualUnmodifiableListView) return _history;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_history);
  }

  @override
  String toString() {
    return 'WorkflowSnapshot(workflowKey: $workflowKey, currentState: $currentState, enteredAt: $enteredAt, dueAt: $dueAt, availableActions: $availableActions, history: $history)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$WorkflowSnapshotImpl &&
            (identical(other.workflowKey, workflowKey) ||
                other.workflowKey == workflowKey) &&
            (identical(other.currentState, currentState) ||
                other.currentState == currentState) &&
            (identical(other.enteredAt, enteredAt) ||
                other.enteredAt == enteredAt) &&
            (identical(other.dueAt, dueAt) || other.dueAt == dueAt) &&
            const DeepCollectionEquality().equals(
              other._availableActions,
              _availableActions,
            ) &&
            const DeepCollectionEquality().equals(other._history, _history));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    workflowKey,
    currentState,
    enteredAt,
    dueAt,
    const DeepCollectionEquality().hash(_availableActions),
    const DeepCollectionEquality().hash(_history),
  );

  /// Create a copy of WorkflowSnapshot
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$WorkflowSnapshotImplCopyWith<_$WorkflowSnapshotImpl> get copyWith =>
      __$$WorkflowSnapshotImplCopyWithImpl<_$WorkflowSnapshotImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$WorkflowSnapshotImplToJson(this);
  }
}

abstract class _WorkflowSnapshot implements WorkflowSnapshot {
  const factory _WorkflowSnapshot({
    required final String workflowKey,
    required final WorkflowState currentState,
    required final String enteredAt,
    final String? dueAt,
    final List<AvailableAction> availableActions,
    final List<WorkflowHistoryEntry> history,
  }) = _$WorkflowSnapshotImpl;

  factory _WorkflowSnapshot.fromJson(Map<String, dynamic> json) =
      _$WorkflowSnapshotImpl.fromJson;

  @override
  String get workflowKey;
  @override
  WorkflowState get currentState;
  @override
  String get enteredAt;
  @override
  String? get dueAt;
  @override
  List<AvailableAction> get availableActions;
  @override
  List<WorkflowHistoryEntry> get history;

  /// Create a copy of WorkflowSnapshot
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$WorkflowSnapshotImplCopyWith<_$WorkflowSnapshotImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

WorkflowActionRequest _$WorkflowActionRequestFromJson(
  Map<String, dynamic> json,
) {
  return _WorkflowActionRequest.fromJson(json);
}

/// @nodoc
mixin _$WorkflowActionRequest {
  String get actionCode => throw _privateConstructorUsedError;
  String? get reason => throw _privateConstructorUsedError;

  /// Payload for the transition's `formKey`, when it has one.
  Map<String, Object?>? get payload => throw _privateConstructorUsedError;

  /// Serializes this WorkflowActionRequest to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of WorkflowActionRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $WorkflowActionRequestCopyWith<WorkflowActionRequest> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $WorkflowActionRequestCopyWith<$Res> {
  factory $WorkflowActionRequestCopyWith(
    WorkflowActionRequest value,
    $Res Function(WorkflowActionRequest) then,
  ) = _$WorkflowActionRequestCopyWithImpl<$Res, WorkflowActionRequest>;
  @useResult
  $Res call({String actionCode, String? reason, Map<String, Object?>? payload});
}

/// @nodoc
class _$WorkflowActionRequestCopyWithImpl<
  $Res,
  $Val extends WorkflowActionRequest
>
    implements $WorkflowActionRequestCopyWith<$Res> {
  _$WorkflowActionRequestCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of WorkflowActionRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? actionCode = null,
    Object? reason = freezed,
    Object? payload = freezed,
  }) {
    return _then(
      _value.copyWith(
            actionCode: null == actionCode
                ? _value.actionCode
                : actionCode // ignore: cast_nullable_to_non_nullable
                      as String,
            reason: freezed == reason
                ? _value.reason
                : reason // ignore: cast_nullable_to_non_nullable
                      as String?,
            payload: freezed == payload
                ? _value.payload
                : payload // ignore: cast_nullable_to_non_nullable
                      as Map<String, Object?>?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$WorkflowActionRequestImplCopyWith<$Res>
    implements $WorkflowActionRequestCopyWith<$Res> {
  factory _$$WorkflowActionRequestImplCopyWith(
    _$WorkflowActionRequestImpl value,
    $Res Function(_$WorkflowActionRequestImpl) then,
  ) = __$$WorkflowActionRequestImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String actionCode, String? reason, Map<String, Object?>? payload});
}

/// @nodoc
class __$$WorkflowActionRequestImplCopyWithImpl<$Res>
    extends
        _$WorkflowActionRequestCopyWithImpl<$Res, _$WorkflowActionRequestImpl>
    implements _$$WorkflowActionRequestImplCopyWith<$Res> {
  __$$WorkflowActionRequestImplCopyWithImpl(
    _$WorkflowActionRequestImpl _value,
    $Res Function(_$WorkflowActionRequestImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of WorkflowActionRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? actionCode = null,
    Object? reason = freezed,
    Object? payload = freezed,
  }) {
    return _then(
      _$WorkflowActionRequestImpl(
        actionCode: null == actionCode
            ? _value.actionCode
            : actionCode // ignore: cast_nullable_to_non_nullable
                  as String,
        reason: freezed == reason
            ? _value.reason
            : reason // ignore: cast_nullable_to_non_nullable
                  as String?,
        payload: freezed == payload
            ? _value._payload
            : payload // ignore: cast_nullable_to_non_nullable
                  as Map<String, Object?>?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$WorkflowActionRequestImpl implements _WorkflowActionRequest {
  const _$WorkflowActionRequestImpl({
    required this.actionCode,
    this.reason,
    final Map<String, Object?>? payload,
  }) : _payload = payload;

  factory _$WorkflowActionRequestImpl.fromJson(Map<String, dynamic> json) =>
      _$$WorkflowActionRequestImplFromJson(json);

  @override
  final String actionCode;
  @override
  final String? reason;

  /// Payload for the transition's `formKey`, when it has one.
  final Map<String, Object?>? _payload;

  /// Payload for the transition's `formKey`, when it has one.
  @override
  Map<String, Object?>? get payload {
    final value = _payload;
    if (value == null) return null;
    if (_payload is EqualUnmodifiableMapView) return _payload;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableMapView(value);
  }

  @override
  String toString() {
    return 'WorkflowActionRequest(actionCode: $actionCode, reason: $reason, payload: $payload)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$WorkflowActionRequestImpl &&
            (identical(other.actionCode, actionCode) ||
                other.actionCode == actionCode) &&
            (identical(other.reason, reason) || other.reason == reason) &&
            const DeepCollectionEquality().equals(other._payload, _payload));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    actionCode,
    reason,
    const DeepCollectionEquality().hash(_payload),
  );

  /// Create a copy of WorkflowActionRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$WorkflowActionRequestImplCopyWith<_$WorkflowActionRequestImpl>
  get copyWith =>
      __$$WorkflowActionRequestImplCopyWithImpl<_$WorkflowActionRequestImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$WorkflowActionRequestImplToJson(this);
  }
}

abstract class _WorkflowActionRequest implements WorkflowActionRequest {
  const factory _WorkflowActionRequest({
    required final String actionCode,
    final String? reason,
    final Map<String, Object?>? payload,
  }) = _$WorkflowActionRequestImpl;

  factory _WorkflowActionRequest.fromJson(Map<String, dynamic> json) =
      _$WorkflowActionRequestImpl.fromJson;

  @override
  String get actionCode;
  @override
  String? get reason;

  /// Payload for the transition's `formKey`, when it has one.
  @override
  Map<String, Object?>? get payload;

  /// Create a copy of WorkflowActionRequest
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$WorkflowActionRequestImplCopyWith<_$WorkflowActionRequestImpl>
  get copyWith => throw _privateConstructorUsedError;
}
