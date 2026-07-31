/// Registry documents as the server returns them, as raw JSON.
///
/// Raw maps rather than constructed Dart objects, on purpose and for the same reason
/// `autohub_forms`' fixture is: a test that built a `FormDefinition` by hand would prove that the
/// screen can draw a form somebody wrote in Dart, which is not the claim being made. The claim is
/// that a document authored in an admin console, serialised as JSON and parsed by the client
/// produces the fields, the rules and the conditional visibility it describes — with nothing in the
/// binary naming any of them.
///
/// The sell form here is deliberately **not** the production `sell_vehicle` document. The renderer's
/// fidelity against the real thing is already covered in `autohub_forms`; what these tests need to
/// prove is that this app renders *whatever arrives*, so the fixture carries a field
/// (`trailer_axles`) that no vehicle marketplace UI would ever hand-write. If the screen draws it,
/// the screen is not hand-written.
library;

const Map<String, Object?> featuresJson = <String, Object?>{
  'version': 12,
  'modules': <String, Object?>{'auction': true, 'lease': false, 'attachment': true, 'rental': true},
  'flags': <String, Object?>{'instant_offer': true},
};

const Map<String, Object?> citiesJson = <String, Object?>{
  'version': 9,
  'items': <Object?>[
    <String, Object?>{
      'code': 'pune',
      'name': 'Pune',
      'stateCode': 'MH',
      'serviceable': true,
      'modules': <Object?>['buy', 'sell'],
    },
  ],
};

/// Empty on purpose: an empty bundle exercises the fallback chain the contract mandates — locale,
/// then `en-IN`, then the authored default, and never an empty string.
const Map<String, Object?> i18nJson = <String, Object?>{
  'locale': 'en-IN',
  'version': 88,
  'resources': <String, Object?>{},
};

/// A two-step form with a field this app could not possibly have anticipated.
const Map<String, Object?> sellFormJson = <String, Object?>{
  'key': 'sell_vehicle',
  'version': 7,
  'title': 'Tell us about your vehicle',
  'submitLabel': 'Get my price',
  'steps': <Object?>[
    <String, Object?>{
      'key': 'identity',
      'title': 'Your vehicle',
      'description': 'We pull most of this from the RTO record.',
      'fields': <Object?>[
        <String, Object?>{
          'key': 'registration_number',
          'label': 'Registration number',
          'control': 'plate',
          'required': true,
          'placeholder': 'MH 12 AB 1234',
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
          'key': 'kms_driven',
          'label': 'Kms driven',
          'control': 'number',
          'unit': 'km',
          'required': true,
          'validation': <Object?>[
            <String, Object?>{
              'rule': 'max',
              'value': 500000,
              'code': 'kms.too_high',
              'message': 'Enter the odometer reading in kilometres.',
            },
          ],
        },
        // The proof. Nothing in this binary knows what a trailer axle is, and the field renders.
        <String, Object?>{
          'key': 'trailer_axles',
          'label': 'Trailer axles',
          'control': 'number',
          'required': false,
          'helpText': 'Only for goods carriers.',
        },
      ],
    },
    <String, Object?>{
      'key': 'condition',
      'title': 'Condition',
      'fields': <Object?>[
        <String, Object?>{
          'key': 'notes',
          'label': 'Anything we should know',
          'control': 'text',
          'required': false,
        },
      ],
    },
  ],
};

/// A quote, with the shape §10 pins: bands in integer minor units, a range, a confidence and a date.
const Map<String, Object?> valuationQuoteJson = <String, Object?>{
  'quoteId': '019fab0c-quote',
  'vehicle': <String, Object?>{
    'title': '2021 Maruti Suzuki Swift VXi',
    'registration': <String, Object?>{
      'canonical': 'MH12AB1234',
      'display': 'MH 12 AB 1234',
      'plateClass': 'private',
    },
    'year': 2021,
    'fuel': 'Petrol',
    'source': 'vahan',
  },
  'currency': 'INR',
  'valuations': <Object?>[
    <String, Object?>{
      'code': 'market',
      'label': 'Market value',
      'amountMinor': 54900000,
      'isPrimary': true,
    },
    <String, Object?>{'code': 'trade', 'label': 'Trade value', 'amountMinor': 49500000},
  ],
  'range': <String, Object?>{'lowMinor': 47800000, 'highMinor': 58200000},
  'confidence': <String, Object?>{
    'score': 0.86,
    'band': 'high',
    'basedOn': '412 comparable sales in Pune over 90 days',
  },
  'asOf': '2026-07-29',
  'expiresAt': '2026-08-05T00:00:00Z',
  'modelVersion': 'valuation-ensemble@2026.07.3',
};

/// One search hit, in the §9 projection.
const Map<String, Object?> searchItemJson = <String, Object?>{
  'listingId': '019fab0c-listing',
  'intent': 'sale',
  'registration': <String, Object?>{
    'canonical': 'MH12AB1234',
    'display': 'MH 12 AB 1234',
    'plateClass': 'private',
  },
  'title': '2021 Maruti Suzuki Swift VXi',
  'priceMinor': 54900000,
  'city': 'Pune',
  'specStrip': <Object?>[
    <String, Object?>{'label': 'Year', 'value': '2021'},
    <String, Object?>{'label': 'Kms', 'value': '38,400'},
  ],
  'media': <String, Object?>{'aspectRatio': 1.5, 'count': 14},
  'badges': <Object?>['inspected'],
};

/// Facets exactly as §9 describes them — and one style this build has never heard of, which must be
/// skipped rather than take the sheet down.
const List<Object?> facetsJson = <Object?>[
  <String, Object?>{
    'code': 'fuel_type',
    'label': 'Fuel',
    'style': 'chips',
    'options': <Object?>[
      <String, Object?>{'value': 'petrol', 'label': 'Petrol', 'count': 412},
      <String, Object?>{'value': 'ev', 'label': 'Electric', 'count': 18},
    ],
  },
  <String, Object?>{
    'code': 'kms_driven',
    'label': 'Kms driven',
    'style': 'range',
    'min': 0,
    'max': 180000,
    'unit': 'km',
  },
  <String, Object?>{
    'code': 'price_histogram',
    'label': 'Price distribution',
    'style': 'histogram',
  },
];
