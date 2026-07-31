import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/providers.dart';
import '../../app/strings.dart';
import '../../data/auction_feed.dart';
import '../../widgets/async_section.dart';
import '../../widgets/console.dart';
import 'auction_floor_screen.dart' show bidPositionOf, bidTicketLabels;
import 'bid_panel.dart';
import 'lot_controller.dart';

/// The ladder, newest bid first. Bidders are masked from each other by the server.
final AutoDisposeFutureProviderFamily<CursorPage<Bid>, String> lotBidsProvider =
    FutureProvider.autoDispose.family<CursorPage<Bid>, String>(
  (ref, lotId) => ref.watch(dealerApiProvider).lotBids(lotId),
);

/// One lot: media, inspection, terms, the ladder, and the bid panel.
///
/// The countdown and the bid figure both come from [LotController], which folds the read-only
/// socket into a single immutable view. Nothing on this page reads `DateTime.now()` and nothing
/// composes a price.
class LotScreen extends ConsumerWidget {
  const LotScreen({required this.lotId, super.key});

  final String lotId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = ref.watch(stringsProvider);
    final view = ref.watch(lotProvider(lotId));

    return Scaffold(
      appBar: AppBar(
        title: Text(
          view.valueOrNull?.lot.listing.title ?? t(DealerText.auctionTitle),
          style: AhText.subheading.copyWith(color: context.ahSurfaces.textPrimary),
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
        ),
        actions: <Widget>[
          if (view.valueOrNull != null)
            Padding(
              padding: const EdgeInsets.only(right: AhSpace.md),
              child: _FeedIndicator(status: view.valueOrNull!.feed, strings: t),
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () => ref.read(lotProvider(lotId).notifier).refresh(),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: consolePagePadding(context),
          children: <Widget>[
            AsyncSection<LotView>(
              value: view,
              onRetry: () => ref.invalidate(lotProvider(lotId)),
              loading: const AhSkeleton.block(height: 320),
              builder: (context, data) => _Lot(view: data, strings: t),
            ),
          ],
        ),
      ),
    );
  }
}

class _Lot extends ConsumerWidget {
  const _Lot({required this.view, required this.strings});

  final LotView view;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final surfaces = context.ahSurfaces;
    final lot = view.lot;
    final endsAt = view.endsAt;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        _Media(lot: lot),
        const SizedBox(height: AhSpace.lg),
        Wrap(
          spacing: AhSpace.sm,
          runSpacing: AhSpace.sm,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: <Widget>[
            PlateBlock(
              registration: lot.listing.registration.canonical,
              display: lot.listing.registration.display,
              plateClass: plateClassFromWire(lot.listing.registration.plateClass),
            ),
            StatusPill(category: lot.state.category, label: lot.state.name),
            if (lot.lotNumber != null)
              AhBadge(
                label: '${strings(DealerText.lotNumber)} ${lot.lotNumber}',
                tone: AhBadgeTone.outline,
                size: AhBadgeSize.sm,
              ),
          ],
        ),
        const SizedBox(height: AhSpace.md),
        Text(lot.listing.title, style: AhText.heading.copyWith(color: surfaces.textPrimary)),
        if (lot.listing.specStrip.isNotEmpty) ...<Widget>[
          const SizedBox(height: AhSpace.md),
          DecoratedBox(
            decoration: BoxDecoration(
              border: Border.symmetric(
                horizontal: BorderSide(color: surfaces.borderHairline),
              ),
            ),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: AhSpace.md),
              child: SpecStrip(items: lot.listing.specStrip),
            ),
          ),
        ],
        const SizedBox(height: kConsoleBlockGap),

        // The ticket owns the countdown and the current figure; the panel inside it owns the
        // action. Neither of them owns both, which is what keeps "what the server said" and "what
        // the dealer is composing" from being the same piece of state.
        if (endsAt != null)
          BidTicket(
            endsAt: endsAt,
            clock: view.clock,
            currentBidMinor: lot.currentBidMinor,
            bidCount: lot.bidCount,
            position: bidPositionOf(lot),
            yourMaxMinor: lot.yourMaxBidMinor,
            extended: lot.extensionCount > 0,
            labels: bidTicketLabels(strings),
            // The countdown reaching zero is not proof the lot closed — the server decides that,
            // and an anti-snipe extension may already have moved the close time. So refetch rather
            // than assume.
            onEnd: () => ref.read(lotProvider(lot.lotId).notifier).refresh(),
            child: BidPanel(view: view),
          )
        else
          BidPanel(view: view),

        const SizedBox(height: kConsoleBlockGap),
        _Terms(lot: lot, strings: strings),
        const SizedBox(height: kConsoleBlockGap),
        _Ladder(lotId: lot.lotId, strings: strings),
        const SizedBox(height: kConsoleBlockGap),
        Text(
          strings(DealerText.bindingNotice),
          style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
        ),
      ],
    );
  }
}

/// The lot's cover, at the aspect ratio the server declared.
///
/// The ratio is honoured whether the bytes have arrived or not. A card that sizes itself to
/// whatever pixels turn up moves the countdown the dealer was reaching for, on the connection where
/// the image takes longest — which is the showroom wifi this screen is used on.
class _Media extends StatelessWidget {
  const _Media({required this.lot});

  final AuctionLot lot;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final media = lot.listing.media;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.sunken,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: ClipRRect(
        borderRadius: const BorderRadius.all(AhRadius.card),
        child: AspectRatio(
          aspectRatio: media.displayAspectRatio,
          child: media.hasCover
              ? CachedNetworkImage(
                  imageUrl: media.coverUrl!,
                  fit: BoxFit.cover,
                  // A photo that will not load leaves the reserved box empty rather than showing an
                  // error glyph. A lot whose cover is missing is an ordinary state, not a fault,
                  // and the box keeps its height either way so nothing below it moves.
                  errorWidget: (_, __, ___) => const SizedBox.shrink(),
                  placeholder: (_, __) => const SizedBox.shrink(),
                )
              : const SizedBox.shrink(),
        ),
      ),
    );
  }
}

class _Terms extends StatelessWidget {
  const _Terms({required this.lot, required this.strings});

  final AuctionLot lot;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        SectionHeading(
          title: strings(DealerText.lotTerms),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        ConsoleMetrics(
          metrics: <ConsoleMetric>[
            ConsoleMetric(
              label: strings(DealerText.startPrice),
              value: MoneyValue(minorUnits: lot.startPriceMinor, compact: true),
            ),
            ConsoleMetric(
              label: strings(DealerText.increment),
              value: MoneyValue(minorUnits: lot.incrementMinor, compact: true),
              hint: strings(DealerText.incrementHint),
            ),
            ConsoleMetric(
              label: strings(DealerText.reserve),
              value: Text(
                lot.reserveMet
                    ? strings(DealerText.reserveMet)
                    : strings(DealerText.reserveNotMet),
                style: AhText.data.copyWith(color: context.ahSurfaces.textPrimary),
              ),
              hint: strings(DealerText.reserveHint),
            ),
            ConsoleMetric(
              label: strings(DealerText.bidsSoFar),
              value: Text(
                '${lot.bidCount}',
                style: AhText.data.copyWith(color: context.ahSurfaces.textPrimary),
              ),
            ),
            ConsoleMetric(
              label: strings(DealerText.extensions),
              value: Text(
                '${lot.extensionCount}',
                style: AhText.data.copyWith(color: context.ahSurfaces.textPrimary),
              ),
              hint: strings(DealerText.extensionsHint),
            ),
            ConsoleMetric(
              label: strings(DealerText.inspection),
              value: Text(
                lot.inspectionReportUrl == null
                    ? strings(DealerText.inspectionNone)
                    : strings(DealerText.inspectionOpen),
                style: AhText.body.copyWith(color: context.ahSurfaces.textPrimary),
              ),
            ),
          ],
        ),
      ],
    );
  }
}

/// The bid ladder as the server ordered it.
///
/// Bidders arrive already masked — `Dealer ••42` — because the server masks them. This screen has
/// no way to reveal an identity and no reason to want one.
class _Ladder extends ConsumerWidget {
  const _Ladder({required this.lotId, required this.strings});

  final String lotId;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bids = ref.watch(lotBidsProvider(lotId));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        SectionHeading(
          title: strings(DealerText.bidLadder),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.sm),
        AsyncSection<CursorPage<Bid>>(
          value: bids,
          onRetry: () => ref.invalidate(lotBidsProvider(lotId)),
          builder: (context, page) {
            if (page.items.isEmpty) {
              return EmptyState(
                title: strings(DealerText.noBidsTitle),
                body: strings(DealerText.noBidsBody),
                align: CrossAxisAlignment.start,
              );
            }

            return Column(
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                for (var index = 0; index < page.items.length; index += 1)
                  _BidRow(
                    bid: page.items[index],
                    strings: strings,
                    divider: index != page.items.length - 1,
                  ),
              ],
            );
          },
        ),
      ],
    );
  }
}

class _BidRow extends StatelessWidget {
  const _BidRow({required this.bid, required this.strings, required this.divider});

  final Bid bid;
  final DealerStrings strings;
  final bool divider;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;
    final placedAt = DateTime.tryParse(bid.placedAt);

    return ConsoleRow(
      divider: divider,
      child: Row(
        children: <Widget>[
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                Text(
                  bid.isYours ? strings(DealerText.yourBid) : bid.bidderMask,
                  style: AhText.bodySm.copyWith(
                    color: bid.isYours ? surfaces.accentPrimary : surfaces.textPrimary,
                    fontWeight: bid.isYours ? FontWeight.w600 : FontWeight.w400,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (placedAt != null)
                  Text(
                    // IST, not device-local. A dealer comparing a ladder against what a colleague
                    // in the Pune office saw needs both screens to agree.
                    formatIstStamp(placedAt),
                    style: AhText.data.copyWith(fontSize: 12, color: surfaces.textSecondary),
                  ),
              ],
            ),
          ),
          MoneyValue(minorUnits: bid.amountMinor, size: MoneySize.sm),
        ],
      ),
    );
  }
}

/// Whether the figure on screen is live.
///
/// A dealer bidding on a truck is entitled to know whether they are looking at a broadcast or at
/// the last thing that arrived before the wifi went — "the screen looked current" is the story
/// behind every disputed bid.
class _FeedIndicator extends StatelessWidget {
  const _FeedIndicator({required this.status, required this.strings});

  final AuctionFeedStatus status;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context) {
    final (String key, AhBadgeTone tone) = switch (status) {
      AuctionFeedStatus.open => (DealerText.feedLive, AhBadgeTone.clear),
      AuctionFeedStatus.connecting => (DealerText.feedConnecting, AhBadgeTone.neutral),
      AuctionFeedStatus.closed => (DealerText.feedOffline, AhBadgeTone.caution),
    };

    return Center(
      child: AhBadge(label: strings(key), tone: tone, size: AhBadgeSize.sm),
    );
  }
}
