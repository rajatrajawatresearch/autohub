import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/router.dart';
import '../../data/app_providers.dart';
import '../../data/search.dart';
import '../../i18n/customer_text.dart';
import '../../widgets/listing_card.dart';
import 'valuation_hero.dart';

/// The money shot.
///
/// Four things, in this order, and nothing else:
///
/// 1. the hero — an empty plate under "What is your car worth today?"
/// 2. the four intents, as flat hairline tiles
/// 3. a live stock strip, to show there is a market
/// 4. a trust row of three `FieldBlock`s
///
/// There is no testimonial carousel, no logo wall, no gradient statistics band and no app-download
/// interstitial. Every one of those competes with the plate input, which is the only thing on this
/// screen that makes the company money — and the screen is disciplined precisely so that the input
/// is unmissable on a 360dp handset held one-handed.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async {
            ref
              ..invalidate(liveStockProvider)
              ..invalidate(featuresProvider);
          },
          child: ListView(
            // Always scrollable, so pull-to-refresh works on a screen whose content happens to fit.
            physics: const AlwaysScrollableScrollPhysics(),
            padding: EdgeInsets.zero,
            children: const <Widget>[
              Padding(
                padding: EdgeInsets.fromLTRB(AhSpace.lg, AhSpace.xxl, AhSpace.lg, AhSpace.xxl),
                child: ValuationHero(),
              ),
              _IntentTiles(),
              _LiveStock(),
              _TrustRow(),
            ],
          ),
        ),
      ),
    );
  }
}

/* ── The four intents ──────────────────────────────────────────────────────── */

/// One tile: a route this build has a screen for, gated on the module registry.
@immutable
class _Intent {
  const _Intent({
    required this.titleKey,
    required this.lineKey,
    required this.location,
    this.module,
  });

  final String titleKey;
  final String lineKey;
  final String location;

  /// The module code in `GET /platform/features`. `null` means the tile is always shown — buying
  /// and selling are the product, not a feature.
  ///
  /// This is the whole reason there is no `const` list of four labels here: whether Lease is live
  /// in Coimbatore is an operations decision evaluated server-side per user, and a tile that opens
  /// a flow the city cannot serve is a support call.
  final String? module;
}

class _IntentTiles extends ConsumerWidget {
  const _IntentTiles();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);
    final Features? features = ref.watch(featuresProvider).valueOrNull;

    final List<_Intent> intents = <_Intent>[
      const _Intent(
        titleKey: CustomerText.intentBuyTitle,
        lineKey: CustomerText.intentBuyLine,
        location: Routes.buy,
      ),
      const _Intent(
        titleKey: CustomerText.intentSellTitle,
        lineKey: CustomerText.intentSellLine,
        location: Routes.sell,
      ),
      _Intent(
        titleKey: CustomerText.intentRentTitle,
        lineKey: CustomerText.intentRentLine,
        location: Routes.buyWithIntent(ListingIntent.rental),
        module: 'rental',
      ),
      _Intent(
        titleKey: CustomerText.intentAttachTitle,
        lineKey: CustomerText.intentAttachLine,
        location: Routes.buyWithIntent(ListingIntent.attachment),
        module: 'attachment',
      ),
    ];

    final List<_Intent> visible = <_Intent>[
      for (final _Intent intent in intents)
        // Fail closed while the registry loads. Showing a tile and then removing it under the
        // user's thumb is worse than showing it a frame late.
        if (intent.module == null || (features?.module(intent.module!) ?? false)) intent,
    ];

    return DecoratedBox(
      decoration: BoxDecoration(
        border: Border(
          top: BorderSide(color: surfaces.borderHairline),
          bottom: BorderSide(color: surfaces.borderHairline),
        ),
      ),
      child: Column(
        children: <Widget>[
          for (int index = 0; index < visible.length; index += 1)
            DecoratedBox(
              decoration: BoxDecoration(
                border: Border(
                  // Hairlines *between* tiles, not around them. Structure comes from rules.
                  top: index == 0
                      ? BorderSide.none
                      : BorderSide(color: surfaces.borderHairline),
                ),
              ),
              child: _IntentTile(
                title: t(visible[index].titleKey),
                line: t(visible[index].lineKey),
                ordinal: index + 1,
                location: visible[index].location,
              ),
            ),
        ],
      ),
    );
  }
}

class _IntentTile extends StatelessWidget {
  const _IntentTile({
    required this.title,
    required this.line,
    required this.ordinal,
    required this.location,
  });

  final String title;
  final String line;
  final int ordinal;
  final String location;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return Semantics(
      button: true,
      label: title,
      excludeSemantics: true,
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: () => context.go(location),
          splashFactory: NoSplash.splashFactory,
          hoverColor: Colors.transparent,
          focusColor: Colors.transparent,
          highlightColor: Colors.transparent,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: AhSpace.lg, vertical: AhSpace.xl),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(
                  // Zero-padded, like an index on a printed form. Mono, so the four line up.
                  '0$ordinal',
                  style: AhText.data.copyWith(fontSize: 12, color: surfaces.textSecondary),
                ),
                const SizedBox(width: AhSpace.lg),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: <Widget>[
                      Text(
                        title,
                        style: AhText.heading.copyWith(color: surfaces.textPrimary),
                      ),
                      const SizedBox(height: AhSpace.xs),
                      Text(
                        line,
                        style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                      ),
                    ],
                  ),
                ),
                Icon(Icons.arrow_forward, size: 18, color: surfaces.accentPrimary),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/* ── Live stock ────────────────────────────────────────────────────────────── */

/// Proof that there is a market, not a shopping surface.
///
/// A horizontal strip rather than a grid, deliberately: this is the third thing on the page and it
/// must not push the trust row two screens down. Anyone who wants to shop taps *See all*.
class _LiveStock extends ConsumerWidget {
  const _LiveStock();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final AsyncValue<List<SearchItem>> stock = ref.watch(liveStockProvider);
    final List<SearchItem> items = stock.valueOrNull ?? const <SearchItem>[];

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AhSpace.xxl),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: AhSpace.lg),
            child: SectionHeading(
              eyebrow: t(CustomerText.stockEyebrow),
              title: t(CustomerText.stockTitle),
              action: AhButton(
                label: t(CustomerText.stockAll),
                variant: AhButtonVariant.ghost,
                size: AhButtonSize.sm,
                onPressed: () => context.go(Routes.buy),
              ),
            ),
          ),
          const SizedBox(height: AhSpace.lg),
          if (stock.isLoading && items.isEmpty)
            const _StockSkeleton()
          else if (items.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AhSpace.lg),
              child: Text(
                // An instruction, not a mood.
                t(CustomerText.stockEmpty),
                style: AhText.bodySm.copyWith(color: context.ahSurfaces.textSecondary),
              ),
            )
          else
            SizedBox(
              height: 320,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: AhSpace.lg),
                itemCount: items.length,
                separatorBuilder: (BuildContext context, int index) =>
                    const SizedBox(width: AhSpace.md),
                itemBuilder: (BuildContext context, int index) => SizedBox(
                  width: 260,
                  child: ListingCard(item: items[index]),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _StockSkeleton extends StatelessWidget {
  const _StockSkeleton();

  @override
  Widget build(BuildContext context) {
    // Held slots at the real card's dimensions, so nothing shifts when the data lands. A skeleton
    // that is the wrong size promises a shape and then moves what the user was reaching for.
    return SizedBox(
      height: 320,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: AhSpace.lg),
        itemCount: 3,
        separatorBuilder: (BuildContext context, int index) => const SizedBox(width: AhSpace.md),
        itemBuilder: (BuildContext context, int index) =>
            const AhSkeleton.block(height: 320, width: 260),
      ),
    );
  }
}

/* ── Trust ─────────────────────────────────────────────────────────────────── */

/// Three facts, as RC-card fields. The same atom the rest of the product is made of.
class _TrustRow extends ConsumerWidget {
  const _TrustRow();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);

    return Container(
      width: double.infinity,
      color: surfaces.sunken,
      padding: const EdgeInsets.symmetric(horizontal: AhSpace.lg, vertical: AhSpace.xxl),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          FieldBlock(
            label: t(CustomerText.trustOwnershipLabel),
            value: t(CustomerText.trustOwnershipValue),
            hint: t(CustomerText.trustOwnershipHint),
          ),
          const SizedBox(height: AhSpace.xl),
          FieldBlock(
            label: t(CustomerText.trustConditionLabel),
            value: t(CustomerText.trustConditionValue),
            hint: t(CustomerText.trustConditionHint),
          ),
          const SizedBox(height: AhSpace.xl),
          FieldBlock(
            label: t(CustomerText.trustPaymentLabel),
            value: t(CustomerText.trustPaymentValue),
            hint: t(CustomerText.trustPaymentHint),
          ),
        ],
      ),
    );
  }
}
