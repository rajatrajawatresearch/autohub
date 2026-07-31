import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// What happened to a bid.
///
/// Sealed, because the panel must do something **different** for every one of them and the analyzer
/// must refuse to compile a handler that forgets one. Collapsing these into "worked / did not work"
/// is how an outbid race gets shown as a network error, and a dealer retries a bid they have
/// already lost instead of reading the new price.
sealed class BidResult {
  const BidResult();
}

/// The server accepted it and returned the lot as it now stands.
///
/// The lot in [response] is what the screen renders. There is no optimistic path anywhere near
/// this type: until the server says a bid leads, the client does not say it either.
@immutable
final class BidPlaced extends BidResult {
  const BidPlaced(this.response);

  final PlaceBidResponse response;
}

/// `409`. The lot moved while the dealer was composing — usually `auction.bid.outbid_race`.
///
/// **Never retried automatically.** The figure the dealer agreed to in the confirmation sheet is no
/// longer the figure on the ladder, so re-sending it would commit them to a number they did not
/// read. The screen refreshes the truth and lets them decide again.
@immutable
final class BidStale extends BidResult {
  const BidStale({required this.code, this.detail, this.currentState});

  final String code;
  final String? detail;

  /// The server's view of the lot right now, verbatim from `problem.metadata.currentState`.
  final Map<String, dynamic>? currentState;
}

/// `4xx` other than `401` and `409` — below the ladder step, KYC incomplete, lot closed.
///
/// Wrong now and wrong in a minute. Retrying spends the dealer's battery to learn nothing.
@immutable
final class BidRefused extends BidResult {
  const BidRefused({required this.statusCode, required this.code, this.detail});

  final int statusCode;
  final String code;
  final String? detail;
}

/// `5xx` or `429`. Might not be true in a minute, so the panel offers a retry — but the *dealer*
/// presses it, because a client that re-sends a bid nobody asked it to re-send is a client making a
/// decision about somebody else's money.
@immutable
final class BidServerFailure extends BidResult {
  const BidServerFailure({required this.statusCode, required this.code, this.retryAfter});

  final int statusCode;
  final String code;
  final Duration? retryAfter;
}

/// `401` after the transport already spent its one refresh. The bid is valid; the session is not.
@immutable
final class BidNeedsSignIn extends BidResult {
  const BidNeedsSignIn(this.code);

  final String code;
}

/// The request never reached a server.
///
/// **The bid was not placed, and it is not waiting anywhere.** This is the loud, immediate failure
/// §4 of `docs/10-mobile-architecture.md` requires, and it exists because the alternative is worse
/// in a way the dealer cannot detect: a queued bid drains ninety seconds later onto a lot that has
/// closed at a price three increments past the figure they agreed to, and in the meantime they
/// stopped watching, because as far as they knew they were in.
@immutable
final class BidUnsent extends BidResult {
  const BidUnsent(this.code);

  final String code;
}

/// The only path a bid takes.
///
/// Three properties, each of which is the reason this is a class rather than a call to `post`:
///
/// 1. **It cannot queue.** `sendStrict` throws on a queueable intent, and the intent is built with
///    `WriteIntent.immediateNow`. There is no flag to flip and no second code path.
/// 2. **The `Idempotency-Key` is minted on the gesture**, inside the intent's constructor, at the
///    moment [place] is called from the confirmation sheet — not at send time. A thumb that bounces
///    on *Place bid* over showroom wifi produces one bid, because the replayed POST carries the
///    same key and the server returns the original response.
/// 3. **`seenBidSeq` travels with every bid.** It is the sequence the screen had actually rendered
///    when the dealer confirmed, so a bid composed against a stale figure is rejected server-side
///    as an outbid race rather than quietly accepted at the wrong ladder step.
final class BidClient {
  const BidClient(this._writer);

  final ImmediateWriter _writer;

  /// Places one bid. Called directly from the confirmation sheet's confirm handler — the intent,
  /// and therefore the idempotency key, is born on that tap.
  ///
  /// [seenBidSeq] must be the highest sequence whose effect the dealer has **seen on screen**. Not
  /// the highest received, and not the lot's `latestSeq` as most recently fetched: a figure they
  /// never saw is a figure they did not agree to.
  Future<BidResult> place({
    required String lotId,
    required int amountMinor,
    required int seenBidSeq,
    int? maxAmountMinor,
    String? activeCity,
  }) async {
    final intent = WriteIntent.immediateNow(
      endpoint: '/auction/lots/${Uri.encodeComponent(lotId)}/bids',
      method: WriteMethod.post,
      body: <String, dynamic>{
        'amountMinor': amountMinor,
        'seenBidSeq': seenBidSeq,
        if (maxAmountMinor != null) 'maxAmountMinor': maxAmountMinor,
      },
      headers: <String, String>{
        // Frozen at the gesture, like every other write. A bid belongs to the city the dealer was
        // bidding from, and the pricing rules that will settle it are that city's.
        if (activeCity != null && activeCity.isNotEmpty) 'X-Active-City': activeCity,
      },
    );

    final outcome = await _writer.sendStrict(intent);

    return switch (outcome) {
      WriteAccepted(:final body) => _placed(body),
      WriteConflicted(:final code, :final detail, :final currentState) =>
        BidStale(code: code, detail: detail, currentState: currentState),
      WriteRejected(:final statusCode, :final code, :final detail) =>
        BidRefused(statusCode: statusCode, code: code, detail: detail),
      WriteUnauthenticated(:final code) => BidNeedsSignIn(code),
      WriteFailedServer(:final statusCode, :final code, :final retryAfter) =>
        BidServerFailure(statusCode: statusCode, code: code, retryAfter: retryAfter),
      WriteUnreachable(:final reason) => BidUnsent(reason),
    };
  }

  /// A `2xx` whose body does not decode is still a bid the server acted on — the money moved.
  ///
  /// So this reports a refusal the screen will react to by refetching the lot, rather than a
  /// success it cannot substantiate. Guessing the new state from the amount just sent would be the
  /// optimistic render this whole path exists to avoid.
  static BidResult _placed(Map<String, dynamic>? body) {
    if (body == null) {
      return const BidRefused(
        statusCode: 200,
        code: ApiErrorCodes.malformedResponse,
        detail: null,
      );
    }
    try {
      return BidPlaced(PlaceBidResponse.fromJson(body));
    } on Object {
      return const BidRefused(
        statusCode: 200,
        code: ApiErrorCodes.malformedResponse,
        detail: null,
      );
    }
  }
}
