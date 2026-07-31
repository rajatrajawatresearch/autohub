import 'dart:async';
import 'dart:convert';
import 'dart:io' show WebSocket;

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';

/// What the read-side connection is doing, as far as the UI needs to know.
///
/// A legal enum: this is the client's own transport state, not a registry row. It is surfaced
/// because a dealer bidding on a truck is entitled to know whether the figure in front of them is
/// live or the last thing that arrived before the wifi went — and because "the screen looked
/// current" is the story behind every disputed bid.
enum AuctionFeedStatus {
  connecting,

  /// Frames are arriving. The figure on screen is what the server last broadcast.
  open,

  /// Not connected. The lot is still bid-able over HTTP — the socket carries no bids — but the
  /// screen will not learn about someone else's bid until it is refreshed.
  closed,
}

/// One thing the feed has to say.
sealed class AuctionFeedMessage {
  const AuctionFeedMessage();
}

/// A broadcast frame, already decoded. Includes [AuctionUnknownEvent] — see `auction.dart` for why
/// an event this build cannot interpret must still reach the consumer.
@immutable
final class AuctionFeedEvent extends AuctionFeedMessage {
  const AuctionFeedEvent(this.event);

  final AuctionEvent event;
}

@immutable
final class AuctionFeedStatusChanged extends AuctionFeedMessage {
  const AuctionFeedStatusChanged(this.status);

  final AuctionFeedStatus status;
}

/// A live subscription to one lot.
abstract interface class AuctionFeed {
  Stream<AuctionFeedMessage> get messages;

  Future<void> close();
}

/// Opens feeds. An interface so the auction screens can be tested without a socket — which is the
/// whole point, because the interesting behaviour is what the screen does when the socket is *not*
/// there and a test that needs a real server to arrange that is a test nobody runs.
abstract interface class AuctionFeedFactory {
  /// [sinceSeq] is the reconnect anchor: the server replays the gap from there, per
  /// `contracts/registry-api.md` §11.
  AuctionFeed open(String lotId, {required int sinceSeq, String? accessToken});
}

/// The socket, and nothing else.
///
/// **It is read-only, and that is a correctness decision rather than a simplification.** A bid sent
/// over a socket cannot promise delivery exactly once: the frame leaves, the connection drops, and
/// neither end knows whether it landed. Bids therefore go to `POST /auction/lots/{id}/bids` with an
/// `Idempotency-Key`, where a replay is deduplicated server-side — so a dropped connection can
/// neither lose a bid nor place it twice. This class has no `send`, and there is nowhere to add one
/// without changing its interface.
///
/// Reconnection carries the highest sequence it has **forwarded**, including sequences it could not
/// interpret. Dropping an unknown frame's number would leave the client asking for a gap the server
/// has already sent, forever.
final class WebSocketAuctionFeedFactory implements AuctionFeedFactory {
  const WebSocketAuctionFeedFactory({
    required this.wsBaseUrl,
    this.backoff = const BackoffPolicy(),
  });

  /// Derived from `ApiConfig.wsBaseUrl`, so the socket and the API can never point at two
  /// different environments.
  final String wsBaseUrl;

  final BackoffPolicy backoff;

  @override
  AuctionFeed open(String lotId, {required int sinceSeq, String? accessToken}) =>
      _WebSocketAuctionFeed(
        wsBaseUrl: wsBaseUrl,
        lotId: lotId,
        sinceSeq: sinceSeq,
        accessToken: accessToken,
        backoff: backoff,
      );
}

class _WebSocketAuctionFeed implements AuctionFeed {
  _WebSocketAuctionFeed({
    required this.wsBaseUrl,
    required this.lotId,
    required int sinceSeq,
    required this.accessToken,
    required this.backoff,
  }) : _anchor = sinceSeq {
    unawaited(_run());
  }

  final String wsBaseUrl;
  final String lotId;
  final String? accessToken;
  final BackoffPolicy backoff;

  final StreamController<AuctionFeedMessage> _messages =
      StreamController<AuctionFeedMessage>.broadcast();

  /// The highest sequence forwarded to the consumer. The reconnect anchor, and only that — see
  /// `LotFeed` for why the *bid* guard is a different number.
  int _anchor;

  int _attempts = 0;
  bool _closed = false;
  WebSocket? _socket;
  StreamSubscription<Object?>? _frames;

  @override
  Stream<AuctionFeedMessage> get messages => _messages.stream;

  @override
  Future<void> close() async {
    _closed = true;
    await _frames?.cancel();
    _frames = null;
    await _socket?.close();
    _socket = null;
    if (!_messages.isClosed) await _messages.close();
  }

  Future<void> _run() async {
    while (!_closed) {
      _emit(const AuctionFeedStatusChanged(AuctionFeedStatus.connecting));

      final connected = await _connectOnce();
      if (_closed) return;

      if (connected) {
        _attempts = 0;
        // `_connectOnce` returns when the socket ends. A close is not an error — the server
        // recycles connections — so the loop simply reconnects from the anchor.
        _emit(const AuctionFeedStatusChanged(AuctionFeedStatus.closed));
      } else {
        _attempts += 1;
        _emit(const AuctionFeedStatusChanged(AuctionFeedStatus.closed));
      }

      // Jittered, for the reason `BackoffPolicy` states: when a tower comes back every handset
      // behind it reconnects in the same millisecond, and a synchronised herd can hold the socket
      // tier down after the outage that caused it has ended.
      await Future<void>.delayed(backoff.delayFor(_attempts == 0 ? 1 : _attempts));
    }
  }

  /// Connects and pumps until the socket ends. Returns whether the connection was ever established.
  Future<bool> _connectOnce() async {
    final query = <String, String>{
      if (accessToken != null && accessToken!.isNotEmpty) 'token': accessToken!,
      'sinceSeq': '$_anchor',
    };
    final uri = Uri.parse('$wsBaseUrl/realtime/auction/${Uri.encodeComponent(lotId)}')
        .replace(queryParameters: query);

    final WebSocket socket;
    try {
      socket = await WebSocket.connect(uri.toString());
    } on Object {
      // Deliberately broad, and deliberately silent to the user. The socket is an accelerator, not
      // the source of truth: the lot was fetched over HTTP and bids go over HTTP. A failure here
      // costs liveness, which the status line already reports, and must never surface as an error
      // dialog over a screen that is working.
      return false;
    }

    if (_closed) {
      await socket.close();
      return true;
    }

    _socket = socket;
    _emit(const AuctionFeedStatusChanged(AuctionFeedStatus.open));

    final done = Completer<void>();
    _frames = socket.listen(
      _onFrame,
      onError: (Object _) {},
      onDone: () {
        if (!done.isCompleted) done.complete();
      },
      cancelOnError: false,
    );

    await done.future;
    await _frames?.cancel();
    _frames = null;
    _socket = null;
    return true;
  }

  void _onFrame(Object? frame) {
    if (frame is! String) return;

    final AuctionEvent event;
    try {
      final decoded = jsonDecode(frame);
      if (decoded is! Map<String, Object?>) return;
      event = AuctionEvent.fromJson(decoded);
    } on Object {
      // A frame this build cannot read is a frame it ignores. The next one carries a higher `seq`,
      // and the anchor is only advanced by frames that were understood well enough to forward.
      return;
    }

    // `seq` is monotonic per lot. A replayed frame after a reconnect is not new information, and
    // forwarding it would let a screen "re-render" a bid it has already shown.
    if (event.seq <= _anchor) return;
    _anchor = event.seq;
    _emit(AuctionFeedEvent(event));
  }

  void _emit(AuctionFeedMessage message) {
    if (_closed || _messages.isClosed) return;
    _messages.add(message);
  }
}
