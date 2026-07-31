// See the note in `taxonomy.dart` — the workspace root already settled this diagnostic.
// ignore_for_file: invalid_annotation_target

import 'package:autohub_core/src/models/common.dart';
import 'package:autohub_core/src/models/search.dart';
import 'package:autohub_core/src/models/workflow.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'listing.freezed.dart';
part 'listing.g.dart';

/// The card projection of a listing.
///
/// Deliberately the *same type* as a search hit rather than a parallel class with the same fields.
/// One shape means one card widget, and a card that renders a search result and a saved listing
/// identically is the point — the user is looking at the same car.
typedef ListingSummary = SearchItem;

/// Spec rows grouped by the taxonomy attribute's `displayGroup`. Order comes from the registry.
@freezed
class SpecGroup with _$SpecGroup {
  const factory SpecGroup({
    required String code,
    required String label,
    @Default(<SpecItem>[]) List<SpecItem> items,
  }) = _SpecGroup;

  factory SpecGroup.fromJson(Map<String, Object?> json) => _$SpecGroupFromJson(json);
}

@freezed
class Seller with _$Seller {
  const factory Seller({
    required String id,
    required String displayName,

    /// `individual` or `dealer`. Drives which trust signals are shown, never a colour.
    @Default('individual') String type,
    String? city,
    @Default(false) bool isVerified,
    String? memberSince,
    int? listingsCount,
    int? responseTimeMinutes,
  }) = _Seller;

  factory Seller.fromJson(Map<String, Object?> json) => _$SellerFromJson(json);
}

/// The detail projection.
///
/// PROVISIONAL, exactly as `packages/api/src/schemas/listings.ts` says: `contracts/registry-api.md`
/// pins the *search* projection (§9) but not yet the detail one. Everything below is composed from
/// shapes the contract does define — registration, spec strip, media, dynamic attributes, workflow
/// snapshot — so reconciling with the published endpoint is additive rather than a rewrite. Nothing
/// here invents a business constant: spec groups, attribute codes and badge codes all arrive as
/// data.
///
/// The summary's fields are restated rather than inherited because freezed has no inheritance for
/// data classes. Keep them in step with [SearchItem]; they are one projection widened, not two
/// shapes.
@freezed
class ListingDetail with _$ListingDetail {
  const factory ListingDetail({
    required String listingId,
    @JsonKey(unknownEnumValue: ListingIntent.sale)
    @Default(ListingIntent.sale)
    ListingIntent intent,
    required Registration registration,
    required String title,
    int? priceMinor,
    String? pricePeriod,
    @Default('') String city,
    @Default(<SpecItem>[]) List<SpecItem> specStrip,
    @Default(Media()) Media media,
    @Default(<String>[]) List<String> badges,
    String? dealerName,
    String? description,
    @Default('INR') String currency,
    @Default(<MediaAsset>[]) List<MediaAsset> gallery,

    /// Dynamic per-category values, keyed by taxonomy attribute code.
    @Default(<String, Object?>{}) AttributeValues attributes,
    @Default(<SpecGroup>[]) List<SpecGroup> specGroups,
    Seller? seller,

    /// What this user may do to this listing right now. Read `availableActions` from here and
    /// render buttons from it; never re-derive authorisation from `state` and a permission list.
    WorkflowSnapshot? workflow,
    String? categoryCode,
    String? makeCode,
    String? modelCode,
    String? variantCode,
    int? year,

    /// Where the vehicle physically is, for a test drive.
    String? locality,
    String? createdAt,
    String? expiresAt,
    int? viewCount,
  }) = _ListingDetail;

  factory ListingDetail.fromJson(Map<String, Object?> json) => _$ListingDetailFromJson(json);
}

@freezed
class ListingDraftRequest with _$ListingDraftRequest {
  const factory ListingDraftRequest({
    required ListingIntent intent,
    required String categoryCode,
    String? registrationNumber,

    /// The sell form's own payload, validated against the form registry — deliberately untyped.
    /// Typing it would put the registry's field list in the binary, which is the one thing this
    /// architecture exists to avoid.
    @Default(<String, Object?>{}) Map<String, Object?> values,
    int? askingPriceMinor,
  }) = _ListingDraftRequest;

  factory ListingDraftRequest.fromJson(Map<String, Object?> json) =>
      _$ListingDraftRequestFromJson(json);
}

/// Query for a seller's or dealer's own listings.
@immutable
final class ListingListParams {
  const ListingListParams({this.intent, this.state, this.q, this.cursor, this.limit});

  final ListingIntent? intent;

  /// A workflow state code. Which codes exist is answered by the workflow registry, never by a
  /// constant in this file.
  final String? state;
  final String? q;
  final String? cursor;
  final int? limit;

  Map<String, Object?> toQuery() {
    final query = <String, Object?>{
      'intent': intent?.wire,
      'state': state,
      'q': q,
      'cursor': cursor,
      'limit': limit,
    }..removeWhere((_, value) => value == null || value == '');
    return query;
  }
}
