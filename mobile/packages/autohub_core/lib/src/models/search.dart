// See the note in `taxonomy.dart` — the workspace root already settled this diagnostic.
// ignore_for_file: invalid_annotation_target

import 'package:autohub_core/src/models/common.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'search.freezed.dart';
part 'search.g.dart';

/// A numeric bound. Either end may be open.
@immutable
final class RangeFilter {
  const RangeFilter({this.min, this.max});

  final num? min;
  final num? max;

  bool get isEmpty => min == null && max == null;

  /// `0..40000`, `..40000`, `300000..` — the grammar from §9 of the contract.
  String get wire => '${min ?? ''}..${max ?? ''}';

  @override
  bool operator ==(Object other) =>
      other is RangeFilter && other.min == min && other.max == max;

  @override
  int get hashCode => Object.hash(min, max);

  @override
  String toString() => wire;
}

/// One attribute's filter.
///
/// Sealed rather than a union of loose types, because the three shapes serialise differently and a
/// filter that serialises wrong returns the wrong cars silently — the worst kind of search bug,
/// because it looks like an empty market rather than like a defect.
///
/// Hand-written rather than generated: this is a *request* shape that never arrives as JSON. It
/// leaves as a query string and nothing decodes it.
sealed class AttributeFilter {
  const AttributeFilter();

  /// Multi-value is OR *within* an attribute: `attr.fuel_type=petrol,cng`.
  const factory AttributeFilter.anyOf(List<String> values) = AttributeAnyOf;

  /// `attr.kms_driven=0..40000`.
  const factory AttributeFilter.range(RangeFilter range) = AttributeRangeFilter;

  /// A toggle facet: `attr.verified_rc=true`.
  const factory AttributeFilter.flag(bool value) = AttributeFlag;

  /// The value as it appears in the query string, or `null` when the filter selects nothing and
  /// should therefore be absent rather than sent empty.
  String? get wire;
}

final class AttributeAnyOf extends AttributeFilter {
  const AttributeAnyOf(this.values);

  final List<String> values;

  @override
  String? get wire => values.isEmpty ? null : values.join(',');
}

final class AttributeRangeFilter extends AttributeFilter {
  const AttributeRangeFilter(this.range);

  final RangeFilter range;

  @override
  String? get wire => range.isEmpty ? null : range.wire;
}

final class AttributeFlag extends AttributeFilter {
  const AttributeFlag(this.value);

  final bool value;

  @override
  String? get wire => '$value';
}

/// The search query.
///
/// The attribute filters are keyed by taxonomy attribute code and **nothing in this package knows
/// what those codes are** — they come from the taxonomy registry and from the facets the previous
/// response returned. That is why the query grammar cannot be a fixed field list, and why adding a
/// filterable attribute is a registry publish rather than a release.
@immutable
final class SearchParams {
  const SearchParams({
    this.q,
    this.city,
    this.intent,
    this.price,
    this.attributes = const <String, AttributeFilter>{},
    this.sort = SearchSort.relevance,
    this.cursor,
    this.limit,
  });

  final String? q;
  final String? city;
  final ListingIntent? intent;
  final RangeFilter? price;
  final Map<String, AttributeFilter> attributes;
  final SearchSort sort;
  final String? cursor;
  final int? limit;

  SearchParams copyWith({
    String? q,
    String? city,
    ListingIntent? intent,
    RangeFilter? price,
    Map<String, AttributeFilter>? attributes,
    SearchSort? sort,
    String? cursor,
    int? limit,
  }) =>
      SearchParams(
        q: q ?? this.q,
        city: city ?? this.city,
        intent: intent ?? this.intent,
        price: price ?? this.price,
        attributes: attributes ?? this.attributes,
        sort: sort ?? this.sort,
        cursor: cursor ?? this.cursor,
        limit: limit ?? this.limit,
      );

  /// The next page of the same query. Written out because dropping the cursor is the single
  /// easiest way to build an infinite list that re-fetches page one forever.
  SearchParams nextPage(String? nextCursor) => copyWith(cursor: nextCursor);

  Map<String, Object?> toQuery() {
    final query = <String, Object?>{
      'q': q,
      'city': city,
      'intent': intent?.wire,
      'price': (price == null || price!.isEmpty) ? null : price!.wire,
      'sort': sort == SearchSort.relevance ? null : sort.wire,
      'cursor': cursor,
      'limit': limit,
    };

    attributes.forEach((code, filter) {
      final value = filter.wire;
      if (value != null) query['attr.$code'] = value;
    });

    query.removeWhere((_, value) => value == null || value == '');
    return query;
  }
}

/// One search hit. Identical to a listing's card projection — one shape, one card widget.
@freezed
class SearchItem with _$SearchItem {
  const factory SearchItem({
    required String listingId,
    @JsonKey(unknownEnumValue: ListingIntent.sale)
    @Default(ListingIntent.sale)
    ListingIntent intent,
    required Registration registration,
    required String title,

    /// Nullable. An auction lot before its first bid, and a listing whose price is "on request",
    /// both legitimately have none.
    int? priceMinor,

    /// Rental and lease price by period; absent on a straight sale.
    String? pricePeriod,
    @Default('') String city,

    /// Rendered by `SpecStrip`. Order and content come from the registry, not from the widget.
    @Default(<SpecItem>[]) List<SpecItem> specStrip,
    @Default(Media()) Media media,

    /// `inspected`, `verified_rc` — codes. Labels resolve through the i18n registry.
    @Default(<String>[]) List<String> badges,
    String? dealerName,
  }) = _SearchItem;

  factory SearchItem.fromJson(Map<String, Object?> json) => _$SearchItemFromJson(json);
}

/// A facet, discriminated on `style`.
///
/// Sealed because the four styles are four different widgets with four different state shapes, and
/// the compiler refusing to build a facet bar that forgot one is the whole value of modelling it as
/// a union rather than as a bag of nullable fields.
///
/// A style this build has not shipped decodes to [SearchFacetUnsupported] instead of failing. The
/// reason is the same one that made `coverUrl` nullable: on the web a `discriminatedUnion` failure
/// takes down the *entire* search response, so one facet the client had not heard of would render
/// an empty marketplace. One missing filter chip is a far better outcome than no results.
@Freezed(unionKey: 'style', fallbackUnion: 'unsupported')
sealed class SearchFacet with _$SearchFacet {
  @FreezedUnionValue('chips')
  const factory SearchFacet.chips({
    required String code,
    required String label,
    @Default(<Option>[]) List<Option> options,
  }) = SearchFacetChips;

  @FreezedUnionValue('list')
  const factory SearchFacet.list({
    required String code,
    required String label,
    @Default(<Option>[]) List<Option> options,
  }) = SearchFacetList;

  @FreezedUnionValue('toggle')
  const factory SearchFacet.toggle({
    required String code,
    required String label,
    @Default(<Option>[]) List<Option> options,
  }) = SearchFacetToggle;

  @FreezedUnionValue('range')
  const factory SearchFacet.range({
    required String code,
    required String label,
    required double min,
    required double max,
    double? step,
    String? unit,
  }) = SearchFacetRange;

  const factory SearchFacet.unsupported({
    @Default('') String code,
    @Default('') String label,
  }) = SearchFacetUnsupported;

  factory SearchFacet.fromJson(Map<String, Object?> json) => _$SearchFacetFromJson(json);
}

@freezed
class SearchResponse with _$SearchResponse {
  const factory SearchResponse({
    @Default(<SearchItem>[]) List<SearchItem> items,
    @Default(<SearchFacet>[]) List<SearchFacet> facets,
    String? nextCursor,

    /// An estimate by contract — never a `COUNT(*)` on a hot table. The UI says "about 1,284" and
    /// never promises precision it does not have.
    int? totalEstimate,
  }) = _SearchResponse;

  factory SearchResponse.fromJson(Map<String, Object?> json) => _$SearchResponseFromJson(json);
}
