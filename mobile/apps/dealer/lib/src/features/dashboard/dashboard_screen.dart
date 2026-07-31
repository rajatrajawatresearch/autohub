import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/providers.dart';
import '../../app/routes.dart';
import '../../app/strings.dart';
import '../../data/models/cursor_page.dart';
import '../../data/models/deal.dart';
import '../../data/models/inventory_item.dart';
import '../../data/server_time.dart';
import '../../widgets/async_section.dart';
import '../../widgets/console.dart';

/// A wide slice of stock, for the totals. Deliberately one request rather than a per-tile endpoint
/// the contract does not have — see [DealerDashboard] for what is and is not derived here.
final AutoDisposeFutureProvider<CursorPage<InventoryItem>> dashboardStockProvider =
    FutureProvider.autoDispose<CursorPage<InventoryItem>>(
  (ref) => ref.watch(dealerApiProvider).inventory(limit: 60),
);

final AutoDisposeFutureProvider<Clocked<CursorPage<AuctionLot>>> dashboardLotsProvider =
    FutureProvider.autoDispose<Clocked<CursorPage<AuctionLot>>>(
  (ref) => ref.watch(dealerApiProvider).auctionLots(limit: 6),
);

/// The dealer's own side of the table. `side=selling` is the query grammar the deals endpoint
/// defines, not a role check invented here.
final AutoDisposeFutureProvider<CursorPage<DealSummary>> dashboardDealsProvider =
    FutureProvider.autoDispose<CursorPage<DealSummary>>(
  (ref) => ref.watch(dealerApiProvider).deals(
        const DealListParams(side: DealSide.selling, limit: 60),
      ),
);

/// A reconciliation screen, not a marketing surface.
///
/// What a dealer asks at 9am is "what is my money doing and what needs me today", so the page
/// answers exactly that in mono, on hairlines, and gets out of the way. No donut charts, no
/// gradient KPI cards, and nothing animated — `01-design-system.md` spends the entire motion budget
/// on the odometer roll, which is a customer surface.
///
/// **Every figure here is the server's.** Stock value sums `priceMinor`; payout due sums
/// `amountMinor` on deals the ledger has not settled. Nothing multiplies an amount by a rate — a
/// commission on this screen would be this binary's opinion about money, and the pricing registry
/// is the only thing entitled to one.
class DealerDashboard extends ConsumerWidget {
  const DealerDashboard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = ref.watch(stringsProvider);

    return RefreshIndicator(
      onRefresh: () async {
        ref
          ..invalidate(dashboardStockProvider)
          ..invalidate(dashboardLotsProvider)
          ..invalidate(dashboardDealsProvider);
        try {
          await Future.wait<void>(<Future<void>>[
            ref.read(dashboardStockProvider.future),
            ref.read(dashboardLotsProvider.future),
            ref.read(dashboardDealsProvider.future),
          ]);
        } on Object {
          // Swallowed on purpose: each section renders its own banner over whatever it still has,
          // and a pull-to-refresh that throws would take down the page the dealer is holding.
        }
      },
      child: ListView(
        // Always scrollable, so pull-to-refresh works on a short page — a dealer with two vehicles
        // must still be able to refresh.
        physics: const AlwaysScrollableScrollPhysics(),
        padding: consolePagePadding(context),
        children: <Widget>[
          ConsolePageHeader(
            eyebrow: t(DealerText.dashboardEyebrow),
            title: t(DealerText.dashboardTitle),
            description: t(DealerText.dashboardDescription),
          ),
          const SizedBox(height: kConsoleBlockGap),
          const _MetricStrip(),
          const SizedBox(height: kConsoleBlockGap),
          _NeedsYouToday(strings: t),
          const SizedBox(height: kConsoleBlockGap),
          _LiveFloor(strings: t),
          const SizedBox(height: kConsoleBlockGap),
          _SettlementTerms(strings: t),
        ],
      ),
    );
  }
}

class _MetricStrip extends ConsumerWidget {
  const _MetricStrip();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = ref.watch(stringsProvider);
    final stock = ref.watch(dashboardStockProvider);
    final lots = ref.watch(dashboardLotsProvider);
    final deals = ref.watch(dashboardDealsProvider);

    final items = stock.valueOrNull?.items ?? const <InventoryItem>[];
    final ageing = AgeingProfile.of(items, DateTime.now().toUtc());

    // Sums are computed over what actually arrived. A partial page under-reports rather than
    // guesses, and the count beside the figure says how many rows it covers — a total whose
    // denominator is invisible is the kind of number a dealer stops trusting after one surprise.
    final stockValueMinor = items.fold<int>(
      0,
      (sum, item) => sum + (item.listing.priceMinor ?? 0),
    );

    final unsettled = (deals.valueOrNull?.items ?? const <DealSummary>[])
        .where((deal) => !deal.state.isTerminal)
        .toList();
    final payoutMinor = unsettled.fold<int>(0, (sum, deal) => sum + deal.amountMinor);

    return ConsoleMetrics(
      metrics: <ConsoleMetric>[
        ConsoleMetric(
          label: t(DealerText.stockValue),
          value: stock.hasValue
              ? MoneyValue(minorUnits: stockValueMinor, compact: true, size: MoneySize.lg)
              : const AhSkeleton(width: 96, height: 26),
          hint: stock.hasValue ? '${items.length} ${t(DealerText.vehiclesInStock)}' : null,
        ),
        ConsoleMetric(
          label: t(DealerText.vehiclesInStock),
          value: stock.hasValue
              ? _Numeral('${items.length}')
              : const AhSkeleton(width: 48, height: 26),
        ),
        ConsoleMetric(
          label: '${t(DealerText.ageing)} ${AgeingProfile.labelFor(ahAgeingBucketEdges.length)}',
          value: stock.hasValue
              ? _Numeral('${ageing.overOldestEdge}')
              : const AhSkeleton(width: 48, height: 26),
          hint: t(DealerText.ageingHint),
        ),
        ConsoleMetric(
          label: t(DealerText.liveLots),
          value: lots.hasValue
              ? _Numeral('${lots.valueOrNull?.value.items.length ?? 0}')
              : const AhSkeleton(width: 48, height: 26),
        ),
        ConsoleMetric(
          label: t(DealerText.payoutDue),
          value: deals.hasValue
              ? MoneyValue(minorUnits: payoutMinor, compact: true, size: MoneySize.lg)
              : const AhSkeleton(width: 96, height: 26),
          hint: deals.hasValue ? t(DealerText.payoutDueHint) : null,
        ),
      ],
    );
  }
}

/// A count, in the data face. Tabular figures so a column of them agrees on its digits.
class _Numeral extends StatelessWidget {
  const _Numeral(this.value);

  final String value;

  @override
  Widget build(BuildContext context) => Text(
        value,
        style: AhText.data.copyWith(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          color: context.ahSurfaces.textPrimary,
        ),
      );
}

class _NeedsYouToday extends ConsumerWidget {
  const _NeedsYouToday({required this.strings});

  final DealerStrings strings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final stock = ref.watch(dashboardStockProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        SectionHeading(
          title: strings(DealerText.needsYouToday),
          size: SectionHeadingSize.sm,
          rule: true,
          action: AhButton(
            label: strings(DealerText.navInventory),
            onPressed: () => context.go(DealerRoutes.inventory),
            variant: AhButtonVariant.ghost,
            size: AhButtonSize.sm,
          ),
        ),
        const SizedBox(height: AhSpace.sm),
        AsyncSection<CursorPage<InventoryItem>>(
          value: stock,
          onRetry: () => ref.invalidate(dashboardStockProvider),
          loading: const _RowSkeletons(),
          builder: (context, page) {
            // Oldest first: the point of this list is what is *not* moving. Sorting by newest
            // would show a dealer the stock they already remember buying.
            final now = DateTime.now().toUtc();
            final items = page.items.toList()
              ..sort((a, b) => (b.ageInDaysAt(now) ?? -1).compareTo(a.ageInDaysAt(now) ?? -1));
            final shown = items.take(6).toList();

            if (shown.isEmpty) {
              return EmptyState(
                title: strings(DealerText.noStockTitle),
                body: strings(DealerText.noStockBody),
              );
            }

            return Column(
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                for (var index = 0; index < shown.length; index += 1)
                  _StockLine(
                    item: shown[index],
                    strings: strings,
                    divider: index != shown.length - 1,
                  ),
              ],
            );
          },
        ),
      ],
    );
  }
}

class _StockLine extends StatelessWidget {
  const _StockLine({required this.item, required this.strings, required this.divider});

  final InventoryItem item;
  final DealerStrings strings;
  final bool divider;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final age = item.ageInDaysAt(DateTime.now().toUtc());
    final price = item.listing.priceMinor;

    return ConsoleRow(
      divider: divider,
      onTap: () => context.push(DealerRoutes.stock(item.listingId)),
      child: Row(
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
          const SizedBox(width: AhSpace.sm),
          Text(
            age == null ? ahAbsent : '$age${strings(DealerText.daysInStock)}',
            style: AhText.data.copyWith(fontSize: 13, color: surfaces.textSecondary),
          ),
          const SizedBox(width: AhSpace.sm),
          if (price != null)
            MoneyValue(
              minorUnits: price,
              compact: true,
              size: MoneySize.xs,
              tone: MoneyTone.secondary,
            )
          else
            Text(
              strings(DealerText.onRequest),
              style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
            ),
        ],
      ),
    );
  }
}

class _LiveFloor extends ConsumerWidget {
  const _LiveFloor({required this.strings});

  final DealerStrings strings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lots = ref.watch(dashboardLotsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        SectionHeading(
          title: strings(DealerText.auctionTitle),
          size: SectionHeadingSize.sm,
          rule: true,
          action: AhButton(
            label: strings(DealerText.openLot),
            onPressed: () => context.go(DealerRoutes.auction),
            variant: AhButtonVariant.ghost,
            size: AhButtonSize.sm,
          ),
        ),
        const SizedBox(height: AhSpace.sm),
        AsyncSection<Clocked<CursorPage<AuctionLot>>>(
          value: lots,
          onRetry: () => ref.invalidate(dashboardLotsProvider),
          loading: const _RowSkeletons(),
          builder: (context, clocked) {
            final items = clocked.value.items;
            if (items.isEmpty) {
              return EmptyState(
                title: strings(DealerText.noLotsTitle),
                body: strings(DealerText.noLotsBody),
              );
            }

            return Column(
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                for (var index = 0; index < items.length; index += 1)
                  _LotLine(
                    lot: items[index],
                    clock: clocked.clock,
                    strings: strings,
                    divider: index != items.length - 1,
                  ),
              ],
            );
          },
        ),
      ],
    );
  }
}

class _LotLine extends StatelessWidget {
  const _LotLine({
    required this.lot,
    required this.clock,
    required this.strings,
    required this.divider,
  });

  final AuctionLot lot;
  final AhServerClock clock;
  final DealerStrings strings;
  final bool divider;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final endsAt = DateTime.tryParse(lot.endsAt);
    final bid = lot.currentBidMinor;

    return ConsoleRow(
      divider: divider,
      onTap: () => context.push(DealerRoutes.lot(lot.lotId)),
      child: Row(
        children: <Widget>[
          Expanded(
            child: Text(
              lot.listing.title,
              style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          const SizedBox(width: AhSpace.sm),
          if (endsAt != null)
            Text(
              // The server clock, not the handset's. A dashboard countdown that disagrees with the
              // lot screen's is a dealer who does not know which one to believe.
              formatRemaining(clock.remainingUntil(endsAt)),
              style: AhText.data.copyWith(fontSize: 13, color: surfaces.textSecondary),
            ),
          const SizedBox(width: AhSpace.sm),
          StatusPill(
            category: lot.state.category,
            label: lot.state.name,
            size: StatusPillSize.sm,
          ),
          const SizedBox(width: AhSpace.sm),
          if (bid != null)
            MoneyValue(minorUnits: bid, compact: true, size: MoneySize.xs)
          else
            Text(
              strings(DealerText.noBidsTitle),
              style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
            ),
        ],
      ),
    );
  }
}

/// Settlement terms.
///
/// Every value resolves through the localisation registry — including "T+2 working days", which is
/// a commercial term the platform can change without shipping a binary. It reads as a constant and
/// it is not one; hardcoding it here is how a dealer ends up quoted a cycle the finance team
/// stopped honouring last quarter.
class _SettlementTerms extends StatelessWidget {
  const _SettlementTerms({required this.strings});

  final DealerStrings strings;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        SectionHeading(
          title: strings(DealerText.settlement),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Expanded(
              child: FieldBlock(
                label: strings(DealerText.settlementCycle),
                value: strings(DealerText.settlementCycleValue),
                mono: true,
              ),
            ),
            const SizedBox(width: AhSpace.lg),
            Expanded(
              child: FieldBlock(
                label: strings(DealerText.settlementCutOff),
                value: strings(DealerText.settlementCutOffValue),
                mono: true,
              ),
            ),
          ],
        ),
        const SizedBox(height: AhSpace.lg),
        FieldBlock(
          label: strings(DealerText.commissionTerms),
          value: strings(DealerText.commissionTermsValue),
          hint: strings(DealerText.commissionTermsHint),
        ),
      ],
    );
  }
}

class _RowSkeletons extends StatelessWidget {
  const _RowSkeletons();

  @override
  Widget build(BuildContext context) {
    final height = context.ahDensity.rowHeight;
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        for (var index = 0; index < 4; index += 1)
          Padding(
            padding: const EdgeInsets.only(bottom: AhSpace.xs),
            child: AhSkeleton(height: height - AhSpace.xs),
          ),
      ],
    );
  }
}
