// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'listing.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

SpecGroup _$SpecGroupFromJson(Map<String, dynamic> json) {
  return _SpecGroup.fromJson(json);
}

/// @nodoc
mixin _$SpecGroup {
  String get code => throw _privateConstructorUsedError;
  String get label => throw _privateConstructorUsedError;
  List<SpecItem> get items => throw _privateConstructorUsedError;

  /// Serializes this SpecGroup to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of SpecGroup
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $SpecGroupCopyWith<SpecGroup> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $SpecGroupCopyWith<$Res> {
  factory $SpecGroupCopyWith(SpecGroup value, $Res Function(SpecGroup) then) =
      _$SpecGroupCopyWithImpl<$Res, SpecGroup>;
  @useResult
  $Res call({String code, String label, List<SpecItem> items});
}

/// @nodoc
class _$SpecGroupCopyWithImpl<$Res, $Val extends SpecGroup>
    implements $SpecGroupCopyWith<$Res> {
  _$SpecGroupCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of SpecGroup
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? code = null, Object? label = null, Object? items = null}) {
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
            items: null == items
                ? _value.items
                : items // ignore: cast_nullable_to_non_nullable
                      as List<SpecItem>,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$SpecGroupImplCopyWith<$Res>
    implements $SpecGroupCopyWith<$Res> {
  factory _$$SpecGroupImplCopyWith(
    _$SpecGroupImpl value,
    $Res Function(_$SpecGroupImpl) then,
  ) = __$$SpecGroupImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String code, String label, List<SpecItem> items});
}

/// @nodoc
class __$$SpecGroupImplCopyWithImpl<$Res>
    extends _$SpecGroupCopyWithImpl<$Res, _$SpecGroupImpl>
    implements _$$SpecGroupImplCopyWith<$Res> {
  __$$SpecGroupImplCopyWithImpl(
    _$SpecGroupImpl _value,
    $Res Function(_$SpecGroupImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of SpecGroup
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? code = null, Object? label = null, Object? items = null}) {
    return _then(
      _$SpecGroupImpl(
        code: null == code
            ? _value.code
            : code // ignore: cast_nullable_to_non_nullable
                  as String,
        label: null == label
            ? _value.label
            : label // ignore: cast_nullable_to_non_nullable
                  as String,
        items: null == items
            ? _value._items
            : items // ignore: cast_nullable_to_non_nullable
                  as List<SpecItem>,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SpecGroupImpl implements _SpecGroup {
  const _$SpecGroupImpl({
    required this.code,
    required this.label,
    final List<SpecItem> items = const <SpecItem>[],
  }) : _items = items;

  factory _$SpecGroupImpl.fromJson(Map<String, dynamic> json) =>
      _$$SpecGroupImplFromJson(json);

  @override
  final String code;
  @override
  final String label;
  final List<SpecItem> _items;
  @override
  @JsonKey()
  List<SpecItem> get items {
    if (_items is EqualUnmodifiableListView) return _items;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_items);
  }

  @override
  String toString() {
    return 'SpecGroup(code: $code, label: $label, items: $items)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SpecGroupImpl &&
            (identical(other.code, code) || other.code == code) &&
            (identical(other.label, label) || other.label == label) &&
            const DeepCollectionEquality().equals(other._items, _items));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    code,
    label,
    const DeepCollectionEquality().hash(_items),
  );

  /// Create a copy of SpecGroup
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SpecGroupImplCopyWith<_$SpecGroupImpl> get copyWith =>
      __$$SpecGroupImplCopyWithImpl<_$SpecGroupImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$SpecGroupImplToJson(this);
  }
}

abstract class _SpecGroup implements SpecGroup {
  const factory _SpecGroup({
    required final String code,
    required final String label,
    final List<SpecItem> items,
  }) = _$SpecGroupImpl;

  factory _SpecGroup.fromJson(Map<String, dynamic> json) =
      _$SpecGroupImpl.fromJson;

  @override
  String get code;
  @override
  String get label;
  @override
  List<SpecItem> get items;

  /// Create a copy of SpecGroup
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SpecGroupImplCopyWith<_$SpecGroupImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

Seller _$SellerFromJson(Map<String, dynamic> json) {
  return _Seller.fromJson(json);
}

/// @nodoc
mixin _$Seller {
  String get id => throw _privateConstructorUsedError;
  String get displayName => throw _privateConstructorUsedError;

  /// `individual` or `dealer`. Drives which trust signals are shown, never a colour.
  String get type => throw _privateConstructorUsedError;
  String? get city => throw _privateConstructorUsedError;
  bool get isVerified => throw _privateConstructorUsedError;
  String? get memberSince => throw _privateConstructorUsedError;
  int? get listingsCount => throw _privateConstructorUsedError;
  int? get responseTimeMinutes => throw _privateConstructorUsedError;

  /// Serializes this Seller to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of Seller
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $SellerCopyWith<Seller> get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $SellerCopyWith<$Res> {
  factory $SellerCopyWith(Seller value, $Res Function(Seller) then) =
      _$SellerCopyWithImpl<$Res, Seller>;
  @useResult
  $Res call({
    String id,
    String displayName,
    String type,
    String? city,
    bool isVerified,
    String? memberSince,
    int? listingsCount,
    int? responseTimeMinutes,
  });
}

/// @nodoc
class _$SellerCopyWithImpl<$Res, $Val extends Seller>
    implements $SellerCopyWith<$Res> {
  _$SellerCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of Seller
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? displayName = null,
    Object? type = null,
    Object? city = freezed,
    Object? isVerified = null,
    Object? memberSince = freezed,
    Object? listingsCount = freezed,
    Object? responseTimeMinutes = freezed,
  }) {
    return _then(
      _value.copyWith(
            id: null == id
                ? _value.id
                : id // ignore: cast_nullable_to_non_nullable
                      as String,
            displayName: null == displayName
                ? _value.displayName
                : displayName // ignore: cast_nullable_to_non_nullable
                      as String,
            type: null == type
                ? _value.type
                : type // ignore: cast_nullable_to_non_nullable
                      as String,
            city: freezed == city
                ? _value.city
                : city // ignore: cast_nullable_to_non_nullable
                      as String?,
            isVerified: null == isVerified
                ? _value.isVerified
                : isVerified // ignore: cast_nullable_to_non_nullable
                      as bool,
            memberSince: freezed == memberSince
                ? _value.memberSince
                : memberSince // ignore: cast_nullable_to_non_nullable
                      as String?,
            listingsCount: freezed == listingsCount
                ? _value.listingsCount
                : listingsCount // ignore: cast_nullable_to_non_nullable
                      as int?,
            responseTimeMinutes: freezed == responseTimeMinutes
                ? _value.responseTimeMinutes
                : responseTimeMinutes // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$SellerImplCopyWith<$Res> implements $SellerCopyWith<$Res> {
  factory _$$SellerImplCopyWith(
    _$SellerImpl value,
    $Res Function(_$SellerImpl) then,
  ) = __$$SellerImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String id,
    String displayName,
    String type,
    String? city,
    bool isVerified,
    String? memberSince,
    int? listingsCount,
    int? responseTimeMinutes,
  });
}

/// @nodoc
class __$$SellerImplCopyWithImpl<$Res>
    extends _$SellerCopyWithImpl<$Res, _$SellerImpl>
    implements _$$SellerImplCopyWith<$Res> {
  __$$SellerImplCopyWithImpl(
    _$SellerImpl _value,
    $Res Function(_$SellerImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of Seller
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? displayName = null,
    Object? type = null,
    Object? city = freezed,
    Object? isVerified = null,
    Object? memberSince = freezed,
    Object? listingsCount = freezed,
    Object? responseTimeMinutes = freezed,
  }) {
    return _then(
      _$SellerImpl(
        id: null == id
            ? _value.id
            : id // ignore: cast_nullable_to_non_nullable
                  as String,
        displayName: null == displayName
            ? _value.displayName
            : displayName // ignore: cast_nullable_to_non_nullable
                  as String,
        type: null == type
            ? _value.type
            : type // ignore: cast_nullable_to_non_nullable
                  as String,
        city: freezed == city
            ? _value.city
            : city // ignore: cast_nullable_to_non_nullable
                  as String?,
        isVerified: null == isVerified
            ? _value.isVerified
            : isVerified // ignore: cast_nullable_to_non_nullable
                  as bool,
        memberSince: freezed == memberSince
            ? _value.memberSince
            : memberSince // ignore: cast_nullable_to_non_nullable
                  as String?,
        listingsCount: freezed == listingsCount
            ? _value.listingsCount
            : listingsCount // ignore: cast_nullable_to_non_nullable
                  as int?,
        responseTimeMinutes: freezed == responseTimeMinutes
            ? _value.responseTimeMinutes
            : responseTimeMinutes // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$SellerImpl implements _Seller {
  const _$SellerImpl({
    required this.id,
    required this.displayName,
    this.type = 'individual',
    this.city,
    this.isVerified = false,
    this.memberSince,
    this.listingsCount,
    this.responseTimeMinutes,
  });

  factory _$SellerImpl.fromJson(Map<String, dynamic> json) =>
      _$$SellerImplFromJson(json);

  @override
  final String id;
  @override
  final String displayName;

  /// `individual` or `dealer`. Drives which trust signals are shown, never a colour.
  @override
  @JsonKey()
  final String type;
  @override
  final String? city;
  @override
  @JsonKey()
  final bool isVerified;
  @override
  final String? memberSince;
  @override
  final int? listingsCount;
  @override
  final int? responseTimeMinutes;

  @override
  String toString() {
    return 'Seller(id: $id, displayName: $displayName, type: $type, city: $city, isVerified: $isVerified, memberSince: $memberSince, listingsCount: $listingsCount, responseTimeMinutes: $responseTimeMinutes)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$SellerImpl &&
            (identical(other.id, id) || other.id == id) &&
            (identical(other.displayName, displayName) ||
                other.displayName == displayName) &&
            (identical(other.type, type) || other.type == type) &&
            (identical(other.city, city) || other.city == city) &&
            (identical(other.isVerified, isVerified) ||
                other.isVerified == isVerified) &&
            (identical(other.memberSince, memberSince) ||
                other.memberSince == memberSince) &&
            (identical(other.listingsCount, listingsCount) ||
                other.listingsCount == listingsCount) &&
            (identical(other.responseTimeMinutes, responseTimeMinutes) ||
                other.responseTimeMinutes == responseTimeMinutes));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    id,
    displayName,
    type,
    city,
    isVerified,
    memberSince,
    listingsCount,
    responseTimeMinutes,
  );

  /// Create a copy of Seller
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$SellerImplCopyWith<_$SellerImpl> get copyWith =>
      __$$SellerImplCopyWithImpl<_$SellerImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$SellerImplToJson(this);
  }
}

abstract class _Seller implements Seller {
  const factory _Seller({
    required final String id,
    required final String displayName,
    final String type,
    final String? city,
    final bool isVerified,
    final String? memberSince,
    final int? listingsCount,
    final int? responseTimeMinutes,
  }) = _$SellerImpl;

  factory _Seller.fromJson(Map<String, dynamic> json) = _$SellerImpl.fromJson;

  @override
  String get id;
  @override
  String get displayName;

  /// `individual` or `dealer`. Drives which trust signals are shown, never a colour.
  @override
  String get type;
  @override
  String? get city;
  @override
  bool get isVerified;
  @override
  String? get memberSince;
  @override
  int? get listingsCount;
  @override
  int? get responseTimeMinutes;

  /// Create a copy of Seller
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$SellerImplCopyWith<_$SellerImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

ListingDetail _$ListingDetailFromJson(Map<String, dynamic> json) {
  return _ListingDetail.fromJson(json);
}

/// @nodoc
mixin _$ListingDetail {
  String get listingId => throw _privateConstructorUsedError;
  @JsonKey(unknownEnumValue: ListingIntent.sale)
  ListingIntent get intent => throw _privateConstructorUsedError;
  Registration get registration => throw _privateConstructorUsedError;
  String get title => throw _privateConstructorUsedError;
  int? get priceMinor => throw _privateConstructorUsedError;
  String? get pricePeriod => throw _privateConstructorUsedError;
  String get city => throw _privateConstructorUsedError;
  List<SpecItem> get specStrip => throw _privateConstructorUsedError;
  Media get media => throw _privateConstructorUsedError;
  List<String> get badges => throw _privateConstructorUsedError;
  String? get dealerName => throw _privateConstructorUsedError;
  String? get description => throw _privateConstructorUsedError;
  String get currency => throw _privateConstructorUsedError;
  List<MediaAsset> get gallery => throw _privateConstructorUsedError;

  /// Dynamic per-category values, keyed by taxonomy attribute code.
  Map<String, Object?> get attributes => throw _privateConstructorUsedError;
  List<SpecGroup> get specGroups => throw _privateConstructorUsedError;
  Seller? get seller => throw _privateConstructorUsedError;

  /// What this user may do to this listing right now. Read `availableActions` from here and
  /// render buttons from it; never re-derive authorisation from `state` and a permission list.
  WorkflowSnapshot? get workflow => throw _privateConstructorUsedError;
  String? get categoryCode => throw _privateConstructorUsedError;
  String? get makeCode => throw _privateConstructorUsedError;
  String? get modelCode => throw _privateConstructorUsedError;
  String? get variantCode => throw _privateConstructorUsedError;
  int? get year => throw _privateConstructorUsedError;

  /// Where the vehicle physically is, for a test drive.
  String? get locality => throw _privateConstructorUsedError;
  String? get createdAt => throw _privateConstructorUsedError;
  String? get expiresAt => throw _privateConstructorUsedError;
  int? get viewCount => throw _privateConstructorUsedError;

  /// Serializes this ListingDetail to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ListingDetail
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ListingDetailCopyWith<ListingDetail> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ListingDetailCopyWith<$Res> {
  factory $ListingDetailCopyWith(
    ListingDetail value,
    $Res Function(ListingDetail) then,
  ) = _$ListingDetailCopyWithImpl<$Res, ListingDetail>;
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
    String? description,
    String currency,
    List<MediaAsset> gallery,
    Map<String, Object?> attributes,
    List<SpecGroup> specGroups,
    Seller? seller,
    WorkflowSnapshot? workflow,
    String? categoryCode,
    String? makeCode,
    String? modelCode,
    String? variantCode,
    int? year,
    String? locality,
    String? createdAt,
    String? expiresAt,
    int? viewCount,
  });

  $RegistrationCopyWith<$Res> get registration;
  $MediaCopyWith<$Res> get media;
  $SellerCopyWith<$Res>? get seller;
  $WorkflowSnapshotCopyWith<$Res>? get workflow;
}

/// @nodoc
class _$ListingDetailCopyWithImpl<$Res, $Val extends ListingDetail>
    implements $ListingDetailCopyWith<$Res> {
  _$ListingDetailCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ListingDetail
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
    Object? description = freezed,
    Object? currency = null,
    Object? gallery = null,
    Object? attributes = null,
    Object? specGroups = null,
    Object? seller = freezed,
    Object? workflow = freezed,
    Object? categoryCode = freezed,
    Object? makeCode = freezed,
    Object? modelCode = freezed,
    Object? variantCode = freezed,
    Object? year = freezed,
    Object? locality = freezed,
    Object? createdAt = freezed,
    Object? expiresAt = freezed,
    Object? viewCount = freezed,
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
            description: freezed == description
                ? _value.description
                : description // ignore: cast_nullable_to_non_nullable
                      as String?,
            currency: null == currency
                ? _value.currency
                : currency // ignore: cast_nullable_to_non_nullable
                      as String,
            gallery: null == gallery
                ? _value.gallery
                : gallery // ignore: cast_nullable_to_non_nullable
                      as List<MediaAsset>,
            attributes: null == attributes
                ? _value.attributes
                : attributes // ignore: cast_nullable_to_non_nullable
                      as Map<String, Object?>,
            specGroups: null == specGroups
                ? _value.specGroups
                : specGroups // ignore: cast_nullable_to_non_nullable
                      as List<SpecGroup>,
            seller: freezed == seller
                ? _value.seller
                : seller // ignore: cast_nullable_to_non_nullable
                      as Seller?,
            workflow: freezed == workflow
                ? _value.workflow
                : workflow // ignore: cast_nullable_to_non_nullable
                      as WorkflowSnapshot?,
            categoryCode: freezed == categoryCode
                ? _value.categoryCode
                : categoryCode // ignore: cast_nullable_to_non_nullable
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
            locality: freezed == locality
                ? _value.locality
                : locality // ignore: cast_nullable_to_non_nullable
                      as String?,
            createdAt: freezed == createdAt
                ? _value.createdAt
                : createdAt // ignore: cast_nullable_to_non_nullable
                      as String?,
            expiresAt: freezed == expiresAt
                ? _value.expiresAt
                : expiresAt // ignore: cast_nullable_to_non_nullable
                      as String?,
            viewCount: freezed == viewCount
                ? _value.viewCount
                : viewCount // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }

  /// Create a copy of ListingDetail
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $RegistrationCopyWith<$Res> get registration {
    return $RegistrationCopyWith<$Res>(_value.registration, (value) {
      return _then(_value.copyWith(registration: value) as $Val);
    });
  }

  /// Create a copy of ListingDetail
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $MediaCopyWith<$Res> get media {
    return $MediaCopyWith<$Res>(_value.media, (value) {
      return _then(_value.copyWith(media: value) as $Val);
    });
  }

  /// Create a copy of ListingDetail
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $SellerCopyWith<$Res>? get seller {
    if (_value.seller == null) {
      return null;
    }

    return $SellerCopyWith<$Res>(_value.seller!, (value) {
      return _then(_value.copyWith(seller: value) as $Val);
    });
  }

  /// Create a copy of ListingDetail
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $WorkflowSnapshotCopyWith<$Res>? get workflow {
    if (_value.workflow == null) {
      return null;
    }

    return $WorkflowSnapshotCopyWith<$Res>(_value.workflow!, (value) {
      return _then(_value.copyWith(workflow: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$ListingDetailImplCopyWith<$Res>
    implements $ListingDetailCopyWith<$Res> {
  factory _$$ListingDetailImplCopyWith(
    _$ListingDetailImpl value,
    $Res Function(_$ListingDetailImpl) then,
  ) = __$$ListingDetailImplCopyWithImpl<$Res>;
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
    String? description,
    String currency,
    List<MediaAsset> gallery,
    Map<String, Object?> attributes,
    List<SpecGroup> specGroups,
    Seller? seller,
    WorkflowSnapshot? workflow,
    String? categoryCode,
    String? makeCode,
    String? modelCode,
    String? variantCode,
    int? year,
    String? locality,
    String? createdAt,
    String? expiresAt,
    int? viewCount,
  });

  @override
  $RegistrationCopyWith<$Res> get registration;
  @override
  $MediaCopyWith<$Res> get media;
  @override
  $SellerCopyWith<$Res>? get seller;
  @override
  $WorkflowSnapshotCopyWith<$Res>? get workflow;
}

/// @nodoc
class __$$ListingDetailImplCopyWithImpl<$Res>
    extends _$ListingDetailCopyWithImpl<$Res, _$ListingDetailImpl>
    implements _$$ListingDetailImplCopyWith<$Res> {
  __$$ListingDetailImplCopyWithImpl(
    _$ListingDetailImpl _value,
    $Res Function(_$ListingDetailImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ListingDetail
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
    Object? description = freezed,
    Object? currency = null,
    Object? gallery = null,
    Object? attributes = null,
    Object? specGroups = null,
    Object? seller = freezed,
    Object? workflow = freezed,
    Object? categoryCode = freezed,
    Object? makeCode = freezed,
    Object? modelCode = freezed,
    Object? variantCode = freezed,
    Object? year = freezed,
    Object? locality = freezed,
    Object? createdAt = freezed,
    Object? expiresAt = freezed,
    Object? viewCount = freezed,
  }) {
    return _then(
      _$ListingDetailImpl(
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
        description: freezed == description
            ? _value.description
            : description // ignore: cast_nullable_to_non_nullable
                  as String?,
        currency: null == currency
            ? _value.currency
            : currency // ignore: cast_nullable_to_non_nullable
                  as String,
        gallery: null == gallery
            ? _value._gallery
            : gallery // ignore: cast_nullable_to_non_nullable
                  as List<MediaAsset>,
        attributes: null == attributes
            ? _value._attributes
            : attributes // ignore: cast_nullable_to_non_nullable
                  as Map<String, Object?>,
        specGroups: null == specGroups
            ? _value._specGroups
            : specGroups // ignore: cast_nullable_to_non_nullable
                  as List<SpecGroup>,
        seller: freezed == seller
            ? _value.seller
            : seller // ignore: cast_nullable_to_non_nullable
                  as Seller?,
        workflow: freezed == workflow
            ? _value.workflow
            : workflow // ignore: cast_nullable_to_non_nullable
                  as WorkflowSnapshot?,
        categoryCode: freezed == categoryCode
            ? _value.categoryCode
            : categoryCode // ignore: cast_nullable_to_non_nullable
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
        locality: freezed == locality
            ? _value.locality
            : locality // ignore: cast_nullable_to_non_nullable
                  as String?,
        createdAt: freezed == createdAt
            ? _value.createdAt
            : createdAt // ignore: cast_nullable_to_non_nullable
                  as String?,
        expiresAt: freezed == expiresAt
            ? _value.expiresAt
            : expiresAt // ignore: cast_nullable_to_non_nullable
                  as String?,
        viewCount: freezed == viewCount
            ? _value.viewCount
            : viewCount // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ListingDetailImpl implements _ListingDetail {
  const _$ListingDetailImpl({
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
    this.description,
    this.currency = 'INR',
    final List<MediaAsset> gallery = const <MediaAsset>[],
    final Map<String, Object?> attributes = const <String, Object?>{},
    final List<SpecGroup> specGroups = const <SpecGroup>[],
    this.seller,
    this.workflow,
    this.categoryCode,
    this.makeCode,
    this.modelCode,
    this.variantCode,
    this.year,
    this.locality,
    this.createdAt,
    this.expiresAt,
    this.viewCount,
  }) : _specStrip = specStrip,
       _badges = badges,
       _gallery = gallery,
       _attributes = attributes,
       _specGroups = specGroups;

  factory _$ListingDetailImpl.fromJson(Map<String, dynamic> json) =>
      _$$ListingDetailImplFromJson(json);

  @override
  final String listingId;
  @override
  @JsonKey(unknownEnumValue: ListingIntent.sale)
  final ListingIntent intent;
  @override
  final Registration registration;
  @override
  final String title;
  @override
  final int? priceMinor;
  @override
  final String? pricePeriod;
  @override
  @JsonKey()
  final String city;
  final List<SpecItem> _specStrip;
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
  final List<String> _badges;
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
  final String? description;
  @override
  @JsonKey()
  final String currency;
  final List<MediaAsset> _gallery;
  @override
  @JsonKey()
  List<MediaAsset> get gallery {
    if (_gallery is EqualUnmodifiableListView) return _gallery;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_gallery);
  }

  /// Dynamic per-category values, keyed by taxonomy attribute code.
  final Map<String, Object?> _attributes;

  /// Dynamic per-category values, keyed by taxonomy attribute code.
  @override
  @JsonKey()
  Map<String, Object?> get attributes {
    if (_attributes is EqualUnmodifiableMapView) return _attributes;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableMapView(_attributes);
  }

  final List<SpecGroup> _specGroups;
  @override
  @JsonKey()
  List<SpecGroup> get specGroups {
    if (_specGroups is EqualUnmodifiableListView) return _specGroups;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_specGroups);
  }

  @override
  final Seller? seller;

  /// What this user may do to this listing right now. Read `availableActions` from here and
  /// render buttons from it; never re-derive authorisation from `state` and a permission list.
  @override
  final WorkflowSnapshot? workflow;
  @override
  final String? categoryCode;
  @override
  final String? makeCode;
  @override
  final String? modelCode;
  @override
  final String? variantCode;
  @override
  final int? year;

  /// Where the vehicle physically is, for a test drive.
  @override
  final String? locality;
  @override
  final String? createdAt;
  @override
  final String? expiresAt;
  @override
  final int? viewCount;

  @override
  String toString() {
    return 'ListingDetail(listingId: $listingId, intent: $intent, registration: $registration, title: $title, priceMinor: $priceMinor, pricePeriod: $pricePeriod, city: $city, specStrip: $specStrip, media: $media, badges: $badges, dealerName: $dealerName, description: $description, currency: $currency, gallery: $gallery, attributes: $attributes, specGroups: $specGroups, seller: $seller, workflow: $workflow, categoryCode: $categoryCode, makeCode: $makeCode, modelCode: $modelCode, variantCode: $variantCode, year: $year, locality: $locality, createdAt: $createdAt, expiresAt: $expiresAt, viewCount: $viewCount)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ListingDetailImpl &&
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
                other.dealerName == dealerName) &&
            (identical(other.description, description) ||
                other.description == description) &&
            (identical(other.currency, currency) ||
                other.currency == currency) &&
            const DeepCollectionEquality().equals(other._gallery, _gallery) &&
            const DeepCollectionEquality().equals(
              other._attributes,
              _attributes,
            ) &&
            const DeepCollectionEquality().equals(
              other._specGroups,
              _specGroups,
            ) &&
            (identical(other.seller, seller) || other.seller == seller) &&
            (identical(other.workflow, workflow) ||
                other.workflow == workflow) &&
            (identical(other.categoryCode, categoryCode) ||
                other.categoryCode == categoryCode) &&
            (identical(other.makeCode, makeCode) ||
                other.makeCode == makeCode) &&
            (identical(other.modelCode, modelCode) ||
                other.modelCode == modelCode) &&
            (identical(other.variantCode, variantCode) ||
                other.variantCode == variantCode) &&
            (identical(other.year, year) || other.year == year) &&
            (identical(other.locality, locality) ||
                other.locality == locality) &&
            (identical(other.createdAt, createdAt) ||
                other.createdAt == createdAt) &&
            (identical(other.expiresAt, expiresAt) ||
                other.expiresAt == expiresAt) &&
            (identical(other.viewCount, viewCount) ||
                other.viewCount == viewCount));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hashAll([
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
    description,
    currency,
    const DeepCollectionEquality().hash(_gallery),
    const DeepCollectionEquality().hash(_attributes),
    const DeepCollectionEquality().hash(_specGroups),
    seller,
    workflow,
    categoryCode,
    makeCode,
    modelCode,
    variantCode,
    year,
    locality,
    createdAt,
    expiresAt,
    viewCount,
  ]);

  /// Create a copy of ListingDetail
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ListingDetailImplCopyWith<_$ListingDetailImpl> get copyWith =>
      __$$ListingDetailImplCopyWithImpl<_$ListingDetailImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$ListingDetailImplToJson(this);
  }
}

abstract class _ListingDetail implements ListingDetail {
  const factory _ListingDetail({
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
    final String? description,
    final String currency,
    final List<MediaAsset> gallery,
    final Map<String, Object?> attributes,
    final List<SpecGroup> specGroups,
    final Seller? seller,
    final WorkflowSnapshot? workflow,
    final String? categoryCode,
    final String? makeCode,
    final String? modelCode,
    final String? variantCode,
    final int? year,
    final String? locality,
    final String? createdAt,
    final String? expiresAt,
    final int? viewCount,
  }) = _$ListingDetailImpl;

  factory _ListingDetail.fromJson(Map<String, dynamic> json) =
      _$ListingDetailImpl.fromJson;

  @override
  String get listingId;
  @override
  @JsonKey(unknownEnumValue: ListingIntent.sale)
  ListingIntent get intent;
  @override
  Registration get registration;
  @override
  String get title;
  @override
  int? get priceMinor;
  @override
  String? get pricePeriod;
  @override
  String get city;
  @override
  List<SpecItem> get specStrip;
  @override
  Media get media;
  @override
  List<String> get badges;
  @override
  String? get dealerName;
  @override
  String? get description;
  @override
  String get currency;
  @override
  List<MediaAsset> get gallery;

  /// Dynamic per-category values, keyed by taxonomy attribute code.
  @override
  Map<String, Object?> get attributes;
  @override
  List<SpecGroup> get specGroups;
  @override
  Seller? get seller;

  /// What this user may do to this listing right now. Read `availableActions` from here and
  /// render buttons from it; never re-derive authorisation from `state` and a permission list.
  @override
  WorkflowSnapshot? get workflow;
  @override
  String? get categoryCode;
  @override
  String? get makeCode;
  @override
  String? get modelCode;
  @override
  String? get variantCode;
  @override
  int? get year;

  /// Where the vehicle physically is, for a test drive.
  @override
  String? get locality;
  @override
  String? get createdAt;
  @override
  String? get expiresAt;
  @override
  int? get viewCount;

  /// Create a copy of ListingDetail
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ListingDetailImplCopyWith<_$ListingDetailImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

ListingDraftRequest _$ListingDraftRequestFromJson(Map<String, dynamic> json) {
  return _ListingDraftRequest.fromJson(json);
}

/// @nodoc
mixin _$ListingDraftRequest {
  ListingIntent get intent => throw _privateConstructorUsedError;
  String get categoryCode => throw _privateConstructorUsedError;
  String? get registrationNumber => throw _privateConstructorUsedError;

  /// The sell form's own payload, validated against the form registry — deliberately untyped.
  /// Typing it would put the registry's field list in the binary, which is the one thing this
  /// architecture exists to avoid.
  Map<String, Object?> get values => throw _privateConstructorUsedError;
  int? get askingPriceMinor => throw _privateConstructorUsedError;

  /// Serializes this ListingDraftRequest to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ListingDraftRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ListingDraftRequestCopyWith<ListingDraftRequest> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ListingDraftRequestCopyWith<$Res> {
  factory $ListingDraftRequestCopyWith(
    ListingDraftRequest value,
    $Res Function(ListingDraftRequest) then,
  ) = _$ListingDraftRequestCopyWithImpl<$Res, ListingDraftRequest>;
  @useResult
  $Res call({
    ListingIntent intent,
    String categoryCode,
    String? registrationNumber,
    Map<String, Object?> values,
    int? askingPriceMinor,
  });
}

/// @nodoc
class _$ListingDraftRequestCopyWithImpl<$Res, $Val extends ListingDraftRequest>
    implements $ListingDraftRequestCopyWith<$Res> {
  _$ListingDraftRequestCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ListingDraftRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? intent = null,
    Object? categoryCode = null,
    Object? registrationNumber = freezed,
    Object? values = null,
    Object? askingPriceMinor = freezed,
  }) {
    return _then(
      _value.copyWith(
            intent: null == intent
                ? _value.intent
                : intent // ignore: cast_nullable_to_non_nullable
                      as ListingIntent,
            categoryCode: null == categoryCode
                ? _value.categoryCode
                : categoryCode // ignore: cast_nullable_to_non_nullable
                      as String,
            registrationNumber: freezed == registrationNumber
                ? _value.registrationNumber
                : registrationNumber // ignore: cast_nullable_to_non_nullable
                      as String?,
            values: null == values
                ? _value.values
                : values // ignore: cast_nullable_to_non_nullable
                      as Map<String, Object?>,
            askingPriceMinor: freezed == askingPriceMinor
                ? _value.askingPriceMinor
                : askingPriceMinor // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$ListingDraftRequestImplCopyWith<$Res>
    implements $ListingDraftRequestCopyWith<$Res> {
  factory _$$ListingDraftRequestImplCopyWith(
    _$ListingDraftRequestImpl value,
    $Res Function(_$ListingDraftRequestImpl) then,
  ) = __$$ListingDraftRequestImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    ListingIntent intent,
    String categoryCode,
    String? registrationNumber,
    Map<String, Object?> values,
    int? askingPriceMinor,
  });
}

/// @nodoc
class __$$ListingDraftRequestImplCopyWithImpl<$Res>
    extends _$ListingDraftRequestCopyWithImpl<$Res, _$ListingDraftRequestImpl>
    implements _$$ListingDraftRequestImplCopyWith<$Res> {
  __$$ListingDraftRequestImplCopyWithImpl(
    _$ListingDraftRequestImpl _value,
    $Res Function(_$ListingDraftRequestImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of ListingDraftRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? intent = null,
    Object? categoryCode = null,
    Object? registrationNumber = freezed,
    Object? values = null,
    Object? askingPriceMinor = freezed,
  }) {
    return _then(
      _$ListingDraftRequestImpl(
        intent: null == intent
            ? _value.intent
            : intent // ignore: cast_nullable_to_non_nullable
                  as ListingIntent,
        categoryCode: null == categoryCode
            ? _value.categoryCode
            : categoryCode // ignore: cast_nullable_to_non_nullable
                  as String,
        registrationNumber: freezed == registrationNumber
            ? _value.registrationNumber
            : registrationNumber // ignore: cast_nullable_to_non_nullable
                  as String?,
        values: null == values
            ? _value._values
            : values // ignore: cast_nullable_to_non_nullable
                  as Map<String, Object?>,
        askingPriceMinor: freezed == askingPriceMinor
            ? _value.askingPriceMinor
            : askingPriceMinor // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$ListingDraftRequestImpl implements _ListingDraftRequest {
  const _$ListingDraftRequestImpl({
    required this.intent,
    required this.categoryCode,
    this.registrationNumber,
    final Map<String, Object?> values = const <String, Object?>{},
    this.askingPriceMinor,
  }) : _values = values;

  factory _$ListingDraftRequestImpl.fromJson(Map<String, dynamic> json) =>
      _$$ListingDraftRequestImplFromJson(json);

  @override
  final ListingIntent intent;
  @override
  final String categoryCode;
  @override
  final String? registrationNumber;

  /// The sell form's own payload, validated against the form registry — deliberately untyped.
  /// Typing it would put the registry's field list in the binary, which is the one thing this
  /// architecture exists to avoid.
  final Map<String, Object?> _values;

  /// The sell form's own payload, validated against the form registry — deliberately untyped.
  /// Typing it would put the registry's field list in the binary, which is the one thing this
  /// architecture exists to avoid.
  @override
  @JsonKey()
  Map<String, Object?> get values {
    if (_values is EqualUnmodifiableMapView) return _values;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableMapView(_values);
  }

  @override
  final int? askingPriceMinor;

  @override
  String toString() {
    return 'ListingDraftRequest(intent: $intent, categoryCode: $categoryCode, registrationNumber: $registrationNumber, values: $values, askingPriceMinor: $askingPriceMinor)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ListingDraftRequestImpl &&
            (identical(other.intent, intent) || other.intent == intent) &&
            (identical(other.categoryCode, categoryCode) ||
                other.categoryCode == categoryCode) &&
            (identical(other.registrationNumber, registrationNumber) ||
                other.registrationNumber == registrationNumber) &&
            const DeepCollectionEquality().equals(other._values, _values) &&
            (identical(other.askingPriceMinor, askingPriceMinor) ||
                other.askingPriceMinor == askingPriceMinor));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    intent,
    categoryCode,
    registrationNumber,
    const DeepCollectionEquality().hash(_values),
    askingPriceMinor,
  );

  /// Create a copy of ListingDraftRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ListingDraftRequestImplCopyWith<_$ListingDraftRequestImpl> get copyWith =>
      __$$ListingDraftRequestImplCopyWithImpl<_$ListingDraftRequestImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$ListingDraftRequestImplToJson(this);
  }
}

abstract class _ListingDraftRequest implements ListingDraftRequest {
  const factory _ListingDraftRequest({
    required final ListingIntent intent,
    required final String categoryCode,
    final String? registrationNumber,
    final Map<String, Object?> values,
    final int? askingPriceMinor,
  }) = _$ListingDraftRequestImpl;

  factory _ListingDraftRequest.fromJson(Map<String, dynamic> json) =
      _$ListingDraftRequestImpl.fromJson;

  @override
  ListingIntent get intent;
  @override
  String get categoryCode;
  @override
  String? get registrationNumber;

  /// The sell form's own payload, validated against the form registry — deliberately untyped.
  /// Typing it would put the registry's field list in the binary, which is the one thing this
  /// architecture exists to avoid.
  @override
  Map<String, Object?> get values;
  @override
  int? get askingPriceMinor;

  /// Create a copy of ListingDraftRequest
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ListingDraftRequestImplCopyWith<_$ListingDraftRequestImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
