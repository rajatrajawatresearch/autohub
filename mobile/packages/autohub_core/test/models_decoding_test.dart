import 'package:autohub_core/src/contract/state_category.dart';
import 'package:autohub_core/src/models/models.dart';
import 'package:flutter_test/flutter_test.dart';

/// What the models do when the server is ahead of the app.
///
/// Every case below is a registry row an operations user can create today, on a build shipped last
/// month, and every one of them used to be — or would be — a blank screen. On the web a bad decode
/// is fixed by a deploy; here it waits for an app-store review, so the whole decode layer is built
/// to lose a field rather than a page.
void main() {
  group('media — the bug that blanked a search page', () {
    test('a listing with no photos is a real state, not a parse failure', () {
      // A dealer's bulk upload lands the rows before the images. Requiring `coverUrl` here once
      // failed the parse for an *entire* search response, and the marketplace rendered empty.
      final item = SearchItem.fromJson(<String, Object?>{
        'listingId': '019fa',
        'intent': 'sale',
        'registration': <String, Object?>{'canonical': 'MH12AB1234', 'display': 'MH 12 AB 1234'},
        'title': '2021 Maruti Suzuki Swift VXi',
        'media': <String, Object?>{'coverUrl': null, 'aspectRatio': null, 'count': 0},
      });

      expect(item.media.hasCover, isFalse);
      expect(item.media.coverUrl, isNull);
    });

    test('a missing aspect ratio still reserves a slot, so the list does not jump', () {
      const media = Media();

      expect(media.aspectRatio, isNull);
      expect(media.displayAspectRatio, 1.5);
    });

    test('the media object may be absent entirely', () {
      final item = SearchItem.fromJson(<String, Object?>{
        'listingId': '019fa',
        'registration': <String, Object?>{'canonical': 'MH12AB1234', 'display': 'MH 12 AB 1234'},
        'title': 'Swift',
      });

      expect(item.media.count, 0);
      expect(item.intent, ListingIntent.sale);
    });
  });

  group('search facets — one unknown style must not empty the marketplace', () {
    test('the four known styles decode to their own cases', () {
      final facets = <SearchFacet>[
        SearchFacet.fromJson(<String, Object?>{
          'code': 'fuel_type',
          'label': 'Fuel',
          'style': 'chips',
          'options': <Object?>[
            <String, Object?>{'value': 'petrol', 'label': 'Petrol', 'count': 412},
          ],
        }),
        SearchFacet.fromJson(<String, Object?>{
          'code': 'kms_driven',
          'label': 'Kms driven',
          'style': 'range',
          'min': 0,
          'max': 180000,
        }),
      ];

      // The union is sealed, so this switch is exhaustive by compilation rather than by review.
      final rendered = facets.map((facet) => switch (facet) {
            SearchFacetChips(:final options) => 'chips:${options.length}',
            SearchFacetList(:final options) => 'list:${options.length}',
            SearchFacetToggle(:final options) => 'toggle:${options.length}',
            SearchFacetRange(:final min, :final max) => 'range:$min-$max',
            SearchFacetUnsupported(:final code) => 'unsupported:$code',
          });

      expect(rendered, <String>['chips:1', 'range:0.0-180000.0']);
    });

    test('a style this build has never heard of degrades to one missing filter', () {
      final facet = SearchFacet.fromJson(<String, Object?>{
        'code': 'body_style',
        'label': 'Body',
        'style': 'colour_swatch',
      });

      expect(facet, isA<SearchFacetUnsupported>());
    });

    test('and the rest of the response still parses around it', () {
      final response = SearchResponse.fromJson(<String, Object?>{
        'items': <Object?>[
          <String, Object?>{
            'listingId': '019fa',
            'registration': <String, Object?>{'canonical': 'MH12AB1234', 'display': 'MH 12 AB 1234'},
            'title': 'Swift',
          },
        ],
        'facets': <Object?>[
          <String, Object?>{'code': 'body_style', 'label': 'Body', 'style': 'colour_swatch'},
          <String, Object?>{'code': 'fuel_type', 'label': 'Fuel', 'style': 'chips'},
        ],
        'nextCursor': 'eyJ',
        'totalEstimate': 1284,
      });

      expect(response.items, hasLength(1));
      expect(response.facets, hasLength(2));
      expect(response.totalEstimate, 1284);
    });
  });

  group('form registry — a schema ahead of the client', () {
    test('an unshipped control renders a placeholder rather than deleting the field', () {
      final field = FormField.fromJson(<String, Object?>{
        'key': 'ownership_proof',
        'label': 'Ownership proof',
        'control': 'signature_pad',
        'required': true,
      });

      expect(field.control, FormControl.unsupported);
      // The field survives, so the step is not silently one question shorter.
      expect(field.key, 'ownership_proof');
      expect(field.isRequired, isTrue);
    });

    test('the wire spelling of `required` is preserved despite the Dart keyword', () {
      final field = FormField.fromJson(<String, Object?>{
        'key': 'kms_driven',
        'label': 'Kms driven',
        'control': 'number',
        'required': true,
      });

      expect(field.isRequired, isTrue);
      expect(field.toJson()['required'], true);
      expect(field.toJson().containsKey('isRequired'), isFalse);
    });

    test('validation rules decode to their own cases, and the odd one out survives', () {
      final rules = <ValidationRule>[
        ValidationRule.fromJson(<String, Object?>{
          'rule': 'pattern',
          'value': r'^[A-Z]{2}[0-9]{1,2}[A-Z]{0,3}[0-9]{1,4}$',
          'code': 'registration.malformed',
          'message': 'Check the digits — example MH12AB1234.',
        }),
        ValidationRule.fromJson(<String, Object?>{
          'rule': 'max',
          'value': 500000,
          'code': 'kms.too_high',
          'message': 'Enter the odometer reading in kilometres.',
        }),
        ValidationRule.fromJson(<String, Object?>{
          'rule': 'luhn',
          'code': 'account.checksum',
          'message': 'That account number does not check out.',
        }),
      ];

      expect(rules[0], isA<ValidationRulePattern>());
      expect(rules[1], isA<ValidationRuleMax>());
      // Left to the server, which is authoritative anyway. The alternative — failing the whole form
      // definition — means the seller cannot list their car at all.
      expect(rules[2], isA<ValidationRuleUnsupported>());
    });

    test('a full sell-form document round-trips, conditional field and all', () {
      final definition = FormDefinition.fromJson(<String, Object?>{
        'key': 'sell_vehicle',
        'version': 7,
        'title': 'Tell us about your car',
        'submitLabel': 'Get my price',
        'steps': <Object?>[
          <String, Object?>{
            'key': 'identity',
            'title': 'Your car',
            'fields': <Object?>[
              <String, Object?>{
                'key': 'battery_health_pct',
                'label': 'Battery health',
                'control': 'slider',
                'unit': '%',
                'required': true,
                'visibleWhen': <String, Object?>{
                  '==': <Object?>[
                    <String, Object?>{'var': 'fuel_type'},
                    'ev',
                  ],
                },
              },
            ],
          },
        ],
      });

      final field = definition.steps.single.fields.single;
      expect(field.control, FormControl.slider);
      expect(field.unit, '%');
      // The rule travels as opaque JSON. Nothing here interprets it — `JsonLogic` does, identically
      // to the web and the server.
      expect(field.visibleWhen, isA<Map<String, Object?>>());
    });
  });

  group('workflow', () {
    test('a state code is data; its category is contract', () {
      final state = WorkflowState.fromJson(<String, Object?>{
        'code': 'pending_review',
        'name': 'Under review',
        'category': 'awaiting_action',
        'slaMinutes': 240,
      });

      expect(state.category, AhStateCategory.awaitingAction);
      expect(state.slaMinutes, 240);
    });

    test('a category added after this build shipped renders neutrally, as it does on the web', () {
      final state = WorkflowState.fromJson(<String, Object?>{
        'code': 'legal_hold',
        'name': 'Legal hold',
        'category': 'impounded',
      });

      // `packages/api` catches to `in_progress`. A phone that fell back to something else would
      // show the same listing in a different colour from the laptop beside it.
      expect(state.category, AhStateCategory.inProgress);
    });

    test('available actions come from the server, never from a permission check here', () {
      final snapshot = WorkflowSnapshot.fromJson(<String, Object?>{
        'workflowKey': 'listing',
        'currentState': <String, Object?>{
          'code': 'pending_review',
          'name': 'Under review',
          'category': 'awaiting_action',
        },
        'enteredAt': '2026-07-29T04:10:00Z',
        'availableActions': <Object?>[
          <String, Object?>{'actionCode': 'approve', 'label': 'Approve', 'emphasis': 'primary'},
          <String, Object?>{
            'actionCode': 'reject',
            'label': 'Reject',
            'requiresReason': true,
            'emphasis': 'nuclear',
          },
        ],
      });

      expect(snapshot.availableActions.first.emphasis, ActionEmphasis.primary);
      expect(snapshot.availableActions.last.requiresReason, isTrue);
      expect(snapshot.availableActions.last.emphasis, ActionEmphasis.standard);
    });
  });

  group('auction realtime', () {
    test('the three broadcast types decode to their own cases', () {
      final event = AuctionEvent.fromJson(<String, Object?>{
        'seq': 1042,
        'type': 'bid.placed',
        'data': <String, Object?>{
          'lotId': '019fa',
          'amountMinor': 51500000,
          'bidderMask': 'Dealer ••42',
          'placedAt': '2026-07-29T09:14:02.418Z',
          'highestBidderIsYou': false,
        },
      });

      final described = switch (event) {
        AuctionBidPlaced(:final seq, :final data) => '$seq:${data.amountMinor}',
        AuctionLotExtended(:final seq) => '$seq:extended',
        AuctionLotClosed(:final seq) => '$seq:closed',
        AuctionUnknownEvent(:final seq) => '$seq:unknown',
      };

      expect(described, '1042:51500000');
    });

    test('an event type this build cannot interpret still yields its sequence', () {
      // `seq` is the reconnect anchor: the client sends `?sinceSeq=` and receives the gap. Dropping
      // an unreadable frame would leave it asking for a gap it has already been sent, forever.
      final event = AuctionEvent.fromJson(<String, Object?>{
        'seq': 1051,
        'type': 'lot.reserve_lowered',
        'data': <String, Object?>{'newReserveMinor': 48000000},
      });

      expect(event, isA<AuctionUnknownEvent>());
      expect(event.seq, 1051);
    });
  });

  group('problem details', () {
    test('field errors merge the RFC pointers and the form registry`s metadata map', () {
      // Both are populated by the contract, and a renderer reading only one applies half the
      // server`s 422 while the user stares at a field that looks fine.
      final problem = ProblemDetails.fromJson(<String, Object?>{
        'status': 422,
        'code': 'common.validation.failed',
        'errors': <Object?>[
          <String, Object?>{
            'pointer': '/attributes/boom_length_m',
            'code': 'range.too_high',
            'message': 'Maximum 32 m.',
          },
        ],
        'metadata': <String, Object?>{
          'fieldErrors': <String, Object?>{
            'kms_driven': <Object?>['kms.too_high'],
          },
        },
      });

      expect(problem.fieldErrors, <String, List<String>>{
        'attributes.boom_length_m': <String>['Maximum 32 m.'],
        'kms_driven': <String>['kms.too_high'],
      });
    });

    test('a problem with nothing in it does not throw', () {
      expect(ProblemDetails.fromJson(const <String, Object?>{}).fieldErrors, isEmpty);
    });
  });

  group('registries', () {
    test('an unknown feature or module reads as off', () {
      final features = Features.fromJson(<String, Object?>{
        'version': 12,
        'modules': <String, Object?>{'auction': true, 'lease': false},
        'flags': <String, Object?>{'instant_offer': true},
      });

      expect(features.module('auction'), isTrue);
      expect(features.module('lease'), isFalse);
      expect(features.module('spaceflight'), isFalse);
      expect(features.flag('ai_damage_detection'), isFalse);
    });

    test('a missing string resolves to its key, never to an empty button', () {
      final bundle = I18nBundle.fromJson(<String, Object?>{
        'locale': 'hi-IN',
        'version': 88,
        'resources': <String, Object?>{'listing.cta.get_valuation': 'कीमत जानें'},
      });

      expect(bundle.resolve('listing.cta.get_valuation'), 'कीमत जानें');
      expect(bundle.resolve('listing.cta.book_test_drive'), 'listing.cta.book_test_drive');
    });

    test('a taxonomy attribute carries everything a field needs, and nothing is in Dart', () {
      final response = AttributesResponse.fromJson(<String, Object?>{
        'version': 41,
        'attributes': <Object?>[
          <String, Object?>{
            'code': 'fuel_type',
            'label': 'Fuel',
            'dataType': 'enum',
            'required': true,
            'searchable': true,
            'facetStyle': 'chips',
            'options': <Object?>[
              <String, Object?>{'value': 'ev', 'label': 'Electric', 'sortOrder': 4},
            ],
          },
          <String, Object?>{
            'code': 'boom_length_m',
            'label': 'Boom length',
            'dataType': 'furlongs',
            'unit': 'm',
            'facetStyle': 'dial',
          },
        ],
      });

      expect(response.attributes.first.dataType, AttributeDataType.singleChoice);
      expect(response.attributes.first.options?.single.value, 'ev');
      // An unrecognised data type edits as text and an unrecognised facet style renders as a list,
      // rather than dropping the attribute from the form and the filter bar.
      expect(response.attributes.last.dataType, AttributeDataType.string);
      expect(response.attributes.last.facetStyle, FacetStyle.list);
    });

    test('the category tree nests', () {
      final tree = TaxonomyTree.fromJson(<String, Object?>{
        'version': 41,
        'categories': <Object?>[
          <String, Object?>{
            'code': 'car',
            'name': 'Car',
            'attributes': <Object?>['fuel_type'],
            'children': <Object?>[
              <String, Object?>{'code': 'suv', 'name': 'SUV', 'sortOrder': 2},
            ],
          },
        ],
      });

      expect(tree.categories.single.children.single.code, 'suv');
      expect(tree.categories.single.attributes, <String>['fuel_type']);
    });
  });

  group('search params', () {
    test('the query grammar is built from attribute codes nothing here knows', () {
      const params = SearchParams(
        q: 'swift',
        city: 'pune',
        intent: ListingIntent.sale,
        price: RangeFilter(min: 300000, max: 600000),
        attributes: <String, AttributeFilter>{
          'fuel_type': AttributeFilter.anyOf(<String>['petrol', 'cng']),
          'kms_driven': AttributeFilter.range(RangeFilter(max: 40000)),
          'verified_rc': AttributeFilter.flag(true),
        },
        sort: SearchSort.priceAsc,
      );

      expect(params.toQuery(), <String, Object?>{
        'q': 'swift',
        'city': 'pune',
        'intent': 'sale',
        'price': '300000..600000',
        'sort': 'price_asc',
        'attr.fuel_type': 'petrol,cng',
        'attr.kms_driven': '..40000',
        'attr.verified_rc': 'true',
      });
    });

    test('an empty filter is absent rather than sent blank', () {
      const params = SearchParams(
        attributes: <String, AttributeFilter>{
          'fuel_type': AttributeFilter.anyOf(<String>[]),
        },
      );

      expect(params.toQuery(), isEmpty);
    });

    test('the cursor survives paging, which is what stops an infinite list looping on page one', () {
      const first = SearchParams(q: 'swift', city: 'pune');

      expect(first.nextPage('eyJ').toQuery()['cursor'], 'eyJ');
      expect(first.nextPage('eyJ').q, 'swift');
    });
  });
}
