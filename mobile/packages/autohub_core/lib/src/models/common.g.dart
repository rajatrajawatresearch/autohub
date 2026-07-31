// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'common.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$OptionImpl _$$OptionImplFromJson(Map<String, dynamic> json) => _$OptionImpl(
  value: json['value'] as String,
  label: json['label'] as String,
  sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
  icon: json['icon'] as String?,
  count: (json['count'] as num?)?.toInt(),
);

Map<String, dynamic> _$$OptionImplToJson(_$OptionImpl instance) =>
    <String, dynamic>{
      'value': instance.value,
      'label': instance.label,
      'sortOrder': instance.sortOrder,
      'icon': instance.icon,
      'count': instance.count,
    };

_$MoneyAmountImpl _$$MoneyAmountImplFromJson(Map<String, dynamic> json) =>
    _$MoneyAmountImpl(
      minorUnits: (json['minorUnits'] as num).toInt(),
      currency: json['currency'] as String? ?? 'INR',
    );

Map<String, dynamic> _$$MoneyAmountImplToJson(_$MoneyAmountImpl instance) =>
    <String, dynamic>{
      'minorUnits': instance.minorUnits,
      'currency': instance.currency,
    };

_$RegistrationImpl _$$RegistrationImplFromJson(Map<String, dynamic> json) =>
    _$RegistrationImpl(
      canonical: json['canonical'] as String,
      display: json['display'] as String,
      plateClass: json['plateClass'] as String?,
      stateCode: json['stateCode'] as String?,
    );

Map<String, dynamic> _$$RegistrationImplToJson(_$RegistrationImpl instance) =>
    <String, dynamic>{
      'canonical': instance.canonical,
      'display': instance.display,
      'plateClass': instance.plateClass,
      'stateCode': instance.stateCode,
    };

_$SpecItemImpl _$$SpecItemImplFromJson(Map<String, dynamic> json) =>
    _$SpecItemImpl(
      label: json['label'] as String,
      value: json['value'] as String,
      code: json['code'] as String?,
    );

Map<String, dynamic> _$$SpecItemImplToJson(_$SpecItemImpl instance) =>
    <String, dynamic>{
      'label': instance.label,
      'value': instance.value,
      'code': instance.code,
    };

_$MediaImpl _$$MediaImplFromJson(Map<String, dynamic> json) => _$MediaImpl(
  coverUrl: json['coverUrl'] as String?,
  aspectRatio: (json['aspectRatio'] as num?)?.toDouble(),
  count: (json['count'] as num?)?.toInt() ?? 0,
);

Map<String, dynamic> _$$MediaImplToJson(_$MediaImpl instance) =>
    <String, dynamic>{
      'coverUrl': instance.coverUrl,
      'aspectRatio': instance.aspectRatio,
      'count': instance.count,
    };

_$MediaAssetImpl _$$MediaAssetImplFromJson(Map<String, dynamic> json) =>
    _$MediaAssetImpl(
      id: json['id'] as String,
      url: json['url'] as String,
      aspectRatio: (json['aspectRatio'] as num?)?.toDouble() ?? 1.5,
      kind: json['kind'] as String? ?? 'photo',
      caption: json['caption'] as String?,
    );

Map<String, dynamic> _$$MediaAssetImplToJson(_$MediaAssetImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'url': instance.url,
      'aspectRatio': instance.aspectRatio,
      'kind': instance.kind,
      'caption': instance.caption,
    };
