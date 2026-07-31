// See the note in `taxonomy.dart` — the workspace root already settled this diagnostic.
// ignore_for_file: invalid_annotation_target

import 'package:autohub_core/src/models/common.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'valuation.freezed.dart';
part 'valuation.g.dart';

/// How much the model trusts its own number. Three bands, closed by the contract, each rendered
/// differently — a number without its confidence is a rumour with a rupee sign on it.
enum ConfidenceBand {
  @JsonValue('high')
  high,
  @JsonValue('medium')
  medium,
  @JsonValue('low')
  low,
}

@freezed
class ValuationQuoteRequest with _$ValuationQuoteRequest {
  const factory ValuationQuoteRequest({
    required String registrationNumber,
    int? kmsDriven,
    String? city,

    /// `excellent` / `good` / `fair` / `poor` — but the options themselves come from the form
    /// registry, which is why this is a `String`. An enum here would mean a fifth condition band
    /// needs an app release.
    String? condition,

    /// Set when the plate is not in VAHAN and the user typed the vehicle in by hand.
    String? makeCode,
    String? modelCode,
    String? variantCode,
    int? year,
  }) = _ValuationQuoteRequest;

  factory ValuationQuoteRequest.fromJson(Map<String, Object?> json) =>
      _$ValuationQuoteRequestFromJson(json);
}

@freezed
class ValuationVehicle with _$ValuationVehicle {
  const factory ValuationVehicle({
    required String title,
    required Registration registration,
    int? year,
    String? fuel,
    String? transmission,

    /// `vahan` when the RTO record answered, `manual` when the user typed it. Shown, because a
    /// number derived from a guess must say so.
    String? source,
  }) = _ValuationVehicle;

  factory ValuationVehicle.fromJson(Map<String, Object?> json) => _$ValuationVehicleFromJson(json);
}

@freezed
class ValuationBand with _$ValuationBand {
  const factory ValuationBand({
    required String code,
    required String label,
    required int amountMinor,

    /// Exactly one band is primary — the number the odometer rolls to.
    @Default(false) bool isPrimary,
    String? helpText,
  }) = _ValuationBand;

  factory ValuationBand.fromJson(Map<String, Object?> json) => _$ValuationBandFromJson(json);
}

@freezed
class ValuationRange with _$ValuationRange {
  const factory ValuationRange({
    required int lowMinor,
    required int highMinor,
  }) = _ValuationRange;

  factory ValuationRange.fromJson(Map<String, Object?> json) => _$ValuationRangeFromJson(json);
}

@freezed
class ValuationConfidence with _$ValuationConfidence {
  const factory ValuationConfidence({
    required double score,
    @JsonKey(unknownEnumValue: ConfidenceBand.medium)
    @Default(ConfidenceBand.medium)
    ConfidenceBand band,

    /// "412 comparable sales in Pune over 90 days".
    String? basedOn,
  }) = _ValuationConfidence;

  factory ValuationConfidence.fromJson(Map<String, Object?> json) =>
      _$ValuationConfidenceFromJson(json);
}

/// The number the hero screen exists to show.
///
/// `amountMinor` everywhere — the server never sends a formatted string, and the client renders
/// `₹5.49L` at the edge. [expiresAt] is not decoration: a valuation is a quote, and a quote with no
/// expiry is a promise the platform did not make.
@freezed
class ValuationQuote with _$ValuationQuote {
  const factory ValuationQuote({
    required String quoteId,
    required ValuationVehicle vehicle,
    @Default('INR') String currency,
    @Default(<ValuationBand>[]) List<ValuationBand> valuations,
    required ValuationRange range,
    required ValuationConfidence confidence,

    /// The "as of" calendar date.
    required String asOf,
    required String expiresAt,
    String? modelVersion,
  }) = _ValuationQuote;

  factory ValuationQuote.fromJson(Map<String, Object?> json) => _$ValuationQuoteFromJson(json);

  const ValuationQuote._();

  /// The band the odometer rolls to, or the first one if the server marked none.
  ValuationBand? get primary {
    for (final band in valuations) {
      if (band.isPrimary) return band;
    }
    return valuations.isEmpty ? null : valuations.first;
  }
}
