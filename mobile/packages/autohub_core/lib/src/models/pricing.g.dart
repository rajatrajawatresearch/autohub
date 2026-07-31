// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'pricing.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$PricingVehicleContextImpl _$$PricingVehicleContextImplFromJson(
  Map<String, dynamic> json,
) => _$PricingVehicleContextImpl(
  category: json['category'] as String?,
  makeCode: json['makeCode'] as String?,
  modelCode: json['modelCode'] as String?,
  year: (json['year'] as num?)?.toInt(),
);

Map<String, dynamic> _$$PricingVehicleContextImplToJson(
  _$PricingVehicleContextImpl instance,
) => <String, dynamic>{
  'category': instance.category,
  'makeCode': instance.makeCode,
  'modelCode': instance.modelCode,
  'year': instance.year,
};

_$PricingLocationContextImpl _$$PricingLocationContextImplFromJson(
  Map<String, dynamic> json,
) => _$PricingLocationContextImpl(
  state: json['state'] as String?,
  city: json['city'] as String?,
);

Map<String, dynamic> _$$PricingLocationContextImplToJson(
  _$PricingLocationContextImpl instance,
) => <String, dynamic>{'state': instance.state, 'city': instance.city};

_$PricingPartyContextImpl _$$PricingPartyContextImplFromJson(
  Map<String, dynamic> json,
) => _$PricingPartyContextImpl(
  type: json['type'] as String?,
  tier: json['tier'] as String?,
  id: json['id'] as String?,
);

Map<String, dynamic> _$$PricingPartyContextImplToJson(
  _$PricingPartyContextImpl instance,
) => <String, dynamic>{
  'type': instance.type,
  'tier': instance.tier,
  'id': instance.id,
};

_$PricingAmountContextImpl _$$PricingAmountContextImplFromJson(
  Map<String, dynamic> json,
) => _$PricingAmountContextImpl(
  minorUnits: (json['minorUnits'] as num).toInt(),
  currency: json['currency'] as String? ?? 'INR',
);

Map<String, dynamic> _$$PricingAmountContextImplToJson(
  _$PricingAmountContextImpl instance,
) => <String, dynamic>{
  'minorUnits': instance.minorUnits,
  'currency': instance.currency,
};

_$PricingContextImpl _$$PricingContextImplFromJson(
  Map<String, dynamic> json,
) => _$PricingContextImpl(
  vehicle: json['vehicle'] == null
      ? null
      : PricingVehicleContext.fromJson(json['vehicle'] as Map<String, dynamic>),
  location: json['location'] == null
      ? null
      : PricingLocationContext.fromJson(
          json['location'] as Map<String, dynamic>,
        ),
  party: json['party'] == null
      ? null
      : PricingPartyContext.fromJson(json['party'] as Map<String, dynamic>),
  amount: json['amount'] == null
      ? null
      : PricingAmountContext.fromJson(json['amount'] as Map<String, dynamic>),
);

Map<String, dynamic> _$$PricingContextImplToJson(
  _$PricingContextImpl instance,
) => <String, dynamic>{
  'vehicle': instance.vehicle,
  'location': instance.location,
  'party': instance.party,
  'amount': instance.amount,
};

_$PricingQuoteRequestImpl _$$PricingQuoteRequestImplFromJson(
  Map<String, dynamic> json,
) => _$PricingQuoteRequestImpl(
  scope: json['scope'] as String,
  context: PricingContext.fromJson(json['context'] as Map<String, dynamic>),
);

Map<String, dynamic> _$$PricingQuoteRequestImplToJson(
  _$PricingQuoteRequestImpl instance,
) => <String, dynamic>{'scope': instance.scope, 'context': instance.context};

_$PricingLineImpl _$$PricingLineImplFromJson(Map<String, dynamic> json) =>
    _$PricingLineImpl(
      code: json['code'] as String,
      label: json['label'] as String,
      basis:
          $enumDecodeNullable(
            _$PricingBasisEnumMap,
            json['basis'],
            unknownValue: PricingBasis.flat,
          ) ??
          PricingBasis.flat,
      rate: (json['rate'] as num?)?.toDouble(),
      amountMinor: (json['amountMinor'] as num).toInt(),
      appliesTo: (json['appliesTo'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
      ruleId: json['ruleId'] as String?,
      ruleName: json['ruleName'] as String?,
      hsnSac: json['hsnSac'] as String?,
    );

Map<String, dynamic> _$$PricingLineImplToJson(_$PricingLineImpl instance) =>
    <String, dynamic>{
      'code': instance.code,
      'label': instance.label,
      'basis': _$PricingBasisEnumMap[instance.basis]!,
      'rate': instance.rate,
      'amountMinor': instance.amountMinor,
      'appliesTo': instance.appliesTo,
      'ruleId': instance.ruleId,
      'ruleName': instance.ruleName,
      'hsnSac': instance.hsnSac,
    };

const _$PricingBasisEnumMap = {
  PricingBasis.percent: 'percent',
  PricingBasis.flat: 'flat',
  PricingBasis.percentOfLines: 'percent_of_lines',
  PricingBasis.slab: 'slab',
  PricingBasis.perUnit: 'per_unit',
};

_$PricingQuoteImpl _$$PricingQuoteImplFromJson(Map<String, dynamic> json) =>
    _$PricingQuoteImpl(
      currency: json['currency'] as String? ?? 'INR',
      baseAmount: (json['baseAmount'] as num).toInt(),
      lines:
          (json['lines'] as List<dynamic>?)
              ?.map((e) => PricingLine.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <PricingLine>[],
      totalFeesMinor: (json['totalFeesMinor'] as num).toInt(),
      payableMinor: (json['payableMinor'] as num).toInt(),
      explanation: json['explanation'] as String?,
    );

Map<String, dynamic> _$$PricingQuoteImplToJson(_$PricingQuoteImpl instance) =>
    <String, dynamic>{
      'currency': instance.currency,
      'baseAmount': instance.baseAmount,
      'lines': instance.lines,
      'totalFeesMinor': instance.totalFeesMinor,
      'payableMinor': instance.payableMinor,
      'explanation': instance.explanation,
    };
