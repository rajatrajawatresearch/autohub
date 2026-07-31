import 'package:flutter/foundation.dart';

/// Identifies one registry document — both where to fetch it and where it lives on disk.
///
/// The cache key is *derived* from the path and query rather than supplied alongside them. Two
/// callers who spell the same document differently would otherwise cache it twice and revalidate it
/// twice, and — far worse — a caller who reuses a name across two different query strings would
/// serve an SUV's attribute list to someone selling a truck. Deriving makes that class of bug
/// unrepresentable.
@immutable
final class RegistryKey {
  const RegistryKey._(this.path, this.queryString);

  /// Query entries are sorted and empty values dropped, so `{make: x, category: y}` and
  /// `{category: y, make: x}` are one cache entry rather than two.
  factory RegistryKey(String path, [Map<String, Object?> query = const {}]) {
    final pairs = <String>[];
    for (final entry in query.entries) {
      final value = entry.value;
      if (value == null) continue;
      final encoded = value.toString();
      if (encoded.isEmpty) continue;
      pairs.add('${entry.key}=$encoded');
    }
    pairs.sort();
    return RegistryKey._(path, pairs.join('&'));
  }

  /// Path relative to the API base — `/platform/taxonomy/tree`.
  final String path;

  /// Canonical, sorted query string without the leading `?`. Empty when there is none.
  final String queryString;

  /// The primary key of the row in `CachedRegistries`.
  String get name => queryString.isEmpty ? path : '$path?$queryString';

  @override
  bool operator ==(Object other) => other is RegistryKey && other.name == name;

  @override
  int get hashCode => name.hashCode;

  @override
  String toString() => 'RegistryKey($name)';
}

/// Every registry document the mobile clients read, named once.
///
/// Note what this class is *not*: it is not an enum, and it never could be. Half of these documents
/// are parameterised by a value that lives in a database row — a form key, a category code, a
/// locale — so the set of documents is open by construction. That is the shape of the whole
/// architecture in one class: the client knows the *seven kinds* of registry (which the contract
/// fixes) and nothing at all about their contents (which operations changes on a Tuesday).
abstract final class RegistryDocuments {
  /// Registry #1 — the category tree behind every picker and facet.
  static final RegistryKey taxonomyTree = RegistryKey('/platform/taxonomy/tree');

  /// The attribute definitions one category exposes. This is the document that makes
  /// `battery_health_pct` appear in a sell form without a release.
  static RegistryKey attributes(String categoryCode) =>
      RegistryKey('/platform/taxonomy/attributes', {'category': categoryCode});

  static RegistryKey makes({String? categoryCode}) =>
      RegistryKey('/platform/taxonomy/makes', {'category': categoryCode});

  static RegistryKey models(String makeCode) =>
      RegistryKey('/platform/taxonomy/models', {'make': makeCode});

  static RegistryKey variants(String modelCode, {int? year}) =>
      RegistryKey('/platform/taxonomy/variants', {'model': modelCode, 'year': year});

  /// Registry #2 — a form definition. [context] selects a category variant, `category:suv`.
  ///
  /// The key is percent-encoded because it reaches this method straight from a registry row, and a
  /// row is data: nothing guarantees an operations user will not name a form with a space in it.
  static RegistryKey form(String key, {String? context}) => RegistryKey(
        '/platform/forms/${Uri.encodeComponent(key)}',
        {'context': context},
      );

  /// Registry #3 — a workflow's states and transitions.
  static RegistryKey workflow(String key) =>
      RegistryKey('/platform/workflows/${Uri.encodeComponent(key)}');

  /// Registry #6 — modules and flags, already evaluated for this user.
  static final RegistryKey features = RegistryKey('/platform/features');

  /// Registry #9 — a locale bundle.
  ///
  /// [namespace] defaults to the web bundle deliberately. The strings are the same product and
  /// splitting them would let the two clients drift apart in wording; when mobile genuinely needs
  /// its own set, that is a new namespace in the registry, not a new release of this file.
  static RegistryKey i18n(String locale, {String namespace = 'web'}) => RegistryKey(
        '/platform/i18n/${Uri.encodeComponent(locale)}',
        {'namespace': namespace},
      );

  /// Registry #10 — serviceable cities.
  static final RegistryKey cities =
      RegistryKey('/platform/geo/cities', {'serviceable': 'true'});
}
