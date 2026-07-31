/// The **real** `sell_vehicle` document, as `GET /platform/forms/sell_vehicle` returns it.
///
/// Transcribed from `packages/api/src/mock/forms.ts`, which is what the three web portals render
/// and what the server validates against. It is here as raw JSON rather than as constructed Dart
/// objects on purpose: a test that built `FieldDefinition`s by hand would prove that the renderer
/// can draw a form somebody wrote in Dart, which is not the claim. The claim is that a document
/// authored in an admin console, serialised as JSON and parsed by this package produces the same
/// fields, the same rules and the same conditional visibility as it does on the web.
///
/// If this file and `mock/forms.ts` ever drift, the tests that use it stop meaning anything — so
/// keep it verbatim, including the parts no assertion touches.
library;

const Map<String, Object?> sellVehicleJson = <String, Object?>{
  'key': 'sell_vehicle',
  'version': 7,
  'title': 'Tell us about your car',
  'description': 'We pull most of this from the RTO record. Correct anything that looks wrong.',
  'submitLabel': 'Get my price',
  'steps': <Object?>[
    <String, Object?>{
      'key': 'identity',
      'title': 'Your car',
      'description': 'We pull most of this from the RTO record.',
      'fields': <Object?>[
        <String, Object?>{
          'key': 'registration_number',
          'label': 'Registration number',
          'control': 'plate',
          'required': true,
          'placeholder': 'MH 12 AB 1234',
          'helpText': 'Exactly as printed on the plate.',
          'validation': <Object?>[
            <String, Object?>{
              'rule': 'pattern',
              'value': r'^[A-Z]{2}[0-9]{1,2}[A-Z]{0,3}[0-9]{1,4}$',
              'code': 'registration.malformed',
              'message': 'Check the digits — example MH12AB1234.',
            },
          ],
        },
        <String, Object?>{
          'key': 'city',
          'label': 'City',
          'control': 'select',
          'required': true,
          'placeholder': 'Where is the car now?',
          'optionsFromAttribute': null,
          'options': <Object?>[
            <String, Object?>{'value': 'pune', 'label': 'Pune'},
            <String, Object?>{'value': 'mumbai', 'label': 'Mumbai'},
            <String, Object?>{'value': 'bengaluru', 'label': 'Bengaluru'},
            <String, Object?>{'value': 'delhi', 'label': 'Delhi'},
            <String, Object?>{'value': 'hyderabad', 'label': 'Hyderabad'},
            <String, Object?>{'value': 'ahmedabad', 'label': 'Ahmedabad'},
          ],
          'validation': <Object?>[
            <String, Object?>{
              'rule': 'required',
              'code': 'city.required',
              'message': 'Pick the city the car is in.',
            },
          ],
        },
        <String, Object?>{
          'key': 'fuel_type',
          'label': 'Fuel',
          'control': 'radio',
          'required': true,
          'optionsFromAttribute': 'fuel_type',
          'validation': <Object?>[
            <String, Object?>{
              'rule': 'required',
              'code': 'fuel_type.required',
              'message': 'Select the fuel type.',
            },
          ],
        },
        <String, Object?>{
          'key': 'kms_driven',
          'label': 'Kms driven',
          'control': 'number',
          'unit': 'km',
          'required': true,
          'placeholder': '38400',
          'validation': <Object?>[
            <String, Object?>{
              'rule': 'max',
              'value': 500000,
              'code': 'kms.too_high',
              'message': 'Enter the odometer reading in kilometres.',
            },
            <String, Object?>{
              'rule': 'min',
              'value': 0,
              'code': 'kms.negative',
              'message': 'Kms driven cannot be negative.',
            },
          ],
        },
        <String, Object?>{
          'key': 'owners',
          'label': 'Owners',
          'control': 'select',
          'required': true,
          'optionsFromAttribute': 'owners',
          'helpText': 'As recorded on the RC.',
        },
        <String, Object?>{
          'key': 'battery_health_pct',
          'label': 'Battery health',
          'control': 'slider',
          'unit': '%',
          'required': true,
          'helpText': 'From the vehicle display, or from the last service report.',
          'config': <String, Object?>{'min': 40, 'max': 100, 'step': 1},
          // This is why adding EVs needed no code.
          'visibleWhen': <String, Object?>{
            '==': <Object?>[
              <String, Object?>{'var': 'fuel_type'},
              'ev',
            ],
          },
        },
      ],
    },
    <String, Object?>{
      'key': 'condition',
      'title': 'Condition',
      'description': 'Be straight about it. An inspection will find everything anyway.',
      'fields': <Object?>[
        <String, Object?>{
          'key': 'condition',
          'label': 'Overall condition',
          'control': 'radio',
          'required': true,
          'options': <Object?>[
            <String, Object?>{'value': 'excellent', 'label': 'Excellent — nothing to mention'},
            <String, Object?>{'value': 'good', 'label': 'Good — normal wear'},
            <String, Object?>{'value': 'fair', 'label': 'Fair — needs minor work'},
            <String, Object?>{'value': 'poor', 'label': 'Poor — needs major work'},
          ],
        },
        <String, Object?>{
          'key': 'accident_history',
          'label': 'Accident history',
          'control': 'toggle',
          'required': false,
          'helpText': 'Any repair involving structural work.',
        },
        <String, Object?>{
          'key': 'accident_notes',
          'label': 'What was repaired',
          'control': 'rich_text',
          'required': true,
          'placeholder':
              'Front-left panel replaced after a 2023 collision. Insurance claim settled.',
          'visibleWhen': <String, Object?>{
            '==': <Object?>[
              <String, Object?>{'var': 'accident_history'},
              true,
            ],
          },
        },
        <String, Object?>{
          'key': 'photos',
          'label': 'Photos',
          'control': 'photo_set',
          'required': true,
          'helpText':
              'Daylight, all four corners, and the odometer. Buyers skip listings without them.',
          'config': <String, Object?>{
            'min': 6,
            'max': 20,
            'prompts': <Object?>[
              'Front 3/4',
              'Rear 3/4',
              'Odometer',
              'Interior',
              'Engine bay',
              'Tyres',
            ],
          },
        },
      ],
    },
    <String, Object?>{
      'key': 'contact',
      'title': 'Where to send the price',
      'fields': <Object?>[
        <String, Object?>{
          'key': 'phone',
          'label': 'Mobile number',
          'control': 'phone',
          'required': true,
          'placeholder': '98200 12345',
          'helpText': 'Used for the offer and nothing else.',
          'validation': <Object?>[
            <String, Object?>{
              'rule': 'phone',
              'code': 'phone.invalid',
              'message': 'Enter a 10-digit Indian mobile number.',
            },
          ],
        },
        <String, Object?>{
          'key': 'expected_price_minor',
          'label': 'Expected price',
          'control': 'money',
          'required': false,
          'helpText': 'Optional. We will show you what the market says either way.',
          'config': <String, Object?>{'currency': 'INR', 'minMinor': 1000000},
        },
      ],
    },
  ],
};
