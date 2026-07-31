/// The partner app's dependency graph.
///
/// Two conventions, both inherited from `autohub_core`'s own providers and both here for the same
/// reason — that the parts of this app worth testing are the parts that misbehave when the network
/// does. Everything is overridable, and the one leaf that needs a socket ([apiClientProvider])
/// throws until the shell wires it, so a missing override is a five-second fix at launch rather
/// than a null surfacing inside a settlement screen.
library;

import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/partner_side.dart';
import '../app/partner_text.dart';
import 'attachment_api.dart';
import 'attachment_models.dart';
import 'wire.dart';

/* ── Transport ────────────────────────────────────────────────────────────── */

/// The dio-backed client. **Must be overridden at the composition root** — it needs the device id
/// from the keychain and the hydrated session, neither of which is available synchronously.
final Provider<ApiClient> apiClientProvider = Provider<ApiClient>(
  (ref) => throw UnimplementedError(
    'apiClientProvider was not overridden. bootstrapPartner() supplies it in the ProviderScope.',
  ),
);

final Provider<AttachmentApi> attachmentApiProvider =
    Provider<AttachmentApi>((ref) => AttachmentApi(ref.watch(apiClientProvider)));

/// The clock every countdown in this app renders against.
///
/// Unsynchronised today, and named rather than defaulted so that using it looks like a decision.
/// The honest position: every countdown here is measured in whole days, so a handset would have to
/// be more than twelve hours out for its drift to change an answer — and a phone that wrong is one
/// whose owner has already noticed. The provider exists so that the day the transport starts
/// surfacing the response `Date` header, one override corrects every expiry countdown at once
/// instead of a sweep through the screens.
final Provider<AhServerClock> serverClockProvider =
    Provider<AhServerClock>((ref) => AhServerClock.unsynchronised);

/* ── Localisation ─────────────────────────────────────────────────────────── */

/// The locale this device is reading in.
///
/// Marathi, Kannada, Hindi and Tamil are the priority set for attachment because owner-drivers are
/// the supply side of this market — so the locale is a stored preference, not a compile-time
/// constant, and it falls back to `en-IN` rather than to a device locale the registry may have no
/// bundle for.
final StreamProvider<String> activeLocaleProvider = StreamProvider<String>(
  (ref) => ref
      .watch(devicePreferencesProvider)
      .watch(DevicePreferences.localeKey)
      .map((stored) => stored ?? AhStrings.fallbackLocale),
);

final FutureProvider<AhStrings> partnerStringsProvider = FutureProvider<AhStrings>((ref) {
  final String locale = ref.watch(activeLocaleProvider).valueOrNull ?? AhStrings.fallbackLocale;
  return ref.watch(registryClientProvider).strings(locale);
});

/// The callable string resolver every screen reads.
///
/// Synchronous by construction: it hands [PartnerText] whatever the bundle has resolved to *so
/// far*, which is `null` on a first launch with no network. That is the point — a screen never
/// waits on the localisation registry, it renders in the fallback voice and re-renders when the
/// bundle lands. A settlement screen that spins because a translation is in flight is a settlement
/// screen that is blank in a basement.
final Provider<PartnerText> partnerTextProvider = Provider<PartnerText>(
  (ref) => PartnerText(ref.watch(partnerStringsProvider).valueOrNull),
);

/* ── Registries ───────────────────────────────────────────────────────────── */

final FutureProvider<Features> featuresProvider =
    FutureProvider<Features>((ref) => ref.watch(registryClientProvider).features());

/// A form definition from the Form Schema Registry.
///
/// The family key is the form's registry address. Nothing here knows what fields come back, which
/// is the claim in `docs/10-mobile-architecture.md` §1 that this app exists to keep honest: an
/// operations user adds a field to `attach.supply_post` and it appears on the next launch, with its
/// validation and its conditional visibility, with nothing rebuilt.
final FutureProviderFamily<FormDefinition, String> formDefinitionProvider =
    FutureProvider.family<FormDefinition, String>(
  (ref, key) => ref.watch(registryClientProvider).formDefinition(key),
);

/// A workflow's declared states, for the timeline's vocabulary.
final FutureProviderFamily<WorkflowDefinition, String> workflowDefinitionProvider =
    FutureProvider.family<WorkflowDefinition, String>(
  (ref, key) => ref.watch(registryClientProvider).workflowDefinition(key),
);

/* ── Which side of the market ─────────────────────────────────────────────── */

/// The side read off disk during bootstrap. Overridden at the composition root; `null` means the
/// device has never chosen.
final Provider<PartnerSide?> initialPartnerSideProvider = Provider<PartnerSide?>((ref) => null);

final NotifierProvider<PartnerSideController, PartnerSide?> partnerSideProvider =
    NotifierProvider<PartnerSideController, PartnerSide?>(PartnerSideController.new);

/// Holds the chosen side and writes it through.
///
/// State changes first and notifies immediately; the disk write follows. The same trade
/// `AhThemeController` makes and for the same reason — a chooser that waits on SQLite before it
/// navigates feels broken on a cheap handset, and the failure mode of a lost write is that the
/// partner picks their side once more.
class PartnerSideController extends Notifier<PartnerSide?> {
  @override
  PartnerSide? build() => ref.read(initialPartnerSideProvider);

  Future<void> choose(PartnerSide side) async {
    if (state == side) return;
    state = side;
    await ref.read(devicePreferencesProvider).write(PartnerSide.storageKey, side.storageValue);
  }
}

/* ── The boards ───────────────────────────────────────────────────────────── */

/// Covers the frame during a side switch, when the outgoing screen is still in the tree and the new
/// side has not settled. A race, never a state a partner sees — the router will not mount any of
/// these until a side exists.
Future<CursorPage<T>> _emptyDuringSideSwitch<T>() =>
    Future<CursorPage<T>>.value(CursorPage<T>(items: <T>[]));

final AutoDisposeFutureProvider<CursorPage<SupplyPost>> supplyPostsProvider =
    FutureProvider.autoDispose<CursorPage<SupplyPost>>((ref) {
  if (ref.watch(partnerSideProvider) == null) return _emptyDuringSideSwitch<SupplyPost>();
  return ref.watch(attachmentApiProvider).supplyPosts();
});

final AutoDisposeFutureProvider<CursorPage<DemandPost>> demandPostsProvider =
    FutureProvider.autoDispose<CursorPage<DemandPost>>((ref) {
  if (ref.watch(partnerSideProvider) == null) return _emptyDuringSideSwitch<DemandPost>();
  return ref.watch(attachmentApiProvider).demandPosts();
});

/// The match board, optionally narrowed to one of this partner's posts.
///
/// `autoDispose` on a family so that walking into a post's matches and back out does not keep a
/// page of forty candidates — each with a commission quote — alive for the session.
final AutoDisposeFutureProviderFamily<CursorPage<AttachmentMatch>, String?> matchesProvider =
    FutureProvider.autoDispose.family<CursorPage<AttachmentMatch>, String?>((ref, postId) {
  final PartnerSide? side = ref.watch(partnerSideProvider);
  if (side == null) return _emptyDuringSideSwitch<AttachmentMatch>();
  return ref.watch(attachmentApiProvider).matches(side: side, postId: postId);
});

final AutoDisposeFutureProvider<CursorPage<AttachmentContract>> contractsProvider =
    FutureProvider.autoDispose<CursorPage<AttachmentContract>>((ref) {
  final PartnerSide? side = ref.watch(partnerSideProvider);
  if (side == null) return _emptyDuringSideSwitch<AttachmentContract>();
  return ref.watch(attachmentApiProvider).contracts(side: side);
});

final AutoDisposeFutureProviderFamily<AttachmentContract, String> contractProvider =
    FutureProvider.autoDispose.family<AttachmentContract, String>(
  (ref, id) => ref.watch(attachmentApiProvider).contract(id),
);

/// Live state and available actions, read per visit and never carried across one.
final AutoDisposeFutureProviderFamily<WorkflowSnapshot, String> contractWorkflowProvider =
    FutureProvider.autoDispose.family<WorkflowSnapshot, String>(
  (ref, id) => ref.watch(attachmentApiProvider).contractWorkflow(id),
);

final AutoDisposeFutureProviderFamily<List<ComplianceDocument>, String> contractComplianceProvider =
    FutureProvider.autoDispose.family<List<ComplianceDocument>, String>(
  (ref, contractId) => ref.watch(attachmentApiProvider).compliance(contractId),
);

/// Settlement cycles, across every contract when the key is `null`.
final AutoDisposeFutureProviderFamily<CursorPage<SettlementCycle>, String?> cyclesProvider =
    FutureProvider.autoDispose.family<CursorPage<SettlementCycle>, String?>(
  (ref, contractId) => ref.watch(attachmentApiProvider).cycles(contractId: contractId),
);

final AutoDisposeFutureProviderFamily<SettlementCycle, String> cycleProvider =
    FutureProvider.autoDispose.family<SettlementCycle, String>(
  (ref, id) => ref.watch(attachmentApiProvider).cycle(id),
);

/* ── The queue ────────────────────────────────────────────────────────────── */

/// Everything captured but not yet delivered, live.
///
/// Surfaced as a screen rather than hidden, because on this app the queue holds a partner's supply
/// post and their contract transitions. "Where did my listing go" has an answer, and the answer is
/// a list with a support reference on each row.
final StreamProvider<List<OutboxEntry>> outboxEntriesProvider =
    StreamProvider<List<OutboxEntry>>((ref) => ref.watch(outboxProvider).watch());

/* ── Theme ────────────────────────────────────────────────────────────────── */

/// Persists the theme choice in device preferences.
///
/// `autohub_ui` declares the port and refuses to implement it — it is the package that knows what a
/// colour is and has no business opening a database — so the app supplies this.
final class PreferencesThemeStore implements AhThemeStore {
  const PreferencesThemeStore(this._preferences);

  static const String _key = 'theme_mode';

  final DevicePreferences _preferences;

  @override
  Future<AhThemeMode?> read() async => AhThemeMode.tryParse(await _preferences.read(_key));

  @override
  Future<void> write(AhThemeMode mode) => _preferences.write(_key, mode.storageKey);
}

final Provider<AhThemeController> themeControllerProvider = Provider<AhThemeController>((ref) {
  final AhThemeController controller = AhThemeController(
    store: PreferencesThemeStore(ref.watch(devicePreferencesProvider)),
  );
  // Reading from disk is not a network call, so starting it on first read is safe — and the
  // controller repaints when it lands, so a partner who chose dark sees at worst one light frame.
  unawaited(controller.load());
  ref.onDispose(controller.dispose);
  return controller;
});
