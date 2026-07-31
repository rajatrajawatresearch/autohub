import 'dart:async';
import 'dart:io';

import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

/// Loads the real fonts before any test in this package runs.
///
/// `flutter_test` ships a single placeholder font that draws every glyph as a filled box, so a
/// golden taken without this is a picture of rectangles — technically a regression test, useless as
/// a design review, and actively misleading if anyone opens it expecting to see the product.
///
/// Discovered by looking at one: the first design-sheet golden came out as red and black blocks,
/// which is also how the missing font declaration in `pubspec.yaml` was found. The fonts were
/// referenced by the design system and bundled by nobody.
Future<void> testExecutable(FutureOr<void> Function() testMain) async {
  TestWidgetsFlutterBinding.ensureInitialized();

  const Map<String, String> families = <String, String>{
    'Archivo': 'assets/fonts/Archivo-Variable.ttf',
    'InstrumentSans': 'assets/fonts/InstrumentSans-Variable.ttf',
    'JetBrainsMono': 'assets/fonts/JetBrainsMono-Variable.ttf',
  };

  for (final MapEntry<String, String> family in families.entries) {
    final File file = File(family.value);
    if (!file.existsSync()) continue;

    final FontLoader loader = FontLoader(family.key)
      ..addFont(Future<ByteData>.value(ByteData.sublistView(file.readAsBytesSync())));
    await loader.load();
  }

  await testMain();
}
