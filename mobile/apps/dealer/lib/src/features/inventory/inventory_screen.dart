import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/providers.dart';
import '../../app/routes.dart';
import '../../app/strings.dart';
import '../../data/models/inventory_item.dart';
import '../../widgets/async_section.dart';
import '../../widgets/console.dart';

/// How the list is ordered.
///
/// A legal enum: this is a *presentation* choice the dealer makes on their own screen, not a
/// registry row and not part of any request — the `/listings` query grammar has no sort parameter
/// and the ordering is applied to what has already been fetched.
enum InventorySort { newestIn, oldestIn, askHigh, askLow, mostLeads }

/// The stock list, plus where it is up to.
@immutable
final class InventoryView {
  const InventoryView({
    required this.items,
    this.nextCursor,
    this.totalEstimate,
    this.stateFilter,
    this.loadingMore = false,
  });

  final List<InventoryItem> items;
  final String? nextCursor;
  final int? totalEstimate;

  /// A workflow state *code*, or null for everything. Which codes exist comes from the listing
  /// workflow registry — the chips are built from it, so a state added on a Tuesday becomes a
  /// filter on handsets that shipped in March.
  final String? stateFilter;

  final bool loadingMore;

  bool get hasMore => nextCursor != null && nextCursor!.isNotEmpty;

  InventoryView copyWith({bool? loadingMore}) => InventoryView(
        items: items,
        nextCursor: nextCursor,
        totalEstimate: totalEstimate,
        stateFilter: stateFilter,
        loadingMore: loadingMore ?? this.loadingMore,
      );
}

/// Fetches and pages the dealer's stock.
class InventoryController extends AutoDisposeAsyncNotifier<InventoryView> {
  @override
  Future<InventoryView> build() => _fetch(null);

  Future<InventoryView> _fetch(String? stateFilter) async {
    final page = await ref.read(dealerApiProvider).inventory(state: stateFilter);
    return InventoryView(
      items: page.items,
      nextCursor: page.nextCursor,
      totalEstimate: page.totalEstimate,
      stateFilter: stateFilter,
    );
  }

  Future<void> setStateFilter(String? code) async {
    if (state.valueOrNull?.stateFilter == code) return;
    await _reload(code);
  }

  Future<void> refresh() => _reload(state.valueOrNull?.stateFilter);

  /// `copyWithPrevious` is what keeps the rows on screen while the refetch runs. A dealer who pulls
  /// to refresh must not watch their inventory blank and come back — the list they were reading is
  /// still true until the server says otherwise.
  Future<void> _reload(String? code) async {
    state = const AsyncLoading<InventoryView>().copyWithPrevious(state);
    state = await AsyncValue.guard(() => _fetch(code));
  }

  Future<void> loadMore() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasMore || current.loadingMore) return;

    state = AsyncData<InventoryView>(current.copyWith(loadingMore: true));
    try {
      final page = await ref.read(dealerApiProvider).inventory(
            state: current.stateFilter,
            cursor: current.nextCursor,
          );
      state = AsyncData<InventoryView>(
        InventoryView(
          items: <InventoryItem>[...current.items, ...page.items],
          nextCursor: page.nextCursor,
          totalEstimate: page.totalEstimate ?? current.totalEstimate,
          stateFilter: current.stateFilter,
        ),
      );
    } on Object catch (error, stack) {
      // The rows already loaded stay. A failure on page four must not throw away pages one to
      // three, which is precisely what a bare `AsyncError` assignment would do.
      state = AsyncError<InventoryView>(error, stack)
          .copyWithPrevious(AsyncData<InventoryView>(current));
    }
  }
}

final AutoDisposeAsyncNotifierProvider<InventoryController, InventoryView> inventoryProvider =
    AsyncNotifierProvider.autoDispose<InventoryController, InventoryView>(
  InventoryController.new,
);

/// The screen a dealer lives in.
///
/// Dense rows, mono where numbers are compared, and the four things they are actually scanning
/// for: the plate they can match against the car in front of them, what it is, how long it has been
/// sitting, and what they are asking. The filter is local to the loaded page because it has to keep
/// working in a basement — a server round trip per keystroke is a filter that stops working exactly
/// where a dealer is standing.
class InventoryScreen extends ConsumerStatefulWidget {
  const InventoryScreen({super.key});

  @override
  ConsumerState<InventoryScreen> createState() => _InventoryScreenState();
}

class _InventoryScreenState extends ConsumerState<InventoryScreen> {
  final TextEditingController _filter = TextEditingController();
  InventorySort _sort = InventorySort.oldestIn;

  @override
  void dispose() {
    _filter.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = ref.watch(stringsProvider);
    final view = ref.watch(inventoryProvider);

    return RefreshIndicator(
      onRefresh: () => ref.read(inventoryProvider.notifier).refresh(),
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: consolePagePadding(context),
        children: <Widget>[
          ConsolePageHeader(
            eyebrow: t(DealerText.inventoryEyebrow),
            title: t(DealerText.inventoryTitle),
            description: t(DealerText.inventoryDescription),
          ),
          const SizedBox(height: AhSpace.lg),
          AhTextField(
            controller: _filter,
            placeholder: t(DealerText.filterPlaceholder),
            mono: true,
            size: AhFieldSize.sm,
            textCapitalization: TextCapitalization.characters,
            onChanged: (_) => setState(() {}),
          ),
          const SizedBox(height: AhSpace.md),
          _StateChips(strings: t),
          const SizedBox(height: AhSpace.sm),
          _SortChips(
            sort: _sort,
            strings: t,
            onChanged: (next) => setState(() => _sort = next),
          ),
          const SizedBox(height: AhSpace.md),
          AsyncSection<InventoryView>(
            value: view,
            onRetry: () => unawaited(ref.read(inventoryProvider.notifier).refresh()),
            builder: (context, data) => _StockList(
              view: data,
              query: _filter.text,
              sort: _sort,
              strings: t,
            ),
          ),
        ],
      ),
    );
  }
}

/// Filter chips built from the listing workflow's declared states.
///
/// Not a hardcoded `['draft', 'live', 'sold']`. That list is a registry document, and a console
/// that compiled three of its states cannot filter by the fourth — which is exactly the class of
/// bug a shipped binary cannot be hot-fixed out of.
class _StateChips extends ConsumerWidget {
  const _StateChips({required this.strings});

  final DealerStrings strings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final definition = ref.watch(workflowDefinitionProvider('listing')).valueOrNull;
    if (definition == null || definition.states.isEmpty) return const SizedBox.shrink();

    final active = ref.watch(inventoryProvider).valueOrNull?.stateFilter;
    final controller = ref.read(inventoryProvider.notifier);

    return Wrap(
      spacing: AhSpace.xs + 2,
      runSpacing: AhSpace.xs + 2,
      children: <Widget>[
        _Chip(
          label: strings(DealerText.inventoryTitle),
          selected: active == null,
          onTap: () => unawaited(controller.setStateFilter(null)),
        ),
        for (final state in definition.states)
          _Chip(
            // The registry's own `name`, already localised by the server. Nothing here maps a code
            // to a word.
            label: state.name,
            selected: active == state.code,
            onTap: () => unawaited(controller.setStateFilter(state.code)),
          ),
      ],
    );
  }
}

class _SortChips extends StatelessWidget {
  const _SortChips({required this.sort, required this.strings, required this.onChanged});

  final InventorySort sort;
  final DealerStrings strings;
  final ValueChanged<InventorySort> onChanged;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: AhSpace.xs + 2,
      runSpacing: AhSpace.xs + 2,
      children: <Widget>[
        for (final option in InventorySort.values)
          _Chip(
            label: strings(_key(option)),
            selected: option == sort,
            onTap: () => onChanged(option),
          ),
      ],
    );
  }

  /// Exhaustive by construction: a sixth sort order stops this compiling until someone writes the
  /// word for it.
  static String _key(InventorySort sort) => switch (sort) {
        InventorySort.newestIn => DealerText.sortNewest,
        InventorySort.oldestIn => DealerText.sortOldest,
        InventorySort.askHigh => DealerText.sortAskHigh,
        InventorySort.askLow => DealerText.sortAskLow,
        InventorySort.mostLeads => DealerText.sortLeads,
      };
}

class _Chip extends StatelessWidget {
  const _Chip({required this.label, required this.selected, required this.onTap});

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;

    return Semantics(
      button: true,
      selected: selected,
      label: label,
      excludeSemantics: true,
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: onTap,
          splashFactory: NoSplash.splashFactory,
          hoverColor: Colors.transparent,
          focusColor: Colors.transparent,
          highlightColor: Colors.transparent,
          child: ConstrainedBox(
            // Compact rows, full-size targets. See `ahMinTouchTarget`.
            constraints: const BoxConstraints(minHeight: ahMinTouchTarget),
            child: Align(
              widthFactor: 1,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: selected ? surfaces.accentPrimary : Colors.transparent,
                  borderRadius: const BorderRadius.all(AhRadius.control),
                  border: Border.all(
                    color: selected ? surfaces.accentPrimary : surfaces.borderStrong,
                  ),
                ),
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AhSpace.md,
                    vertical: AhSpace.xs + 2,
                  ),
                  child: Text(
                    label,
                    style: AhText.bodySm.copyWith(
                      fontWeight: FontWeight.w600,
                      color: selected ? surfaces.textOnPrimary : surfaces.textSecondary,
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _StockList extends ConsumerWidget {
  const _StockList({
    required this.view,
    required this.query,
    required this.sort,
    required this.strings,
  });

  final InventoryView view;
  final String query;
  final InventorySort sort;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final surfaces = context.ahSurfaces;
    final now = DateTime.now().toUtc();
    final rows = _ordered(_filtered(view.items, query), sort, now);

    if (rows.isEmpty) {
      return EmptyState(
        title: query.isEmpty
            ? strings(DealerText.noStockTitle)
            : strings(DealerText.noMatchTitle),
        body: query.isEmpty
            ? strings(DealerText.noStockBody)
            : strings(DealerText.noMatchBody),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Padding(
          padding: const EdgeInsets.only(bottom: AhSpace.xs),
          child: Text(
            // The denominator, always. `12` on its own does not tell a dealer whether their stock
            // shrank or their filter is on.
            '${rows.length} / ${view.items.length}',
            style: AhText.data.copyWith(fontSize: 13, color: surfaces.textSecondary),
          ),
        ),
        for (var index = 0; index < rows.length; index += 1)
          _StockRow(
            item: rows[index],
            now: now,
            strings: strings,
            divider: index != rows.length - 1,
          ),
        if (view.hasMore) ...<Widget>[
          const SizedBox(height: AhSpace.md),
          AhButton(
            label: strings(DealerText.loadMore),
            onPressed: view.loadingMore
                ? null
                : () => unawaited(ref.read(inventoryProvider.notifier).loadMore()),
            variant: AhButtonVariant.outline,
            block: true,
          ),
        ],
      ],
    );
  }

  /// Matches the plate the dealer is holding, or the model they remember.
  ///
  /// The plate is matched against the **canonical** form with separators stripped from the needle,
  /// so `MH 12 AB` finds `MH12AB1234`. A dealer typing a plate off a windscreen types the spaces
  /// they can see, and a filter that fails on them is a filter they stop using.
  static List<InventoryItem> _filtered(List<InventoryItem> items, String query) {
    final needle = query.trim().toLowerCase();
    if (needle.isEmpty) return items;
    final plateNeedle = needle.replaceAll(RegExp('[^0-9a-z]'), '');

    return items.where((item) {
      final registration = item.listing.registration;
      return item.listing.title.toLowerCase().contains(needle) ||
          (plateNeedle.isNotEmpty &&
              registration.canonical.toLowerCase().contains(plateNeedle));
    }).toList();
  }

  static List<InventoryItem> _ordered(
    List<InventoryItem> items,
    InventorySort sort,
    DateTime now,
  ) {
    final ordered = items.toList();

    // Undated and priceless rows sort last in every order rather than to the top. A vehicle the
    // server did not date is not the oldest thing in stock, and one with no ask is not the
    // cheapest — sorting them as if they were is how a dealer chases a row that means nothing.
    switch (sort) {
      case InventorySort.newestIn:
        ordered.sort((a, b) => _ageOf(a, now, 1 << 30).compareTo(_ageOf(b, now, 1 << 30)));
      case InventorySort.oldestIn:
        ordered.sort((a, b) => _ageOf(b, now, -1).compareTo(_ageOf(a, now, -1)));
      case InventorySort.askHigh:
        ordered.sort((a, b) => (b.listing.priceMinor ?? -1).compareTo(a.listing.priceMinor ?? -1));
      case InventorySort.askLow:
        ordered.sort(
          (a, b) => (a.listing.priceMinor ?? 1 << 62).compareTo(b.listing.priceMinor ?? 1 << 62),
        );
      case InventorySort.mostLeads:
        ordered.sort((a, b) => (b.leadCount ?? -1).compareTo(a.leadCount ?? -1));
    }

    return ordered;
  }

  static int _ageOf(InventoryItem item, DateTime now, int whenUnknown) =>
      item.ageInDaysAt(now) ?? whenUnknown;
}

class _StockRow extends StatelessWidget {
  const _StockRow({
    required this.item,
    required this.now,
    required this.strings,
    required this.divider,
  });

  final InventoryItem item;
  final DateTime now;
  final DealerStrings strings;
  final bool divider;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final age = item.ageInDaysAt(now);
    final price = item.listing.priceMinor;
    final state = item.state;

    return ConsoleRow(
      divider: divider,
      onTap: () => context.push(DealerRoutes.stock(item.listingId)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Row(
            children: <Widget>[
              PlateBlock(
                registration: item.listing.registration.canonical,
                display: item.listing.registration.display,
                plateClass: plateClassFromWire(item.listing.registration.plateClass),
                size: PlateSize.xs,
              ),
              const SizedBox(width: AhSpace.sm),
              Expanded(
                child: Text(
                  item.listing.title,
                  style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              if (state != null) ...<Widget>[
                const SizedBox(width: AhSpace.sm),
                StatusPill(
                  category: state.category,
                  label: state.name,
                  size: StatusPillSize.sm,
                ),
              ],
            ],
          ),
          const SizedBox(height: AhSpace.xs),
          Row(
            children: <Widget>[
              _Cell(
                label: strings(DealerText.colAge),
                value: age == null ? ahAbsent : '$age${strings(DealerText.daysInStock)}',
              ),
              _Cell(
                label: strings(DealerText.colLeads),
                value: item.leadCount == null ? ahAbsent : '${item.leadCount}',
              ),
              const Spacer(),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  Text(
                    strings(DealerText.colAsk).toUpperCase(),
                    style: AhText.label.copyWith(color: surfaces.textSecondary),
                  ),
                  const SizedBox(height: 2),
                  if (price != null)
                    MoneyValue(minorUnits: price, compact: true, size: MoneySize.sm)
                  else
                    Text(
                      strings(DealerText.onRequest),
                      style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                    ),
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Cell extends StatelessWidget {
  const _Cell({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;

    return Padding(
      padding: const EdgeInsets.only(right: AhSpace.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Text(
            label.toUpperCase(),
            style: AhText.label.copyWith(color: surfaces.textSecondary),
          ),
          const SizedBox(height: 2),
          Text(
            value,
            style: AhText.data.copyWith(fontSize: 13, color: surfaces.textPrimary),
          ),
        ],
      ),
    );
  }
}
