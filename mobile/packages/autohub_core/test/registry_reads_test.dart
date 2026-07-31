import 'package:autohub_core/autohub_core.dart';
import 'package:flutter_test/flutter_test.dart';

/// The north star's claim, tested where it is cheapest to test it: an operations user adds a field
/// to a registry document and it reaches a handset with its validation, its unit and its
/// conditional visibility, with nothing rebuilt.
///
/// These read through the cache with the network explicitly dead, so passing means the claim holds
/// on the flight home as well as on office wifi.
void main() {
  late AhDatabase database;

  setUp(() => database = openInMemoryAhDatabase());
  tearDown(() => database.close());

  /// Seeds the cache the way a previous session would have, then hands back a client with no
  /// network at all.
  Future<RegistryClient> offlineClientWith(RegistryKey key, Map<String, Object?> body) async {
    final store = RegistryStore(database);
    await store.write(
      RegistrySnapshot(
        name: key.name,
        version: 7,
        etag: '"seeded"',
        body: body,
        fetchedAt: DateTime.utc(2026, 7, 22),
      ),
    );
    return RegistryClient(store: store, transport: _DeadNetwork());
  }

  group('the sell form', () {
    test('the EV field arrives from a registry row, not from Dart', () async {
      // Exactly the document in contracts/registry-api.md §2. Nothing in this workspace names
      // `battery_health_pct`, `ev`, or `fuel_type` outside this fixture.
      final client = await offlineClientWith(RegistryDocuments.form('sell_vehicle'), const {
        'key': 'sell_vehicle',
        'version': 7,
        'title': 'Tell us about your car',
        'submitLabel': 'Get my price',
        'steps': [
          {
            'key': 'identity',
            'title': 'Your car',
            'fields': [
              {
                'key': 'fuel_type',
                'label': 'Fuel',
                'control': 'select',
                'required': true,
                'optionsFromAttribute': 'fuel_type',
              },
              {
                'key': 'battery_health_pct',
                'label': 'Battery health',
                'control': 'slider',
                'unit': '%',
                'required': true,
                'visibleWhen': {
                  '==': [
                    {'var': 'fuel_type'},
                    'ev',
                  ],
                },
              },
            ],
          },
        ],
      });

      final form = await client.formDefinition('sell_vehicle');

      expect(form.key, 'sell_vehicle');
      expect(form.submitLabel, 'Get my price');

      final battery = form.steps.single.fields.firstWhere(
        (field) => field.key == 'battery_health_pct',
      );
      expect(battery.control, FormControl.slider);
      expect(battery.unit, '%');
      expect(battery.isRequired, isTrue);
      expect(
        battery.visibleWhen,
        {
          '==': [
            {'var': 'fuel_type'},
            'ev',
          ],
        },
        reason: 'conditional visibility is JsonLogic from the registry, not a Dart if-statement',
      );

      await client.settle();
      await client.dispose();
    });

    test('an unknown control degrades instead of blanking the screen that creates supply',
        () async {
      final client = await offlineClientWith(RegistryDocuments.form('sell_vehicle'), const {
        'key': 'sell_vehicle',
        'version': 8,
        'title': 'Tell us about your car',
        'steps': [
          {
            'key': 'identity',
            'title': 'Your car',
            'fields': [
              {'key': 'holographic_scan', 'label': 'Scan', 'control': 'holo_capture'},
            ],
          },
        ],
      });

      final form = await client.formDefinition('sell_vehicle');
      expect(form.steps.single.fields.single.control, FormControl.unsupported);

      await client.settle();
      await client.dispose();
    });
  });

  group('taxonomy', () {
    test('a category the client has never heard of decodes anyway', () async {
      final client = await offlineClientWith(RegistryDocuments.taxonomyTree, const {
        'version': 41,
        'categories': [
          {
            'code': 'tractor',
            'name': 'Tractor',
            'sortOrder': 9,
            'attributes': ['fuel_type', 'pto_hp'],
            'children': <Object?>[],
          },
        ],
      });

      final tree = await client.taxonomyTree();
      expect(tree.version, 41);
      expect(tree.categories.single.code, 'tractor');
      expect(tree.categories.single.attributes, contains('pto_hp'));

      await client.settle();
      await client.dispose();
    });

    test('attributes are keyed per category, so a truck never gets an SUV list', () async {
      final store = RegistryStore(database);
      await store.write(
        RegistrySnapshot(
          name: RegistryDocuments.attributes('suv').name,
          version: 41,
          etag: null,
          body: const {
            'version': 41,
            'attributes': [
              {'code': 'seats', 'label': 'Seats', 'dataType': 'integer'},
            ],
          },
          fetchedAt: DateTime.utc(2026, 7, 22),
        ),
      );
      await store.write(
        RegistrySnapshot(
          name: RegistryDocuments.attributes('truck').name,
          version: 41,
          etag: null,
          body: const {
            'version': 41,
            'attributes': [
              {'code': 'payload_kg', 'label': 'Payload', 'dataType': 'integer', 'unit': 'kg'},
            ],
          },
          fetchedAt: DateTime.utc(2026, 7, 22),
        ),
      );

      final client = RegistryClient(store: store, transport: _DeadNetwork());
      expect((await client.attributes('suv')).attributes.single.code, 'seats');
      expect((await client.attributes('truck')).attributes.single.code, 'payload_kg');

      await client.settle();
      await client.dispose();
    });
  });

  group('features', () {
    test('an unknown flag reads as off', () async {
      final client = await offlineClientWith(RegistryDocuments.features, const {
        'version': 12,
        'modules': {'auction': true, 'lease': false},
        'flags': {'instant_offer': true},
      });

      final features = await client.features();
      expect(features.module('auction'), isTrue);
      expect(features.module('lease'), isFalse);
      // Fail closed: a Lease button in a city where leasing is not live is a support call.
      expect(features.module('spaceflight'), isFalse);
      expect(features.flag('ai_damage_detection'), isFalse);

      await client.settle();
      await client.dispose();
    });
  });

  group('i18n', () {
    test('resolves through the locale, then en-IN, then the key itself', () async {
      final store = RegistryStore(database);
      await store.write(
        RegistrySnapshot(
          name: RegistryDocuments.i18n('mr-IN').name,
          version: 88,
          etag: null,
          body: const {
            'locale': 'mr-IN',
            'version': 88,
            'resources': {'listing.cta.get_valuation': 'किंमत जाणून घ्या'},
          },
          fetchedAt: DateTime.utc(2026, 7, 22),
        ),
      );
      await store.write(
        RegistrySnapshot(
          name: RegistryDocuments.i18n('en-IN').name,
          version: 88,
          etag: null,
          body: const {
            'locale': 'en-IN',
            'version': 88,
            'resources': {
              'listing.cta.get_valuation': 'Get valuation',
              'listing.cta.book_inspection': 'Book an inspection',
            },
          },
          fetchedAt: DateTime.utc(2026, 7, 22),
        ),
      );

      final client = RegistryClient(store: store, transport: _DeadNetwork());
      final strings = await client.strings('mr-IN');

      expect(strings('listing.cta.get_valuation'), 'किंमत जाणून घ्या');
      // Not yet translated into Marathi — English rather than a blank button.
      expect(strings('listing.cta.book_inspection'), 'Book an inspection');
      // In neither bundle — the key, which is ugly and diagnosable. Never an empty string.
      expect(strings('listing.cta.never_translated'), 'listing.cta.never_translated');
      expect(strings.has('listing.cta.never_translated'), isFalse);

      await client.settle();
      await client.dispose();
    });

    test('a missing en-IN bundle degrades rather than failing the screen', () async {
      final client = await offlineClientWith(RegistryDocuments.i18n('mr-IN'), const {
        'locale': 'mr-IN',
        'version': 88,
        'resources': {'listing.cta.get_valuation': 'किंमत जाणून घ्या'},
      });

      final strings = await client.strings('mr-IN');
      expect(strings.fallback, isNull);
      expect(strings('listing.cta.get_valuation'), 'किंमत जाणून घ्या');
      expect(strings('listing.cta.book_inspection'), 'listing.cta.book_inspection');

      await client.settle();
      await client.dispose();
    });
  });

  group('decoding failures', () {
    test('a body this build cannot read is evicted so the next launch can fix itself', () async {
      // What an app upgrade against an older cached document looks like.
      final store = RegistryStore(database);
      await store.write(
        RegistrySnapshot(
          name: RegistryDocuments.taxonomyTree.name,
          version: 40,
          etag: null,
          body: const {'categories': 'this used to be a list'},
          fetchedAt: DateTime.utc(2026, 7, 22),
        ),
      );

      final client = RegistryClient(store: store, transport: _DeadNetwork());
      await expectLater(client.taxonomyTree(), throwsA(isA<RegistryUnavailableException>()));

      expect(
        await store.read(RegistryDocuments.taxonomyTree.name),
        isNull,
        reason: 'keeping it means the screen stays broken every launch forever',
      );

      await client.settle();
      await client.dispose();
    });
  });
}

/// No signal, no wifi, no captive portal. Every fetch fails.
final class _DeadNetwork implements RegistryTransport {
  @override
  Future<RegistryFetch> fetch(RegistryKey key, {String? ifNoneMatch}) async {
    throw const _Unreachable();
  }
}

final class _Unreachable implements Exception {
  const _Unreachable();
  @override
  String toString() => 'Unreachable';
}
