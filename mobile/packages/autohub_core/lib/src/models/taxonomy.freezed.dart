// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'taxonomy.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

AttributeRange _$AttributeRangeFromJson(Map<String, dynamic> json) {
  return _AttributeRange.fromJson(json);
}

/// @nodoc
mixin _$AttributeRange {
  double get min => throw _privateConstructorUsedError;
  double get max => throw _privateConstructorUsedError;
  double? get step => throw _privateConstructorUsedError;

  /// Serializes this AttributeRange to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of AttributeRange
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $AttributeRangeCopyWith<AttributeRange> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $AttributeRangeCopyWith<$Res> {
  factory $AttributeRangeCopyWith(
    AttributeRange value,
    $Res Function(AttributeRange) then,
  ) = _$AttributeRangeCopyWithImpl<$Res, AttributeRange>;
  @useResult
  $Res call({double min, double max, double? step});
}

/// @nodoc
class _$AttributeRangeCopyWithImpl<$Res, $Val extends AttributeRange>
    implements $AttributeRangeCopyWith<$Res> {
  _$AttributeRangeCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of AttributeRange
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? min = null, Object? max = null, Object? step = freezed}) {
    return _then(
      _value.copyWith(
            min: null == min
                ? _value.min
                : min // ignore: cast_nullable_to_non_nullable
                      as double,
            max: null == max
                ? _value.max
                : max // ignore: cast_nullable_to_non_nullable
                      as double,
            step: freezed == step
                ? _value.step
                : step // ignore: cast_nullable_to_non_nullable
                      as double?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$AttributeRangeImplCopyWith<$Res>
    implements $AttributeRangeCopyWith<$Res> {
  factory _$$AttributeRangeImplCopyWith(
    _$AttributeRangeImpl value,
    $Res Function(_$AttributeRangeImpl) then,
  ) = __$$AttributeRangeImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({double min, double max, double? step});
}

/// @nodoc
class __$$AttributeRangeImplCopyWithImpl<$Res>
    extends _$AttributeRangeCopyWithImpl<$Res, _$AttributeRangeImpl>
    implements _$$AttributeRangeImplCopyWith<$Res> {
  __$$AttributeRangeImplCopyWithImpl(
    _$AttributeRangeImpl _value,
    $Res Function(_$AttributeRangeImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AttributeRange
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? min = null, Object? max = null, Object? step = freezed}) {
    return _then(
      _$AttributeRangeImpl(
        min: null == min
            ? _value.min
            : min // ignore: cast_nullable_to_non_nullable
                  as double,
        max: null == max
            ? _value.max
            : max // ignore: cast_nullable_to_non_nullable
                  as double,
        step: freezed == step
            ? _value.step
            : step // ignore: cast_nullable_to_non_nullable
                  as double?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$AttributeRangeImpl implements _AttributeRange {
  const _$AttributeRangeImpl({required this.min, required this.max, this.step});

  factory _$AttributeRangeImpl.fromJson(Map<String, dynamic> json) =>
      _$$AttributeRangeImplFromJson(json);

  @override
  final double min;
  @override
  final double max;
  @override
  final double? step;

  @override
  String toString() {
    return 'AttributeRange(min: $min, max: $max, step: $step)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AttributeRangeImpl &&
            (identical(other.min, min) || other.min == min) &&
            (identical(other.max, max) || other.max == max) &&
            (identical(other.step, step) || other.step == step));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, min, max, step);

  /// Create a copy of AttributeRange
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AttributeRangeImplCopyWith<_$AttributeRangeImpl> get copyWith =>
      __$$AttributeRangeImplCopyWithImpl<_$AttributeRangeImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$AttributeRangeImplToJson(this);
  }
}

abstract class _AttributeRange implements AttributeRange {
  const factory _AttributeRange({
    required final double min,
    required final double max,
    final double? step,
  }) = _$AttributeRangeImpl;

  factory _AttributeRange.fromJson(Map<String, dynamic> json) =
      _$AttributeRangeImpl.fromJson;

  @override
  double get min;
  @override
  double get max;
  @override
  double? get step;

  /// Create a copy of AttributeRange
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AttributeRangeImplCopyWith<_$AttributeRangeImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

TaxonomyAttribute _$TaxonomyAttributeFromJson(Map<String, dynamic> json) {
  return _TaxonomyAttribute.fromJson(json);
}

/// @nodoc
mixin _$TaxonomyAttribute {
  String get code => throw _privateConstructorUsedError;
  String get label => throw _privateConstructorUsedError;
  @JsonKey(unknownEnumValue: AttributeDataType.string)
  AttributeDataType get dataType => throw _privateConstructorUsedError;

  /// `km`, `%`, `kg`, `bhp` — rendered beside the value, never folded into the label, because
  /// the same attribute on a commercial category may be declared in a different unit tomorrow.
  String? get unit => throw _privateConstructorUsedError;
  @JsonKey(name: 'required')
  bool get isRequired => throw _privateConstructorUsedError;

  /// True when the attribute is a search facet.
  bool get searchable => throw _privateConstructorUsedError;
  @JsonKey(unknownEnumValue: FacetStyle.list)
  FacetStyle? get facetStyle => throw _privateConstructorUsedError;

  /// Groups spec rows on the detail page: `engine`, `usage`, `documents`.
  String? get displayGroup => throw _privateConstructorUsedError;
  int get sortOrder => throw _privateConstructorUsedError;
  List<Option>? get options => throw _privateConstructorUsedError;
  AttributeRange? get range => throw _privateConstructorUsedError;
  String? get helpText => throw _privateConstructorUsedError;

  /// Serializes this TaxonomyAttribute to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of TaxonomyAttribute
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $TaxonomyAttributeCopyWith<TaxonomyAttribute> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $TaxonomyAttributeCopyWith<$Res> {
  factory $TaxonomyAttributeCopyWith(
    TaxonomyAttribute value,
    $Res Function(TaxonomyAttribute) then,
  ) = _$TaxonomyAttributeCopyWithImpl<$Res, TaxonomyAttribute>;
  @useResult
  $Res call({
    String code,
    String label,
    @JsonKey(unknownEnumValue: AttributeDataType.string)
    AttributeDataType dataType,
    String? unit,
    @JsonKey(name: 'required') bool isRequired,
    bool searchable,
    @JsonKey(unknownEnumValue: FacetStyle.list) FacetStyle? facetStyle,
    String? displayGroup,
    int sortOrder,
    List<Option>? options,
    AttributeRange? range,
    String? helpText,
  });

  $AttributeRangeCopyWith<$Res>? get range;
}

/// @nodoc
class _$TaxonomyAttributeCopyWithImpl<$Res, $Val extends TaxonomyAttribute>
    implements $TaxonomyAttributeCopyWith<$Res> {
  _$TaxonomyAttributeCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of TaxonomyAttribute
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? dataType = null,
    Object? unit = freezed,
    Object? isRequired = null,
    Object? searchable = null,
    Object? facetStyle = freezed,
    Object? displayGroup = freezed,
    Object? sortOrder = null,
    Object? options = freezed,
    Object? range = freezed,
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
            dataType: null == dataType
                ? _value.dataType
                : dataType // ignore: cast_nullable_to_non_nullable
                      as AttributeDataType,
            unit: freezed == unit
                ? _value.unit
                : unit // ignore: cast_nullable_to_non_nullable
                      as String?,
            isRequired: null == isRequired
                ? _value.isRequired
                : isRequired // ignore: cast_nullable_to_non_nullable
                      as bool,
            searchable: null == searchable
                ? _value.searchable
                : searchable // ignore: cast_nullable_to_non_nullable
                      as bool,
            facetStyle: freezed == facetStyle
                ? _value.facetStyle
                : facetStyle // ignore: cast_nullable_to_non_nullable
                      as FacetStyle?,
            displayGroup: freezed == displayGroup
                ? _value.displayGroup
                : displayGroup // ignore: cast_nullable_to_non_nullable
                      as String?,
            sortOrder: null == sortOrder
                ? _value.sortOrder
                : sortOrder // ignore: cast_nullable_to_non_nullable
                      as int,
            options: freezed == options
                ? _value.options
                : options // ignore: cast_nullable_to_non_nullable
                      as List<Option>?,
            range: freezed == range
                ? _value.range
                : range // ignore: cast_nullable_to_non_nullable
                      as AttributeRange?,
            helpText: freezed == helpText
                ? _value.helpText
                : helpText // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }

  /// Create a copy of TaxonomyAttribute
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $AttributeRangeCopyWith<$Res>? get range {
    if (_value.range == null) {
      return null;
    }

    return $AttributeRangeCopyWith<$Res>(_value.range!, (value) {
      return _then(_value.copyWith(range: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$TaxonomyAttributeImplCopyWith<$Res>
    implements $TaxonomyAttributeCopyWith<$Res> {
  factory _$$TaxonomyAttributeImplCopyWith(
    _$TaxonomyAttributeImpl value,
    $Res Function(_$TaxonomyAttributeImpl) then,
  ) = __$$TaxonomyAttributeImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String code,
    String label,
    @JsonKey(unknownEnumValue: AttributeDataType.string)
    AttributeDataType dataType,
    String? unit,
    @JsonKey(name: 'required') bool isRequired,
    bool searchable,
    @JsonKey(unknownEnumValue: FacetStyle.list) FacetStyle? facetStyle,
    String? displayGroup,
    int sortOrder,
    List<Option>? options,
    AttributeRange? range,
    String? helpText,
  });

  @override
  $AttributeRangeCopyWith<$Res>? get range;
}

/// @nodoc
class __$$TaxonomyAttributeImplCopyWithImpl<$Res>
    extends _$TaxonomyAttributeCopyWithImpl<$Res, _$TaxonomyAttributeImpl>
    implements _$$TaxonomyAttributeImplCopyWith<$Res> {
  __$$TaxonomyAttributeImplCopyWithImpl(
    _$TaxonomyAttributeImpl _value,
    $Res Function(_$TaxonomyAttributeImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of TaxonomyAttribute
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? dataType = null,
    Object? unit = freezed,
    Object? isRequired = null,
    Object? searchable = null,
    Object? facetStyle = freezed,
    Object? displayGroup = freezed,
    Object? sortOrder = null,
    Object? options = freezed,
    Object? range = freezed,
    Object? helpText = freezed,
  }) {
    return _then(
      _$TaxonomyAttributeImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        dataType: null == dataType
            ? _value.dataType
            : dataType // ignore: cast_nullable_to_non_nullable
                  as AttributeDataType,
        unit: freezed == unit
            ? _value.unit
            : unit // ignore: cast_nullable_to_non_nullable
                  as String?,
        isRequired: null == isRequired
            ? _value.isRequired
            : isRequired // ignore: cast_nullable_to_non_nullable
                  as bool,
        searchable: null == searchable
            ? _value.searchable
            : searchable // ignore: cast_nullable_to_non_nullable
                  as bool,
        facetStyle: freezed == facetStyle
            ? _value.facetStyle
            : facetStyle // ignore: cast_nullable_to_non_nullable
                  as FacetStyle?,
        displayGroup: freezed == displayGroup
            ? _value.displayGroup
            : displayGroup // ignore: cast_nullable_to_non_nullable
                  as String?,
        sortOrder: null == sortOrder
            ? _value.sortOrder
            : sortOrder // ignore: cast_nullable_to_non_nullable
                  as int,
        options: freezed == options
            ? _value._options
            : options // ignore: cast_nullable_to_non_nullable
                  as List<Option>?,
        range: freezed == range
            ? _value.range
            : range // ignore: cast_nullable_to_non_nullable
                  as AttributeRange?,
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
class _$TaxonomyAttributeImpl implements _TaxonomyAttribute {
  const _$TaxonomyAttributeImpl({
    required this.code,
    required this.label,
    @JsonKey(unknownEnumValue: AttributeDataType.string)
    this.dataType = AttributeDataType.string,
    this.unit,
    @JsonKey(name: 'required') this.isRequired = false,
    this.searchable = false,
    @JsonKey(unknownEnumValue: FacetStyle.list) this.facetStyle,
    this.displayGroup,
    this.sortOrder = 0,
    final List<Option>? options,
    this.range,
    this.helpText,
  }) : _options = options;

  factory _$TaxonomyAttributeImpl.fromJson(Map<String, dynamic> json) =>
      _$$TaxonomyAttributeImplFromJson(json);

  @override
  final String code;
  @override
  final String label;
  @override
  @JsonKey(unknownEnumValue: AttributeDataType.string)
  final AttributeDataType dataType;

  /// `km`, `%`, `kg`, `bhp` — rendered beside the value, never folded into the label, because
  /// the same attribute on a commercial category may be declared in a different unit tomorrow.
  @override
  final String? unit;
  @override
  @JsonKey(name: 'required')
  final bool isRequired;

  /// True when the attribute is a search facet.
  @override
  @JsonKey()
  final bool searchable;
  @override
  @JsonKey(unknownEnumValue: FacetStyle.list)
  final FacetStyle? facetStyle;

  /// Groups spec rows on the detail page: `engine`, `usage`, `documents`.
  @override
  final String? displayGroup;
  @override
  @JsonKey()
  final int sortOrder;
  final List<Option>? _options;
  @override
  List<Option>? get options {
    final value = _options;
    if (value == null) return null;
    if (_options is EqualUnmodifiableListView) return _options;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(value);
  }

  @override
  final AttributeRange? range;
  @override
  final String? helpText;

  @override
  String toString() {
    return 'TaxonomyAttribute(code: $code, label: $label, dataType: $dataType, unit: $unit, isRequired: $isRequired, searchable: $searchable, facetStyle: $facetStyle, displayGroup: $displayGroup, sortOrder: $sortOrder, options: $options, range: $range, helpText: $helpText)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$TaxonomyAttributeImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.label, label) || other.label == label) &&
            (identical(other.dataType, dataType) ||
                other.dataType == dataType) &&
            (identical(other.unit, unit) || other.unit == unit) &&
            (identical(other.isRequired, isRequired) ||
                other.isRequired == isRequired) &&
            (identical(other.searchable, searchable) ||
                other.searchable == searchable) &&
            (identical(other.facetStyle, facetStyle) ||
                other.facetStyle == facetStyle) &&
            (identical(other.displayGroup, displayGroup) ||
                other.displayGroup == displayGroup) &&
            (identical(other.sortOrder, sortOrder) ||
                other.sortOrder == sortOrder) &&
            const DeepCollectionEquality().equals(other._options, _options) &&
            (identical(other.range, range) || other.range == range) &&
            (identical(other.helpText, helpText) ||
                other.helpText == helpText));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    label,
    dataType,
    unit,
    isRequired,
    searchable,
    facetStyle,
    displayGroup,
    sortOrder,
    const DeepCollectionEquality().hash(_options),
    range,
    helpText,
  );

  /// Create a copy of TaxonomyAttribute
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$TaxonomyAttributeImplCopyWith<_$TaxonomyAttributeImpl> get copyWith =>
      __$$TaxonomyAttributeImplCopyWithImpl<_$TaxonomyAttributeImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$TaxonomyAttributeImplToJson(this);
  }
}

abstract class _TaxonomyAttribute implements TaxonomyAttribute {
  const factory _TaxonomyAttribute({
    required final String code,
    required final String label,
    @JsonKey(unknownEnumValue: AttributeDataType.string)
    final AttributeDataType dataType,
    final String? unit,
    @JsonKey(name: 'required') final bool isRequired,
    final bool searchable,
    @JsonKey(unknownEnumValue: FacetStyle.list) final FacetStyle? facetStyle,
    final String? displayGroup,
    final int sortOrder,
    final List<Option>? options,
    final AttributeRange? range,
    final String? helpText,
  }) = _$TaxonomyAttributeImpl;

  factory _TaxonomyAttribute.fromJson(Map<String, dynamic> json) =
      _$TaxonomyAttributeImpl.fromJson;

  @override
  String get code;
  @override
  String get label;
  @override
  @JsonKey(unknownEnumValue: AttributeDataType.string)
  AttributeDataType get dataType;

  /// `km`, `%`, `kg`, `bhp` — rendered beside the value, never folded into the label, because
  /// the same attribute on a commercial category may be declared in a different unit tomorrow.
  @override
  String? get unit;
  @override
  @JsonKey(name: 'required')
  bool get isRequired;

  /// True when the attribute is a search facet.
  @override
  bool get searchable;
  @override
  @JsonKey(unknownEnumValue: FacetStyle.list)
  FacetStyle? get facetStyle;

  /// Groups spec rows on the detail page: `engine`, `usage`, `documents`.
  @override
  String? get displayGroup;
  @override
  int get sortOrder;
  @override
  List<Option>? get options;
  @override
  AttributeRange? get range;
  @override
  String? get helpText;

  /// Create a copy of TaxonomyAttribute
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$TaxonomyAttributeImplCopyWith<_$TaxonomyAttributeImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

AttributesResponse _$AttributesResponseFromJson(Map<String, dynamic> json) {
  return _AttributesResponse.fromJson(json);
}

/// @nodoc
mixin _$AttributesResponse {
  int get version => throw _privateConstructorUsedError;
  List<TaxonomyAttribute> get attributes => throw _privateConstructorUsedError;

  /// Serializes this AttributesResponse to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of AttributesResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $AttributesResponseCopyWith<AttributesResponse> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $AttributesResponseCopyWith<$Res> {
  factory $AttributesResponseCopyWith(
    AttributesResponse value,
    $Res Function(AttributesResponse) then,
  ) = _$AttributesResponseCopyWithImpl<$Res, AttributesResponse>;
  @useResult
  $Res call({int version, List<TaxonomyAttribute> attributes});
}

/// @nodoc
class _$AttributesResponseCopyWithImpl<$Res, $Val extends AttributesResponse>
    implements $AttributesResponseCopyWith<$Res> {
  _$AttributesResponseCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of AttributesResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? version = null, Object? attributes = null}) {
    return _then(
      _value.copyWith(
            version: null == version
                ? _value.version
                : version // ignore: cast_nullable_to_non_nullable
                      as int,
            attributes: null == attributes
                ? _value.attributes
                : attributes // ignore: cast_nullable_to_non_nullable
                      as List<TaxonomyAttribute>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$AttributesResponseImplCopyWith<$Res>
    implements $AttributesResponseCopyWith<$Res> {
  factory _$$AttributesResponseImplCopyWith(
    _$AttributesResponseImpl value,
    $Res Function(_$AttributesResponseImpl) then,
  ) = __$$AttributesResponseImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int version, List<TaxonomyAttribute> attributes});
}

/// @nodoc
class __$$AttributesResponseImplCopyWithImpl<$Res>
    extends _$AttributesResponseCopyWithImpl<$Res, _$AttributesResponseImpl>
    implements _$$AttributesResponseImplCopyWith<$Res> {
  __$$AttributesResponseImplCopyWithImpl(
    _$AttributesResponseImpl _value,
    $Res Function(_$AttributesResponseImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AttributesResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? version = null, Object? attributes = null}) {
    return _then(
      _$AttributesResponseImpl(
        version: null == version
            ? _value.version
            : version // ignore: cast_nullable_to_non_nullable
                  as int,
        attributes: null == attributes
            ? _value._attributes
            : attributes // ignore: cast_nullable_to_non_nullable
                  as List<TaxonomyAttribute>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$AttributesResponseImpl implements _AttributesResponse {
  const _$AttributesResponseImpl({
    required this.version,
    final List<TaxonomyAttribute> attributes = const <TaxonomyAttribute>[],
  }) : _attributes = attributes;

  factory _$AttributesResponseImpl.fromJson(Map<String, dynamic> json) =>
      _$$AttributesResponseImplFromJson(json);

  @override
  final int version;
  final List<TaxonomyAttribute> _attributes;
  @override
  @JsonKey()
  List<TaxonomyAttribute> get attributes {
    if (_attributes is EqualUnmodifiableListView) return _attributes;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_attributes);
  }

  @override
  String toString() {
    return 'AttributesResponse(version: $version, attributes: $attributes)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AttributesResponseImpl &&
            (identical(other.version, version) || other.version == version) &&
            const DeepCollectionEquality().equals(
              other._attributes,
              _attributes,
            ));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    version,
    const DeepCollectionEquality().hash(_attributes),
  );

  /// Create a copy of AttributesResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AttributesResponseImplCopyWith<_$AttributesResponseImpl> get copyWith =>
      __$$AttributesResponseImplCopyWithImpl<_$AttributesResponseImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$AttributesResponseImplToJson(this);
  }
}

abstract class _AttributesResponse implements AttributesResponse {
  const factory _AttributesResponse({
    required final int version,
    final List<TaxonomyAttribute> attributes,
  }) = _$AttributesResponseImpl;

  factory _AttributesResponse.fromJson(Map<String, dynamic> json) =
      _$AttributesResponseImpl.fromJson;

  @override
  int get version;
  @override
  List<TaxonomyAttribute> get attributes;

  /// Create a copy of AttributesResponse
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AttributesResponseImplCopyWith<_$AttributesResponseImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

TaxonomyCategory _$TaxonomyCategoryFromJson(Map<String, dynamic> json) {
  return _TaxonomyCategory.fromJson(json);
}

/// @nodoc
mixin _$TaxonomyCategory {
  String get code => throw _privateConstructorUsedError;
  String get name => throw _privateConstructorUsedError;
  String? get icon => throw _privateConstructorUsedError;
  int get sortOrder => throw _privateConstructorUsedError;

  /// Attribute *codes* this category exposes. Resolve them through
  /// `GET /platform/taxonomy/attributes`; the tree deliberately does not inline the definitions,
  /// so the picker can render before the attribute documents have been fetched.
  List<String> get attributes => throw _privateConstructorUsedError;
  List<TaxonomyCategory> get children => throw _privateConstructorUsedError;

  /// Serializes this TaxonomyCategory to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of TaxonomyCategory
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $TaxonomyCategoryCopyWith<TaxonomyCategory> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $TaxonomyCategoryCopyWith<$Res> {
  factory $TaxonomyCategoryCopyWith(
    TaxonomyCategory value,
    $Res Function(TaxonomyCategory) then,
  ) = _$TaxonomyCategoryCopyWithImpl<$Res, TaxonomyCategory>;
  @useResult
  $Res call({
    String code,
    String name,
    String? icon,
    int sortOrder,
    List<String> attributes,
    List<TaxonomyCategory> children,
  });
}

/// @nodoc
class _$TaxonomyCategoryCopyWithImpl<$Res, $Val extends TaxonomyCategory>
    implements $TaxonomyCategoryCopyWith<$Res> {
  _$TaxonomyCategoryCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of TaxonomyCategory
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? name = null,
    Object? icon = freezed,
    Object? sortOrder = null,
    Object? attributes = null,
    Object? children = null,
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
            icon: freezed == icon
                ? _value.icon
                : icon // ignore: cast_nullable_to_non_nullable
                      as String?,
            sortOrder: null == sortOrder
                ? _value.sortOrder
                : sortOrder // ignore: cast_nullable_to_non_nullable
                      as int,
            attributes: null == attributes
                ? _value.attributes
                : attributes // ignore: cast_nullable_to_non_nullable
                      as List<String>,
            children: null == children
                ? _value.children
                : children // ignore: cast_nullable_to_non_nullable
                      as List<TaxonomyCategory>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$TaxonomyCategoryImplCopyWith<$Res>
    implements $TaxonomyCategoryCopyWith<$Res> {
  factory _$$TaxonomyCategoryImplCopyWith(
    _$TaxonomyCategoryImpl value,
    $Res Function(_$TaxonomyCategoryImpl) then,
  ) = __$$TaxonomyCategoryImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String code,
    String name,
    String? icon,
    int sortOrder,
    List<String> attributes,
    List<TaxonomyCategory> children,
  });
}

/// @nodoc
class __$$TaxonomyCategoryImplCopyWithImpl<$Res>
    extends _$TaxonomyCategoryCopyWithImpl<$Res, _$TaxonomyCategoryImpl>
    implements _$$TaxonomyCategoryImplCopyWith<$Res> {
  __$$TaxonomyCategoryImplCopyWithImpl(
    _$TaxonomyCategoryImpl _value,
    $Res Function(_$TaxonomyCategoryImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of TaxonomyCategory
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? name = null,
    Object? icon = freezed,
    Object? sortOrder = null,
    Object? attributes = null,
    Object? children = null,
  }) {
    return _then(
      _$TaxonomyCategoryImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        name: null == name
            ? _value.name
            : name // ignore: cast_nullable_to_non_nullable
                  as String,
        icon: freezed == icon
            ? _value.icon
            : icon // ignore: cast_nullable_to_non_nullable
                  as String?,
        sortOrder: null == sortOrder
            ? _value.sortOrder
            : sortOrder // ignore: cast_nullable_to_non_nullable
                  as int,
        attributes: null == attributes
            ? _value._attributes
            : attributes // ignore: cast_nullable_to_non_nullable
                  as List<String>,
        children: null == children
            ? _value._children
            : children // ignore: cast_nullable_to_non_nullable
                  as List<TaxonomyCategory>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$TaxonomyCategoryImpl implements _TaxonomyCategory {
  const _$TaxonomyCategoryImpl({
    required this.code,
    required this.name,
    this.icon,
    this.sortOrder = 0,
    final List<String> attributes = const <String>[],
    final List<TaxonomyCategory> children = const <TaxonomyCategory>[],
  }) : _attributes = attributes,
       _children = children;

  factory _$TaxonomyCategoryImpl.fromJson(Map<String, dynamic> json) =>
      _$$TaxonomyCategoryImplFromJson(json);

  @override
  final String code;
  @override
  final String name;
  @override
  final String? icon;
  @override
  @JsonKey()
  final int sortOrder;

  /// Attribute *codes* this category exposes. Resolve them through
  /// `GET /platform/taxonomy/attributes`; the tree deliberately does not inline the definitions,
  /// so the picker can render before the attribute documents have been fetched.
  final List<String> _attributes;

  /// Attribute *codes* this category exposes. Resolve them through
  /// `GET /platform/taxonomy/attributes`; the tree deliberately does not inline the definitions,
  /// so the picker can render before the attribute documents have been fetched.
  @override
  @JsonKey()
  List<String> get attributes {
    if (_attributes is EqualUnmodifiableListView) return _attributes;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_attributes);
  }

  final List<TaxonomyCategory> _children;
  @override
  @JsonKey()
  List<TaxonomyCategory> get children {
    if (_children is EqualUnmodifiableListView) return _children;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_children);
  }

  @override
  String toString() {
    return 'TaxonomyCategory(code: $code, name: $name, icon: $icon, sortOrder: $sortOrder, attributes: $attributes, children: $children)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$TaxonomyCategoryImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.name, name) || other.name == name) &&
            (identical(other.icon, icon) || other.icon == icon) &&
            (identical(other.sortOrder, sortOrder) ||
                other.sortOrder == sortOrder) &&
            const DeepCollectionEquality().equals(
              other._attributes,
              _attributes,
            ) &&
            const DeepCollectionEquality().equals(other._children, _children));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    name,
    icon,
    sortOrder,
    const DeepCollectionEquality().hash(_attributes),
    const DeepCollectionEquality().hash(_children),
  );

  /// Create a copy of TaxonomyCategory
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$TaxonomyCategoryImplCopyWith<_$TaxonomyCategoryImpl> get copyWith =>
      __$$TaxonomyCategoryImplCopyWithImpl<_$TaxonomyCategoryImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$TaxonomyCategoryImplToJson(this);
  }
}

abstract class _TaxonomyCategory implements TaxonomyCategory {
  const factory _TaxonomyCategory({
    required final String code,
    required final String name,
    final String? icon,
    final int sortOrder,
    final List<String> attributes,
    final List<TaxonomyCategory> children,
  }) = _$TaxonomyCategoryImpl;

  factory _TaxonomyCategory.fromJson(Map<String, dynamic> json) =
      _$TaxonomyCategoryImpl.fromJson;

  @override
  String get code;
  @override
  String get name;
  @override
  String? get icon;
  @override
  int get sortOrder;

  /// Attribute *codes* this category exposes. Resolve them through
  /// `GET /platform/taxonomy/attributes`; the tree deliberately does not inline the definitions,
  /// so the picker can render before the attribute documents have been fetched.
  @override
  List<String> get attributes;
  @override
  List<TaxonomyCategory> get children;

  /// Create a copy of TaxonomyCategory
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$TaxonomyCategoryImplCopyWith<_$TaxonomyCategoryImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

TaxonomyTree _$TaxonomyTreeFromJson(Map<String, dynamic> json) {
  return _TaxonomyTree.fromJson(json);
}

/// @nodoc
mixin _$TaxonomyTree {
  int get version => throw _privateConstructorUsedError;
  List<TaxonomyCategory> get categories => throw _privateConstructorUsedError;

  /// Serializes this TaxonomyTree to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of TaxonomyTree
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $TaxonomyTreeCopyWith<TaxonomyTree> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $TaxonomyTreeCopyWith<$Res> {
  factory $TaxonomyTreeCopyWith(
    TaxonomyTree value,
    $Res Function(TaxonomyTree) then,
  ) = _$TaxonomyTreeCopyWithImpl<$Res, TaxonomyTree>;
  @useResult
  $Res call({int version, List<TaxonomyCategory> categories});
}

/// @nodoc
class _$TaxonomyTreeCopyWithImpl<$Res, $Val extends TaxonomyTree>
    implements $TaxonomyTreeCopyWith<$Res> {
  _$TaxonomyTreeCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of TaxonomyTree
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? version = null, Object? categories = null}) {
    return _then(
      _value.copyWith(
            version: null == version
                ? _value.version
                : version // ignore: cast_nullable_to_non_nullable
                      as int,
            categories: null == categories
                ? _value.categories
                : categories // ignore: cast_nullable_to_non_nullable
                      as List<TaxonomyCategory>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$TaxonomyTreeImplCopyWith<$Res>
    implements $TaxonomyTreeCopyWith<$Res> {
  factory _$$TaxonomyTreeImplCopyWith(
    _$TaxonomyTreeImpl value,
    $Res Function(_$TaxonomyTreeImpl) then,
  ) = __$$TaxonomyTreeImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int version, List<TaxonomyCategory> categories});
}

/// @nodoc
class __$$TaxonomyTreeImplCopyWithImpl<$Res>
    extends _$TaxonomyTreeCopyWithImpl<$Res, _$TaxonomyTreeImpl>
    implements _$$TaxonomyTreeImplCopyWith<$Res> {
  __$$TaxonomyTreeImplCopyWithImpl(
    _$TaxonomyTreeImpl _value,
    $Res Function(_$TaxonomyTreeImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of TaxonomyTree
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? version = null, Object? categories = null}) {
    return _then(
      _$TaxonomyTreeImpl(
        version: null == version
            ? _value.version
            : version // ignore: cast_nullable_to_non_nullable
                  as int,
        categories: null == categories
            ? _value._categories
            : categories // ignore: cast_nullable_to_non_nullable
                  as List<TaxonomyCategory>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$TaxonomyTreeImpl implements _TaxonomyTree {
  const _$TaxonomyTreeImpl({
    required this.version,
    final List<TaxonomyCategory> categories = const <TaxonomyCategory>[],
  }) : _categories = categories;

  factory _$TaxonomyTreeImpl.fromJson(Map<String, dynamic> json) =>
      _$$TaxonomyTreeImplFromJson(json);

  @override
  final int version;
  final List<TaxonomyCategory> _categories;
  @override
  @JsonKey()
  List<TaxonomyCategory> get categories {
    if (_categories is EqualUnmodifiableListView) return _categories;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_categories);
  }

  @override
  String toString() {
    return 'TaxonomyTree(version: $version, categories: $categories)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$TaxonomyTreeImpl &&
            (identical(other.version, version) || other.version == version) &&
            const DeepCollectionEquality().equals(
              other._categories,
              _categories,
            ));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    version,
    const DeepCollectionEquality().hash(_categories),
  );

  /// Create a copy of TaxonomyTree
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$TaxonomyTreeImplCopyWith<_$TaxonomyTreeImpl> get copyWith =>
      __$$TaxonomyTreeImplCopyWithImpl<_$TaxonomyTreeImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$TaxonomyTreeImplToJson(this);
  }
}

abstract class _TaxonomyTree implements TaxonomyTree {
  const factory _TaxonomyTree({
    required final int version,
    final List<TaxonomyCategory> categories,
  }) = _$TaxonomyTreeImpl;

  factory _TaxonomyTree.fromJson(Map<String, dynamic> json) =
      _$TaxonomyTreeImpl.fromJson;

  @override
  int get version;
  @override
  List<TaxonomyCategory> get categories;

  /// Create a copy of TaxonomyTree
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$TaxonomyTreeImplCopyWith<_$TaxonomyTreeImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

TaxonomyNode _$TaxonomyNodeFromJson(Map<String, dynamic> json) {
  return _TaxonomyNode.fromJson(json);
}

/// @nodoc
mixin _$TaxonomyNode {
  String get code => throw _privateConstructorUsedError;
  String get name => throw _privateConstructorUsedError;
  String? get makeCode => throw _privateConstructorUsedError;
  String? get modelCode => throw _privateConstructorUsedError;
  String? get categoryCode => throw _privateConstructorUsedError;

  /// `[firstYear, lastYear]`, inclusive.
  List<int>? get years => throw _privateConstructorUsedError;
  String? get logoUrl => throw _privateConstructorUsedError;
  int? get popularity => throw _privateConstructorUsedError;

  /// Serializes this TaxonomyNode to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of TaxonomyNode
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $TaxonomyNodeCopyWith<TaxonomyNode> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $TaxonomyNodeCopyWith<$Res> {
  factory $TaxonomyNodeCopyWith(
    TaxonomyNode value,
    $Res Function(TaxonomyNode) then,
  ) = _$TaxonomyNodeCopyWithImpl<$Res, TaxonomyNode>;
  @useResult
  $Res call({
    String code,
    String name,
    String? makeCode,
    String? modelCode,
    String? categoryCode,
    List<int>? years,
    String? logoUrl,
    int? popularity,
  });
}

/// @nodoc
class _$TaxonomyNodeCopyWithImpl<$Res, $Val extends TaxonomyNode>
    implements $TaxonomyNodeCopyWith<$Res> {
  _$TaxonomyNodeCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of TaxonomyNode
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? name = null,
    Object? makeCode = freezed,
    Object? modelCode = freezed,
    Object? categoryCode = freezed,
    Object? years = freezed,
    Object? logoUrl = freezed,
    Object? popularity = freezed,
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
            makeCode: freezed == makeCode
                ? _value.makeCode
                : makeCode // ignore: cast_nullable_to_non_nullable
                      as String?,
            modelCode: freezed == modelCode
                ? _value.modelCode
                : modelCode // ignore: cast_nullable_to_non_nullable
                      as String?,
            categoryCode: freezed == categoryCode
                ? _value.categoryCode
                : categoryCode // ignore: cast_nullable_to_non_nullable
                      as String?,
            years: freezed == years
                ? _value.years
                : years // ignore: cast_nullable_to_non_nullable
                      as List<int>?,
            logoUrl: freezed == logoUrl
                ? _value.logoUrl
                : logoUrl // ignore: cast_nullable_to_non_nullable
                      as String?,
            popularity: freezed == popularity
                ? _value.popularity
                : popularity // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$TaxonomyNodeImplCopyWith<$Res>
    implements $TaxonomyNodeCopyWith<$Res> {
  factory _$$TaxonomyNodeImplCopyWith(
    _$TaxonomyNodeImpl value,
    $Res Function(_$TaxonomyNodeImpl) then,
  ) = __$$TaxonomyNodeImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String code,
    String name,
    String? makeCode,
    String? modelCode,
    String? categoryCode,
    List<int>? years,
    String? logoUrl,
    int? popularity,
  });
}

/// @nodoc
class __$$TaxonomyNodeImplCopyWithImpl<$Res>
    extends _$TaxonomyNodeCopyWithImpl<$Res, _$TaxonomyNodeImpl>
    implements _$$TaxonomyNodeImplCopyWith<$Res> {
  __$$TaxonomyNodeImplCopyWithImpl(
    _$TaxonomyNodeImpl _value,
    $Res Function(_$TaxonomyNodeImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of TaxonomyNode
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? name = null,
    Object? makeCode = freezed,
    Object? modelCode = freezed,
    Object? categoryCode = freezed,
    Object? years = freezed,
    Object? logoUrl = freezed,
    Object? popularity = freezed,
  }) {
    return _then(
      _$TaxonomyNodeImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        name: null == name
            ? _value.name
            : name // ignore: cast_nullable_to_non_nullable
                  as String,
        makeCode: freezed == makeCode
            ? _value.makeCode
            : makeCode // ignore: cast_nullable_to_non_nullable
                  as String?,
        modelCode: freezed == modelCode
            ? _value.modelCode
            : modelCode // ignore: cast_nullable_to_non_nullable
                  as String?,
        categoryCode: freezed == categoryCode
            ? _value.categoryCode
            : categoryCode // ignore: cast_nullable_to_non_nullable
                  as String?,
        years: freezed == years
            ? _value._years
            : years // ignore: cast_nullable_to_non_nullable
                  as List<int>?,
        logoUrl: freezed == logoUrl
            ? _value.logoUrl
            : logoUrl // ignore: cast_nullable_to_non_nullable
                  as String?,
        popularity: freezed == popularity
            ? _value.popularity
            : popularity // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$TaxonomyNodeImpl implements _TaxonomyNode {
  const _$TaxonomyNodeImpl({
    required this.code,
    required this.name,
    this.makeCode,
    this.modelCode,
    this.categoryCode,
    final List<int>? years,
    this.logoUrl,
    this.popularity,
  }) : _years = years;

  factory _$TaxonomyNodeImpl.fromJson(Map<String, dynamic> json) =>
      _$$TaxonomyNodeImplFromJson(json);

  @override
  final String code;
  @override
  final String name;
  @override
  final String? makeCode;
  @override
  final String? modelCode;
  @override
  final String? categoryCode;

  /// `[firstYear, lastYear]`, inclusive.
  final List<int>? _years;

  /// `[firstYear, lastYear]`, inclusive.
  @override
  List<int>? get years {
    final value = _years;
    if (value == null) return null;
    if (_years is EqualUnmodifiableListView) return _years;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(value);
  }

  @override
  final String? logoUrl;
  @override
  final int? popularity;

  @override
  String toString() {
    return 'TaxonomyNode(code: $code, name: $name, makeCode: $makeCode, modelCode: $modelCode, categoryCode: $categoryCode, years: $years, logoUrl: $logoUrl, popularity: $popularity)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$TaxonomyNodeImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.name, name) || other.name == name) &&
            (identical(other.makeCode, makeCode) ||
                other.makeCode == makeCode) &&
            (identical(other.modelCode, modelCode) ||
                other.modelCode == modelCode) &&
            (identical(other.categoryCode, categoryCode) ||
                other.categoryCode == categoryCode) &&
            const DeepCollectionEquality().equals(other._years, _years) &&
            (identical(other.logoUrl, logoUrl) || other.logoUrl == logoUrl) &&
            (identical(other.popularity, popularity) ||
                other.popularity == popularity));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    name,
    makeCode,
    modelCode,
    categoryCode,
    const DeepCollectionEquality().hash(_years),
    logoUrl,
    popularity,
  );

  /// Create a copy of TaxonomyNode
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$TaxonomyNodeImplCopyWith<_$TaxonomyNodeImpl> get copyWith =>
      __$$TaxonomyNodeImplCopyWithImpl<_$TaxonomyNodeImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$TaxonomyNodeImplToJson(this);
  }
}

abstract class _TaxonomyNode implements TaxonomyNode {
  const factory _TaxonomyNode({
    required final String code,
    required final String name,
    final String? makeCode,
    final String? modelCode,
    final String? categoryCode,
    final List<int>? years,
    final String? logoUrl,
    final int? popularity,
  }) = _$TaxonomyNodeImpl;

  factory _TaxonomyNode.fromJson(Map<String, dynamic> json) =
      _$TaxonomyNodeImpl.fromJson;

  @override
  String get code;
  @override
  String get name;
  @override
  String? get makeCode;
  @override
  String? get modelCode;
  @override
  String? get categoryCode;

  /// `[firstYear, lastYear]`, inclusive.
  @override
  List<int>? get years;
  @override
  String? get logoUrl;
  @override
  int? get popularity;

  /// Create a copy of TaxonomyNode
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$TaxonomyNodeImplCopyWith<_$TaxonomyNodeImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

TaxonomyNodesResponse _$TaxonomyNodesResponseFromJson(
  Map<String, dynamic> json,
) {
  return _TaxonomyNodesResponse.fromJson(json);
}

/// @nodoc
mixin _$TaxonomyNodesResponse {
  int get version => throw _privateConstructorUsedError;
  List<TaxonomyNode> get items => throw _privateConstructorUsedError;

  /// Serializes this TaxonomyNodesResponse to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of TaxonomyNodesResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $TaxonomyNodesResponseCopyWith<TaxonomyNodesResponse> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $TaxonomyNodesResponseCopyWith<$Res> {
  factory $TaxonomyNodesResponseCopyWith(
    TaxonomyNodesResponse value,
    $Res Function(TaxonomyNodesResponse) then,
  ) = _$TaxonomyNodesResponseCopyWithImpl<$Res, TaxonomyNodesResponse>;
  @useResult
  $Res call({int version, List<TaxonomyNode> items});
}

/// @nodoc
class _$TaxonomyNodesResponseCopyWithImpl<
  $Res,
  $Val extends TaxonomyNodesResponse
>
    implements $TaxonomyNodesResponseCopyWith<$Res> {
  _$TaxonomyNodesResponseCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of TaxonomyNodesResponse
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
                      as List<TaxonomyNode>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$TaxonomyNodesResponseImplCopyWith<$Res>
    implements $TaxonomyNodesResponseCopyWith<$Res> {
  factory _$$TaxonomyNodesResponseImplCopyWith(
    _$TaxonomyNodesResponseImpl value,
    $Res Function(_$TaxonomyNodesResponseImpl) then,
  ) = __$$TaxonomyNodesResponseImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int version, List<TaxonomyNode> items});
}

/// @nodoc
class __$$TaxonomyNodesResponseImplCopyWithImpl<$Res>
    extends
        _$TaxonomyNodesResponseCopyWithImpl<$Res, _$TaxonomyNodesResponseImpl>
    implements _$$TaxonomyNodesResponseImplCopyWith<$Res> {
  __$$TaxonomyNodesResponseImplCopyWithImpl(
    _$TaxonomyNodesResponseImpl _value,
    $Res Function(_$TaxonomyNodesResponseImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of TaxonomyNodesResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? version = null, Object? items = null}) {
    return _then(
      _$TaxonomyNodesResponseImpl(
        version: null == version
            ? _value.version
            : version // ignore: cast_nullable_to_non_nullable
                  as int,
        items: null == items
            ? _value._items
            : items // ignore: cast_nullable_to_non_nullable
                  as List<TaxonomyNode>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$TaxonomyNodesResponseImpl implements _TaxonomyNodesResponse {
  const _$TaxonomyNodesResponseImpl({
    required this.version,
    final List<TaxonomyNode> items = const <TaxonomyNode>[],
  }) : _items = items;

  factory _$TaxonomyNodesResponseImpl.fromJson(Map<String, dynamic> json) =>
      _$$TaxonomyNodesResponseImplFromJson(json);

  @override
  final int version;
  final List<TaxonomyNode> _items;
  @override
  @JsonKey()
  List<TaxonomyNode> get items {
    if (_items is EqualUnmodifiableListView) return _items;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_items);
  }

  @override
  String toString() {
    return 'TaxonomyNodesResponse(version: $version, items: $items)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$TaxonomyNodesResponseImpl &&
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

  /// Create a copy of TaxonomyNodesResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$TaxonomyNodesResponseImplCopyWith<_$TaxonomyNodesResponseImpl>
  get copyWith =>
      __$$TaxonomyNodesResponseImplCopyWithImpl<_$TaxonomyNodesResponseImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$TaxonomyNodesResponseImplToJson(this);
  }
}

abstract class _TaxonomyNodesResponse implements TaxonomyNodesResponse {
  const factory _TaxonomyNodesResponse({
    required final int version,
    final List<TaxonomyNode> items,
  }) = _$TaxonomyNodesResponseImpl;

  factory _TaxonomyNodesResponse.fromJson(Map<String, dynamic> json) =
      _$TaxonomyNodesResponseImpl.fromJson;

  @override
  int get version;
  @override
  List<TaxonomyNode> get items;

  /// Create a copy of TaxonomyNodesResponse
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$TaxonomyNodesResponseImplCopyWith<_$TaxonomyNodesResponseImpl>
  get copyWith => throw _privateConstructorUsedError;
}

VehicleSelection _$VehicleSelectionFromJson(Map<String, dynamic> json) {
  return _VehicleSelection.fromJson(json);
}

/// @nodoc
mixin _$VehicleSelection {
  String get categoryCode => throw _privateConstructorUsedError;
  String get makeCode => throw _privateConstructorUsedError;
  String get modelCode => throw _privateConstructorUsedError;
  String? get variantCode => throw _privateConstructorUsedError;
  int? get year => throw _privateConstructorUsedError;
  String? get vehicleId => throw _privateConstructorUsedError;

  /// Serializes this VehicleSelection to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of VehicleSelection
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $VehicleSelectionCopyWith<VehicleSelection> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $VehicleSelectionCopyWith<$Res> {
  factory $VehicleSelectionCopyWith(
    VehicleSelection value,
    $Res Function(VehicleSelection) then,
  ) = _$VehicleSelectionCopyWithImpl<$Res, VehicleSelection>;
  @useResult
  $Res call({
    String categoryCode,
    String makeCode,
    String modelCode,
    String? variantCode,
    int? year,
    String? vehicleId,
  });
}

/// @nodoc
class _$VehicleSelectionCopyWithImpl<$Res, $Val extends VehicleSelection>
    implements $VehicleSelectionCopyWith<$Res> {
  _$VehicleSelectionCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of VehicleSelection
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? categoryCode = null,
    Object? makeCode = null,
    Object? modelCode = null,
    Object? variantCode = freezed,
    Object? year = freezed,
    Object? vehicleId = freezed,
  }) {
    return _then(
      _value.copyWith(
            categoryCode: null == categoryCode
                ? _value.categoryCode
                : categoryCode // ignore: cast_nullable_to_non_nullable
                      as String,
            makeCode: null == makeCode
                ? _value.makeCode
                : makeCode // ignore: cast_nullable_to_non_nullable
                      as String,
            modelCode: null == modelCode
                ? _value.modelCode
                : modelCode // ignore: cast_nullable_to_non_nullable
                      as String,
            variantCode: freezed == variantCode
                ? _value.variantCode
                : variantCode // ignore: cast_nullable_to_non_nullable
                      as String?,
            year: freezed == year
                ? _value.year
                : year // ignore: cast_nullable_to_non_nullable
                      as int?,
            vehicleId: freezed == vehicleId
                ? _value.vehicleId
                : vehicleId // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$VehicleSelectionImplCopyWith<$Res>
    implements $VehicleSelectionCopyWith<$Res> {
  factory _$$VehicleSelectionImplCopyWith(
    _$VehicleSelectionImpl value,
    $Res Function(_$VehicleSelectionImpl) then,
  ) = __$$VehicleSelectionImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String categoryCode,
    String makeCode,
    String modelCode,
    String? variantCode,
    int? year,
    String? vehicleId,
  });
}

/// @nodoc
class __$$VehicleSelectionImplCopyWithImpl<$Res>
    extends _$VehicleSelectionCopyWithImpl<$Res, _$VehicleSelectionImpl>
    implements _$$VehicleSelectionImplCopyWith<$Res> {
  __$$VehicleSelectionImplCopyWithImpl(
    _$VehicleSelectionImpl _value,
    $Res Function(_$VehicleSelectionImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of VehicleSelection
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? categoryCode = null,
    Object? makeCode = null,
    Object? modelCode = null,
    Object? variantCode = freezed,
    Object? year = freezed,
    Object? vehicleId = freezed,
  }) {
    return _then(
      _$VehicleSelectionImpl(
        categoryCode: null == categoryCode
            ? _value.categoryCode
            : categoryCode // ignore: cast_nullable_to_non_nullable
                  as String,
        makeCode: null == makeCode
            ? _value.makeCode
            : makeCode // ignore: cast_nullable_to_non_nullable
                  as String,
        modelCode: null == modelCode
            ? _value.modelCode
            : modelCode // ignore: cast_nullable_to_non_nullable
                  as String,
        variantCode: freezed == variantCode
            ? _value.variantCode
            : variantCode // ignore: cast_nullable_to_non_nullable
                  as String?,
        year: freezed == year
            ? _value.year
            : year // ignore: cast_nullable_to_non_nullable
                  as int?,
        vehicleId: freezed == vehicleId
            ? _value.vehicleId
            : vehicleId // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$VehicleSelectionImpl implements _VehicleSelection {
  const _$VehicleSelectionImpl({
    required this.categoryCode,
    required this.makeCode,
    required this.modelCode,
    this.variantCode,
    this.year,
    this.vehicleId,
  });

  factory _$VehicleSelectionImpl.fromJson(Map<String, dynamic> json) =>
      _$$VehicleSelectionImplFromJson(json);

  @override
  final String categoryCode;
  @override
  final String makeCode;
  @override
  final String modelCode;
  @override
  final String? variantCode;
  @override
  final int? year;
  @override
  final String? vehicleId;

  @override
  String toString() {
    return 'VehicleSelection(categoryCode: $categoryCode, makeCode: $makeCode, modelCode: $modelCode, variantCode: $variantCode, year: $year, vehicleId: $vehicleId)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$VehicleSelectionImpl &&
            (identical(other.categoryCode, categoryCode) ||
                other.categoryCode == categoryCode) &&
            (identical(other.makeCode, makeCode) ||
                other.makeCode == makeCode) &&
            (identical(other.modelCode, modelCode) ||
                other.modelCode == modelCode) &&
            (identical(other.variantCode, variantCode) ||
                other.variantCode == variantCode) &&
            (identical(other.year, year) || other.year == year) &&
            (identical(other.vehicleId, vehicleId) ||
                other.vehicleId == vehicleId));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    categoryCode,
    makeCode,
    modelCode,
    variantCode,
    year,
    vehicleId,
  );

  /// Create a copy of VehicleSelection
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$VehicleSelectionImplCopyWith<_$VehicleSelectionImpl> get copyWith =>
      __$$VehicleSelectionImplCopyWithImpl<_$VehicleSelectionImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$VehicleSelectionImplToJson(this);
  }
}

abstract class _VehicleSelection implements VehicleSelection {
  const factory _VehicleSelection({
    required final String categoryCode,
    required final String makeCode,
    required final String modelCode,
    final String? variantCode,
    final int? year,
    final String? vehicleId,
  }) = _$VehicleSelectionImpl;

  factory _VehicleSelection.fromJson(Map<String, dynamic> json) =
      _$VehicleSelectionImpl.fromJson;

  @override
  String get categoryCode;
  @override
  String get makeCode;
  @override
  String get modelCode;
  @override
  String? get variantCode;
  @override
  int? get year;
  @override
  String? get vehicleId;

  /// Create a copy of VehicleSelection
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$VehicleSelectionImplCopyWith<_$VehicleSelectionImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
