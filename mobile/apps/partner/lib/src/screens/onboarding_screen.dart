import 'dart:async';

import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/partner_side.dart';
import '../app/partner_text.dart';
import '../data/partner_providers.dart';

/// The first screen, and the only one that exists because this module is a market rather than a
/// catalogue.
///
/// Every other AutoHub app opens on inventory. This one opens on a question, because attachment is
/// genuinely two-sided: an owner with one Dzire and a fleet operator who needs forty sedans in
/// Whitefield are not two views of a list, they are two products that meet in the middle. Guessing
/// which one somebody is — from their profile, from their city, from whether they have a vehicle
/// on file — gets it wrong for exactly the users who matter most, the ones who are both.
///
/// The answer is remembered and switchable in settings, which is the whole ceremony. One tap, no
/// account type, no irreversible decision.
class OnboardingScreen extends ConsumerWidget {
  const OnboardingScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerText text = ref.watch(partnerTextProvider);

    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AhSpace.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              const SizedBox(height: AhSpace.xl),
              SectionHeading(
                eyebrow: text(PartnerKeys.sideEyebrow),
                title: text(PartnerKeys.sideTitle),
                description: text(PartnerKeys.sideBody),
                size: SectionHeadingSize.lg,
              ),
              const SizedBox(height: AhSpace.xl),
              _SideCard(
                side: PartnerSide.supply,
                title: text(PartnerKeys.sideSupplyTitle),
                body: text(PartnerKeys.sideSupplyBody),
                cta: text(PartnerKeys.sideSupplyCta),
                // The rationed yellow, spent once on this screen. Supply is the side this
                // marketplace is short of — a fleet operator with a requirement and no vehicles to
                // match is a dead board — so it is the consequential action in view.
                emphasis: AhButtonVariant.action,
              ),
              const SizedBox(height: AhSpace.md),
              _SideCard(
                side: PartnerSide.demand,
                title: text(PartnerKeys.sideDemandTitle),
                body: text(PartnerKeys.sideDemandBody),
                cta: text(PartnerKeys.sideDemandCta),
                emphasis: AhButtonVariant.outline,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _SideCard extends ConsumerWidget {
  const _SideCard({
    required this.side,
    required this.title,
    required this.body,
    required this.cta,
    required this.emphasis,
  });

  final PartnerSide side;
  final String title;
  final String body;
  final String cta;
  final AhButtonVariant emphasis;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
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
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            Text(title, style: AhText.heading.copyWith(color: surfaces.textPrimary)),
            const SizedBox(height: AhSpace.sm),
            Text(body, style: AhText.body.copyWith(color: surfaces.textSecondary)),
            const SizedBox(height: AhSpace.lg),
            AhButton(
              label: cta,
              variant: emphasis,
              block: true,
              // `unawaited` rather than a bare call: the choice repaints from memory on this frame
              // and the disk write follows, and `discarded_futures` being an error here is what
              // makes that a decision somebody wrote down rather than a dropped future.
              onPressed: () =>
                  unawaited(ref.read(partnerSideProvider.notifier).choose(side)),
            ),
          ],
        ),
      ),
    );
  }
}
