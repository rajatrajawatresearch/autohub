import 'package:autohub_core/autohub_core.dart';

import 'models/cursor_page.dart';
import 'models/deal.dart';
import 'models/inventory_item.dart';
import 'server_time.dart';

/// Every endpoint the dealer console reads, in one place.
///
/// `ApiClient`'s doc names this the intended caller shape: an app that reaches for the transport
/// from a screen is an app that will grow a second copy of the header logic and a third opinion
/// about what a `409` means. Nothing here knows dio exists — that stays inside `autohub_core` —
/// and nothing here decides anything. Every method is a path, a query and a decoder.
///
/// **Reads only.** Writes go through the outbox (`WorkflowActionRunner`) or, for a bid, through
/// `ImmediateWriter`. Keeping them out of this class is what makes "does this write queue?" a
/// question with one answer per call site rather than a flag someone can flip.
final class DealerApi {
  const DealerApi(this._client);

  final ApiClient _client;

  /* ── Stock ──────────────────────────────────────────────────────────────── */

  /// The dealer's own listings. `state` is a workflow state *code* from the registry.
  Future<CursorPage<InventoryItem>> inventory({
    String? state,
    String? query,
    String? cursor,
    int limit = 60,
  }) =>
      _client.get(
        '/listings',
        read: CursorPage.reader(InventoryItem.fromJson),
        query: <String, Object?>{
          'state': state,
          'q': query,
          'cursor': cursor,
          'limit': limit,
        },
      );

  Future<ListingDetail> listing(String listingId) => _client.get(
        '/listings/${Uri.encodeComponent(listingId)}',
        read: ListingDetail.fromJson,
      );

  /// The live workflow state of one entity, with the actions **this** dealer may fire right now.
  ///
  /// Never cached as a registry document, and the reason is in `registry_reads.dart`: permissions
  /// are evaluated server-side per request, so a stale `availableActions` would offer a dealer a
  /// button the server is going to refuse.
  Future<WorkflowSnapshot> workflowSnapshot({
    required String module,
    required String resource,
    required String id,
  }) =>
      _client.get(
        '/$module/$resource/${Uri.encodeComponent(id)}/workflow',
        read: WorkflowSnapshot.fromJson,
      );

  /* ── Trade ──────────────────────────────────────────────────────────────── */

  Future<CursorPage<DealSummary>> deals(DealListParams params) => _client.get(
        '/deals/deals',
        read: CursorPage.reader(DealSummary.fromJson),
        query: params.toQuery(),
      );

  Future<DealDetail> deal(String dealId) => _client.get(
        '/deals/deals/${Uri.encodeComponent(dealId)}',
        read: DealDetail.fromJson,
      );

  /* ── Auction ────────────────────────────────────────────────────────────── */

  /// The floor, plus the clock its countdowns render against.
  ///
  /// The two travel together because they are measured together — see [Clocked]. A screen that
  /// fetched lots here and a clock somewhere else could pair a fresh lot with a stale offset,
  /// which is the exact failure `AhServerClock` exists to prevent.
  Future<Clocked<CursorPage<AuctionLot>>> auctionLots({
    String? state,
    String? city,
    String? categoryCode,
    String? cursor,
    int limit = 24,
  }) =>
      _clockedGet(
        '/auction/lots',
        read: CursorPage.reader(AuctionLot.fromJson),
        query: AuctionLotListParams(
          state: state,
          city: city,
          categoryCode: categoryCode,
          cursor: cursor,
          limit: limit,
        ).toQuery(),
      );

  Future<Clocked<AuctionLot>> auctionLot(String lotId) => _clockedGet(
        '/auction/lots/${Uri.encodeComponent(lotId)}',
        read: AuctionLot.fromJson,
      );

  /// The bid ladder — newest first, as the server orders it.
  Future<CursorPage<Bid>> lotBids(String lotId, {int limit = 25}) => _client.get(
        '/auction/lots/${Uri.encodeComponent(lotId)}/bids',
        read: CursorPage.reader(Bid.fromJson),
        query: <String, Object?>{'limit': limit},
      );

  /* ── The one raw path ───────────────────────────────────────────────────── */

  /// A `GET` that also yields the server clock.
  ///
  /// `sendRaw` is the transport's documented escape hatch for callers that need headers, and this
  /// is the only place in the app that takes it: the countdown is the one thing on any screen that
  /// must not be rendered against the handset clock, and `Date` is where the correction comes from.
  /// Every other read uses the typed helper, decode boundary and all.
  Future<Clocked<T>> _clockedGet<T>(
    String path, {
    required JsonReader<T> read,
    Map<String, Object?>? query,
  }) async {
    final sentAt = DateTime.now().toUtc();
    final response = await _client.sendRaw(HttpMethod.get, path, query: query);
    final receivedAt = DateTime.now().toUtc();

    final body = response.data;
    if (body is! Map<String, Object?>) {
      throw ApiException(
        statusCode: response.statusCode ?? 200,
        code: ApiErrorCodes.malformedResponse,
        message: 'The response from $path was not a JSON object.',
      );
    }

    final T value;
    try {
      value = read(body);
    } on Object {
      // Deliberately broad, and for the reason `ApiClient._read` gives: `json_serializable`
      // reports a shape mismatch as a `TypeError`, which is an `Error` rather than an `Exception`,
      // and letting it escape surfaces contract drift three frames inside a widget build with no
      // mention of the endpoint that produced it.
      throw ApiException(
        statusCode: response.statusCode ?? 200,
        code: ApiErrorCodes.malformedResponse,
        message: 'The response from $path did not match the contract.',
      );
    }

    return Clocked<T>(
      value: value,
      clock: clockFromHttpDate(
            response.headers.value('date'),
            sentAt: sentAt,
            receivedAt: receivedAt,
          ) ??
          // A response with no usable `Date` leaves the countdown on the device clock for this
          // fetch. Named rather than silent: `unsynchronised` is what the type is called precisely
          // so that reaching for it is visible in review.
          AhServerClock.unsynchronised,
    );
  }
}
