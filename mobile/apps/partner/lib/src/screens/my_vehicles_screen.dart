import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../app/partner_text.dart';
import '../data/attachment_models.dart';
import '../data/partner_providers.dart';
import '../widgets/compliance_view.dart';
import '../widgets/failure_view.dart';
import '../widgets/partner_page.dart';

/// The supply side's home, and the screen that protects the partner's income.
///
/// A vehicle list on this app is not an inventory list. What an owner comes here to find out is not
/// "what do I have" — they know, it is parked outside — but "is anything about to stop paying me".
/// An expired permit, fitness certificate or driver document lapses the assignment, suspends the
/// contract after a grace period, and blocks the **payout** while earnings continue to accrue. That
/// is a proportionate lever on the server's side and a very unpleasant surprise on the owner's, so
/// the countdown is the loudest thing on each row after the plate.
class MyVehiclesScreen extends ConsumerWidget {
  const MyVehiclesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerText text = ref.watch(partnerTextProvider);

    return PartnerBoard(
      eyebrow: text(PartnerKeys.sideEyebrow),
      title: text(PartnerKeys.vehiclesTitle),
      action: AhButton(
        label: text(PartnerKeys.vehiclesAdd),
        size: AhButtonSize.sm,
        onPressed: () => context.go('/vehicles/new'),
      ),
      onRefresh: () async => ref.invalidate(supplyPostsProvider),
      children: <Widget>[
        AsyncSection<CursorPage<SupplyPost>>(
          value: ref.watch(supplyPostsProvider),
          onRetry: () => ref.invalidate(supplyPostsProvider),
          builder: (BuildContext context, CursorPage<SupplyPost> page) {
            if (page.isEmpty) {
              return EmptyState(
                title: text(PartnerKeys.vehiclesEmptyTitle),
                body: text(PartnerKeys.vehiclesEmptyBody),
                action: AhButton(
                  label: text(PartnerKeys.vehiclesAdd),
                  variant: AhButtonVariant.action,
                  onPressed: () => context.go('/vehicles/new'),
                ),
              );
            }

            final DateTime now = ref.watch(serverClockProvider).now();

            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                for (final SupplyPost post in page.items) ...<Widget>[
                  _VehicleRow(post: post, now: now, text: text),
                  const SizedBox(height: AhSpace.md),
                ],
              ],
            );
          },
        ),
      ],
    );
  }
}

class _VehicleRow extends StatelessWidget {
  const _VehicleRow({required this.post, required this.now, required this.text});

  final SupplyPost post;
  final DateTime now;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return PartnerCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              // The plate first. On this app more than any other it is what the owner matches
              // against the vehicle in front of them, and they are frequently standing next to it.
              PlateBlock(
                registration: post.registration.canonical,
                display: post.registration.display,
                plateClass: plateClassFromWire(post.registration.plateClass),
                size: PlateSize.sm,
              ),
              const Spacer(),
              StatusPill(
                category: post.state.category,
                label: post.state.name,
                size: StatusPillSize.sm,
              ),
            ],
          ),
          const SizedBox(height: AhSpace.sm),
          Text(post.title, style: AhText.body.copyWith(color: surfaces.textPrimary)),
          if (post.specStrip.isNotEmpty) ...<Widget>[
            const SizedBox(height: AhSpace.sm),
            SpecStrip(items: post.specStrip, size: SpecStripSize.sm),
          ],
          if (post.rateMinor != null) ...<Widget>[
            const SizedBox(height: AhSpace.sm),
            FieldBlock(
              label: post.rateLabel ?? text(PartnerKeys.contractsRate),
              valueWidget: MoneyValue(minorUnits: post.rateMinor!, size: MoneySize.sm),
            ),
          ],
          if (post.compliance.isNotEmpty) ...<Widget>[
            const SizedBox(height: AhSpace.md),
            Divider(height: 1, thickness: 1, color: surfaces.borderHairline),
            const SizedBox(height: AhSpace.md),
            ComplianceSummary(documents: post.compliance, now: now, text: text),
          ],
        ],
      ),
    );
  }
}
