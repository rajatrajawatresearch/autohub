import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/providers.dart';
import '../../app/routes.dart';
import '../../app/strings.dart';
import '../../data/models/cursor_page.dart';
import '../../data/server_time.dart';
import '../../widgets/async_section.dart';
import '../../widgets/console.dart';

final AutoDisposeFutureProvider<Clocked<CursorPage<AuctionLot>>> auctionFloorProvider =
    FutureProvider.autoDispose<Clocked<CursorPage<AuctionLot>>>(
  (ref) => ref.watch(dealerApiProvider).auctionLots(limit: 24),
);

/// A wall of live lots.
///
/// **No bidding happens here.** Every ticket links to its lot and the bid lives behind a
/// confirmation sheet there, because a mis-tap on a scrolling grid of six countdowns is an
/// expensive way to buy a truck. The floor's job is to show a dealer where their attention is
/// needed and get them onto the right lot.
///
/// This is the one screen where the rationed yellow is deliberately repeated: each ticket's
/// countdown strip carries it, and every lot on the page genuinely is time-boxed and irreversible.
/// That is why the control inside each ticket stays primary green — the ticket has already spent
/// that card's yellow.
class AuctionFloorScreen extends ConsumerWidget {
  const AuctionFloorScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = ref.watch(stringsProvider);
    final floor = ref.watch(auctionFloorProvider);

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(auctionFloorProvider);
        try {
          await ref.read(auctionFloorProvider.future);
        } on Object {
          // Surfaced by the section's own banner over the lots already on screen. A dealer must
          // never lose sight of a running countdown because a refresh failed.
        }
      },
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: consolePagePadding(context),
        children: <Widget>[
          ConsolePageHeader(
            eyebrow: t(DealerText.auctionEyebrow),
            title: t(DealerText.auctionTitle),
            description: t(DealerText.auctionDescription),
          ),
          const SizedBox(height: kConsoleBlockGap),
          AsyncSection<Clocked<CursorPage<AuctionLot>>>(
            value: floor,
            onRetry: () => ref.invalidate(auctionFloorProvider),
            loading: const _FloorSkeleton(),
            builder: (context, clocked) {
              final lots = clocked.value.items;
              if (lots.isEmpty) {
                return EmptyState(
                  title: t(DealerText.noLotsTitle),
                  body: t(DealerText.noLotsBody),
                );
              }

              return Column(
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  for (final lot in lots)
                    Padding(
                      padding: const EdgeInsets.only(bottom: AhSpace.lg),
                      child: _LotCard(lot: lot, clock: clocked.clock, strings: t),
                    ),
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}

class _LotCard extends ConsumerWidget {
  const _LotCard({required this.lot, required this.clock, required this.strings});

  final AuctionLot lot;
  final AhServerClock clock;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final surfaces = context.ahSurfaces;
    final endsAt = DateTime.tryParse(lot.endsAt);

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AhSpace.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                PlateBlock(
                  registration: lot.listing.registration.canonical,
                  display: lot.listing.registration.display,
                  plateClass: plateClassFromWire(lot.listing.registration.plateClass),
                  size: PlateSize.xs,
                ),
                const Spacer(),
                StatusPill(
                  category: lot.state.category,
                  label: lot.state.name,
                  size: StatusPillSize.sm,
                ),
              ],
            ),
            const SizedBox(height: AhSpace.sm),
            Text(
              lot.listing.title,
              style: AhText.subheading.copyWith(color: surfaces.textPrimary),
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
            if (lot.listing.specStrip.isNotEmpty) ...<Widget>[
              const SizedBox(height: AhSpace.sm),
              SpecStrip(
                items: lot.listing.specStrip.take(4).toList(),
                size: SpecStripSize.sm,
                labels: false,
              ),
            ],
            const SizedBox(height: AhSpace.md),
            if (endsAt != null)
              BidTicket(
                endsAt: endsAt,
                // The clock measured when this page was fetched. `BidTicket` has no parameter that
                // lets a caller fall back to the handset clock by omission, and this is why.
                clock: clock,
                currentBidMinor: lot.currentBidMinor,
                bidCount: lot.bidCount,
                position: bidPositionOf(lot),
                yourMaxMinor: lot.yourMaxBidMinor,
                extended: lot.extensionCount > 0,
                labels: bidTicketLabels(strings),
                child: AhButton(
                  label: strings(DealerText.openLot),
                  onPressed: () => context.push(DealerRoutes.lot(lot.lotId)),
                  block: true,
                ),
              )
            else
              AhButton(
                label: strings(DealerText.openLot),
                onPressed: () => context.push(DealerRoutes.lot(lot.lotId)),
                variant: AhButtonVariant.outline,
                block: true,
              ),
            const SizedBox(height: AhSpace.sm),
            Text(
              <String>[
                lot.reserveMet
                    ? strings(DealerText.reserveMet)
                    : strings(DealerText.reserveNotMet),
                if (lot.lotNumber != null) '${strings(DealerText.lotNumber)} ${lot.lotNumber}',
              ].join(' · '),
              style: AhText.data.copyWith(fontSize: 12, color: surfaces.textSecondary),
            ),
          ],
        ),
      ),
    );
  }
}

/// Where the dealer stands, **as the server last reported it**.
///
/// Derived from `highestBidderIsYou` and nothing else. Comparing a local maximum against the
/// current bid gives an answer that is right most of the time, and "you are leading" being wrong
/// even occasionally is showing a dealer something false about money.
BidPosition bidPositionOf(AuctionLot lot) {
  if (lot.highestBidderIsYou) return BidPosition.leading;
  return lot.bidCount > 0 ? BidPosition.outbid : BidPosition.none;
}

/// The ticket's chrome, resolved through the localisation registry.
///
/// `BidTicketLabels`' defaults are en-IN and its own doc says to pass resolved strings in
/// production — otherwise the auction floor is the one screen that cannot ship in Marathi.
BidTicketLabels bidTicketLabels(DealerStrings strings) => BidTicketLabels(
      currentBid: strings(DealerText.currentBid),
      noBids: strings(DealerText.noBidsTitle),
      bids: strings(DealerText.bidsSoFar),
      yourMax: strings(DealerText.yourMax),
      leading: strings(DealerText.leading),
      outbid: strings(DealerText.outbid),
      timeLeft: strings(DealerText.timeLeft),
      closed: strings(DealerText.lotClosedLabel),
      extended: strings(DealerText.lotExtended),
    );

class _FloorSkeleton extends StatelessWidget {
  const _FloorSkeleton();

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        for (var index = 0; index < 3; index += 1)
          const Padding(
            padding: EdgeInsets.only(bottom: AhSpace.lg),
            // Sized to a real card, not a guess. A skeleton that is the wrong height promises a
            // shape and then moves the countdown the dealer was reaching for.
            child: AhSkeleton.block(height: 260),
          ),
      ],
    );
  }
}
