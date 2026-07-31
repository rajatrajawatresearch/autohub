import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_dealer/src/app/providers.dart';
import 'package:autohub_dealer/src/app/strings.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// The smallest tree a dealer widget needs, at the density the dealer binary actually runs in.
///
/// [localisationProvider] is overridden rather than left to resolve, because reading it for real
/// would walk to the registry client and open SQLite — which is exactly the kind of accidental
/// platform dependency that makes widget tests get deleted.
Widget dealerHarness(
  Widget child, {
  List<Override> overrides = const <Override>[],
  Brightness brightness = Brightness.light,
}) {
  return ProviderScope(
    overrides: <Override>[
      localisationProvider.overrideWith((ref) async => const DealerStrings(null)),
      // The real one reads device preferences out of SQLite. A widget test that opens a database
      // is a widget test that leaves a file behind and races the next one.
      activeCityProvider.overrideWith((ref) => Stream<String?>.value('pune')),
      ...overrides,
    ],
    child: MaterialApp(
      theme: brightness == Brightness.dark
          ? AhTheme.dark(density: AhDensity.compact)
          : AhTheme.light(density: AhDensity.compact),
      home: Scaffold(
        body: SingleChildScrollView(
          child: Padding(
            // 360dp is the narrow handset this design system targets and the width a dealer
            // actually holds one-handed on a showroom floor.
            padding: const EdgeInsets.all(AhSpace.md),
            child: child,
          ),
        ),
      ),
    ),
  );
}

/// Replaces the write path with one the test controls, and records what a bid put on the wire.
///
/// Overriding `immediateWriterProvider` rather than `bidClientProvider` is deliberate: it keeps the
/// real `BidClient` — including its refusal to queue and its `Idempotency-Key` minting — in the
/// test, so the assertions are about the code that ships.
Override immediateWriterOverride(WriteOutcome outcome, List<WriteIntent> sent) =>
    immediateWriterProvider.overrideWithValue(ImmediateWriter(_StubTransport(outcome, sent)));

class _StubTransport implements OutboxTransport {
  _StubTransport(this._outcome, this._sent);

  final WriteOutcome _outcome;
  final List<WriteIntent> _sent;

  @override
  Future<WriteOutcome> send(WriteIntent intent) async {
    _sent.add(intent);
    return _outcome;
  }
}
