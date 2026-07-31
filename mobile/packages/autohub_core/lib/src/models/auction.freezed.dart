// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'auction.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
  'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models',
);

Bid _$BidFromJson(Map<String, dynamic> json) {
  return _Bid.fromJson(json);
}

/// @nodoc
mixin _$Bid {
  String get bidId => throw _privateConstructorUsedError;
  String get lotId => throw _privateConstructorUsedError;

  /// Monotonic per lot. The client's reconnect anchor and its stale-bid guard.
  int get seq => throw _privateConstructorUsedError;
  int get amountMinor => throw _privateConstructorUsedError;

  /// `Dealer ••42` — bidders are masked from each other.
  String get bidderMask => throw _privateConstructorUsedError;
  String get placedAt => throw _privateConstructorUsedError;
  bool get isYours => throw _privateConstructorUsedError;
  bool get isAutoBid => throw _privateConstructorUsedError;

  /// Serializes this Bid to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of Bid
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $BidCopyWith<Bid> get copyWith => throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $BidCopyWith<$Res> {
  factory $BidCopyWith(Bid value, $Res Function(Bid) then) =
      _$BidCopyWithImpl<$Res, Bid>;
  @useResult
  $Res call({
    String bidId,
    String lotId,
    int seq,
    int amountMinor,
    String bidderMask,
    String placedAt,
    bool isYours,
    bool isAutoBid,
  });
}

/// @nodoc
class _$BidCopyWithImpl<$Res, $Val extends Bid> implements $BidCopyWith<$Res> {
  _$BidCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of Bid
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? bidId = null,
    Object? lotId = null,
    Object? seq = null,
    Object? amountMinor = null,
    Object? bidderMask = null,
    Object? placedAt = null,
    Object? isYours = null,
    Object? isAutoBid = null,
  }) {
    return _then(
      _value.copyWith(
            bidId: null == bidId
                ? _value.bidId
                : bidId // ignore: cast_nullable_to_non_nullable
                      as String,
            lotId: null == lotId
                ? _value.lotId
                : lotId // ignore: cast_nullable_to_non_nullable
                      as String,
            seq: null == seq
                ? _value.seq
                : seq // ignore: cast_nullable_to_non_nullable
                      as int,
            amountMinor: null == amountMinor
                ? _value.amountMinor
                : amountMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            bidderMask: null == bidderMask
                ? _value.bidderMask
                : bidderMask // ignore: cast_nullable_to_non_nullable
                      as String,
            placedAt: null == placedAt
                ? _value.placedAt
                : placedAt // ignore: cast_nullable_to_non_nullable
                      as String,
            isYours: null == isYours
                ? _value.isYours
                : isYours // ignore: cast_nullable_to_non_nullable
                      as bool,
            isAutoBid: null == isAutoBid
                ? _value.isAutoBid
                : isAutoBid // ignore: cast_nullable_to_non_nullable
                      as bool,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$BidImplCopyWith<$Res> implements $BidCopyWith<$Res> {
  factory _$$BidImplCopyWith(_$BidImpl value, $Res Function(_$BidImpl) then) =
      __$$BidImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String bidId,
    String lotId,
    int seq,
    int amountMinor,
    String bidderMask,
    String placedAt,
    bool isYours,
    bool isAutoBid,
  });
}

/// @nodoc
class __$$BidImplCopyWithImpl<$Res> extends _$BidCopyWithImpl<$Res, _$BidImpl>
    implements _$$BidImplCopyWith<$Res> {
  __$$BidImplCopyWithImpl(_$BidImpl _value, $Res Function(_$BidImpl) _then)
    : super(_value, _then);

  /// Create a copy of Bid
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? bidId = null,
    Object? lotId = null,
    Object? seq = null,
    Object? amountMinor = null,
    Object? bidderMask = null,
    Object? placedAt = null,
    Object? isYours = null,
    Object? isAutoBid = null,
  }) {
    return _then(
      _$BidImpl(
        bidId: null == bidId
            ? _value.bidId
            : bidId // ignore: cast_nullable_to_non_nullable
                  as String,
        lotId: null == lotId
            ? _value.lotId
            : lotId // ignore: cast_nullable_to_non_nullable
                  as String,
        seq: null == seq
            ? _value.seq
            : seq // ignore: cast_nullable_to_non_nullable
                  as int,
        amountMinor: null == amountMinor
            ? _value.amountMinor
            : amountMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        bidderMask: null == bidderMask
            ? _value.bidderMask
            : bidderMask // ignore: cast_nullable_to_non_nullable
                  as String,
        placedAt: null == placedAt
            ? _value.placedAt
            : placedAt // ignore: cast_nullable_to_non_nullable
                  as String,
        isYours: null == isYours
            ? _value.isYours
            : isYours // ignore: cast_nullable_to_non_nullable
                  as bool,
        isAutoBid: null == isAutoBid
            ? _value.isAutoBid
            : isAutoBid // ignore: cast_nullable_to_non_nullable
                  as bool,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$BidImpl implements _Bid {
  const _$BidImpl({
    required this.bidId,
    required this.lotId,
    this.seq = 0,
    required this.amountMinor,
    this.bidderMask = '',
    required this.placedAt,
    this.isYours = false,
    this.isAutoBid = false,
  });

  factory _$BidImpl.fromJson(Map<String, dynamic> json) =>
      _$$BidImplFromJson(json);

  @override
  final String bidId;
  @override
  final String lotId;

  /// Monotonic per lot. The client's reconnect anchor and its stale-bid guard.
  @override
  @JsonKey()
  final int seq;
  @override
  final int amountMinor;

  /// `Dealer ••42` — bidders are masked from each other.
  @override
  @JsonKey()
  final String bidderMask;
  @override
  final String placedAt;
  @override
  @JsonKey()
  final bool isYours;
  @override
  @JsonKey()
  final bool isAutoBid;

  @override
  String toString() {
    return 'Bid(bidId: $bidId, lotId: $lotId, seq: $seq, amountMinor: $amountMinor, bidderMask: $bidderMask, placedAt: $placedAt, isYours: $isYours, isAutoBid: $isAutoBid)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$BidImpl &&
            (identical(other.bidId, bidId) || other.bidId == bidId) &&
            (identical(other.lotId, lotId) || other.lotId == lotId) &&
            (identical(other.seq, seq) || other.seq == seq) &&
            (identical(other.amountMinor, amountMinor) ||
                other.amountMinor == amountMinor) &&
            (identical(other.bidderMask, bidderMask) ||
                other.bidderMask == bidderMask) &&
            (identical(other.placedAt, placedAt) ||
                other.placedAt == placedAt) &&
            (identical(other.isYours, isYours) || other.isYours == isYours) &&
            (identical(other.isAutoBid, isAutoBid) ||
                other.isAutoBid == isAutoBid));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    bidId,
    lotId,
    seq,
    amountMinor,
    bidderMask,
    placedAt,
    isYours,
    isAutoBid,
  );

  /// Create a copy of Bid
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$BidImplCopyWith<_$BidImpl> get copyWith =>
      __$$BidImplCopyWithImpl<_$BidImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$BidImplToJson(this);
  }
}

abstract class _Bid implements Bid {
  const factory _Bid({
    required final String bidId,
    required final String lotId,
    final int seq,
    required final int amountMinor,
    final String bidderMask,
    required final String placedAt,
    final bool isYours,
    final bool isAutoBid,
  }) = _$BidImpl;

  factory _Bid.fromJson(Map<String, dynamic> json) = _$BidImpl.fromJson;

  @override
  String get bidId;
  @override
  String get lotId;

  /// Monotonic per lot. The client's reconnect anchor and its stale-bid guard.
  @override
  int get seq;
  @override
  int get amountMinor;

  /// `Dealer ••42` — bidders are masked from each other.
  @override
  String get bidderMask;
  @override
  String get placedAt;
  @override
  bool get isYours;
  @override
  bool get isAutoBid;

  /// Create a copy of Bid
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$BidImplCopyWith<_$BidImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

AuctionLot _$AuctionLotFromJson(Map<String, dynamic> json) {
  return _AuctionLot.fromJson(json);
}

/// @nodoc
mixin _$AuctionLot {
  String get lotId => throw _privateConstructorUsedError;
  String? get lotNumber => throw _privateConstructorUsedError;
  SearchItem get listing => throw _privateConstructorUsedError;
  String get currency => throw _privateConstructorUsedError;
  WorkflowState get state => throw _privateConstructorUsedError;
  String get startsAt => throw _privateConstructorUsedError;

  /// The server's closing time. Render the countdown against the offset measured at connect,
  /// never against the handset clock — a phone two minutes fast shows a lot as closed while it is
  /// still taking bids, and the dealer stops bidding on a truck they were winning.
  String get endsAt => throw _privateConstructorUsedError;
  int get startPriceMinor => throw _privateConstructorUsedError;

  /// The ladder step. The next legal bid is `currentBidMinor + incrementMinor`.
  int get incrementMinor => throw _privateConstructorUsedError;
  int? get currentBidMinor => throw _privateConstructorUsedError;
  int get minNextBidMinor => throw _privateConstructorUsedError;
  int get bidCount => throw _privateConstructorUsedError;

  /// The reserve *amount* is never exposed — only whether it has been met.
  bool get reserveMet => throw _privateConstructorUsedError;

  /// Highest sequence broadcast so far. Sent back as `seenBidSeq` to detect an outbid race.
  int get latestSeq => throw _privateConstructorUsedError;
  bool get highestBidderIsYou => throw _privateConstructorUsedError;
  int? get yourMaxBidMinor => throw _privateConstructorUsedError;

  /// Anti-snipe extensions already spent.
  int get extensionCount => throw _privateConstructorUsedError;
  String? get inspectionReportUrl => throw _privateConstructorUsedError;

  /// Serializes this AuctionLot to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of AuctionLot
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $AuctionLotCopyWith<AuctionLot> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $AuctionLotCopyWith<$Res> {
  factory $AuctionLotCopyWith(
    AuctionLot value,
    $Res Function(AuctionLot) then,
  ) = _$AuctionLotCopyWithImpl<$Res, AuctionLot>;
  @useResult
  $Res call({
    String lotId,
    String? lotNumber,
    SearchItem listing,
    String currency,
    WorkflowState state,
    String startsAt,
    String endsAt,
    int startPriceMinor,
    int incrementMinor,
    int? currentBidMinor,
    int minNextBidMinor,
    int bidCount,
    bool reserveMet,
    int latestSeq,
    bool highestBidderIsYou,
    int? yourMaxBidMinor,
    int extensionCount,
    String? inspectionReportUrl,
  });

  $SearchItemCopyWith<$Res> get listing;
  $WorkflowStateCopyWith<$Res> get state;
}

/// @nodoc
class _$AuctionLotCopyWithImpl<$Res, $Val extends AuctionLot>
    implements $AuctionLotCopyWith<$Res> {
  _$AuctionLotCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of AuctionLot
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? lotId = null,
    Object? lotNumber = freezed,
    Object? listing = null,
    Object? currency = null,
    Object? state = null,
    Object? startsAt = null,
    Object? endsAt = null,
    Object? startPriceMinor = null,
    Object? incrementMinor = null,
    Object? currentBidMinor = freezed,
    Object? minNextBidMinor = null,
    Object? bidCount = null,
    Object? reserveMet = null,
    Object? latestSeq = null,
    Object? highestBidderIsYou = null,
    Object? yourMaxBidMinor = freezed,
    Object? extensionCount = null,
    Object? inspectionReportUrl = freezed,
  }) {
    return _then(
      _value.copyWith(
            lotId: null == lotId
                ? _value.lotId
                : lotId // ignore: cast_nullable_to_non_nullable
                      as String,
            lotNumber: freezed == lotNumber
                ? _value.lotNumber
                : lotNumber // ignore: cast_nullable_to_non_nullable
                      as String?,
            listing: null == listing
                ? _value.listing
                : listing // ignore: cast_nullable_to_non_nullable
                      as SearchItem,
            currency: null == currency
                ? _value.currency
                : currency // ignore: cast_nullable_to_non_nullable
                      as String,
            state: null == state
                ? _value.state
                : state // ignore: cast_nullable_to_non_nullable
                      as WorkflowState,
            startsAt: null == startsAt
                ? _value.startsAt
                : startsAt // ignore: cast_nullable_to_non_nullable
                      as String,
            endsAt: null == endsAt
                ? _value.endsAt
                : endsAt // ignore: cast_nullable_to_non_nullable
                      as String,
            startPriceMinor: null == startPriceMinor
                ? _value.startPriceMinor
                : startPriceMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            incrementMinor: null == incrementMinor
                ? _value.incrementMinor
                : incrementMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            currentBidMinor: freezed == currentBidMinor
                ? _value.currentBidMinor
                : currentBidMinor // ignore: cast_nullable_to_non_nullable
                      as int?,
            minNextBidMinor: null == minNextBidMinor
                ? _value.minNextBidMinor
                : minNextBidMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            bidCount: null == bidCount
                ? _value.bidCount
                : bidCount // ignore: cast_nullable_to_non_nullable
                      as int,
            reserveMet: null == reserveMet
                ? _value.reserveMet
                : reserveMet // ignore: cast_nullable_to_non_nullable
                      as bool,
            latestSeq: null == latestSeq
                ? _value.latestSeq
                : latestSeq // ignore: cast_nullable_to_non_nullable
                      as int,
            highestBidderIsYou: null == highestBidderIsYou
                ? _value.highestBidderIsYou
                : highestBidderIsYou // ignore: cast_nullable_to_non_nullable
                      as bool,
            yourMaxBidMinor: freezed == yourMaxBidMinor
                ? _value.yourMaxBidMinor
                : yourMaxBidMinor // ignore: cast_nullable_to_non_nullable
                      as int?,
            extensionCount: null == extensionCount
                ? _value.extensionCount
                : extensionCount // ignore: cast_nullable_to_non_nullable
                      as int,
            inspectionReportUrl: freezed == inspectionReportUrl
                ? _value.inspectionReportUrl
                : inspectionReportUrl // ignore: cast_nullable_to_non_nullable
                      as String?,
          )
          as $Val,
    );
  }

  /// Create a copy of AuctionLot
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $SearchItemCopyWith<$Res> get listing {
    return $SearchItemCopyWith<$Res>(_value.listing, (value) {
      return _then(_value.copyWith(listing: value) as $Val);
    });
  }

  /// Create a copy of AuctionLot
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $WorkflowStateCopyWith<$Res> get state {
    return $WorkflowStateCopyWith<$Res>(_value.state, (value) {
      return _then(_value.copyWith(state: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$AuctionLotImplCopyWith<$Res>
    implements $AuctionLotCopyWith<$Res> {
  factory _$$AuctionLotImplCopyWith(
    _$AuctionLotImpl value,
    $Res Function(_$AuctionLotImpl) then,
  ) = __$$AuctionLotImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String lotId,
    String? lotNumber,
    SearchItem listing,
    String currency,
    WorkflowState state,
    String startsAt,
    String endsAt,
    int startPriceMinor,
    int incrementMinor,
    int? currentBidMinor,
    int minNextBidMinor,
    int bidCount,
    bool reserveMet,
    int latestSeq,
    bool highestBidderIsYou,
    int? yourMaxBidMinor,
    int extensionCount,
    String? inspectionReportUrl,
  });

  @override
  $SearchItemCopyWith<$Res> get listing;
  @override
  $WorkflowStateCopyWith<$Res> get state;
}

/// @nodoc
class __$$AuctionLotImplCopyWithImpl<$Res>
    extends _$AuctionLotCopyWithImpl<$Res, _$AuctionLotImpl>
    implements _$$AuctionLotImplCopyWith<$Res> {
  __$$AuctionLotImplCopyWithImpl(
    _$AuctionLotImpl _value,
    $Res Function(_$AuctionLotImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AuctionLot
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? lotId = null,
    Object? lotNumber = freezed,
    Object? listing = null,
    Object? currency = null,
    Object? state = null,
    Object? startsAt = null,
    Object? endsAt = null,
    Object? startPriceMinor = null,
    Object? incrementMinor = null,
    Object? currentBidMinor = freezed,
    Object? minNextBidMinor = null,
    Object? bidCount = null,
    Object? reserveMet = null,
    Object? latestSeq = null,
    Object? highestBidderIsYou = null,
    Object? yourMaxBidMinor = freezed,
    Object? extensionCount = null,
    Object? inspectionReportUrl = freezed,
  }) {
    return _then(
      _$AuctionLotImpl(
        lotId: null == lotId
            ? _value.lotId
            : lotId // ignore: cast_nullable_to_non_nullable
                  as String,
        lotNumber: freezed == lotNumber
            ? _value.lotNumber
            : lotNumber // ignore: cast_nullable_to_non_nullable
                  as String?,
        listing: null == listing
            ? _value.listing
            : listing // ignore: cast_nullable_to_non_nullable
                  as SearchItem,
        currency: null == currency
            ? _value.currency
            : currency // ignore: cast_nullable_to_non_nullable
                  as String,
        state: null == state
            ? _value.state
            : state // ignore: cast_nullable_to_non_nullable
                  as WorkflowState,
        startsAt: null == startsAt
            ? _value.startsAt
            : startsAt // ignore: cast_nullable_to_non_nullable
                  as String,
        endsAt: null == endsAt
            ? _value.endsAt
            : endsAt // ignore: cast_nullable_to_non_nullable
                  as String,
        startPriceMinor: null == startPriceMinor
            ? _value.startPriceMinor
            : startPriceMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        incrementMinor: null == incrementMinor
            ? _value.incrementMinor
            : incrementMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        currentBidMinor: freezed == currentBidMinor
            ? _value.currentBidMinor
            : currentBidMinor // ignore: cast_nullable_to_non_nullable
                  as int?,
        minNextBidMinor: null == minNextBidMinor
            ? _value.minNextBidMinor
            : minNextBidMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        bidCount: null == bidCount
            ? _value.bidCount
            : bidCount // ignore: cast_nullable_to_non_nullable
                  as int,
        reserveMet: null == reserveMet
            ? _value.reserveMet
            : reserveMet // ignore: cast_nullable_to_non_nullable
                  as bool,
        latestSeq: null == latestSeq
            ? _value.latestSeq
            : latestSeq // ignore: cast_nullable_to_non_nullable
                  as int,
        highestBidderIsYou: null == highestBidderIsYou
            ? _value.highestBidderIsYou
            : highestBidderIsYou // ignore: cast_nullable_to_non_nullable
                  as bool,
        yourMaxBidMinor: freezed == yourMaxBidMinor
            ? _value.yourMaxBidMinor
            : yourMaxBidMinor // ignore: cast_nullable_to_non_nullable
                  as int?,
        extensionCount: null == extensionCount
            ? _value.extensionCount
            : extensionCount // ignore: cast_nullable_to_non_nullable
                  as int,
        inspectionReportUrl: freezed == inspectionReportUrl
            ? _value.inspectionReportUrl
            : inspectionReportUrl // ignore: cast_nullable_to_non_nullable
                  as String?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$AuctionLotImpl implements _AuctionLot {
  const _$AuctionLotImpl({
    required this.lotId,
    this.lotNumber,
    required this.listing,
    this.currency = 'INR',
    required this.state,
    required this.startsAt,
    required this.endsAt,
    required this.startPriceMinor,
    required this.incrementMinor,
    this.currentBidMinor,
    required this.minNextBidMinor,
    this.bidCount = 0,
    this.reserveMet = false,
    this.latestSeq = 0,
    this.highestBidderIsYou = false,
    this.yourMaxBidMinor,
    this.extensionCount = 0,
    this.inspectionReportUrl,
  });

  factory _$AuctionLotImpl.fromJson(Map<String, dynamic> json) =>
      _$$AuctionLotImplFromJson(json);

  @override
  final String lotId;
  @override
  final String? lotNumber;
  @override
  final SearchItem listing;
  @override
  @JsonKey()
  final String currency;
  @override
  final WorkflowState state;
  @override
  final String startsAt;

  /// The server's closing time. Render the countdown against the offset measured at connect,
  /// never against the handset clock — a phone two minutes fast shows a lot as closed while it is
  /// still taking bids, and the dealer stops bidding on a truck they were winning.
  @override
  final String endsAt;
  @override
  final int startPriceMinor;

  /// The ladder step. The next legal bid is `currentBidMinor + incrementMinor`.
  @override
  final int incrementMinor;
  @override
  final int? currentBidMinor;
  @override
  final int minNextBidMinor;
  @override
  @JsonKey()
  final int bidCount;

  /// The reserve *amount* is never exposed — only whether it has been met.
  @override
  @JsonKey()
  final bool reserveMet;

  /// Highest sequence broadcast so far. Sent back as `seenBidSeq` to detect an outbid race.
  @override
  @JsonKey()
  final int latestSeq;
  @override
  @JsonKey()
  final bool highestBidderIsYou;
  @override
  final int? yourMaxBidMinor;

  /// Anti-snipe extensions already spent.
  @override
  @JsonKey()
  final int extensionCount;
  @override
  final String? inspectionReportUrl;

  @override
  String toString() {
    return 'AuctionLot(lotId: $lotId, lotNumber: $lotNumber, listing: $listing, currency: $currency, state: $state, startsAt: $startsAt, endsAt: $endsAt, startPriceMinor: $startPriceMinor, incrementMinor: $incrementMinor, currentBidMinor: $currentBidMinor, minNextBidMinor: $minNextBidMinor, bidCount: $bidCount, reserveMet: $reserveMet, latestSeq: $latestSeq, highestBidderIsYou: $highestBidderIsYou, yourMaxBidMinor: $yourMaxBidMinor, extensionCount: $extensionCount, inspectionReportUrl: $inspectionReportUrl)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AuctionLotImpl &&
            (identical(other.lotId, lotId) || other.lotId == lotId) &&
            (identical(other.lotNumber, lotNumber) ||
                other.lotNumber == lotNumber) &&
            (identical(other.listing, listing) || other.listing == listing) &&
            (identical(other.currency, currency) ||
                other.currency == currency) &&
            (identical(other.state, state) || other.state == state) &&
            (identical(other.startsAt, startsAt) ||
                other.startsAt == startsAt) &&
            (identical(other.endsAt, endsAt) || other.endsAt == endsAt) &&
            (identical(other.startPriceMinor, startPriceMinor) ||
                other.startPriceMinor == startPriceMinor) &&
            (identical(other.incrementMinor, incrementMinor) ||
                other.incrementMinor == incrementMinor) &&
            (identical(other.currentBidMinor, currentBidMinor) ||
                other.currentBidMinor == currentBidMinor) &&
            (identical(other.minNextBidMinor, minNextBidMinor) ||
                other.minNextBidMinor == minNextBidMinor) &&
            (identical(other.bidCount, bidCount) ||
                other.bidCount == bidCount) &&
            (identical(other.reserveMet, reserveMet) ||
                other.reserveMet == reserveMet) &&
            (identical(other.latestSeq, latestSeq) ||
                other.latestSeq == latestSeq) &&
            (identical(other.highestBidderIsYou, highestBidderIsYou) ||
                other.highestBidderIsYou == highestBidderIsYou) &&
            (identical(other.yourMaxBidMinor, yourMaxBidMinor) ||
                other.yourMaxBidMinor == yourMaxBidMinor) &&
            (identical(other.extensionCount, extensionCount) ||
                other.extensionCount == extensionCount) &&
            (identical(other.inspectionReportUrl, inspectionReportUrl) ||
                other.inspectionReportUrl == inspectionReportUrl));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    lotId,
    lotNumber,
    listing,
    currency,
    state,
    startsAt,
    endsAt,
    startPriceMinor,
    incrementMinor,
    currentBidMinor,
    minNextBidMinor,
    bidCount,
    reserveMet,
    latestSeq,
    highestBidderIsYou,
    yourMaxBidMinor,
    extensionCount,
    inspectionReportUrl,
  );

  /// Create a copy of AuctionLot
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AuctionLotImplCopyWith<_$AuctionLotImpl> get copyWith =>
      __$$AuctionLotImplCopyWithImpl<_$AuctionLotImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$AuctionLotImplToJson(this);
  }
}

abstract class _AuctionLot implements AuctionLot {
  const factory _AuctionLot({
    required final String lotId,
    final String? lotNumber,
    required final SearchItem listing,
    final String currency,
    required final WorkflowState state,
    required final String startsAt,
    required final String endsAt,
    required final int startPriceMinor,
    required final int incrementMinor,
    final int? currentBidMinor,
    required final int minNextBidMinor,
    final int bidCount,
    final bool reserveMet,
    final int latestSeq,
    final bool highestBidderIsYou,
    final int? yourMaxBidMinor,
    final int extensionCount,
    final String? inspectionReportUrl,
  }) = _$AuctionLotImpl;

  factory _AuctionLot.fromJson(Map<String, dynamic> json) =
      _$AuctionLotImpl.fromJson;

  @override
  String get lotId;
  @override
  String? get lotNumber;
  @override
  SearchItem get listing;
  @override
  String get currency;
  @override
  WorkflowState get state;
  @override
  String get startsAt;

  /// The server's closing time. Render the countdown against the offset measured at connect,
  /// never against the handset clock — a phone two minutes fast shows a lot as closed while it is
  /// still taking bids, and the dealer stops bidding on a truck they were winning.
  @override
  String get endsAt;
  @override
  int get startPriceMinor;

  /// The ladder step. The next legal bid is `currentBidMinor + incrementMinor`.
  @override
  int get incrementMinor;
  @override
  int? get currentBidMinor;
  @override
  int get minNextBidMinor;
  @override
  int get bidCount;

  /// The reserve *amount* is never exposed — only whether it has been met.
  @override
  bool get reserveMet;

  /// Highest sequence broadcast so far. Sent back as `seenBidSeq` to detect an outbid race.
  @override
  int get latestSeq;
  @override
  bool get highestBidderIsYou;
  @override
  int? get yourMaxBidMinor;

  /// Anti-snipe extensions already spent.
  @override
  int get extensionCount;
  @override
  String? get inspectionReportUrl;

  /// Create a copy of AuctionLot
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AuctionLotImplCopyWith<_$AuctionLotImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

PlaceBidRequest _$PlaceBidRequestFromJson(Map<String, dynamic> json) {
  return _PlaceBidRequest.fromJson(json);
}

/// @nodoc
mixin _$PlaceBidRequest {
  int get amountMinor => throw _privateConstructorUsedError;

  /// The highest sequence this screen has actually **rendered** — not the highest it has
  /// received, and not the lot's `latestSeq` as fetched. A bid composed against a figure the
  /// dealer never saw is a bid they did not agree to, and the server rejects it as
  /// `auction.bid.outbid_race` so the app can show the truth rather than guess the next
  /// increment locally.
  int? get seenBidSeq => throw _privateConstructorUsedError;

  /// Proxy bidding: bid up to this automatically.
  int? get maxAmountMinor => throw _privateConstructorUsedError;

  /// Serializes this PlaceBidRequest to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PlaceBidRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PlaceBidRequestCopyWith<PlaceBidRequest> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PlaceBidRequestCopyWith<$Res> {
  factory $PlaceBidRequestCopyWith(
    PlaceBidRequest value,
    $Res Function(PlaceBidRequest) then,
  ) = _$PlaceBidRequestCopyWithImpl<$Res, PlaceBidRequest>;
  @useResult
  $Res call({int amountMinor, int? seenBidSeq, int? maxAmountMinor});
}

/// @nodoc
class _$PlaceBidRequestCopyWithImpl<$Res, $Val extends PlaceBidRequest>
    implements $PlaceBidRequestCopyWith<$Res> {
  _$PlaceBidRequestCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PlaceBidRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? amountMinor = null,
    Object? seenBidSeq = freezed,
    Object? maxAmountMinor = freezed,
  }) {
    return _then(
      _value.copyWith(
            amountMinor: null == amountMinor
                ? _value.amountMinor
                : amountMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            seenBidSeq: freezed == seenBidSeq
                ? _value.seenBidSeq
                : seenBidSeq // ignore: cast_nullable_to_non_nullable
                      as int?,
            maxAmountMinor: freezed == maxAmountMinor
                ? _value.maxAmountMinor
                : maxAmountMinor // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$PlaceBidRequestImplCopyWith<$Res>
    implements $PlaceBidRequestCopyWith<$Res> {
  factory _$$PlaceBidRequestImplCopyWith(
    _$PlaceBidRequestImpl value,
    $Res Function(_$PlaceBidRequestImpl) then,
  ) = __$$PlaceBidRequestImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int amountMinor, int? seenBidSeq, int? maxAmountMinor});
}

/// @nodoc
class __$$PlaceBidRequestImplCopyWithImpl<$Res>
    extends _$PlaceBidRequestCopyWithImpl<$Res, _$PlaceBidRequestImpl>
    implements _$$PlaceBidRequestImplCopyWith<$Res> {
  __$$PlaceBidRequestImplCopyWithImpl(
    _$PlaceBidRequestImpl _value,
    $Res Function(_$PlaceBidRequestImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PlaceBidRequest
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? amountMinor = null,
    Object? seenBidSeq = freezed,
    Object? maxAmountMinor = freezed,
  }) {
    return _then(
      _$PlaceBidRequestImpl(
        amountMinor: null == amountMinor
            ? _value.amountMinor
            : amountMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        seenBidSeq: freezed == seenBidSeq
            ? _value.seenBidSeq
            : seenBidSeq // ignore: cast_nullable_to_non_nullable
                  as int?,
        maxAmountMinor: freezed == maxAmountMinor
            ? _value.maxAmountMinor
            : maxAmountMinor // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PlaceBidRequestImpl implements _PlaceBidRequest {
  const _$PlaceBidRequestImpl({
    required this.amountMinor,
    this.seenBidSeq,
    this.maxAmountMinor,
  });

  factory _$PlaceBidRequestImpl.fromJson(Map<String, dynamic> json) =>
      _$$PlaceBidRequestImplFromJson(json);

  @override
  final int amountMinor;

  /// The highest sequence this screen has actually **rendered** — not the highest it has
  /// received, and not the lot's `latestSeq` as fetched. A bid composed against a figure the
  /// dealer never saw is a bid they did not agree to, and the server rejects it as
  /// `auction.bid.outbid_race` so the app can show the truth rather than guess the next
  /// increment locally.
  @override
  final int? seenBidSeq;

  /// Proxy bidding: bid up to this automatically.
  @override
  final int? maxAmountMinor;

  @override
  String toString() {
    return 'PlaceBidRequest(amountMinor: $amountMinor, seenBidSeq: $seenBidSeq, maxAmountMinor: $maxAmountMinor)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PlaceBidRequestImpl &&
            (identical(other.amountMinor, amountMinor) ||
                other.amountMinor == amountMinor) &&
            (identical(other.seenBidSeq, seenBidSeq) ||
                other.seenBidSeq == seenBidSeq) &&
            (identical(other.maxAmountMinor, maxAmountMinor) ||
                other.maxAmountMinor == maxAmountMinor));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, amountMinor, seenBidSeq, maxAmountMinor);

  /// Create a copy of PlaceBidRequest
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PlaceBidRequestImplCopyWith<_$PlaceBidRequestImpl> get copyWith =>
      __$$PlaceBidRequestImplCopyWithImpl<_$PlaceBidRequestImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$PlaceBidRequestImplToJson(this);
  }
}

abstract class _PlaceBidRequest implements PlaceBidRequest {
  const factory _PlaceBidRequest({
    required final int amountMinor,
    final int? seenBidSeq,
    final int? maxAmountMinor,
  }) = _$PlaceBidRequestImpl;

  factory _PlaceBidRequest.fromJson(Map<String, dynamic> json) =
      _$PlaceBidRequestImpl.fromJson;

  @override
  int get amountMinor;

  /// The highest sequence this screen has actually **rendered** — not the highest it has
  /// received, and not the lot's `latestSeq` as fetched. A bid composed against a figure the
  /// dealer never saw is a bid they did not agree to, and the server rejects it as
  /// `auction.bid.outbid_race` so the app can show the truth rather than guess the next
  /// increment locally.
  @override
  int? get seenBidSeq;

  /// Proxy bidding: bid up to this automatically.
  @override
  int? get maxAmountMinor;

  /// Create a copy of PlaceBidRequest
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PlaceBidRequestImplCopyWith<_$PlaceBidRequestImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

PlaceBidResponse _$PlaceBidResponseFromJson(Map<String, dynamic> json) {
  return _PlaceBidResponse.fromJson(json);
}

/// @nodoc
mixin _$PlaceBidResponse {
  Bid get bid => throw _privateConstructorUsedError;
  AuctionLot get lot => throw _privateConstructorUsedError;

  /// Serializes this PlaceBidResponse to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of PlaceBidResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $PlaceBidResponseCopyWith<PlaceBidResponse> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $PlaceBidResponseCopyWith<$Res> {
  factory $PlaceBidResponseCopyWith(
    PlaceBidResponse value,
    $Res Function(PlaceBidResponse) then,
  ) = _$PlaceBidResponseCopyWithImpl<$Res, PlaceBidResponse>;
  @useResult
  $Res call({Bid bid, AuctionLot lot});

  $BidCopyWith<$Res> get bid;
  $AuctionLotCopyWith<$Res> get lot;
}

/// @nodoc
class _$PlaceBidResponseCopyWithImpl<$Res, $Val extends PlaceBidResponse>
    implements $PlaceBidResponseCopyWith<$Res> {
  _$PlaceBidResponseCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of PlaceBidResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? bid = null, Object? lot = null}) {
    return _then(
      _value.copyWith(
            bid: null == bid
                ? _value.bid
                : bid // ignore: cast_nullable_to_non_nullable
                      as Bid,
            lot: null == lot
                ? _value.lot
                : lot // ignore: cast_nullable_to_non_nullable
                      as AuctionLot,
          )
          as $Val,
    );
  }

  /// Create a copy of PlaceBidResponse
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $BidCopyWith<$Res> get bid {
    return $BidCopyWith<$Res>(_value.bid, (value) {
      return _then(_value.copyWith(bid: value) as $Val);
    });
  }

  /// Create a copy of PlaceBidResponse
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $AuctionLotCopyWith<$Res> get lot {
    return $AuctionLotCopyWith<$Res>(_value.lot, (value) {
      return _then(_value.copyWith(lot: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$PlaceBidResponseImplCopyWith<$Res>
    implements $PlaceBidResponseCopyWith<$Res> {
  factory _$$PlaceBidResponseImplCopyWith(
    _$PlaceBidResponseImpl value,
    $Res Function(_$PlaceBidResponseImpl) then,
  ) = __$$PlaceBidResponseImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({Bid bid, AuctionLot lot});

  @override
  $BidCopyWith<$Res> get bid;
  @override
  $AuctionLotCopyWith<$Res> get lot;
}

/// @nodoc
class __$$PlaceBidResponseImplCopyWithImpl<$Res>
    extends _$PlaceBidResponseCopyWithImpl<$Res, _$PlaceBidResponseImpl>
    implements _$$PlaceBidResponseImplCopyWith<$Res> {
  __$$PlaceBidResponseImplCopyWithImpl(
    _$PlaceBidResponseImpl _value,
    $Res Function(_$PlaceBidResponseImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of PlaceBidResponse
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? bid = null, Object? lot = null}) {
    return _then(
      _$PlaceBidResponseImpl(
        bid: null == bid
            ? _value.bid
            : bid // ignore: cast_nullable_to_non_nullable
                  as Bid,
        lot: null == lot
            ? _value.lot
            : lot // ignore: cast_nullable_to_non_nullable
                  as AuctionLot,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$PlaceBidResponseImpl implements _PlaceBidResponse {
  const _$PlaceBidResponseImpl({required this.bid, required this.lot});

  factory _$PlaceBidResponseImpl.fromJson(Map<String, dynamic> json) =>
      _$$PlaceBidResponseImplFromJson(json);

  @override
  final Bid bid;
  @override
  final AuctionLot lot;

  @override
  String toString() {
    return 'PlaceBidResponse(bid: $bid, lot: $lot)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$PlaceBidResponseImpl &&
            (identical(other.bid, bid) || other.bid == bid) &&
            (identical(other.lot, lot) || other.lot == lot));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, bid, lot);

  /// Create a copy of PlaceBidResponse
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$PlaceBidResponseImplCopyWith<_$PlaceBidResponseImpl> get copyWith =>
      __$$PlaceBidResponseImplCopyWithImpl<_$PlaceBidResponseImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$PlaceBidResponseImplToJson(this);
  }
}

abstract class _PlaceBidResponse implements PlaceBidResponse {
  const factory _PlaceBidResponse({
    required final Bid bid,
    required final AuctionLot lot,
  }) = _$PlaceBidResponseImpl;

  factory _PlaceBidResponse.fromJson(Map<String, dynamic> json) =
      _$PlaceBidResponseImpl.fromJson;

  @override
  Bid get bid;
  @override
  AuctionLot get lot;

  /// Create a copy of PlaceBidResponse
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$PlaceBidResponseImplCopyWith<_$PlaceBidResponseImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

BidPlacedEvent _$BidPlacedEventFromJson(Map<String, dynamic> json) {
  return _BidPlacedEvent.fromJson(json);
}

/// @nodoc
mixin _$BidPlacedEvent {
  String get lotId => throw _privateConstructorUsedError;
  int get amountMinor => throw _privateConstructorUsedError;
  String get bidderMask => throw _privateConstructorUsedError;
  String get placedAt => throw _privateConstructorUsedError;
  bool get highestBidderIsYou => throw _privateConstructorUsedError;
  int? get bidCount => throw _privateConstructorUsedError;
  int? get minNextBidMinor => throw _privateConstructorUsedError;

  /// Serializes this BidPlacedEvent to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of BidPlacedEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $BidPlacedEventCopyWith<BidPlacedEvent> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $BidPlacedEventCopyWith<$Res> {
  factory $BidPlacedEventCopyWith(
    BidPlacedEvent value,
    $Res Function(BidPlacedEvent) then,
  ) = _$BidPlacedEventCopyWithImpl<$Res, BidPlacedEvent>;
  @useResult
  $Res call({
    String lotId,
    int amountMinor,
    String bidderMask,
    String placedAt,
    bool highestBidderIsYou,
    int? bidCount,
    int? minNextBidMinor,
  });
}

/// @nodoc
class _$BidPlacedEventCopyWithImpl<$Res, $Val extends BidPlacedEvent>
    implements $BidPlacedEventCopyWith<$Res> {
  _$BidPlacedEventCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of BidPlacedEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? lotId = null,
    Object? amountMinor = null,
    Object? bidderMask = null,
    Object? placedAt = null,
    Object? highestBidderIsYou = null,
    Object? bidCount = freezed,
    Object? minNextBidMinor = freezed,
  }) {
    return _then(
      _value.copyWith(
            lotId: null == lotId
                ? _value.lotId
                : lotId // ignore: cast_nullable_to_non_nullable
                      as String,
            amountMinor: null == amountMinor
                ? _value.amountMinor
                : amountMinor // ignore: cast_nullable_to_non_nullable
                      as int,
            bidderMask: null == bidderMask
                ? _value.bidderMask
                : bidderMask // ignore: cast_nullable_to_non_nullable
                      as String,
            placedAt: null == placedAt
                ? _value.placedAt
                : placedAt // ignore: cast_nullable_to_non_nullable
                      as String,
            highestBidderIsYou: null == highestBidderIsYou
                ? _value.highestBidderIsYou
                : highestBidderIsYou // ignore: cast_nullable_to_non_nullable
                      as bool,
            bidCount: freezed == bidCount
                ? _value.bidCount
                : bidCount // ignore: cast_nullable_to_non_nullable
                      as int?,
            minNextBidMinor: freezed == minNextBidMinor
                ? _value.minNextBidMinor
                : minNextBidMinor // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$BidPlacedEventImplCopyWith<$Res>
    implements $BidPlacedEventCopyWith<$Res> {
  factory _$$BidPlacedEventImplCopyWith(
    _$BidPlacedEventImpl value,
    $Res Function(_$BidPlacedEventImpl) then,
  ) = __$$BidPlacedEventImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({
    String lotId,
    int amountMinor,
    String bidderMask,
    String placedAt,
    bool highestBidderIsYou,
    int? bidCount,
    int? minNextBidMinor,
  });
}

/// @nodoc
class __$$BidPlacedEventImplCopyWithImpl<$Res>
    extends _$BidPlacedEventCopyWithImpl<$Res, _$BidPlacedEventImpl>
    implements _$$BidPlacedEventImplCopyWith<$Res> {
  __$$BidPlacedEventImplCopyWithImpl(
    _$BidPlacedEventImpl _value,
    $Res Function(_$BidPlacedEventImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of BidPlacedEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? lotId = null,
    Object? amountMinor = null,
    Object? bidderMask = null,
    Object? placedAt = null,
    Object? highestBidderIsYou = null,
    Object? bidCount = freezed,
    Object? minNextBidMinor = freezed,
  }) {
    return _then(
      _$BidPlacedEventImpl(
        lotId: null == lotId
            ? _value.lotId
            : lotId // ignore: cast_nullable_to_non_nullable
                  as String,
        amountMinor: null == amountMinor
            ? _value.amountMinor
            : amountMinor // ignore: cast_nullable_to_non_nullable
                  as int,
        bidderMask: null == bidderMask
            ? _value.bidderMask
            : bidderMask // ignore: cast_nullable_to_non_nullable
                  as String,
        placedAt: null == placedAt
            ? _value.placedAt
            : placedAt // ignore: cast_nullable_to_non_nullable
                  as String,
        highestBidderIsYou: null == highestBidderIsYou
            ? _value.highestBidderIsYou
            : highestBidderIsYou // ignore: cast_nullable_to_non_nullable
                  as bool,
        bidCount: freezed == bidCount
            ? _value.bidCount
            : bidCount // ignore: cast_nullable_to_non_nullable
                  as int?,
        minNextBidMinor: freezed == minNextBidMinor
            ? _value.minNextBidMinor
            : minNextBidMinor // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$BidPlacedEventImpl implements _BidPlacedEvent {
  const _$BidPlacedEventImpl({
    required this.lotId,
    required this.amountMinor,
    this.bidderMask = '',
    required this.placedAt,
    this.highestBidderIsYou = false,
    this.bidCount,
    this.minNextBidMinor,
  });

  factory _$BidPlacedEventImpl.fromJson(Map<String, dynamic> json) =>
      _$$BidPlacedEventImplFromJson(json);

  @override
  final String lotId;
  @override
  final int amountMinor;
  @override
  @JsonKey()
  final String bidderMask;
  @override
  final String placedAt;
  @override
  @JsonKey()
  final bool highestBidderIsYou;
  @override
  final int? bidCount;
  @override
  final int? minNextBidMinor;

  @override
  String toString() {
    return 'BidPlacedEvent(lotId: $lotId, amountMinor: $amountMinor, bidderMask: $bidderMask, placedAt: $placedAt, highestBidderIsYou: $highestBidderIsYou, bidCount: $bidCount, minNextBidMinor: $minNextBidMinor)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$BidPlacedEventImpl &&
            (identical(other.lotId, lotId) || other.lotId == lotId) &&
            (identical(other.amountMinor, amountMinor) ||
                other.amountMinor == amountMinor) &&
            (identical(other.bidderMask, bidderMask) ||
                other.bidderMask == bidderMask) &&
            (identical(other.placedAt, placedAt) ||
                other.placedAt == placedAt) &&
            (identical(other.highestBidderIsYou, highestBidderIsYou) ||
                other.highestBidderIsYou == highestBidderIsYou) &&
            (identical(other.bidCount, bidCount) ||
                other.bidCount == bidCount) &&
            (identical(other.minNextBidMinor, minNextBidMinor) ||
                other.minNextBidMinor == minNextBidMinor));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
    runtimeType,
    lotId,
    amountMinor,
    bidderMask,
    placedAt,
    highestBidderIsYou,
    bidCount,
    minNextBidMinor,
  );

  /// Create a copy of BidPlacedEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$BidPlacedEventImplCopyWith<_$BidPlacedEventImpl> get copyWith =>
      __$$BidPlacedEventImplCopyWithImpl<_$BidPlacedEventImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$BidPlacedEventImplToJson(this);
  }
}

abstract class _BidPlacedEvent implements BidPlacedEvent {
  const factory _BidPlacedEvent({
    required final String lotId,
    required final int amountMinor,
    final String bidderMask,
    required final String placedAt,
    final bool highestBidderIsYou,
    final int? bidCount,
    final int? minNextBidMinor,
  }) = _$BidPlacedEventImpl;

  factory _BidPlacedEvent.fromJson(Map<String, dynamic> json) =
      _$BidPlacedEventImpl.fromJson;

  @override
  String get lotId;
  @override
  int get amountMinor;
  @override
  String get bidderMask;
  @override
  String get placedAt;
  @override
  bool get highestBidderIsYou;
  @override
  int? get bidCount;
  @override
  int? get minNextBidMinor;

  /// Create a copy of BidPlacedEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$BidPlacedEventImplCopyWith<_$BidPlacedEventImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

LotExtendedEvent _$LotExtendedEventFromJson(Map<String, dynamic> json) {
  return _LotExtendedEvent.fromJson(json);
}

/// @nodoc
mixin _$LotExtendedEvent {
  String get endsAt => throw _privateConstructorUsedError;

  /// `anti_snipe` — why the clock moved. The UI says so; a countdown that jumps silently reads
  /// as a broken app rather than as a working auction.
  String get reason => throw _privateConstructorUsedError;

  /// Serializes this LotExtendedEvent to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of LotExtendedEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $LotExtendedEventCopyWith<LotExtendedEvent> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $LotExtendedEventCopyWith<$Res> {
  factory $LotExtendedEventCopyWith(
    LotExtendedEvent value,
    $Res Function(LotExtendedEvent) then,
  ) = _$LotExtendedEventCopyWithImpl<$Res, LotExtendedEvent>;
  @useResult
  $Res call({String endsAt, String reason});
}

/// @nodoc
class _$LotExtendedEventCopyWithImpl<$Res, $Val extends LotExtendedEvent>
    implements $LotExtendedEventCopyWith<$Res> {
  _$LotExtendedEventCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of LotExtendedEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? endsAt = null, Object? reason = null}) {
    return _then(
      _value.copyWith(
            endsAt: null == endsAt
                ? _value.endsAt
                : endsAt // ignore: cast_nullable_to_non_nullable
                      as String,
            reason: null == reason
                ? _value.reason
                : reason // ignore: cast_nullable_to_non_nullable
                      as String,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$LotExtendedEventImplCopyWith<$Res>
    implements $LotExtendedEventCopyWith<$Res> {
  factory _$$LotExtendedEventImplCopyWith(
    _$LotExtendedEventImpl value,
    $Res Function(_$LotExtendedEventImpl) then,
  ) = __$$LotExtendedEventImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String endsAt, String reason});
}

/// @nodoc
class __$$LotExtendedEventImplCopyWithImpl<$Res>
    extends _$LotExtendedEventCopyWithImpl<$Res, _$LotExtendedEventImpl>
    implements _$$LotExtendedEventImplCopyWith<$Res> {
  __$$LotExtendedEventImplCopyWithImpl(
    _$LotExtendedEventImpl _value,
    $Res Function(_$LotExtendedEventImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of LotExtendedEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? endsAt = null, Object? reason = null}) {
    return _then(
      _$LotExtendedEventImpl(
        endsAt: null == endsAt
            ? _value.endsAt
            : endsAt // ignore: cast_nullable_to_non_nullable
                  as String,
        reason: null == reason
            ? _value.reason
            : reason // ignore: cast_nullable_to_non_nullable
                  as String,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$LotExtendedEventImpl implements _LotExtendedEvent {
  const _$LotExtendedEventImpl({required this.endsAt, this.reason = ''});

  factory _$LotExtendedEventImpl.fromJson(Map<String, dynamic> json) =>
      _$$LotExtendedEventImplFromJson(json);

  @override
  final String endsAt;

  /// `anti_snipe` — why the clock moved. The UI says so; a countdown that jumps silently reads
  /// as a broken app rather than as a working auction.
  @override
  @JsonKey()
  final String reason;

  @override
  String toString() {
    return 'LotExtendedEvent(endsAt: $endsAt, reason: $reason)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$LotExtendedEventImpl &&
            (identical(other.endsAt, endsAt) || other.endsAt == endsAt) &&
            (identical(other.reason, reason) || other.reason == reason));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, endsAt, reason);

  /// Create a copy of LotExtendedEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$LotExtendedEventImplCopyWith<_$LotExtendedEventImpl> get copyWith =>
      __$$LotExtendedEventImplCopyWithImpl<_$LotExtendedEventImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$LotExtendedEventImplToJson(this);
  }
}

abstract class _LotExtendedEvent implements LotExtendedEvent {
  const factory _LotExtendedEvent({
    required final String endsAt,
    final String reason,
  }) = _$LotExtendedEventImpl;

  factory _LotExtendedEvent.fromJson(Map<String, dynamic> json) =
      _$LotExtendedEventImpl.fromJson;

  @override
  String get endsAt;

  /// `anti_snipe` — why the clock moved. The UI says so; a countdown that jumps silently reads
  /// as a broken app rather than as a working auction.
  @override
  String get reason;

  /// Create a copy of LotExtendedEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$LotExtendedEventImplCopyWith<_$LotExtendedEventImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

LotClosedEvent _$LotClosedEventFromJson(Map<String, dynamic> json) {
  return _LotClosedEvent.fromJson(json);
}

/// @nodoc
mixin _$LotClosedEvent {
  String get outcome => throw _privateConstructorUsedError;
  bool get winnerIsYou => throw _privateConstructorUsedError;
  int? get finalAmountMinor => throw _privateConstructorUsedError;

  /// Serializes this LotClosedEvent to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of LotClosedEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $LotClosedEventCopyWith<LotClosedEvent> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $LotClosedEventCopyWith<$Res> {
  factory $LotClosedEventCopyWith(
    LotClosedEvent value,
    $Res Function(LotClosedEvent) then,
  ) = _$LotClosedEventCopyWithImpl<$Res, LotClosedEvent>;
  @useResult
  $Res call({String outcome, bool winnerIsYou, int? finalAmountMinor});
}

/// @nodoc
class _$LotClosedEventCopyWithImpl<$Res, $Val extends LotClosedEvent>
    implements $LotClosedEventCopyWith<$Res> {
  _$LotClosedEventCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of LotClosedEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? outcome = null,
    Object? winnerIsYou = null,
    Object? finalAmountMinor = freezed,
  }) {
    return _then(
      _value.copyWith(
            outcome: null == outcome
                ? _value.outcome
                : outcome // ignore: cast_nullable_to_non_nullable
                      as String,
            winnerIsYou: null == winnerIsYou
                ? _value.winnerIsYou
                : winnerIsYou // ignore: cast_nullable_to_non_nullable
                      as bool,
            finalAmountMinor: freezed == finalAmountMinor
                ? _value.finalAmountMinor
                : finalAmountMinor // ignore: cast_nullable_to_non_nullable
                      as int?,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$LotClosedEventImplCopyWith<$Res>
    implements $LotClosedEventCopyWith<$Res> {
  factory _$$LotClosedEventImplCopyWith(
    _$LotClosedEventImpl value,
    $Res Function(_$LotClosedEventImpl) then,
  ) = __$$LotClosedEventImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({String outcome, bool winnerIsYou, int? finalAmountMinor});
}

/// @nodoc
class __$$LotClosedEventImplCopyWithImpl<$Res>
    extends _$LotClosedEventCopyWithImpl<$Res, _$LotClosedEventImpl>
    implements _$$LotClosedEventImplCopyWith<$Res> {
  __$$LotClosedEventImplCopyWithImpl(
    _$LotClosedEventImpl _value,
    $Res Function(_$LotClosedEventImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of LotClosedEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? outcome = null,
    Object? winnerIsYou = null,
    Object? finalAmountMinor = freezed,
  }) {
    return _then(
      _$LotClosedEventImpl(
        outcome: null == outcome
            ? _value.outcome
            : outcome // ignore: cast_nullable_to_non_nullable
                  as String,
        winnerIsYou: null == winnerIsYou
            ? _value.winnerIsYou
            : winnerIsYou // ignore: cast_nullable_to_non_nullable
                  as bool,
        finalAmountMinor: freezed == finalAmountMinor
            ? _value.finalAmountMinor
            : finalAmountMinor // ignore: cast_nullable_to_non_nullable
                  as int?,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$LotClosedEventImpl implements _LotClosedEvent {
  const _$LotClosedEventImpl({
    this.outcome = '',
    this.winnerIsYou = false,
    this.finalAmountMinor,
  });

  factory _$LotClosedEventImpl.fromJson(Map<String, dynamic> json) =>
      _$$LotClosedEventImplFromJson(json);

  @override
  @JsonKey()
  final String outcome;
  @override
  @JsonKey()
  final bool winnerIsYou;
  @override
  final int? finalAmountMinor;

  @override
  String toString() {
    return 'LotClosedEvent(outcome: $outcome, winnerIsYou: $winnerIsYou, finalAmountMinor: $finalAmountMinor)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$LotClosedEventImpl &&
            (identical(other.outcome, outcome) || other.outcome == outcome) &&
            (identical(other.winnerIsYou, winnerIsYou) ||
                other.winnerIsYou == winnerIsYou) &&
            (identical(other.finalAmountMinor, finalAmountMinor) ||
                other.finalAmountMinor == finalAmountMinor));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode =>
      Object.hash(runtimeType, outcome, winnerIsYou, finalAmountMinor);

  /// Create a copy of LotClosedEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$LotClosedEventImplCopyWith<_$LotClosedEventImpl> get copyWith =>
      __$$LotClosedEventImplCopyWithImpl<_$LotClosedEventImpl>(
        this,
        _$identity,
      );

  @override
  Map<String, dynamic> toJson() {
    return _$$LotClosedEventImplToJson(this);
  }
}

abstract class _LotClosedEvent implements LotClosedEvent {
  const factory _LotClosedEvent({
    final String outcome,
    final bool winnerIsYou,
    final int? finalAmountMinor,
  }) = _$LotClosedEventImpl;

  factory _LotClosedEvent.fromJson(Map<String, dynamic> json) =
      _$LotClosedEventImpl.fromJson;

  @override
  String get outcome;
  @override
  bool get winnerIsYou;
  @override
  int? get finalAmountMinor;

  /// Create a copy of LotClosedEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$LotClosedEventImplCopyWith<_$LotClosedEventImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

AuctionEvent _$AuctionEventFromJson(Map<String, dynamic> json) {
  switch (json['type']) {
    case 'bid.placed':
      return AuctionBidPlaced.fromJson(json);
    case 'lot.extended':
      return AuctionLotExtended.fromJson(json);
    case 'lot.closed':
      return AuctionLotClosed.fromJson(json);

    default:
      return AuctionUnknownEvent.fromJson(json);
  }
}

/// @nodoc
mixin _$AuctionEvent {
  int get seq => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(int seq, BidPlacedEvent data) bidPlaced,
    required TResult Function(int seq, LotExtendedEvent data) lotExtended,
    required TResult Function(int seq, LotClosedEvent data) lotClosed,
    required TResult Function(int seq) unknown,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult? Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult? Function(int seq, LotClosedEvent data)? lotClosed,
    TResult? Function(int seq)? unknown,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult Function(int seq, LotClosedEvent data)? lotClosed,
    TResult Function(int seq)? unknown,
    required TResult orElse(),
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(AuctionBidPlaced value) bidPlaced,
    required TResult Function(AuctionLotExtended value) lotExtended,
    required TResult Function(AuctionLotClosed value) lotClosed,
    required TResult Function(AuctionUnknownEvent value) unknown,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(AuctionBidPlaced value)? bidPlaced,
    TResult? Function(AuctionLotExtended value)? lotExtended,
    TResult? Function(AuctionLotClosed value)? lotClosed,
    TResult? Function(AuctionUnknownEvent value)? unknown,
  }) => throw _privateConstructorUsedError;
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(AuctionBidPlaced value)? bidPlaced,
    TResult Function(AuctionLotExtended value)? lotExtended,
    TResult Function(AuctionLotClosed value)? lotClosed,
    TResult Function(AuctionUnknownEvent value)? unknown,
    required TResult orElse(),
  }) => throw _privateConstructorUsedError;

  /// Serializes this AuctionEvent to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $AuctionEventCopyWith<AuctionEvent> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $AuctionEventCopyWith<$Res> {
  factory $AuctionEventCopyWith(
    AuctionEvent value,
    $Res Function(AuctionEvent) then,
  ) = _$AuctionEventCopyWithImpl<$Res, AuctionEvent>;
  @useResult
  $Res call({int seq});
}

/// @nodoc
class _$AuctionEventCopyWithImpl<$Res, $Val extends AuctionEvent>
    implements $AuctionEventCopyWith<$Res> {
  _$AuctionEventCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? seq = null}) {
    return _then(
      _value.copyWith(
            seq: null == seq
                ? _value.seq
                : seq // ignore: cast_nullable_to_non_nullable
                      as int,
          )
          as $Val,
    );
  }
}

/// @nodoc
abstract class _$$AuctionBidPlacedImplCopyWith<$Res>
    implements $AuctionEventCopyWith<$Res> {
  factory _$$AuctionBidPlacedImplCopyWith(
    _$AuctionBidPlacedImpl value,
    $Res Function(_$AuctionBidPlacedImpl) then,
  ) = __$$AuctionBidPlacedImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int seq, BidPlacedEvent data});

  $BidPlacedEventCopyWith<$Res> get data;
}

/// @nodoc
class __$$AuctionBidPlacedImplCopyWithImpl<$Res>
    extends _$AuctionEventCopyWithImpl<$Res, _$AuctionBidPlacedImpl>
    implements _$$AuctionBidPlacedImplCopyWith<$Res> {
  __$$AuctionBidPlacedImplCopyWithImpl(
    _$AuctionBidPlacedImpl _value,
    $Res Function(_$AuctionBidPlacedImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? seq = null, Object? data = null}) {
    return _then(
      _$AuctionBidPlacedImpl(
        seq: null == seq
            ? _value.seq
            : seq // ignore: cast_nullable_to_non_nullable
                  as int,
        data: null == data
            ? _value.data
            : data // ignore: cast_nullable_to_non_nullable
                  as BidPlacedEvent,
      ),
    );
  }

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $BidPlacedEventCopyWith<$Res> get data {
    return $BidPlacedEventCopyWith<$Res>(_value.data, (value) {
      return _then(_value.copyWith(data: value));
    });
  }
}

/// @nodoc
@JsonSerializable()
class _$AuctionBidPlacedImpl implements AuctionBidPlaced {
  const _$AuctionBidPlacedImpl({
    required this.seq,
    required this.data,
    final String? $type,
  }) : $type = $type ?? 'bid.placed';

  factory _$AuctionBidPlacedImpl.fromJson(Map<String, dynamic> json) =>
      _$$AuctionBidPlacedImplFromJson(json);

  @override
  final int seq;
  @override
  final BidPlacedEvent data;

  @JsonKey(name: 'type')
  final String $type;

  @override
  String toString() {
    return 'AuctionEvent.bidPlaced(seq: $seq, data: $data)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AuctionBidPlacedImpl &&
            (identical(other.seq, seq) || other.seq == seq) &&
            (identical(other.data, data) || other.data == data));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, seq, data);

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AuctionBidPlacedImplCopyWith<_$AuctionBidPlacedImpl> get copyWith =>
      __$$AuctionBidPlacedImplCopyWithImpl<_$AuctionBidPlacedImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(int seq, BidPlacedEvent data) bidPlaced,
    required TResult Function(int seq, LotExtendedEvent data) lotExtended,
    required TResult Function(int seq, LotClosedEvent data) lotClosed,
    required TResult Function(int seq) unknown,
  }) {
    return bidPlaced(seq, data);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult? Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult? Function(int seq, LotClosedEvent data)? lotClosed,
    TResult? Function(int seq)? unknown,
  }) {
    return bidPlaced?.call(seq, data);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult Function(int seq, LotClosedEvent data)? lotClosed,
    TResult Function(int seq)? unknown,
    required TResult orElse(),
  }) {
    if (bidPlaced != null) {
      return bidPlaced(seq, data);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(AuctionBidPlaced value) bidPlaced,
    required TResult Function(AuctionLotExtended value) lotExtended,
    required TResult Function(AuctionLotClosed value) lotClosed,
    required TResult Function(AuctionUnknownEvent value) unknown,
  }) {
    return bidPlaced(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(AuctionBidPlaced value)? bidPlaced,
    TResult? Function(AuctionLotExtended value)? lotExtended,
    TResult? Function(AuctionLotClosed value)? lotClosed,
    TResult? Function(AuctionUnknownEvent value)? unknown,
  }) {
    return bidPlaced?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(AuctionBidPlaced value)? bidPlaced,
    TResult Function(AuctionLotExtended value)? lotExtended,
    TResult Function(AuctionLotClosed value)? lotClosed,
    TResult Function(AuctionUnknownEvent value)? unknown,
    required TResult orElse(),
  }) {
    if (bidPlaced != null) {
      return bidPlaced(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$AuctionBidPlacedImplToJson(this);
  }
}

abstract class AuctionBidPlaced implements AuctionEvent {
  const factory AuctionBidPlaced({
    required final int seq,
    required final BidPlacedEvent data,
  }) = _$AuctionBidPlacedImpl;

  factory AuctionBidPlaced.fromJson(Map<String, dynamic> json) =
      _$AuctionBidPlacedImpl.fromJson;

  @override
  int get seq;
  BidPlacedEvent get data;

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AuctionBidPlacedImplCopyWith<_$AuctionBidPlacedImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$AuctionLotExtendedImplCopyWith<$Res>
    implements $AuctionEventCopyWith<$Res> {
  factory _$$AuctionLotExtendedImplCopyWith(
    _$AuctionLotExtendedImpl value,
    $Res Function(_$AuctionLotExtendedImpl) then,
  ) = __$$AuctionLotExtendedImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int seq, LotExtendedEvent data});

  $LotExtendedEventCopyWith<$Res> get data;
}

/// @nodoc
class __$$AuctionLotExtendedImplCopyWithImpl<$Res>
    extends _$AuctionEventCopyWithImpl<$Res, _$AuctionLotExtendedImpl>
    implements _$$AuctionLotExtendedImplCopyWith<$Res> {
  __$$AuctionLotExtendedImplCopyWithImpl(
    _$AuctionLotExtendedImpl _value,
    $Res Function(_$AuctionLotExtendedImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? seq = null, Object? data = null}) {
    return _then(
      _$AuctionLotExtendedImpl(
        seq: null == seq
            ? _value.seq
            : seq // ignore: cast_nullable_to_non_nullable
                  as int,
        data: null == data
            ? _value.data
            : data // ignore: cast_nullable_to_non_nullable
                  as LotExtendedEvent,
      ),
    );
  }

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $LotExtendedEventCopyWith<$Res> get data {
    return $LotExtendedEventCopyWith<$Res>(_value.data, (value) {
      return _then(_value.copyWith(data: value));
    });
  }
}

/// @nodoc
@JsonSerializable()
class _$AuctionLotExtendedImpl implements AuctionLotExtended {
  const _$AuctionLotExtendedImpl({
    required this.seq,
    required this.data,
    final String? $type,
  }) : $type = $type ?? 'lot.extended';

  factory _$AuctionLotExtendedImpl.fromJson(Map<String, dynamic> json) =>
      _$$AuctionLotExtendedImplFromJson(json);

  @override
  final int seq;
  @override
  final LotExtendedEvent data;

  @JsonKey(name: 'type')
  final String $type;

  @override
  String toString() {
    return 'AuctionEvent.lotExtended(seq: $seq, data: $data)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AuctionLotExtendedImpl &&
            (identical(other.seq, seq) || other.seq == seq) &&
            (identical(other.data, data) || other.data == data));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, seq, data);

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AuctionLotExtendedImplCopyWith<_$AuctionLotExtendedImpl> get copyWith =>
      __$$AuctionLotExtendedImplCopyWithImpl<_$AuctionLotExtendedImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(int seq, BidPlacedEvent data) bidPlaced,
    required TResult Function(int seq, LotExtendedEvent data) lotExtended,
    required TResult Function(int seq, LotClosedEvent data) lotClosed,
    required TResult Function(int seq) unknown,
  }) {
    return lotExtended(seq, data);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult? Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult? Function(int seq, LotClosedEvent data)? lotClosed,
    TResult? Function(int seq)? unknown,
  }) {
    return lotExtended?.call(seq, data);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult Function(int seq, LotClosedEvent data)? lotClosed,
    TResult Function(int seq)? unknown,
    required TResult orElse(),
  }) {
    if (lotExtended != null) {
      return lotExtended(seq, data);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(AuctionBidPlaced value) bidPlaced,
    required TResult Function(AuctionLotExtended value) lotExtended,
    required TResult Function(AuctionLotClosed value) lotClosed,
    required TResult Function(AuctionUnknownEvent value) unknown,
  }) {
    return lotExtended(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(AuctionBidPlaced value)? bidPlaced,
    TResult? Function(AuctionLotExtended value)? lotExtended,
    TResult? Function(AuctionLotClosed value)? lotClosed,
    TResult? Function(AuctionUnknownEvent value)? unknown,
  }) {
    return lotExtended?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(AuctionBidPlaced value)? bidPlaced,
    TResult Function(AuctionLotExtended value)? lotExtended,
    TResult Function(AuctionLotClosed value)? lotClosed,
    TResult Function(AuctionUnknownEvent value)? unknown,
    required TResult orElse(),
  }) {
    if (lotExtended != null) {
      return lotExtended(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$AuctionLotExtendedImplToJson(this);
  }
}

abstract class AuctionLotExtended implements AuctionEvent {
  const factory AuctionLotExtended({
    required final int seq,
    required final LotExtendedEvent data,
  }) = _$AuctionLotExtendedImpl;

  factory AuctionLotExtended.fromJson(Map<String, dynamic> json) =
      _$AuctionLotExtendedImpl.fromJson;

  @override
  int get seq;
  LotExtendedEvent get data;

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AuctionLotExtendedImplCopyWith<_$AuctionLotExtendedImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$AuctionLotClosedImplCopyWith<$Res>
    implements $AuctionEventCopyWith<$Res> {
  factory _$$AuctionLotClosedImplCopyWith(
    _$AuctionLotClosedImpl value,
    $Res Function(_$AuctionLotClosedImpl) then,
  ) = __$$AuctionLotClosedImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int seq, LotClosedEvent data});

  $LotClosedEventCopyWith<$Res> get data;
}

/// @nodoc
class __$$AuctionLotClosedImplCopyWithImpl<$Res>
    extends _$AuctionEventCopyWithImpl<$Res, _$AuctionLotClosedImpl>
    implements _$$AuctionLotClosedImplCopyWith<$Res> {
  __$$AuctionLotClosedImplCopyWithImpl(
    _$AuctionLotClosedImpl _value,
    $Res Function(_$AuctionLotClosedImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? seq = null, Object? data = null}) {
    return _then(
      _$AuctionLotClosedImpl(
        seq: null == seq
            ? _value.seq
            : seq // ignore: cast_nullable_to_non_nullable
                  as int,
        data: null == data
            ? _value.data
            : data // ignore: cast_nullable_to_non_nullable
                  as LotClosedEvent,
      ),
    );
  }

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $LotClosedEventCopyWith<$Res> get data {
    return $LotClosedEventCopyWith<$Res>(_value.data, (value) {
      return _then(_value.copyWith(data: value));
    });
  }
}

/// @nodoc
@JsonSerializable()
class _$AuctionLotClosedImpl implements AuctionLotClosed {
  const _$AuctionLotClosedImpl({
    required this.seq,
    required this.data,
    final String? $type,
  }) : $type = $type ?? 'lot.closed';

  factory _$AuctionLotClosedImpl.fromJson(Map<String, dynamic> json) =>
      _$$AuctionLotClosedImplFromJson(json);

  @override
  final int seq;
  @override
  final LotClosedEvent data;

  @JsonKey(name: 'type')
  final String $type;

  @override
  String toString() {
    return 'AuctionEvent.lotClosed(seq: $seq, data: $data)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AuctionLotClosedImpl &&
            (identical(other.seq, seq) || other.seq == seq) &&
            (identical(other.data, data) || other.data == data));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, seq, data);

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AuctionLotClosedImplCopyWith<_$AuctionLotClosedImpl> get copyWith =>
      __$$AuctionLotClosedImplCopyWithImpl<_$AuctionLotClosedImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(int seq, BidPlacedEvent data) bidPlaced,
    required TResult Function(int seq, LotExtendedEvent data) lotExtended,
    required TResult Function(int seq, LotClosedEvent data) lotClosed,
    required TResult Function(int seq) unknown,
  }) {
    return lotClosed(seq, data);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult? Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult? Function(int seq, LotClosedEvent data)? lotClosed,
    TResult? Function(int seq)? unknown,
  }) {
    return lotClosed?.call(seq, data);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult Function(int seq, LotClosedEvent data)? lotClosed,
    TResult Function(int seq)? unknown,
    required TResult orElse(),
  }) {
    if (lotClosed != null) {
      return lotClosed(seq, data);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(AuctionBidPlaced value) bidPlaced,
    required TResult Function(AuctionLotExtended value) lotExtended,
    required TResult Function(AuctionLotClosed value) lotClosed,
    required TResult Function(AuctionUnknownEvent value) unknown,
  }) {
    return lotClosed(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(AuctionBidPlaced value)? bidPlaced,
    TResult? Function(AuctionLotExtended value)? lotExtended,
    TResult? Function(AuctionLotClosed value)? lotClosed,
    TResult? Function(AuctionUnknownEvent value)? unknown,
  }) {
    return lotClosed?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(AuctionBidPlaced value)? bidPlaced,
    TResult Function(AuctionLotExtended value)? lotExtended,
    TResult Function(AuctionLotClosed value)? lotClosed,
    TResult Function(AuctionUnknownEvent value)? unknown,
    required TResult orElse(),
  }) {
    if (lotClosed != null) {
      return lotClosed(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$AuctionLotClosedImplToJson(this);
  }
}

abstract class AuctionLotClosed implements AuctionEvent {
  const factory AuctionLotClosed({
    required final int seq,
    required final LotClosedEvent data,
  }) = _$AuctionLotClosedImpl;

  factory AuctionLotClosed.fromJson(Map<String, dynamic> json) =
      _$AuctionLotClosedImpl.fromJson;

  @override
  int get seq;
  LotClosedEvent get data;

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AuctionLotClosedImplCopyWith<_$AuctionLotClosedImpl> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class _$$AuctionUnknownEventImplCopyWith<$Res>
    implements $AuctionEventCopyWith<$Res> {
  factory _$$AuctionUnknownEventImplCopyWith(
    _$AuctionUnknownEventImpl value,
    $Res Function(_$AuctionUnknownEventImpl) then,
  ) = __$$AuctionUnknownEventImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call({int seq});
}

/// @nodoc
class __$$AuctionUnknownEventImplCopyWithImpl<$Res>
    extends _$AuctionEventCopyWithImpl<$Res, _$AuctionUnknownEventImpl>
    implements _$$AuctionUnknownEventImplCopyWith<$Res> {
  __$$AuctionUnknownEventImplCopyWithImpl(
    _$AuctionUnknownEventImpl _value,
    $Res Function(_$AuctionUnknownEventImpl) _then,
  ) : super(_value, _then);

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({Object? seq = null}) {
    return _then(
      _$AuctionUnknownEventImpl(
        seq: null == seq
            ? _value.seq
            : seq // ignore: cast_nullable_to_non_nullable
                  as int,
      ),
    );
  }
}

/// @nodoc
@JsonSerializable()
class _$AuctionUnknownEventImpl implements AuctionUnknownEvent {
  const _$AuctionUnknownEventImpl({this.seq = 0, final String? $type})
    : $type = $type ?? 'unknown';

  factory _$AuctionUnknownEventImpl.fromJson(Map<String, dynamic> json) =>
      _$$AuctionUnknownEventImplFromJson(json);

  @override
  @JsonKey()
  final int seq;

  @JsonKey(name: 'type')
  final String $type;

  @override
  String toString() {
    return 'AuctionEvent.unknown(seq: $seq)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AuctionUnknownEventImpl &&
            (identical(other.seq, seq) || other.seq == seq));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(runtimeType, seq);

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AuctionUnknownEventImplCopyWith<_$AuctionUnknownEventImpl> get copyWith =>
      __$$AuctionUnknownEventImplCopyWithImpl<_$AuctionUnknownEventImpl>(
        this,
        _$identity,
      );

  @override
  @optionalTypeArgs
  TResult when<TResult extends Object?>({
    required TResult Function(int seq, BidPlacedEvent data) bidPlaced,
    required TResult Function(int seq, LotExtendedEvent data) lotExtended,
    required TResult Function(int seq, LotClosedEvent data) lotClosed,
    required TResult Function(int seq) unknown,
  }) {
    return unknown(seq);
  }

  @override
  @optionalTypeArgs
  TResult? whenOrNull<TResult extends Object?>({
    TResult? Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult? Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult? Function(int seq, LotClosedEvent data)? lotClosed,
    TResult? Function(int seq)? unknown,
  }) {
    return unknown?.call(seq);
  }

  @override
  @optionalTypeArgs
  TResult maybeWhen<TResult extends Object?>({
    TResult Function(int seq, BidPlacedEvent data)? bidPlaced,
    TResult Function(int seq, LotExtendedEvent data)? lotExtended,
    TResult Function(int seq, LotClosedEvent data)? lotClosed,
    TResult Function(int seq)? unknown,
    required TResult orElse(),
  }) {
    if (unknown != null) {
      return unknown(seq);
    }
    return orElse();
  }

  @override
  @optionalTypeArgs
  TResult map<TResult extends Object?>({
    required TResult Function(AuctionBidPlaced value) bidPlaced,
    required TResult Function(AuctionLotExtended value) lotExtended,
    required TResult Function(AuctionLotClosed value) lotClosed,
    required TResult Function(AuctionUnknownEvent value) unknown,
  }) {
    return unknown(this);
  }

  @override
  @optionalTypeArgs
  TResult? mapOrNull<TResult extends Object?>({
    TResult? Function(AuctionBidPlaced value)? bidPlaced,
    TResult? Function(AuctionLotExtended value)? lotExtended,
    TResult? Function(AuctionLotClosed value)? lotClosed,
    TResult? Function(AuctionUnknownEvent value)? unknown,
  }) {
    return unknown?.call(this);
  }

  @override
  @optionalTypeArgs
  TResult maybeMap<TResult extends Object?>({
    TResult Function(AuctionBidPlaced value)? bidPlaced,
    TResult Function(AuctionLotExtended value)? lotExtended,
    TResult Function(AuctionLotClosed value)? lotClosed,
    TResult Function(AuctionUnknownEvent value)? unknown,
    required TResult orElse(),
  }) {
    if (unknown != null) {
      return unknown(this);
    }
    return orElse();
  }

  @override
  Map<String, dynamic> toJson() {
    return _$$AuctionUnknownEventImplToJson(this);
  }
}

abstract class AuctionUnknownEvent implements AuctionEvent {
  const factory AuctionUnknownEvent({final int seq}) =
      _$AuctionUnknownEventImpl;

  factory AuctionUnknownEvent.fromJson(Map<String, dynamic> json) =
      _$AuctionUnknownEventImpl.fromJson;

  @override
  int get seq;

  /// Create a copy of AuctionEvent
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AuctionUnknownEventImplCopyWith<_$AuctionUnknownEventImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
