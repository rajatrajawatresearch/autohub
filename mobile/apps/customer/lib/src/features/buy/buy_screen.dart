import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/error_surface.dart';
import '../../app/router.dart';
import '../../data/app_providers.dart';
import '../../data/search.dart';
import '../../i18n/customer_text.dart';
import '../../widgets/listing_card.dart';
import 'facet_sheet.dart';

/// Search, for one listing intent.
///
/// `/buy`, `/buy?intent=rental` and `/buy?intent=auction` are this screen three times, because they
/// are the same question asked of three markets. The filters come from the response, the sort keys
/// are the contract's closed grammar, and the only thing this file knows about vehicles is that
/// they have a card.
class BuyScreen extends ConsumerStatefulWidget {
  const BuyScreen({required this.intent, super.key});

  final ListingIntent intent;

  @override
  ConsumerState<BuyScreen> createState() => _BuyScreenState();
}

class _BuyScreenState extends ConsumerState<BuyScreen> {
  final ScrollController _scroll = ScrollController();
  final TextEditingController _query = TextEditingController();

  /// How close to the bottom the next page is asked for.
  ///
  /// Roughly two cards' worth. Fetching at the very bottom means the user watches a held slot;
  /// fetching a whole screen early means paying for pages nobody reaches on a metered connection.
  static const double _prefetchWindow = 640;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scroll
      ..removeListener(_onScroll)
      ..dispose();
    _query.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scroll.hasClients) return;
    final double remaining = _scroll.position.maxScrollExtent - _scroll.position.pixels;
    if (remaining > _prefetchWindow) return;
    // The controller is guarded against re-entry, so a fling that fires this three times still
    // produces one request.
    unawaited(ref.read(searchProvider(widget.intent).notifier).loadMore());
  }

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final SearchState state = ref.watch(searchProvider(widget.intent));
    final VehicleSearchController controller = ref.read(searchProvider(widget.intent).notifier);

    return Scaffold(
      body: SafeArea(
        child: Column(
          children: <Widget>[
            _Toolbar(
              intent: widget.intent,
              query: _query,
              state: state,
              controller: controller,
            ),
            Divider(height: 1, color: surfaces.borderHairline),
            Expanded(
              child: RefreshIndicator(
                onRefresh: controller.refresh,
                child: _Results(
                  state: state,
                  controller: controller,
                  scroll: _scroll,
                  t: t,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/* ── Toolbar ───────────────────────────────────────────────────────────────── */

class _Toolbar extends ConsumerWidget {
  const _Toolbar({
    required this.intent,
    required this.query,
    required this.state,
    required this.controller,
  });

  final ListingIntent intent;
  final TextEditingController query;
  final SearchState state;
  final VehicleSearchController controller;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final int? total = state.totalEstimate;

    return Padding(
      padding: const EdgeInsets.fromLTRB(AhSpace.lg, AhSpace.md, AhSpace.lg, AhSpace.md),
      child: Column(
        children: <Widget>[
          AhTextField(
            controller: query,
            placeholder: t(CustomerText.buySearchPlaceholder),
            textInputAction: TextInputAction.search,
            onChanged: controller.setQuery,
          ),
          const SizedBox(height: AhSpace.md),
          Row(
            children: <Widget>[
              Expanded(
                child: Text(
                  // An estimate by contract, never a `COUNT(*)`. Rendered with Indian grouping,
                  // because `1,284` and `1284` are two different products on the first screen.
                  state.loading && state.items.isEmpty
                      ? ''
                      : t(
                          CustomerText.buyCount,
                          <String, Object?>{
                            'count': formatIndianNumber(total ?? state.items.length),
                          },
                        ),
                  style: AhText.data.copyWith(fontSize: 13, color: surfaces.textSecondary),
                ),
              ),
              _SortButton(controller: controller, sort: state.params.sort),
              const SizedBox(width: AhSpace.sm),
              AhButton(
                label: state.activeFilterCount == 0
                    ? t(CustomerText.buyFilters)
                    : t(
                        CustomerText.buyFiltersClear,
                        <String, Object?>{'count': state.activeFilterCount},
                      ),
                variant: AhButtonVariant.outline,
                size: AhButtonSize.sm,
                icon: Icons.tune,
                onPressed: () => unawaited(FacetSheet.show(context, intent)),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _SortButton extends ConsumerWidget {
  const _SortButton({required this.controller, required this.sort});

  final VehicleSearchController controller;
  final SearchSort sort;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);

    return PopupMenuButton<SearchSort>(
      initialValue: sort,
      tooltip: t(CustomerText.buySort),
      onSelected: controller.setSort,
      itemBuilder: (BuildContext context) => <PopupMenuEntry<SearchSort>>[
        for (final SearchSort value in SearchSort.values)
          PopupMenuItem<SearchSort>(value: value, child: Text(sortLabel(value, t))),
      ],
      child: AhBadge(label: sortLabel(sort, t), tone: AhBadgeTone.outline),
    );
  }
}

/// The sort vocabulary is the contract's closed query grammar — `relevance|price_asc|…` — so the
/// switch is exhaustive and a seventh sort breaks the build. The words are registry keys.
String sortLabel(SearchSort sort, AppStrings t) => switch (sort) {
      SearchSort.relevance => t(CustomerText.buySortRelevance),
      SearchSort.newest => t(CustomerText.buySortNewest),
      SearchSort.priceAsc => t(CustomerText.buySortPriceAsc),
      SearchSort.priceDesc => t(CustomerText.buySortPriceDesc),
      SearchSort.kmsAsc => t(CustomerText.buySortKmsAsc),
    };

/* ── Results ───────────────────────────────────────────────────────────────── */

class _Results extends StatelessWidget {
  const _Results({
    required this.state,
    required this.controller,
    required this.scroll,
    required this.t,
  });

  final SearchState state;
  final VehicleSearchController controller;
  final ScrollController scroll;
  final AppStrings t;

  @override
  Widget build(BuildContext context) {
    if (state.loading && state.items.isEmpty) return const _ResultsSkeleton();

    if (state.items.isEmpty && state.fault != null) {
      return _Scrollable(
        scroll: scroll,
        child: AppErrorView(fault: state.fault!, onRetry: controller.refresh),
      );
    }

    if (state.items.isEmpty) {
      // An empty state is an instruction, never a mood. Which instruction depends on whether the
      // user filtered themselves into a corner or the city genuinely has nothing.
      final bool filtered = state.activeFilterCount > 0;
      return _Scrollable(
        scroll: scroll,
        child: EmptyState(
          title: t(CustomerText.buyEmptyTitle),
          body: filtered
              ? t(CustomerText.buyEmptyBody)
              : t(CustomerText.buyEmptyUnfilteredBody),
          action: filtered
              ? AhButton(
                  label: t(
                    CustomerText.buyFiltersClear,
                    <String, Object?>{'count': state.activeFilterCount},
                  ),
                  variant: AhButtonVariant.outline,
                  onPressed: controller.clearFilters,
                )
              : AhButton(
                  label: t(CustomerText.navSell),
                  variant: AhButtonVariant.outline,
                  onPressed: () => context.go(Routes.sell),
                ),
        ),
      );
    }

    return ListView.separated(
      controller: scroll,
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(AhSpace.lg),
      // One extra row: the paging footer, or the fault that stopped it.
      itemCount: state.items.length + 1,
      separatorBuilder: (BuildContext context, int index) => const SizedBox(height: AhSpace.lg),
      itemBuilder: (BuildContext context, int index) {
        if (index < state.items.length) return ListingCard(item: state.items[index]);

        if (state.fault != null) {
          // Page one is on screen and worth keeping. A banner says the *next* page failed without
          // throwing away what the user is already reading.
          return AppFaultBanner(fault: state.fault!, onRetry: controller.loadMore);
        }
        if (state.loadingMore) return const AhSkeleton.block(height: 320);
        if (state.hasMore) {
          return AhButton(
            label: t(CustomerText.buyMore),
            variant: AhButtonVariant.outline,
            block: true,
            onPressed: () => unawaited(controller.loadMore()),
          );
        }
        return const SizedBox(height: AhSpace.xxl);
      },
    );
  }
}

/// An empty or failed state that still has to scroll, so pull-to-refresh reaches it.
class _Scrollable extends StatelessWidget {
  const _Scrollable({required this.scroll, required this.child});

  final ScrollController scroll;
  final Widget child;

  @override
  Widget build(BuildContext context) => ListView(
        controller: scroll,
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(AhSpace.lg),
        children: <Widget>[child],
      );
}

class _ResultsSkeleton extends StatelessWidget {
  const _ResultsSkeleton();

  @override
  Widget build(BuildContext context) => ListView.separated(
        padding: const EdgeInsets.all(AhSpace.lg),
        itemCount: 4,
        separatorBuilder: (BuildContext context, int index) => const SizedBox(height: AhSpace.lg),
        itemBuilder: (BuildContext context, int index) => const AhSkeleton.block(height: 320),
      );
}
