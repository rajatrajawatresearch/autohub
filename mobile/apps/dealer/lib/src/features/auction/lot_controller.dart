import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/providers.dart';
import '../../data/auction_feed.dart';

/// Everything the lot screen renders, as one immutable value.
///
/// The lot, the clock and [seenBidSeq] travel together deliberately. They are the three things that
/// must agree with each other, and separating them into three providers is how a screen ends up
/// rendering a fresh price against a stale clock, or bidding with a sequence number that describes
/// a figure the dealer never saw.
@immutable
final class LotView {
  const LotView({
    required this.lot,
    required this.clock,
    required this.seenBidSeq,
    required this.feed,
    this.closedByFeed = false,
  });

  final AuctionLot lot;

  /// Measured on the request that fetched [lot]. The countdown renders against this and never
  /// against `DateTime.now()`.
  final AhServerClock clock;

  /// **The sequence whose effect is on the screen right now.**
  ///
  /// This is the number that travels with a bid, and its exact meaning is the point of §6.2. It is
  /// not the highest sequence received and it is not the lot's `latestSeq` as most recently
  /// fetched — it is the sequence of the state a dealer is looking at. Because it lives in this
  /// record beside the lot it describes, the two cannot drift: any change that alters the figure on
  /// screen produces a new [LotView] with both fields moved together, and any event that alters
  /// nothing leaves both alone.
  ///
  /// The consequence is the one the architecture wants. An event this build cannot interpret
  /// advances the feed's *reconnect anchor* but not this number, so the next bid is composed
  /// against a figure the server will recognise as stale — and rejects as an outbid race, which is
  /// correct, because the dealer really has not seen whatever that event carried.
  final int seenBidSeq;

  final AuctionFeedStatus feed;

  /// The socket said the lot closed and the refetch has not landed yet.
  ///
  /// A client-side fact, not a workflow state. Nothing here fabricates a `WorkflowState` named
  /// "Sold" with a category chosen locally — those words belong to the workflow registry, and a
  /// console that invents them is a console that disagrees with the deals screen about the same
  /// lot. It only ever *disables* bidding, which is the safe direction to be wrong in.
  final bool closedByFeed;

  bool get isClosed => closedByFeed || lot.state.isTerminal;

  /// The close time as the server stated it, or null if it will not parse. Null disables the
  /// countdown rather than defaulting to now — a lot whose end time is unreadable must not render
  /// as one second from closing.
  DateTime? get endsAt => DateTime.tryParse(lot.endsAt);

  LotView copyWith({
    AuctionLot? lot,
    AhServerClock? clock,
    int? seenBidSeq,
    AuctionFeedStatus? feed,
    bool? closedByFeed,
  }) =>
      LotView(
        lot: lot ?? this.lot,
        clock: clock ?? this.clock,
        seenBidSeq: seenBidSeq ?? this.seenBidSeq,
        feed: feed ?? this.feed,
        closedByFeed: closedByFeed ?? this.closedByFeed,
      );
}

/// One lot's live state.
///
/// The socket is **read-only** and this class is where that is enforced in practice: it folds
/// broadcast events into [LotView] and has no method that sends anything. Bids leave through
/// `BidClient` over HTTP with an `Idempotency-Key`, and the only way a bid touches this controller
/// is [applyServerLot] — which takes the lot *the server returned*, never a figure composed here.
class LotController extends AutoDisposeFamilyAsyncNotifier<LotView, String> {
  AuctionFeed? _feed;
  StreamSubscription<AuctionFeedMessage>? _messages;
  bool _disposed = false;

  @override
  Future<LotView> build(String lotId) async {
    _disposed = false;
    ref.onDispose(_teardown);

    final clocked = await ref.read(dealerApiProvider).auctionLot(lotId);
    final lot = clocked.value;

    _listen(lotId, sinceSeq: lot.latestSeq);

    return LotView(
      lot: lot,
      clock: clocked.clock,
      // The state about to be rendered *is* the state at `latestSeq`. Starting anywhere else would
      // either send a sequence the dealer has not seen, or refuse every first bid as stale.
      seenBidSeq: lot.latestSeq,
      feed: AuctionFeedStatus.connecting,
    );
  }

  /// Pull to refresh, and the recovery path after any rejection.
  ///
  /// Refetches the lot **and** re-measures the clock, because the two are only trustworthy
  /// together. Resets `seenBidSeq` to the sequence of the state that is about to be painted.
  Future<void> refresh() async {
    final lotId = arg;
    state = const AsyncLoading<LotView>().copyWithPrevious(state);
    state = await AsyncValue.guard(() async {
      final clocked = await ref.read(dealerApiProvider).auctionLot(lotId);
      final previous = state.valueOrNull;
      return LotView(
        lot: clocked.value,
        clock: clocked.clock,
        seenBidSeq: clocked.value.latestSeq,
        feed: previous?.feed ?? AuctionFeedStatus.closed,
        // The server is the authority on whether the lot is closed. A refetch clears a
        // socket-inferred close rather than keeping it, because `lot.state` now says.
        closedByFeed: false,
      );
    });
  }

  /// Installs the lot the server returned from a successful bid.
  ///
  /// The only way the bid figure ever changes as a result of the dealer's own action, and it takes
  /// the server's object wholesale. There is no path here that adds an increment to the last known
  /// price — showing a dealer a bid as leading before the server agrees is showing them something
  /// that may be false about money.
  void applyServerLot(AuctionLot lot) {
    final current = state.valueOrNull;
    if (current == null || _disposed) return;
    state = AsyncData<LotView>(
      current.copyWith(lot: lot, seenBidSeq: lot.latestSeq),
    );
  }

  void _listen(String lotId, {required int sinceSeq}) {
    _feed = ref.read(auctionFeedFactoryProvider).open(
          lotId,
          sinceSeq: sinceSeq,
          accessToken: ref.read(accessTokenProvider),
        );
    _messages = _feed!.messages.listen(_onMessage);
  }

  void _onMessage(AuctionFeedMessage message) {
    final current = state.valueOrNull;
    if (current == null || _disposed) return;

    switch (message) {
      case AuctionFeedStatusChanged(:final status):
        state = AsyncData<LotView>(current.copyWith(feed: status));
      case AuctionFeedEvent(:final event):
        final next = foldAuctionEvent(current, event);
        if (next == null) return;
        state = AsyncData<LotView>(next);

        // The *words* for a closed lot belong to the workflow registry, so the fold marks bidding
        // shut and the refetch goes and gets the real state. Fired rather than awaited: the dealer
        // must not be able to bid during the second it takes, and the fold has already stopped them.
        if (event is AuctionLotClosed) unawaited(refresh());
    }
  }

  void _teardown() {
    _disposed = true;
    unawaited(_messages?.cancel());
    _messages = null;
    unawaited(_feed?.close());
    _feed = null;
  }
}

/// Folds one broadcast frame into the rendered state, or returns null when it changes nothing.
///
/// A pure function, deliberately outside the controller: this is the piece of the auction screen
/// where being wrong costs a dealer money, and it must be testable without a socket, a network or a
/// widget. Everything it needs is its two arguments.
///
/// Exhaustive over the event union. A seventh event type is a compile error here, which is the
/// entire reason `AuctionEvent` is sealed — a new kind of frame must force a decision about what
/// the dealer has and has not seen, rather than falling through a `default` into silence.
LotView? foldAuctionEvent(LotView current, AuctionEvent event) {
  // `seq` is monotonic per lot. A replay after a reconnect carries figures the dealer has already
  // seen, and re-applying it would move `seenBidSeq` backwards on a live screen.
  if (event.seq <= current.seenBidSeq) return null;

  switch (event) {
    case AuctionBidPlaced(:final seq, :final data):
      return current.copyWith(
        lot: current.lot.copyWith(
          latestSeq: seq,
          currentBidMinor: data.amountMinor,
          // The server's own next-legal-bid wins. The local sum is a fallback for a broadcast that
          // omitted it, and it uses the lot's declared increment rather than a guess — the ladder
          // can carry slab rules this client has never been told about.
          minNextBidMinor: data.minNextBidMinor ?? data.amountMinor + current.lot.incrementMinor,
          bidCount: data.bidCount ?? current.lot.bidCount + 1,
          highestBidderIsYou: data.highestBidderIsYou,
        ),
        // Moved in the same breath as the figure it describes. That simultaneity is the invariant
        // `seenBidSeq` depends on.
        seenBidSeq: seq,
      );

    case AuctionLotExtended(:final seq, :final data):
      return current.copyWith(
        lot: current.lot.copyWith(
          latestSeq: seq,
          endsAt: data.endsAt,
          extensionCount: current.lot.extensionCount + 1,
        ),
        seenBidSeq: seq,
      );

    case AuctionLotClosed(:final seq):
      return current.copyWith(
        lot: current.lot.copyWith(latestSeq: seq),
        seenBidSeq: seq,
        closedByFeed: true,
      );

    case AuctionUnknownEvent():
      // **Nothing was rendered, so `seenBidSeq` does not move.** If this frame was in fact a bid,
      // the dealer's next bid is composed against a sequence the server will read as stale and
      // rejects as an outbid race — after which this screen refreshes and shows them the real
      // figure. That is the correct outcome, because they genuinely have not seen it. Advancing the
      // guard here to "keep the numbers tidy" would let them commit money to a price they never
      // saw. The feed has already advanced its own reconnect anchor, which is a different number
      // for a different job.
      return null;
  }
}

final AutoDisposeAsyncNotifierProviderFamily<LotController, LotView, String> lotProvider =
    AsyncNotifierProvider.autoDispose.family<LotController, LotView, String>(
  LotController.new,
);
