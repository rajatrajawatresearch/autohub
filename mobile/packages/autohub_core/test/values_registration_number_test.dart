import 'package:autohub_core/src/values/registration_number.dart';
import 'package:flutter_test/flutter_test.dart';

/// The full case list from
/// `backend/tests/AutoHub.Tests.Unit/SharedKernel/RegistrationNumberTests.cs`.
///
/// A plate the phone accepts and the server rejects strands a seller who has already photographed
/// their car; a plate the phone rejects and the server would have accepted loses the listing
/// outright. Both directions are ported.
void main() {
  group('canonicalisation', () {
    const cases = <String, String>{
      'MH12AB1234': 'MH12AB1234',
      'mh 12 ab 1234': 'MH12AB1234', // user typing
      'MH-12-AB-1234': 'MH12AB1234', // dealer bulk upload
      '  MH12 AB1234 ': 'MH12AB1234', // RC OCR output
    };

    cases.forEach((input, expected) {
      test('`$input` canonicalises to $expected', () {
        expect(RegistrationNumber.tryParse(input).valueOrNull?.canonical, expected);
      });
    });
  });

  group('state series', () {
    const cases = <String, List<String>>{
      'MH12AB1234': <String>['MH', '12', 'AB', '1234'],
      'DL8CAF5031': <String>['DL', '8', 'CAF', '5031'], // Delhi carries a vehicle-class letter
      'KA01MA9999': <String>['KA', '01', 'MA', '9999'],
      'TN10K1234': <String>['TN', '10', 'K', '1234'],
    };

    cases.forEach((input, parts) {
      test('`$input` splits into its parts', () {
        final plate = RegistrationNumber.tryParse(input).valueOrNull;

        expect(plate, isNotNull);
        expect(plate!.format, RegistrationFormat.stateSeries);
        expect(plate.stateOrYearCode, parts[0]);
        expect(plate.rtoOrSeriesCode, parts[1]);
        expect(plate.series, parts[2]);
        expect(plate.number, parts[3]);
      });
    });
  });

  test('the Bharat series is recognised before the state pattern can claim it', () {
    // `24BH1234AA` is structurally close enough to a state series to be mis-parsed, which would
    // file a BH-series vehicle under a state that does not exist.
    final plate = RegistrationNumber.tryParse('24BH1234AA').valueOrNull;

    expect(plate, isNotNull);
    expect(plate!.format, RegistrationFormat.bharatSeries);
    expect(plate.stateOrYearCode, '24');
    expect(plate.rtoOrSeriesCode, 'BH');
    expect(plate.number, '1234');
    expect(plate.series, 'AA');
  });

  group('plate display', () {
    const cases = <String, String>{
      'MH12AB1234': 'MH 12 AB 1234',
      'DL8CAF5031': 'DL 8 CAF 5031',
      '24BH1234AA': '24 BH 1234 AA',
    };

    cases.forEach((input, expected) {
      test('`$input` groups as $expected', () {
        expect(RegistrationNumber.tryParse(input).valueOrNull?.plateDisplay, expected);
      });
    });
  });

  group('rejection', () {
    for (final input in <String?>[null, '', '   ']) {
      test('`$input` asks for the number rather than calling it malformed', () {
        expect(RegistrationNumber.tryParse(input).errorCode, 'registration.required');
      });
    }

    const impossible = <String>[
      'ABCD',
      '1234567890123',
      'MH12AB12345', // five digits — no Indian series goes that far
      'M12AB1234', // one-letter state
    ];

    for (final input in impossible) {
      test('`$input` is structurally impossible', () {
        expect(RegistrationNumber.tryParse(input).errorCode, 'registration.malformed');
      });
    }
  });

  test('does not pass judgement on whether the state code is real', () {
    // Structure is validated here; meaning lives in the geography registry. `ZZ` is structurally a
    // plate, and deciding it is not a real state is an operations question — one that must be
    // answerable without shipping a new binary to every handset.
    expect(RegistrationNumber.tryParse('ZZ99XX0001').isValid, isTrue);
  });

  test('two spellings of one plate are one value', () {
    expect(
      RegistrationNumber.tryParse('mh-12 ab 1234').valueOrNull,
      RegistrationNumber.tryParse('MH12AB1234').valueOrNull,
    );
  });
}
