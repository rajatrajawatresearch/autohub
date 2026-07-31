import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'src/app/customer_app.dart';
import 'src/data/app_providers.dart';

/// The composition root.
///
/// Everything asynchronous that must happen before the graph exists happens here, and only that:
/// the device id and the stored session are both keychain reads, and an `ApiClient` built without
/// them sends its first request unauthenticated and with no `X-Device-Id` — which the server
/// correctly treats as an unbound refresh, and which the user experiences as being signed out at
/// random, weeks later.
///
/// Everything else — warming the registries, starting the drain loop, purging stale drafts — runs
/// *after* the first frame, in `CustomerApp`. A launch that waits on the network is a launch that
/// hangs in a basement, and this app's whole offline story is that it opens with what it already
/// has.
Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Opened lazily: `ahDatabaseFile` defers `path_provider` and the file itself until the first
  // query, so nothing here touches the disk on the launch path.
  final AhDatabase database = openAhDatabase();

  const SecureKeyValueStore secure = KeychainStore();
  final String deviceId = await DeviceIdentity(secure).ensure();
  final SessionTokenStore tokens = await SessionTokenStore.open(const SessionStore(secure));

  final ApiClient api = ApiClient(
    deviceId: deviceId,
    tokens: tokens,
    // `X-Client` per app, so a broken release can be identified in the server's logs without
    // anybody having to guess which binary the user is on.
    config: const ApiConfig(clientId: 'customer/0.1.0'),
  );

  runApp(
    ProviderScope(
      overrides: <Override>[
        ahDatabaseProvider.overrideWithValue(database),
        secureStoreProvider.overrideWithValue(secure),
        sessionTokenStoreProvider.overrideWithValue(tokens),
        apiClientProvider.overrideWithValue(api),
        // The two seams `autohub_core` declares and refuses to implement, so that the registry
        // cache and the write queue stay testable without a socket. This is the only place in the
        // binary that knows they are dio.
        registryTransportProvider.overrideWithValue(DioRegistryTransport(api)),
        outboxTransportProvider.overrideWithValue(DioOutboxTransport(api)),
      ],
      child: const CustomerApp(),
    ),
  );
}
