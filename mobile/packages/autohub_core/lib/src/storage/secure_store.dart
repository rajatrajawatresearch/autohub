import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// A tiny key-value store for the handful of strings that must not be readable by anything else on
/// the device.
///
/// An interface rather than the plugin directly for one practical reason: `flutter_secure_storage`
/// is a platform channel, and a platform channel cannot answer in a `flutter test` without a mock
/// harness. The auth and device-identity logic worth testing is all on this side of the boundary.
abstract interface class SecureKeyValueStore {
  Future<String?> read(String key);
  Future<void> write(String key, String value);
  Future<void> delete(String key);
  Future<void> deleteAll();
}

/// The real one: iOS Keychain and Android EncryptedSharedPreferences.
///
/// ## Why not `shared_preferences`
///
/// `shared_preferences` is a plaintext plist on iOS and a plaintext XML file on Android. Both sit
/// inside the app sandbox, which is adequate against another app on an unmodified device and
/// adequate against nothing else:
///
/// - **A rooted or jailbroken handset reads it trivially**, and this market has a lot of them —
///   including phones the user did not root themselves, bought second-hand with an unlocked
///   bootloader.
/// - **It goes into device backups.** An unencrypted iCloud or Android auto-backup carries a
///   30-day refresh token off the device entirely, and restoring that backup onto a *different*
///   handset hands someone a live session. The Keychain entry does not travel that way, and the
///   Android keystore key that decrypts the shared preferences is hardware-bound.
/// - **ADB backup and file-manager access on older Android** read the app sandbox directly.
///
/// The token this protects is a 30-day rotating refresh token bound to a device id. Whoever holds
/// it can act as the user — list their car, accept an offer on it, place a bid in their name —
/// until it is detected and the family is revoked. That is not a class of secret that belongs in
/// an XML file.
final class KeychainStore implements SecureKeyValueStore {
  const KeychainStore([FlutterSecureStorage? storage])
      : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;

  /// `encryptedSharedPreferences` moves Android off the legacy plaintext fallback and onto a
  /// keystore-backed file. It is off by default in the plugin for backwards compatibility; there is
  /// nothing here to be backwards compatible with.
  static const AndroidOptions _android = AndroidOptions(encryptedSharedPreferences: true);

  /// `first_unlock` rather than `unlocked`: the outbox drains in the background, and a write queued
  /// on a train must be able to reach for its token while the phone is in a pocket. It still
  /// requires the device to have been unlocked at least once since boot, so a lost handset that has
  /// not been opened yields nothing.
  ///
  /// Not `..._this_device_only`, deliberately paired with the device id: a restored backup on a new
  /// handset presents a token bound to the *old* device id, which the server rejects and treats as
  /// reuse — a signal worth having, rather than a silent failure to restore.
  static const IOSOptions _ios = IOSOptions(accessibility: KeychainAccessibility.first_unlock);

  @override
  Future<String?> read(String key) =>
      _storage.read(key: key, aOptions: _android, iOptions: _ios);

  @override
  Future<void> write(String key, String value) =>
      _storage.write(key: key, value: value, aOptions: _android, iOptions: _ios);

  @override
  Future<void> delete(String key) =>
      _storage.delete(key: key, aOptions: _android, iOptions: _ios);

  @override
  Future<void> deleteAll() => _storage.deleteAll(aOptions: _android, iOptions: _ios);
}

/// A process-lifetime store, for tests and for the seam where a platform has no keychain.
///
/// Named for what it is. Anything that reaches for this in production has moved a refresh token
/// into RAM that dies with the process, which is a session that silently ends every cold launch.
final class InMemorySecureStore implements SecureKeyValueStore {
  InMemorySecureStore([Map<String, String>? seed]) : _values = {...?seed};

  final Map<String, String> _values;

  @override
  Future<String?> read(String key) async => _values[key];

  @override
  Future<void> write(String key, String value) async => _values[key] = value;

  @override
  Future<void> delete(String key) async => _values.remove(key);

  @override
  Future<void> deleteAll() async => _values.clear();
}
