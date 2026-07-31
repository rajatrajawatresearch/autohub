import 'dart:async';

/// Whether the app currently believes it can reach AutoHub.
///
/// The queue drains on the rising edge of this signal, which is the difference between a listing
/// that appears the moment a train leaves a tunnel and one that appears whenever the user next
/// happens to open the app.
abstract interface class ConnectivityMonitor {
  bool get isOnline;

  /// Emits on every transition, and only on transitions. A drain triggered by a stream that
  /// re-emits its current value would run on every rebuild.
  Stream<bool> get changes;
}

/// Fed by whatever actually talks to the network.
///
/// Separated from [ConnectivityMonitor] so the drain loop can report evidence without being handed
/// a concrete implementation — an app that swaps in a platform plugin implements the monitor and
/// may ignore this half entirely.
abstract interface class ReachabilityReporter {
  /// A request got an answer from the server, whatever that answer was.
  void recordReachable();

  /// A request never made it out. Not a rejection — an absence.
  void recordUnreachable();
}

/// Reachability inferred from what actually happened to requests.
///
/// The platform APIs — and the plugins over them — answer a different question from the one that
/// matters. `connectivity_plus` reports that a wifi interface is associated; it cannot tell you
/// that the hotel portal is intercepting every request, that the SIM has no data balance left, or
/// that AutoHub itself is unreachable while the rest of the internet is fine. All three are
/// ordinary in this market, and all three look like "connected" to the OS.
///
/// The only ground truth about reachability is a request that reached. So this implementation is
/// fed by the drain loop: a [WriteUnreachable] outcome is evidence of an outage, and any response
/// at all — including a `500` — is proof the network works. That makes the signal *lagging* rather
/// than predictive, which is why [OutboxDrainer] also polls on a slow timer instead of relying on
/// this alone.
///
/// An app that adds a platform connectivity plugin should implement [ConnectivityMonitor] over it
/// and override the provider; the plugin's edges are a better *trigger*, and this remains the
/// better *truth*.
final class OutcomeDrivenConnectivity implements ConnectivityMonitor, ReachabilityReporter {
  /// Starts optimistic. A first launch has no evidence either way, and assuming an outage means
  /// the app refuses to try — which produces the outage it assumed.
  OutcomeDrivenConnectivity({bool assumeOnline = true}) : _online = assumeOnline;

  bool _online;
  final StreamController<bool> _changes = StreamController<bool>.broadcast();

  @override
  bool get isOnline => _online;

  @override
  Stream<bool> get changes => _changes.stream;

  @override
  void recordReachable() => _set(true);

  @override
  void recordUnreachable() => _set(false);

  Future<void> dispose() => _changes.close();

  void _set(bool next) {
    if (_online == next || _changes.isClosed) return;
    _online = next;
    _changes.add(next);
  }
}
