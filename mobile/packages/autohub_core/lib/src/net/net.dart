/// The transport. The only directory in this package that imports dio.
///
/// The two adapters here — `DioRegistryTransport` and `DioOutboxTransport` — exist so that the
/// registry cache and the offline queue can be tested without a socket. Both of those components
/// are interesting precisely when the network misbehaves, and a test that needs a real network to
/// make the network misbehave is a test that gets deleted.
library;

export 'api_client.dart';
export 'api_config.dart';
export 'api_exception.dart';
export 'auth_tokens.dart';
export 'dio_outbox_transport.dart';
export 'dio_registry_transport.dart';
export 'session_token_store.dart';
