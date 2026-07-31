import 'dart:convert';

import 'package:drift/drift.dart';

import '../storage/ah_database.dart';
import 'registry_transport.dart';

/// The disk half of the registry cache.
///
/// Deliberately dumb: it persists and returns snapshots and makes no decision about freshness.
/// Every "should I refetch?" question belongs to [RegistryClient], because that is the question
/// whose answer depends on the network, and this class must remain testable without one.
final class RegistryStore {
  const RegistryStore(this._db);

  final AhDatabase _db;

  /// Returns what this device knows about [name], or null if it has never seen it.
  ///
  /// A row whose body will not parse is treated as a miss and evicted rather than thrown from.
  /// SQLite gives us atomicity per statement, not per byte of a JSON string that was truncated by a
  /// filesystem that filled up mid-write; and the cost of being wrong here is the app failing to
  /// launch, offline, with no way for the user to clear it.
  Future<RegistrySnapshot?> read(String name) async {
    final query = _db.select(_db.cachedRegistries)..where((row) => row.name.equals(name));
    final row = await query.getSingleOrNull();
    if (row == null) return null;

    final body = _decodeBody(row.body);
    if (body == null) {
      await evict(name);
      return null;
    }

    return RegistrySnapshot(
      name: row.name,
      version: row.version,
      etag: row.etag,
      body: body,
      fetchedAt: row.fetchedAt,
    );
  }

  /// Upserts a snapshot. The name is the primary key, so a newer version replaces its predecessor.
  Future<void> write(RegistrySnapshot snapshot) {
    return _db.into(_db.cachedRegistries).insertOnConflictUpdate(
          CachedRegistriesCompanion.insert(
            name: snapshot.name,
            version: snapshot.version,
            etag: Value(snapshot.etag),
            body: jsonEncode(snapshot.body),
            fetchedAt: snapshot.fetchedAt,
          ),
        );
  }

  /// Records that a `304` confirmed the stored body, without rewriting it.
  ///
  /// Separated from [write] so that revalidating a 400 KB taxonomy costs one `UPDATE` of a
  /// timestamp instead of re-serialising and re-writing the whole document on every launch.
  Future<void> markRevalidated(String name, DateTime at, {String? etag}) {
    final update = _db.update(_db.cachedRegistries)..where((row) => row.name.equals(name));
    return update.write(
      CachedRegistriesCompanion(
        fetchedAt: Value(at),
        // Only overwrite the ETag when the server re-issued one; a 304 that omits it leaves the
        // stored value standing, and writing null there would force a full body on the next launch.
        etag: etag == null ? const Value.absent() : Value(etag),
      ),
    );
  }

  Future<void> evict(String name) async {
    final delete = _db.delete(_db.cachedRegistries)..where((row) => row.name.equals(name));
    await delete.go();
  }

  /// Everything cached, newest confirmation first. Powers a diagnostics screen, not a code path.
  Future<List<RegistrySnapshot>> all() async {
    final query = _db.select(_db.cachedRegistries)
      ..orderBy([(row) => OrderingTerm.desc(row.fetchedAt)]);
    final rows = await query.get();
    return [
      for (final row in rows)
        if (_decodeBody(row.body) case final Map<String, dynamic> body)
          RegistrySnapshot(
            name: row.name,
            version: row.version,
            etag: row.etag,
            body: body,
            fetchedAt: row.fetchedAt,
          ),
    ];
  }

  /// Drops every cached document.
  ///
  /// Called on sign-out and nowhere else. Features and i18n are evaluated *for the calling user*,
  /// so leaving them behind on a shared handset shows the next person which modules the last one
  /// had access to.
  Future<int> clear() => _db.delete(_db.cachedRegistries).go();

  static Map<String, dynamic>? _decodeBody(String raw) {
    try {
      final decoded = jsonDecode(raw);
      return decoded is Map<String, dynamic> ? decoded : null;
    } on FormatException {
      return null;
    }
  }
}
