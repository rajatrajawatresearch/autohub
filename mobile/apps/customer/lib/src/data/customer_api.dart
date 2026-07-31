import 'package:autohub_core/autohub_core.dart';

import 'provisional_models.dart';

/// Every call this binary makes that is not a registry document and not a queued write.
///
/// An interface, not a class, and that is load-bearing in two directions. It keeps the screens from
/// ever holding an [ApiClient] — a screen with a transport in reach grows its own header logic and
/// its own retry policy — and it is the seam every widget test in `test/` substitutes, so the
/// interesting behaviour (a reveal that must not navigate, a facet sheet built from whatever the
/// server sent, a `422` landing back on the right field) is exercised without a socket.
///
/// Registry documents deliberately do **not** appear here. They go through `RegistryClient`, which
/// serves from disk first and revalidates behind — a sell form must render in a tunnel, and routing
/// it through this interface would make it a network call like any other.
///
/// Writes that a tunnel must not lose deliberately do not appear here either. They are captured as
/// a `WriteIntent` at the moment of the gesture and travel through the outbox; see
/// `sell_controller.dart` for why the key is minted on the tap rather than at send time.
abstract interface class CustomerApi {
  /// `GET /catalog/search`. The facets in the response are the only source of filters in this app.
  Future<SearchResponse> search(SearchParams params);

  /// `GET /listings/{id}`.
  Future<ListingDetail> listing(String listingId);

  /// `GET /listings` — the signed-in user's own.
  Future<CursorPage<SearchItem>> myListings(ListingListParams params);

  /// `GET /deals/deals?side=buying`.
  Future<CursorPage<GarageBooking>> myBookings({String? cursor, int? limit});

  /// `POST /valuation/quotes`.
  ///
  /// [idempotencyKey] is minted where the user tapped, not here: a retry after a dead cell must be
  /// the same act, or a seller who tapped once gets two quote records and two follow-up calls.
  Future<ValuationQuote> valuationQuote(
    ValuationQuoteRequest request, {
    required String idempotencyKey,
  });

  /// `POST /auth/otp/request`.
  Future<OtpChallenge> requestOtp(OtpRequest request, {required String idempotencyKey});

  /// `POST /auth/otp/verify`.
  Future<AuthSession> verifyOtp(OtpVerifyRequest request, {required String idempotencyKey});

  /// `GET /auth/me` — resolved permissions, never roles-as-authorisation.
  Future<AuthUser> me();

  /// `POST /auth/logout`. Best-effort: the local session ends whether or not the server answered.
  Future<void> logout(String refreshToken);
}

/// The dio-backed implementation. Paths and shapes match `packages/api/src/endpoints`, because the
/// contract is the seam and two clients that spell the same endpoint differently is a bug nobody
/// finds until one of them is in an app store.
final class HttpCustomerApi implements CustomerApi {
  const HttpCustomerApi(this._client);

  final ApiClient _client;

  @override
  Future<SearchResponse> search(SearchParams params) => _client.get<SearchResponse>(
        '/catalog/search',
        read: SearchResponse.fromJson,
        query: params.toQuery(),
        // Search is browsable signed out. Sending a bearer when there is one still matters — it is
        // what personalises `priceMinor` for a dealer tier — so this stays authenticated and the
        // interceptor simply omits the header when there is no session.
      );

  @override
  Future<ListingDetail> listing(String listingId) => _client.get<ListingDetail>(
        '/listings/${Uri.encodeComponent(listingId)}',
        read: ListingDetail.fromJson,
      );

  @override
  Future<CursorPage<SearchItem>> myListings(ListingListParams params) =>
      _client.get<CursorPage<SearchItem>>(
        '/listings',
        read: (Map<String, Object?> json) =>
            CursorPage<SearchItem>.fromJson(json, SearchItem.fromJson),
        query: params.toQuery(),
      );

  @override
  Future<CursorPage<GarageBooking>> myBookings({String? cursor, int? limit}) =>
      _client.get<CursorPage<GarageBooking>>(
        '/deals/deals',
        read: (Map<String, Object?> json) =>
            CursorPage<GarageBooking>.fromJson(json, GarageBooking.fromJson),
        query: <String, Object?>{
          'side': 'buying',
          if (cursor != null) 'cursor': cursor,
          if (limit != null) 'limit': limit,
        },
      );

  @override
  Future<ValuationQuote> valuationQuote(
    ValuationQuoteRequest request, {
    required String idempotencyKey,
  }) =>
      _client.post<ValuationQuote>(
        '/valuation/quotes',
        read: ValuationQuote.fromJson,
        body: request.toJson(),
        // Unauthenticated on purpose, per §10 of the contract. Asking for a phone number before
        // showing a number is why users bounce, and this is the one screen the product is judged on.
        context: RequestContext(authenticated: false, idempotencyKey: idempotencyKey),
      );

  @override
  Future<OtpChallenge> requestOtp(OtpRequest request, {required String idempotencyKey}) =>
      _client.post<OtpChallenge>(
        '/auth/otp/request',
        read: OtpChallenge.fromJson,
        body: request.toJson(),
        context: RequestContext(authenticated: false, idempotencyKey: idempotencyKey),
      );

  @override
  Future<AuthSession> verifyOtp(OtpVerifyRequest request, {required String idempotencyKey}) =>
      _client.post<AuthSession>(
        '/auth/otp/verify',
        read: AuthSession.fromJson,
        body: <String, Object?>{
          ...request.toJson(),
          // The device the tokens get bound to. Sent explicitly rather than left to the header,
          // because §8 makes it part of the body the refresh family is keyed on.
          'deviceId': _client.deviceId,
        },
        context: RequestContext(authenticated: false, idempotencyKey: idempotencyKey),
      );

  @override
  Future<AuthUser> me() => _client.get<AuthUser>('/auth/me', read: AuthUser.fromJson);

  @override
  Future<void> logout(String refreshToken) => _client.send(
        HttpMethod.post,
        '/auth/logout',
        body: <String, Object?>{'refreshToken': refreshToken},
      );
}
