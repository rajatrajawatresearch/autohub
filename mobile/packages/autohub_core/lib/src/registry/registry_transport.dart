import 'package:flutter/foundation.dart';

import 'registry_key.dart';

/// A registry document as it exists on this device, with the version that produced it.
@immutable
final class RegistrySnapshot {
  const RegistrySnapshot({
    required this.name,
    required this.version,
    required this.etag,
    required this.body,
    required this.fetchedAt,
  });

  /// The cache key — [RegistryKey.name].
  final String name;

  /// The server's `X-Registry-Version` for this body.
  final int version;

  /// The `ETag` verbatim, for the next `If-None-Match`.
  final String? etag;

  /// The decoded response body.
  final Map<String, dynamic> body;

  /// When the server last confirmed this body current, whether by `200` or by `304`.
  final DateTime fetchedAt;

  RegistrySnapshot revalidatedAt(DateTime at) => RegistrySnapshot(
        name: name,
        version: version,
        etag: etag,
        body: body,
        fetchedAt: at,
      );

  @override
  String toString() => 'RegistrySnapshot($name v$version @ ${fetchedAt.toIso8601String()})';
}

/// What a conditional GET of a registry document came back as.
///
/// Sealed, so the two outcomes are handled exhaustively at every call site. A `304` silently
/// treated as "no data" is the bug that makes an app blank its own taxonomy on a good network.
sealed class RegistryFetch {
  const RegistryFetch();
}

/// `200` — a new body, and the version that produced it.
@immutable
final class RegistryFresh extends RegistryFetch {
  const RegistryFresh({required this.version, required this.etag, required this.body});

  final int version;
  final String? etag;
  final Map<String, dynamic> body;
}

/// `304` — what is on disk is still what the server would send.
///
/// This is the response the whole design optimises for. A 304 is a couple of hundred bytes, which
/// is why revalidation can happen on every cold launch without costing a user on a metered
/// connection anything they would notice.
@immutable
final class RegistryNotModified extends RegistryFetch {
  const RegistryNotModified({this.etag});

  /// Servers may re-issue the ETag on a 304. When they do not, the cached one still stands.
  final String? etag;
}

/// The seam between the registry cache and whatever is doing HTTP.
///
/// Declared here, implemented in `net/`. The cache must be testable without dio and without a
/// socket — the interesting behaviour in this package is what happens when the network is *not*
/// there, and a test that needs a real transport to prove that is a test that will be deleted.
abstract interface class RegistryTransport {
  /// Issues a conditional GET.
  ///
  /// Implementations must send [ifNoneMatch] as `If-None-Match` when it is non-null, and must throw
  /// rather than invent an empty body when the network is unreachable — [RegistryClient] treats a
  /// throw as "keep what we have", and a fabricated empty body as "the taxonomy is now empty".
  Future<RegistryFetch> fetch(RegistryKey key, {String? ifNoneMatch});
}

/// Raised when a document is needed, is not on disk, and cannot be fetched.
///
/// This is the *only* registry failure a screen ever has to handle, because every other path is
/// served from the last known snapshot. It means "this device has never seen this document and
/// cannot see it now" — first launch in an airport basement — and the right UI is a retry, not an
/// error page.
final class RegistryUnavailableException implements Exception {
  const RegistryUnavailableException(this.name, this.reason, [this.cause]);

  final String name;
  final String reason;
  final Object? cause;

  @override
  String toString() => 'RegistryUnavailableException($name): $reason';
}
