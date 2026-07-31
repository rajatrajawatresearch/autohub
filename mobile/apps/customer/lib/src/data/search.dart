import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/fault.dart';
import 'app_providers.dart';

/// One search, as the screen sees it.
///
/// [facets] is the whole filter UI. There is no list of filterable attributes in this binary and
/// there cannot be one: the facets arrive inside the search response, generated from the taxonomy
/// registry, so marking `battery_health_pct` searchable is a database row and the filter sheet
/// grows a control for it on the next request.
@immutable
final class SearchState {
  const SearchState({
    required this.params,
    this.items = const <SearchItem>[],
    this.facets = const <SearchFacet>[],
    this.nextCursor,
    this.totalEstimate,
    this.loading = false,
    this.loadingMore = false,
    this.fault,
  });

  final SearchParams params;
  final List<SearchItem> items;
  final List<SearchFacet> facets;

  /// `null` when the list is exhausted. The infinite scroll stops on this and never on a count.
  final String? nextCursor;

  /// An estimate by contract. The UI says "about", because the server never promised precision.
  final int? totalEstimate;

  /// First page in flight. The screen shows held slots, not a spinner.
  final bool loading;

  /// A later page in flight. Distinct, because the results already on screen must not be replaced.
  final bool loadingMore;

  final AppFault? fault;

  bool get hasMore => (nextCursor ?? '').isNotEmpty;

  /// How many filters the user has applied, for the "Clear 3" affordance.
  ///
  /// The free-text query counts: it is a filter in every way that matters to somebody looking at an
  /// empty result set and wondering what to undo.
  int get activeFilterCount =>
      params.attributes.length +
      ((params.q ?? '').isEmpty ? 0 : 1) +
      (params.price == null || params.price!.isEmpty ? 0 : 1);

  SearchState copyWith({
    SearchParams? params,
    List<SearchItem>? items,
    List<SearchFacet>? facets,
    String? nextCursor,
    bool clearCursor = false,
    int? totalEstimate,
    bool? loading,
    bool? loadingMore,
    AppFault? fault,
    bool clearFault = false,
  }) =>
      SearchState(
        params: params ?? this.params,
        items: items ?? this.items,
        facets: facets ?? this.facets,
        nextCursor: clearCursor ? null : (nextCursor ?? this.nextCursor),
        totalEstimate: totalEstimate ?? this.totalEstimate,
        loading: loading ?? this.loading,
        loadingMore: loadingMore ?? this.loadingMore,
        fault: clearFault ? null : (fault ?? this.fault),
      );
}

/// The home screen's stock strip.
///
/// A separate read from [searchProvider] on purpose. Sharing the buy screen's controller would mean
/// a filter the user set on `/buy` silently reshapes the home page — and the strip's job is to show
/// that there is a live market, not to answer a query.
final FutureProvider<List<SearchItem>> liveStockProvider =
    FutureProvider<List<SearchItem>>((ref) async {
  final String? city = ref.watch(activeCityProvider).valueOrNull;
  try {
    final SearchResponse response = await ref.watch(customerApiProvider).search(
          SearchParams(intent: ListingIntent.sale, city: city, limit: 8),
        );
    return response.items;
  } on Object catch (error) {
    throw ref.read(diagnosticsLogProvider.notifier).capture(error);
  }
});

/// One controller per listing intent.
///
/// A family rather than a singleton because `/buy`, `/buy?intent=rental` and `/buy?intent=auction`
/// are three result sets a user moves between, and one controller would throw away the scroll
/// position and the filters of whichever they left.
final NotifierProviderFamily<VehicleSearchController, SearchState, ListingIntent> searchProvider =
    NotifierProvider.family<VehicleSearchController, SearchState, ListingIntent>(
  VehicleSearchController.new,
);

class VehicleSearchController extends FamilyNotifier<SearchState, ListingIntent> {
  /// Twenty-four fills three screens on a handset and is one request rather than three.
  static const int _pageSize = 24;

  /// Long enough that a ten-character model name is one request rather than ten, short enough that
  /// a user who has stopped typing does not notice waiting. Typing is the common case here — nobody
  /// pastes a search term.
  static const Duration _typingWindow = Duration(milliseconds: 350);

  Timer? _debounce;

  @override
  SearchState build(ListingIntent intent) {
    // Rebuilt when the user switches city, which is correct: results, prices and availability are
    // all scoped by `X-Active-City`, and showing Pune stock under a Coimbatore header is worse than
    // a reload.
    final String? city = ref.watch(activeCityProvider).valueOrNull;

    ref.onDispose(() {
      _debounce?.cancel();
      _debounce = null;
    });

    final SearchParams params = SearchParams(
      intent: intent,
      city: city,
      limit: _pageSize,
    );

    unawaited(Future<void>.microtask(() => _fetch(params, append: false)));
    return SearchState(params: params, loading: true);
  }

  /* ── Filters ───────────────────────────────────────────────────────────── */

  /// Search-as-you-type, debounced.
  ///
  /// The text lands in state immediately so the field never lags the thumb; only the request waits.
  void setQuery(String query) {
    state = state.copyWith(params: state.params.copyWith(q: query));
    _debounce?.cancel();
    _debounce = Timer(_typingWindow, () => unawaited(refresh()));
  }

  void setSort(SearchSort sort) {
    state = state.copyWith(params: state.params.copyWith(sort: sort));
    unawaited(refresh());
  }

  /// Adds or removes one value of a multi-value facet. Multi-value is OR *within* an attribute,
  /// per §9 — `attr.fuel_type=petrol,cng` means either, not both.
  void toggleAttributeValue(String code, String value) {
    final Map<String, AttributeFilter> next =
        Map<String, AttributeFilter>.of(state.params.attributes);
    final AttributeFilter? current = next[code];
    final List<String> selected = current is AttributeAnyOf ? current.values : const <String>[];

    final List<String> updated = selected.contains(value)
        ? <String>[
            for (final String entry in selected)
              if (entry != value) entry,
          ]
        : <String>[...selected, value];

    if (updated.isEmpty) {
      next.remove(code);
    } else {
      next[code] = AttributeFilter.anyOf(updated);
    }

    _applyAttributes(next);
  }

  void setAttributeRange(String code, RangeFilter? range) {
    final Map<String, AttributeFilter> next =
        Map<String, AttributeFilter>.of(state.params.attributes);
    if (range == null || range.isEmpty) {
      next.remove(code);
    } else {
      next[code] = AttributeFilter.range(range);
    }
    _applyAttributes(next);
  }

  void setAttributeFlag(String code, {required bool value}) {
    final Map<String, AttributeFilter> next =
        Map<String, AttributeFilter>.of(state.params.attributes);
    if (value) {
      next[code] = const AttributeFilter.flag(true);
    } else {
      next.remove(code);
    }
    _applyAttributes(next);
  }

  /// Drops every filter and the query, keeping the intent and the city.
  void clearFilters() {
    state = state.copyWith(
      params: SearchParams(
        intent: state.params.intent,
        city: state.params.city,
        sort: state.params.sort,
        limit: _pageSize,
      ),
    );
    unawaited(refresh());
  }

  void _applyAttributes(Map<String, AttributeFilter> attributes) {
    state = state.copyWith(params: state.params.copyWith(attributes: attributes));
    unawaited(refresh());
  }

  /* ── Fetching ──────────────────────────────────────────────────────────── */

  /// Re-runs the current query from page one.
  Future<void> refresh() {
    _debounce?.cancel();
    return _fetch(state.params.copyWith(cursor: null), append: false);
  }

  /// The next page.
  ///
  /// Guarded on both flags and on the cursor, because a fling can fire the trigger three times
  /// before the first response lands — and three appends of page two is the same car three times.
  Future<void> loadMore() {
    if (state.loading || state.loadingMore || !state.hasMore) return Future<void>.value();
    return _fetch(state.params.nextPage(state.nextCursor), append: true);
  }

  Future<void> _fetch(SearchParams params, {required bool append}) async {
    state = state.copyWith(
      params: params,
      loading: !append,
      loadingMore: append,
      clearFault: true,
    );

    try {
      final SearchResponse response = await ref.read(customerApiProvider).search(params);

      // A response for a query the user has already moved on from is discarded. Without this, a
      // slow page-one lands after a fast page-one for a different filter and the screen shows the
      // wrong cars under the right chips.
      if (!identical(state.params, params)) return;

      state = state.copyWith(
        items: append ? <SearchItem>[...state.items, ...response.items] : response.items,
        // Facets belong to the query, not to the page. Replacing them on an append would flicker
        // the sheet and, on a server that scopes counts per page, would lie about them.
        facets: append ? state.facets : response.facets,
        nextCursor: response.nextCursor,
        clearCursor: response.nextCursor == null,
        totalEstimate: response.totalEstimate,
        loading: false,
        loadingMore: false,
      );
    } on Object catch (error) {
      if (!identical(state.params, params)) return;
      state = state.copyWith(
        loading: false,
        loadingMore: false,
        fault: ref.read(diagnosticsLogProvider.notifier).capture(error),
      );
    }
  }
}
