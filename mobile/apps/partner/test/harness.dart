import 'package:autohub_partner/src/app/partner_text.dart';
import 'package:autohub_partner/src/data/partner_providers.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// The smallest tree a partner screen needs.
///
/// Two things it does on purpose.
///
/// **Compact density, always.** This is the partner binary's density and it is not a detail — the
/// row heights, the button heights and the field padding under test are the ones that ship. A test
/// that pumped the comfortable theme would be testing the dealer app's layout.
///
/// **`partnerTextProvider` is overridden with an empty resolver.** That short-circuits the whole
/// registry chain — SQLite, the cache, the transport — which none of these tests is about, and it
/// exercises the fallback bundle, which is what a partner actually sees on a first cold launch in a
/// basement. Every assertion below therefore reads against strings that ship in the binary rather
/// than strings a fixture invented.
Widget partnerHarness(Widget child, {List<Override> overrides = const <Override>[]}) {
  return ProviderScope(
    overrides: <Override>[
      partnerTextProvider.overrideWithValue(const PartnerText(null)),
      ...overrides,
    ],
    child: MaterialApp(
      theme: AhTheme.light(density: AhDensity.compact),
      darkTheme: AhTheme.dark(density: AhDensity.compact),
      home: Scaffold(body: child),
    ),
  );
}
