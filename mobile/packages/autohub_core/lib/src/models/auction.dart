import 'package:autohub_core/src/models/listing.dart';
// `ListingSummary` is an alias for `SearchItem`, and generated code names the underlying type.
import 'package:autohub_core/src/models/search.dart';
import 'package:autohub_core/src/models/workflow.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'auction.freezed.dart';
part 'auction.g.dart';

@freezed
class Bid with _$Bid {
  const factory Bid({
    required String bidId,
    required String lotId,

    /// Monotonic per lot. The client's reconnect anchor and its stale-bid guard.
    @Default(0) int seq,
    required int amountMinor,

    /// `Dealer ••42` — bidders are masked from each other.
    @Default('') String bidderMask,
    required String placedAt,
    @Default(false) bool isYours,
    @Default(false) bool isAutoBid,
  }) = _Bid;

  factory Bid.fromJson(Map<String, Object?> json) => _$BidFromJson(json);
}

@freezed
class AuctionLot with _$AuctionLot {
  const factory AuctionLot({
    required String lotId,
    String? lotNumber,
    required ListingSummary listing,
    @Default('INR') String currency,
    required WorkflowState state,
    required String startsAt,

    /// The server's closing time. Render the countdown against the offset measured at connect,
    /// never against the handset clock — a phone two minutes fast shows a lot as closed while it is
    /// still taking bids, and the dealer stops bidding on a truck they were winning.
    required String endsAt,
    required int startPriceMinor,

    /// The ladder step. The next legal bid is `currentBidMinor + incrementMinor`.
    required int incrementMinor,
    int? currentBidMinor,
    required int minNextBidMinor,
    @Default(0) int bidCount,

    /// The reserve *amount* is never exposed — only whether it has been met.
    @Default(false) bool reserveMet,

    /// Highest sequence broadcast so far. Sent back as `seenBidSeq` to detect an outbid race.
    @Default(0) int latestSeq,
    @Default(false) bool highestBidderIsYou,
    int? yourMaxBidMinor,

    /// Anti-snipe extensions already spent.
    @Default(0) int extensionCount,
    String? inspectionReportUrl,
  }) = _AuctionLot;

  factory AuctionLot.fromJson(Map<String, Object?> json) => _$AuctionLotFromJson(json);
}

@freezed
class PlaceBidRequest with _$PlaceBidRequest {
  const factory PlaceBidRequest({
    required int amountMinor,

    /// The highest sequence this screen has actually **rendered** — not the highest it has
    /// received, and not the lot's `latestSeq` as fetched. A bid composed against a figure the
    /// dealer never saw is a bid they did not agree to, and the server rejects it as
    /// `auction.bid.outbid_race` so the app can show the truth rather than guess the next
    /// increment locally.
    int? seenBidSeq,

    /// Proxy bidding: bid up to this automatically.
    int? maxAmountMinor,
  }) = _PlaceBidRequest;

  factory PlaceBidRequest.fromJson(Map<String, Object?> json) => _$PlaceBidRequestFromJson(json);
}

@freezed
class PlaceBidResponse with _$PlaceBidResponse {
  const factory PlaceBidResponse({
    required Bid bid,
    required AuctionLot lot,
  }) = _PlaceBidResponse;

  factory PlaceBidResponse.fromJson(Map<String, Object?> json) => _$PlaceBidResponseFromJson(json);
}

/// Query for the auction floor.
@immutable
final class AuctionLotListParams {
  const AuctionLotListParams({this.state, this.city, this.categoryCode, this.cursor, this.limit});

  /// A workflow state code — `live`, `scheduled`, `closed`. The registry says which exist.
  final String? state;
  final String? city;
  final String? categoryCode;
  final String? cursor;
  final int? limit;

  Map<String, Object?> toQuery() => <String, Object?>{
        'state': state,
        'city': city,
        'categoryCode': categoryCode,
        'cursor': cursor,
        'limit': limit,
      }..removeWhere((_, value) => value == null || value == '');
}

/* ── Realtime, §11 ─────────────────────────────────────────────────────────── */

@freezed
class BidPlacedEvent with _$BidPlacedEvent {
  const factory BidPlacedEvent({
    required String lotId,
    required int amountMinor,
    @Default('') String bidderMask,
    required String placedAt,
    @Default(false) bool highestBidderIsYou,
    int? bidCount,
    int? minNextBidMinor,
  }) = _BidPlacedEvent;

  factory BidPlacedEvent.fromJson(Map<String, Object?> json) => _$BidPlacedEventFromJson(json);
}

@freezed
class LotExtendedEvent with _$LotExtendedEvent {
  const factory LotExtendedEvent({
    required String endsAt,

    /// `anti_snipe` — why the clock moved. The UI says so; a countdown that jumps silently reads
    /// as a broken app rather than as a working auction.
    @Default('') String reason,
  }) = _LotExtendedEvent;

  factory LotExtendedEvent.fromJson(Map<String, Object?> json) => _$LotExtendedEventFromJson(json);
}

@freezed
class LotClosedEvent with _$LotClosedEvent {
  const factory LotClosedEvent({
    @Default('') String outcome,
    @Default(false) bool winnerIsYou,
    int? finalAmountMinor,
  }) = _LotClosedEvent;

  factory LotClosedEvent.fromJson(Map<String, Object?> json) => _$LotClosedEventFromJson(json);
}

/// What the read-side socket broadcasts, discriminated on `type`.
///
/// Bids are never *placed* over this socket — they go to `POST /auction/lots/{id}/bids` with an
/// `Idempotency-Key`, so a dropped connection can neither lose a bid nor place it twice, which a
/// socket-delivered bid cannot promise.
///
/// The [AuctionEvent.unknown] case exists for one specific reason and it is not politeness: `seq`
/// is the reconnect anchor. On reconnect the client sends `?sinceSeq=` and receives the gap, so an
/// event this build cannot interpret must still yield its sequence number. Dropping the frame
/// entirely would leave the client asking for a gap it has already been sent, forever.
@Freezed(unionKey: 'type', fallbackUnion: 'unknown')
sealed class AuctionEvent with _$AuctionEvent {
  @FreezedUnionValue('bid.placed')
  const factory AuctionEvent.bidPlaced({
    required int seq,
    required BidPlacedEvent data,
  }) = AuctionBidPlaced;

  @FreezedUnionValue('lot.extended')
  const factory AuctionEvent.lotExtended({
    required int seq,
    required LotExtendedEvent data,
  }) = AuctionLotExtended;

  @FreezedUnionValue('lot.closed')
  const factory AuctionEvent.lotClosed({
    required int seq,
    required LotClosedEvent data,
  }) = AuctionLotClosed;

  /// An event type this build does not understand. Carries its sequence so the reconnect anchor
  /// still advances.
  const factory AuctionEvent.unknown({
    @Default(0) int seq,
  }) = AuctionUnknownEvent;

  factory AuctionEvent.fromJson(Map<String, Object?> json) => _$AuctionEventFromJson(json);
}
