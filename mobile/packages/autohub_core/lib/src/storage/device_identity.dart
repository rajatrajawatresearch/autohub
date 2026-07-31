import 'package:uuid/uuid.dart';

import 'secure_store.dart';

/// The `X-Device-Id` this install sends on every request.
///
/// Two jobs, per `contracts/registry-api.md` §12: it binds refresh tokens, so a token lifted from
/// one handset does not work from another, and it feeds fraud signals — one device placing bids
/// from four accounts is a pattern worth seeing.
///
/// ## Why it is generated, not read from the hardware
///
/// Neither platform will give an app a stable hardware identifier any more, and the ones that
/// remain should not be used even where they exist. Android's `ANDROID_ID` is already
/// per-app-signing-key and resets on factory reset; iOS's `identifierForVendor` resets when the
/// last app from a vendor is removed. Both are *less* stable than a UUID this app persists itself,
/// and both are privacy-sensitive in ways a random value is not: a generated id identifies this
/// install of this app and nothing else about the person holding it.
///
/// ## Why the keychain
///
/// It is stored beside the session because it is half of the session's binding — a device id that
/// resets while the refresh token survives produces a token the server rejects, and the user is
/// signed out for no reason they can see. On iOS this means the id survives an uninstall, which is
/// the correct behaviour here: reinstalling the app is not becoming a new device, and treating it
/// as one would make "delete and reinstall" a way to shed a fraud history.
final class DeviceIdentity {
  DeviceIdentity(this._secure, {Uuid uuid = const Uuid()}) : _uuid = uuid;

  static const String _key = 'ah.device_id.v1';

  final SecureKeyValueStore _secure;
  final Uuid _uuid;

  String? _cached;

  /// The device id, generating and persisting one on first call.
  ///
  /// Memoised because it is read on every single request. A keychain read is a platform channel
  /// hop, and doing one per HTTP call would put an asynchronous boundary in front of every header
  /// this app builds.
  Future<String> ensure() async {
    final memo = _cached;
    if (memo != null) return memo;

    final existing = await _secure.read(_key);
    if (existing != null && existing.isNotEmpty) {
      _cached = existing;
      return existing;
    }

    final created = _uuid.v4();
    await _secure.write(_key, created);
    _cached = created;
    return created;
  }

  /// The memoised id, or null before [ensure] has completed once.
  ///
  /// For the synchronous header-building path. A caller that gets null here must fall back to
  /// awaiting [ensure] rather than sending a request without the header — an unbound refresh is a
  /// refresh the server cannot tie to anything.
  String? get cached => _cached;

  /// Forgets the id. Sign-out does **not** call this: the whole point of the id is that it outlives
  /// sessions. Reserved for an explicit "reset this device" support action.
  Future<void> reset() async {
    _cached = null;
    await _secure.delete(_key);
  }
}
