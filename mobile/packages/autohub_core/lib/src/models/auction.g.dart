// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'auction.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$BidImpl _$$BidImplFromJson(Map<String, dynamic> json) => _$BidImpl(
  bidId: json['bidId'] as String,
  lotId: json['lotId'] as String,
  seq: (json['seq'] as num?)?.toInt() ?? 0,
  amountMinor: (json['amountMinor'] as num).toInt(),
  bidderMask: json['bidderMask'] as String? ?? '',
  placedAt: json['placedAt'] as String,
  isYours: json['isYours'] as bool? ?? false,
  isAutoBid: json['isAutoBid'] as bool? ?? false,
);

Map<String, dynamic> _$$BidImplToJson(_$BidImpl instance) => <String, dynamic>{
  'bidId': instance.bidId,
  'lotId': instance.lotId,
  'seq': instance.seq,
  'amountMinor': instance.amountMinor,
  'bidderMask': instance.bidderMask,
  'placedAt': instance.placedAt,
  'isYours': instance.isYours,
  'isAutoBid': instance.isAutoBid,
};

_$AuctionLotImpl _$$AuctionLotImplFromJson(Map<String, dynamic> json) =>
    _$AuctionLotImpl(
      lotId: json['lotId'] as String,
      lotNumber: json['lotNumber'] as String?,
      listing: SearchItem.fromJson(json['listing'] as Map<String, dynamic>),
      currency: json['currency'] as String? ?? 'INR',
      state: WorkflowState.fromJson(json['state'] as Map<String, dynamic>),
      startsAt: json['startsAt'] as String,
      endsAt: json['endsAt'] as String,
      startPriceMinor: (json['startPriceMinor'] as num).toInt(),
      incrementMinor: (json['incrementMinor'] as num).toInt(),
      currentBidMinor: (json['currentBidMinor'] as num?)?.toInt(),
      minNextBidMinor: (json['minNextBidMinor'] as num).toInt(),
      bidCount: (json['bidCount'] as num?)?.toInt() ?? 0,
      reserveMet: json['reserveMet'] as bool? ?? false,
      latestSeq: (json['latestSeq'] as num?)?.toInt() ?? 0,
      highestBidderIsYou: json['highestBidderIsYou'] as bool? ?? false,
      yourMaxBidMinor: (json['yourMaxBidMinor'] as num?)?.toInt(),
      extensionCount: (json['extensionCount'] as num?)?.toInt() ?? 0,
      inspectionReportUrl: json['inspectionReportUrl'] as String?,
    );

Map<String, dynamic> _$$AuctionLotImplToJson(_$AuctionLotImpl instance) =>
    <String, dynamic>{
      'lotId': instance.lotId,
      'lotNumber': instance.lotNumber,
      'listing': instance.listing,
      'currency': instance.currency,
      'state': instance.state,
      'startsAt': instance.startsAt,
      'endsAt': instance.endsAt,
      'startPriceMinor': instance.startPriceMinor,
      'incrementMinor': instance.incrementMinor,
      'currentBidMinor': instance.currentBidMinor,
      'minNextBidMinor': instance.minNextBidMinor,
      'bidCount': instance.bidCount,
      'reserveMet': instance.reserveMet,
      'latestSeq': instance.latestSeq,
      'highestBidderIsYou': instance.highestBidderIsYou,
      'yourMaxBidMinor': instance.yourMaxBidMinor,
      'extensionCount': instance.extensionCount,
      'inspectionReportUrl': instance.inspectionReportUrl,
    };

_$PlaceBidRequestImpl _$$PlaceBidRequestImplFromJson(
  Map<String, dynamic> json,
) => _$PlaceBidRequestImpl(
  amountMinor: (json['amountMinor'] as num).toInt(),
  seenBidSeq: (json['seenBidSeq'] as num?)?.toInt(),
  maxAmountMinor: (json['maxAmountMinor'] as num?)?.toInt(),
);

Map<String, dynamic> _$$PlaceBidRequestImplToJson(
  _$PlaceBidRequestImpl instance,
) => <String, dynamic>{
  'amountMinor': instance.amountMinor,
  'seenBidSeq': instance.seenBidSeq,
  'maxAmountMinor': instance.maxAmountMinor,
};

_$PlaceBidResponseImpl _$$PlaceBidResponseImplFromJson(
  Map<String, dynamic> json,
) => _$PlaceBidResponseImpl(
  bid: Bid.fromJson(json['bid'] as Map<String, dynamic>),
  lot: AuctionLot.fromJson(json['lot'] as Map<String, dynamic>),
);

Map<String, dynamic> _$$PlaceBidResponseImplToJson(
  _$PlaceBidResponseImpl instance,
) => <String, dynamic>{'bid': instance.bid, 'lot': instance.lot};

_$BidPlacedEventImpl _$$BidPlacedEventImplFromJson(Map<String, dynamic> json) =>
    _$BidPlacedEventImpl(
      lotId: json['lotId'] as String,
      amountMinor: (json['amountMinor'] as num).toInt(),
      bidderMask: json['bidderMask'] as String? ?? '',
      placedAt: json['placedAt'] as String,
      highestBidderIsYou: json['highestBidderIsYou'] as bool? ?? false,
      bidCount: (json['bidCount'] as num?)?.toInt(),
      minNextBidMinor: (json['minNextBidMinor'] as num?)?.toInt(),
    );

Map<String, dynamic> _$$BidPlacedEventImplToJson(
  _$BidPlacedEventImpl instance,
) => <String, dynamic>{
  'lotId': instance.lotId,
  'amountMinor': instance.amountMinor,
  'bidderMask': instance.bidderMask,
  'placedAt': instance.placedAt,
  'highestBidderIsYou': instance.highestBidderIsYou,
  'bidCount': instance.bidCount,
  'minNextBidMinor': instance.minNextBidMinor,
};

_$LotExtendedEventImpl _$$LotExtendedEventImplFromJson(
  Map<String, dynamic> json,
) => _$LotExtendedEventImpl(
  endsAt: json['endsAt'] as String,
  reason: json['reason'] as String? ?? '',
);

Map<String, dynamic> _$$LotExtendedEventImplToJson(
  _$LotExtendedEventImpl instance,
) => <String, dynamic>{'endsAt': instance.endsAt, 'reason': instance.reason};

_$LotClosedEventImpl _$$LotClosedEventImplFromJson(Map<String, dynamic> json) =>
    _$LotClosedEventImpl(
      outcome: json['outcome'] as String? ?? '',
      winnerIsYou: json['winnerIsYou'] as bool? ?? false,
      finalAmountMinor: (json['finalAmountMinor'] as num?)?.toInt(),
    );

Map<String, dynamic> _$$LotClosedEventImplToJson(
  _$LotClosedEventImpl instance,
) => <String, dynamic>{
  'outcome': instance.outcome,
  'winnerIsYou': instance.winnerIsYou,
  'finalAmountMinor': instance.finalAmountMinor,
};

_$AuctionBidPlacedImpl _$$AuctionBidPlacedImplFromJson(
  Map<String, dynamic> json,
) => _$AuctionBidPlacedImpl(
  seq: (json['seq'] as num).toInt(),
  data: BidPlacedEvent.fromJson(json['data'] as Map<String, dynamic>),
  $type: json['type'] as String?,
);

Map<String, dynamic> _$$AuctionBidPlacedImplToJson(
  _$AuctionBidPlacedImpl instance,
) => <String, dynamic>{
  'seq': instance.seq,
  'data': instance.data,
  'type': instance.$type,
};

_$AuctionLotExtendedImpl _$$AuctionLotExtendedImplFromJson(
  Map<String, dynamic> json,
) => _$AuctionLotExtendedImpl(
  seq: (json['seq'] as num).toInt(),
  data: LotExtendedEvent.fromJson(json['data'] as Map<String, dynamic>),
  $type: json['type'] as String?,
);

Map<String, dynamic> _$$AuctionLotExtendedImplToJson(
  _$AuctionLotExtendedImpl instance,
) => <String, dynamic>{
  'seq': instance.seq,
  'data': instance.data,
  'type': instance.$type,
};

_$AuctionLotClosedImpl _$$AuctionLotClosedImplFromJson(
  Map<String, dynamic> json,
) => _$AuctionLotClosedImpl(
  seq: (json['seq'] as num).toInt(),
  data: LotClosedEvent.fromJson(json['data'] as Map<String, dynamic>),
  $type: json['type'] as String?,
);

Map<String, dynamic> _$$AuctionLotClosedImplToJson(
  _$AuctionLotClosedImpl instance,
) => <String, dynamic>{
  'seq': instance.seq,
  'data': instance.data,
  'type': instance.$type,
};

_$AuctionUnknownEventImpl _$$AuctionUnknownEventImplFromJson(
  Map<String, dynamic> json,
) => _$AuctionUnknownEventImpl(
  seq: (json['seq'] as num?)?.toInt() ?? 0,
  $type: json['type'] as String?,
);

Map<String, dynamic> _$$AuctionUnknownEventImplToJson(
  _$AuctionUnknownEventImpl instance,
) => <String, dynamic>{'seq': instance.seq, 'type': instance.$type};
