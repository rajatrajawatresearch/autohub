import 'package:freezed_annotation/freezed_annotation.dart';

part 'platform.freezed.dart';
part 'platform.g.dart';

/// Feature and module availability, already evaluated server-side for the calling user.
///
/// The client receives booleans and never targeting rules. Targeting logic on the client is
/// targeting logic an attacker can read — and on a phone it is targeting logic they can read at
/// leisure, from a binary they own, with no rate limit.
@freezed
class Features with _$Features {
  const factory Features({
    required int version,

    /// `auction`, `lease`, `attachment` — whole modules, on or off for this user in this city.
    @Default(<String, bool>{}) Map<String, bool> modules,

    /// `instant_offer`, `ai_damage_detection`, `whatsapp_chat` — behaviour inside a module.
    @Default(<String, bool>{}) Map<String, bool> flags,
  }) = _Features;

  factory Features.fromJson(Map<String, Object?> json) => _$FeaturesFromJson(json);

  const Features._();

  /// Unknown module or flag reads as **off**.
  ///
  /// Fail closed, for the same reason the condition evaluator does: an app that shows a Lease
  /// button in a city where leasing is not live has sent a user down a flow that ends in a support
  /// call, and the registry is the only thing that knows.
  bool module(String code) => modules[code] ?? false;

  bool flag(String code) => flags[code] ?? false;
}

/// A localisation bundle.
///
/// `locale` stays a `String` rather than becoming an enum. The list of Indian languages the product
/// serves is an operations decision — Odia is added by publishing a bundle, not by shipping a
/// binary — and an enum here would make a language launch an app-store submission.
@freezed
class I18nBundle with _$I18nBundle {
  const factory I18nBundle({
    required String locale,
    required int version,
    @Default(<String, String>{}) Map<String, String> resources,
  }) = _I18nBundle;

  factory I18nBundle.fromJson(Map<String, Object?> json) => _$I18nBundleFromJson(json);

  const I18nBundle._();

  /// Missing key → the key itself. **Never an empty string.**
  ///
  /// A blank button is unrecoverable for the user: they cannot guess what it does, cannot report it
  /// usefully, and cannot work around it. `listing.cta.get_valuation` on the button is ugly and
  /// diagnosable, which is strictly the better failure. The `en-IN` fallback in between is composed
  /// by the caller, which holds both bundles.
  String resolve(String key) {
    final value = resources[key];
    return (value == null || value.isEmpty) ? key : value;
  }
}

@freezed
class City with _$City {
  const factory City({
    required String code,
    required String name,
    required String stateCode,
    double? lat,
    double? lng,
    @Default(false) bool serviceable,

    /// Which modules are live here. Lease in Bengaluru only is a registry row, not a deploy.
    @Default(<String>[]) List<String> modules,
  }) = _City;

  factory City.fromJson(Map<String, Object?> json) => _$CityFromJson(json);
}

@freezed
class CitiesResponse with _$CitiesResponse {
  const factory CitiesResponse({
    required int version,
    @Default(<City>[]) List<City> items,
  }) = _CitiesResponse;

  factory CitiesResponse.fromJson(Map<String, Object?> json) => _$CitiesResponseFromJson(json);
}
