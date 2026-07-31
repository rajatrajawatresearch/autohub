import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'src/app/bootstrap.dart';
import 'src/app/dealer_app.dart';

/// The dealer console's entry point.
///
/// Everything interesting happens in `bootstrapDealer`, and it happens *before* `runApp` for the
/// reason that function documents: the transport needs a hydrated session and a stable device id
/// synchronously, and a provider that resolved them lazily would send the first request of every
/// cold launch unauthenticated.
Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  final runtime = await bootstrapDealer();

  runApp(
    ProviderScope(
      overrides: runtime.overrides,
      child: const DealerApp(),
    ),
  );
}
