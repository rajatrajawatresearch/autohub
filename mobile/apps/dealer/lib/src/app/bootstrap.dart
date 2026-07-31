import 'package:autohub_core/autohub_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'providers.dart';

/// What the composition root built, ready to hand to a `ProviderScope`.
final class DealerRuntime {
  const DealerRuntime({required this.overrides, required this.api});

  final List<Override> overrides;

  /// Held so a future sign-out path can close the sockets deliberately rather than leaving them to
  /// the process.
  final ApiClient api;
}

/// Builds the object graph before the first frame.
///
/// **Why this happens in `main()` and not lazily in a provider.** Two of the leaves are
/// asynchronous — the device id and the session both come from the keychain, which is a platform
/// channel — and both are needed *synchronously* on the very first request. An `ApiClient` built
/// against an unhydrated token store sends its opening call unauthenticated and takes a `401` for a
/// session that was perfectly good, which the dealer experiences as the app randomly signing them
/// out on launch. Resolving them up front costs one channel hop at start-up and removes that class
/// of bug entirely.
///
/// The leaves are also *supplied back* into the graph rather than reconstructed by it. Letting
/// `secureStoreProvider` build a second `KeychainStore` would work — it is stateless — but
/// `DeviceIdentity` memoises, and two instances would mean two reads and, on a first launch, two
/// generated device ids racing for the same keychain slot.
Future<DealerRuntime> bootstrapDealer({
  ApiConfig config = const ApiConfig(clientId: 'dealer/0.1.0'),
}) async {
  const SecureKeyValueStore secure = KeychainStore();
  const SessionStore sessions = SessionStore(secure);
  final DeviceIdentity identity = DeviceIdentity(secure);

  final String deviceId = await identity.ensure();
  final SessionTokenStore tokens = await SessionTokenStore.open(sessions);

  final ApiClient api = ApiClient(deviceId: deviceId, config: config, tokens: tokens);

  return DealerRuntime(
    api: api,
    overrides: <Override>[
      secureStoreProvider.overrideWithValue(secure),
      sessionStoreProvider.overrideWithValue(sessions),
      deviceIdentityProvider.overrideWithValue(identity),
      apiClientProvider.overrideWithValue(api),
      // The two seams `autohub_core` declares and refuses to implement, so that the registry cache
      // and the write queue stay testable without a socket.
      registryTransportProvider.overrideWithValue(DioRegistryTransport(api)),
      outboxTransportProvider.overrideWithValue(DioOutboxTransport(api)),
    ],
  );
}
