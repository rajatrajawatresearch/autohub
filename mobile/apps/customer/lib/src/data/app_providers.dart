import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/fault.dart';
import '../i18n/customer_text.dart';
import 'customer_api.dart';

/* ── Transport ─────────────────────────────────────────────────────────────── */

/// The HTTP client. **Overridden at the composition root** in `main.dart`.
///
/// It throws rather than constructing a default for the same reason `autohub_core`'s two transport
/// providers do: a client built with no device id sends unbound refresh tokens and no `X-Device-Id`,
/// and that failure is invisible until a session is silently rejected weeks later. Failing on the
/// first run of an unwired app is a five-second fix.
final Provider<ApiClient> apiClientProvider = Provider<ApiClient>(
  (ref) => throw UnimplementedError(
    'apiClientProvider was not overridden. main.dart builds the ApiClient once, after the device '
    'id and the stored session have been read, and supplies it in the ProviderScope overrides.',
  ),
);

/// The keychain-backed token store the transport runs on. Overridden alongside [apiClientProvider].
final Provider<SessionTokenStore> sessionTokenStoreProvider = Provider<SessionTokenStore>(
  (ref) => throw UnimplementedError(
    'sessionTokenStoreProvider was not overridden. See bootstrap() in main.dart.',
  ),
);

/// Every non-registry, non-queued call this app makes.
///
/// Widget tests override this one provider and get the whole app without a socket.
final Provider<CustomerApi> customerApiProvider =
    Provider<CustomerApi>((ref) => HttpCustomerApi(ref.watch(apiClientProvider)));

/* ── Device preferences ────────────────────────────────────────────────────── */

/// The locale to ask the i18n registry for, live.
///
/// Falls back to `en-IN` — the contract's own fallback, and Indian English rather than `en`,
/// because the product's English says lakh, crore, RTO and groups `₹` two-two-three.
final StreamProvider<String> appLocaleProvider = StreamProvider<String>(
  (ref) => ref
      .watch(devicePreferencesProvider)
      .watch(DevicePreferences.localeKey)
      .map((String? stored) => (stored == null || stored.isEmpty)
          ? AhStrings.fallbackLocale
          : stored),
);

/* ── Localisation ──────────────────────────────────────────────────────────── */

/// The i18n bundles, from disk first and revalidated behind.
///
/// A failure here is not an error state anywhere in the UI. It means the phone has never been
/// online in this locale, and the authored fallback in `customer_text.dart` carries the screen —
/// which is why this provider is only ever read through [appStringsProvider].
final FutureProvider<AppStrings> appStringsFutureProvider =
    FutureProvider<AppStrings>((ref) async {
  final String locale = await ref.watch(appLocaleProvider.future);
  final AhStrings bundles = await ref.watch(registryClientProvider).strings(locale);
  return AppStrings(bundles);
});

/// The strings, synchronously, for widgets.
///
/// Every screen reads this. Returning the authored fallback while the bundle loads is deliberate:
/// a splash of unlabelled controls followed by a re-layout is worse on a cheap handset than one
/// frame of English, and the swap is a single rebuild when the registry answers.
final Provider<AppStrings> appStringsProvider = Provider<AppStrings>(
  (ref) => ref.watch(appStringsFutureProvider).valueOrNull ?? const AppStrings(),
);

/* ── Registry documents ────────────────────────────────────────────────────── */

/// Modules and flags, already evaluated server-side for this user in this city.
///
/// Read as "off" while it loads, by `Features.module`'s own fail-closed rule. Showing a Lease tile
/// in a city where leasing is not live sends a user down a flow that ends in a support call.
final FutureProvider<Features> featuresProvider =
    FutureProvider<Features>((ref) => ref.watch(registryClientProvider).features());

final FutureProvider<CitiesResponse> citiesProvider =
    FutureProvider<CitiesResponse>((ref) => ref.watch(registryClientProvider).cities());

/// A form definition by key. `sell_vehicle` is the one this app asks for.
final FutureProviderFamily<FormDefinition, String> formDefinitionProvider =
    FutureProvider.family<FormDefinition, String>(
  (ref, key) => ref.watch(registryClientProvider).formDefinition(key),
);

/// A workflow's states and transitions, for turning a history of state *codes* into names.
final FutureProviderFamily<WorkflowDefinition, String> workflowDefinitionProvider =
    FutureProvider.family<WorkflowDefinition, String>(
  (ref, key) => ref.watch(registryClientProvider).workflowDefinition(key),
);

/* ── The form renderer's two ports ─────────────────────────────────────────── */

String? _stringOf(Object? value) {
  if (value is String && value.isNotEmpty) return value;
  if (value is num || value is bool) return '$value';
  return null;
}

int? _intOf(Object? value) {
  if (value is num) return value.isFinite ? value.round() : null;
  if (value is String) return num.tryParse(value.trim())?.round();
  return null;
}

Option _nodeOption(TaxonomyNode node) => Option(value: node.code, label: node.name);

/// Fills `SchemaForm`'s options port from the taxonomy and geography registries.
///
/// This function is the reason there is no list of fuel types, makes or cities anywhere in this
/// binary. The form registry names an attribute or a source; this resolves it against the cached
/// registry documents and hands back whatever operations published. Adding `battery_health_pct`
/// with four options is a database row, and it arrives here without a line changing.
///
/// The source names are matched on their last segment so that `taxonomy.makes`, `makes` and `make`
/// all resolve — the registry is authored by people through an admin console, and a form that
/// renders no options because somebody typed the singular is a form that loses a sale. A source
/// this build genuinely cannot resolve yields an empty list, which the control renders as "no
/// options" rather than as a failure: an unknown source is not something a retry can fix.
final Provider<FormOptionsLoader> formOptionsLoaderProvider = Provider<FormOptionsLoader>(
  (ref) => (OptionsRequest request) async {
    final RegistryClient registry = ref.read(registryClientProvider);
    final Map<String, Object?> params = request.params;
    final String? category =
        _stringOf(params['category']) ?? _stringOf(params['categoryCode']);

    final String source =
        (request.source ?? request.level ?? '').toLowerCase().split('.').last;

    switch (source) {
      case 'make':
      case 'makes':
        return <Option>[
          for (final TaxonomyNode node in (await registry.makes(categoryCode: category)).items)
            _nodeOption(node),
        ];

      case 'model':
      case 'models':
        final String? make = _stringOf(params['make']) ?? _stringOf(params['makeCode']);
        if (make == null) return const <Option>[];
        return <Option>[
          for (final TaxonomyNode node in (await registry.models(make)).items) _nodeOption(node),
        ];

      case 'variant':
      case 'variants':
        final String? model = _stringOf(params['model']) ?? _stringOf(params['modelCode']);
        if (model == null) return const <Option>[];
        final int? year = _intOf(params['year']);
        return <Option>[
          for (final TaxonomyNode node in (await registry.variants(model, year: year)).items)
            _nodeOption(node),
        ];

      case 'city':
      case 'cities':
        return <Option>[
          for (final City city in (await registry.cities()).items)
            if (city.serviceable) Option(value: city.code, label: city.name),
        ];
    }

    // No named source: the field pointed at a taxonomy attribute instead, which is the common case
    // for a `select`. The attribute's options are the registry's answer to "what fuel types exist",
    // and this app has never heard of any of them.
    final String? attribute = request.attribute;
    if (attribute != null && category != null) {
      final AttributesResponse attributes = await registry.attributes(category);
      for (final TaxonomyAttribute definition in attributes.attributes) {
        if (definition.code == attribute) return definition.options ?? const <Option>[];
      }
    }

    return const <Option>[];
  },
);

/* ── Diagnostics ───────────────────────────────────────────────────────────── */

/// The last few failures, newest first, for the support path.
///
/// Bounded and in memory. It exists so that "it did this an hour ago" has an answer that is not a
/// user's recollection, and it deliberately holds [AppFault] — the code, the status, the trace id —
/// and never the user-facing sentence, which is whatever the registry said at the time and is worth
/// nothing to a log query.
final NotifierProvider<DiagnosticsLog, List<AppFault>> diagnosticsLogProvider =
    NotifierProvider<DiagnosticsLog, List<AppFault>>(DiagnosticsLog.new);

class DiagnosticsLog extends Notifier<List<AppFault>> {
  /// Twenty is enough to cover one session's worth of a user retrying, and small enough that it is
  /// never worth persisting — a crash loses it, and a crash has its own report.
  static const int _limit = 20;

  @override
  List<AppFault> build() => const <AppFault>[];

  /// The one line every controller writes in its `catch`.
  ///
  /// Describing and recording are the same act on purpose: a fault that reaches a user and is not
  /// in the log is a support call with nothing behind it, and separating the two steps is how one
  /// of them gets forgotten in the next `catch` somebody writes.
  AppFault capture(Object error) {
    final AppFault fault = AppFault.of(error, ref.read(appStringsProvider));
    record(fault);
    return fault;
  }

  void record(AppFault fault) {
    state = <AppFault>[fault, ...state.take(_limit - 1)];
  }
}
