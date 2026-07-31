import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../offline/backoff.dart';
import '../offline/connectivity.dart';
import '../offline/draft_store.dart';
import '../offline/immediate_writer.dart';
import '../offline/outbox.dart';
import '../offline/outbox_drainer.dart';
import '../offline/write_outcome.dart';
import '../registry/registry_client.dart';
import '../registry/registry_store.dart';
import '../registry/registry_transport.dart';
import '../storage/ah_database.dart';
import '../storage/device_identity.dart';
import '../storage/device_preferences.dart';
import '../storage/open_database.dart';
import '../storage/secure_store.dart';
import '../storage/session_store.dart';

/// The dependency graph for everything in this package.
///
/// Two conventions run through it and both exist for the same reason — that the parts of this
/// package worth testing are the parts that misbehave when the network does:
///
/// 1. **Everything is overridable, and the leaves are overridden in tests.** A test swaps
///    [ahDatabaseProvider] for an in-memory database and [registryTransportProvider] for a fake
///    that refuses to connect, and gets the real cache, the real store and the real client.
/// 2. **The two transports throw until they are overridden.** They are implemented in `net/`, which
///    depends on this package rather than the other way round. A provider that threw at *runtime*
///    in production would be a bug; one that throws the first time the app is run without its
///    composition root wired is a five-second fix with a message that says exactly what to do.

/* ── Storage ──────────────────────────────────────────────────────────────── */

/// The SQLite database. Opened lazily on first query — see `ahDatabaseFile`.
final Provider<AhDatabase> ahDatabaseProvider = Provider<AhDatabase>((ref) {
  final database = openAhDatabase();
  ref.onDispose(() => unawaited(database.close()));
  return database;
});

/// Keychain-backed secret storage. Overridden with `InMemorySecureStore` in tests.
final Provider<SecureKeyValueStore> secureStoreProvider =
    Provider<SecureKeyValueStore>((ref) => const KeychainStore());

final Provider<SessionStore> sessionStoreProvider =
    Provider<SessionStore>((ref) => SessionStore(ref.watch(secureStoreProvider)));

final Provider<DeviceIdentity> deviceIdentityProvider =
    Provider<DeviceIdentity>((ref) => DeviceIdentity(ref.watch(secureStoreProvider)));

/// The `X-Device-Id`, generated on first launch.
///
/// A `FutureProvider` because the first read touches the keychain. Everything after that is
/// memoised inside [DeviceIdentity], so the header path is synchronous once the app has started.
final FutureProvider<String> deviceIdProvider =
    FutureProvider<String>((ref) => ref.watch(deviceIdentityProvider).ensure());

final Provider<DevicePreferences> devicePreferencesProvider =
    Provider<DevicePreferences>((ref) => DevicePreferences(ref.watch(ahDatabaseProvider)));

/// The city code for `X-Active-City`, live.
///
/// Null until the user picks one. Nothing here defaults it — see `DevicePreferences.activeCity`.
final StreamProvider<String?> activeCityProvider =
    StreamProvider<String?>((ref) => ref.watch(devicePreferencesProvider).watchActiveCity());

/* ── Registry cache ───────────────────────────────────────────────────────── */

final Provider<RegistryStore> registryStoreProvider =
    Provider<RegistryStore>((ref) => RegistryStore(ref.watch(ahDatabaseProvider)));

/// Implemented in `net/` over dio. **Must be overridden at the composition root.**
final Provider<RegistryTransport> registryTransportProvider = Provider<RegistryTransport>(
  (ref) => throw UnimplementedError(
    'registryTransportProvider was not overridden. The app shell must supply the dio-backed '
    'RegistryTransport from net/ in its ProviderScope overrides.',
  ),
);

final Provider<RegistryClient> registryClientProvider = Provider<RegistryClient>((ref) {
  final client = RegistryClient(
    store: ref.watch(registryStoreProvider),
    transport: ref.watch(registryTransportProvider),
  );
  ref.onDispose(() => unawaited(client.dispose()));
  return client;
});

/* ── Offline queue ────────────────────────────────────────────────────────── */

final Provider<Outbox> outboxProvider =
    Provider<Outbox>((ref) => Outbox(ref.watch(ahDatabaseProvider)));

/// Implemented in `net/` over dio. **Must be overridden at the composition root.**
///
/// Implementations must send `intent.idempotencyKey` verbatim and must not retry internally.
final Provider<OutboxTransport> outboxTransportProvider = Provider<OutboxTransport>(
  (ref) => throw UnimplementedError(
    'outboxTransportProvider was not overridden. The app shell must supply the dio-backed '
    'OutboxTransport from net/ in its ProviderScope overrides.',
  ),
);

/// Reachability, inferred from request outcomes.
///
/// Exposed as [ConnectivityMonitor] so an app that adds a platform connectivity plugin can override
/// it without anything downstream changing.
final Provider<ConnectivityMonitor> connectivityProvider = Provider<ConnectivityMonitor>((ref) {
  final monitor = OutcomeDrivenConnectivity();
  ref.onDispose(() => unawaited(monitor.dispose()));
  return monitor;
});

final Provider<BackoffPolicy> backoffPolicyProvider =
    Provider<BackoffPolicy>((ref) => const BackoffPolicy());

/// The drain loop.
///
/// Deliberately does **not** start itself. Reading a provider is not consent to begin making
/// network calls, and a widget that happens to `watch` this during a build would otherwise start
/// sending the user's queued writes as a side effect of rendering. The app shell calls `start()`
/// once, after auth is resolved.
final Provider<OutboxDrainer> outboxDrainerProvider = Provider<OutboxDrainer>((ref) {
  final drainer = OutboxDrainer(
    outbox: ref.watch(outboxProvider),
    transport: ref.watch(outboxTransportProvider),
    connectivity: ref.watch(connectivityProvider),
    backoff: ref.watch(backoffPolicyProvider),
  );
  ref.onDispose(() => unawaited(drainer.dispose()));
  return drainer;
});

/// The non-queued write path. Bids, and the few other writes that expire faster than a queue.
final Provider<ImmediateWriter> immediateWriterProvider = Provider<ImmediateWriter>((ref) {
  final reachability = switch (ref.watch(connectivityProvider)) {
    final ReachabilityReporter reporter => reporter,
    _ => null,
  };
  return ImmediateWriter(ref.watch(outboxTransportProvider), reachability: reachability);
});

/* ── Drafts ───────────────────────────────────────────────────────────────── */

final Provider<DraftStore> draftStoreProvider =
    Provider<DraftStore>((ref) => DraftStore(ref.watch(ahDatabaseProvider)));

/// Identifies one open form. A record so the family key gets structural equality for free.
typedef DraftRef = ({String formKey, String? entityId});

/// A debouncer scoped to one open form.
///
/// A family rather than a singleton because [DraftAutosave] holds the pending state of exactly one
/// form: a dealer with a listing and an onboarding form both open would otherwise have one
/// debouncer writing each form's values over the other's row.
///
/// `autoDispose` flushes on the way out — closing the form writes whatever the debounce window was
/// still holding.
final AutoDisposeProviderFamily<DraftAutosave, DraftRef> draftAutosaveProvider =
    Provider.autoDispose.family<DraftAutosave, DraftRef>((ref, key) {
  final autosave = DraftAutosave(ref.watch(draftStoreProvider));
  ref.onDispose(() => unawaited(autosave.dispose()));
  return autosave;
});
