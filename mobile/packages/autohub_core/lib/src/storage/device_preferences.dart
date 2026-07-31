import 'ah_database.dart';

/// Device-scoped settings that are not secrets.
///
/// In SQLite rather than the keychain because a keychain read is a platform channel hop and these
/// are read on the render path — `X-Active-City` scopes search, pricing and feature evaluation, so
/// it is attached to nearly every request. Putting a non-secret behind a hardware-backed decrypt is
/// paying for a guarantee nothing here needs.
final class DevicePreferences {
  DevicePreferences(this._db, {DateTime Function()? clock}) : _clock = clock ?? _wallClock;

  static DateTime _wallClock() => DateTime.now().toUtc();

  /// `contracts/registry-api.md` §12 — scopes search, pricing and feature evaluation.
  static const String activeCityKey = 'active_city';

  /// `Accept-Language`. The value is a locale code from the i18n registry, never an enum here: the
  /// set of locales AutoHub serves is a registry decision, and a handset that hardcoded eight of
  /// them cannot render the ninth.
  static const String localeKey = 'locale';

  final AhDatabase _db;
  final DateTime Function() _clock;

  Future<String?> read(String key) async {
    final query = _db.select(_db.preferenceRows)..where((row) => row.key.equals(key));
    final row = await query.getSingleOrNull();
    return row?.value;
  }

  Future<void> write(String key, String value) {
    return _db.into(_db.preferenceRows).insertOnConflictUpdate(
          PreferenceRowsCompanion.insert(key: key, value: value, updatedAt: _clock()),
        );
  }

  Future<void> remove(String key) async {
    final delete = _db.delete(_db.preferenceRows)..where((row) => row.key.equals(key));
    await delete.go();
  }

  Stream<String?> watch(String key) {
    final query = _db.select(_db.preferenceRows)..where((row) => row.key.equals(key));
    return query.watchSingleOrNull().map((row) => row?.value);
  }

  /// The city code the user is browsing, or null before they have chosen.
  ///
  /// Null is a real state and not a defaulted one. Guessing Mumbai for a user in Coimbatore shows
  /// them a marketplace with nothing they can buy and quotes them fees from another state's rule
  /// set; the city picker exists because the answer must come from the user or from the geography
  /// registry, not from a constant in this file.
  Future<String?> activeCity() => read(activeCityKey);

  Future<void> setActiveCity(String cityCode) => write(activeCityKey, cityCode);

  Stream<String?> watchActiveCity() => watch(activeCityKey);

  Future<String?> locale() => read(localeKey);

  Future<void> setLocale(String locale) => write(localeKey, locale);
}
