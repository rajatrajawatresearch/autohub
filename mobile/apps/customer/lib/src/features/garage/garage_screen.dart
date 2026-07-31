import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/router.dart';
import '../../data/app_providers.dart';
import '../../data/garage.dart';
import '../../data/provisional_models.dart';
import '../../data/session.dart';
import '../../data/valuation.dart';
import '../../i18n/customer_text.dart';
import '../../widgets/listing_card.dart';

/// Everything of this person's that AutoHub knows about.
///
/// Three sections, and they come from three different places on purpose:
///
/// * **Vehicles** are the valuations this phone has taken. They are local, they work signed out,
///   and they work in a lift — a valuation is an unauthenticated question about your own car, and
///   the answer belongs on the device that asked.
/// * **Listings** and **bookings** are the server's, behind the session.
/// * **Waiting to go up** is the outbox, surfaced rather than hidden. A seller whose listing is
///   sitting in a queue has a right to know it has not gone up yet; the alternative is somebody who
///   submitted on a train, saw nothing, and submitted again from the office.
class GarageScreen extends ConsumerWidget {
  const GarageScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final SessionState session = ref.watch(sessionProvider);

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async {
            ref
              ..invalidate(savedValuationsProvider)
              ..invalidate(myListingsProvider)
              ..invalidate(myBookingsProvider);
          },
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(AhSpace.lg),
            children: <Widget>[
              SectionHeading(
                title: t(CustomerText.garageTitle),
                rule: true,
                action: session is SignedIn
                    ? AhButton(
                        label: t(CustomerText.garageSignOut),
                        variant: AhButtonVariant.ghost,
                        size: AhButtonSize.sm,
                        onPressed: () =>
                            unawaited(ref.read(sessionProvider.notifier).signOut()),
                      )
                    : null,
              ),
              const SizedBox(height: AhSpace.xl),
              const _PendingWrites(),
              const _Vehicles(),
              if (session is SignedIn) ...<Widget>[
                const SizedBox(height: AhSpace.xxl),
                const _Listings(),
                const SizedBox(height: AhSpace.xxl),
                const _Bookings(),
              ] else ...<Widget>[
                const SizedBox(height: AhSpace.xxl),
                EmptyState(
                  title: t(CustomerText.garageSignedOutTitle),
                  body: t(CustomerText.garageSignedOutBody),
                  action: AhButton(
                    label: t(CustomerText.garageSignIn),
                    onPressed: () => context.push(Routes.login),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

/* ── Writes still on the phone ─────────────────────────────────────────────── */

class _PendingWrites extends ConsumerWidget {
  const _PendingWrites();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final List<OutboxEntry> pending =
        ref.watch(pendingWritesProvider).valueOrNull ?? const <OutboxEntry>[];
    if (pending.isEmpty) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.only(bottom: AhSpace.xl),
      child: AhBadge(
        label: t(CustomerText.garageQueued, <String, Object?>{'count': pending.length}),
        // Live and money-moving, so it spends the screen's rationed yellow. Nothing else in the
        // garage may wear it.
        tone: AhBadgeTone.action,
      ),
    );
  }
}

/* ── Vehicles ──────────────────────────────────────────────────────────────── */

class _Vehicles extends ConsumerWidget {
  const _Vehicles();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final List<ValuationQuote> quotes =
        ref.watch(savedValuationsProvider).valueOrNull ?? const <ValuationQuote>[];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(title: t(CustomerText.garageVehicles), size: SectionHeadingSize.sm),
        const SizedBox(height: AhSpace.lg),
        if (quotes.isEmpty)
          Text(
            t(CustomerText.garageVehiclesEmpty),
            style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
          )
        else
          for (final ValuationQuote quote in quotes)
            Padding(
              padding: const EdgeInsets.only(bottom: AhSpace.md),
              child: _VehicleRow(quote: quote),
            ),
      ],
    );
  }
}

class _VehicleRow extends ConsumerWidget {
  const _VehicleRow({required this.quote});

  final ValuationQuote quote;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final ValuationBand? headline = quote.primary;

    return Semantics(
      button: true,
      label: quote.vehicle.title,
      excludeSemantics: true,
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          // The quote rides along in `extra`, so reopening it costs nothing and works offline.
          onTap: () => context.push(Routes.valuation(quote.quoteId), extra: quote),
          splashFactory: NoSplash.splashFactory,
          hoverColor: Colors.transparent,
          focusColor: Colors.transparent,
          highlightColor: Colors.transparent,
          child: DecoratedBox(
            decoration: BoxDecoration(
              color: surfaces.card,
              borderRadius: const BorderRadius.all(AhRadius.card),
              border: Border.all(color: surfaces.borderHairline),
            ),
            child: Padding(
              padding: const EdgeInsets.all(AhSpace.lg),
              child: Row(
                children: <Widget>[
                  PlateBlock(
                    registration: quote.vehicle.registration.canonical,
                    display: quote.vehicle.registration.display,
                    plateClass: plateClassFromWire(quote.vehicle.registration.plateClass),
                    size: PlateSize.sm,
                  ),
                  const SizedBox(width: AhSpace.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: <Widget>[
                        Text(
                          quote.vehicle.title,
                          style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        // A number without its date is a rumour, and a week-old valuation is a
                        // different number from today's.
                        Text(
                          quote.asOf,
                          style: AhText.data.copyWith(
                            fontSize: 12,
                            color: surfaces.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                  if (headline != null)
                    MoneyValue(
                      minorUnits: headline.amountMinor,
                      compact: true,
                      currency: quote.currency,
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/* ── Listings ──────────────────────────────────────────────────────────────── */

class _Listings extends ConsumerWidget {
  const _Listings();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final AsyncValue<CursorPage<SearchItem>> listings = ref.watch(myListingsProvider);
    final List<SearchItem> items = listings.valueOrNull?.items ?? const <SearchItem>[];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(title: t(CustomerText.garageListings), size: SectionHeadingSize.sm),
        const SizedBox(height: AhSpace.lg),
        if (listings.isLoading && items.isEmpty)
          const AhSkeleton.block(height: 200)
        else if (items.isEmpty)
          Text(
            t(CustomerText.garageListingsEmpty),
            style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
          )
        else
          for (final SearchItem item in items)
            Padding(
              padding: const EdgeInsets.only(bottom: AhSpace.lg),
              child: ListingCard(item: item),
            ),
      ],
    );
  }
}

/* ── Bookings ──────────────────────────────────────────────────────────────── */

class _Bookings extends ConsumerWidget {
  const _Bookings();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final AsyncValue<CursorPage<GarageBooking>> bookings = ref.watch(myBookingsProvider);
    final List<GarageBooking> items = bookings.valueOrNull?.items ?? const <GarageBooking>[];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(title: t(CustomerText.garageBookings), size: SectionHeadingSize.sm),
        const SizedBox(height: AhSpace.lg),
        if (bookings.isLoading && items.isEmpty)
          const AhSkeleton.block(height: 120)
        else if (items.isEmpty)
          Text(
            t(CustomerText.garageBookingsEmpty),
            style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
          )
        else
          for (final GarageBooking booking in items)
            Padding(
              padding: const EdgeInsets.only(bottom: AhSpace.md),
              child: _BookingRow(booking: booking),
            ),
      ],
    );
  }
}

class _BookingRow extends StatelessWidget {
  const _BookingRow({required this.booking});

  final GarageBooking booking;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AhSpace.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Row(
              children: <Widget>[
                Expanded(
                  child: Text(
                    booking.listing.title,
                    style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                // Tone from the registry's category. This screen never decides that "awaiting
                // documents" is amber.
                StatusPill(
                  category: booking.state.category,
                  label: booking.state.name,
                  size: StatusPillSize.sm,
                ),
              ],
            ),
            const SizedBox(height: AhSpace.sm),
            Row(
              children: <Widget>[
                Expanded(
                  child: Text(
                    // The reference a customer reads out to support. Mono, always.
                    booking.reference,
                    style: AhText.data.copyWith(fontSize: 12, color: surfaces.textSecondary),
                  ),
                ),
                if (booking.amountMinor != null)
                  MoneyValue(
                    minorUnits: booking.amountMinor!,
                    compact: true,
                    size: MoneySize.sm,
                    currency: booking.currency,
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
