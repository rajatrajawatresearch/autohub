// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'common.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

Option _$OptionFromJson(Map<String, dynamic> json) {
  return _Option.fromJson(json);
}

/// @nodoc
mixin _$Option {
  String get value => throw _privateConstructorUsedError;
  String get label => throw _privateConstructorUsedError;
  int get sortOrder => throw _privateConstructorUsedError;
  String? get icon => throw _privateConstructorUsedError;

  /// Present on facet options only — the number of matching documents.
  int? get count => throw _privateConstructorUsedError;

  /// Serializes this Option to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of Option
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $OptionCopyWith<Option> get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $OptionCopyWith<$Res> {
  factory $OptionCopyWith(Option value, $Res Function(Option) then) =
      _$OptionCopyWithImpl<$Res, Option>;
  @useResult
  $Res call({
    String value,
    String label,
    int sortOrder,
    String? icon,
    int? count,
  });
}

/// @nodoc
class _$OptionCopyWithImpl<$Res, $Val extends Option>
    implements $OptionCopyWith<$Res> {
  _$OptionCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of Option
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? value = null,
    Object? label = null,
    Object? sortOrder = null,
    Object? icon = freezed,
    Object? count = freezed,
  }) {
    return _then(
      _value.copyWith(
            value: null == value
                ? _value.value
                : value // ignore: cast_nullable_to_non_nullable
                      as String,
            label: null == label
                ? _value.label
                : label // ignore: cast_nullable_to_non_nullable
                      as String,
            sortOrder: null == sortOrder
                ? _value.sortOrder
                : sortOrder // ignore: cast_nullable_to_non_nullable
                      as int,
            icon: freezed == icon
                ? _value.icon
                : icon // ignore: cast_nullable_to_non_nullable
                      as String?,
            count: freezed == count
                ? _value.count
                : count // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$OptionImplCopyWith<$Res> implements $OptionCopyWith<$Res> {
  factory _$$OptionImplCopyWith(
    _$OptionImpl value,
    $Res Function(_$OptionImpl) then,
  ) = __$$OptionImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String value,
    String label,
    int sortOrder,
    String? icon,
    int? count,
  });
}

/// @nodoc
class __$$OptionImplCopyWithImpl<$Res>
    extends _$OptionCopyWithImpl<$Res, _$OptionImpl>
    implements _$$OptionImplCopyWith<$Res> {
  __$$OptionImplCopyWithImpl(
    _$OptionImpl _value,
    $Res Function(_$OptionImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of Option
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? value = null,
    Object? label = null,
    Object? sortOrder = null,
    Object? icon = freezed,
    Object? count = freezed,
  }) {
    return _then(
      _$OptionImpl(
        value: null == value
            ? _value.value
            : value // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        sortOrder: null == sortOrder
            ? _value.sortOrder
            : sortOrder // ignore: cast_nullable_to_non_nullable
                  as int,
        icon: freezed == icon
            ? _value.icon
            : icon // ignore: cast_nullable_to_non_nullable
                  as String?,
        count: freezed == count
            ? _value.count
            : count // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$OptionImpl implements _Option {
  const _$OptionImpl({
    required this.value,
    required this.label,
    this.sortOrder = 0,
    this.icon,
    this.count,
  });

  factory _$OptionImpl.fromJson(Map<String, dynamic> json) =>
      _$$OptionImplFromJson(json);

  @override
  final String value;
  @override
  final String label;
  @override
  @JsonKey()
  final int sortOrder;
  @override
  final String? icon;

  /// Present on facet options only — the number of matching documents.
  @override
  final int? count;

  @override
  String toString() {
    return 'Option(value: $value, label: $label, sortOrder: $sortOrder, icon: $icon, count: $count)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$OptionImpl &&
            (identical(other.value, value) || other.value == value) &&
            (identical(other.label, label) || other.label == label) &&
            (identical(other.sortOrder, sortOrder) ||
                other.sortOrder == sortOrder) &&
            (identical(other.icon, icon) || other.icon == icon) &&
            (identical(other.count, count) || other.count == count));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, value, label, sortOrder, icon, count);

  /// Create a copy of Option
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$OptionImplCopyWith<_$OptionImpl> get copyWith =>
      __$$OptionImplCopyWithImpl<_$OptionImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$OptionImplToJson(this);
  }
}

abstract class _Option implements Option {
  const factory _Option({
    required final String value,
    required final String label,
    final int sortOrder,
    final String? icon,
    final int? count,
  }) = _$OptionImpl;

  factory _Option.fromJson(Map<String, dynamic> json) = _$OptionImpl.fromJson;

  @override
  String get value;
  @override
  String get label;
  @override
  int get sortOrder;
  @override
  String? get icon;

  /// Present on facet options only — the number of matching documents.
  @override
  int? get count;

  /// Create a copy of Option
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$OptionImplCopyWith<_$OptionImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

MoneyAmount _$MoneyAmountFromJson(Map<String, dynamic> json) {
  return _MoneyAmount.fromJson(json);
}

/// @nodoc
mixin _$MoneyAmount {
  int get minorUnits => throw _privateConstructorUsedError;
  String get currency => throw _privateConstructorUsedError;

  /// Serializes this MoneyAmount to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of MoneyAmount
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $MoneyAmountCopyWith<MoneyAmount> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $MoneyAmountCopyWith<$Res> {
  factory $MoneyAmountCopyWith(
    MoneyAmount value,
    $Res Function(MoneyAmount) then,
  ) = _$MoneyAmountCopyWithImpl<$Res, MoneyAmount>;
  @useResult
  $Res call({int minorUnits, String currency});
}

/// @nodoc
class _$MoneyAmountCopyWithImpl<$Res, $Val extends MoneyAmount>
    implements $MoneyAmountCopyWith<$Res> {
  _$MoneyAmountCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of MoneyAmount
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
abstract class _$$MoneyAmountImplCopyWith<$Res>
    implements $MoneyAmountCopyWith<$Res> {
  factory _$$MoneyAmountImplCopyWith(
    _$MoneyAmountImpl value,
    $Res Function(_$MoneyAmountImpl) then,
  ) = __$$MoneyAmountImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int minorUnits, String currency});
}

/// @nodoc
class __$$MoneyAmountImplCopyWithImpl<$Res>
    extends _$MoneyAmountCopyWithImpl<$Res, _$MoneyAmountImpl>
    implements _$$MoneyAmountImplCopyWith<$Res> {
  __$$MoneyAmountImplCopyWithImpl(
    _$MoneyAmountImpl _value,
    $Res Function(_$MoneyAmountImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of MoneyAmount
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? minorUnits = null, Object? currency = null}) {
    return _then(
      _$MoneyAmountImpl(
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
class _$MoneyAmountImpl implements _MoneyAmount {
  const _$MoneyAmountImpl({required this.minorUnits, this.currency = 'INR'});

  factory _$MoneyAmountImpl.fromJson(Map<String, dynamic> json) =>
      _$$MoneyAmountImplFromJson(json);

  @override
  final int minorUnits;
  @override
  @JsonKey()
  final String currency;

  @override
  String toString() {
    return 'MoneyAmount(minorUnits: $minorUnits, currency: $currency)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$MoneyAmountImpl &&
            (identical(other.minorUnits, minorUnits) ||
                other.minorUnits == minorUnits) &&
            (identical(other.currency, currency) ||
                other.currency == currency));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, minorUnits, currency);

  /// Create a copy of MoneyAmount
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$MoneyAmountImplCopyWith<_$MoneyAmountImpl> get copyWith =>
      __$$MoneyAmountImplCopyWithImpl<_$MoneyAmountImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$MoneyAmountImplToJson(this);
  }
}

abstract class _MoneyAmount implements MoneyAmount {
  const factory _MoneyAmount({
    required final int minorUnits,
    final String currency,
  }) = _$MoneyAmountImpl;

  factory _MoneyAmount.fromJson(Map<String, dynamic> json) =
      _$MoneyAmountImpl.fromJson;

  @override
  int get minorUnits;
  @override
  String get currency;

  /// Create a copy of MoneyAmount
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$MoneyAmountImplCopyWith<_$MoneyAmountImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

Registration _$RegistrationFromJson(Map<String, dynamic> json) {
  return _Registration.fromJson(json);
}

/// @nodoc
mixin _$Registration {
  /// `MH12AB1234` — the storage and comparison form.
  String get canonical => throw _privateConstructorUsedError;

  /// `MH 12 AB 1234` — the form `PlateBlock` renders.
  String get display => throw _privateConstructorUsedError;

  /// `private`, `commercial`, `ev`, `government`, `defence`.
  ///
  /// Deliberately a `String` and not an enum. Plate classes are a road-transport fact the
  /// geography registry owns — a new class (the green-and-yellow commercial EV plate, say)
  /// arrives as a data row, and an app that could not decode a listing carrying one would drop
  /// it out of search results. `autohub_ui` maps the known values to a palette and falls back to
  /// the private white for anything else.
  String? get plateClass => throw _privateConstructorUsedError;
  String? get stateCode => throw _privateConstructorUsedError;

  /// Serializes this Registration to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of Registration
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $RegistrationCopyWith<Registration> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $RegistrationCopyWith<$Res> {
  factory $RegistrationCopyWith(
    Registration value,
    $Res Function(Registration) then,
  ) = _$RegistrationCopyWithImpl<$Res, Registration>;
  @useResult
  $Res call({
    String canonical,
    String display,
    String? plateClass,
    String? stateCode,
  });
}

/// @nodoc
class _$RegistrationCopyWithImpl<$Res, $Val extends Registration>
    implements $RegistrationCopyWith<$Res> {
  _$RegistrationCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of Registration
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? canonical = null,
    Object? display = null,
    Object? plateClass = freezed,
    Object? stateCode = freezed,
  }) {
    return _then(
      _value.copyWith(
            canonical: null == canonical
                ? _value.canonical
                : canonical // ignore: cast_nullable_to_non_nullable
                      as String,
            display: null == display
                ? _value.display
                : display // ignore: cast_nullable_to_non_nullable
                      as String,
            plateClass: freezed == plateClass
                ? _value.plateClass
                : plateClass // ignore: cast_nullable_to_non_nullable
                      as String?,
            stateCode: freezed == stateCode
                ? _value.stateCode
                : stateCode // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$RegistrationImplCopyWith<$Res>
    implements $RegistrationCopyWith<$Res> {
  factory _$$RegistrationImplCopyWith(
    _$RegistrationImpl value,
    $Res Function(_$RegistrationImpl) then,
  ) = __$$RegistrationImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String canonical,
    String display,
    String? plateClass,
    String? stateCode,
  });
}

/// @nodoc
class __$$RegistrationImplCopyWithImpl<$Res>
    extends _$RegistrationCopyWithImpl<$Res, _$RegistrationImpl>
    implements _$$RegistrationImplCopyWith<$Res> {
  __$$RegistrationImplCopyWithImpl(
    _$RegistrationImpl _value,
    $Res Function(_$RegistrationImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of Registration
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? canonical = null,
    Object? display = null,
    Object? plateClass = freezed,
    Object? stateCode = freezed,
  }) {
    return _then(
      _$RegistrationImpl(
        canonical: null == canonical
            ? _value.canonical
            : canonical // ignore: cast_nullable_to_non_nullable
                  as String,
        display: null == display
            ? _value.display
            : display // ignore: cast_nullable_to_non_nullable
                  as String,
        plateClass: freezed == plateClass
            ? _value.plateClass
            : plateClass // ignore: cast_nullable_to_non_nullable
                  as String?,
        stateCode: freezed == stateCode
            ? _value.stateCode
            : stateCode // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$RegistrationImpl implements _Registration {
  const _$RegistrationImpl({
    required this.canonical,
    required this.display,
    this.plateClass,
    this.stateCode,
  });

  factory _$RegistrationImpl.fromJson(Map<String, dynamic> json) =>
      _$$RegistrationImplFromJson(json);

  /// `MH12AB1234` — the storage and comparison form.
  @override
  final String canonical;

  /// `MH 12 AB 1234` — the form `PlateBlock` renders.
  @override
  final String display;

  /// `private`, `commercial`, `ev`, `government`, `defence`.
  ///
  /// Deliberately a `String` and not an enum. Plate classes are a road-transport fact the
  /// geography registry owns — a new class (the green-and-yellow commercial EV plate, say)
  /// arrives as a data row, and an app that could not decode a listing carrying one would drop
  /// it out of search results. `autohub_ui` maps the known values to a palette and falls back to
  /// the private white for anything else.
  @override
  final String? plateClass;
  @override
  final String? stateCode;

  @override
  String toString() {
    return 'Registration(canonical: $canonical, display: $display, plateClass: $plateClass, stateCode: $stateCode)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$RegistrationImpl &&
            (identical(other.canonical, canonical) ||
                other.canonical == canonical) &&
            (identical(other.display, display) || other.display == display) &&
            (identical(other.plateClass, plateClass) ||
                other.plateClass == plateClass) &&
            (identical(other.stateCode, stateCode) ||
                other.stateCode == stateCode));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, canonical, display, plateClass, stateCode);

  /// Create a copy of Registration
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$RegistrationImplCopyWith<_$RegistrationImpl> get copyWith =>
      __$$RegistrationImplCopyWithImpl<_$RegistrationImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$RegistrationImplToJson(this);
  }
}

abstract class _Registration implements Registration {
  const factory _Registration({
    required final String canonical,
    required final String display,
    final String? plateClass,
    final String? stateCode,
  }) = _$RegistrationImpl;

  factory _Registration.fromJson(Map<String, dynamic> json) =
      _$RegistrationImpl.fromJson;

  /// `MH12AB1234` — the storage and comparison form.
  @override
  String get canonical;

  /// `MH 12 AB 1234` — the form `PlateBlock` renders.
  @override
  String get display;

  /// `private`, `commercial`, `ev`, `government`, `defence`.
  ///
  /// Deliberately a `String` and not an enum. Plate classes are a road-transport fact the
  /// geography registry owns — a new class (the green-and-yellow commercial EV plate, say)
  /// arrives as a data row, and an app that could not decode a listing carrying one would drop
  /// it out of search results. `autohub_ui` maps the known values to a palette and falls back to
  /// the private white for anything else.
  @override
  String? get plateClass;
  @override
  String? get stateCode;

  /// Create a copy of Registration
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$RegistrationImplCopyWith<_$RegistrationImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

SpecItem _$SpecItemFromJson(Map<String, dynamic> json) {
  return _SpecItem.fromJson(json);
}

/// @nodoc
mixin _$SpecItem {
  String get label => throw _privateConstructorUsedError;
  String get value => throw _privateConstructorUsedError;
  String? get code => throw _privateConstructorUsedError;

  /// Serializes this SpecItem to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of SpecItem
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $SpecItemCopyWith<SpecItem> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $SpecItemCopyWith<$Res> {
  factory $SpecItemCopyWith(SpecItem value, $Res Function(SpecItem) then) =
      _$SpecItemCopyWithImpl<$Res, SpecItem>;
  @useResult
  $Res call({String label, String value, String? code});
}

/// @nodoc
class _$SpecItemCopyWithImpl<$Res, $Val extends SpecItem>
    implements $SpecItemCopyWith<$Res> {
  _$SpecItemCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of SpecItem
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? label = null,
    Object? value = null,
    Object? code = freezed,
  }) {
    return _then(
      _value.copyWith(
            label: null == label
                ? _value.label
                : label // ignore: cast_nullable_to_non_nullable
                      as String,
            value: null == value
                ? _value.value
                : value // ignore: cast_nullable_to_non_nullable
                      as String,
            code: freezed == code
                ? _value.code
                : code // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$SpecItemImplCopyWith<$Res>
    implements $SpecItemCopyWith<$Res> {
  factory _$$SpecItemImplCopyWith(
    _$SpecItemImpl value,
    $Res Function(_$SpecItemImpl) then,
  ) = __$$SpecItemImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String label, String value, String? code});
}

/// @nodoc
class __$$SpecItemImplCopyWithImpl<$Res>
    extends _$SpecItemCopyWithImpl<$Res, _$SpecItemImpl>
    implements _$$SpecItemImplCopyWith<$Res> {
  __$$SpecItemImplCopyWithImpl(
    _$SpecItemImpl _value,
    $Res Function(_$SpecItemImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SpecItem
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? label = null,
    Object? value = null,
    Object? code = freezed,
  }) {
    return _then(
      _$SpecItemImpl(
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        value: null == value
            ? _value.value
            : value // ignore: cast_nullable_to_non_nullable
                  as String,
        code: freezed == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SpecItemImpl implements _SpecItem {
  const _$SpecItemImpl({required this.label, required this.value, this.code});

  factory _$SpecItemImpl.fromJson(Map<String, dynamic> json) =>
      _$$SpecItemImplFromJson(json);

  @override
  final String label;
  @override
  final String value;
  @override
  final String? code;

  @override
  String toString() {
    return 'SpecItem(label: $label, value: $value, code: $code)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SpecItemImpl &&
            (identical(other.label, label) || other.label == label) &&
            (identical(other.value, value) || other.value == value) &&
            (identical(other.code, code) || other.code == code));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, label, value, code);

  /// Create a copy of SpecItem
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SpecItemImplCopyWith<_$SpecItemImpl> get copyWith =>
      __$$SpecItemImplCopyWithImpl<_$SpecItemImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$SpecItemImplToJson(this);
  }
}

abstract class _SpecItem implements SpecItem {
  const factory _SpecItem({
    required final String label,
    required final String value,
    final String? code,
  }) = _$SpecItemImpl;

  factory _SpecItem.fromJson(Map<String, dynamic> json) =
      _$SpecItemImpl.fromJson;

  @override
  String get label;
  @override
  String get value;
  @override
  String? get code;

  /// Create a copy of SpecItem
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SpecItemImplCopyWith<_$SpecItemImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

Media _$MediaFromJson(Map<String, dynamic> json) {
  return _Media.fromJson(json);
}

/// @nodoc
mixin _$Media {
  String? get coverUrl => throw _privateConstructorUsedError;
  double? get aspectRatio => throw _privateConstructorUsedError;
  int get count => throw _privateConstructorUsedError;

  /// Serializes this Media to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of Media
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $MediaCopyWith<Media> get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $MediaCopyWith<$Res> {
  factory $MediaCopyWith(Media value, $Res Function(Media) then) =
      _$MediaCopyWithImpl<$Res, Media>;
  @useResult
  $Res call({String? coverUrl, double? aspectRatio, int count});
}

/// @nodoc
class _$MediaCopyWithImpl<$Res, $Val extends Media>
    implements $MediaCopyWith<$Res> {
  _$MediaCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of Media
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? coverUrl = freezed,
    Object? aspectRatio = freezed,
    Object? count = null,
  }) {
    return _then(
      _value.copyWith(
            coverUrl: freezed == coverUrl
                ? _value.coverUrl
                : coverUrl // ignore: cast_nullable_to_non_nullable
                      as String?,
            aspectRatio: freezed == aspectRatio
                ? _value.aspectRatio
                : aspectRatio // ignore: cast_nullable_to_non_nullable
                      as double?,
            count: null == count
                ? _value.count
                : count // ignore: cast_nullable_to_non_nullable
                      as int,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$MediaImplCopyWith<$Res> implements $MediaCopyWith<$Res> {
  factory _$$MediaImplCopyWith(
    _$MediaImpl value,
    $Res Function(_$MediaImpl) then,
  ) = __$$MediaImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String? coverUrl, double? aspectRatio, int count});
}

/// @nodoc
class __$$MediaImplCopyWithImpl<$Res>
    extends _$MediaCopyWithImpl<$Res, _$MediaImpl>
    implements _$$MediaImplCopyWith<$Res> {
  __$$MediaImplCopyWithImpl(
    _$MediaImpl _value,
    $Res Function(_$MediaImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of Media
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? coverUrl = freezed,
    Object? aspectRatio = freezed,
    Object? count = null,
  }) {
    return _then(
      _$MediaImpl(
        coverUrl: freezed == coverUrl
            ? _value.coverUrl
            : coverUrl // ignore: cast_nullable_to_non_nullable
                  as String?,
        aspectRatio: freezed == aspectRatio
            ? _value.aspectRatio
            : aspectRatio // ignore: cast_nullable_to_non_nullable
                  as double?,
        count: null == count
            ? _value.count
            : count // ignore: cast_nullable_to_non_nullable
                  as int,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$MediaImpl extends _Media {
  const _$MediaImpl({this.coverUrl, this.aspectRatio, this.count = 0})
    : super._();

  factory _$MediaImpl.fromJson(Map<String, dynamic> json) =>
      _$$MediaImplFromJson(json);

  @override
  final String? coverUrl;
  @override
  final double? aspectRatio;
  @override
  @JsonKey()
  final int count;

  @override
  String toString() {
    return 'Media(coverUrl: $coverUrl, aspectRatio: $aspectRatio, count: $count)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$MediaImpl &&
            (identical(other.coverUrl, coverUrl) ||
                other.coverUrl == coverUrl) &&
            (identical(other.aspectRatio, aspectRatio) ||
                other.aspectRatio == aspectRatio) &&
            (identical(other.count, count) || other.count == count));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, coverUrl, aspectRatio, count);

  /// Create a copy of Media
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$MediaImplCopyWith<_$MediaImpl> get copyWith =>
      __$$MediaImplCopyWithImpl<_$MediaImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$MediaImplToJson(this);
  }
}

abstract class _Media extends Media {
  const factory _Media({
    final String? coverUrl,
    final double? aspectRatio,
    final int count,
  }) = _$MediaImpl;
  const _Media._() : super._();

  factory _Media.fromJson(Map<String, dynamic> json) = _$MediaImpl.fromJson;

  @override
  String? get coverUrl;
  @override
  double? get aspectRatio;
  @override
  int get count;

  /// Create a copy of Media
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$MediaImplCopyWith<_$MediaImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

MediaAsset _$MediaAssetFromJson(Map<String, dynamic> json) {
  return _MediaAsset.fromJson(json);
}

/// @nodoc
mixin _$MediaAsset {
  String get id => throw _privateConstructorUsedError;
  String get url => throw _privateConstructorUsedError;
  double get aspectRatio => throw _privateConstructorUsedError;
  String get kind => throw _privateConstructorUsedError;
  String? get caption => throw _privateConstructorUsedError;

  /// Serializes this MediaAsset to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of MediaAsset
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $MediaAssetCopyWith<MediaAsset> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $MediaAssetCopyWith<$Res> {
  factory $MediaAssetCopyWith(
    MediaAsset value,
    $Res Function(MediaAsset) then,
  ) = _$MediaAssetCopyWithImpl<$Res, MediaAsset>;
  @useResult
  $Res call({
    String id,
    String url,
    double aspectRatio,
    String kind,
    String? caption,
  });
}

/// @nodoc
class _$MediaAssetCopyWithImpl<$Res, $Val extends MediaAsset>
    implements $MediaAssetCopyWith<$Res> {
  _$MediaAssetCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of MediaAsset
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? url = null,
    Object? aspectRatio = null,
    Object? kind = null,
    Object? caption = freezed,
  }) {
    return _then(
      _value.copyWith(
            id: null == id
                ? _value.id
                : id // ignore: cast_nullable_to_non_nullable
                      as String,
            url: null == url
                ? _value.url
                : url // ignore: cast_nullable_to_non_nullable
                      as String,
            aspectRatio: null == aspectRatio
                ? _value.aspectRatio
                : aspectRatio // ignore: cast_nullable_to_non_nullable
                      as double,
            kind: null == kind
                ? _value.kind
                : kind // ignore: cast_nullable_to_non_nullable
                      as String,
            caption: freezed == caption
                ? _value.caption
                : caption // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$MediaAssetImplCopyWith<$Res>
    implements $MediaAssetCopyWith<$Res> {
  factory _$$MediaAssetImplCopyWith(
    _$MediaAssetImpl value,
    $Res Function(_$MediaAssetImpl) then,
  ) = __$$MediaAssetImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String id,
    String url,
    double aspectRatio,
    String kind,
    String? caption,
  });
}

/// @nodoc
class __$$MediaAssetImplCopyWithImpl<$Res>
    extends _$MediaAssetCopyWithImpl<$Res, _$MediaAssetImpl>
    implements _$$MediaAssetImplCopyWith<$Res> {
  __$$MediaAssetImplCopyWithImpl(
    _$MediaAssetImpl _value,
    $Res Function(_$MediaAssetImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of MediaAsset
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? url = null,
    Object? aspectRatio = null,
    Object? kind = null,
    Object? caption = freezed,
  }) {
    return _then(
      _$MediaAssetImpl(
        id: null == id
            ? _value.id
            : id // ignore: cast_nullable_to_non_nullable
                  as String,
        url: null == url
            ? _value.url
            : url // ignore: cast_nullable_to_non_nullable
                  as String,
        aspectRatio: null == aspectRatio
            ? _value.aspectRatio
            : aspectRatio // ignore: cast_nullable_to_non_nullable
                  as double,
        kind: null == kind
            ? _value.kind
            : kind // ignore: cast_nullable_to_non_nullable
                  as String,
        caption: freezed == caption
            ? _value.caption
            : caption // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$MediaAssetImpl implements _MediaAsset {
  const _$MediaAssetImpl({
    required this.id,
    required this.url,
    this.aspectRatio = 1.5,
    this.kind = 'photo',
    this.caption,
  });

  factory _$MediaAssetImpl.fromJson(Map<String, dynamic> json) =>
      _$$MediaAssetImplFromJson(json);

  @override
  final String id;
  @override
  final String url;
  @override
  @JsonKey()
  final double aspectRatio;
  @override
  @JsonKey()
  final String kind;
  @override
  final String? caption;

  @override
  String toString() {
    return 'MediaAsset(id: $id, url: $url, aspectRatio: $aspectRatio, kind: $kind, caption: $caption)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$MediaAssetImpl &&
            (identical(other.id, id) || other.id == id) &&
            (identical(other.url, url) || other.url == url) &&
            (identical(other.aspectRatio, aspectRatio) ||
                other.aspectRatio == aspectRatio) &&
            (identical(other.kind, kind) || other.kind == kind) &&
            (identical(other.caption, caption) || other.caption == caption));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, id, url, aspectRatio, kind, caption);

  /// Create a copy of MediaAsset
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$MediaAssetImplCopyWith<_$MediaAssetImpl> get copyWith =>
      __$$MediaAssetImplCopyWithImpl<_$MediaAssetImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$MediaAssetImplToJson(this);
  }
}

abstract class _MediaAsset implements MediaAsset {
  const factory _MediaAsset({
    required final String id,
    required final String url,
    final double aspectRatio,
    final String kind,
    final String? caption,
  }) = _$MediaAssetImpl;

  factory _MediaAsset.fromJson(Map<String, dynamic> json) =
      _$MediaAssetImpl.fromJson;

  @override
  String get id;
  @override
  String get url;
  @override
  double get aspectRatio;
  @override
  String get kind;
  @override
  String? get caption;

  /// Create a copy of MediaAsset
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$MediaAssetImplCopyWith<_$MediaAssetImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
