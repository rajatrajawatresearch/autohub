import 'dart:async';

import 'registry_key.dart';
import 'registry_store.dart';
import 'registry_transport.dart';

/// Turns a registry body into a model. Matches the signature `json_serializable` generates, so a
/// generated `fromJson` is passed as a tear-off with no adapter.
typedef RegistryDecoder<T> = T Function(Map<String, Object?> json);

/// Why a background revalidation did not happen. Surfaced for logging, never for the user.
typedef RegistryFailureListener = void Function(RegistryKey key, Object error);

/// The registry cache: disk first, network second, never the other way round.
///
/// ## Why version-keyed and not time-keyed
///
/// The obvious design is a TTL — cache the taxonomy for an hour, refetch when it expires. It is
/// wrong here in both directions at once, because a TTL is a guess about how often somebody in
/// operations edits a registry, and that rate is neither knowable nor uniform.
///
/// Set it short and the app revalidates a 400 KB taxonomy that changes twice a year, on every
/// launch, on a metered connection, for nothing. Set it long and the promise the whole platform is
/// built on breaks: an operations user adds `battery_health_pct` to the EV category and the field
/// does not appear on a handset until an arbitrary timer that nobody can see runs out. Neither
/// number is defensible, and the one that gets picked is always the one that was least annoying in
/// the office.
///
/// `ETag` plus `X-Registry-Version` removes the guess. Revalidation is one conditional GET whose
/// unchanged answer is a couple of hundred bytes — cheap enough to do on *every* cold launch — and
/// its changed answer is exact rather than probable. Freshness stops being a policy the client
/// invents and becomes a fact the server states.
///
/// ## Why the cache is served before the network is consulted
///
/// A screen that waits for the network to confirm data it already has is a screen that is blank in
/// a lift. Every read here returns the last known snapshot synchronously-as-possible and lets the
/// revalidation land later; §4 of `docs/10-mobile-architecture.md` makes this the requirement, not
/// an optimisation. The consequence a caller must accept is that a body may be one version old for
/// a few hundred milliseconds after launch — which is true of the web too, and is why the server,
/// not the client, is the authority on whether a submitted payload is valid.
final class RegistryClient {
  RegistryClient({
    required RegistryStore store,
    required RegistryTransport transport,
    DateTime Function()? clock,
    this.onRevalidationFailure,
  })  : _store = store,
        _transport = transport,
        _clock = clock ?? _wallClock;

  static DateTime _wallClock() => DateTime.now().toUtc();

  final RegistryStore _store;
  final RegistryTransport _transport;
  final DateTime Function() _clock;

  /// Hook for the app's logger. Revalidation failures are never user-facing — the screen is already
  /// rendering the cached copy — but they are exactly the signal that says an ETag is being sent
  /// wrong and every launch is doing a full download.
  final RegistryFailureListener? onRevalidationFailure;

  /// Parsed bodies, so that a widget that reads the taxonomy on every rebuild does not re-read
  /// SQLite and re-run `jsonDecode` on 400 KB sixty times a second.
  ///
  /// Unbounded on purpose: the working set is the registries one session touches, each a few KB
  /// beyond the single large taxonomy tree. A bounded LRU here would be machinery guarding against
  /// a growth pattern this data does not have.
  final Map<String, RegistrySnapshot> _memo = {};

  /// One in-flight fetch per document. Six widgets asking for the sell form during the same frame
  /// must produce one request, not six — and on a train, six timeouts instead of one.
  final Map<String, Future<RegistrySnapshot?>> _inFlight = {};

  final StreamController<RegistryKey> _changes = StreamController<RegistryKey>.broadcast();

  bool _disposed = false;

  /// Fires when a revalidation replaced a body with a newer version.
  ///
  /// Nothing is required to listen. A screen that ignores this shows the previous version until it
  /// is rebuilt for some other reason, which is correct — swapping a form's fields underneath a
  /// user who is halfway through filling it in is worse than being one version behind.
  Stream<RegistryKey> get changes => _changes.stream;

  /// What is on disk, or null. Never touches the network.
  ///
  /// This is the call a splash screen makes to decide whether it can render at all.
  Future<RegistrySnapshot?> peek(RegistryKey key) async {
    final memoised = _memo[key.name];
    if (memoised != null) return memoised;

    final stored = await _store.read(key.name);
    if (stored != null) _memo[key.name] = stored;
    return stored;
  }

  /// The document, from disk if this device has ever seen it.
  ///
  /// Revalidation is kicked off but not awaited, so a cold launch with no network still returns the
  /// last known snapshot. Throws [RegistryUnavailableException] only in the one case that cannot be
  /// served: never fetched, and unreachable now.
  Future<RegistrySnapshot> read(RegistryKey key) async {
    final cached = await peek(key);
    if (cached != null) {
      unawaited(_revalidate(key, cached));
      return cached;
    }

    final fetched = await _revalidate(key, null);
    if (fetched == null) {
      throw RegistryUnavailableException(
        key.name,
        'never cached on this device and the network did not answer',
        _lastFailure[key.name],
      );
    }
    return fetched;
  }

  /// [read], decoded.
  ///
  /// A body that will not decode is evicted before the error is rethrown. That body came from an
  /// older contract and this build cannot read it, so keeping it means the screen stays broken
  /// every launch forever; dropping it means the next launch with a network fixes itself.
  Future<T> readAs<T>(RegistryKey key, RegistryDecoder<T> decode) async {
    final snapshot = await read(key);
    try {
      return decode(snapshot.body);
    } on Object catch (error) {
      _memo.remove(key.name);
      await _store.evict(key.name);
      throw RegistryUnavailableException(
        key.name,
        'cached body v${snapshot.version} does not decode against this build; evicted',
        error,
      );
    }
  }

  /// Forces a conditional GET and waits for it. Pull-to-refresh, and nothing else — a screen that
  /// calls this on build has reinvented the network-first cache this class exists to avoid.
  Future<RegistrySnapshot?> refresh(RegistryKey key) async {
    final cached = await peek(key);
    return _revalidate(key, cached);
  }

  /// Fetches the documents the shell cannot paint without, in parallel, failing quietly.
  ///
  /// Failing quietly is the point: this runs at launch, and a launch that blocks on the network is
  /// a launch that hangs in a basement. Whatever lands, lands; whatever does not is served from the
  /// previous session's cache by [read].
  Future<void> warm(Iterable<RegistryKey> keys) async {
    await Future.wait([
      for (final key in keys) _revalidate(key, await peek(key)),
    ]);
  }

  /// Awaits every background revalidation currently in flight.
  ///
  /// Exists for two callers: a test that needs the revalidation to have landed before asserting,
  /// and a "sync now" affordance on a settings screen. Application code on a render path must not
  /// call it — waiting for revalidation is exactly what [read] refuses to do.
  Future<void> settle() async {
    // A revalidation can be scheduled while an earlier one is being awaited, so drain rather than
    // snapshot once.
    while (_inFlight.isNotEmpty) {
      await Future.wait(_inFlight.values.toList());
    }
  }

  /// The last error that stopped [key] revalidating, if any. Diagnostics only.
  Object? lastFailureFor(RegistryKey key) => _lastFailure[key.name];

  final Map<String, Object> _lastFailure = {};

  Future<void> dispose() async {
    _disposed = true;
    await settle();
    await _changes.close();
    _memo.clear();
  }

  Future<RegistrySnapshot?> _revalidate(RegistryKey key, RegistrySnapshot? cached) {
    final existing = _inFlight[key.name];
    if (existing != null) return existing;

    // A block body, not an arrow. `Map.remove` returns the value it removed — which here is this
    // very future — and `whenComplete` waits on any Future its callback returns, so the arrow form
    // makes the future wait for itself and hangs forever.
    final future = _fetch(key, cached).whenComplete(() {
      _inFlight.remove(key.name);
    });
    _inFlight[key.name] = future;
    return future;
  }

  Future<RegistrySnapshot?> _fetch(RegistryKey key, RegistrySnapshot? cached) async {
    try {
      final result = await _transport.fetch(key, ifNoneMatch: cached?.etag);

      switch (result) {
        case RegistryFresh(:final version, :final etag, :final body):
          final next = RegistrySnapshot(
            name: key.name,
            version: version,
            etag: etag,
            body: body,
            fetchedAt: _clock(),
          );
          await _store.write(next);
          _memo[key.name] = next;
          _lastFailure.remove(key.name);
          if (cached == null || cached.version != version) _announce(key);
          return next;

        case RegistryNotModified(:final etag):
          if (cached == null) {
            // We sent no `If-None-Match`, so a 304 here is the server answering a question we did
            // not ask. Treated as a failure rather than as an empty document, because the
            // alternative is caching nothing forever and refetching on every single read.
            _lastFailure[key.name] = StateError(
              '304 for ${key.name} with no cached copy and no If-None-Match sent',
            );
            _notifyFailure(key);
            return null;
          }
          final touched = cached.revalidatedAt(_clock());
          await _store.markRevalidated(key.name, touched.fetchedAt, etag: etag);
          _memo[key.name] = touched;
          _lastFailure.remove(key.name);
          return touched;
      }
    } on Object catch (error) {
      // Everything is swallowed, including programmer errors, because this future is detached: an
      // escaping error here takes down the zone during a background refresh of a screen that is
      // rendering correctly from cache. It is recorded and handed to [onRevalidationFailure]
      // instead, which is where an app that wants a crash report should raise one.
      _lastFailure[key.name] = error;
      _notifyFailure(key);
      return null;
    }
  }

  void _announce(RegistryKey key) {
    if (_disposed || _changes.isClosed) return;
    _changes.add(key);
  }

  void _notifyFailure(RegistryKey key) {
    final error = _lastFailure[key.name];
    if (error != null) onRevalidationFailure?.call(key, error);
  }
}
