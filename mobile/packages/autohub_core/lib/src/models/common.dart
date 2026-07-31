import 'package:freezed_annotation/freezed_annotation.dart';

part 'common.freezed.dart';
part 'common.g.dart';

/// Dynamic per-category values, keyed by taxonomy attribute code.
///
/// Left as an open map on purpose. The whole claim of this architecture is that
/// `battery_health_pct` can appear on a handset that shipped before EVs existed as a category, and
/// a generated class per category would be a compile-time list of exactly the thing the registry
/// owns.
typedef AttributeValues = Map<String, Object?>;

/// A selectable value. Taxonomy attributes, form fields and search facets all speak this.
@freezed
class Option with _$Option {
  const factory Option({
    required String value,
    required String label,
    @Default(0) int sortOrder,
    String? icon,

    /// Present on facet options only — the number of matching documents.
    int? count,
  }) = _Option;

  factory Option.fromJson(Map<String, Object?> json) => _$OptionFromJson(json);
}

/// Money on the wire: integer minor units and an ISO code, never a formatted string.
///
/// Convert with `Money.fromMinorUnits(amount.minorUnits, Currency.fromCode(amount.currency))` at
/// the point arithmetic is needed. The model stays a transport shape so an unknown currency
/// degrades to "cannot do arithmetic" rather than failing the parse of the response that carried
/// it.
@freezed
class MoneyAmount with _$MoneyAmount {
  const factory MoneyAmount({
    required int minorUnits,
    @Default('INR') String currency,
  }) = _MoneyAmount;

  factory MoneyAmount.fromJson(Map<String, Object?> json) => _$MoneyAmountFromJson(json);
}

/// A vehicle's registration, as the server resolved it.
@freezed
class Registration with _$Registration {
  const factory Registration({
    /// `MH12AB1234` — the storage and comparison form.
    required String canonical,

    /// `MH 12 AB 1234` — the form `PlateBlock` renders.
    required String display,

    /// `private`, `commercial`, `ev`, `government`, `defence`.
    ///
    /// Deliberately a `String` and not an enum. Plate classes are a road-transport fact the
    /// geography registry owns — a new class (the green-and-yellow commercial EV plate, say)
    /// arrives as a data row, and an app that could not decode a listing carrying one would drop
    /// it out of search results. `autohub_ui` maps the known values to a palette and falls back to
    /// the private white for anything else.
    String? plateClass,
    String? stateCode,
  }) = _Registration;

  factory Registration.fromJson(Map<String, Object?> json) => _$RegistrationFromJson(json);
}

/// A hairline-divided mono data row. Order and content come from the registry, never from code.
@freezed
class SpecItem with _$SpecItem {
  const factory SpecItem({
    required String label,
    required String value,
    String? code,
  }) = _SpecItem;

  factory SpecItem.fromJson(Map<String, Object?> json) => _$SpecItemFromJson(json);
}

/// A listing's cover image, if it has one.
///
/// **`coverUrl` and `aspectRatio` are nullable, and that is load-bearing.** A listing with no
/// photos is a real state, not an error: a dealer's bulk upload lands the rows before the images,
/// and a listing in `draft` may never have had any. An earlier version of the web schema required a
/// string here and the effect was out of all proportion to the cause — one photoless listing failed
/// the parse for the *entire* search page and the marketplace rendered empty. Tolerating the null
/// at the boundary is what stops a missing thumbnail from looking like an outage.
///
/// Do not repeat that here by making either field required.
@freezed
class Media with _$Media {
  const factory Media({
    String? coverUrl,
    double? aspectRatio,
    @Default(0) int count,
  }) = _Media;

  factory Media.fromJson(Map<String, Object?> json) => _$MediaFromJson(json);

  const Media._();

  /// The ratio to reserve before the image arrives.
  ///
  /// Normalised rather than left null because every media slot must have a known ratio *before* the
  /// bytes land, or the card reflows as each thumbnail loads and the list jumps under the thumb.
  double get displayAspectRatio => aspectRatio ?? 1.5;

  bool get hasCover => (coverUrl ?? '').isNotEmpty;
}

@freezed
class MediaAsset with _$MediaAsset {
  const factory MediaAsset({
    required String id,
    required String url,
    @Default(1.5) double aspectRatio,
    @Default('photo') String kind,
    String? caption,
  }) = _MediaAsset;

  factory MediaAsset.fromJson(Map<String, Object?> json) => _$MediaAssetFromJson(json);
}

/// Listing intents.
///
/// One of the few closed sets this workspace is allowed to compile in, and the justification is
/// specific: an intent is a *route shape*, not a business constant. `/buy`, `/rent` and `/auction`
/// are screens, and a seventh intent needs a screen — so a compile error at every switch is the
/// desired outcome rather than a silent gap in navigation.
///
/// Whether an intent is *available* to this user in this city is answered by the feature registry
/// at runtime, never by this enum.
enum ListingIntent {
  sale,
  auction,
  rental,
  lease,
  attachment,
  exchange;

  String get wire => name;

  /// Unknown falls back to [sale] rather than failing the parse. A listing carrying an intent this
  /// build has no screen for still belongs in search results; the detail page routes it to the
  /// closest thing it can render.
  static ListingIntent fromWire(String? wire) => switch (wire) {
        'sale' => ListingIntent.sale,
        'auction' => ListingIntent.auction,
        'rental' => ListingIntent.rental,
        'lease' => ListingIntent.lease,
        'attachment' => ListingIntent.attachment,
        'exchange' => ListingIntent.exchange,
        _ => ListingIntent.sale,
      };
}

/// Sort orders the search endpoint accepts. Closed query grammar, not a registry.
enum SearchSort {
  relevance,
  priceAsc,
  priceDesc,
  newest,
  kmsAsc;

  String get wire => switch (this) {
        SearchSort.relevance => 'relevance',
        SearchSort.priceAsc => 'price_asc',
        SearchSort.priceDesc => 'price_desc',
        SearchSort.newest => 'newest',
        SearchSort.kmsAsc => 'kms_asc',
      };

  static SearchSort fromWire(String? wire) => switch (wire) {
        'price_asc' => SearchSort.priceAsc,
        'price_desc' => SearchSort.priceDesc,
        'newest' => SearchSort.newest,
        'kms_asc' => SearchSort.kmsAsc,
        _ => SearchSort.relevance,
      };
}
