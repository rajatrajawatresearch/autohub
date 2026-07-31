import 'package:autohub_core/autohub_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/auction_feed.dart';
import '../data/bid_client.dart';
import '../data/dealer_api.dart';
import '../data/workflow_actions.dart';
import 'strings.dart';

/* ── Transport ─────────────────────────────────────────────────────────────── */

/// The configured client. **Overridden at the composition root** in `main.dart`.
///
/// It throws rather than building a default for the same reason `registryTransportProvider` in
/// `autohub_core` does: a client constructed lazily here would have no hydrated session and no
/// device id, and would send its first request unauthenticated — taking a `401` for a session that
/// was perfectly good. Failing loudly on the first run without a composition root is a five-second
/// fix; failing subtly in production is a support ticket about random sign-outs.
final Provider<ApiClient> apiClientProvider = Provider<ApiClient>(
  (ref) => throw UnimplementedError(
    'apiClientProvider was not overridden. main() builds the ApiClient after hydrating the '
    'session and the device id, and supplies it in the ProviderScope overrides.',
  ),
);

final Provider<DealerApi> dealerApiProvider =
    Provider<DealerApi>((ref) => DealerApi(ref.watch(apiClientProvider)));

/// The read-side auction socket.
///
/// Its origin is derived from the API base URL rather than configured separately, so the socket
/// and the REST client can never point at two different environments — which is a failure that
/// looks exactly like "the countdown is wrong" and takes a day to find.
final Provider<AuctionFeedFactory> auctionFeedFactoryProvider = Provider<AuctionFeedFactory>(
  (ref) => WebSocketAuctionFeedFactory(
    wsBaseUrl: ref.watch(apiClientProvider).config.wsBaseUrl,
    backoff: ref.watch(backoffPolicyProvider),
  ),
);

/// The bearer the socket authenticates with. Read at connect time, never cached by a screen.
final Provider<String?> accessTokenProvider =
    Provider<String?>((ref) => ref.watch(apiClientProvider).tokens.tokens?.accessToken);

/// The only path a bid takes. See `BidClient` — it cannot queue, by construction.
final Provider<BidClient> bidClientProvider =
    Provider<BidClient>((ref) => BidClient(ref.watch(immediateWriterProvider)));

/// Workflow actions, through the durable queue.
final Provider<WorkflowActionRunner> workflowActionRunnerProvider =
    Provider<WorkflowActionRunner>(
  (ref) => WorkflowActionRunner(
    outbox: ref.watch(outboxProvider),
    drainer: ref.watch(outboxDrainerProvider),
  ),
);

/* ── Locale and words ──────────────────────────────────────────────────────── */

/// The locale the user picked, from device preferences. Null until they choose one.
final StreamProvider<String?> localePreferenceProvider = StreamProvider<String?>(
  (ref) => ref.watch(devicePreferencesProvider).watch(DevicePreferences.localeKey),
);

/// The locale in force: the user's choice, else the transport's default.
///
/// Not an enum anywhere in this app. Which languages AutoHub serves is an operations decision — a
/// bundle is published, not a binary — and a handset that compiled in eight of them cannot render
/// the ninth.
final Provider<String> activeLocaleProvider = Provider<String>(
  (ref) =>
      ref.watch(localePreferenceProvider).valueOrNull ??
      ref.watch(apiClientProvider).config.defaultLocale,
);

final FutureProvider<DealerStrings> localisationProvider = FutureProvider<DealerStrings>(
  (ref) async {
    final client = ref.watch(registryClientProvider);
    try {
      return DealerStrings(await client.strings(ref.watch(activeLocaleProvider)));
    } on RegistryUnavailableException {
      // First launch, no network, nothing on disk. The shipped `en-IN` map carries the screen —
      // see `kDealerEnIn`. Refusing to render because a translation could not be fetched would be
      // the one failure the fallback chain exists to prevent.
      return const DealerStrings(null);
    }
  },
);

/// How every widget asks for a word: `ref.watch(stringsProvider)(DealerText.placeBid)`.
///
/// Synchronous on purpose. Threading an `AsyncValue` through every label would put a loading state
/// on the word "Cancel", and a screen that cannot render its own chrome until a registry answers is
/// a screen that is blank in a lift.
final Provider<DealerStrings> stringsProvider = Provider<DealerStrings>(
  (ref) => ref.watch(localisationProvider).valueOrNull ?? const DealerStrings(null),
);

/* ── Registries ────────────────────────────────────────────────────────────── */

/// Modules and flags, already evaluated server-side for this dealer.
///
/// The console's sections are filtered on these: a dealer in a city where auction has not launched
/// does not see the tab. No empty page, no "coming soon", and no code change on the day it launches.
final FutureProvider<Features> featuresProvider =
    FutureProvider<Features>((ref) => ref.watch(registryClientProvider).features());

/// A workflow's declared states and transitions — `listing`, `deal`.
///
/// The *definition* only. What this dealer may do to one entity is a per-entity read; serving a
/// cached `availableActions` would offer a button the server is going to refuse.
final FutureProviderFamily<WorkflowDefinition, String> workflowDefinitionProvider =
    FutureProvider.family<WorkflowDefinition, String>(
  (ref, key) => ref.watch(registryClientProvider).workflowDefinition(key),
);
