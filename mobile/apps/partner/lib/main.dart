import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'src/app/bootstrap.dart';
import 'src/app/partner_app.dart';

/// The taxi attachment centre, on a phone.
///
/// This is the module with no equivalent on Cars24 or Spinny: a two-sided matching market where an
/// owner publishes earning capacity, an operator publishes a requirement, and the platform takes a
/// cut of every cycle for as long as the match lives. Everything that makes it work — the contract
/// types, the workflow states, the fee components, the SLA clauses, the deduction order, the
/// compliance thresholds — is a registry row, and none of it is spelled in this binary.
Future<void> main() async {
  // Required before `path_provider` and `flutter_secure_storage` can be touched, and bootstrap
  // touches both.
  WidgetsFlutterBinding.ensureInitialized();

  runApp(
    ProviderScope(
      overrides: await bootstrapPartner(),
      child: const PartnerApp(),
    ),
  );
}
