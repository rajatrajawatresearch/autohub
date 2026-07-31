// See the note in `taxonomy.dart` — the workspace root already settled this diagnostic.
// ignore_for_file: invalid_annotation_target

import 'package:freezed_annotation/freezed_annotation.dart';

part 'pricing.freezed.dart';
part 'pricing.g.dart';

/// How a fee line was derived.
///
/// Closed by the contract and consequential for rendering: a `percent` line shows its rate, a
/// `percent_of_lines` line has to name what it rode on top of, a `slab` line needs its band
/// explained. A tenth basis needs a renderer, so the compile error is the point.
enum PricingBasis {
  @JsonValue('percent')
  percent,
  @JsonValue('flat')
  flat,
  @JsonValue('percent_of_lines')
  percentOfLines,
  @JsonValue('slab')
  slab,
  @JsonValue('per_unit')
  perUnit,
}

@freezed
class PricingVehicleContext with _$PricingVehicleContext {
  const factory PricingVehicleContext({
    String? category,
    String? makeCode,
    String? modelCode,
    int? year,
  }) = _PricingVehicleContext;

  factory PricingVehicleContext.fromJson(Map<String, Object?> json) =>
      _$PricingVehicleContextFromJson(json);
}

@freezed
class PricingLocationContext with _$PricingLocationContext {
  const factory PricingLocationContext({String? state, String? city}) = _PricingLocationContext;

  factory PricingLocationContext.fromJson(Map<String, Object?> json) =>
      _$PricingLocationContextFromJson(json);
}

@freezed
class PricingPartyContext with _$PricingPartyContext {
  const factory PricingPartyContext({String? type, String? tier, String? id}) =
      _PricingPartyContext;

  factory PricingPartyContext.fromJson(Map<String, Object?> json) =>
      _$PricingPartyContextFromJson(json);
}

@freezed
class PricingAmountContext with _$PricingAmountContext {
  const factory PricingAmountContext({
    required int minorUnits,
    @Default('INR') String currency,
  }) = _PricingAmountContext;

  factory PricingAmountContext.fromJson(Map<String, Object?> json) =>
      _$PricingAmountContextFromJson(json);
}

@freezed
class PricingContext with _$PricingContext {
  const factory PricingContext({
    PricingVehicleContext? vehicle,
    PricingLocationContext? location,
    PricingPartyContext? party,
    PricingAmountContext? amount,
  }) = _PricingContext;

  factory PricingContext.fromJson(Map<String, Object?> json) => _$PricingContextFromJson(json);
}

@freezed
class PricingQuoteRequest with _$PricingQuoteRequest {
  const factory PricingQuoteRequest({
    /// The rule set to evaluate: `auction.win`, `listing.publish`, `rental.booking`.
    required String scope,
    required PricingContext context,
  }) = _PricingQuoteRequest;

  factory PricingQuoteRequest.fromJson(Map<String, Object?> json) =>
      _$PricingQuoteRequestFromJson(json);
}

/// One line of a quote, with the rule that produced it named.
///
/// Nothing in this app computes a fee — it asks, and renders what comes back. The `ruleName` is not
/// decoration: a dealer will ask why they were charged, and "the code says so" is not an answer
/// support can give.
@freezed
class PricingLine with _$PricingLine {
  const factory PricingLine({
    required String code,
    required String label,
    @JsonKey(unknownEnumValue: PricingBasis.flat)
    @Default(PricingBasis.flat)
    PricingBasis basis,

    /// A fraction, not a percentage: `0.02` is 2%. Convert with `Rate.fromWire` before applying it
    /// to money — the double is for display only.
    double? rate,
    required int amountMinor,

    /// Line codes this line was computed over. Present when [basis] is `percent_of_lines`.
    List<String>? appliesTo,
    String? ruleId,
    String? ruleName,
    String? hsnSac,
  }) = _PricingLine;

  factory PricingLine.fromJson(Map<String, Object?> json) => _$PricingLineFromJson(json);
}

@freezed
class PricingQuote with _$PricingQuote {
  const factory PricingQuote({
    @Default('INR') String currency,
    required int baseAmount,
    @Default(<PricingLine>[]) List<PricingLine> lines,
    required int totalFeesMinor,
    required int payableMinor,

    /// One sentence a dealer can read: "Gold dealers in Maharashtra pay 2% on auction wins."
    String? explanation,
  }) = _PricingQuote;

  factory PricingQuote.fromJson(Map<String, Object?> json) => _$PricingQuoteFromJson(json);
}
