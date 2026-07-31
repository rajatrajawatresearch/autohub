import 'package:autohub_core/src/net/api_client.dart';
import 'package:autohub_core/src/net/api_exception.dart';
import 'package:autohub_core/src/registry/registry_key.dart';
import 'package:autohub_core/src/registry/registry_transport.dart';

/// The `RegistryTransport` the cache in `registry/` declared and refused to implement itself.
///
/// It is deliberately thin. Everything interesting about registry caching — what to serve when the
/// network is gone, when to revalidate, what a version bump invalidates — belongs to
/// `RegistryClient`, which is testable without a socket precisely because this class exists.
final class DioRegistryTransport implements RegistryTransport {
  const DioRegistryTransport(this._client);

  final ApiClient _client;

  @override
  Future<RegistryFetch> fetch(RegistryKey key, {String? ifNoneMatch}) async {
    final response = await _client.getConditional(
      key.path,
      ifNoneMatch: ifNoneMatch,
      query: _queryOf(key),
    );

    if (response.isNotModified) {
      // Servers may or may not re-issue the ETag on a 304. Passing whatever came back lets the
      // cache refresh its validator when the server offers one and keep the stored one when it
      // does not — dropping it would turn every subsequent revalidation into a full fetch.
      return RegistryNotModified(etag: response.etag ?? ifNoneMatch);
    }

    final body = response.value;
    if (body == null) {
      // A `200` with no object body is not "the taxonomy is now empty" — it is a broken response,
      // and the cache's contract says to throw so the last known snapshot survives. Fabricating an
      // empty document here would blank a working app's category tree on a bad deploy.
      throw const ApiException(
        statusCode: 200,
        code: ApiErrorCodes.malformedResponse,
        message: 'A registry document came back without a body.',
      );
    }

    return RegistryFresh(
      // The header is the authority on which snapshot served this body. Falling back to the body's
      // own `version` covers the endpoints that carry it inline, and to `0` only when neither
      // exists — which the cache reads as "unversioned", not as "version zero of a real registry".
      version: response.registryVersion ?? _versionInBody(body) ?? 0,
      etag: response.etag,
      body: body,
    );
  }

  /// Rebuilds the query the key was derived from.
  ///
  /// `RegistryKey` canonicalises and sorts its query so two spellings of one document share a cache
  /// row; that canonical form is what gets sent, so the request and the cache key can never
  /// describe different documents.
  static Map<String, Object?>? _queryOf(RegistryKey key) {
    if (key.queryString.isEmpty) return null;
    final query = <String, Object?>{};
    for (final pair in key.queryString.split('&')) {
      final separator = pair.indexOf('=');
      if (separator <= 0) continue;
      query[pair.substring(0, separator)] = pair.substring(separator + 1);
    }
    return query.isEmpty ? null : query;
  }

  static int? _versionInBody(Map<String, dynamic> body) {
    final version = body['version'];
    return version is int ? version : null;
  }
}
