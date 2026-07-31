import 'package:flutter/foundation.dart';

import '../models/form.dart';
import '../models/platform.dart';
import '../models/taxonomy.dart';
import '../models/workflow.dart';
import 'registry_client.dart';
import 'registry_key.dart';
import 'registry_transport.dart';

/// The typed face of the registry cache.
///
/// An extension rather than methods on [RegistryClient] for one reason: it keeps the cache itself
/// free of any knowledge of what a taxonomy or a form *is*. The client stores bodies and versions;
/// this file is the only place that knows a body decodes to a `TaxonomyTree`. Adding a ninth
/// registry means adding a line here and nothing else.
///
/// Every method inherits the client's behaviour whole: disk first, revalidate behind, and a throw
/// only when the document has never been seen on this device.
extension RegistryReads on RegistryClient {
  /// Registry #1 — the category tree.
  Future<TaxonomyTree> taxonomyTree() =>
      readAs(RegistryDocuments.taxonomyTree, TaxonomyTree.fromJson);

  /// The attribute definitions a category exposes.
  ///
  /// This is the call that makes the north star's claim true: `battery_health_pct` appears here
  /// because someone added a row, and the sell form renders it because the form registry references
  /// it. Nothing in Dart names either one.
  Future<AttributesResponse> attributes(String categoryCode) =>
      readAs(RegistryDocuments.attributes(categoryCode), AttributesResponse.fromJson);

  Future<TaxonomyNodesResponse> makes({String? categoryCode}) => readAs(
        RegistryDocuments.makes(categoryCode: categoryCode),
        TaxonomyNodesResponse.fromJson,
      );

  Future<TaxonomyNodesResponse> models(String makeCode) =>
      readAs(RegistryDocuments.models(makeCode), TaxonomyNodesResponse.fromJson);

  Future<TaxonomyNodesResponse> variants(String modelCode, {int? year}) => readAs(
        RegistryDocuments.variants(modelCode, year: year),
        TaxonomyNodesResponse.fromJson,
      );

  /// Registry #2 — a form definition.
  ///
  /// The one document that must survive offline above all others: a seller who opened the sell flow
  /// on a train and lost signal in a tunnel has to keep typing, and the fields they are typing into
  /// only exist because this returned.
  Future<FormDefinition> formDefinition(String key, {String? context}) =>
      readAs(RegistryDocuments.form(key, context: context), FormDefinition.fromJson);

  /// Registry #3 — a workflow's states and transitions.
  ///
  /// The *definition* only. The live state of one entity, and which actions this user may fire on
  /// it right now, is a per-entity read that is never cached as a registry — permissions are
  /// evaluated server-side per request, and serving a stale `availableActions` would offer a dealer
  /// a button the server will refuse.
  Future<WorkflowDefinition> workflowDefinition(String key) =>
      readAs(RegistryDocuments.workflow(key), WorkflowDefinition.fromJson);

  /// Registry #6 — modules and flags for this user.
  Future<Features> features() => readAs(RegistryDocuments.features, Features.fromJson);

  /// Registry #9 — one locale bundle, raw.
  ///
  /// Prefer [strings], which composes the `en-IN` fallback the contract requires.
  Future<I18nBundle> i18nBundle(String locale, {String namespace = 'web'}) => readAs(
        RegistryDocuments.i18n(locale, namespace: namespace),
        I18nBundle.fromJson,
      );

  /// Registry #10 — serviceable cities.
  Future<CitiesResponse> cities() => readAs(RegistryDocuments.cities, CitiesResponse.fromJson);

  /// The full three-step lookup: requested locale → `en-IN` → the key itself.
  ///
  /// Composed here because it needs *two* registry documents, and only something holding the cache
  /// can fetch the second one. `I18nBundle.resolve` deliberately stops at step three and leaves the
  /// middle step to whoever holds both bundles; this is that caller.
  ///
  /// The `en-IN` bundle is best-effort. A user who has the Marathi bundle on disk and has never
  /// been online in English gets Marathi plus raw keys for anything Marathi is missing — which is
  /// still a usable screen, and better than refusing to render because a fallback could not be
  /// fetched.
  Future<AhStrings> strings(String locale, {String namespace = 'web'}) async {
    final primary = await i18nBundle(locale, namespace: namespace);
    if (primary.locale == AhStrings.fallbackLocale) return AhStrings(primary: primary);

    try {
      final fallback = await i18nBundle(AhStrings.fallbackLocale, namespace: namespace);
      return AhStrings(primary: primary, fallback: fallback);
    } on RegistryUnavailableException {
      return AhStrings(primary: primary);
    }
  }
}

/// Two bundles and the lookup chain between them.
///
/// The chain is from `contracts/registry-api.md` §6 and it is absolute: **never render an empty
/// string.** A blank button is unrecoverable for the user — they cannot guess what it does, cannot
/// describe it to support, and cannot work around it. `listing.cta.get_valuation` rendered on the
/// button is ugly and immediately diagnosable, which is strictly the better failure.
@immutable
final class AhStrings {
  const AhStrings({required this.primary, this.fallback});

  /// The locale every other locale falls back to. `en-IN` and not `en`, because the product's
  /// English is Indian English — lakh, crore, RTO, and a `₹` that groups two-two-three.
  static const String fallbackLocale = 'en-IN';

  final I18nBundle primary;

  /// The `en-IN` bundle, when it could be obtained. Null is a degraded but working state.
  final I18nBundle? fallback;

  String get locale => primary.locale;

  /// Resolves [key], never returning empty.
  ///
  /// There is no placeholder interpolation here on purpose. The contract does not define a
  /// substitution syntax, so inventing one would be the client deciding something the registry
  /// owns — and the moment two clients invent two syntaxes, a translator's `{count}` renders
  /// literally on one of them. Values that vary belong in the *server's* resolved string, or in
  /// composition at the call site where the type of the value is known.
  String call(String key) {
    final direct = primary.resources[key];
    if (direct != null && direct.isNotEmpty) return direct;

    final english = fallback?.resources[key];
    if (english != null && english.isNotEmpty) return english;

    return key;
  }

  /// True when [key] resolved to a real translation rather than to itself. For a diagnostics
  /// screen that lists what a locale is still missing.
  bool has(String key) {
    final direct = primary.resources[key];
    if (direct != null && direct.isNotEmpty) return true;
    final english = fallback?.resources[key];
    return english != null && english.isNotEmpty;
  }

  @override
  String toString() => 'AhStrings($locale, ${primary.resources.length} keys, '
      'fallback: ${fallback == null ? 'none' : '${fallback!.resources.length} keys'})';
}
