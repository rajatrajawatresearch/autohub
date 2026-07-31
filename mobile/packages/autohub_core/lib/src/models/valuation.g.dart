// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'valuation.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$ValuationQuoteRequestImpl _$$ValuationQuoteRequestImplFromJson(
  Map<String, dynamic> json,
) => _$ValuationQuoteRequestImpl(
  registrationNumber: json['registrationNumber'] as String,
  kmsDriven: (json['kmsDriven'] as num?)?.toInt(),
  city: json['city'] as String?,
  condition: json['condition'] as String?,
  makeCode: json['makeCode'] as String?,
  modelCode: json['modelCode'] as String?,
  variantCode: json['variantCode'] as String?,
  year: (json['year'] as num?)?.toInt(),
);

Map<String, dynamic> _$$ValuationQuoteRequestImplToJson(
  _$ValuationQuoteRequestImpl instance,
) => <String, dynamic>{
  'registrationNumber': instance.registrationNumber,
  'kmsDriven': instance.kmsDriven,
  'city': instance.city,
  'condition': instance.condition,
  'makeCode': instance.makeCode,
  'modelCode': instance.modelCode,
  'variantCode': instance.variantCode,
  'year': instance.year,
};

_$ValuationVehicleImpl _$$ValuationVehicleImplFromJson(
  Map<String, dynamic> json,
) => _$ValuationVehicleImpl(
  title: json['title'] as String,
  registration: Registration.fromJson(
    json['registration'] as Map<String, dynamic>,
  ),
  year: (json['year'] as num?)?.toInt(),
  fuel: json['fuel'] as String?,
  transmission: json['transmission'] as String?,
  source: json['source'] as String?,
);

Map<String, dynamic> _$$ValuationVehicleImplToJson(
  _$ValuationVehicleImpl instance,
) => <String, dynamic>{
  'title': instance.title,
  'registration': instance.registration,
  'year': instance.year,
  'fuel': instance.fuel,
  'transmission': instance.transmission,
  'source': instance.source,
};

_$ValuationBandImpl _$$ValuationBandImplFromJson(Map<String, dynamic> json) =>
    _$ValuationBandImpl(
      code: json['code'] as String,
      label: json['label'] as String,
      amountMinor: (json['amountMinor'] as num).toInt(),
      isPrimary: json['isPrimary'] as bool? ?? false,
      helpText: json['helpText'] as String?,
    );

Map<String, dynamic> _$$ValuationBandImplToJson(_$ValuationBandImpl instance) =>
    <String, dynamic>{
      'code': instance.code,
      'label': instance.label,
      'amountMinor': instance.amountMinor,
      'isPrimary': instance.isPrimary,
      'helpText': instance.helpText,
    };

_$ValuationRangeImpl _$$ValuationRangeImplFromJson(Map<String, dynamic> json) =>
    _$ValuationRangeImpl(
      lowMinor: (json['lowMinor'] as num).toInt(),
      highMinor: (json['highMinor'] as num).toInt(),
    );

Map<String, dynamic> _$$ValuationRangeImplToJson(
  _$ValuationRangeImpl instance,
) => <String, dynamic>{
  'lowMinor': instance.lowMinor,
  'highMinor': instance.highMinor,
};

_$ValuationConfidenceImpl _$$ValuationConfidenceImplFromJson(
  Map<String, dynamic> json,
) => _$ValuationConfidenceImpl(
  score: (json['score'] as num).toDouble(),
  band:
      $enumDecodeNullable(
        _$ConfidenceBandEnumMap,
        json['band'],
        unknownValue: ConfidenceBand.medium,
      ) ??
      ConfidenceBand.medium,
  basedOn: json['basedOn'] as String?,
);

Map<String, dynamic> _$$ValuationConfidenceImplToJson(
  _$ValuationConfidenceImpl instance,
) => <String, dynamic>{
  'score': instance.score,
  'band': _$ConfidenceBandEnumMap[instance.band]!,
  'basedOn': instance.basedOn,
};

const _$ConfidenceBandEnumMap = {
  ConfidenceBand.high: 'high',
  ConfidenceBand.medium: 'medium',
  ConfidenceBand.low: 'low',
};

_$ValuationQuoteImpl _$$ValuationQuoteImplFromJson(Map<String, dynamic> json) =>
    _$ValuationQuoteImpl(
      quoteId: json['quoteId'] as String,
      vehicle: ValuationVehicle.fromJson(
        json['vehicle'] as Map<String, dynamic>,
      ),
      currency: json['currency'] as String? ?? 'INR',
      valuations:
          (json['valuations'] as List<dynamic>?)
              ?.map((e) => ValuationBand.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <ValuationBand>[],
      range: ValuationRange.fromJson(json['range'] as Map<String, dynamic>),
      confidence: ValuationConfidence.fromJson(
        json['confidence'] as Map<String, dynamic>,
      ),
      asOf: json['asOf'] as String,
      expiresAt: json['expiresAt'] as String,
      modelVersion: json['modelVersion'] as String?,
    );

Map<String, dynamic> _$$ValuationQuoteImplToJson(
  _$ValuationQuoteImpl instance,
) => <String, dynamic>{
  'quoteId': instance.quoteId,
  'vehicle': instance.vehicle,
  'currency': instance.currency,
  'valuations': instance.valuations,
  'range': instance.range,
  'confidence': instance.confidence,
  'asOf': instance.asOf,
  'expiresAt': instance.expiresAt,
  'modelVersion': instance.modelVersion,
};
