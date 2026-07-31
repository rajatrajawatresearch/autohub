// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'search.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$SearchItemImpl _$$SearchItemImplFromJson(Map<String, dynamic> json) =>
    _$SearchItemImpl(
      listingId: json['listingId'] as String,
      intent:
          $enumDecodeNullable(
            _$ListingIntentEnumMap,
            json['intent'],
            unknownValue: ListingIntent.sale,
          ) ??
          ListingIntent.sale,
      registration: Registration.fromJson(
        json['registration'] as Map<String, dynamic>,
      ),
      title: json['title'] as String,
      priceMinor: (json['priceMinor'] as num?)?.toInt(),
      pricePeriod: json['pricePeriod'] as String?,
      city: json['city'] as String? ?? '',
      specStrip:
          (json['specStrip'] as List<dynamic>?)
              ?.map((e) => SpecItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <SpecItem>[],
      media: json['media'] == null
          ? const Media()
          : Media.fromJson(json['media'] as Map<String, dynamic>),
      badges:
          (json['badges'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          const <String>[],
      dealerName: json['dealerName'] as String?,
    );

Map<String, dynamic> _$$SearchItemImplToJson(_$SearchItemImpl instance) =>
    <String, dynamic>{
      'listingId': instance.listingId,
      'intent': _$ListingIntentEnumMap[instance.intent]!,
      'registration': instance.registration,
      'title': instance.title,
      'priceMinor': instance.priceMinor,
      'pricePeriod': instance.pricePeriod,
      'city': instance.city,
      'specStrip': instance.specStrip,
      'media': instance.media,
      'badges': instance.badges,
      'dealerName': instance.dealerName,
    };

const _$ListingIntentEnumMap = {
  ListingIntent.sale: 'sale',
  ListingIntent.auction: 'auction',
  ListingIntent.rental: 'rental',
  ListingIntent.lease: 'lease',
  ListingIntent.attachment: 'attachment',
  ListingIntent.exchange: 'exchange',
};

_$SearchFacetChipsImpl _$$SearchFacetChipsImplFromJson(
  Map<String, dynamic> json,
) => _$SearchFacetChipsImpl(
  code: json['code'] as String,
  label: json['label'] as String,
  options:
      (json['options'] as List<dynamic>?)
          ?.map((e) => Option.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <Option>[],
  $type: json['style'] as String?,
);

Map<String, dynamic> _$$SearchFacetChipsImplToJson(
  _$SearchFacetChipsImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'label': instance.label,
  'options': instance.options,
  'style': instance.$type,
};

_$SearchFacetListImpl _$$SearchFacetListImplFromJson(
  Map<String, dynamic> json,
) => _$SearchFacetListImpl(
  code: json['code'] as String,
  label: json['label'] as String,
  options:
      (json['options'] as List<dynamic>?)
          ?.map((e) => Option.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <Option>[],
  $type: json['style'] as String?,
);

Map<String, dynamic> _$$SearchFacetListImplToJson(
  _$SearchFacetListImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'label': instance.label,
  'options': instance.options,
  'style': instance.$type,
};

_$SearchFacetToggleImpl _$$SearchFacetToggleImplFromJson(
  Map<String, dynamic> json,
) => _$SearchFacetToggleImpl(
  code: json['code'] as String,
  label: json['label'] as String,
  options:
      (json['options'] as List<dynamic>?)
          ?.map((e) => Option.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <Option>[],
  $type: json['style'] as String?,
);

Map<String, dynamic> _$$SearchFacetToggleImplToJson(
  _$SearchFacetToggleImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'label': instance.label,
  'options': instance.options,
  'style': instance.$type,
};

_$SearchFacetRangeImpl _$$SearchFacetRangeImplFromJson(
  Map<String, dynamic> json,
) => _$SearchFacetRangeImpl(
  code: json['code'] as String,
  label: json['label'] as String,
  min: (json['min'] as num).toDouble(),
  max: (json['max'] as num).toDouble(),
  step: (json['step'] as num?)?.toDouble(),
  unit: json['unit'] as String?,
  $type: json['style'] as String?,
);

Map<String, dynamic> _$$SearchFacetRangeImplToJson(
  _$SearchFacetRangeImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'label': instance.label,
  'min': instance.min,
  'max': instance.max,
  'step': instance.step,
  'unit': instance.unit,
  'style': instance.$type,
};

_$SearchFacetUnsupportedImpl _$$SearchFacetUnsupportedImplFromJson(
  Map<String, dynamic> json,
) => _$SearchFacetUnsupportedImpl(
  code: json['code'] as String? ?? '',
  label: json['label'] as String? ?? '',
  $type: json['style'] as String?,
);

Map<String, dynamic> _$$SearchFacetUnsupportedImplToJson(
  _$SearchFacetUnsupportedImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'label': instance.label,
  'style': instance.$type,
};

_$SearchResponseImpl _$$SearchResponseImplFromJson(Map<String, dynamic> json) =>
    _$SearchResponseImpl(
      items:
          (json['items'] as List<dynamic>?)
              ?.map((e) => SearchItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <SearchItem>[],
      facets:
          (json['facets'] as List<dynamic>?)
              ?.map((e) => SearchFacet.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <SearchFacet>[],
      nextCursor: json['nextCursor'] as String?,
      totalEstimate: (json['totalEstimate'] as num?)?.toInt(),
    );

Map<String, dynamic> _$$SearchResponseImplToJson(
  _$SearchResponseImpl instance,
) => <String, dynamic>{
  'items': instance.items,
  'facets': instance.facets,
  'nextCursor': instance.nextCursor,
  'totalEstimate': instance.totalEstimate,
};
