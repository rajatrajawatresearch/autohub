// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'valuation.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

ValuationQuoteRequest _$ValuationQuoteRequestFromJson(
  Map<String, dynamic> json,
) {
  return _ValuationQuoteRequest.fromJson(json);
}

/// @nodoc
mixin _$ValuationQuoteRequest {
  String get registrationNumber => throw _privateConstructorUsedError;
  int? get kmsDriven => throw _privateConstructorUsedError;
  String? get city => throw _privateConstructorUsedError;

  /// `excellent` / `good` / `fair` / `poor` — but the options themselves come from the form
  /// registry, which is why this is a `String`. An enum here would mean a fifth condition band
  /// needs an app release.
  String? get condition => throw _privateConstructorUsedError;

  /// Set when the plate is not in VAHAN and the user typed the vehicle in by hand.
  String? get makeCode => throw _privateConstructorUsedError;
  String? get modelCode => throw _privateConstructorUsedError;
  String? get variantCode => throw _privateConstructorUsedError;
  int? get year => throw _privateConstructorUsedError;

  /// Serializes this ValuationQuoteRequest to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ValuationQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ValuationQuoteRequestCopyWith<ValuationQuoteRequest> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ValuationQuoteRequestCopyWith<$Res> {
  factory $ValuationQuoteRequestCopyWith(
    ValuationQuoteRequest value,
    $Res Function(ValuationQuoteRequest) then,
  ) = _$ValuationQuoteRequestCopyWithImpl<$Res, ValuationQuoteRequest>;
  @useResult
  $Res call({
    String registrationNumber,
    int? kmsDriven,
    String? city,
    String? condition,
    String? makeCode,
    String? modelCode,
    String? variantCode,
    int? year,
  });
}

/// @nodoc
class _$ValuationQuoteRequestCopyWithImpl<
  $Res,
  $Val extends ValuationQuoteRequest
>
    implements $ValuationQuoteRequestCopyWith<$Res> {
  _$ValuationQuoteRequestCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ValuationQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? registrationNumber = null,
    Object? kmsDriven = freezed,
    Object? city = freezed,
    Object? condition = freezed,
    Object? makeCode = freezed,
    Object? modelCode = freezed,
    Object? variantCode = freezed,
    Object? year = freezed,
  }) {
    return _then(
      _value.copyWith(
            registrationNumber: null == registrationNumber
                ? _value.registrationNumber
                : registrationNumber // ignore: cast_nullable_to_non_nullable
                      as String,
            kmsDriven: freezed == kmsDriven
                ? _value.kmsDriven
                : kmsDriven // ignore: cast_nullable_to_non_nullable
                      as int?,
            city: freezed == city
                ? _value.city
                : city // ignore: cast_nullable_to_non_nullable
                      as String?,
            condition: freezed == condition
                ? _value.condition
                : condition // ignore: cast_nullable_to_non_nullable
                      as String?,
            makeCode: freezed == makeCode
                ? _value.makeCode
                : makeCode // ignore: cast_nullable_to_non_nullable
                      as String?,
            modelCode: freezed == modelCode
                ? _value.modelCode
                : modelCode // ignore: cast_nullable_to_non_nullable
                      as String?,
            variantCode: freezed == variantCode
                ? _value.variantCode
                : variantCode // ignore: cast_nullable_to_non_nullable
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
abstract class _$$ValuationQuoteRequestImplCopyWith<$Res>
    implements $ValuationQuoteRequestCopyWith<$Res> {
  factory _$$ValuationQuoteRequestImplCopyWith(
    _$ValuationQuoteRequestImpl value,
    $Res Function(_$ValuationQuoteRequestImpl) then,
  ) = __$$ValuationQuoteRequestImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String registrationNumber,
    int? kmsDriven,
    String? city,
    String? condition,
    String? makeCode,
    String? modelCode,
    String? variantCode,
    int? year,
  });
}

/// @nodoc
class __$$ValuationQuoteRequestImplCopyWithImpl<$Res>
    extends
        _$ValuationQuoteRequestCopyWithImpl<$Res, _$ValuationQuoteRequestImpl>
    implements _$$ValuationQuoteRequestImplCopyWith<$Res> {
  __$$ValuationQuoteRequestImplCopyWithImpl(
    _$ValuationQuoteRequestImpl _value,
    $Res Function(_$ValuationQuoteRequestImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValuationQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? registrationNumber = null,
    Object? kmsDriven = freezed,
    Object? city = freezed,
    Object? condition = freezed,
    Object? makeCode = freezed,
    Object? modelCode = freezed,
    Object? variantCode = freezed,
    Object? year = freezed,
  }) {
    return _then(
      _$ValuationQuoteRequestImpl(
        registrationNumber: null == registrationNumber
            ? _value.registrationNumber
            : registrationNumber // ignore: cast_nullable_to_non_nullable
                  as String,
        kmsDriven: freezed == kmsDriven
            ? _value.kmsDriven
            : kmsDriven // ignore: cast_nullable_to_non_nullable
                  as int?,
        city: freezed == city
            ? _value.city
            : city // ignore: cast_nullable_to_non_nullable
                  as String?,
        condition: freezed == condition
            ? _value.condition
            : condition // ignore: cast_nullable_to_non_nullable
                  as String?,
        makeCode: freezed == makeCode
            ? _value.makeCode
            : makeCode // ignore: cast_nullable_to_non_nullable
                  as String?,
        modelCode: freezed == modelCode
            ? _value.modelCode
            : modelCode // ignore: cast_nullable_to_non_nullable
                  as String?,
        variantCode: freezed == variantCode
            ? _value.variantCode
            : variantCode // ignore: cast_nullable_to_non_nullable
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
class _$ValuationQuoteRequestImpl implements _ValuationQuoteRequest {
  const _$ValuationQuoteRequestImpl({
    required this.registrationNumber,
    this.kmsDriven,
    this.city,
    this.condition,
    this.makeCode,
    this.modelCode,
    this.variantCode,
    this.year,
  });

  factory _$ValuationQuoteRequestImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValuationQuoteRequestImplFromJson(json);

  @override
  final String registrationNumber;
  @override
  final int? kmsDriven;
  @override
  final String? city;

  /// `excellent` / `good` / `fair` / `poor` — but the options themselves come from the form
  /// registry, which is why this is a `String`. An enum here would mean a fifth condition band
  /// needs an app release.
  @override
  final String? condition;

  /// Set when the plate is not in VAHAN and the user typed the vehicle in by hand.
  @override
  final String? makeCode;
  @override
  final String? modelCode;
  @override
  final String? variantCode;
  @override
  final int? year;

  @override
  String toString() {
    return 'ValuationQuoteRequest(registrationNumber: $registrationNumber, kmsDriven: $kmsDriven, city: $city, condition: $condition, makeCode: $makeCode, modelCode: $modelCode, variantCode: $variantCode, year: $year)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValuationQuoteRequestImpl &&
            (identical(other.registrationNumber, registrationNumber) ||
                other.registrationNumber == registrationNumber) &&
            (identical(other.kmsDriven, kmsDriven) ||
                other.kmsDriven == kmsDriven) &&
            (identical(other.city, city) || other.city == city) &&
            (identical(other.condition, condition) ||
                other.condition == condition) &&
            (identical(other.makeCode, makeCode) ||
                other.makeCode == makeCode) &&
            (identical(other.modelCode, modelCode) ||
                other.modelCode == modelCode) &&
            (identical(other.variantCode, variantCode) ||
                other.variantCode == variantCode) &&
            (identical(other.year, year) || other.year == year));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    registrationNumber,
    kmsDriven,
    city,
    condition,
    makeCode,
    modelCode,
    variantCode,
    year,
  );

  /// Create a copy of ValuationQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValuationQuoteRequestImplCopyWith<_$ValuationQuoteRequestImpl>
  get copyWith =>
      __$$ValuationQuoteRequestImplCopyWithImpl<_$ValuationQuoteRequestImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$ValuationQuoteRequestImplToJson(this);
  }
}

abstract class _ValuationQuoteRequest implements ValuationQuoteRequest {
  const factory _ValuationQuoteRequest({
    required final String registrationNumber,
    final int? kmsDriven,
    final String? city,
    final String? condition,
    final String? makeCode,
    final String? modelCode,
    final String? variantCode,
    final int? year,
  }) = _$ValuationQuoteRequestImpl;

  factory _ValuationQuoteRequest.fromJson(Map<String, dynamic> json) =
      _$ValuationQuoteRequestImpl.fromJson;

  @override
  String get registrationNumber;
  @override
  int? get kmsDriven;
  @override
  String? get city;

  /// `excellent` / `good` / `fair` / `poor` — but the options themselves come from the form
  /// registry, which is why this is a `String`. An enum here would mean a fifth condition band
  /// needs an app release.
  @override
  String? get condition;

  /// Set when the plate is not in VAHAN and the user typed the vehicle in by hand.
  @override
  String? get makeCode;
  @override
  String? get modelCode;
  @override
  String? get variantCode;
  @override
  int? get year;

  /// Create a copy of ValuationQuoteRequest
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValuationQuoteRequestImplCopyWith<_$ValuationQuoteRequestImpl>
  get copyWith => throw _privateConstructorUsedError;
}

ValuationVehicle _$ValuationVehicleFromJson(Map<String, dynamic> json) {
  return _ValuationVehicle.fromJson(json);
}

/// @nodoc
mixin _$ValuationVehicle {
  String get title => throw _privateConstructorUsedError;
  Registration get registration => throw _privateConstructorUsedError;
  int? get year => throw _privateConstructorUsedError;
  String? get fuel => throw _privateConstructorUsedError;
  String? get transmission => throw _privateConstructorUsedError;

  /// `vahan` when the RTO record answered, `manual` when the user typed it. Shown, because a
  /// number derived from a guess must say so.
  String? get source => throw _privateConstructorUsedError;

  /// Serializes this ValuationVehicle to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ValuationVehicle
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ValuationVehicleCopyWith<ValuationVehicle> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ValuationVehicleCopyWith<$Res> {
  factory $ValuationVehicleCopyWith(
    ValuationVehicle value,
    $Res Function(ValuationVehicle) then,
  ) = _$ValuationVehicleCopyWithImpl<$Res, ValuationVehicle>;
  @useResult
  $Res call({
    String title,
    Registration registration,
    int? year,
    String? fuel,
    String? transmission,
    String? source,
  });

  $RegistrationCopyWith<$Res> get registration;
}

/// @nodoc
class _$ValuationVehicleCopyWithImpl<$Res, $Val extends ValuationVehicle>
    implements $ValuationVehicleCopyWith<$Res> {
  _$ValuationVehicleCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ValuationVehicle
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? title = null,
    Object? registration = null,
    Object? year = freezed,
    Object? fuel = freezed,
    Object? transmission = freezed,
    Object? source = freezed,
  }) {
    return _then(
      _value.copyWith(
            title: null == title
                ? _value.title
                : title // ignore: cast_nullable_to_non_nullable
                      as String,
            registration: null == registration
                ? _value.registration
                : registration // ignore: cast_nullable_to_non_nullable
                      as Registration,
            year: freezed == year
                ? _value.year
                : year // ignore: cast_nullable_to_non_nullable
                      as int?,
            fuel: freezed == fuel
                ? _value.fuel
                : fuel // ignore: cast_nullable_to_non_nullable
                      as String?,
            transmission: freezed == transmission
                ? _value.transmission
                : transmission // ignore: cast_nullable_to_non_nullable
                      as String?,
            source: freezed == source
                ? _value.source
                : source // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }

  /// Create a copy of ValuationVehicle
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $RegistrationCopyWith<$Res> get registration {
    return $RegistrationCopyWith<$Res>(_value.registration, (value) {
      return _then(_value.copyWith(registration: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$ValuationVehicleImplCopyWith<$Res>
    implements $ValuationVehicleCopyWith<$Res> {
  factory _$$ValuationVehicleImplCopyWith(
    _$ValuationVehicleImpl value,
    $Res Function(_$ValuationVehicleImpl) then,
  ) = __$$ValuationVehicleImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String title,
    Registration registration,
    int? year,
    String? fuel,
    String? transmission,
    String? source,
  });

  @override
  $RegistrationCopyWith<$Res> get registration;
}

/// @nodoc
class __$$ValuationVehicleImplCopyWithImpl<$Res>
    extends _$ValuationVehicleCopyWithImpl<$Res, _$ValuationVehicleImpl>
    implements _$$ValuationVehicleImplCopyWith<$Res> {
  __$$ValuationVehicleImplCopyWithImpl(
    _$ValuationVehicleImpl _value,
    $Res Function(_$ValuationVehicleImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValuationVehicle
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? title = null,
    Object? registration = null,
    Object? year = freezed,
    Object? fuel = freezed,
    Object? transmission = freezed,
    Object? source = freezed,
  }) {
    return _then(
      _$ValuationVehicleImpl(
        title: null == title
            ? _value.title
            : title // ignore: cast_nullable_to_non_nullable
                  as String,
        registration: null == registration
            ? _value.registration
            : registration // ignore: cast_nullable_to_non_nullable
                  as Registration,
        year: freezed == year
            ? _value.year
            : year // ignore: cast_nullable_to_non_nullable
                  as int?,
        fuel: freezed == fuel
            ? _value.fuel
            : fuel // ignore: cast_nullable_to_non_nullable
                  as String?,
        transmission: freezed == transmission
            ? _value.transmission
            : transmission // ignore: cast_nullable_to_non_nullable
                  as String?,
        source: freezed == source
            ? _value.source
            : source // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValuationVehicleImpl implements _ValuationVehicle {
  const _$ValuationVehicleImpl({
    required this.title,
    required this.registration,
    this.year,
    this.fuel,
    this.transmission,
    this.source,
  });

  factory _$ValuationVehicleImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValuationVehicleImplFromJson(json);

  @override
  final String title;
  @override
  final Registration registration;
  @override
  final int? year;
  @override
  final String? fuel;
  @override
  final String? transmission;

  /// `vahan` when the RTO record answered, `manual` when the user typed it. Shown, because a
  /// number derived from a guess must say so.
  @override
  final String? source;

  @override
  String toString() {
    return 'ValuationVehicle(title: $title, registration: $registration, year: $year, fuel: $fuel, transmission: $transmission, source: $source)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValuationVehicleImpl &&
            (identical(other.title, title) || other.title == title) &&
            (identical(other.registration, registration) ||
                other.registration == registration) &&
            (identical(other.year, year) || other.year == year) &&
            (identical(other.fuel, fuel) || other.fuel == fuel) &&
            (identical(other.transmission, transmission) ||
                other.transmission == transmission) &&
            (identical(other.source, source) || other.source == source));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    title,
    registration,
    year,
    fuel,
    transmission,
    source,
  );

  /// Create a copy of ValuationVehicle
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValuationVehicleImplCopyWith<_$ValuationVehicleImpl> get copyWith =>
      __$$ValuationVehicleImplCopyWithImpl<_$ValuationVehicleImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$ValuationVehicleImplToJson(this);
  }
}

abstract class _ValuationVehicle implements ValuationVehicle {
  const factory _ValuationVehicle({
    required final String title,
    required final Registration registration,
    final int? year,
    final String? fuel,
    final String? transmission,
    final String? source,
  }) = _$ValuationVehicleImpl;

  factory _ValuationVehicle.fromJson(Map<String, dynamic> json) =
      _$ValuationVehicleImpl.fromJson;

  @override
  String get title;
  @override
  Registration get registration;
  @override
  int? get year;
  @override
  String? get fuel;
  @override
  String? get transmission;

  /// `vahan` when the RTO record answered, `manual` when the user typed it. Shown, because a
  /// number derived from a guess must say so.
  @override
  String? get source;

  /// Create a copy of ValuationVehicle
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValuationVehicleImplCopyWith<_$ValuationVehicleImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

ValuationBand _$ValuationBandFromJson(Map<String, dynamic> json) {
  return _ValuationBand.fromJson(json);
}

/// @nodoc
mixin _$ValuationBand {
  String get code => throw _privateConstructorUsedError;
  String get label => throw _privateConstructorUsedError;
  int get amountMinor => throw _privateConstructorUsedError;

  /// Exactly one band is primary — the number the odometer rolls to.
  bool get isPrimary => throw _privateConstructorUsedError;
  String? get helpText => throw _privateConstructorUsedError;

  /// Serializes this ValuationBand to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ValuationBand
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ValuationBandCopyWith<ValuationBand> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ValuationBandCopyWith<$Res> {
  factory $ValuationBandCopyWith(
    ValuationBand value,
    $Res Function(ValuationBand) then,
  ) = _$ValuationBandCopyWithImpl<$Res, ValuationBand>;
  @useResult
  $Res call({
    String code,
    String label,
    int amountMinor,
    bool isPrimary,
    String? helpText,
  });
}

/// @nodoc
class _$ValuationBandCopyWithImpl<$Res, $Val extends ValuationBand>
    implements $ValuationBandCopyWith<$Res> {
  _$ValuationBandCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ValuationBand
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? amountMinor = null,
    Object? isPrimary = null,
    Object? helpText = freezed,
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
            amountMinor: null == amountMinor
                ? _value.amountMinor
                : amountMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            isPrimary: null == isPrimary
                ? _value.isPrimary
                : isPrimary // ignore: cast_nullable_to_non_nullable
                      as bool,
            helpText: freezed == helpText
                ? _value.helpText
                : helpText // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$ValuationBandImplCopyWith<$Res>
    implements $ValuationBandCopyWith<$Res> {
  factory _$$ValuationBandImplCopyWith(
    _$ValuationBandImpl value,
    $Res Function(_$ValuationBandImpl) then,
  ) = __$$ValuationBandImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String code,
    String label,
    int amountMinor,
    bool isPrimary,
    String? helpText,
  });
}

/// @nodoc
class __$$ValuationBandImplCopyWithImpl<$Res>
    extends _$ValuationBandCopyWithImpl<$Res, _$ValuationBandImpl>
    implements _$$ValuationBandImplCopyWith<$Res> {
  __$$ValuationBandImplCopyWithImpl(
    _$ValuationBandImpl _value,
    $Res Function(_$ValuationBandImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValuationBand
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? amountMinor = null,
    Object? isPrimary = null,
    Object? helpText = freezed,
  }) {
    return _then(
      _$ValuationBandImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        amountMinor: null == amountMinor
            ? _value.amountMinor
            : amountMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        isPrimary: null == isPrimary
            ? _value.isPrimary
            : isPrimary // ignore: cast_nullable_to_non_nullable
                  as bool,
        helpText: freezed == helpText
            ? _value.helpText
            : helpText // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValuationBandImpl implements _ValuationBand {
  const _$ValuationBandImpl({
    required this.code,
    required this.label,
    required this.amountMinor,
    this.isPrimary = false,
    this.helpText,
  });

  factory _$ValuationBandImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValuationBandImplFromJson(json);

  @override
  final String code;
  @override
  final String label;
  @override
  final int amountMinor;

  /// Exactly one band is primary — the number the odometer rolls to.
  @override
  @JsonKey()
  final bool isPrimary;
  @override
  final String? helpText;

  @override
  String toString() {
    return 'ValuationBand(code: $code, label: $label, amountMinor: $amountMinor, isPrimary: $isPrimary, helpText: $helpText)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValuationBandImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.label, label) || other.label == label) &&
            (identical(other.amountMinor, amountMinor) ||
                other.amountMinor == amountMinor) &&
            (identical(other.isPrimary, isPrimary) ||
                other.isPrimary == isPrimary) &&
            (identical(other.helpText, helpText) ||
                other.helpText == helpText));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, code, label, amountMinor, isPrimary, helpText);

  /// Create a copy of ValuationBand
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValuationBandImplCopyWith<_$ValuationBandImpl> get copyWith =>
      __$$ValuationBandImplCopyWithImpl<_$ValuationBandImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$ValuationBandImplToJson(this);
  }
}

abstract class _ValuationBand implements ValuationBand {
  const factory _ValuationBand({
    required final String code,
    required final String label,
    required final int amountMinor,
    final bool isPrimary,
    final String? helpText,
  }) = _$ValuationBandImpl;

  factory _ValuationBand.fromJson(Map<String, dynamic> json) =
      _$ValuationBandImpl.fromJson;

  @override
  String get code;
  @override
  String get label;
  @override
  int get amountMinor;

  /// Exactly one band is primary — the number the odometer rolls to.
  @override
  bool get isPrimary;
  @override
  String? get helpText;

  /// Create a copy of ValuationBand
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValuationBandImplCopyWith<_$ValuationBandImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

ValuationRange _$ValuationRangeFromJson(Map<String, dynamic> json) {
  return _ValuationRange.fromJson(json);
}

/// @nodoc
mixin _$ValuationRange {
  int get lowMinor => throw _privateConstructorUsedError;
  int get highMinor => throw _privateConstructorUsedError;

  /// Serializes this ValuationRange to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ValuationRange
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ValuationRangeCopyWith<ValuationRange> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ValuationRangeCopyWith<$Res> {
  factory $ValuationRangeCopyWith(
    ValuationRange value,
    $Res Function(ValuationRange) then,
  ) = _$ValuationRangeCopyWithImpl<$Res, ValuationRange>;
  @useResult
  $Res call({int lowMinor, int highMinor});
}

/// @nodoc
class _$ValuationRangeCopyWithImpl<$Res, $Val extends ValuationRange>
    implements $ValuationRangeCopyWith<$Res> {
  _$ValuationRangeCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ValuationRange
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? lowMinor = null, Object? highMinor = null}) {
    return _then(
      _value.copyWith(
            lowMinor: null == lowMinor
                ? _value.lowMinor
                : lowMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            highMinor: null == highMinor
                ? _value.highMinor
                : highMinor // ignore: cast_nullable_to_non_nullable
                      as int,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$ValuationRangeImplCopyWith<$Res>
    implements $ValuationRangeCopyWith<$Res> {
  factory _$$ValuationRangeImplCopyWith(
    _$ValuationRangeImpl value,
    $Res Function(_$ValuationRangeImpl) then,
  ) = __$$ValuationRangeImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int lowMinor, int highMinor});
}

/// @nodoc
class __$$ValuationRangeImplCopyWithImpl<$Res>
    extends _$ValuationRangeCopyWithImpl<$Res, _$ValuationRangeImpl>
    implements _$$ValuationRangeImplCopyWith<$Res> {
  __$$ValuationRangeImplCopyWithImpl(
    _$ValuationRangeImpl _value,
    $Res Function(_$ValuationRangeImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValuationRange
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? lowMinor = null, Object? highMinor = null}) {
    return _then(
      _$ValuationRangeImpl(
        lowMinor: null == lowMinor
            ? _value.lowMinor
            : lowMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        highMinor: null == highMinor
            ? _value.highMinor
            : highMinor // ignore: cast_nullable_to_non_nullable
                  as int,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValuationRangeImpl implements _ValuationRange {
  const _$ValuationRangeImpl({required this.lowMinor, required this.highMinor});

  factory _$ValuationRangeImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValuationRangeImplFromJson(json);

  @override
  final int lowMinor;
  @override
  final int highMinor;

  @override
  String toString() {
    return 'ValuationRange(lowMinor: $lowMinor, highMinor: $highMinor)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValuationRangeImpl &&
            (identical(other.lowMinor, lowMinor) ||
                other.lowMinor == lowMinor) &&
            (identical(other.highMinor, highMinor) ||
                other.highMinor == highMinor));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, lowMinor, highMinor);

  /// Create a copy of ValuationRange
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValuationRangeImplCopyWith<_$ValuationRangeImpl> get copyWith =>
      __$$ValuationRangeImplCopyWithImpl<_$ValuationRangeImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$ValuationRangeImplToJson(this);
  }
}

abstract class _ValuationRange implements ValuationRange {
  const factory _ValuationRange({
    required final int lowMinor,
    required final int highMinor,
  }) = _$ValuationRangeImpl;

  factory _ValuationRange.fromJson(Map<String, dynamic> json) =
      _$ValuationRangeImpl.fromJson;

  @override
  int get lowMinor;
  @override
  int get highMinor;

  /// Create a copy of ValuationRange
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValuationRangeImplCopyWith<_$ValuationRangeImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

ValuationConfidence _$ValuationConfidenceFromJson(Map<String, dynamic> json) {
  return _ValuationConfidence.fromJson(json);
}

/// @nodoc
mixin _$ValuationConfidence {
  double get score => throw _privateConstructorUsedError;
  @JsonKey(unknownEnumValue: ConfidenceBand.medium)
  ConfidenceBand get band => throw _privateConstructorUsedError;

  /// "412 comparable sales in Pune over 90 days".
  String? get basedOn => throw _privateConstructorUsedError;

  /// Serializes this ValuationConfidence to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ValuationConfidence
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ValuationConfidenceCopyWith<ValuationConfidence> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ValuationConfidenceCopyWith<$Res> {
  factory $ValuationConfidenceCopyWith(
    ValuationConfidence value,
    $Res Function(ValuationConfidence) then,
  ) = _$ValuationConfidenceCopyWithImpl<$Res, ValuationConfidence>;
  @useResult
  $Res call({
    double score,
    @JsonKey(unknownEnumValue: ConfidenceBand.medium) ConfidenceBand band,
    String? basedOn,
  });
}

/// @nodoc
class _$ValuationConfidenceCopyWithImpl<$Res, $Val extends ValuationConfidence>
    implements $ValuationConfidenceCopyWith<$Res> {
  _$ValuationConfidenceCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ValuationConfidence
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? score = null,
    Object? band = null,
    Object? basedOn = freezed,
  }) {
    return _then(
      _value.copyWith(
            score: null == score
                ? _value.score
                : score // ignore: cast_nullable_to_non_nullable
                      as double,
            band: null == band
                ? _value.band
                : band // ignore: cast_nullable_to_non_nullable
                      as ConfidenceBand,
            basedOn: freezed == basedOn
                ? _value.basedOn
                : basedOn // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$ValuationConfidenceImplCopyWith<$Res>
    implements $ValuationConfidenceCopyWith<$Res> {
  factory _$$ValuationConfidenceImplCopyWith(
    _$ValuationConfidenceImpl value,
    $Res Function(_$ValuationConfidenceImpl) then,
  ) = __$$ValuationConfidenceImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    double score,
    @JsonKey(unknownEnumValue: ConfidenceBand.medium) ConfidenceBand band,
    String? basedOn,
  });
}

/// @nodoc
class __$$ValuationConfidenceImplCopyWithImpl<$Res>
    extends _$ValuationConfidenceCopyWithImpl<$Res, _$ValuationConfidenceImpl>
    implements _$$ValuationConfidenceImplCopyWith<$Res> {
  __$$ValuationConfidenceImplCopyWithImpl(
    _$ValuationConfidenceImpl _value,
    $Res Function(_$ValuationConfidenceImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValuationConfidence
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? score = null,
    Object? band = null,
    Object? basedOn = freezed,
  }) {
    return _then(
      _$ValuationConfidenceImpl(
        score: null == score
            ? _value.score
            : score // ignore: cast_nullable_to_non_nullable
                  as double,
        band: null == band
            ? _value.band
            : band // ignore: cast_nullable_to_non_nullable
                  as ConfidenceBand,
        basedOn: freezed == basedOn
            ? _value.basedOn
            : basedOn // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValuationConfidenceImpl implements _ValuationConfidence {
  const _$ValuationConfidenceImpl({
    required this.score,
    @JsonKey(unknownEnumValue: ConfidenceBand.medium)
    this.band = ConfidenceBand.medium,
    this.basedOn,
  });

  factory _$ValuationConfidenceImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValuationConfidenceImplFromJson(json);

  @override
  final double score;
  @override
  @JsonKey(unknownEnumValue: ConfidenceBand.medium)
  final ConfidenceBand band;

  /// "412 comparable sales in Pune over 90 days".
  @override
  final String? basedOn;

  @override
  String toString() {
    return 'ValuationConfidence(score: $score, band: $band, basedOn: $basedOn)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValuationConfidenceImpl &&
            (identical(other.score, score) || other.score == score) &&
            (identical(other.band, band) || other.band == band) &&
            (identical(other.basedOn, basedOn) || other.basedOn == basedOn));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, score, band, basedOn);

  /// Create a copy of ValuationConfidence
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValuationConfidenceImplCopyWith<_$ValuationConfidenceImpl> get copyWith =>
      __$$ValuationConfidenceImplCopyWithImpl<_$ValuationConfidenceImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$ValuationConfidenceImplToJson(this);
  }
}

abstract class _ValuationConfidence implements ValuationConfidence {
  const factory _ValuationConfidence({
    required final double score,
    @JsonKey(unknownEnumValue: ConfidenceBand.medium) final ConfidenceBand band,
    final String? basedOn,
  }) = _$ValuationConfidenceImpl;

  factory _ValuationConfidence.fromJson(Map<String, dynamic> json) =
      _$ValuationConfidenceImpl.fromJson;

  @override
  double get score;
  @override
  @JsonKey(unknownEnumValue: ConfidenceBand.medium)
  ConfidenceBand get band;

  /// "412 comparable sales in Pune over 90 days".
  @override
  String? get basedOn;

  /// Create a copy of ValuationConfidence
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValuationConfidenceImplCopyWith<_$ValuationConfidenceImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

ValuationQuote _$ValuationQuoteFromJson(Map<String, dynamic> json) {
  return _ValuationQuote.fromJson(json);
}

/// @nodoc
mixin _$ValuationQuote {
  String get quoteId => throw _privateConstructorUsedError;
  ValuationVehicle get vehicle => throw _privateConstructorUsedError;
  String get currency => throw _privateConstructorUsedError;
  List<ValuationBand> get valuations => throw _privateConstructorUsedError;
  ValuationRange get range => throw _privateConstructorUsedError;
  ValuationConfidence get confidence => throw _privateConstructorUsedError;

  /// The "as of" calendar date.
  String get asOf => throw _privateConstructorUsedError;
  String get expiresAt => throw _privateConstructorUsedError;
  String? get modelVersion => throw _privateConstructorUsedError;

  /// Serializes this ValuationQuote to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ValuationQuote
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ValuationQuoteCopyWith<ValuationQuote> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ValuationQuoteCopyWith<$Res> {
  factory $ValuationQuoteCopyWith(
    ValuationQuote value,
    $Res Function(ValuationQuote) then,
  ) = _$ValuationQuoteCopyWithImpl<$Res, ValuationQuote>;
  @useResult
  $Res call({
    String quoteId,
    ValuationVehicle vehicle,
    String currency,
    List<ValuationBand> valuations,
    ValuationRange range,
    ValuationConfidence confidence,
    String asOf,
    String expiresAt,
    String? modelVersion,
  });

  $ValuationVehicleCopyWith<$Res> get vehicle;
  $ValuationRangeCopyWith<$Res> get range;
  $ValuationConfidenceCopyWith<$Res> get confidence;
}

/// @nodoc
class _$ValuationQuoteCopyWithImpl<$Res, $Val extends ValuationQuote>
    implements $ValuationQuoteCopyWith<$Res> {
  _$ValuationQuoteCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ValuationQuote
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? quoteId = null,
    Object? vehicle = null,
    Object? currency = null,
    Object? valuations = null,
    Object? range = null,
    Object? confidence = null,
    Object? asOf = null,
    Object? expiresAt = null,
    Object? modelVersion = freezed,
  }) {
    return _then(
      _value.copyWith(
            quoteId: null == quoteId
                ? _value.quoteId
                : quoteId // ignore: cast_nullable_to_non_nullable
                      as String,
            vehicle: null == vehicle
                ? _value.vehicle
                : vehicle // ignore: cast_nullable_to_non_nullable
                      as ValuationVehicle,
            currency: null == currency
                ? _value.currency
                : currency // ignore: cast_nullable_to_non_nullable
                      as String,
            valuations: null == valuations
                ? _value.valuations
                : valuations // ignore: cast_nullable_to_non_nullable
                      as List<ValuationBand>,
            range: null == range
                ? _value.range
                : range // ignore: cast_nullable_to_non_nullable
                      as ValuationRange,
            confidence: null == confidence
                ? _value.confidence
                : confidence // ignore: cast_nullable_to_non_nullable
                      as ValuationConfidence,
            asOf: null == asOf
                ? _value.asOf
                : asOf // ignore: cast_nullable_to_non_nullable
                      as String,
            expiresAt: null == expiresAt
                ? _value.expiresAt
                : expiresAt // ignore: cast_nullable_to_non_nullable
                      as String,
            modelVersion: freezed == modelVersion
                ? _value.modelVersion
                : modelVersion // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }

  /// Create a copy of ValuationQuote
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $ValuationVehicleCopyWith<$Res> get vehicle {
    return $ValuationVehicleCopyWith<$Res>(_value.vehicle, (value) {
      return _then(_value.copyWith(vehicle: value) as $Val);
    });
  }

  /// Create a copy of ValuationQuote
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $ValuationRangeCopyWith<$Res> get range {
    return $ValuationRangeCopyWith<$Res>(_value.range, (value) {
      return _then(_value.copyWith(range: value) as $Val);
    });
  }

  /// Create a copy of ValuationQuote
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $ValuationConfidenceCopyWith<$Res> get confidence {
    return $ValuationConfidenceCopyWith<$Res>(_value.confidence, (value) {
      return _then(_value.copyWith(confidence: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$ValuationQuoteImplCopyWith<$Res>
    implements $ValuationQuoteCopyWith<$Res> {
  factory _$$ValuationQuoteImplCopyWith(
    _$ValuationQuoteImpl value,
    $Res Function(_$ValuationQuoteImpl) then,
  ) = __$$ValuationQuoteImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String quoteId,
    ValuationVehicle vehicle,
    String currency,
    List<ValuationBand> valuations,
    ValuationRange range,
    ValuationConfidence confidence,
    String asOf,
    String expiresAt,
    String? modelVersion,
  });

  @override
  $ValuationVehicleCopyWith<$Res> get vehicle;
  @override
  $ValuationRangeCopyWith<$Res> get range;
  @override
  $ValuationConfidenceCopyWith<$Res> get confidence;
}

/// @nodoc
class __$$ValuationQuoteImplCopyWithImpl<$Res>
    extends _$ValuationQuoteCopyWithImpl<$Res, _$ValuationQuoteImpl>
    implements _$$ValuationQuoteImplCopyWith<$Res> {
  __$$ValuationQuoteImplCopyWithImpl(
    _$ValuationQuoteImpl _value,
    $Res Function(_$ValuationQuoteImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ValuationQuote
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? quoteId = null,
    Object? vehicle = null,
    Object? currency = null,
    Object? valuations = null,
    Object? range = null,
    Object? confidence = null,
    Object? asOf = null,
    Object? expiresAt = null,
    Object? modelVersion = freezed,
  }) {
    return _then(
      _$ValuationQuoteImpl(
        quoteId: null == quoteId
            ? _value.quoteId
            : quoteId // ignore: cast_nullable_to_non_nullable
                  as String,
        vehicle: null == vehicle
            ? _value.vehicle
            : vehicle // ignore: cast_nullable_to_non_nullable
                  as ValuationVehicle,
        currency: null == currency
            ? _value.currency
            : currency // ignore: cast_nullable_to_non_nullable
                  as String,
        valuations: null == valuations
            ? _value._valuations
            : valuations // ignore: cast_nullable_to_non_nullable
                  as List<ValuationBand>,
        range: null == range
            ? _value.range
            : range // ignore: cast_nullable_to_non_nullable
                  as ValuationRange,
        confidence: null == confidence
            ? _value.confidence
            : confidence // ignore: cast_nullable_to_non_nullable
                  as ValuationConfidence,
        asOf: null == asOf
            ? _value.asOf
            : asOf // ignore: cast_nullable_to_non_nullable
                  as String,
        expiresAt: null == expiresAt
            ? _value.expiresAt
            : expiresAt // ignore: cast_nullable_to_non_nullable
                  as String,
        modelVersion: freezed == modelVersion
            ? _value.modelVersion
            : modelVersion // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ValuationQuoteImpl extends _ValuationQuote {
  const _$ValuationQuoteImpl({
    required this.quoteId,
    required this.vehicle,
    this.currency = 'INR',
    final List<ValuationBand> valuations = const <ValuationBand>[],
    required this.range,
    required this.confidence,
    required this.asOf,
    required this.expiresAt,
    this.modelVersion,
  }) : _valuations = valuations,
       super._();

  factory _$ValuationQuoteImpl.fromJson(Map<String, dynamic> json) =>
      _$$ValuationQuoteImplFromJson(json);

  @override
  final String quoteId;
  @override
  final ValuationVehicle vehicle;
  @override
  @JsonKey()
  final String currency;
  final List<ValuationBand> _valuations;
  @override
  @JsonKey()
  List<ValuationBand> get valuations {
    if (_valuations is EqualUnmodifiableListView) return _valuations;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_valuations);
  }

  @override
  final ValuationRange range;
  @override
  final ValuationConfidence confidence;

  /// The "as of" calendar date.
  @override
  final String asOf;
  @override
  final String expiresAt;
  @override
  final String? modelVersion;

  @override
  String toString() {
    return 'ValuationQuote(quoteId: $quoteId, vehicle: $vehicle, currency: $currency, valuations: $valuations, range: $range, confidence: $confidence, asOf: $asOf, expiresAt: $expiresAt, modelVersion: $modelVersion)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValuationQuoteImpl &&
            (identical(other.quoteId, quoteId) || other.quoteId == quoteId) &&
            (identical(other.vehicle, vehicle) || other.vehicle == vehicle) &&
            (identical(other.currency, currency) ||
                other.currency == currency) &&
            const DeepCollectionEquality().equals(
              other._valuations,
              _valuations,
            ) &&
            (identical(other.range, range) || other.range == range) &&
            (identical(other.confidence, confidence) ||
                other.confidence == confidence) &&
            (identical(other.asOf, asOf) || other.asOf == asOf) &&
            (identical(other.expiresAt, expiresAt) ||
                other.expiresAt == expiresAt) &&
            (identical(other.modelVersion, modelVersion) ||
                other.modelVersion == modelVersion));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    quoteId,
    vehicle,
    currency,
    const DeepCollectionEquality().hash(_valuations),
    range,
    confidence,
    asOf,
    expiresAt,
    modelVersion,
  );

  /// Create a copy of ValuationQuote
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValuationQuoteImplCopyWith<_$ValuationQuoteImpl> get copyWith =>
      __$$ValuationQuoteImplCopyWithImpl<_$ValuationQuoteImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$ValuationQuoteImplToJson(this);
  }
}

abstract class _ValuationQuote extends ValuationQuote {
  const factory _ValuationQuote({
    required final String quoteId,
    required final ValuationVehicle vehicle,
    final String currency,
    final List<ValuationBand> valuations,
    required final ValuationRange range,
    required final ValuationConfidence confidence,
    required final String asOf,
    required final String expiresAt,
    final String? modelVersion,
  }) = _$ValuationQuoteImpl;
  const _ValuationQuote._() : super._();

  factory _ValuationQuote.fromJson(Map<String, dynamic> json) =
      _$ValuationQuoteImpl.fromJson;

  @override
  String get quoteId;
  @override
  ValuationVehicle get vehicle;
  @override
  String get currency;
  @override
  List<ValuationBand> get valuations;
  @override
  ValuationRange get range;
  @override
  ValuationConfidence get confidence;

  /// The "as of" calendar date.
  @override
  String get asOf;
  @override
  String get expiresAt;
  @override
  String? get modelVersion;

  /// Create a copy of ValuationQuote
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValuationQuoteImplCopyWith<_$ValuationQuoteImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
