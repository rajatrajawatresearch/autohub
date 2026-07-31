import 'package:autohub_core/autohub_core.dart';
import 'package:flutter_test/flutter_test.dart';

/// The registry cache is the load-bearing claim of §4: *a cold launch on a dead network still
/// renders a sell form.* These tests exist to make that claim false-if-broken rather than
/// aspirational, so every one of them either kills the network or moves a version.
void main() {
  late AhDatabase database;

  setUp(() => database = openInMemoryAhDatabase());
  tearDown(() => database.close());

  final RegistryKey sellForm = RegistryDocuments.form('sell_vehicle');

  group('cold launch with no network', () {
    test('renders the sell form from the last known snapshot', () async {
      // ── Session one: the user opened the app on wifi at home, once, a week ago. ──
      final online = _FakeRegistryTransport()
        ..respondWith(
          const RegistryFresh(
            version: 7,
            etag: '"forms:sell_vehicle:v7"',
            body: {'key': 'sell_vehicle', 'version': 7},
          ),
          steps: [
            {'key': 'identity'},
          ],
        );
      final firstLaunch = RegistryClient(store: RegistryStore(database), transport: online);
      await firstLaunch.read(sellForm);
      await firstLaunch.settle();
      await firstLaunch.dispose();

      // ── Session two: the same handset, in a basement, with no signal at all. ──
      final offline = _FakeRegistryTransport()..failEveryFetch('no route to host');
      final coldLaunch = RegistryClient(store: RegistryStore(database), transport: offline);

      final snapshot = await coldLaunch.read(sellForm);

      expect(snapshot.version, 7);
      expect(snapshot.body['key'], 'sell_vehicle');
      expect(
        (snapshot.body['steps']! as List<Object?>).length,
        1,
        reason: 'the form the renderer needs must survive the process, not just the session',
      );

      // It did try — this is a cache that revalidates, not a cache that gives up.
      await coldLaunch.settle();
      expect(offline.attempts, 1);

      // And the failure never reached the caller: the screen rendered.
      expect(coldLaunch.lastFailureFor(sellForm), isNotNull);
      await coldLaunch.dispose();
    });

    test('throws only when the document was never cached and cannot be fetched', () async {
      final offline = _FakeRegistryTransport()..failEveryFetch('no route to host');
      final client = RegistryClient(store: RegistryStore(database), transport: offline);

      await expectLater(
        client.read(RegistryDocuments.taxonomyTree),
        throwsA(isA<RegistryUnavailableException>()),
      );
      await client.dispose();
    });
  });

  group('revalidation', () {
    test('sends the stored ETag as If-None-Match and keeps the body on 304', () async {
      const monday = _FixedClock(2026, 7, 27);
      const friday = _FixedClock(2026, 7, 31);

      final transport = _FakeRegistryTransport()
        ..respondWith(
          const RegistryFresh(
            version: 41,
            etag: '"taxonomy:v41"',
            body: {'version': 41, 'categories': <Object?>[]},
          ),
        );
      final store = RegistryStore(database);
      final first = RegistryClient(store: store, transport: transport, clock: monday.now);
      await first.read(RegistryDocuments.taxonomyTree);
      await first.settle();
      await first.dispose();

      transport
        ..reset()
        ..respondWith(const RegistryNotModified(etag: '"taxonomy:v41"'));

      final second = RegistryClient(store: store, transport: transport, clock: friday.now);
      final served = await second.read(RegistryDocuments.taxonomyTree);
      expect(served.version, 41, reason: 'disk is served before the network is consulted');
      await second.settle();

      expect(
        transport.conditionalHeaders,
        ['"taxonomy:v41"'],
        reason: 'without the ETag every launch re-downloads the whole taxonomy',
      );

      // A 304 is the server confirming the body, so the body stays and only its confirmation time
      // moves. Re-reading through a fresh store proves it was persisted, not just memoised.
      final persisted = await RegistryStore(database).read(RegistryDocuments.taxonomyTree.name);
      expect(persisted!.version, 41);
      expect(persisted.body['version'], 41);
      expect(persisted.fetchedAt.toUtc(), friday.now());
      await second.dispose();
    });

    test('swaps the body on 200 and announces the new version', () async {
      final store = RegistryStore(database);
      final transport = _FakeRegistryTransport()
        ..respondWith(
          const RegistryFresh(
            version: 41,
            etag: '"taxonomy:v41"',
            body: {'version': 41, 'categories': <Object?>[]},
          ),
        );
      final first = RegistryClient(store: store, transport: transport);
      await first.read(RegistryDocuments.taxonomyTree);
      await first.settle();
      await first.dispose();

      // Operations added the EV category. The version moves; nothing was rebuilt.
      transport
        ..reset()
        ..respondWith(
          const RegistryFresh(
            version: 42,
            etag: '"taxonomy:v42"',
            body: {
              'version': 42,
              'categories': [
                {'code': 'ev'},
              ],
            },
          ),
        );

      final second = RegistryClient(store: store, transport: transport);
      final announced = <String>[];
      final subscription = second.changes.listen((key) => announced.add(key.name));

      expect((await second.read(RegistryDocuments.taxonomyTree)).version, 41);
      await second.settle();
      await Future<void>.delayed(Duration.zero);

      expect(announced, [RegistryDocuments.taxonomyTree.name]);
      final swapped = await RegistryStore(database).read(RegistryDocuments.taxonomyTree.name);
      expect(swapped!.version, 42);
      expect(swapped.etag, '"taxonomy:v42"');

      await subscription.cancel();
      await second.dispose();
    });

    test('collapses concurrent reads of one document into one request', () async {
      final transport = _FakeRegistryTransport()
        ..respondWith(
          const RegistryFresh(version: 12, etag: '"features:v12"', body: {'version': 12}),
        );
      final client = RegistryClient(store: RegistryStore(database), transport: transport);

      // Six widgets asking during the same frame. On a train this is the difference between one
      // timeout and six.
      await Future.wait([
        for (var i = 0; i < 6; i++) client.read(RegistryDocuments.features),
      ]);
      await client.settle();

      expect(transport.attempts, 1);
      await client.dispose();
    });
  });

  group('cache keys', () {
    test('two categories are two documents, not one overwriting the other', () async {
      // The bug this prevents: an SUV's attribute list served to somebody selling a truck.
      expect(
        RegistryDocuments.attributes('suv').name,
        isNot(RegistryDocuments.attributes('truck').name),
      );
    });

    test('query order does not create a second cache entry', () {
      expect(
        RegistryKey('/platform/taxonomy/variants', {'model': 'swift', 'year': 2021}).name,
        RegistryKey('/platform/taxonomy/variants', {'year': 2021, 'model': 'swift'}).name,
      );
    });
  });

  group('corrupt cache', () {
    test('an undecodable body reads as a miss rather than a crash', () async {
      // What a filesystem that filled up mid-write leaves behind. The launch must survive it.
      await database.into(database.cachedRegistries).insert(
            CachedRegistriesCompanion.insert(
              name: '/platform/features',
              version: 12,
              body: '{"version":',
              fetchedAt: DateTime.utc(2026, 7, 29, 10),
            ),
          );

      final store = RegistryStore(database);
      expect(await store.read('/platform/features'), isNull);
      // And it was evicted, so the next launch refetches instead of retrying the same bad row.
      expect(await store.all(), isEmpty);
    });
  });
}

/// A transport under the test's control.
///
/// Deliberately not a mock of dio: the behaviour worth testing is what the cache does with a
/// `304`, a version bump and a dead socket, and none of those need HTTP to reproduce.
final class _FakeRegistryTransport implements RegistryTransport {
  RegistryFetch? _response;
  Map<String, Object?>? _extraBody;
  String? _failure;

  /// Every `If-None-Match` this transport was handed, in order.
  final List<String?> conditionalHeaders = [];
  int attempts = 0;

  void respondWith(RegistryFetch response, {List<Map<String, Object?>>? steps}) {
    _response = response;
    _extraBody = steps == null ? null : {'steps': steps};
    _failure = null;
  }

  void failEveryFetch(String reason) {
    _failure = reason;
    _response = null;
  }

  void reset() {
    conditionalHeaders.clear();
    attempts = 0;
  }

  @override
  Future<RegistryFetch> fetch(RegistryKey key, {String? ifNoneMatch}) async {
    attempts += 1;
    conditionalHeaders.add(ifNoneMatch);

    final failure = _failure;
    if (failure != null) throw _UnreachableException(failure);

    final response = _response;
    if (response == null) throw _UnreachableException('no response scripted for ${key.name}');

    return switch (response) {
      RegistryFresh(:final version, :final etag, :final body) => RegistryFresh(
          version: version,
          etag: etag,
          body: {...body, ...?_extraBody},
        ),
      RegistryNotModified() => response,
    };
  }
}

final class _UnreachableException implements Exception {
  const _UnreachableException(this.reason);
  final String reason;
  @override
  String toString() => 'Unreachable: $reason';
}

/// A clock that does not move, so an assertion about *which* timestamp was written is an assertion
/// about behaviour rather than about how fast the test machine ran.
final class _FixedClock {
  const _FixedClock(this.year, this.month, this.day);

  final int year;
  final int month;
  final int day;

  DateTime now() => DateTime.utc(year, month, day);
}
