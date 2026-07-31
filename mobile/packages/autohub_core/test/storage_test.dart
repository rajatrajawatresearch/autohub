import 'dart:convert';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  _providerGroup();

  group('DeviceIdentity', () {
    test('generates once and then never changes', () async {
      final secure = InMemorySecureStore();
      final first = await DeviceIdentity(secure).ensure();

      // A new instance, as on the next cold launch. A device id that resets while the refresh
      // token survives signs the user out for a reason they cannot see.
      final second = await DeviceIdentity(secure).ensure();

      expect(first, isNotEmpty);
      expect(second, first);
    });

    test('memoises, because it is read on every request', () async {
      final secure = _CountingSecureStore();
      final identity = DeviceIdentity(secure);

      await identity.ensure();
      await identity.ensure();
      await identity.ensure();

      expect(secure.reads, 1, reason: 'a keychain hop per HTTP header is a platform channel storm');
      expect(identity.cached, isNotNull);
    });

    test('two installs are two devices', () async {
      final a = await DeviceIdentity(InMemorySecureStore()).ensure();
      final b = await DeviceIdentity(InMemorySecureStore()).ensure();
      expect(a, isNot(b));
    });
  });

  group('SessionStore', () {
    test('round-trips a session through the keychain', () async {
      final store = SessionStore(InMemorySecureStore());
      final session = StoredSession(
        accessToken: 'access',
        refreshToken: 'refresh',
        accessTokenExpiresAt: DateTime.utc(2026, 7, 29, 9, 15),
        userId: '019fab0c',
      );

      await store.write(session);
      final read = await store.read();

      expect(read!.accessToken, 'access');
      expect(read.refreshToken, 'refresh');
      expect(read.userId, '019fab0c');
      expect(read.accessTokenExpiresAt, DateTime.utc(2026, 7, 29, 9, 15));
    });

    test('a corrupt entry reads as signed out rather than crashing the launch', () async {
      final secure = InMemorySecureStore({'ah.session.v1': '{"accessToken":'});
      expect(await SessionStore(secure).read(), isNull);
    });

    test('an entry with no refresh token is not a session', () async {
      final secure = InMemorySecureStore({
        'ah.session.v1': jsonEncode({'accessToken': 'access'}),
      });
      expect(await SessionStore(secure).read(), isNull);
    });

    test('clear removes it', () async {
      final store = SessionStore(InMemorySecureStore());
      await store.write(const StoredSession(accessToken: 'a', refreshToken: 'r'));
      expect(await store.read(), isNotNull);

      await store.clear();
      expect(await store.read(), isNull);
    });

    test('never prints a token', () {
      const session = StoredSession(accessToken: 'access-secret', refreshToken: 'refresh-secret');
      expect(session.toString(), isNot(contains('secret')));
    });

    test('refreshes early enough that the token is still valid on arrival', () {
      final now = DateTime.utc(2026, 7, 29, 9);
      final almostExpired = StoredSession(
        accessToken: 'a',
        refreshToken: 'r',
        accessTokenExpiresAt: now.add(const Duration(seconds: 4)),
      );
      final comfortable = StoredSession(
        accessToken: 'a',
        refreshToken: 'r',
        accessTokenExpiresAt: now.add(const Duration(minutes: 10)),
      );

      expect(almostExpired.needsRefreshAt(now), isTrue);
      expect(comfortable.needsRefreshAt(now), isFalse);
      // No expiry means "let the 401 path decide", not "refresh on every launch".
      expect(
        const StoredSession(accessToken: 'a', refreshToken: 'r').needsRefreshAt(now),
        isFalse,
      );
    });
  });

  group('DevicePreferences', () {
    late AhDatabase database;

    setUp(() => database = openInMemoryAhDatabase());
    tearDown(() => database.close());

    test('the active city has no default, because guessing quotes the wrong state', () async {
      final preferences = DevicePreferences(database);
      expect(await preferences.activeCity(), isNull);

      await preferences.setActiveCity('pune');
      expect(await preferences.activeCity(), 'pune');

      await preferences.setActiveCity('coimbatore');
      expect(await preferences.activeCity(), 'coimbatore');
    });

    test('watching emits the change', () async {
      final preferences = DevicePreferences(database);
      // Subscribed before the write, so this asserts propagation rather than a lucky delay.
      final arrived = preferences.watchActiveCity().firstWhere((city) => city == 'pune');

      await preferences.setActiveCity('pune');

      expect(await arrived, 'pune');
    });
  });

  group('DraftStore', () {
    late AhDatabase database;
    late DraftStore drafts;

    setUp(() {
      database = openInMemoryAhDatabase();
      drafts = DraftStore(database, clock: () => DateTime.utc(2026, 7, 29, 9));
    });
    tearDown(() => database.close());

    test('recovers a half-finished sell form after the process died', () async {
      await drafts.record(
        formKey: 'sell_vehicle',
        values: const {'registration_number': 'MH12AB1234', 'fuel_type': 'ev'},
        schemaVersion: 7,
      );

      // A different store over the same file — the relaunch.
      final recovered = await DraftStore(database).recover(formKey: 'sell_vehicle');

      expect(recovered!.values['registration_number'], 'MH12AB1234');
      expect(recovered.schemaVersion, 7);
      expect(recovered.entityId, isNull);
    });

    test('two new drafts of one form are two drafts, not one NULL row', () async {
      await drafts.record(formKey: 'sell_vehicle', values: const {'a': 1});
      await drafts.record(formKey: 'sell_vehicle', entityId: '019fab0c', values: const {'a': 2});

      expect((await drafts.recover(formKey: 'sell_vehicle'))!.values['a'], 1);
      expect(
        (await drafts.recover(formKey: 'sell_vehicle', entityId: '019fab0c'))!.values['a'],
        2,
      );
      expect(await drafts.all(), hasLength(2));
    });

    test('keeps values for currently hidden fields', () async {
      // `battery_health_pct` is hidden the moment fuel_type stops being `ev`. Dropping it here
      // would erase what the user already typed the moment they tapped the wrong chip.
      await drafts.record(
        formKey: 'sell_vehicle',
        values: const {'fuel_type': 'petrol', 'battery_health_pct': 88},
      );
      final recovered = await drafts.recover(formKey: 'sell_vehicle');
      expect(recovered!.values['battery_health_pct'], 88);
    });

    test('knows when it was composed against an older form version', () async {
      await drafts.record(formKey: 'sell_vehicle', values: const {}, schemaVersion: 7);
      final draft = (await drafts.recover(formKey: 'sell_vehicle'))!;

      expect(draft.isStaleAgainst(9), isTrue);
      expect(draft.isStaleAgainst(7), isFalse);
    });

    test('discard removes only the draft asked for', () async {
      await drafts.record(formKey: 'sell_vehicle', values: const {'a': 1});
      await drafts.record(formKey: 'inspection_checklist', values: const {'b': 2});

      await drafts.discard(formKey: 'sell_vehicle');

      expect(await drafts.recover(formKey: 'sell_vehicle'), isNull);
      expect(await drafts.recover(formKey: 'inspection_checklist'), isNotNull);
    });

    test('an unreadable draft is an empty form, not a crash on the supply screen', () async {
      await database.into(database.draftRows).insert(
            DraftRowsCompanion.insert(
              formKey: 'sell_vehicle',
              entityId: '',
              values: '{"registration_number":',
              updatedAt: DateTime.utc(2026, 7, 29),
            ),
          );

      final recovered = await drafts.recover(formKey: 'sell_vehicle');
      expect(recovered!.values, isEmpty);
    });

    test('purges drafts nobody came back to', () async {
      await DraftStore(database, clock: () => DateTime.utc(2026, 5)).record(
        formKey: 'sell_vehicle',
        values: const {'a': 1},
      );
      await drafts.record(formKey: 'inspection_checklist', values: const {'b': 2});

      final removed = await drafts.purgeOlderThan(const Duration(days: 30));

      expect(removed, 1);
      expect(await drafts.recover(formKey: 'inspection_checklist'), isNotNull);
    });
  });

  group('DraftAutosave', () {
    late AhDatabase database;
    late DraftStore drafts;

    setUp(() {
      database = openInMemoryAhDatabase();
      drafts = DraftStore(database);
    });
    tearDown(() => database.close());

    test('coalesces a burst of keystrokes into the last one', () async {
      final autosave = DraftAutosave(drafts, window: const Duration(seconds: 30));

      for (final value in ['M', 'MH', 'MH1', 'MH12']) {
        autosave.record(
          FormDraft(
            formKey: 'sell_vehicle',
            values: {'registration_number': value},
            updatedAt: DateTime.utc(2026, 7, 29),
          ),
        );
      }

      // Nothing has hit SQLite yet — the window has not elapsed, which is the point on a handset
      // where a journal flush per keystroke is felt as keyboard lag.
      expect(await drafts.recover(formKey: 'sell_vehicle'), isNull);

      // What the app does on AppLifecycleState.paused, because Android can kill the process
      // without ever letting the timer fire.
      await autosave.flush();

      final saved = await drafts.recover(formKey: 'sell_vehicle');
      expect(saved!.values['registration_number'], 'MH12');
      await autosave.dispose();
    });

    test('dispose flushes rather than dropping what was pending', () async {
      final autosave = DraftAutosave(drafts, window: const Duration(seconds: 30))
        ..record(
          FormDraft(
            formKey: 'sell_vehicle',
            values: const {'kms_driven': 38400},
            updatedAt: DateTime.utc(2026, 7, 29),
          ),
        );

      await autosave.dispose();

      expect((await drafts.recover(formKey: 'sell_vehicle'))!.values['kms_driven'], 38400);
    });
  });
}

/// The provider graph is only worth having if a test can stand the whole thing up with two fakes
/// at the leaves. If wiring it needs more than that, the seams are in the wrong places.
void _providerGroup() {
  group('providers', () {
    late AhDatabase database;
    late ProviderContainer container;

    setUp(() {
      database = openInMemoryAhDatabase();
      container = ProviderContainer(
        overrides: [
          ahDatabaseProvider.overrideWithValue(database),
          secureStoreProvider.overrideWithValue(InMemorySecureStore()),
          registryTransportProvider.overrideWithValue(_NeverTransport()),
          outboxTransportProvider.overrideWithValue(_NeverOutboxTransport()),
        ],
      );
    });

    tearDown(() async {
      container.dispose();
      await database.close();
    });

    test('the whole graph builds from two overridden transports', () {
      expect(container.read(registryClientProvider), isA<RegistryClient>());
      expect(container.read(outboxProvider), isA<Outbox>());
      expect(container.read(outboxDrainerProvider), isA<OutboxDrainer>());
      expect(container.read(immediateWriterProvider), isA<ImmediateWriter>());
      expect(container.read(draftStoreProvider), isA<DraftStore>());
      expect(container.read(sessionStoreProvider), isA<SessionStore>());
      expect(
        container.read(draftAutosaveProvider((formKey: 'sell_vehicle', entityId: null))),
        isA<DraftAutosave>(),
      );
    });

    test('two open forms get two debouncers, not one overwriting the other', () {
      final sell = container.read(draftAutosaveProvider((formKey: 'sell', entityId: null)));
      final onboarding = container.read(
        draftAutosaveProvider((formKey: 'dealer_onboarding', entityId: null)),
      );
      expect(identical(sell, onboarding), isFalse);
    });

    test('the drainer does not start sending as a side effect of being read', () async {
      final transport = _NeverOutboxTransport();
      final scoped = ProviderContainer(
        overrides: [
          ahDatabaseProvider.overrideWithValue(database),
          secureStoreProvider.overrideWithValue(InMemorySecureStore()),
          outboxTransportProvider.overrideWithValue(transport),
        ],
      );
      await scoped.read(outboxProvider).enqueue(
            WriteIntent.now(endpoint: '/listings', method: WriteMethod.post),
          );

      scoped.read(outboxDrainerProvider);
      await Future<void>.delayed(Duration.zero);

      expect(
        transport.calls,
        0,
        reason: 'reading a provider during a build is not consent to send the queue',
      );
      scoped.dispose();
    });

    test('an un-overridden transport says exactly what is missing', () {
      final bare = ProviderContainer(
        overrides: [ahDatabaseProvider.overrideWithValue(database)],
      );
      expect(
        () => bare.read(registryTransportProvider),
        throwsA(
          isA<UnimplementedError>().having(
            (error) => error.message,
            'message',
            contains('ProviderScope'),
          ),
        ),
      );
      bare.dispose();
    });
  });
}

final class _NeverTransport implements RegistryTransport {
  @override
  Future<RegistryFetch> fetch(RegistryKey key, {String? ifNoneMatch}) =>
      Future<RegistryFetch>.error(StateError('the test never expected a fetch'));
}

final class _NeverOutboxTransport implements OutboxTransport {
  int calls = 0;

  @override
  Future<WriteOutcome> send(WriteIntent intent) async {
    calls += 1;
    return const WriteUnreachable(reason: 'the test never expected a send');
  }
}

/// Counts reads, so "is it memoised?" is a fact rather than a hope.
final class _CountingSecureStore implements SecureKeyValueStore {
  final Map<String, String> _values = {};
  int reads = 0;

  @override
  Future<String?> read(String key) async {
    reads += 1;
    return _values[key];
  }

  @override
  Future<void> write(String key, String value) async => _values[key] = value;

  @override
  Future<void> delete(String key) async => _values.remove(key);

  @override
  Future<void> deleteAll() async => _values.clear();
}
