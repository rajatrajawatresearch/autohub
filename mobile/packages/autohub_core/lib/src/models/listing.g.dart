// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'listing.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$SpecGroupImpl _$$SpecGroupImplFromJson(Map<String, dynamic> json) =>
    _$SpecGroupImpl(
      code: json['code'] as String,
      label: json['label'] as String,
      items:
          (json['items'] as List<dynamic>?)
              ?.map((e) => SpecItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <SpecItem>[],
    );

Map<String, dynamic> _$$SpecGroupImplToJson(_$SpecGroupImpl instance) =>
    <String, dynamic>{
      'code': instance.code,
      'label': instance.label,
      'items': instance.items,
    };

_$SellerImpl _$$SellerImplFromJson(Map<String, dynamic> json) => _$SellerImpl(
  id: json['id'] as String,
  displayName: json['displayName'] as String,
  type: json['type'] as String? ?? 'individual',
  city: json['city'] as String?,
  isVerified: json['isVerified'] as bool? ?? false,
  memberSince: json['memberSince'] as String?,
  listingsCount: (json['listingsCount'] as num?)?.toInt(),
  responseTimeMinutes: (json['responseTimeMinutes'] as num?)?.toInt(),
);

Map<String, dynamic> _$$SellerImplToJson(_$SellerImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'displayName': instance.displayName,
      'type': instance.type,
      'city': instance.city,
      'isVerified': instance.isVerified,
      'memberSince': instance.memberSince,
      'listingsCount': instance.listingsCount,
      'responseTimeMinutes': instance.responseTimeMinutes,
    };

_$ListingDetailImpl _$$ListingDetailImplFromJson(
  Map<String, dynamic> json,
) => _$ListingDetailImpl(
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
      (json['badges'] as List<dynamic>?)?.map((e) => e as String).toList() ??
      const <String>[],
  dealerName: json['dealerName'] as String?,
  description: json['description'] as String?,
  currency: json['currency'] as String? ?? 'INR',
  gallery:
      (json['gallery'] as List<dynamic>?)
          ?.map((e) => MediaAsset.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <MediaAsset>[],
  attributes:
      json['attributes'] as Map<String, dynamic>? ?? const <String, Object?>{},
  specGroups:
      (json['specGroups'] as List<dynamic>?)
          ?.map((e) => SpecGroup.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <SpecGroup>[],
  seller: json['seller'] == null
      ? null
      : Seller.fromJson(json['seller'] as Map<String, dynamic>),
  workflow: json['workflow'] == null
      ? null
      : WorkflowSnapshot.fromJson(json['workflow'] as Map<String, dynamic>),
  categoryCode: json['categoryCode'] as String?,
  makeCode: json['makeCode'] as String?,
  modelCode: json['modelCode'] as String?,
  variantCode: json['variantCode'] as String?,
  year: (json['year'] as num?)?.toInt(),
  locality: json['locality'] as String?,
  createdAt: json['createdAt'] as String?,
  expiresAt: json['expiresAt'] as String?,
  viewCount: (json['viewCount'] as num?)?.toInt(),
);

Map<String, dynamic> _$$ListingDetailImplToJson(_$ListingDetailImpl instance) =>
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
      'description': instance.description,
      'currency': instance.currency,
      'gallery': instance.gallery,
      'attributes': instance.attributes,
      'specGroups': instance.specGroups,
      'seller': instance.seller,
      'workflow': instance.workflow,
      'categoryCode': instance.categoryCode,
      'makeCode': instance.makeCode,
      'modelCode': instance.modelCode,
      'variantCode': instance.variantCode,
      'year': instance.year,
      'locality': instance.locality,
      'createdAt': instance.createdAt,
      'expiresAt': instance.expiresAt,
      'viewCount': instance.viewCount,
    };

const _$ListingIntentEnumMap = {
  ListingIntent.sale: 'sale',
  ListingIntent.auction: 'auction',
  ListingIntent.rental: 'rental',
  ListingIntent.lease: 'lease',
  ListingIntent.attachment: 'attachment',
  ListingIntent.exchange: 'exchange',
};

_$ListingDraftRequestImpl _$$ListingDraftRequestImplFromJson(
  Map<String, dynamic> json,
) => _$ListingDraftRequestImpl(
  intent: $enumDecode(_$ListingIntentEnumMap, json['intent']),
  categoryCode: json['categoryCode'] as String,
  registrationNumber: json['registrationNumber'] as String?,
  values: json['values'] as Map<String, dynamic>? ?? const <String, Object?>{},
  askingPriceMinor: (json['askingPriceMinor'] as num?)?.toInt(),
);

Map<String, dynamic> _$$ListingDraftRequestImplToJson(
  _$ListingDraftRequestImpl instance,
) => <String, dynamic>{
  'intent': _$ListingIntentEnumMap[instance.intent]!,
  'categoryCode': instance.categoryCode,
  'registrationNumber': instance.registrationNumber,
  'values': instance.values,
  'askingPriceMinor': instance.askingPriceMinor,
};
