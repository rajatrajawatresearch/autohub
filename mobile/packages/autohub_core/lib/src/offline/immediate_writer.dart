import 'connectivity.dart';
import 'write_intent.dart';
import 'write_outcome.dart';

/// The escape hatch: a write that meets the network now or fails in front of the user.
///
/// ## Why bids are never queued
///
/// An auction lot is a moving target with a deadline. The queue's entire value proposition —
/// *your write will land eventually* — is worthless against it and actively harmful:
///
/// - **Late is not better than never.** A bid drained ninety seconds after the tap is a bid on a
///   lot that has closed, or one whose price has moved three increments past the figure the dealer
///   agreed to in the confirmation sheet. The server rejects it as an outbid race, so the queue
///   spent the delay to produce a failure the dealer could have seen immediately — except now they
///   have stopped watching the lot, because as far as they knew the bid was placed.
/// - **Silence reads as success.** A dealer who taps *Place bid* and sees no error assumes they
///   are in. On a queued path they are not in; they are in a row in SQLite. They stop bidding, and
///   they lose the truck without ever seeing a failure.
/// - **`seenBidSeq` goes stale.** Per §6.2 a bid carries the highest sequence the screen actually
///   rendered. Held in a queue, that number describes an auction that has since moved on, and the
///   only correct thing the server can do with it is refuse the bid.
///
/// So a bid fails loudly and immediately, and the dealer — who is looking at the screen, because
/// they just tapped it — retries against a price they can see.
///
/// The `Idempotency-Key` still matters and is still minted at gesture time. Not queueing is not
/// the same as not deduplicating: a thumb that bounces on *Place bid* over showroom wifi must
/// produce one bid, and the replayed POST carries the same key so the server returns the original
/// response instead of bidding twice.
///
/// This is also the right path for the handful of other writes where queueing is meaningless
/// rather than dangerous — verifying an OTP whose challenge expires in five minutes, refreshing a
/// token — and it is deliberately the *only* other path, so that "does this write queue?" has one
/// answer per call site rather than a flag somebody can flip.
final class ImmediateWriter {
  const ImmediateWriter(this._transport, {ReachabilityReporter? reachability})
      : _reachability = reachability;

  final OutboxTransport _transport;
  final ReachabilityReporter? _reachability;

  /// Sends [intent] once. No retry, no persistence, no fallback into the queue.
  ///
  /// Returns the outcome rather than throwing, because the caller has a real decision to make for
  /// each one and a `try`/`catch` flattens them: an outbid `409` shows the new price and the next
  /// valid increment, while a `WriteUnreachable` shows "no signal — the lot is still open" and
  /// keeps the countdown running.
  Future<WriteOutcome> send(WriteIntent intent) async {
    final outcome = await _transport.send(intent);

    // The bid path is often the first thing to notice an outage, because it is the one screen a
    // dealer keeps open. Reporting it here is what lets the write queue start draining the moment
    // the showroom wifi comes back, without waiting for the sweep timer.
    switch (outcome) {
      case WriteUnreachable():
        _reachability?.recordUnreachable();
      case WriteAccepted() ||
            WriteConflicted() ||
            WriteRejected() ||
            WriteUnauthenticated() ||
            WriteFailedServer():
        _reachability?.recordReachable();
    }

    return outcome;
  }

  /// The mirror of the outbox's refusal to queue an immediate write: an intent captured as
  /// queueable has no business being force-sent through here, because somewhere upstream a caller
  /// decided it was safe to lose to a tunnel and this path will lose it.
  Future<WriteOutcome> sendStrict(WriteIntent intent) {
    if (intent.isQueueable) throw NotSendableImmediatelyError(intent);
    return send(intent);
  }
}

/// Thrown by [ImmediateWriter.sendStrict] for a write that was captured as queueable.
final class NotSendableImmediatelyError extends Error {
  NotSendableImmediatelyError(this.intent);

  final WriteIntent intent;

  @override
  String toString() =>
      'NotSendableImmediatelyError: ${intent.method.wire} ${intent.endpoint} was captured as '
      'WriteDurability.queued. Capture it with WriteIntent.immediateNow if losing it to a tunnel '
      'is the correct behaviour.';
}
