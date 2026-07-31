import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/providers.dart';
import '../../app/strings.dart';
import '../../data/models/cursor_page.dart';
import '../../data/models/deal.dart';
import '../../widgets/async_section.dart';
import '../../widgets/console.dart';

final AutoDisposeFutureProvider<CursorPage<DealSummary>> walletDealsProvider =
    FutureProvider.autoDispose<CursorPage<DealSummary>>(
  (ref) => ref.watch(dealerApiProvider).deals(
        const DealListParams(side: DealSide.selling, limit: 40),
      ),
);

/// One deal in full, fetched only when a dealer opens it. This is where the commission lines are.
final AutoDisposeFutureProviderFamily<DealDetail, String> dealDetailProvider =
    FutureProvider.autoDispose.family<DealDetail, String>(
  (ref, dealId) => ref.watch(dealerApiProvider).deal(dealId),
);

/// Balances a dealer can argue from.
///
/// **Every figure is a ledger amount the server sent.** The balances sum `amountMinor` and group by
/// the workflow *category* — `succeeded` is settled, non-terminal is in flight — so the split comes
/// from the registry rather than from a list of state codes compiled into this binary.
///
/// **Nothing here computes a fee.** The web console derived a commission as `settled × 0.02`; that
/// is this app's opinion about somebody else's money, and it is wrong the day a gold dealer in
/// Maharashtra moves to a different slab. The commission lines are the [PricingLine]s the pricing
/// engine returned on the deal itself, each carrying the rule that produced it — which is the only
/// version of this screen support can answer a dispute from.
class WalletScreen extends ConsumerWidget {
  const WalletScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = ref.watch(stringsProvider);
    final deals = ref.watch(walletDealsProvider);

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(walletDealsProvider);
        try {
          await ref.read(walletDealsProvider.future);
        } on Object {
          // Balances stay on screen with a banner over them. A dealer mid-reconciliation must not
          // lose their figures to a failed refresh.
        }
      },
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: consolePagePadding(context),
        children: <Widget>[
          ConsolePageHeader(
            eyebrow: t(DealerText.walletEyebrow),
            title: t(DealerText.walletTitle),
            description: t(DealerText.walletDescription),
          ),
          const SizedBox(height: kConsoleBlockGap),
          AsyncSection<CursorPage<DealSummary>>(
            value: deals,
            onRetry: () => ref.invalidate(walletDealsProvider),
            builder: (context, page) => _Balances(deals: page.items, strings: t),
          ),
          const SizedBox(height: kConsoleBlockGap),
          _SettlementTerms(strings: t),
        ],
      ),
    );
  }
}

/// The ledger split, by workflow category.
///
/// Public and pure so the arithmetic can be tested without a widget. Money is integer paise
/// throughout — a marketplace that accumulates commission in `double` reconciles short by the end
/// of the month, and binary floating point cannot represent `0.1`.
@immutable
final class WalletBalances {
  const WalletBalances({
    required this.settledMinor,
    required this.inFlightMinor,
    required this.settledCount,
    required this.inFlightCount,
  });

  factory WalletBalances.of(Iterable<DealSummary> deals) {
    var settled = 0;
    var inFlight = 0;
    var settledCount = 0;
    var inFlightCount = 0;

    for (final deal in deals) {
      switch (deal.state.category) {
        case AhStateCategory.succeeded:
          settled += deal.amountMinor;
          settledCount += 1;

        case AhStateCategory.draft ||
              AhStateCategory.awaitingAction ||
              AhStateCategory.inProgress:
          inFlight += deal.amountMinor;
          inFlightCount += 1;

        // Terminal and not good. A rejected or withdrawn deal is neither settled nor coming: adding
        // it to either balance would overstate what the dealer is owed, and a wallet that
        // overstates is a wallet that produces a complaint at settlement.
        case AhStateCategory.failed || AhStateCategory.cancelled:
          break;
      }
    }

    return WalletBalances(
      settledMinor: settled,
      inFlightMinor: inFlight,
      settledCount: settledCount,
      inFlightCount: inFlightCount,
    );
  }

  final int settledMinor;
  final int inFlightMinor;
  final int settledCount;
  final int inFlightCount;
}

class _Balances extends StatelessWidget {
  const _Balances({required this.deals, required this.strings});

  final List<DealSummary> deals;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final balances = WalletBalances.of(deals);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        ConsoleMetrics(
          metrics: <ConsoleMetric>[
            ConsoleMetric(
              label: strings(DealerText.settled),
              value: MoneyValue(
                minorUnits: balances.settledMinor,
                compact: true,
                size: MoneySize.lg,
                tone: MoneyTone.positive,
              ),
              hint: '${balances.settledCount} ${strings(DealerText.dealCount)}',
            ),
            ConsoleMetric(
              label: strings(DealerText.inFlight),
              value: MoneyValue(
                minorUnits: balances.inFlightMinor,
                compact: true,
                size: MoneySize.lg,
              ),
              hint: '${balances.inFlightCount} ${strings(DealerText.dealCount)}',
            ),
          ],
        ),
        const SizedBox(height: kConsoleBlockGap),
        SectionHeading(
          title: strings(DealerText.recentDeals),
          description: strings(DealerText.commissionLinesHint),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.sm),
        if (deals.isEmpty)
          EmptyState(
            title: strings(DealerText.noDealsTitle),
            body: strings(DealerText.noDealsBody),
          )
        else
          for (var index = 0; index < deals.length; index += 1)
            _DealRow(
              deal: deals[index],
              strings: strings,
              divider: index != deals.length - 1,
            ),
        if (deals.isNotEmpty) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          Text(
            strings(DealerText.commissionLinesHint),
            style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
          ),
        ],
      ],
    );
  }
}

class _DealRow extends ConsumerWidget {
  const _DealRow({required this.deal, required this.strings, required this.divider});

  final DealSummary deal;
  final DealerStrings strings;
  final bool divider;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final surfaces = context.ahSurfaces;

    return ConsoleRow(
      divider: divider,
      onTap: () => unawaited(_openLines(context, ref)),
      child: Row(
        children: <Widget>[
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                Text(
                  deal.listing.title,
                  style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 2),
                Text(
                  deal.reference,
                  style: AhText.data.copyWith(fontSize: 12, color: surfaces.textSecondary),
                ),
              ],
            ),
          ),
          const SizedBox(width: AhSpace.sm),
          StatusPill(
            category: deal.state.category,
            label: deal.state.name,
            size: StatusPillSize.sm,
          ),
          const SizedBox(width: AhSpace.sm),
          MoneyValue(minorUnits: deal.amountMinor, compact: true, size: MoneySize.sm),
        ],
      ),
    );
  }

  /// The commission lines for one deal, straight from the pricing engine.
  Future<void> _openLines(BuildContext context, WidgetRef ref) {
    return showModalBottomSheet<void>(
      context: context,
      backgroundColor: context.ahSurfaces.card,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (sheetContext) => SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(AhSpace.lg, 0, AhSpace.lg, AhSpace.lg),
          child: Consumer(
            builder: (context, ref, _) => AsyncSection<DealDetail>(
              value: ref.watch(dealDetailProvider(deal.dealId)),
              onRetry: () => ref.invalidate(dealDetailProvider(deal.dealId)),
              builder: (context, detail) => _FeeLines(detail: detail, strings: strings),
            ),
          ),
        ),
      ),
    );
  }
}

class _FeeLines extends StatelessWidget {
  const _FeeLines({required this.detail, required this.strings});

  final DealDetail detail;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        SectionHeading(
          eyebrow: detail.summary.reference,
          title: strings(DealerText.commissionLines),
          size: SectionHeadingSize.sm,
        ),
        const SizedBox(height: AhSpace.lg),
        if (detail.fees.isEmpty)
          Text(
            strings(DealerText.noFeeLines),
            style: AhText.body.copyWith(color: surfaces.textSecondary),
          )
        else
          for (var index = 0; index < detail.fees.length; index += 1)
            _FeeLine(
              line: detail.fees[index],
              divider: index != detail.fees.length - 1,
            ),
        const SizedBox(height: AhSpace.lg),
        ConsoleMetrics(
          metrics: <ConsoleMetric>[
            ConsoleMetric(
              label: strings(DealerText.totalFees),
              value: MoneyValue(minorUnits: detail.totalFeesMinor, size: MoneySize.md),
            ),
            ConsoleMetric(
              label: strings(DealerText.payable),
              value: MoneyValue(minorUnits: detail.payableMinor, size: MoneySize.md),
            ),
            if (detail.escrowHeldMinor != null)
              ConsoleMetric(
                label: strings(DealerText.escrowHeld),
                value: MoneyValue(minorUnits: detail.escrowHeldMinor!, size: MoneySize.md),
              ),
          ],
        ),
      ],
    );
  }
}

class _FeeLine extends StatelessWidget {
  const _FeeLine({required this.line, required this.divider});

  final PricingLine line;
  final bool divider;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final rate = line.rate;

    return ConsoleRow(
      divider: divider,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                Text(
                  line.label,
                  style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
                ),
                if (line.ruleName != null) ...<Widget>[
                  const SizedBox(height: 2),
                  Text(
                    // The rule that produced the number. A dealer *will* ask why they were charged,
                    // and "the code says so" is not an answer support can give.
                    line.ruleName!,
                    style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: AhSpace.sm),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              MoneyValue(minorUnits: line.amountMinor, size: MoneySize.sm),
              if (rate != null) ...<Widget>[
                const SizedBox(height: 2),
                Text(
                  // The server sends a fraction — `0.02` is 2%. Rendering it is display only; no
                  // amount in this app is ever derived by multiplying by it.
                  '${formatIndianNumber(rate * 100)}%',
                  style: AhText.data.copyWith(fontSize: 12, color: surfaces.textSecondary),
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }
}

/// Settlement terms, every value from the localisation registry.
///
/// "T+2 working days" reads like a constant and is not one — it is a commercial term the platform
/// can change, and hardcoding it is how a dealer ends up quoted a cycle finance stopped honouring
/// last quarter.
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
      ],
    );
  }
}
