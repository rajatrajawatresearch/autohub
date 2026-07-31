/// Transport, registries, auth, storage and the offline queue — the only package in this
/// workspace that knows a network exists.
///
/// Everything a client is forbidden to re-derive lives behind this barrel: the money formatter,
/// the exact-arithmetic money type, the closed contract enums, the JsonLogic evaluator, the
/// registry cache and the idempotent write queue. `autohub_ui` and `autohub_forms` consume these
/// and never reimplement them — two formatters that disagree about `₹8.4L`, or two JsonLogic
/// evaluators that disagree about a `visibleWhen`, are defects only a user will find.
library;

export 'src/contract/state_category.dart';
export 'src/format/indian_format.dart';
export 'src/logic/json_logic.dart';
// Reconciled: `autohub_forms` no longer declares its own `FormDefinition` / `FormField` /
// `ValidationRule`. There is one Dart definition of each contract shape in this workspace and it
// lives here, with the package that owns the wire. The renderer consumes them.
export 'src/models/models.dart';
export 'src/net/net.dart';
export 'src/offline/backoff.dart';
export 'src/offline/connectivity.dart';
export 'src/offline/draft_store.dart';
export 'src/offline/immediate_writer.dart';
export 'src/offline/outbox.dart';
export 'src/offline/outbox_drainer.dart';
export 'src/offline/write_intent.dart';
export 'src/offline/write_outcome.dart';
export 'src/providers/core_providers.dart';
export 'src/registry/registry_client.dart';
export 'src/registry/registry_key.dart';
export 'src/registry/registry_reads.dart';
export 'src/registry/registry_store.dart';
export 'src/registry/registry_transport.dart';
export 'src/storage/ah_database.dart';
export 'src/storage/device_identity.dart';
export 'src/storage/device_preferences.dart';
export 'src/storage/open_database.dart';
export 'src/storage/secure_store.dart';
export 'src/storage/session_store.dart';
export 'src/time/server_clock.dart';
export 'src/values/values.dart';
