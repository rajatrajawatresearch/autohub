import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures/sell_vehicle.dart';

/// Machine-code parity with the server.
///
/// `POST /platform/forms/{key}/validate` answers a failure with codes and nothing else:
/// `metadata.fieldErrors: { "kms_driven": ["kms.too_high"] }`. Every assertion below is that the
/// handset raises **that** code, from **that** rule, with **that** authored sentence.
///
/// Why this is worth testing rather than reading: a client that invents its own codes drifts from
/// the server on the first schema change, and the drift is only ever discovered by a user — a
/// seller blocked by a rule the server would have accepted, or a seller who fills in six screens
/// and is rejected at the end for something the phone should have caught.
void main() {
  final FormDefinition sellVehicle = FormDefinition.fromJson(sellVehicleJson);

  FieldFault? faultFor(String key, Object? value) =>
      validateField(sellVehicle.fieldFor(key)!, value);

  group('registry-authored rules keep their own code and sentence', () {
    test('an odometer over the bound raises kms.too_high', () {
      final FieldFault? fault = faultFor('kms_driven', 600000);
      expect(fault?.code, 'kms.too_high');
      expect(fault?.message, 'Enter the odometer reading in kilometres.');
    });

    test('a plausible odometer raises nothing', () {
      expect(faultFor('kms_driven', 38400), isNull);
    });

    test('a malformed registration raises registration.malformed', () {
      expect(faultFor('registration_number', 'MH-12')?.code, 'registration.malformed');
      expect(faultFor('registration_number', 'MH12AB1234'), isNull);
      // The BH series (`22BH1234AA`) starts with digits, and the pattern this registry row carries
      // does not admit it. That is a true statement about the fixture and it is asserted rather
      // than worked around, because the fix is a registry publish — one row, live on web, Android
      // and iOS at once — and not a special case in this file. The moment a client starts
      // "helpfully" accepting what the schema rejects, the phone and the server disagree.
      expect(faultFor('registration_number', '22BH1234AA')?.code, 'registration.malformed');
    });

    test('an authored required rule wins over the synthesised one', () {
      // `city` declares its own `required` with a better sentence than the generic fallback. A
      // synthesised rule must never shadow something an operations user wrote on purpose.
      final FieldFault? fault = faultFor('city', null);
      expect(fault?.code, 'city.required');
      expect(fault?.message, 'Pick the city the car is in.');
    });

    test('a field with no authored required rule gets the synthesised code', () {
      final FieldFault? fault = faultFor('owners', null);
      expect(fault?.code, 'owners.required');
    });

    test('a ten-digit mobile passes and a short one raises phone.invalid', () {
      expect(faultFor('phone', '+919812345678'), isNull);
      expect(faultFor('phone', '98123')?.code, 'phone.invalid');
    });
  });

  group('config-implied bounds', () {
    test('a photo set under config.min raises a count fault, not a numeric one', () {
      // `min` means what the value's shape says it means. Six is a count here and forty is a
      // percentage on `battery_health_pct`, and nothing in this package hardcodes which is which.
      final FieldFault? fault = faultFor('photos', <Object?>[1, 2, 3]);
      expect(fault?.code, 'photos.min');
      expect(fault?.message, 'Add at least 6.');
    });

    test('a full photo set passes', () {
      expect(faultFor('photos', List<Object?>.filled(6, 1)), isNull);
    });

    test('a battery percentage below the registry bound raises battery_health_pct.min', () {
      expect(faultFor('battery_health_pct', 12)?.code, 'battery_health_pct.min');
      expect(faultFor('battery_health_pct', 88), isNull);
    });
  });

  group('emptiness', () {
    test('zero is an answer and false is not', () {
      // A zero-kilometre odometer and a zero price are real. An unticked consent has not been
      // answered, which is what makes `required` mean "must be ticked" on a checkbox.
      expect(isEmptyValue(0), isFalse);
      expect(isEmptyValue(false), isTrue);
      expect(isEmptyValue(''), isTrue);
      expect(isEmptyValue('  '), isTrue);
      expect(isEmptyValue(<Object?>[]), isTrue);
    });

    test('an untouched optional field raises nothing at all', () {
      // Only `required` speaks about emptiness. A `max` of 500000 has nothing to say about a blank
      // odometer, and firing it would tell a user their empty field is too large.
      expect(faultFor('expected_price_minor', null), isNull);
    });
  });

  group('rules this build has not learned', () {
    test('an unknown rule passes on the client and is left to the server', () {
      // A registry that publishes a rule ahead of a binary is a normal Tuesday. Failing closed here
      // would brick the sell flow on every handset that has not updated.
      // `autohub_core` decodes a rule name it has never seen into `ValidationRule.unsupported`
      // rather than failing the parse of the whole document, and this package then leaves it to the
      // server. The alternative — one unknown rule killing the definition — means the user cannot
      // sell their car at all.
      final FormField field = FormField.fromJson(<String, Object?>{
        'key': 'gstin',
        'label': 'GSTIN',
        'control': 'text',
        'validation': <Object?>[
          <String, Object?>{
            'rule': 'checksum_v2',
            'code': 'gstin.checksum',
            'message': 'That GSTIN does not check out.',
          },
        ],
      });
      expect(field.validation.single, isA<ValidationRuleUnsupported>());
      expect(validateField(field, 'anything at all'), isNull);
    });

    test('a custom rule is server-side only', () {
      const FormField field = FormField(
        key: 'registration_number',
        label: 'Registration',
        control: FormControl.plate,
        validation: <ValidationRule>[
          ValidationRule.custom(
            code: 'listing.duplicate_registration',
            message: 'This car is already listed.',
          ),
        ],
      );
      // The phone has neither the data nor the authority to answer this.
      expect(validateField(field, 'MH12AB1234'), isNull);
    });
  });

  group('a 422 comes back wearing the registry sentence', () {
    test('field errors resolve to the authored message for their code', () {
      final Map<String, FieldFault> faults = faultsFromProblem(
        sellVehicle,
        <String, Object?>{
          'type': 'https://autohub.in/errors/validation.failed',
          'status': 422,
          'code': 'validation.failed',
          'metadata': <String, Object?>{
            'fieldErrors': <String, Object?>{
              'kms_driven': <Object?>['kms.too_high'],
              'registration_number': 'registration.malformed',
            },
          },
        },
      );

      expect(faults['kms_driven']?.message, 'Enter the odometer reading in kilometres.');
      expect(faults['registration_number']?.code, 'registration.malformed');
      // A fault raised by the server must be indistinguishable from one raised on the phone.
      expect(faults['kms_driven'], validateField(sellVehicle.fieldFor('kms_driven')!, 600000));
    });

    test('a code the definition has never heard of still reaches the user', () {
      final Map<String, FieldFault> faults = faultsFromProblem(
        sellVehicle,
        <String, Object?>{
          'metadata': <String, Object?>{
            'fieldErrors': <String, Object?>{
              'phone': <Object?>['identity.phone.blocked'],
            },
          },
        },
      );
      // Carrying the code is the point: a fault the user can quote is worth far more than one
      // silently dropped because this build does not recognise it.
      expect(faults['phone']?.code, 'identity.phone.blocked');
      expect(faults['phone']?.message, isNotEmpty);
    });

    test('a body with no field errors resolves to nothing rather than throwing', () {
      expect(faultsFromProblem(sellVehicle, null), isEmpty);
      expect(faultsFromProblem(sellVehicle, <String, Object?>{'status': 409}), isEmpty);
    });

    test('the form walks back to the step holding the first fault', () async {
      final FormDefinition definition = sellVehicle;
      final SchemaFormController controller = SchemaFormController(
        definition: definition,
        onSubmit: (FormValues _) async => throw const FormSubmissionRejected(
          problem: <String, Object?>{
            'metadata': <String, Object?>{
              'fieldErrors': <String, Object?>{
                'registration_number': <Object?>['registration.malformed'],
              },
            },
          },
        ),
        initialValues: <String, Object?>{
          'registration_number': 'MH12AB1234',
          'city': 'pune',
          'fuel_type': 'petrol',
          'kms_driven': 38400,
          'owners': '1',
          'condition': 'good',
          'photos': <Object?>[1, 2, 3, 4, 5, 6],
          'phone': '+919812345678',
        },
      );
      addTearDown(controller.dispose);

      controller.goTo(0);
      expect(await controller.submit(), isFalse);
      // A 422 routinely names a field three screens back. Leaving the seller on the last step with
      // an error about something they cannot see is how a submission is abandoned.
      expect(controller.stepIndex, 0);
      expect(controller.faultFor('registration_number')?.code, 'registration.malformed');
    });
  });

  group('money survives the trip', () {
    test('rupees typed by a human become exact paise', () {
      // `8.4 * 100` is 840.0000000000001 in binary floating point. A listing price one paise out is
      // a settlement that does not reconcile, three weeks later, in a different system.
      expect(rupeesToMinor('8.4'), 840);
      expect(rupeesToMinor('847350'), 84735000);
      expect(rupeesToMinor('549000.99'), 54900099);
      expect(rupeesToMinor('0'), 0);
      expect(rupeesToMinor(''), isNull);
      expect(rupeesToMinor('.'), isNull);
    });

    test('paise round-trip back to the string a human edits', () {
      expect(minorToRupees(84735000), '847350');
      expect(minorToRupees(54900099), '549000.99');
      expect(minorToRupees(840), '8.40');
      expect(minorToRupees(null), '');
    });
  });
}
