// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'platform.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$FeaturesImpl _$$FeaturesImplFromJson(Map<String, dynamic> json) =>
    _$FeaturesImpl(
      version: (json['version'] as num).toInt(),
      modules:
          (json['modules'] as Map<String, dynamic>?)?.map(
            (k, e) => MapEntry(k, e as bool),
          ) ??
          const <String, bool>{},
      flags:
          (json['flags'] as Map<String, dynamic>?)?.map(
            (k, e) => MapEntry(k, e as bool),
          ) ??
          const <String, bool>{},
    );

Map<String, dynamic> _$$FeaturesImplToJson(_$FeaturesImpl instance) =>
    <String, dynamic>{
      'version': instance.version,
      'modules': instance.modules,
      'flags': instance.flags,
    };

_$I18nBundleImpl _$$I18nBundleImplFromJson(Map<String, dynamic> json) =>
    _$I18nBundleImpl(
      locale: json['locale'] as String,
      version: (json['version'] as num).toInt(),
      resources:
          (json['resources'] as Map<String, dynamic>?)?.map(
            (k, e) => MapEntry(k, e as String),
          ) ??
          const <String, String>{},
    );

Map<String, dynamic> _$$I18nBundleImplToJson(_$I18nBundleImpl instance) =>
    <String, dynamic>{
      'locale': instance.locale,
      'version': instance.version,
      'resources': instance.resources,
    };

_$CityImpl _$$CityImplFromJson(Map<String, dynamic> json) => _$CityImpl(
  code: json['code'] as String,
  name: json['name'] as String,
  stateCode: json['stateCode'] as String,
  lat: (json['lat'] as num?)?.toDouble(),
  lng: (json['lng'] as num?)?.toDouble(),
  serviceable: json['serviceable'] as bool? ?? false,
  modules:
      (json['modules'] as List<dynamic>?)?.map((e) => e as String).toList() ??
      const <String>[],
);

Map<String, dynamic> _$$CityImplToJson(_$CityImpl instance) =>
    <String, dynamic>{
      'code': instance.code,
      'name': instance.name,
      'stateCode': instance.stateCode,
      'lat': instance.lat,
      'lng': instance.lng,
      'serviceable': instance.serviceable,
      'modules': instance.modules,
    };

_$CitiesResponseImpl _$$CitiesResponseImplFromJson(Map<String, dynamic> json) =>
    _$CitiesResponseImpl(
      version: (json['version'] as num).toInt(),
      items:
          (json['items'] as List<dynamic>?)
              ?.map((e) => City.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <City>[],
    );

Map<String, dynamic> _$$CitiesResponseImplToJson(
  _$CitiesResponseImpl instance,
) => <String, dynamic>{'version': instance.version, 'items': instance.items};
