// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'platform.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

Features _$FeaturesFromJson(Map<String, dynamic> json) {
  return _Features.fromJson(json);
}

/// @nodoc
mixin _$Features {
  int get version => throw _privateConstructorUsedError;

  /// `auction`, `lease`, `attachment` — whole modules, on or off for this user in this city.
  Map<String, bool> get modules => throw _privateConstructorUsedError;

  /// `instant_offer`, `ai_damage_detection`, `whatsapp_chat` — behaviour inside a module.
  Map<String, bool> get flags => throw _privateConstructorUsedError;

  /// Serializes this Features to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of Features
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $FeaturesCopyWith<Features> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $FeaturesCopyWith<$Res> {
  factory $FeaturesCopyWith(Features value, $Res Function(Features) then) =
      _$FeaturesCopyWithImpl<$Res, Features>;
  @useResult
  $Res call({int version, Map<String, bool> modules, Map<String, bool> flags});
}

/// @nodoc
class _$FeaturesCopyWithImpl<$Res, $Val extends Features>
    implements $FeaturesCopyWith<$Res> {
  _$FeaturesCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of Features
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? version = null,
    Object? modules = null,
    Object? flags = null,
  }) {
    return _then(
      _value.copyWith(
            version: null == version
                ? _value.version
                : version // ignore: cast_nullable_to_non_nullable
                      as int,
            modules: null == modules
                ? _value.modules
                : modules // ignore: cast_nullable_to_non_nullable
                      as Map<String, bool>,
            flags: null == flags
                ? _value.flags
                : flags // ignore: cast_nullable_to_non_nullable
                      as Map<String, bool>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$FeaturesImplCopyWith<$Res>
    implements $FeaturesCopyWith<$Res> {
  factory _$$FeaturesImplCopyWith(
    _$FeaturesImpl value,
    $Res Function(_$FeaturesImpl) then,
  ) = __$$FeaturesImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int version, Map<String, bool> modules, Map<String, bool> flags});
}

/// @nodoc
class __$$FeaturesImplCopyWithImpl<$Res>
    extends _$FeaturesCopyWithImpl<$Res, _$FeaturesImpl>
    implements _$$FeaturesImplCopyWith<$Res> {
  __$$FeaturesImplCopyWithImpl(
    _$FeaturesImpl _value,
    $Res Function(_$FeaturesImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of Features
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? version = null,
    Object? modules = null,
    Object? flags = null,
  }) {
    return _then(
      _$FeaturesImpl(
        version: null == version
            ? _value.version
            : version // ignore: cast_nullable_to_non_nullable
                  as int,
        modules: null == modules
            ? _value._modules
            : modules // ignore: cast_nullable_to_non_nullable
                  as Map<String, bool>,
        flags: null == flags
            ? _value._flags
            : flags // ignore: cast_nullable_to_non_nullable
                  as Map<String, bool>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$FeaturesImpl extends _Features {
  const _$FeaturesImpl({
    required this.version,
    final Map<String, bool> modules = const <String, bool>{},
    final Map<String, bool> flags = const <String, bool>{},
  }) : _modules = modules,
       _flags = flags,
       super._();

  factory _$FeaturesImpl.fromJson(Map<String, dynamic> json) =>
      _$$FeaturesImplFromJson(json);

  @override
  final int version;

  /// `auction`, `lease`, `attachment` — whole modules, on or off for this user in this city.
  final Map<String, bool> _modules;

  /// `auction`, `lease`, `attachment` — whole modules, on or off for this user in this city.
  @override
  @JsonKey()
  Map<String, bool> get modules {
    if (_modules is EqualUnmodifiableMapView) return _modules;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableMapView(_modules);
  }

  /// `instant_offer`, `ai_damage_detection`, `whatsapp_chat` — behaviour inside a module.
  final Map<String, bool> _flags;

  /// `instant_offer`, `ai_damage_detection`, `whatsapp_chat` — behaviour inside a module.
  @override
  @JsonKey()
  Map<String, bool> get flags {
    if (_flags is EqualUnmodifiableMapView) return _flags;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableMapView(_flags);
  }

  @override
  String toString() {
    return 'Features(version: $version, modules: $modules, flags: $flags)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$FeaturesImpl &&
            (identical(other.version, version) || other.version == version) &&
            const DeepCollectionEquality().equals(other._modules, _modules) &&
            const DeepCollectionEquality().equals(other._flags, _flags));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    version,
    const DeepCollectionEquality().hash(_modules),
    const DeepCollectionEquality().hash(_flags),
  );

  /// Create a copy of Features
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$FeaturesImplCopyWith<_$FeaturesImpl> get copyWith =>
      __$$FeaturesImplCopyWithImpl<_$FeaturesImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$FeaturesImplToJson(this);
  }
}

abstract class _Features extends Features {
  const factory _Features({
    required final int version,
    final Map<String, bool> modules,
    final Map<String, bool> flags,
  }) = _$FeaturesImpl;
  const _Features._() : super._();

  factory _Features.fromJson(Map<String, dynamic> json) =
      _$FeaturesImpl.fromJson;

  @override
  int get version;

  /// `auction`, `lease`, `attachment` — whole modules, on or off for this user in this city.
  @override
  Map<String, bool> get modules;

  /// `instant_offer`, `ai_damage_detection`, `whatsapp_chat` — behaviour inside a module.
  @override
  Map<String, bool> get flags;

  /// Create a copy of Features
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$FeaturesImplCopyWith<_$FeaturesImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

I18nBundle _$I18nBundleFromJson(Map<String, dynamic> json) {
  return _I18nBundle.fromJson(json);
}

/// @nodoc
mixin _$I18nBundle {
  String get locale => throw _privateConstructorUsedError;
  int get version => throw _privateConstructorUsedError;
  Map<String, String> get resources => throw _privateConstructorUsedError;

  /// Serializes this I18nBundle to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of I18nBundle
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $I18nBundleCopyWith<I18nBundle> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $I18nBundleCopyWith<$Res> {
  factory $I18nBundleCopyWith(
    I18nBundle value,
    $Res Function(I18nBundle) then,
  ) = _$I18nBundleCopyWithImpl<$Res, I18nBundle>;
  @useResult
  $Res call({String locale, int version, Map<String, String> resources});
}

/// @nodoc
class _$I18nBundleCopyWithImpl<$Res, $Val extends I18nBundle>
    implements $I18nBundleCopyWith<$Res> {
  _$I18nBundleCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of I18nBundle
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? locale = null,
    Object? version = null,
    Object? resources = null,
  }) {
    return _then(
      _value.copyWith(
            locale: null == locale
                ? _value.locale
                : locale // ignore: cast_nullable_to_non_nullable
                      as String,
            version: null == version
                ? _value.version
                : version // ignore: cast_nullable_to_non_nullable
                      as int,
            resources: null == resources
                ? _value.resources
                : resources // ignore: cast_nullable_to_non_nullable
                      as Map<String, String>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$I18nBundleImplCopyWith<$Res>
    implements $I18nBundleCopyWith<$Res> {
  factory _$$I18nBundleImplCopyWith(
    _$I18nBundleImpl value,
    $Res Function(_$I18nBundleImpl) then,
  ) = __$$I18nBundleImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String locale, int version, Map<String, String> resources});
}

/// @nodoc
class __$$I18nBundleImplCopyWithImpl<$Res>
    extends _$I18nBundleCopyWithImpl<$Res, _$I18nBundleImpl>
    implements _$$I18nBundleImplCopyWith<$Res> {
  __$$I18nBundleImplCopyWithImpl(
    _$I18nBundleImpl _value,
    $Res Function(_$I18nBundleImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of I18nBundle
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? locale = null,
    Object? version = null,
    Object? resources = null,
  }) {
    return _then(
      _$I18nBundleImpl(
        locale: null == locale
            ? _value.locale
            : locale // ignore: cast_nullable_to_non_nullable
                  as String,
        version: null == version
            ? _value.version
            : version // ignore: cast_nullable_to_non_nullable
                  as int,
        resources: null == resources
            ? _value._resources
            : resources // ignore: cast_nullable_to_non_nullable
                  as Map<String, String>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$I18nBundleImpl extends _I18nBundle {
  const _$I18nBundleImpl({
    required this.locale,
    required this.version,
    final Map<String, String> resources = const <String, String>{},
  }) : _resources = resources,
       super._();

  factory _$I18nBundleImpl.fromJson(Map<String, dynamic> json) =>
      _$$I18nBundleImplFromJson(json);

  @override
  final String locale;
  @override
  final int version;
  final Map<String, String> _resources;
  @override
  @JsonKey()
  Map<String, String> get resources {
    if (_resources is EqualUnmodifiableMapView) return _resources;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableMapView(_resources);
  }

  @override
  String toString() {
    return 'I18nBundle(locale: $locale, version: $version, resources: $resources)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$I18nBundleImpl &&
            (identical(other.locale, locale) || other.locale == locale) &&
            (identical(other.version, version) || other.version == version) &&
            const DeepCollectionEquality().equals(
              other._resources,
              _resources,
            ));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    locale,
    version,
    const DeepCollectionEquality().hash(_resources),
  );

  /// Create a copy of I18nBundle
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$I18nBundleImplCopyWith<_$I18nBundleImpl> get copyWith =>
      __$$I18nBundleImplCopyWithImpl<_$I18nBundleImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$I18nBundleImplToJson(this);
  }
}

abstract class _I18nBundle extends I18nBundle {
  const factory _I18nBundle({
    required final String locale,
    required final int version,
    final Map<String, String> resources,
  }) = _$I18nBundleImpl;
  const _I18nBundle._() : super._();

  factory _I18nBundle.fromJson(Map<String, dynamic> json) =
      _$I18nBundleImpl.fromJson;

  @override
  String get locale;
  @override
  int get version;
  @override
  Map<String, String> get resources;

  /// Create a copy of I18nBundle
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$I18nBundleImplCopyWith<_$I18nBundleImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

City _$CityFromJson(Map<String, dynamic> json) {
  return _City.fromJson(json);
}

/// @nodoc
mixin _$City {
  String get code => throw _privateConstructorUsedError;
  String get name => throw _privateConstructorUsedError;
  String get stateCode => throw _privateConstructorUsedError;
  double? get lat => throw _privateConstructorUsedError;
  double? get lng => throw _privateConstructorUsedError;
  bool get serviceable => throw _privateConstructorUsedError;

  /// Which modules are live here. Lease in Bengaluru only is a registry row, not a deploy.
  List<String> get modules => throw _privateConstructorUsedError;

  /// Serializes this City to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of City
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $CityCopyWith<City> get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $CityCopyWith<$Res> {
  factory $CityCopyWith(City value, $Res Function(City) then) =
      _$CityCopyWithImpl<$Res, City>;
  @useResult
  $Res call({
    String code,
    String name,
    String stateCode,
    double? lat,
    double? lng,
    bool serviceable,
    List<String> modules,
  });
}

/// @nodoc
class _$CityCopyWithImpl<$Res, $Val extends City>
    implements $CityCopyWith<$Res> {
  _$CityCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of City
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? name = null,
    Object? stateCode = null,
    Object? lat = freezed,
    Object? lng = freezed,
    Object? serviceable = null,
    Object? modules = null,
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
            stateCode: null == stateCode
                ? _value.stateCode
                : stateCode // ignore: cast_nullable_to_non_nullable
                      as String,
            lat: freezed == lat
                ? _value.lat
                : lat // ignore: cast_nullable_to_non_nullable
                      as double?,
            lng: freezed == lng
                ? _value.lng
                : lng // ignore: cast_nullable_to_non_nullable
                      as double?,
            serviceable: null == serviceable
                ? _value.serviceable
                : serviceable // ignore: cast_nullable_to_non_nullable
                      as bool,
            modules: null == modules
                ? _value.modules
                : modules // ignore: cast_nullable_to_non_nullable
                      as List<String>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$CityImplCopyWith<$Res> implements $CityCopyWith<$Res> {
  factory _$$CityImplCopyWith(
    _$CityImpl value,
    $Res Function(_$CityImpl) then,
  ) = __$$CityImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String code,
    String name,
    String stateCode,
    double? lat,
    double? lng,
    bool serviceable,
    List<String> modules,
  });
}

/// @nodoc
class __$$CityImplCopyWithImpl<$Res>
    extends _$CityCopyWithImpl<$Res, _$CityImpl>
    implements _$$CityImplCopyWith<$Res> {
  __$$CityImplCopyWithImpl(_$CityImpl _value, $Res Function(_$CityImpl) _then)
    : super(_value, _then);

  /// Create a copy of City
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? name = null,
    Object? stateCode = null,
    Object? lat = freezed,
    Object? lng = freezed,
    Object? serviceable = null,
    Object? modules = null,
  }) {
    return _then(
      _$CityImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        name: null == name
            ? _value.name
            : name // ignore: cast_nullable_to_non_nullable
                  as String,
        stateCode: null == stateCode
            ? _value.stateCode
            : stateCode // ignore: cast_nullable_to_non_nullable
                  as String,
        lat: freezed == lat
            ? _value.lat
            : lat // ignore: cast_nullable_to_non_nullable
                  as double?,
        lng: freezed == lng
            ? _value.lng
            : lng // ignore: cast_nullable_to_non_nullable
                  as double?,
        serviceable: null == serviceable
            ? _value.serviceable
            : serviceable // ignore: cast_nullable_to_non_nullable
                  as bool,
        modules: null == modules
            ? _value._modules
            : modules // ignore: cast_nullable_to_non_nullable
                  as List<String>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$CityImpl implements _City {
  const _$CityImpl({
    required this.code,
    required this.name,
    required this.stateCode,
    this.lat,
    this.lng,
    this.serviceable = false,
    final List<String> modules = const <String>[],
  }) : _modules = modules;

  factory _$CityImpl.fromJson(Map<String, dynamic> json) =>
      _$$CityImplFromJson(json);

  @override
  final String code;
  @override
  final String name;
  @override
  final String stateCode;
  @override
  final double? lat;
  @override
  final double? lng;
  @override
  @JsonKey()
  final bool serviceable;

  /// Which modules are live here. Lease in Bengaluru only is a registry row, not a deploy.
  final List<String> _modules;

  /// Which modules are live here. Lease in Bengaluru only is a registry row, not a deploy.
  @override
  @JsonKey()
  List<String> get modules {
    if (_modules is EqualUnmodifiableListView) return _modules;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_modules);
  }

  @override
  String toString() {
    return 'City(code: $code, name: $name, stateCode: $stateCode, lat: $lat, lng: $lng, serviceable: $serviceable, modules: $modules)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$CityImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.name, name) || other.name == name) &&
            (identical(other.stateCode, stateCode) ||
                other.stateCode == stateCode) &&
            (identical(other.lat, lat) || other.lat == lat) &&
            (identical(other.lng, lng) || other.lng == lng) &&
            (identical(other.serviceable, serviceable) ||
                other.serviceable == serviceable) &&
            const DeepCollectionEquality().equals(other._modules, _modules));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    name,
    stateCode,
    lat,
    lng,
    serviceable,
    const DeepCollectionEquality().hash(_modules),
  );

  /// Create a copy of City
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$CityImplCopyWith<_$CityImpl> get copyWith =>
      __$$CityImplCopyWithImpl<_$CityImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$CityImplToJson(this);
  }
}

abstract class _City implements City {
  const factory _City({
    required final String code,
    required final String name,
    required final String stateCode,
    final double? lat,
    final double? lng,
    final bool serviceable,
    final List<String> modules,
  }) = _$CityImpl;

  factory _City.fromJson(Map<String, dynamic> json) = _$CityImpl.fromJson;

  @override
  String get code;
  @override
  String get name;
  @override
  String get stateCode;
  @override
  double? get lat;
  @override
  double? get lng;
  @override
  bool get serviceable;

  /// Which modules are live here. Lease in Bengaluru only is a registry row, not a deploy.
  @override
  List<String> get modules;

  /// Create a copy of City
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$CityImplCopyWith<_$CityImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

CitiesResponse _$CitiesResponseFromJson(Map<String, dynamic> json) {
  return _CitiesResponse.fromJson(json);
}

/// @nodoc
mixin _$CitiesResponse {
  int get version => throw _privateConstructorUsedError;
  List<City> get items => throw _privateConstructorUsedError;

  /// Serializes this CitiesResponse to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of CitiesResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $CitiesResponseCopyWith<CitiesResponse> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $CitiesResponseCopyWith<$Res> {
  factory $CitiesResponseCopyWith(
    CitiesResponse value,
    $Res Function(CitiesResponse) then,
  ) = _$CitiesResponseCopyWithImpl<$Res, CitiesResponse>;
  @useResult
  $Res call({int version, List<City> items});
}

/// @nodoc
class _$CitiesResponseCopyWithImpl<$Res, $Val extends CitiesResponse>
    implements $CitiesResponseCopyWith<$Res> {
  _$CitiesResponseCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of CitiesResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? version = null, Object? items = null}) {
    return _then(
      _value.copyWith(
            version: null == version
                ? _value.version
                : version // ignore: cast_nullable_to_non_nullable
                      as int,
            items: null == items
                ? _value.items
                : items // ignore: cast_nullable_to_non_nullable
                      as List<City>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$CitiesResponseImplCopyWith<$Res>
    implements $CitiesResponseCopyWith<$Res> {
  factory _$$CitiesResponseImplCopyWith(
    _$CitiesResponseImpl value,
    $Res Function(_$CitiesResponseImpl) then,
  ) = __$$CitiesResponseImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int version, List<City> items});
}

/// @nodoc
class __$$CitiesResponseImplCopyWithImpl<$Res>
    extends _$CitiesResponseCopyWithImpl<$Res, _$CitiesResponseImpl>
    implements _$$CitiesResponseImplCopyWith<$Res> {
  __$$CitiesResponseImplCopyWithImpl(
    _$CitiesResponseImpl _value,
    $Res Function(_$CitiesResponseImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of CitiesResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? version = null, Object? items = null}) {
    return _then(
      _$CitiesResponseImpl(
        version: null == version
            ? _value.version
            : version // ignore: cast_nullable_to_non_nullable
                  as int,
        items: null == items
            ? _value._items
            : items // ignore: cast_nullable_to_non_nullable
                  as List<City>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$CitiesResponseImpl implements _CitiesResponse {
  const _$CitiesResponseImpl({
    required this.version,
    final List<City> items = const <City>[],
  }) : _items = items;

  factory _$CitiesResponseImpl.fromJson(Map<String, dynamic> json) =>
      _$$CitiesResponseImplFromJson(json);

  @override
  final int version;
  final List<City> _items;
  @override
  @JsonKey()
  List<City> get items {
    if (_items is EqualUnmodifiableListView) return _items;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_items);
  }

  @override
  String toString() {
    return 'CitiesResponse(version: $version, items: $items)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$CitiesResponseImpl &&
            (identical(other.version, version) || other.version == version) &&
            const DeepCollectionEquality().equals(other._items, _items));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    version,
    const DeepCollectionEquality().hash(_items),
  );

  /// Create a copy of CitiesResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$CitiesResponseImplCopyWith<_$CitiesResponseImpl> get copyWith =>
      __$$CitiesResponseImplCopyWithImpl<_$CitiesResponseImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$CitiesResponseImplToJson(this);
  }
}

abstract class _CitiesResponse implements CitiesResponse {
  const factory _CitiesResponse({
    required final int version,
    final List<City> items,
  }) = _$CitiesResponseImpl;

  factory _CitiesResponse.fromJson(Map<String, dynamic> json) =
      _$CitiesResponseImpl.fromJson;

  @override
  int get version;
  @override
  List<City> get items;

  /// Create a copy of CitiesResponse
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$CitiesResponseImplCopyWith<_$CitiesResponseImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
