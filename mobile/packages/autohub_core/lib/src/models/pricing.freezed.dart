// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'pricing.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

PricingVehicleContext _$PricingVehicleContextFromJson(
  Map<String, dynamic> json,
) {
  return _PricingVehicleContext.fromJson(json);
}

/// @nodoc
mixin _$PricingVehicleContext {
  String? get category => throw _privateConstructorUsedError;
  String? get makeCode => throw _privateConstructorUsedError;
  String? get modelCode => throw _privateConstructorUsedError;
  int? get year => throw _privateConstructorUsedError;

  /// Serializes this PricingVehicleContext to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PricingVehicleContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PricingVehicleContextCopyWith<PricingVehicleContext> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PricingVehicleContextCopyWith<$Res> {
  factory $PricingVehicleContextCopyWith(
    PricingVehicleContext value,
    $Res Function(PricingVehicleContext) then,
  ) = _$PricingVehicleContextCopyWithImpl<$Res, PricingVehicleContext>;
  @useResult
  $Res call({String? category, String? makeCode, String? modelCode, int? year});
}

/// @nodoc
class _$PricingVehicleContextCopyWithImpl<
  $Res,
  $Val extends PricingVehicleContext
>
    implements $PricingVehicleContextCopyWith<$Res> {
  _$PricingVehicleContextCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PricingVehicleContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? category = freezed,
    Object? makeCode = freezed,
    Object? modelCode = freezed,
    Object? year = freezed,
  }) {
    return _then(
      _value.copyWith(
            category: freezed == category
                ? _value.category
                : category // ignore: cast_nullable_to_non_nullable
                      as String?,
            makeCode: freezed == makeCode
                ? _value.makeCode
                : makeCode // ignore: cast_nullable_to_non_nullable
                      as String?,
            modelCode: freezed == modelCode
                ? _value.modelCode
                : modelCode // ignore: cast_nullable_to_non_nullable
                      as String?,
            year: freezed == year
                ? _value.year
                : year // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$PricingVehicleContextImplCopyWith<$Res>
    implements $PricingVehicleContextCopyWith<$Res> {
  factory _$$PricingVehicleContextImplCopyWith(
    _$PricingVehicleContextImpl value,
    $Res Function(_$PricingVehicleContextImpl) then,
  ) = __$$PricingVehicleContextImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String? category, String? makeCode, String? modelCode, int? year});
}

/// @nodoc
class __$$PricingVehicleContextImplCopyWithImpl<$Res>
    extends
        _$PricingVehicleContextCopyWithImpl<$Res, _$PricingVehicleContextImpl>
    implements _$$PricingVehicleContextImplCopyWith<$Res> {
  __$$PricingVehicleContextImplCopyWithImpl(
    _$PricingVehicleContextImpl _value,
    $Res Function(_$PricingVehicleContextImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PricingVehicleContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? category = freezed,
    Object? makeCode = freezed,
    Object? modelCode = freezed,
    Object? year = freezed,
  }) {
    return _then(
      _$PricingVehicleContextImpl(
        category: freezed == category
            ? _value.category
            : category // ignore: cast_nullable_to_non_nullable
                  as String?,
        makeCode: freezed == makeCode
            ? _value.makeCode
            : makeCode // ignore: cast_nullable_to_non_nullable
                  as String?,
        modelCode: freezed == modelCode
            ? _value.modelCode
            : modelCode // ignore: cast_nullable_to_non_nullable
                  as String?,
        year: freezed == year
            ? _value.year
            : year // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PricingVehicleContextImpl implements _PricingVehicleContext {
  const _$PricingVehicleContextImpl({
    this.category,
    this.makeCode,
    this.modelCode,
    this.year,
  });

  factory _$PricingVehicleContextImpl.fromJson(Map<String, dynamic> json) =>
      _$$PricingVehicleContextImplFromJson(json);

  @override
  final String? category;
  @override
  final String? makeCode;
  @override
  final String? modelCode;
  @override
  final int? year;

  @override
  String toString() {
    return 'PricingVehicleContext(category: $category, makeCode: $makeCode, modelCode: $modelCode, year: $year)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PricingVehicleContextImpl &&
            (identical(other.category, category) ||
                other.category == category) &&
            (identical(other.makeCode, makeCode) ||
                other.makeCode == makeCode) &&
            (identical(other.modelCode, modelCode) ||
                other.modelCode == modelCode) &&
            (identical(other.year, year) || other.year == year));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, category, makeCode, modelCode, year);

  /// Create a copy of PricingVehicleContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PricingVehicleContextImplCopyWith<_$PricingVehicleContextImpl>
  get copyWith =>
      __$$PricingVehicleContextImplCopyWithImpl<_$PricingVehicleContextImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$PricingVehicleContextImplToJson(this);
  }
}

abstract class _PricingVehicleContext implements PricingVehicleContext {
  const factory _PricingVehicleContext({
    final String? category,
    final String? makeCode,
    final String? modelCode,
    final int? year,
  }) = _$PricingVehicleContextImpl;

  factory _PricingVehicleContext.fromJson(Map<String, dynamic> json) =
      _$PricingVehicleContextImpl.fromJson;

  @override
  String? get category;
  @override
  String? get makeCode;
  @override
  String? get modelCode;
  @override
  int? get year;

  /// Create a copy of PricingVehicleContext
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PricingVehicleContextImplCopyWith<_$PricingVehicleContextImpl>
  get copyWith => throw _privateConstructorUsedError;
}

PricingLocationContext _$PricingLocationContextFromJson(
  Map<String, dynamic> json,
) {
  return _PricingLocationContext.fromJson(json);
}

/// @nodoc
mixin _$PricingLocationContext {
  String? get state => throw _privateConstructorUsedError;
  String? get city => throw _privateConstructorUsedError;

  /// Serializes this PricingLocationContext to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PricingLocationContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PricingLocationContextCopyWith<PricingLocationContext> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PricingLocationContextCopyWith<$Res> {
  factory $PricingLocationContextCopyWith(
    PricingLocationContext value,
    $Res Function(PricingLocationContext) then,
  ) = _$PricingLocationContextCopyWithImpl<$Res, PricingLocationContext>;
  @useResult
  $Res call({String? state, String? city});
}

/// @nodoc
class _$PricingLocationContextCopyWithImpl<
  $Res,
  $Val extends PricingLocationContext
>
    implements $PricingLocationContextCopyWith<$Res> {
  _$PricingLocationContextCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PricingLocationContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? state = freezed, Object? city = freezed}) {
    return _then(
      _value.copyWith(
            state: freezed == state
                ? _value.state
                : state // ignore: cast_nullable_to_non_nullable
                      as String?,
            city: freezed == city
                ? _value.city
                : city // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$PricingLocationContextImplCopyWith<$Res>
    implements $PricingLocationContextCopyWith<$Res> {
  factory _$$PricingLocationContextImplCopyWith(
    _$PricingLocationContextImpl value,
    $Res Function(_$PricingLocationContextImpl) then,
  ) = __$$PricingLocationContextImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String? state, String? city});
}

/// @nodoc
class __$$PricingLocationContextImplCopyWithImpl<$Res>
    extends
        _$PricingLocationContextCopyWithImpl<$Res, _$PricingLocationContextImpl>
    implements _$$PricingLocationContextImplCopyWith<$Res> {
  __$$PricingLocationContextImplCopyWithImpl(
    _$PricingLocationContextImpl _value,
    $Res Function(_$PricingLocationContextImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PricingLocationContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? state = freezed, Object? city = freezed}) {
    return _then(
      _$PricingLocationContextImpl(
        state: freezed == state
            ? _value.state
            : state // ignore: cast_nullable_to_non_nullable
                  as String?,
        city: freezed == city
            ? _value.city
            : city // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PricingLocationContextImpl implements _PricingLocationContext {
  const _$PricingLocationContextImpl({this.state, this.city});

  factory _$PricingLocationContextImpl.fromJson(Map<String, dynamic> json) =>
      _$$PricingLocationContextImplFromJson(json);

  @override
  final String? state;
  @override
  final String? city;

  @override
  String toString() {
    return 'PricingLocationContext(state: $state, city: $city)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PricingLocationContextImpl &&
            (identical(other.state, state) || other.state == state) &&
            (identical(other.city, city) || other.city == city));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, state, city);

  /// Create a copy of PricingLocationContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PricingLocationContextImplCopyWith<_$PricingLocationContextImpl>
  get copyWith =>
      __$$PricingLocationContextImplCopyWithImpl<_$PricingLocationContextImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$PricingLocationContextImplToJson(this);
  }
}

abstract class _PricingLocationContext implements PricingLocationContext {
  const factory _PricingLocationContext({
    final String? state,
    final String? city,
  }) = _$PricingLocationContextImpl;

  factory _PricingLocationContext.fromJson(Map<String, dynamic> json) =
      _$PricingLocationContextImpl.fromJson;

  @override
  String? get state;
  @override
  String? get city;

  /// Create a copy of PricingLocationContext
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PricingLocationContextImplCopyWith<_$PricingLocationContextImpl>
  get copyWith => throw _privateConstructorUsedError;
}

PricingPartyContext _$PricingPartyContextFromJson(Map<String, dynamic> json) {
  return _PricingPartyContext.fromJson(json);
}

/// @nodoc
mixin _$PricingPartyContext {
  String? get type => throw _privateConstructorUsedError;
  String? get tier => throw _privateConstructorUsedError;
  String? get id => throw _privateConstructorUsedError;

  /// Serializes this PricingPartyContext to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PricingPartyContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PricingPartyContextCopyWith<PricingPartyContext> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PricingPartyContextCopyWith<$Res> {
  factory $PricingPartyContextCopyWith(
    PricingPartyContext value,
    $Res Function(PricingPartyContext) then,
  ) = _$PricingPartyContextCopyWithImpl<$Res, PricingPartyContext>;
  @useResult
  $Res call({String? type, String? tier, String? id});
}

/// @nodoc
class _$PricingPartyContextCopyWithImpl<$Res, $Val extends PricingPartyContext>
    implements $PricingPartyContextCopyWith<$Res> {
  _$PricingPartyContextCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PricingPartyContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? type = freezed,
    Object? tier = freezed,
    Object? id = freezed,
  }) {
    return _then(
      _value.copyWith(
            type: freezed == type
                ? _value.type
                : type // ignore: cast_nullable_to_non_nullable
                      as String?,
            tier: freezed == tier
                ? _value.tier
                : tier // ignore: cast_nullable_to_non_nullable
                      as String?,
            id: freezed == id
                ? _value.id
                : id // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$PricingPartyContextImplCopyWith<$Res>
    implements $PricingPartyContextCopyWith<$Res> {
  factory _$$PricingPartyContextImplCopyWith(
    _$PricingPartyContextImpl value,
    $Res Function(_$PricingPartyContextImpl) then,
  ) = __$$PricingPartyContextImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String? type, String? tier, String? id});
}

/// @nodoc
class __$$PricingPartyContextImplCopyWithImpl<$Res>
    extends _$PricingPartyContextCopyWithImpl<$Res, _$PricingPartyContextImpl>
    implements _$$PricingPartyContextImplCopyWith<$Res> {
  __$$PricingPartyContextImplCopyWithImpl(
    _$PricingPartyContextImpl _value,
    $Res Function(_$PricingPartyContextImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PricingPartyContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? type = freezed,
    Object? tier = freezed,
    Object? id = freezed,
  }) {
    return _then(
      _$PricingPartyContextImpl(
        type: freezed == type
            ? _value.type
            : type // ignore: cast_nullable_to_non_nullable
                  as String?,
        tier: freezed == tier
            ? _value.tier
            : tier // ignore: cast_nullable_to_non_nullable
                  as String?,
        id: freezed == id
            ? _value.id
            : id // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PricingPartyContextImpl implements _PricingPartyContext {
  const _$PricingPartyContextImpl({this.type, this.tier, this.id});

  factory _$PricingPartyContextImpl.fromJson(Map<String, dynamic> json) =>
      _$$PricingPartyContextImplFromJson(json);

  @override
  final String? type;
  @override
  final String? tier;
  @override
  final String? id;

  @override
  String toString() {
    return 'PricingPartyContext(type: $type, tier: $tier, id: $id)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PricingPartyContextImpl &&
            (identical(other.type, type) || other.type == type) &&
            (identical(other.tier, tier) || other.tier == tier) &&
            (identical(other.id, id) || other.id == id));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, type, tier, id);

  /// Create a copy of PricingPartyContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PricingPartyContextImplCopyWith<_$PricingPartyContextImpl> get copyWith =>
      __$$PricingPartyContextImplCopyWithImpl<_$PricingPartyContextImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$PricingPartyContextImplToJson(this);
  }
}

abstract class _PricingPartyContext implements PricingPartyContext {
  const factory _PricingPartyContext({
    final String? type,
    final String? tier,
    final String? id,
  }) = _$PricingPartyContextImpl;

  factory _PricingPartyContext.fromJson(Map<String, dynamic> json) =
      _$PricingPartyContextImpl.fromJson;

  @override
  String? get type;
  @override
  String? get tier;
  @override
  String? get id;

  /// Create a copy of PricingPartyContext
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PricingPartyContextImplCopyWith<_$PricingPartyContextImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

PricingAmountContext _$PricingAmountContextFromJson(Map<String, dynamic> json) {
  return _PricingAmountContext.fromJson(json);
}

/// @nodoc
mixin _$PricingAmountContext {
  int get minorUnits => throw _privateConstructorUsedError;
  String get currency => throw _privateConstructorUsedError;

  /// Serializes this PricingAmountContext to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PricingAmountContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PricingAmountContextCopyWith<PricingAmountContext> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PricingAmountContextCopyWith<$Res> {
  factory $PricingAmountContextCopyWith(
    PricingAmountContext value,
    $Res Function(PricingAmountContext) then,
  ) = _$PricingAmountContextCopyWithImpl<$Res, PricingAmountContext>;
  @useResult
  $Res call({int minorUnits, String currency});
}

/// @nodoc
class _$PricingAmountContextCopyWithImpl<
  $Res,
  $Val extends PricingAmountContext
>
    implements $PricingAmountContextCopyWith<$Res> {
  _$PricingAmountContextCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PricingAmountContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? minorUnits = null, Object? currency = null}) {
    return _then(
      _value.copyWith(
            minorUnits: null == minorUnits
                ? _value.minorUnits
                : minorUnits // ignore: cast_nullable_to_non_nullable
                      as int,
            currency: null == currency
                ? _value.currency
                : currency // ignore: cast_nullable_to_non_nullable
                      as String,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$PricingAmountContextImplCopyWith<$Res>
    implements $PricingAmountContextCopyWith<$Res> {
  factory _$$PricingAmountContextImplCopyWith(
    _$PricingAmountContextImpl value,
    $Res Function(_$PricingAmountContextImpl) then,
  ) = __$$PricingAmountContextImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int minorUnits, String currency});
}

/// @nodoc
class __$$PricingAmountContextImplCopyWithImpl<$Res>
    extends _$PricingAmountContextCopyWithImpl<$Res, _$PricingAmountContextImpl>
    implements _$$PricingAmountContextImplCopyWith<$Res> {
  __$$PricingAmountContextImplCopyWithImpl(
    _$PricingAmountContextImpl _value,
    $Res Function(_$PricingAmountContextImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PricingAmountContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? minorUnits = null, Object? currency = null}) {
    return _then(
      _$PricingAmountContextImpl(
        minorUnits: null == minorUnits
            ? _value.minorUnits
            : minorUnits // ignore: cast_nullable_to_non_nullable
                  as int,
        currency: null == currency
            ? _value.currency
            : currency // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PricingAmountContextImpl implements _PricingAmountContext {
  const _$PricingAmountContextImpl({
    required this.minorUnits,
    this.currency = 'INR',
  });

  factory _$PricingAmountContextImpl.fromJson(Map<String, dynamic> json) =>
      _$$PricingAmountContextImplFromJson(json);

  @override
  final int minorUnits;
  @override
  @JsonKey()
  final String currency;

  @override
  String toString() {
    return 'PricingAmountContext(minorUnits: $minorUnits, currency: $currency)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PricingAmountContextImpl &&
            (identical(other.minorUnits, minorUnits) ||
                other.minorUnits == minorUnits) &&
            (identical(other.currency, currency) ||
                other.currency == currency));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, minorUnits, currency);

  /// Create a copy of PricingAmountContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PricingAmountContextImplCopyWith<_$PricingAmountContextImpl>
  get copyWith =>
      __$$PricingAmountContextImplCopyWithImpl<_$PricingAmountContextImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$PricingAmountContextImplToJson(this);
  }
}

abstract class _PricingAmountContext implements PricingAmountContext {
  const factory _PricingAmountContext({
    required final int minorUnits,
    final String currency,
  }) = _$PricingAmountContextImpl;

  factory _PricingAmountContext.fromJson(Map<String, dynamic> json) =
      _$PricingAmountContextImpl.fromJson;

  @override
  int get minorUnits;
  @override
  String get currency;

  /// Create a copy of PricingAmountContext
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PricingAmountContextImplCopyWith<_$PricingAmountContextImpl>
  get copyWith => throw _privateConstructorUsedError;
}

PricingContext _$PricingContextFromJson(Map<String, dynamic> json) {
  return _PricingContext.fromJson(json);
}

/// @nodoc
mixin _$PricingContext {
  PricingVehicleContext? get vehicle => throw _privateConstructorUsedError;
  PricingLocationContext? get location => throw _privateConstructorUsedError;
  PricingPartyContext? get party => throw _privateConstructorUsedError;
  PricingAmountContext? get amount => throw _privateConstructorUsedError;

  /// Serializes this PricingContext to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PricingContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PricingContextCopyWith<PricingContext> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PricingContextCopyWith<$Res> {
  factory $PricingContextCopyWith(
    PricingContext value,
    $Res Function(PricingContext) then,
  ) = _$PricingContextCopyWithImpl<$Res, PricingContext>;
  @useResult
  $Res call({
    PricingVehicleContext? vehicle,
    PricingLocationContext? location,
    PricingPartyContext? party,
    PricingAmountContext? amount,
  });

  $PricingVehicleContextCopyWith<$Res>? get vehicle;
  $PricingLocationContextCopyWith<$Res>? get location;
  $PricingPartyContextCopyWith<$Res>? get party;
  $PricingAmountContextCopyWith<$Res>? get amount;
}

/// @nodoc
class _$PricingContextCopyWithImpl<$Res, $Val extends PricingContext>
    implements $PricingContextCopyWith<$Res> {
  _$PricingContextCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PricingContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? vehicle = freezed,
    Object? location = freezed,
    Object? party = freezed,
    Object? amount = freezed,
  }) {
    return _then(
      _value.copyWith(
            vehicle: freezed == vehicle
                ? _value.vehicle
                : vehicle // ignore: cast_nullable_to_non_nullable
                      as PricingVehicleContext?,
            location: freezed == location
                ? _value.location
                : location // ignore: cast_nullable_to_non_nullable
                      as PricingLocationContext?,
            party: freezed == party
                ? _value.party
                : party // ignore: cast_nullable_to_non_nullable
                      as PricingPartyContext?,
            amount: freezed == amount
                ? _value.amount
                : amount // ignore: cast_nullable_to_non_nullable
                      as PricingAmountContext?,
          )
          as $Val,
    );
  }

  /// Create a copy of PricingContext
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $PricingVehicleContextCopyWith<$Res>? get vehicle {
    if (_value.vehicle == null) {
      return null;
    }

    return $PricingVehicleContextCopyWith<$Res>(_value.vehicle!, (value) {
      return _then(_value.copyWith(vehicle: value) as $Val);
    });
  }

  /// Create a copy of PricingContext
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $PricingLocationContextCopyWith<$Res>? get location {
    if (_value.location == null) {
      return null;
    }

    return $PricingLocationContextCopyWith<$Res>(_value.location!, (value) {
      return _then(_value.copyWith(location: value) as $Val);
    });
  }

  /// Create a copy of PricingContext
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $PricingPartyContextCopyWith<$Res>? get party {
    if (_value.party == null) {
      return null;
    }

    return $PricingPartyContextCopyWith<$Res>(_value.party!, (value) {
      return _then(_value.copyWith(party: value) as $Val);
    });
  }

  /// Create a copy of PricingContext
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $PricingAmountContextCopyWith<$Res>? get amount {
    if (_value.amount == null) {
      return null;
    }

    return $PricingAmountContextCopyWith<$Res>(_value.amount!, (value) {
      return _then(_value.copyWith(amount: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$PricingContextImplCopyWith<$Res>
    implements $PricingContextCopyWith<$Res> {
  factory _$$PricingContextImplCopyWith(
    _$PricingContextImpl value,
    $Res Function(_$PricingContextImpl) then,
  ) = __$$PricingContextImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    PricingVehicleContext? vehicle,
    PricingLocationContext? location,
    PricingPartyContext? party,
    PricingAmountContext? amount,
  });

  @override
  $PricingVehicleContextCopyWith<$Res>? get vehicle;
  @override
  $PricingLocationContextCopyWith<$Res>? get location;
  @override
  $PricingPartyContextCopyWith<$Res>? get party;
  @override
  $PricingAmountContextCopyWith<$Res>? get amount;
}

/// @nodoc
class __$$PricingContextImplCopyWithImpl<$Res>
    extends _$PricingContextCopyWithImpl<$Res, _$PricingContextImpl>
    implements _$$PricingContextImplCopyWith<$Res> {
  __$$PricingContextImplCopyWithImpl(
    _$PricingContextImpl _value,
    $Res Function(_$PricingContextImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PricingContext
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? vehicle = freezed,
    Object? location = freezed,
    Object? party = freezed,
    Object? amount = freezed,
  }) {
    return _then(
      _$PricingContextImpl(
        vehicle: freezed == vehicle
            ? _value.vehicle
            : vehicle // ignore: cast_nullable_to_non_nullable
                  as PricingVehicleContext?,
        location: freezed == location
            ? _value.location
            : location // ignore: cast_nullable_to_non_nullable
                  as PricingLocationContext?,
        party: freezed == party
            ? _value.party
            : party // ignore: cast_nullable_to_non_nullable
                  as PricingPartyContext?,
        amount: freezed == amount
            ? _value.amount
            : amount // ignore: cast_nullable_to_non_nullable
                  as PricingAmountContext?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PricingContextImpl implements _PricingContext {
  const _$PricingContextImpl({
    this.vehicle,
    this.location,
    this.party,
    this.amount,
  });

  factory _$PricingContextImpl.fromJson(Map<String, dynamic> json) =>
      _$$PricingContextImplFromJson(json);

  @override
  final PricingVehicleContext? vehicle;
  @override
  final PricingLocationContext? location;
  @override
  final PricingPartyContext? party;
  @override
  final PricingAmountContext? amount;

  @override
  String toString() {
    return 'PricingContext(vehicle: $vehicle, location: $location, party: $party, amount: $amount)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PricingContextImpl &&
            (identical(other.vehicle, vehicle) || other.vehicle == vehicle) &&
            (identical(other.location, location) ||
                other.location == location) &&
            (identical(other.party, party) || other.party == party) &&
            (identical(other.amount, amount) || other.amount == amount));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, vehicle, location, party, amount);

  /// Create a copy of PricingContext
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PricingContextImplCopyWith<_$PricingContextImpl> get copyWith =>
      __$$PricingContextImplCopyWithImpl<_$PricingContextImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$PricingContextImplToJson(this);
  }
}

abstract class _PricingContext implements PricingContext {
  const factory _PricingContext({
    final PricingVehicleContext? vehicle,
    final PricingLocationContext? location,
    final PricingPartyContext? party,
    final PricingAmountContext? amount,
  }) = _$PricingContextImpl;

  factory _PricingContext.fromJson(Map<String, dynamic> json) =
      _$PricingContextImpl.fromJson;

  @override
  PricingVehicleContext? get vehicle;
  @override
  PricingLocationContext? get location;
  @override
  PricingPartyContext? get party;
  @override
  PricingAmountContext? get amount;

  /// Create a copy of PricingContext
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PricingContextImplCopyWith<_$PricingContextImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

PricingQuoteRequest _$PricingQuoteRequestFromJson(Map<String, dynamic> json) {
  return _PricingQuoteRequest.fromJson(json);
}

/// @nodoc
mixin _$PricingQuoteRequest {
  /// The rule set to evaluate: `auction.win`, `listing.publish`, `rental.booking`.
  String get scope => throw _privateConstructorUsedError;
  PricingContext get context => throw _privateConstructorUsedError;

  /// Serializes this PricingQuoteRequest to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PricingQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PricingQuoteRequestCopyWith<PricingQuoteRequest> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PricingQuoteRequestCopyWith<$Res> {
  factory $PricingQuoteRequestCopyWith(
    PricingQuoteRequest value,
    $Res Function(PricingQuoteRequest) then,
  ) = _$PricingQuoteRequestCopyWithImpl<$Res, PricingQuoteRequest>;
  @useResult
  $Res call({String scope, PricingContext context});

  $PricingContextCopyWith<$Res> get context;
}

/// @nodoc
class _$PricingQuoteRequestCopyWithImpl<$Res, $Val extends PricingQuoteRequest>
    implements $PricingQuoteRequestCopyWith<$Res> {
  _$PricingQuoteRequestCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PricingQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? scope = null, Object? context = null}) {
    return _then(
      _value.copyWith(
            scope: null == scope
                ? _value.scope
                : scope // ignore: cast_nullable_to_non_nullable
                      as String,
            context: null == context
                ? _value.context
                : context // ignore: cast_nullable_to_non_nullable
                      as PricingContext,
          )
          as $Val,
    );
  }

  /// Create a copy of PricingQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $PricingContextCopyWith<$Res> get context {
    return $PricingContextCopyWith<$Res>(_value.context, (value) {
      return _then(_value.copyWith(context: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$PricingQuoteRequestImplCopyWith<$Res>
    implements $PricingQuoteRequestCopyWith<$Res> {
  factory _$$PricingQuoteRequestImplCopyWith(
    _$PricingQuoteRequestImpl value,
    $Res Function(_$PricingQuoteRequestImpl) then,
  ) = __$$PricingQuoteRequestImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String scope, PricingContext context});

  @override
  $PricingContextCopyWith<$Res> get context;
}

/// @nodoc
class __$$PricingQuoteRequestImplCopyWithImpl<$Res>
    extends _$PricingQuoteRequestCopyWithImpl<$Res, _$PricingQuoteRequestImpl>
    implements _$$PricingQuoteRequestImplCopyWith<$Res> {
  __$$PricingQuoteRequestImplCopyWithImpl(
    _$PricingQuoteRequestImpl _value,
    $Res Function(_$PricingQuoteRequestImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PricingQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? scope = null, Object? context = null}) {
    return _then(
      _$PricingQuoteRequestImpl(
        scope: null == scope
            ? _value.scope
            : scope // ignore: cast_nullable_to_non_nullable
                  as String,
        context: null == context
            ? _value.context
            : context // ignore: cast_nullable_to_non_nullable
                  as PricingContext,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PricingQuoteRequestImpl implements _PricingQuoteRequest {
  const _$PricingQuoteRequestImpl({required this.scope, required this.context});

  factory _$PricingQuoteRequestImpl.fromJson(Map<String, dynamic> json) =>
      _$$PricingQuoteRequestImplFromJson(json);

  /// The rule set to evaluate: `auction.win`, `listing.publish`, `rental.booking`.
  @override
  final String scope;
  @override
  final PricingContext context;

  @override
  String toString() {
    return 'PricingQuoteRequest(scope: $scope, context: $context)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PricingQuoteRequestImpl &&
            (identical(other.scope, scope) || other.scope == scope) &&
            (identical(other.context, context) || other.context == context));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, scope, context);

  /// Create a copy of PricingQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PricingQuoteRequestImplCopyWith<_$PricingQuoteRequestImpl> get copyWith =>
      __$$PricingQuoteRequestImplCopyWithImpl<_$PricingQuoteRequestImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$PricingQuoteRequestImplToJson(this);
  }
}

abstract class _PricingQuoteRequest implements PricingQuoteRequest {
  const factory _PricingQuoteRequest({
    required final String scope,
    required final PricingContext context,
  }) = _$PricingQuoteRequestImpl;

  factory _PricingQuoteRequest.fromJson(Map<String, dynamic> json) =
      _$PricingQuoteRequestImpl.fromJson;

  /// The rule set to evaluate: `auction.win`, `listing.publish`, `rental.booking`.
  @override
  String get scope;
  @override
  PricingContext get context;

  /// Create a copy of PricingQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PricingQuoteRequestImplCopyWith<_$PricingQuoteRequestImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

PricingLine _$PricingLineFromJson(Map<String, dynamic> json) {
  return _PricingLine.fromJson(json);
}

/// @nodoc
mixin _$PricingLine {
  String get code => throw _privateConstructorUsedError;
  String get label => throw _privateConstructorUsedError;
  @JsonKey(unknownEnumValue: PricingBasis.flat)
  PricingBasis get basis => throw _privateConstructorUsedError;

  /// A fraction, not a percentage: `0.02` is 2%. Convert with `Rate.fromWire` before applying it
  /// to money — the double is for display only.
  double? get rate => throw _privateConstructorUsedError;
  int get amountMinor => throw _privateConstructorUsedError;

  /// Line codes this line was computed over. Present when [basis] is `percent_of_lines`.
  List<String>? get appliesTo => throw _privateConstructorUsedError;
  String? get ruleId => throw _privateConstructorUsedError;
  String? get ruleName => throw _privateConstructorUsedError;
  String? get hsnSac => throw _privateConstructorUsedError;

  /// Serializes this PricingLine to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PricingLine
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PricingLineCopyWith<PricingLine> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PricingLineCopyWith<$Res> {
  factory $PricingLineCopyWith(
    PricingLine value,
    $Res Function(PricingLine) then,
  ) = _$PricingLineCopyWithImpl<$Res, PricingLine>;
  @useResult
  $Res call({
    String code,
    String label,
    @JsonKey(unknownEnumValue: PricingBasis.flat) PricingBasis basis,
    double? rate,
    int amountMinor,
    List<String>? appliesTo,
    String? ruleId,
    String? ruleName,
    String? hsnSac,
  });
}

/// @nodoc
class _$PricingLineCopyWithImpl<$Res, $Val extends PricingLine>
    implements $PricingLineCopyWith<$Res> {
  _$PricingLineCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PricingLine
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? basis = null,
    Object? rate = freezed,
    Object? amountMinor = null,
    Object? appliesTo = freezed,
    Object? ruleId = freezed,
    Object? ruleName = freezed,
    Object? hsnSac = freezed,
  }) {
    return _then(
      _value.copyWith(
            code: null == code
                ? _value.code
                : code // ignore: cast_nullable_to_non_nullable
                      as String,
            label: null == label
                ? _value.label
                : label // ignore: cast_nullable_to_non_nullable
                      as String,
            basis: null == basis
                ? _value.basis
                : basis // ignore: cast_nullable_to_non_nullable
                      as PricingBasis,
            rate: freezed == rate
                ? _value.rate
                : rate // ignore: cast_nullable_to_non_nullable
                      as double?,
            amountMinor: null == amountMinor
                ? _value.amountMinor
                : amountMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            appliesTo: freezed == appliesTo
                ? _value.appliesTo
                : appliesTo // ignore: cast_nullable_to_non_nullable
                      as List<String>?,
            ruleId: freezed == ruleId
                ? _value.ruleId
                : ruleId // ignore: cast_nullable_to_non_nullable
                      as String?,
            ruleName: freezed == ruleName
                ? _value.ruleName
                : ruleName // ignore: cast_nullable_to_non_nullable
                      as String?,
            hsnSac: freezed == hsnSac
                ? _value.hsnSac
                : hsnSac // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$PricingLineImplCopyWith<$Res>
    implements $PricingLineCopyWith<$Res> {
  factory _$$PricingLineImplCopyWith(
    _$PricingLineImpl value,
    $Res Function(_$PricingLineImpl) then,
  ) = __$$PricingLineImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String code,
    String label,
    @JsonKey(unknownEnumValue: PricingBasis.flat) PricingBasis basis,
    double? rate,
    int amountMinor,
    List<String>? appliesTo,
    String? ruleId,
    String? ruleName,
    String? hsnSac,
  });
}

/// @nodoc
class __$$PricingLineImplCopyWithImpl<$Res>
    extends _$PricingLineCopyWithImpl<$Res, _$PricingLineImpl>
    implements _$$PricingLineImplCopyWith<$Res> {
  __$$PricingLineImplCopyWithImpl(
    _$PricingLineImpl _value,
    $Res Function(_$PricingLineImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PricingLine
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? basis = null,
    Object? rate = freezed,
    Object? amountMinor = null,
    Object? appliesTo = freezed,
    Object? ruleId = freezed,
    Object? ruleName = freezed,
    Object? hsnSac = freezed,
  }) {
    return _then(
      _$PricingLineImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        basis: null == basis
            ? _value.basis
            : basis // ignore: cast_nullable_to_non_nullable
                  as PricingBasis,
        rate: freezed == rate
            ? _value.rate
            : rate // ignore: cast_nullable_to_non_nullable
                  as double?,
        amountMinor: null == amountMinor
            ? _value.amountMinor
            : amountMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        appliesTo: freezed == appliesTo
            ? _value._appliesTo
            : appliesTo // ignore: cast_nullable_to_non_nullable
                  as List<String>?,
        ruleId: freezed == ruleId
            ? _value.ruleId
            : ruleId // ignore: cast_nullable_to_non_nullable
                  as String?,
        ruleName: freezed == ruleName
            ? _value.ruleName
            : ruleName // ignore: cast_nullable_to_non_nullable
                  as String?,
        hsnSac: freezed == hsnSac
            ? _value.hsnSac
            : hsnSac // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PricingLineImpl implements _PricingLine {
  const _$PricingLineImpl({
    required this.code,
    required this.label,
    @JsonKey(unknownEnumValue: PricingBasis.flat)
    this.basis = PricingBasis.flat,
    this.rate,
    required this.amountMinor,
    final List<String>? appliesTo,
    this.ruleId,
    this.ruleName,
    this.hsnSac,
  }) : _appliesTo = appliesTo;

  factory _$PricingLineImpl.fromJson(Map<String, dynamic> json) =>
      _$$PricingLineImplFromJson(json);

  @override
  final String code;
  @override
  final String label;
  @override
  @JsonKey(unknownEnumValue: PricingBasis.flat)
  final PricingBasis basis;

  /// A fraction, not a percentage: `0.02` is 2%. Convert with `Rate.fromWire` before applying it
  /// to money — the double is for display only.
  @override
  final double? rate;
  @override
  final int amountMinor;

  /// Line codes this line was computed over. Present when [basis] is `percent_of_lines`.
  final List<String>? _appliesTo;

  /// Line codes this line was computed over. Present when [basis] is `percent_of_lines`.
  @override
  List<String>? get appliesTo {
    final value = _appliesTo;
    if (value == null) return null;
    if (_appliesTo is EqualUnmodifiableListView) return _appliesTo;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(value);
  }

  @override
  final String? ruleId;
  @override
  final String? ruleName;
  @override
  final String? hsnSac;

  @override
  String toString() {
    return 'PricingLine(code: $code, label: $label, basis: $basis, rate: $rate, amountMinor: $amountMinor, appliesTo: $appliesTo, ruleId: $ruleId, ruleName: $ruleName, hsnSac: $hsnSac)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PricingLineImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.label, label) || other.label == label) &&
            (identical(other.basis, basis) || other.basis == basis) &&
            (identical(other.rate, rate) || other.rate == rate) &&
            (identical(other.amountMinor, amountMinor) ||
                other.amountMinor == amountMinor) &&
            const DeepCollectionEquality().equals(
              other._appliesTo,
              _appliesTo,
            ) &&
            (identical(other.ruleId, ruleId) || other.ruleId == ruleId) &&
            (identical(other.ruleName, ruleName) ||
                other.ruleName == ruleName) &&
            (identical(other.hsnSac, hsnSac) || other.hsnSac == hsnSac));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    label,
    basis,
    rate,
    amountMinor,
    const DeepCollectionEquality().hash(_appliesTo),
    ruleId,
    ruleName,
    hsnSac,
  );

  /// Create a copy of PricingLine
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PricingLineImplCopyWith<_$PricingLineImpl> get copyWith =>
      __$$PricingLineImplCopyWithImpl<_$PricingLineImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$PricingLineImplToJson(this);
  }
}

abstract class _PricingLine implements PricingLine {
  const factory _PricingLine({
    required final String code,
    required final String label,
    @JsonKey(unknownEnumValue: PricingBasis.flat) final PricingBasis basis,
    final double? rate,
    required final int amountMinor,
    final List<String>? appliesTo,
    final String? ruleId,
    final String? ruleName,
    final String? hsnSac,
  }) = _$PricingLineImpl;

  factory _PricingLine.fromJson(Map<String, dynamic> json) =
      _$PricingLineImpl.fromJson;

  @override
  String get code;
  @override
  String get label;
  @override
  @JsonKey(unknownEnumValue: PricingBasis.flat)
  PricingBasis get basis;

  /// A fraction, not a percentage: `0.02` is 2%. Convert with `Rate.fromWire` before applying it
  /// to money — the double is for display only.
  @override
  double? get rate;
  @override
  int get amountMinor;

  /// Line codes this line was computed over. Present when [basis] is `percent_of_lines`.
  @override
  List<String>? get appliesTo;
  @override
  String? get ruleId;
  @override
  String? get ruleName;
  @override
  String? get hsnSac;

  /// Create a copy of PricingLine
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PricingLineImplCopyWith<_$PricingLineImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

PricingQuote _$PricingQuoteFromJson(Map<String, dynamic> json) {
  return _PricingQuote.fromJson(json);
}

/// @nodoc
mixin _$PricingQuote {
  String get currency => throw _privateConstructorUsedError;
  int get baseAmount => throw _privateConstructorUsedError;
  List<PricingLine> get lines => throw _privateConstructorUsedError;
  int get totalFeesMinor => throw _privateConstructorUsedError;
  int get payableMinor => throw _privateConstructorUsedError;

  /// One sentence a dealer can read: "Gold dealers in Maharashtra pay 2% on auction wins."
  String? get explanation => throw _privateConstructorUsedError;

  /// Serializes this PricingQuote to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PricingQuote
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PricingQuoteCopyWith<PricingQuote> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PricingQuoteCopyWith<$Res> {
  factory $PricingQuoteCopyWith(
    PricingQuote value,
    $Res Function(PricingQuote) then,
  ) = _$PricingQuoteCopyWithImpl<$Res, PricingQuote>;
  @useResult
  $Res call({
    String currency,
    int baseAmount,
    List<PricingLine> lines,
    int totalFeesMinor,
    int payableMinor,
    String? explanation,
  });
}

/// @nodoc
class _$PricingQuoteCopyWithImpl<$Res, $Val extends PricingQuote>
    implements $PricingQuoteCopyWith<$Res> {
  _$PricingQuoteCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PricingQuote
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? currency = null,
    Object? baseAmount = null,
    Object? lines = null,
    Object? totalFeesMinor = null,
    Object? payableMinor = null,
    Object? explanation = freezed,
  }) {
    return _then(
      _value.copyWith(
            currency: null == currency
                ? _value.currency
                : currency // ignore: cast_nullable_to_non_nullable
                      as String,
            baseAmount: null == baseAmount
                ? _value.baseAmount
                : baseAmount // ignore: cast_nullable_to_non_nullable
                      as int,
            lines: null == lines
                ? _value.lines
                : lines // ignore: cast_nullable_to_non_nullable
                      as List<PricingLine>,
            totalFeesMinor: null == totalFeesMinor
                ? _value.totalFeesMinor
                : totalFeesMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            payableMinor: null == payableMinor
                ? _value.payableMinor
                : payableMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            explanation: freezed == explanation
                ? _value.explanation
                : explanation // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$PricingQuoteImplCopyWith<$Res>
    implements $PricingQuoteCopyWith<$Res> {
  factory _$$PricingQuoteImplCopyWith(
    _$PricingQuoteImpl value,
    $Res Function(_$PricingQuoteImpl) then,
  ) = __$$PricingQuoteImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String currency,
    int baseAmount,
    List<PricingLine> lines,
    int totalFeesMinor,
    int payableMinor,
    String? explanation,
  });
}

/// @nodoc
class __$$PricingQuoteImplCopyWithImpl<$Res>
    extends _$PricingQuoteCopyWithImpl<$Res, _$PricingQuoteImpl>
    implements _$$PricingQuoteImplCopyWith<$Res> {
  __$$PricingQuoteImplCopyWithImpl(
    _$PricingQuoteImpl _value,
    $Res Function(_$PricingQuoteImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PricingQuote
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? currency = null,
    Object? baseAmount = null,
    Object? lines = null,
    Object? totalFeesMinor = null,
    Object? payableMinor = null,
    Object? explanation = freezed,
  }) {
    return _then(
      _$PricingQuoteImpl(
        currency: null == currency
            ? _value.currency
            : currency // ignore: cast_nullable_to_non_nullable
                  as String,
        baseAmount: null == baseAmount
            ? _value.baseAmount
            : baseAmount // ignore: cast_nullable_to_non_nullable
                  as int,
        lines: null == lines
            ? _value._lines
            : lines // ignore: cast_nullable_to_non_nullable
                  as List<PricingLine>,
        totalFeesMinor: null == totalFeesMinor
            ? _value.totalFeesMinor
            : totalFeesMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        payableMinor: null == payableMinor
            ? _value.payableMinor
            : payableMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        explanation: freezed == explanation
            ? _value.explanation
            : explanation // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PricingQuoteImpl implements _PricingQuote {
  const _$PricingQuoteImpl({
    this.currency = 'INR',
    required this.baseAmount,
    final List<PricingLine> lines = const <PricingLine>[],
    required this.totalFeesMinor,
    required this.payableMinor,
    this.explanation,
  }) : _lines = lines;

  factory _$PricingQuoteImpl.fromJson(Map<String, dynamic> json) =>
      _$$PricingQuoteImplFromJson(json);

  @override
  @JsonKey()
  final String currency;
  @override
  final int baseAmount;
  final List<PricingLine> _lines;
  @override
  @JsonKey()
  List<PricingLine> get lines {
    if (_lines is EqualUnmodifiableListView) return _lines;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_lines);
  }

  @override
  final int totalFeesMinor;
  @override
  final int payableMinor;

  /// One sentence a dealer can read: "Gold dealers in Maharashtra pay 2% on auction wins."
  @override
  final String? explanation;

  @override
  String toString() {
    return 'PricingQuote(currency: $currency, baseAmount: $baseAmount, lines: $lines, totalFeesMinor: $totalFeesMinor, payableMinor: $payableMinor, explanation: $explanation)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PricingQuoteImpl &&
            (identical(other.currency, currency) ||
                other.currency == currency) &&
            (identical(other.baseAmount, baseAmount) ||
                other.baseAmount == baseAmount) &&
            const DeepCollectionEquality().equals(other._lines, _lines) &&
            (identical(other.totalFeesMinor, totalFeesMinor) ||
                other.totalFeesMinor == totalFeesMinor) &&
            (identical(other.payableMinor, payableMinor) ||
                other.payableMinor == payableMinor) &&
            (identical(other.explanation, explanation) ||
                other.explanation == explanation));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    currency,
    baseAmount,
    const DeepCollectionEquality().hash(_lines),
    totalFeesMinor,
    payableMinor,
    explanation,
  );

  /// Create a copy of PricingQuote
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PricingQuoteImplCopyWith<_$PricingQuoteImpl> get copyWith =>
      __$$PricingQuoteImplCopyWithImpl<_$PricingQuoteImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$PricingQuoteImplToJson(this);
  }
}

abstract class _PricingQuote implements PricingQuote {
  const factory _PricingQuote({
    final String currency,
    required final int baseAmount,
    final List<PricingLine> lines,
    required final int totalFeesMinor,
    required final int payableMinor,
    final String? explanation,
  }) = _$PricingQuoteImpl;

  factory _PricingQuote.fromJson(Map<String, dynamic> json) =
      _$PricingQuoteImpl.fromJson;

  @override
  String get currency;
  @override
  int get baseAmount;
  @override
  List<PricingLine> get lines;
  @override
  int get totalFeesMinor;
  @override
  int get payableMinor;

  /// One sentence a dealer can read: "Gold dealers in Maharashtra pay 2% on auction wins."
  @override
  String? get explanation;

  /// Create a copy of PricingQuote
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PricingQuoteImplCopyWith<_$PricingQuoteImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
