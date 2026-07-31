import 'package:autohub_core/autohub_core.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/partner_providers.dart';
import 'partner_side.dart';

/// The composition root.
///
/// Everything asynchronous that must happen before the first frame happens here, once, and the
/// results are handed to Riverpod as overrides. Two things in particular cannot be lazy:
///
/// * **The device id.** It comes from the keychain and binds refresh tokens. Minting one per launch
///   would unbind every refresh token on every cold start and log the partner out weekly, which
///   they would experience as the app randomly forgetting them.
/// * **The session.** `SessionTokenStore.open` hydrates the stored tokens so the transport can read
///   an access token synchronously while building headers. An `ApiClient` built against an
///   unhydrated store sends its first request unauthenticated and takes a `401` for a session that
///   was perfectly good.
///
/// The database is *not* awaited: `openAhDatabase` defers `path_provider`, the old-Android sqlite
/// workaround and opening the file until the first query, which is what keeps this function short
/// enough that there is no splash screen to look at on a cheap handset.
Future<List<Override>> bootstrapPartner({
  ApiConfig config = const ApiConfig(clientId: 'partner/0.1.0'),
}) async {
  const KeychainStore secure = KeychainStore();

  final AhDatabase database = openAhDatabase();
  final DevicePreferences preferences = DevicePreferences(database);

  final SessionTokenStore tokens = await SessionTokenStore.open(const SessionStore(secure));
  final String deviceId = await DeviceIdentity(secure).ensure();

  // The side is read before the first frame so a returning partner never sees the chooser flash.
  final PartnerSide? side =
      PartnerSide.tryParse(await preferences.read(PartnerSide.storageKey));

  // `X-Active-City` scopes search, pricing and feature evaluation server-side. It is folded into
  // the transport config here rather than attached per call, so a screen cannot forget it — and the
  // outbox still freezes its own copy at gesture time, which is what makes a write drained after a
  // city switch belong to the city it was made in.
  final String? city = await preferences.activeCity();

  final ApiClient client = ApiClient(
    deviceId: deviceId,
    tokens: tokens,
    config: city == null ? config : config.copyWith(defaultCity: city),
  );

  return <Override>[
    ahDatabaseProvider.overrideWithValue(database),
    secureStoreProvider.overrideWithValue(secure),
    // Already resolved above; overriding saves every consumer a keychain round trip on a platform
    // channel that is not free on a mid-range Android device.
    deviceIdProvider.overrideWith((ref) => deviceId),
    apiClientProvider.overrideWithValue(client),
    // The two seams `autohub_core` declares and refuses to implement, because the cache and the
    // queue must both be testable without a socket.
    registryTransportProvider.overrideWithValue(DioRegistryTransport(client)),
    outboxTransportProvider.overrideWithValue(DioOutboxTransport(client)),
    initialPartnerSideProvider.overrideWithValue(side),
  ];
}
