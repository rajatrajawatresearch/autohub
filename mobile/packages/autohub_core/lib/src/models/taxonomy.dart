// `@JsonKey` on a freezed factory parameter is how the wire name and the decode fallback are
// declared; the analyzer disagrees with freezed about whether a constructor parameter is a valid
// target for it. `mobile/analysis_options.yaml` already settles this for the workspace
// (`invalid_annotation_target: ignore`); the package's own options file does not inherit it yet, so
// the same decision is restated here rather than left as noise that hides a real diagnostic.
// ignore_for_file: invalid_annotation_target

import 'package:autohub_core/src/models/common.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'taxonomy.freezed.dart';
part 'taxonomy.g.dart';

/// How an attribute's value is stored and edited.
///
/// A legal enum, and the distinction matters: this is not a list of fuel types, it is the registry's
/// *type system*. The client implements one editor and one renderer per data type, so a ninth data
/// type genuinely needs client work — and a compile error at every switch is how that gets noticed
/// rather than shipped as a blank field.
///
/// The values themselves — petrol, diesel, cng, ev — never appear in Dart. They arrive in
/// [TaxonomyAttribute.options].
enum AttributeDataType {
  @JsonValue('string')
  string,
  @JsonValue('number')
  number,
  @JsonValue('integer')
  integer,
  @JsonValue('boolean')
  boolean,

  /// `enum` on the wire. Renamed here only because `enum` is a Dart keyword.
  @JsonValue('enum')
  singleChoice,
  @JsonValue('multi_enum')
  multiChoice,
  @JsonValue('date')
  date,

  /// Integer minor units, like every other amount in the product.
  @JsonValue('money')
  money,
}

/// How an attribute is offered as a filter. Maps to a facet widget, never to a colour.
enum FacetStyle {
  @JsonValue('chips')
  chips,
  @JsonValue('list')
  list,
  @JsonValue('range')
  range,
  @JsonValue('toggle')
  toggle,
}

@freezed
class AttributeRange with _$AttributeRange {
  const factory AttributeRange({
    required double min,
    required double max,
    double? step,
  }) = _AttributeRange;

  factory AttributeRange.fromJson(Map<String, Object?> json) => _$AttributeRangeFromJson(json);
}

/// One attribute a category exposes.
///
/// **This is the shape that makes a new vehicle type possible without a deploy.** A row here
/// becomes a form field, a search facet and a spec row at once. Nothing about `fuel_type` is
/// written in Dart — not its label, not its options, not its unit, not whether it is required.
@freezed
class TaxonomyAttribute with _$TaxonomyAttribute {
  const factory TaxonomyAttribute({
    required String code,
    required String label,
    @JsonKey(unknownEnumValue: AttributeDataType.string)
    @Default(AttributeDataType.string)
    AttributeDataType dataType,

    /// `km`, `%`, `kg`, `bhp` — rendered beside the value, never folded into the label, because
    /// the same attribute on a commercial category may be declared in a different unit tomorrow.
    String? unit,
    @JsonKey(name: 'required') @Default(false) bool isRequired,

    /// True when the attribute is a search facet.
    @Default(false) bool searchable,
    @JsonKey(unknownEnumValue: FacetStyle.list) FacetStyle? facetStyle,

    /// Groups spec rows on the detail page: `engine`, `usage`, `documents`.
    String? displayGroup,
    @Default(0) int sortOrder,
    List<Option>? options,
    AttributeRange? range,
    String? helpText,
  }) = _TaxonomyAttribute;

  factory TaxonomyAttribute.fromJson(Map<String, Object?> json) =>
      _$TaxonomyAttributeFromJson(json);
}

@freezed
class AttributesResponse with _$AttributesResponse {
  const factory AttributesResponse({
    required int version,
    @Default(<TaxonomyAttribute>[]) List<TaxonomyAttribute> attributes,
  }) = _AttributesResponse;

  factory AttributesResponse.fromJson(Map<String, Object?> json) =>
      _$AttributesResponseFromJson(json);
}

/// A node in the category tree. Self-referential, exactly as the tree is.
@freezed
class TaxonomyCategory with _$TaxonomyCategory {
  const factory TaxonomyCategory({
    required String code,
    required String name,
    String? icon,
    @Default(0) int sortOrder,

    /// Attribute *codes* this category exposes. Resolve them through
    /// `GET /platform/taxonomy/attributes`; the tree deliberately does not inline the definitions,
    /// so the picker can render before the attribute documents have been fetched.
    @Default(<String>[]) List<String> attributes,
    @Default(<TaxonomyCategory>[]) List<TaxonomyCategory> children,
  }) = _TaxonomyCategory;

  factory TaxonomyCategory.fromJson(Map<String, Object?> json) =>
      _$TaxonomyCategoryFromJson(json);
}

@freezed
class TaxonomyTree with _$TaxonomyTree {
  const factory TaxonomyTree({
    required int version,
    @Default(<TaxonomyCategory>[]) List<TaxonomyCategory> categories,
  }) = _TaxonomyTree;

  factory TaxonomyTree.fromJson(Map<String, Object?> json) => _$TaxonomyTreeFromJson(json);
}

/// Makes, models and variants share one node shape — the query string says which you asked for.
@freezed
class TaxonomyNode with _$TaxonomyNode {
  const factory TaxonomyNode({
    required String code,
    required String name,
    String? makeCode,
    String? modelCode,
    String? categoryCode,

    /// `[firstYear, lastYear]`, inclusive.
    List<int>? years,
    String? logoUrl,
    int? popularity,
  }) = _TaxonomyNode;

  factory TaxonomyNode.fromJson(Map<String, Object?> json) => _$TaxonomyNodeFromJson(json);
}

@freezed
class TaxonomyNodesResponse with _$TaxonomyNodesResponse {
  const factory TaxonomyNodesResponse({
    required int version,
    @Default(<TaxonomyNode>[]) List<TaxonomyNode> items,
  }) = _TaxonomyNodesResponse;

  factory TaxonomyNodesResponse.fromJson(Map<String, Object?> json) =>
      _$TaxonomyNodesResponseFromJson(json);
}

/// One resolved make/model/variant/year selection, for the vehicle picker control.
@freezed
class VehicleSelection with _$VehicleSelection {
  const factory VehicleSelection({
    required String categoryCode,
    required String makeCode,
    required String modelCode,
    String? variantCode,
    int? year,
    String? vehicleId,
  }) = _VehicleSelection;

  factory VehicleSelection.fromJson(Map<String, Object?> json) =>
      _$VehicleSelectionFromJson(json);
}
