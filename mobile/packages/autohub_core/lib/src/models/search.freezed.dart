// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'search.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

SearchItem _$SearchItemFromJson(Map<String, dynamic> json) {
  return _SearchItem.fromJson(json);
}

/// @nodoc
mixin _$SearchItem {
  String get listingId => throw _privateConstructorUsedError;
  @JsonKey(unknownEnumValue: ListingIntent.sale)
  ListingIntent get intent => throw _privateConstructorUsedError;
  Registration get registration => throw _privateConstructorUsedError;
  String get title => throw _privateConstructorUsedError;

  /// Nullable. An auction lot before its first bid, and a listing whose price is "on request",
  /// both legitimately have none.
  int? get priceMinor => throw _privateConstructorUsedError;

  /// Rental and lease price by period; absent on a straight sale.
  String? get pricePeriod => throw _privateConstructorUsedError;
  String get city => throw _privateConstructorUsedError;

  /// Rendered by `SpecStrip`. Order and content come from the registry, not from the widget.
  List<SpecItem> get specStrip => throw _privateConstructorUsedError;
  Media get media => throw _privateConstructorUsedError;

  /// `inspected`, `verified_rc` — codes. Labels resolve through the i18n registry.
  List<String> get badges => throw _privateConstructorUsedError;
  String? get dealerName => throw _privateConstructorUsedError;

  /// Serializes this SearchItem to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of SearchItem
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $SearchItemCopyWith<SearchItem> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $SearchItemCopyWith<$Res> {
  factory $SearchItemCopyWith(
    SearchItem value,
    $Res Function(SearchItem) then,
  ) = _$SearchItemCopyWithImpl<$Res, SearchItem>;
  @useResult
  $Res call({
    String listingId,
    @JsonKey(unknownEnumValue: ListingIntent.sale) ListingIntent intent,
    Registration registration,
    String title,
    int? priceMinor,
    String? pricePeriod,
    String city,
    List<SpecItem> specStrip,
    Media media,
    List<String> badges,
    String? dealerName,
  });

  $RegistrationCopyWith<$Res> get registration;
  $MediaCopyWith<$Res> get media;
}

/// @nodoc
class _$SearchItemCopyWithImpl<$Res, $Val extends SearchItem>
    implements $SearchItemCopyWith<$Res> {
  _$SearchItemCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of SearchItem
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? listingId = null,
    Object? intent = null,
    Object? registration = null,
    Object? title = null,
    Object? priceMinor = freezed,
    Object? pricePeriod = freezed,
    Object? city = null,
    Object? specStrip = null,
    Object? media = null,
    Object? badges = null,
    Object? dealerName = freezed,
  }) {
    return _then(
      _value.copyWith(
            listingId: null == listingId
                ? _value.listingId
                : listingId // ignore: cast_nullable_to_non_nullable
                      as String,
            intent: null == intent
                ? _value.intent
                : intent // ignore: cast_nullable_to_non_nullable
                      as ListingIntent,
            registration: null == registration
                ? _value.registration
                : registration // ignore: cast_nullable_to_non_nullable
                      as Registration,
            title: null == title
                ? _value.title
                : title // ignore: cast_nullable_to_non_nullable
                      as String,
            priceMinor: freezed == priceMinor
                ? _value.priceMinor
                : priceMinor // ignore: cast_nullable_to_non_nullable
                      as int?,
            pricePeriod: freezed == pricePeriod
                ? _value.pricePeriod
                : pricePeriod // ignore: cast_nullable_to_non_nullable
                      as String?,
            city: null == city
                ? _value.city
                : city // ignore: cast_nullable_to_non_nullable
                      as String,
            specStrip: null == specStrip
                ? _value.specStrip
                : specStrip // ignore: cast_nullable_to_non_nullable
                      as List<SpecItem>,
            media: null == media
                ? _value.media
                : media // ignore: cast_nullable_to_non_nullable
                      as Media,
            badges: null == badges
                ? _value.badges
                : badges // ignore: cast_nullable_to_non_nullable
                      as List<String>,
            dealerName: freezed == dealerName
                ? _value.dealerName
                : dealerName // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }

  /// Create a copy of SearchItem
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $RegistrationCopyWith<$Res> get registration {
    return $RegistrationCopyWith<$Res>(_value.registration, (value) {
      return _then(_value.copyWith(registration: value) as $Val);
    });
  }

  /// Create a copy of SearchItem
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $MediaCopyWith<$Res> get media {
    return $MediaCopyWith<$Res>(_value.media, (value) {
      return _then(_value.copyWith(media: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$SearchItemImplCopyWith<$Res>
    implements $SearchItemCopyWith<$Res> {
  factory _$$SearchItemImplCopyWith(
    _$SearchItemImpl value,
    $Res Function(_$SearchItemImpl) then,
  ) = __$$SearchItemImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String listingId,
    @JsonKey(unknownEnumValue: ListingIntent.sale) ListingIntent intent,
    Registration registration,
    String title,
    int? priceMinor,
    String? pricePeriod,
    String city,
    List<SpecItem> specStrip,
    Media media,
    List<String> badges,
    String? dealerName,
  });

  @override
  $RegistrationCopyWith<$Res> get registration;
  @override
  $MediaCopyWith<$Res> get media;
}

/// @nodoc
class __$$SearchItemImplCopyWithImpl<$Res>
    extends _$SearchItemCopyWithImpl<$Res, _$SearchItemImpl>
    implements _$$SearchItemImplCopyWith<$Res> {
  __$$SearchItemImplCopyWithImpl(
    _$SearchItemImpl _value,
    $Res Function(_$SearchItemImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SearchItem
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? listingId = null,
    Object? intent = null,
    Object? registration = null,
    Object? title = null,
    Object? priceMinor = freezed,
    Object? pricePeriod = freezed,
    Object? city = null,
    Object? specStrip = null,
    Object? media = null,
    Object? badges = null,
    Object? dealerName = freezed,
  }) {
    return _then(
      _$SearchItemImpl(
        listingId: null == listingId
            ? _value.listingId
            : listingId // ignore: cast_nullable_to_non_nullable
                  as String,
        intent: null == intent
            ? _value.intent
            : intent // ignore: cast_nullable_to_non_nullable
                  as ListingIntent,
        registration: null == registration
            ? _value.registration
            : registration // ignore: cast_nullable_to_non_nullable
                  as Registration,
        title: null == title
            ? _value.title
            : title // ignore: cast_nullable_to_non_nullable
                  as String,
        priceMinor: freezed == priceMinor
            ? _value.priceMinor
            : priceMinor // ignore: cast_nullable_to_non_nullable
                  as int?,
        pricePeriod: freezed == pricePeriod
            ? _value.pricePeriod
            : pricePeriod // ignore: cast_nullable_to_non_nullable
                  as String?,
        city: null == city
            ? _value.city
            : city // ignore: cast_nullable_to_non_nullable
                  as String,
        specStrip: null == specStrip
            ? _value._specStrip
            : specStrip // ignore: cast_nullable_to_non_nullable
                  as List<SpecItem>,
        media: null == media
            ? _value.media
            : media // ignore: cast_nullable_to_non_nullable
                  as Media,
        badges: null == badges
            ? _value._badges
            : badges // ignore: cast_nullable_to_non_nullable
                  as List<String>,
        dealerName: freezed == dealerName
            ? _value.dealerName
            : dealerName // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SearchItemImpl implements _SearchItem {
  const _$SearchItemImpl({
    required this.listingId,
    @JsonKey(unknownEnumValue: ListingIntent.sale)
    this.intent = ListingIntent.sale,
    required this.registration,
    required this.title,
    this.priceMinor,
    this.pricePeriod,
    this.city = '',
    final List<SpecItem> specStrip = const <SpecItem>[],
    this.media = const Media(),
    final List<String> badges = const <String>[],
    this.dealerName,
  }) : _specStrip = specStrip,
       _badges = badges;

  factory _$SearchItemImpl.fromJson(Map<String, dynamic> json) =>
      _$$SearchItemImplFromJson(json);

  @override
  final String listingId;
  @override
  @JsonKey(unknownEnumValue: ListingIntent.sale)
  final ListingIntent intent;
  @override
  final Registration registration;
  @override
  final String title;

  /// Nullable. An auction lot before its first bid, and a listing whose price is "on request",
  /// both legitimately have none.
  @override
  final int? priceMinor;

  /// Rental and lease price by period; absent on a straight sale.
  @override
  final String? pricePeriod;
  @override
  @JsonKey()
  final String city;

  /// Rendered by `SpecStrip`. Order and content come from the registry, not from the widget.
  final List<SpecItem> _specStrip;

  /// Rendered by `SpecStrip`. Order and content come from the registry, not from the widget.
  @override
  @JsonKey()
  List<SpecItem> get specStrip {
    if (_specStrip is EqualUnmodifiableListView) return _specStrip;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_specStrip);
  }

  @override
  @JsonKey()
  final Media media;

  /// `inspected`, `verified_rc` — codes. Labels resolve through the i18n registry.
  final List<String> _badges;

  /// `inspected`, `verified_rc` — codes. Labels resolve through the i18n registry.
  @override
  @JsonKey()
  List<String> get badges {
    if (_badges is EqualUnmodifiableListView) return _badges;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_badges);
  }

  @override
  final String? dealerName;

  @override
  String toString() {
    return 'SearchItem(listingId: $listingId, intent: $intent, registration: $registration, title: $title, priceMinor: $priceMinor, pricePeriod: $pricePeriod, city: $city, specStrip: $specStrip, media: $media, badges: $badges, dealerName: $dealerName)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SearchItemImpl &&
            (identical(other.listingId, listingId) ||
                other.listingId == listingId) &&
            (identical(other.intent, intent) || other.intent == intent) &&
            (identical(other.registration, registration) ||
                other.registration == registration) &&
            (identical(other.title, title) || other.title == title) &&
            (identical(other.priceMinor, priceMinor) ||
                other.priceMinor == priceMinor) &&
            (identical(other.pricePeriod, pricePeriod) ||
                other.pricePeriod == pricePeriod) &&
            (identical(other.city, city) || other.city == city) &&
            const DeepCollectionEquality().equals(
              other._specStrip,
              _specStrip,
            ) &&
            (identical(other.media, media) || other.media == media) &&
            const DeepCollectionEquality().equals(other._badges, _badges) &&
            (identical(other.dealerName, dealerName) ||
                other.dealerName == dealerName));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    listingId,
    intent,
    registration,
    title,
    priceMinor,
    pricePeriod,
    city,
    const DeepCollectionEquality().hash(_specStrip),
    media,
    const DeepCollectionEquality().hash(_badges),
    dealerName,
  );

  /// Create a copy of SearchItem
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SearchItemImplCopyWith<_$SearchItemImpl> get copyWith =>
      __$$SearchItemImplCopyWithImpl<_$SearchItemImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$SearchItemImplToJson(this);
  }
}

abstract class _SearchItem implements SearchItem {
  const factory _SearchItem({
    required final String listingId,
    @JsonKey(unknownEnumValue: ListingIntent.sale) final ListingIntent intent,
    required final Registration registration,
    required final String title,
    final int? priceMinor,
    final String? pricePeriod,
    final String city,
    final List<SpecItem> specStrip,
    final Media media,
    final List<String> badges,
    final String? dealerName,
  }) = _$SearchItemImpl;

  factory _SearchItem.fromJson(Map<String, dynamic> json) =
      _$SearchItemImpl.fromJson;

  @override
  String get listingId;
  @override
  @JsonKey(unknownEnumValue: ListingIntent.sale)
  ListingIntent get intent;
  @override
  Registration get registration;
  @override
  String get title;

  /// Nullable. An auction lot before its first bid, and a listing whose price is "on request",
  /// both legitimately have none.
  @override
  int? get priceMinor;

  /// Rental and lease price by period; absent on a straight sale.
  @override
  String? get pricePeriod;
  @override
  String get city;

  /// Rendered by `SpecStrip`. Order and content come from the registry, not from the widget.
  @override
  List<SpecItem> get specStrip;
  @override
  Media get media;

  /// `inspected`, `verified_rc` — codes. Labels resolve through the i18n registry.
  @override
  List<String> get badges;
  @override
  String? get dealerName;

  /// Create a copy of SearchItem
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SearchItemImplCopyWith<_$SearchItemImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

SearchFacet _$SearchFacetFromJson(Map<String, dynamic> json) {
  switch (json['style']) {
    case 'chips':
      return SearchFacetChips.fromJson(json);
    case 'list':
      return SearchFacetList.fromJson(json);
    case 'toggle':
      return SearchFacetToggle.fromJson(json);
    case 'range':
      return SearchFacetRange.fromJson(json);

    default:
      return SearchFacetUnsupported.fromJson(json);
  }
}

/// @nodoc
mixin _$SearchFacet {
  String get code => throw _privateConstructorUsedError;
  String get label => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String label, List<Option> options)
    chips,
    required TResult Function(String code, String label, List<Option> options)
    list,
    required TResult Function(String code, String label, List<Option> options)
    toggle,
    required TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )
    range,
    required TResult Function(String code, String label) unsupported,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String label, List<Option> options)? chips,
    TResult? Function(String code, String label, List<Option> options)? list,
    TResult? Function(String code, String label, List<Option> options)? toggle,
    TResult? Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult? Function(String code, String label)? unsupported,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String label, List<Option> options)? chips,
    TResult Function(String code, String label, List<Option> options)? list,
    TResult Function(String code, String label, List<Option> options)? toggle,
    TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult Function(String code, String label)? unsupported,
    required TResult orElse(),
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(SearchFacetChips value) chips,
    required TResult Function(SearchFacetList value) list,
    required TResult Function(SearchFacetToggle value) toggle,
    required TResult Function(SearchFacetRange value) range,
    required TResult Function(SearchFacetUnsupported value) unsupported,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(SearchFacetChips value)? chips,
    TResult? Function(SearchFacetList value)? list,
    TResult? Function(SearchFacetToggle value)? toggle,
    TResult? Function(SearchFacetRange value)? range,
    TResult? Function(SearchFacetUnsupported value)? unsupported,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(SearchFacetChips value)? chips,
    TResult Function(SearchFacetList value)? list,
    TResult Function(SearchFacetToggle value)? toggle,
    TResult Function(SearchFacetRange value)? range,
    TResult Function(SearchFacetUnsupported value)? unsupported,
    required TResult orElse(),
  }) => throw _privateConstructorUsedError;

  /// Serializes this SearchFacet to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $SearchFacetCopyWith<SearchFacet> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $SearchFacetCopyWith<$Res> {
  factory $SearchFacetCopyWith(
    SearchFacet value,
    $Res Function(SearchFacet) then,
  ) = _$SearchFacetCopyWithImpl<$Res, SearchFacet>;
  @useResult
  $Res call({String code, String label});
}

/// @nodoc
class _$SearchFacetCopyWithImpl<$Res, $Val extends SearchFacet>
    implements $SearchFacetCopyWith<$Res> {
  _$SearchFacetCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? code = null, Object? label = null}) {
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
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$SearchFacetChipsImplCopyWith<$Res>
    implements $SearchFacetCopyWith<$Res> {
  factory _$$SearchFacetChipsImplCopyWith(
    _$SearchFacetChipsImpl value,
    $Res Function(_$SearchFacetChipsImpl) then,
  ) = __$$SearchFacetChipsImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String label, List<Option> options});
}

/// @nodoc
class __$$SearchFacetChipsImplCopyWithImpl<$Res>
    extends _$SearchFacetCopyWithImpl<$Res, _$SearchFacetChipsImpl>
    implements _$$SearchFacetChipsImplCopyWith<$Res> {
  __$$SearchFacetChipsImplCopyWithImpl(
    _$SearchFacetChipsImpl _value,
    $Res Function(_$SearchFacetChipsImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? options = null,
  }) {
    return _then(
      _$SearchFacetChipsImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        options: null == options
            ? _value._options
            : options // ignore: cast_nullable_to_non_nullable
                  as List<Option>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SearchFacetChipsImpl implements SearchFacetChips {
  const _$SearchFacetChipsImpl({
    required this.code,
    required this.label,
    final List<Option> options = const <Option>[],
    final String? $type,
  }) : _options = options,
       $type = $type ?? 'chips';

  factory _$SearchFacetChipsImpl.fromJson(Map<String, dynamic> json) =>
      _$$SearchFacetChipsImplFromJson(json);

  @override
  final String code;
  @override
  final String label;
  final List<Option> _options;
  @override
  @JsonKey()
  List<Option> get options {
    if (_options is EqualUnmodifiableListView) return _options;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_options);
  }

  @JsonKey(name: 'style')
  final String $type;

  @override
  String toString() {
    return 'SearchFacet.chips(code: $code, label: $label, options: $options)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SearchFacetChipsImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.label, label) || other.label == label) &&
            const DeepCollectionEquality().equals(other._options, _options));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    label,
    const DeepCollectionEquality().hash(_options),
  );

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SearchFacetChipsImplCopyWith<_$SearchFacetChipsImpl> get copyWith =>
      __$$SearchFacetChipsImplCopyWithImpl<_$SearchFacetChipsImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String label, List<Option> options)
    chips,
    required TResult Function(String code, String label, List<Option> options)
    list,
    required TResult Function(String code, String label, List<Option> options)
    toggle,
    required TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )
    range,
    required TResult Function(String code, String label) unsupported,
  }) {
    return chips(code, label, options);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String label, List<Option> options)? chips,
    TResult? Function(String code, String label, List<Option> options)? list,
    TResult? Function(String code, String label, List<Option> options)? toggle,
    TResult? Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult? Function(String code, String label)? unsupported,
  }) {
    return chips?.call(code, label, options);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String label, List<Option> options)? chips,
    TResult Function(String code, String label, List<Option> options)? list,
    TResult Function(String code, String label, List<Option> options)? toggle,
    TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult Function(String code, String label)? unsupported,
    required TResult orElse(),
  }) {
    if (chips != null) {
      return chips(code, label, options);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(SearchFacetChips value) chips,
    required TResult Function(SearchFacetList value) list,
    required TResult Function(SearchFacetToggle value) toggle,
    required TResult Function(SearchFacetRange value) range,
    required TResult Function(SearchFacetUnsupported value) unsupported,
  }) {
    return chips(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(SearchFacetChips value)? chips,
    TResult? Function(SearchFacetList value)? list,
    TResult? Function(SearchFacetToggle value)? toggle,
    TResult? Function(SearchFacetRange value)? range,
    TResult? Function(SearchFacetUnsupported value)? unsupported,
  }) {
    return chips?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(SearchFacetChips value)? chips,
    TResult Function(SearchFacetList value)? list,
    TResult Function(SearchFacetToggle value)? toggle,
    TResult Function(SearchFacetRange value)? range,
    TResult Function(SearchFacetUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (chips != null) {
      return chips(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$SearchFacetChipsImplToJson(this);
  }
}

abstract class SearchFacetChips implements SearchFacet {
  const factory SearchFacetChips({
    required final String code,
    required final String label,
    final List<Option> options,
  }) = _$SearchFacetChipsImpl;

  factory SearchFacetChips.fromJson(Map<String, dynamic> json) =
      _$SearchFacetChipsImpl.fromJson;

  @override
  String get code;
  @override
  String get label;
  List<Option> get options;

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SearchFacetChipsImplCopyWith<_$SearchFacetChipsImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$SearchFacetListImplCopyWith<$Res>
    implements $SearchFacetCopyWith<$Res> {
  factory _$$SearchFacetListImplCopyWith(
    _$SearchFacetListImpl value,
    $Res Function(_$SearchFacetListImpl) then,
  ) = __$$SearchFacetListImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String label, List<Option> options});
}

/// @nodoc
class __$$SearchFacetListImplCopyWithImpl<$Res>
    extends _$SearchFacetCopyWithImpl<$Res, _$SearchFacetListImpl>
    implements _$$SearchFacetListImplCopyWith<$Res> {
  __$$SearchFacetListImplCopyWithImpl(
    _$SearchFacetListImpl _value,
    $Res Function(_$SearchFacetListImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? options = null,
  }) {
    return _then(
      _$SearchFacetListImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        options: null == options
            ? _value._options
            : options // ignore: cast_nullable_to_non_nullable
                  as List<Option>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SearchFacetListImpl implements SearchFacetList {
  const _$SearchFacetListImpl({
    required this.code,
    required this.label,
    final List<Option> options = const <Option>[],
    final String? $type,
  }) : _options = options,
       $type = $type ?? 'list';

  factory _$SearchFacetListImpl.fromJson(Map<String, dynamic> json) =>
      _$$SearchFacetListImplFromJson(json);

  @override
  final String code;
  @override
  final String label;
  final List<Option> _options;
  @override
  @JsonKey()
  List<Option> get options {
    if (_options is EqualUnmodifiableListView) return _options;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_options);
  }

  @JsonKey(name: 'style')
  final String $type;

  @override
  String toString() {
    return 'SearchFacet.list(code: $code, label: $label, options: $options)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SearchFacetListImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.label, label) || other.label == label) &&
            const DeepCollectionEquality().equals(other._options, _options));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    label,
    const DeepCollectionEquality().hash(_options),
  );

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SearchFacetListImplCopyWith<_$SearchFacetListImpl> get copyWith =>
      __$$SearchFacetListImplCopyWithImpl<_$SearchFacetListImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String label, List<Option> options)
    chips,
    required TResult Function(String code, String label, List<Option> options)
    list,
    required TResult Function(String code, String label, List<Option> options)
    toggle,
    required TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )
    range,
    required TResult Function(String code, String label) unsupported,
  }) {
    return list(code, label, options);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String label, List<Option> options)? chips,
    TResult? Function(String code, String label, List<Option> options)? list,
    TResult? Function(String code, String label, List<Option> options)? toggle,
    TResult? Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult? Function(String code, String label)? unsupported,
  }) {
    return list?.call(code, label, options);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String label, List<Option> options)? chips,
    TResult Function(String code, String label, List<Option> options)? list,
    TResult Function(String code, String label, List<Option> options)? toggle,
    TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult Function(String code, String label)? unsupported,
    required TResult orElse(),
  }) {
    if (list != null) {
      return list(code, label, options);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(SearchFacetChips value) chips,
    required TResult Function(SearchFacetList value) list,
    required TResult Function(SearchFacetToggle value) toggle,
    required TResult Function(SearchFacetRange value) range,
    required TResult Function(SearchFacetUnsupported value) unsupported,
  }) {
    return list(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(SearchFacetChips value)? chips,
    TResult? Function(SearchFacetList value)? list,
    TResult? Function(SearchFacetToggle value)? toggle,
    TResult? Function(SearchFacetRange value)? range,
    TResult? Function(SearchFacetUnsupported value)? unsupported,
  }) {
    return list?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(SearchFacetChips value)? chips,
    TResult Function(SearchFacetList value)? list,
    TResult Function(SearchFacetToggle value)? toggle,
    TResult Function(SearchFacetRange value)? range,
    TResult Function(SearchFacetUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (list != null) {
      return list(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$SearchFacetListImplToJson(this);
  }
}

abstract class SearchFacetList implements SearchFacet {
  const factory SearchFacetList({
    required final String code,
    required final String label,
    final List<Option> options,
  }) = _$SearchFacetListImpl;

  factory SearchFacetList.fromJson(Map<String, dynamic> json) =
      _$SearchFacetListImpl.fromJson;

  @override
  String get code;
  @override
  String get label;
  List<Option> get options;

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SearchFacetListImplCopyWith<_$SearchFacetListImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$SearchFacetToggleImplCopyWith<$Res>
    implements $SearchFacetCopyWith<$Res> {
  factory _$$SearchFacetToggleImplCopyWith(
    _$SearchFacetToggleImpl value,
    $Res Function(_$SearchFacetToggleImpl) then,
  ) = __$$SearchFacetToggleImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String label, List<Option> options});
}

/// @nodoc
class __$$SearchFacetToggleImplCopyWithImpl<$Res>
    extends _$SearchFacetCopyWithImpl<$Res, _$SearchFacetToggleImpl>
    implements _$$SearchFacetToggleImplCopyWith<$Res> {
  __$$SearchFacetToggleImplCopyWithImpl(
    _$SearchFacetToggleImpl _value,
    $Res Function(_$SearchFacetToggleImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? options = null,
  }) {
    return _then(
      _$SearchFacetToggleImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        options: null == options
            ? _value._options
            : options // ignore: cast_nullable_to_non_nullable
                  as List<Option>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SearchFacetToggleImpl implements SearchFacetToggle {
  const _$SearchFacetToggleImpl({
    required this.code,
    required this.label,
    final List<Option> options = const <Option>[],
    final String? $type,
  }) : _options = options,
       $type = $type ?? 'toggle';

  factory _$SearchFacetToggleImpl.fromJson(Map<String, dynamic> json) =>
      _$$SearchFacetToggleImplFromJson(json);

  @override
  final String code;
  @override
  final String label;
  final List<Option> _options;
  @override
  @JsonKey()
  List<Option> get options {
    if (_options is EqualUnmodifiableListView) return _options;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_options);
  }

  @JsonKey(name: 'style')
  final String $type;

  @override
  String toString() {
    return 'SearchFacet.toggle(code: $code, label: $label, options: $options)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SearchFacetToggleImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.label, label) || other.label == label) &&
            const DeepCollectionEquality().equals(other._options, _options));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    label,
    const DeepCollectionEquality().hash(_options),
  );

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SearchFacetToggleImplCopyWith<_$SearchFacetToggleImpl> get copyWith =>
      __$$SearchFacetToggleImplCopyWithImpl<_$SearchFacetToggleImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String label, List<Option> options)
    chips,
    required TResult Function(String code, String label, List<Option> options)
    list,
    required TResult Function(String code, String label, List<Option> options)
    toggle,
    required TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )
    range,
    required TResult Function(String code, String label) unsupported,
  }) {
    return toggle(code, label, options);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String label, List<Option> options)? chips,
    TResult? Function(String code, String label, List<Option> options)? list,
    TResult? Function(String code, String label, List<Option> options)? toggle,
    TResult? Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult? Function(String code, String label)? unsupported,
  }) {
    return toggle?.call(code, label, options);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String label, List<Option> options)? chips,
    TResult Function(String code, String label, List<Option> options)? list,
    TResult Function(String code, String label, List<Option> options)? toggle,
    TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult Function(String code, String label)? unsupported,
    required TResult orElse(),
  }) {
    if (toggle != null) {
      return toggle(code, label, options);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(SearchFacetChips value) chips,
    required TResult Function(SearchFacetList value) list,
    required TResult Function(SearchFacetToggle value) toggle,
    required TResult Function(SearchFacetRange value) range,
    required TResult Function(SearchFacetUnsupported value) unsupported,
  }) {
    return toggle(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(SearchFacetChips value)? chips,
    TResult? Function(SearchFacetList value)? list,
    TResult? Function(SearchFacetToggle value)? toggle,
    TResult? Function(SearchFacetRange value)? range,
    TResult? Function(SearchFacetUnsupported value)? unsupported,
  }) {
    return toggle?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(SearchFacetChips value)? chips,
    TResult Function(SearchFacetList value)? list,
    TResult Function(SearchFacetToggle value)? toggle,
    TResult Function(SearchFacetRange value)? range,
    TResult Function(SearchFacetUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (toggle != null) {
      return toggle(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$SearchFacetToggleImplToJson(this);
  }
}

abstract class SearchFacetToggle implements SearchFacet {
  const factory SearchFacetToggle({
    required final String code,
    required final String label,
    final List<Option> options,
  }) = _$SearchFacetToggleImpl;

  factory SearchFacetToggle.fromJson(Map<String, dynamic> json) =
      _$SearchFacetToggleImpl.fromJson;

  @override
  String get code;
  @override
  String get label;
  List<Option> get options;

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SearchFacetToggleImplCopyWith<_$SearchFacetToggleImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$SearchFacetRangeImplCopyWith<$Res>
    implements $SearchFacetCopyWith<$Res> {
  factory _$$SearchFacetRangeImplCopyWith(
    _$SearchFacetRangeImpl value,
    $Res Function(_$SearchFacetRangeImpl) then,
  ) = __$$SearchFacetRangeImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String code,
    String label,
    double min,
    double max,
    double? step,
    String? unit,
  });
}

/// @nodoc
class __$$SearchFacetRangeImplCopyWithImpl<$Res>
    extends _$SearchFacetCopyWithImpl<$Res, _$SearchFacetRangeImpl>
    implements _$$SearchFacetRangeImplCopyWith<$Res> {
  __$$SearchFacetRangeImplCopyWithImpl(
    _$SearchFacetRangeImpl _value,
    $Res Function(_$SearchFacetRangeImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? code = null,
    Object? label = null,
    Object? min = null,
    Object? max = null,
    Object? step = freezed,
    Object? unit = freezed,
  }) {
    return _then(
      _$SearchFacetRangeImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
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
        unit: freezed == unit
            ? _value.unit
            : unit // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SearchFacetRangeImpl implements SearchFacetRange {
  const _$SearchFacetRangeImpl({
    required this.code,
    required this.label,
    required this.min,
    required this.max,
    this.step,
    this.unit,
    final String? $type,
  }) : $type = $type ?? 'range';

  factory _$SearchFacetRangeImpl.fromJson(Map<String, dynamic> json) =>
      _$$SearchFacetRangeImplFromJson(json);

  @override
  final String code;
  @override
  final String label;
  @override
  final double min;
  @override
  final double max;
  @override
  final double? step;
  @override
  final String? unit;

  @JsonKey(name: 'style')
  final String $type;

  @override
  String toString() {
    return 'SearchFacet.range(code: $code, label: $label, min: $min, max: $max, step: $step, unit: $unit)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SearchFacetRangeImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.label, label) || other.label == label) &&
            (identical(other.min, min) || other.min == min) &&
            (identical(other.max, max) || other.max == max) &&
            (identical(other.step, step) || other.step == step) &&
            (identical(other.unit, unit) || other.unit == unit));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, code, label, min, max, step, unit);

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SearchFacetRangeImplCopyWith<_$SearchFacetRangeImpl> get copyWith =>
      __$$SearchFacetRangeImplCopyWithImpl<_$SearchFacetRangeImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String label, List<Option> options)
    chips,
    required TResult Function(String code, String label, List<Option> options)
    list,
    required TResult Function(String code, String label, List<Option> options)
    toggle,
    required TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )
    range,
    required TResult Function(String code, String label) unsupported,
  }) {
    return range(code, label, min, max, step, unit);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String label, List<Option> options)? chips,
    TResult? Function(String code, String label, List<Option> options)? list,
    TResult? Function(String code, String label, List<Option> options)? toggle,
    TResult? Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult? Function(String code, String label)? unsupported,
  }) {
    return range?.call(code, label, min, max, step, unit);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String label, List<Option> options)? chips,
    TResult Function(String code, String label, List<Option> options)? list,
    TResult Function(String code, String label, List<Option> options)? toggle,
    TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult Function(String code, String label)? unsupported,
    required TResult orElse(),
  }) {
    if (range != null) {
      return range(code, label, min, max, step, unit);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(SearchFacetChips value) chips,
    required TResult Function(SearchFacetList value) list,
    required TResult Function(SearchFacetToggle value) toggle,
    required TResult Function(SearchFacetRange value) range,
    required TResult Function(SearchFacetUnsupported value) unsupported,
  }) {
    return range(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(SearchFacetChips value)? chips,
    TResult? Function(SearchFacetList value)? list,
    TResult? Function(SearchFacetToggle value)? toggle,
    TResult? Function(SearchFacetRange value)? range,
    TResult? Function(SearchFacetUnsupported value)? unsupported,
  }) {
    return range?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(SearchFacetChips value)? chips,
    TResult Function(SearchFacetList value)? list,
    TResult Function(SearchFacetToggle value)? toggle,
    TResult Function(SearchFacetRange value)? range,
    TResult Function(SearchFacetUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (range != null) {
      return range(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$SearchFacetRangeImplToJson(this);
  }
}

abstract class SearchFacetRange implements SearchFacet {
  const factory SearchFacetRange({
    required final String code,
    required final String label,
    required final double min,
    required final double max,
    final double? step,
    final String? unit,
  }) = _$SearchFacetRangeImpl;

  factory SearchFacetRange.fromJson(Map<String, dynamic> json) =
      _$SearchFacetRangeImpl.fromJson;

  @override
  String get code;
  @override
  String get label;
  double get min;
  double get max;
  double? get step;
  String? get unit;

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SearchFacetRangeImplCopyWith<_$SearchFacetRangeImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$SearchFacetUnsupportedImplCopyWith<$Res>
    implements $SearchFacetCopyWith<$Res> {
  factory _$$SearchFacetUnsupportedImplCopyWith(
    _$SearchFacetUnsupportedImpl value,
    $Res Function(_$SearchFacetUnsupportedImpl) then,
  ) = __$$SearchFacetUnsupportedImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String label});
}

/// @nodoc
class __$$SearchFacetUnsupportedImplCopyWithImpl<$Res>
    extends _$SearchFacetCopyWithImpl<$Res, _$SearchFacetUnsupportedImpl>
    implements _$$SearchFacetUnsupportedImplCopyWith<$Res> {
  __$$SearchFacetUnsupportedImplCopyWithImpl(
    _$SearchFacetUnsupportedImpl _value,
    $Res Function(_$SearchFacetUnsupportedImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? code = null, Object? label = null}) {
    return _then(
      _$SearchFacetUnsupportedImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SearchFacetUnsupportedImpl implements SearchFacetUnsupported {
  const _$SearchFacetUnsupportedImpl({
    this.code = '',
    this.label = '',
    final String? $type,
  }) : $type = $type ?? 'unsupported';

  factory _$SearchFacetUnsupportedImpl.fromJson(Map<String, dynamic> json) =>
      _$$SearchFacetUnsupportedImplFromJson(json);

  @override
  @JsonKey()
  final String code;
  @override
  @JsonKey()
  final String label;

  @JsonKey(name: 'style')
  final String $type;

  @override
  String toString() {
    return 'SearchFacet.unsupported(code: $code, label: $label)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SearchFacetUnsupportedImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.label, label) || other.label == label));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, code, label);

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SearchFacetUnsupportedImplCopyWith<_$SearchFacetUnsupportedImpl>
  get copyWith =>
      __$$SearchFacetUnsupportedImplCopyWithImpl<_$SearchFacetUnsupportedImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(String code, String label, List<Option> options)
    chips,
    required TResult Function(String code, String label, List<Option> options)
    list,
    required TResult Function(String code, String label, List<Option> options)
    toggle,
    required TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )
    range,
    required TResult Function(String code, String label) unsupported,
  }) {
    return unsupported(code, label);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(String code, String label, List<Option> options)? chips,
    TResult? Function(String code, String label, List<Option> options)? list,
    TResult? Function(String code, String label, List<Option> options)? toggle,
    TResult? Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult? Function(String code, String label)? unsupported,
  }) {
    return unsupported?.call(code, label);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(String code, String label, List<Option> options)? chips,
    TResult Function(String code, String label, List<Option> options)? list,
    TResult Function(String code, String label, List<Option> options)? toggle,
    TResult Function(
      String code,
      String label,
      double min,
      double max,
      double? step,
      String? unit,
    )?
    range,
    TResult Function(String code, String label)? unsupported,
    required TResult orElse(),
  }) {
    if (unsupported != null) {
      return unsupported(code, label);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(SearchFacetChips value) chips,
    required TResult Function(SearchFacetList value) list,
    required TResult Function(SearchFacetToggle value) toggle,
    required TResult Function(SearchFacetRange value) range,
    required TResult Function(SearchFacetUnsupported value) unsupported,
  }) {
    return unsupported(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(SearchFacetChips value)? chips,
    TResult? Function(SearchFacetList value)? list,
    TResult? Function(SearchFacetToggle value)? toggle,
    TResult? Function(SearchFacetRange value)? range,
    TResult? Function(SearchFacetUnsupported value)? unsupported,
  }) {
    return unsupported?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(SearchFacetChips value)? chips,
    TResult Function(SearchFacetList value)? list,
    TResult Function(SearchFacetToggle value)? toggle,
    TResult Function(SearchFacetRange value)? range,
    TResult Function(SearchFacetUnsupported value)? unsupported,
    required TResult orElse(),
  }) {
    if (unsupported != null) {
      return unsupported(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$SearchFacetUnsupportedImplToJson(this);
  }
}

abstract class SearchFacetUnsupported implements SearchFacet {
  const factory SearchFacetUnsupported({
    final String code,
    final String label,
  }) = _$SearchFacetUnsupportedImpl;

  factory SearchFacetUnsupported.fromJson(Map<String, dynamic> json) =
      _$SearchFacetUnsupportedImpl.fromJson;

  @override
  String get code;
  @override
  String get label;

  /// Create a copy of SearchFacet
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SearchFacetUnsupportedImplCopyWith<_$SearchFacetUnsupportedImpl>
  get copyWith => throw _privateConstructorUsedError;
}

SearchResponse _$SearchResponseFromJson(Map<String, dynamic> json) {
  return _SearchResponse.fromJson(json);
}

/// @nodoc
mixin _$SearchResponse {
  List<SearchItem> get items => throw _privateConstructorUsedError;
  List<SearchFacet> get facets => throw _privateConstructorUsedError;
  String? get nextCursor => throw _privateConstructorUsedError;

  /// An estimate by contract — never a `COUNT(*)` on a hot table. The UI says "about 1,284" and
  /// never promises precision it does not have.
  int? get totalEstimate => throw _privateConstructorUsedError;

  /// Serializes this SearchResponse to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of SearchResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $SearchResponseCopyWith<SearchResponse> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $SearchResponseCopyWith<$Res> {
  factory $SearchResponseCopyWith(
    SearchResponse value,
    $Res Function(SearchResponse) then,
  ) = _$SearchResponseCopyWithImpl<$Res, SearchResponse>;
  @useResult
  $Res call({
    List<SearchItem> items,
    List<SearchFacet> facets,
    String? nextCursor,
    int? totalEstimate,
  });
}

/// @nodoc
class _$SearchResponseCopyWithImpl<$Res, $Val extends SearchResponse>
    implements $SearchResponseCopyWith<$Res> {
  _$SearchResponseCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of SearchResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? items = null,
    Object? facets = null,
    Object? nextCursor = freezed,
    Object? totalEstimate = freezed,
  }) {
    return _then(
      _value.copyWith(
            items: null == items
                ? _value.items
                : items // ignore: cast_nullable_to_non_nullable
                      as List<SearchItem>,
            facets: null == facets
                ? _value.facets
                : facets // ignore: cast_nullable_to_non_nullable
                      as List<SearchFacet>,
            nextCursor: freezed == nextCursor
                ? _value.nextCursor
                : nextCursor // ignore: cast_nullable_to_non_nullable
                      as String?,
            totalEstimate: freezed == totalEstimate
                ? _value.totalEstimate
                : totalEstimate // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$SearchResponseImplCopyWith<$Res>
    implements $SearchResponseCopyWith<$Res> {
  factory _$$SearchResponseImplCopyWith(
    _$SearchResponseImpl value,
    $Res Function(_$SearchResponseImpl) then,
  ) = __$$SearchResponseImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    List<SearchItem> items,
    List<SearchFacet> facets,
    String? nextCursor,
    int? totalEstimate,
  });
}

/// @nodoc
class __$$SearchResponseImplCopyWithImpl<$Res>
    extends _$SearchResponseCopyWithImpl<$Res, _$SearchResponseImpl>
    implements _$$SearchResponseImplCopyWith<$Res> {
  __$$SearchResponseImplCopyWithImpl(
    _$SearchResponseImpl _value,
    $Res Function(_$SearchResponseImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SearchResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? items = null,
    Object? facets = null,
    Object? nextCursor = freezed,
    Object? totalEstimate = freezed,
  }) {
    return _then(
      _$SearchResponseImpl(
        items: null == items
            ? _value._items
            : items // ignore: cast_nullable_to_non_nullable
                  as List<SearchItem>,
        facets: null == facets
            ? _value._facets
            : facets // ignore: cast_nullable_to_non_nullable
                  as List<SearchFacet>,
        nextCursor: freezed == nextCursor
            ? _value.nextCursor
            : nextCursor // ignore: cast_nullable_to_non_nullable
                  as String?,
        totalEstimate: freezed == totalEstimate
            ? _value.totalEstimate
            : totalEstimate // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SearchResponseImpl implements _SearchResponse {
  const _$SearchResponseImpl({
    final List<SearchItem> items = const <SearchItem>[],
    final List<SearchFacet> facets = const <SearchFacet>[],
    this.nextCursor,
    this.totalEstimate,
  }) : _items = items,
       _facets = facets;

  factory _$SearchResponseImpl.fromJson(Map<String, dynamic> json) =>
      _$$SearchResponseImplFromJson(json);

  final List<SearchItem> _items;
  @override
  @JsonKey()
  List<SearchItem> get items {
    if (_items is EqualUnmodifiableListView) return _items;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_items);
  }

  final List<SearchFacet> _facets;
  @override
  @JsonKey()
  List<SearchFacet> get facets {
    if (_facets is EqualUnmodifiableListView) return _facets;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_facets);
  }

  @override
  final String? nextCursor;

  /// An estimate by contract — never a `COUNT(*)` on a hot table. The UI says "about 1,284" and
  /// never promises precision it does not have.
  @override
  final int? totalEstimate;

  @override
  String toString() {
    return 'SearchResponse(items: $items, facets: $facets, nextCursor: $nextCursor, totalEstimate: $totalEstimate)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SearchResponseImpl &&
            const DeepCollectionEquality().equals(other._items, _items) &&
            const DeepCollectionEquality().equals(other._facets, _facets) &&
            (identical(other.nextCursor, nextCursor) ||
                other.nextCursor == nextCursor) &&
            (identical(other.totalEstimate, totalEstimate) ||
                other.totalEstimate == totalEstimate));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    const DeepCollectionEquality().hash(_items),
    const DeepCollectionEquality().hash(_facets),
    nextCursor,
    totalEstimate,
  );

  /// Create a copy of SearchResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SearchResponseImplCopyWith<_$SearchResponseImpl> get copyWith =>
      __$$SearchResponseImplCopyWithImpl<_$SearchResponseImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$SearchResponseImplToJson(this);
  }
}

abstract class _SearchResponse implements SearchResponse {
  const factory _SearchResponse({
    final List<SearchItem> items,
    final List<SearchFacet> facets,
    final String? nextCursor,
    final int? totalEstimate,
  }) = _$SearchResponseImpl;

  factory _SearchResponse.fromJson(Map<String, dynamic> json) =
      _$SearchResponseImpl.fromJson;

  @override
  List<SearchItem> get items;
  @override
  List<SearchFacet> get facets;
  @override
  String? get nextCursor;

  /// An estimate by contract — never a `COUNT(*)` on a hot table. The UI says "about 1,284" and
  /// never promises precision it does not have.
  @override
  int? get totalEstimate;

  /// Create a copy of SearchResponse
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SearchResponseImplCopyWith<_$SearchResponseImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
