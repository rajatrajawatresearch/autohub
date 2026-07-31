import 'package:autohub_core/src/values/parse_result.dart';

/// Which series a plate belongs to. Closed by the RTO, versioned with the contract, and a compile
/// error when a third series is introduced is exactly what should happen — a new series changes how
/// the number is grouped on screen, and every call site has to decide.
enum RegistrationFormat {
  /// State + RTO district series, `MH12AB1234`. The overwhelming majority.
  stateSeries,

  /// Bharat series, `24BH1234AA`. No re-registration on interstate transfer.
  bharatSeries,
}

/// An Indian vehicle registration number, parsed into its parts.
///
/// The single most-typed, most-misread, most-searched string in the product. Users type
/// `mh-12 ab 1234`, RC OCR returns `MH12AB1234`, a dealer's bulk upload has `MH 12AB1234`. All
/// three are the same vehicle, so canonicalisation happens here, once, and search, duplicate
/// detection and the plate on screen all work from [canonical].
///
/// Ported from `backend/src/AutoHub.SharedKernel/Vehicles/RegistrationNumber.cs` with the same
/// patterns and the same failure codes. The parity is the point: a plate the app accepts and the
/// server rejects is a sell flow that dead-ends after the user has already photographed their car.
///
/// **Structure is validated here; meaning is not.** Whether `MH` is a real state and whether RTO 12
/// exists is a question for the geography registry — data an operations team edits when an RTO
/// opens. A list of state codes compiled into an app binary would make opening an RTO an app-store
/// submission, which is precisely what this architecture refuses.
///
/// A registration number is **not** a permanent identifier. Vehicles are re-registered on
/// interstate transfer and released numbers are reissued, so uniqueness lives on the server's
/// vehicle identity with a validity period, never on this value.
final class RegistrationNumber {
  const RegistrationNumber._({
    required this.canonical,
    required this.format,
    required this.stateOrYearCode,
    required this.rtoOrSeriesCode,
    required this.series,
    required this.number,
  });

  /// Uppercase, separators removed. The form stored, indexed and compared. `MH12AB1234`.
  final String canonical;

  final RegistrationFormat format;

  /// State code on a state series (`MH`); the two-digit year on a BH series (`24`).
  final String stateOrYearCode;

  /// RTO district number on a state series (`12`); the literal `BH` on a BH series.
  final String rtoOrSeriesCode;

  /// Letter series (`AB`, or Delhi's three-letter `CAF`). Empty on the oldest registrations.
  final String series;

  /// The numeric part as written — leading zeros are significant on a plate.
  final String number;

  static final RegExp _separators = RegExp('[^A-Za-z0-9]');

  // MH 12 AB 1234 — state, RTO district, optional series. Delhi carries a vehicle-class letter and
  // therefore three: DL 8C AF.
  static final RegExp _stateSeries = RegExp(r'^([A-Z]{2})(\d{1,2})([A-Z]{0,3})(\d{1,4})$');

  // 24 BH 1234 AA — the Bharat series, for vehicles that move between states without re-registering.
  static final RegExp _bharatSeries = RegExp(r'^(\d{2})BH(\d{4})([A-Z]{1,2})$');

  static ParseResult<RegistrationNumber> tryParse(String? input) {
    if (input == null || input.trim().isEmpty) {
      return const ParseInvalid<RegistrationNumber>(
        'registration.required',
        'Enter the registration number shown on the number plate.',
      );
    }

    final canonical = input.replaceAll(_separators, '').toUpperCase();

    if (canonical.length < 5 || canonical.length > 11) {
      return const ParseInvalid<RegistrationNumber>(
        'registration.malformed',
        'That does not look like an Indian registration number. Example: MH12AB1234.',
      );
    }

    // BH is matched first. `24BH1234AA` is close enough to a state series to be claimed by it,
    // which would file a BH-series vehicle under a state that does not exist.
    final bharat = _bharatSeries.firstMatch(canonical);
    if (bharat != null) {
      return ParseOk<RegistrationNumber>(
        RegistrationNumber._(
          canonical: canonical,
          format: RegistrationFormat.bharatSeries,
          stateOrYearCode: bharat.group(1)!,
          rtoOrSeriesCode: 'BH',
          series: bharat.group(3)!,
          number: bharat.group(2)!,
        ),
      );
    }

    final state = _stateSeries.firstMatch(canonical);
    if (state != null) {
      return ParseOk<RegistrationNumber>(
        RegistrationNumber._(
          canonical: canonical,
          format: RegistrationFormat.stateSeries,
          stateOrYearCode: state.group(1)!,
          rtoOrSeriesCode: state.group(2)!,
          series: state.group(3)!,
          number: state.group(4)!,
        ),
      );
    }

    return const ParseInvalid<RegistrationNumber>(
      'registration.malformed',
      'That does not look like an Indian registration number. Example: MH12AB1234.',
    );
  }

  /// Spaced the way the number is grouped on a physical plate: `MH 12 AB 1234`.
  ///
  /// Grouping is not decoration. It is how a person proofreads a plate they are copying off a
  /// windscreen, and ungrouped it is measurably easier to transpose two digits — which on a phone,
  /// standing next to the car, is the common case rather than the edge case.
  ///
  /// This agrees character-for-character with `groupRegistration` in `autohub_ui`'s `PlateBlock`
  /// for every series both understand. `PlateBlock` splits on letter/digit runs so it can render a
  /// half-typed plate; this derives the same grouping from the parse. Two spellings of the same
  /// vehicle on two screens of the same app is the failure being avoided.
  String get plateDisplay => switch (format) {
        RegistrationFormat.bharatSeries => '$stateOrYearCode BH $number $series',
        RegistrationFormat.stateSeries => series.isEmpty
            ? '$stateOrYearCode $rtoOrSeriesCode $number'
            : '$stateOrYearCode $rtoOrSeriesCode $series $number',
      };

  @override
  bool operator ==(Object other) =>
      other is RegistrationNumber && other.canonical == canonical;

  @override
  int get hashCode => canonical.hashCode;

  @override
  String toString() => canonical;
}
