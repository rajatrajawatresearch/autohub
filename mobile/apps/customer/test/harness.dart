import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_customer/src/app/customer_app.dart';
import 'package:autohub_customer/src/data/app_providers.dart';
import 'package:autohub_customer/src/data/customer_api.dart';
import 'package:autohub_customer/src/data/provisional_models.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures/registry.dart';

/// The test rig for the customer binary.
///
/// Two decisions in here are worth stating, because they are what make these tests worth running:
///
/// **The real cache, the real queue, the real database.** `ahDatabaseProvider` is overridden with
/// `openInMemoryAhDatabase()` — an actual SQLite database — rather than with a stub, so a draft
/// test exercises the same `insertOnConflictUpdate` a handset does and an outbox test exercises the
/// same primary key that makes a double tap one row. The interesting failures in this app are about
/// what survives a process and what does not, and a fake store agrees with whatever the code
/// happens to do.
///
/// **The seams are the only fakes.** `CustomerApi`, `RegistryTransport` and `OutboxTransport` are
/// the three places this app touches a socket, and each is an interface precisely so a test can make
/// the network misbehave on demand. A test that needs a real network to make the network misbehave
/// is a test that gets deleted.
class TestRig {
  TestRig._({
    required this.api,
    required this.registry,
    required this.outbox,
    required this.database,
    required this.tokens,
    required this.container,
  });

  /// Asynchronous because the session store is: `SessionTokenStore.open` hydrates from the keychain
  /// once, at boot, exactly as `main.dart` does. Skipping that and stubbing the token store would
  /// skip the one ordering bug worth catching — an `ApiClient` built before the stored session is
  /// read sends its first request unauthenticated.
  static Future<TestRig> create({
    FakeCustomerApi? api,
    FakeRegistryTransport? registry,
    FakeOutboxTransport? outbox,
    List<Override> extraOverrides = const <Override>[],
  }) async {
    final AhDatabase database = openInMemoryAhDatabase();
    final InMemorySecureStore secure = InMemorySecureStore();
    final SessionTokenStore tokens = await SessionTokenStore.open(SessionStore(secure));

    final FakeCustomerApi resolvedApi = api ?? FakeCustomerApi();
    final FakeRegistryTransport resolvedRegistry = registry ?? FakeRegistryTransport.withDefaults();
    final FakeOutboxTransport resolvedOutbox = outbox ?? FakeOutboxTransport();

    return TestRig._(
      api: resolvedApi,
      registry: resolvedRegistry,
      outbox: resolvedOutbox,
      database: database,
      tokens: tokens,
      container: ProviderContainer(
        overrides: <Override>[
          ahDatabaseProvider.overrideWithValue(database),
          secureStoreProvider.overrideWithValue(secure),
          sessionTokenStoreProvider.overrideWithValue(tokens),
          customerApiProvider.overrideWithValue(resolvedApi),
          registryTransportProvider.overrideWithValue(resolvedRegistry),
          outboxTransportProvider.overrideWithValue(resolvedOutbox),
          ...extraOverrides,
        ],
      ),
    );
  }

  final FakeCustomerApi api;
  final FakeRegistryTransport registry;
  final FakeOutboxTransport outbox;
  final AhDatabase database;
  final SessionTokenStore tokens;
  final ProviderContainer container;
}

/// A widget test with a rig, torn down inside the test body.
///
/// The teardown has to happen here rather than in `tearDown`, and the reason is specific: the app
/// shell starts `OutboxDrainer`, which holds a periodic sweep timer, and `flutter_test` asserts on
/// pending timers *before* any `tearDown` callback runs. Disposing the container in the `finally`
/// cancels the drain loop through the provider's own `onDispose` — which is also the assertion that
/// the shell's teardown path works, rather than a workaround for it.
void appTest(String description, Future<void> Function(WidgetTester tester, TestRig rig) body) {
  testWidgets(description, (WidgetTester tester) async {
    final TestRig rig = await TestRig.create();
    // The database is closed *outside* the test body. `flutter_test` runs the body inside a
    // fake-async zone where a real I/O future may never complete, and drift's shutdown is real I/O.
    // Disposing the container is what the invariant check cares about, and that is synchronous.
    addTearDown(rig.database.close);
    try {
      await body(tester, rig);
    } finally {
      await tester.pumpWidget(const SizedBox.shrink());
      // Torn down explicitly rather than left to the provider's `onDispose`, which fires it
      // unawaited: the drain loop cancels its sweep timer *after* an await, and the pending-timer
      // invariant does not wait. `runAsync` because the shutdown touches SQLite, and a real I/O
      // future never completes inside the fake-async zone a widget test body runs in.
      await tester.runAsync(() => rig.container.read(outboxDrainerProvider).dispose());
      rig.container.dispose();
      // Tearing down a drift `watch()` schedules a zero-duration timer of its own. Two pumps let
      // it fire, so the invariant check sees a quiet zone rather than somebody else's cleanup.
      await tester.pump(Duration.zero);
      await tester.pump(const Duration(milliseconds: 1));
    }
  });
}

/// Pumps the whole binary — router, theme, shell — against [rig].
///
/// The whole app rather than a screen in isolation, because half of what is being asserted is
/// wiring: that a reveal does not navigate, that a tab keeps its stack, that a deep link resolves.
/// Mounting a screen directly would prove none of it.
Future<void> pumpApp(WidgetTester tester, TestRig rig) async {
  await tester.pumpWidget(
    UncontrolledProviderScope(
      container: rig.container,
      child: const CustomerApp(),
    ),
  );
  await tester.pumpAndSettle();
}

/// A handset, not a tablet.
///
/// 390×844 is an iPhone-class viewport and comfortably narrower than the 800×600 the test harness
/// defaults to. It matters: this product is used one-handed at 360dp by people standing next to a
/// car, and a layout that only holds together on a desktop-sized surface is a layout nobody
/// checked.
void useHandsetViewport(WidgetTester tester) {
  tester.view
    ..physicalSize = const Size(1170, 2532)
    ..devicePixelRatio = 3;
  addTearDown(tester.view.reset);
}

/// Lets a queued write's completion listener finish what it started.
///
/// `OutboxDrainer.drain()` resolves when the pass is over; the `completions` broadcast reaches its
/// listeners a turn later, and the sell controller's listener then does its own asynchronous work —
/// discarding the draft from SQLite before it calls the submission done. Anything asserting on what
/// a *listener* did has to give those turns back, and a handful is cheaper than a fixed sleep.
Future<void> settleEventLoop([int turns = 12]) async {
  for (int turn = 0; turn < turns; turn += 1) {
    await Future<void>.delayed(Duration.zero);
  }
}

/* ── Fakes ─────────────────────────────────────────────────────────────────── */

/// Every read the app makes, scriptable.
class FakeCustomerApi implements CustomerApi {
  SearchResponse searchResponse = const SearchResponse();
  Exception? searchError;

  ListingDetail? listingDetail;
  Exception? listingError;

  ValuationQuote? quote;
  Exception? quoteError;

  CursorPage<SearchItem> listingsPage = const CursorPage<SearchItem>();
  CursorPage<GarageBooking> bookingsPage = const CursorPage<GarageBooking>();

  OtpChallenge challenge = const OtpChallenge(challengeId: 'chal-1');
  AuthSession? session;
  Exception? authError;

  AuthUser? user;

  /// Every search the app issued, in order. The assertion surface for "did the cursor travel?" and
  /// "did the filter reach the query?".
  final List<SearchParams> searches = <SearchParams>[];

  /// The keys the app minted for its unauthenticated writes. A quote requested twice with the same
  /// key is one quote.
  final List<String> idempotencyKeys = <String>[];

  @override
  Future<SearchResponse> search(SearchParams params) async {
    searches.add(params);
    final Exception? failure = searchError;
    if (failure != null) throw failure;
    return searchResponse;
  }

  @override
  Future<ListingDetail> listing(String listingId) async {
    final Exception? failure = listingError;
    if (failure != null) throw failure;
    final ListingDetail? detail = listingDetail;
    if (detail == null) {
      throw const ApiException(
        statusCode: 404,
        code: 'listing.not_found',
        message: 'No listing with that id.',
      );
    }
    return detail;
  }

  @override
  Future<CursorPage<SearchItem>> myListings(ListingListParams params) async => listingsPage;

  @override
  Future<CursorPage<GarageBooking>> myBookings({String? cursor, int? limit}) async => bookingsPage;

  @override
  Future<ValuationQuote> valuationQuote(
    ValuationQuoteRequest request, {
    required String idempotencyKey,
  }) async {
    idempotencyKeys.add(idempotencyKey);
    final Exception? failure = quoteError;
    if (failure != null) throw failure;
    final ValuationQuote? answer = quote;
    if (answer == null) throw StateError('No quote scripted.');
    return answer;
  }

  @override
  Future<OtpChallenge> requestOtp(OtpRequest request, {required String idempotencyKey}) async {
    idempotencyKeys.add(idempotencyKey);
    final Exception? failure = authError;
    if (failure != null) throw failure;
    return challenge;
  }

  @override
  Future<AuthSession> verifyOtp(OtpVerifyRequest request, {required String idempotencyKey}) async {
    idempotencyKeys.add(idempotencyKey);
    final Exception? failure = authError;
    if (failure != null) throw failure;
    final AuthSession? answer = session;
    if (answer == null) throw StateError('No session scripted.');
    return answer;
  }

  @override
  Future<AuthUser> me() async {
    final AuthUser? answer = user;
    if (answer == null) {
      throw const ApiException(
        statusCode: 401,
        code: 'identity.token.expired',
        message: 'Sign in again.',
      );
    }
    return answer;
  }

  @override
  Future<void> logout(String refreshToken) async {}
}

/// Registry documents, from memory. Anything not seeded behaves like a network that is not there,
/// which is what `RegistryClient` treats as "keep what is on disk".
class FakeRegistryTransport implements RegistryTransport {
  FakeRegistryTransport(this.documents);

  /// Everything the shell warms at launch, so a pumped app is not fighting six missing documents.
  factory FakeRegistryTransport.withDefaults() => FakeRegistryTransport(<String, Map<String, Object?>>{
        RegistryDocuments.features.name: featuresJson,
        RegistryDocuments.cities.name: citiesJson,
        RegistryDocuments.form('sell_vehicle').name: sellFormJson,
        RegistryDocuments.i18n('en-IN').name: i18nJson,
      });

  final Map<String, Map<String, Object?>> documents;

  /// Every document the app asked for. Proves the sell form is fetched by key rather than built.
  final List<String> requested = <String>[];

  int version = 1;

  @override
  Future<RegistryFetch> fetch(RegistryKey key, {String? ifNoneMatch}) async {
    requested.add(key.name);
    final Map<String, Object?>? body = documents[key.name];
    if (body == null) {
      throw const ApiException(
        statusCode: 0,
        code: ApiErrorCodes.unreachable,
        message: 'Cannot reach AutoHub.',
      );
    }
    if (ifNoneMatch != null && ifNoneMatch == 'v$version') {
      return RegistryNotModified(etag: ifNoneMatch);
    }
    return RegistryFresh(version: version, etag: 'v$version', body: body);
  }
}

/// The write seam, scripted per attempt.
class FakeOutboxTransport implements OutboxTransport {
  FakeOutboxTransport([List<WriteOutcome>? script]) : _script = <WriteOutcome>[...?script];

  final List<WriteOutcome> _script;

  /// Used once the script runs out. Accepting by default keeps the happy path uncluttered.
  WriteOutcome fallback = const WriteAccepted(statusCode: 201);

  /// Every intent that reached the wire, in order — **including replays**. This is the list the
  /// idempotency test asserts on: the second attempt must carry the first attempt's key.
  final List<WriteIntent> sent = <WriteIntent>[];

  void script(List<WriteOutcome> outcomes) {
    _script
      ..clear()
      ..addAll(outcomes);
  }

  @override
  Future<WriteOutcome> send(WriteIntent intent) async {
    sent.add(intent);
    return _script.isEmpty ? fallback : _script.removeAt(0);
  }
}
