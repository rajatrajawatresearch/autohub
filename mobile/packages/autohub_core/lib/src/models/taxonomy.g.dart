// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'taxonomy.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$AttributeRangeImpl _$$AttributeRangeImplFromJson(Map<String, dynamic> json) =>
    _$AttributeRangeImpl(
      min: (json['min'] as num).toDouble(),
      max: (json['max'] as num).toDouble(),
      step: (json['step'] as num?)?.toDouble(),
    );

Map<String, dynamic> _$$AttributeRangeImplToJson(
  _$AttributeRangeImpl instance,
) => <String, dynamic>{
  'min': instance.min,
  'max': instance.max,
  'step': instance.step,
};

_$TaxonomyAttributeImpl _$$TaxonomyAttributeImplFromJson(
  Map<String, dynamic> json,
) => _$TaxonomyAttributeImpl(
  code: json['code'] as String,
  label: json['label'] as String,
  dataType:
      $enumDecodeNullable(
        _$AttributeDataTypeEnumMap,
        json['dataType'],
        unknownValue: AttributeDataType.string,
      ) ??
      AttributeDataType.string,
  unit: json['unit'] as String?,
  isRequired: json['required'] as bool? ?? false,
  searchable: json['searchable'] as bool? ?? false,
  facetStyle: $enumDecodeNullable(
    _$FacetStyleEnumMap,
    json['facetStyle'],
    unknownValue: FacetStyle.list,
  ),
  displayGroup: json['displayGroup'] as String?,
  sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
  options: (json['options'] as List<dynamic>?)
      ?.map((e) => Option.fromJson(e as Map<String, dynamic>))
      .toList(),
  range: json['range'] == null
      ? null
      : AttributeRange.fromJson(json['range'] as Map<String, dynamic>),
  helpText: json['helpText'] as String?,
);

Map<String, dynamic> _$$TaxonomyAttributeImplToJson(
  _$TaxonomyAttributeImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'label': instance.label,
  'dataType': _$AttributeDataTypeEnumMap[instance.dataType]!,
  'unit': instance.unit,
  'required': instance.isRequired,
  'searchable': instance.searchable,
  'facetStyle': _$FacetStyleEnumMap[instance.facetStyle],
  'displayGroup': instance.displayGroup,
  'sortOrder': instance.sortOrder,
  'options': instance.options,
  'range': instance.range,
  'helpText': instance.helpText,
};

const _$AttributeDataTypeEnumMap = {
  AttributeDataType.string: 'string',
  AttributeDataType.number: 'number',
  AttributeDataType.integer: 'integer',
  AttributeDataType.boolean: 'boolean',
  AttributeDataType.singleChoice: 'enum',
  AttributeDataType.multiChoice: 'multi_enum',
  AttributeDataType.date: 'date',
  AttributeDataType.money: 'money',
};

const _$FacetStyleEnumMap = {
  FacetStyle.chips: 'chips',
  FacetStyle.list: 'list',
  FacetStyle.range: 'range',
  FacetStyle.toggle: 'toggle',
};

_$AttributesResponseImpl _$$AttributesResponseImplFromJson(
  Map<String, dynamic> json,
) => _$AttributesResponseImpl(
  version: (json['version'] as num).toInt(),
  attributes:
      (json['attributes'] as List<dynamic>?)
          ?.map((e) => TaxonomyAttribute.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <TaxonomyAttribute>[],
);

Map<String, dynamic> _$$AttributesResponseImplToJson(
  _$AttributesResponseImpl instance,
) => <String, dynamic>{
  'version': instance.version,
  'attributes': instance.attributes,
};

_$TaxonomyCategoryImpl _$$TaxonomyCategoryImplFromJson(
  Map<String, dynamic> json,
) => _$TaxonomyCategoryImpl(
  code: json['code'] as String,
  name: json['name'] as String,
  icon: json['icon'] as String?,
  sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
  attributes:
      (json['attributes'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList() ??
      const <String>[],
  children:
      (json['children'] as List<dynamic>?)
          ?.map((e) => TaxonomyCategory.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <TaxonomyCategory>[],
);

Map<String, dynamic> _$$TaxonomyCategoryImplToJson(
  _$TaxonomyCategoryImpl instance,
) => <String, dynamic>{
  'code': instance.code,
  'name': instance.name,
  'icon': instance.icon,
  'sortOrder': instance.sortOrder,
  'attributes': instance.attributes,
  'children': instance.children,
};

_$TaxonomyTreeImpl _$$TaxonomyTreeImplFromJson(Map<String, dynamic> json) =>
    _$TaxonomyTreeImpl(
      version: (json['version'] as num).toInt(),
      categories:
          (json['categories'] as List<dynamic>?)
              ?.map((e) => TaxonomyCategory.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <TaxonomyCategory>[],
    );

Map<String, dynamic> _$$TaxonomyTreeImplToJson(_$TaxonomyTreeImpl instance) =>
    <String, dynamic>{
      'version': instance.version,
      'categories': instance.categories,
    };

_$TaxonomyNodeImpl _$$TaxonomyNodeImplFromJson(Map<String, dynamic> json) =>
    _$TaxonomyNodeImpl(
      code: json['code'] as String,
      name: json['name'] as String,
      makeCode: json['makeCode'] as String?,
      modelCode: json['modelCode'] as String?,
      categoryCode: json['categoryCode'] as String?,
      years: (json['years'] as List<dynamic>?)
          ?.map((e) => (e as num).toInt())
          .toList(),
      logoUrl: json['logoUrl'] as String?,
      popularity: (json['popularity'] as num?)?.toInt(),
    );

Map<String, dynamic> _$$TaxonomyNodeImplToJson(_$TaxonomyNodeImpl instance) =>
    <String, dynamic>{
      'code': instance.code,
      'name': instance.name,
      'makeCode': instance.makeCode,
      'modelCode': instance.modelCode,
      'categoryCode': instance.categoryCode,
      'years': instance.years,
      'logoUrl': instance.logoUrl,
      'popularity': instance.popularity,
    };

_$TaxonomyNodesResponseImpl _$$TaxonomyNodesResponseImplFromJson(
  Map<String, dynamic> json,
) => _$TaxonomyNodesResponseImpl(
  version: (json['version'] as num).toInt(),
  items:
      (json['items'] as List<dynamic>?)
          ?.map((e) => TaxonomyNode.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <TaxonomyNode>[],
);

Map<String, dynamic> _$$TaxonomyNodesResponseImplToJson(
  _$TaxonomyNodesResponseImpl instance,
) => <String, dynamic>{'version': instance.version, 'items': instance.items};

_$VehicleSelectionImpl _$$VehicleSelectionImplFromJson(
  Map<String, dynamic> json,
) => _$VehicleSelectionImpl(
  categoryCode: json['categoryCode'] as String,
  makeCode: json['makeCode'] as String,
  modelCode: json['modelCode'] as String,
  variantCode: json['variantCode'] as String?,
  year: (json['year'] as num?)?.toInt(),
  vehicleId: json['vehicleId'] as String?,
);

Map<String, dynamic> _$$VehicleSelectionImplToJson(
  _$VehicleSelectionImpl instance,
) => <String, dynamic>{
  'categoryCode': instance.categoryCode,
  'makeCode': instance.makeCode,
  'modelCode': instance.modelCode,
  'variantCode': instance.variantCode,
  'year': instance.year,
  'vehicleId': instance.vehicleId,
};
