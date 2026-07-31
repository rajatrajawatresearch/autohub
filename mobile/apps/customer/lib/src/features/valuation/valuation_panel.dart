import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/app_providers.dart';
import '../../i18n/customer_text.dart';

/// The answer, wherever it is being shown.
///
/// One widget for the home hero's in-place reveal and for `/valuation/{id}` reached from a saved
/// quote, because they are the same thing: a number, its range, its confidence and its date. Two
/// implementations would be two answers to the same question, and the one nobody is looking at
/// would be the one that drifts.
///
/// [OdometerValue] appears exactly once here, on the primary figure, and that is the only motion in
/// the product. Everything else on this panel is instant — no reveal, no stagger, no fade. The
/// restraint is what makes the one moment land, and a second animation anywhere takes it away.
class ValuationPanel extends ConsumerWidget {
  const ValuationPanel({
    required this.quote,
    this.onReset,
    this.onSell,
    this.onBrowse,
    super.key,
  });

  final ValuationQuote quote;

  /// "Check another vehicle". Null on `/valuation/{id}`, where the back arrow already does it.
  final VoidCallback? onReset;

  final VoidCallback? onSell;
  final VoidCallback? onBrowse;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);

    final ValuationBand? headline = quote.primary;
    final List<ValuationBand> rest = <ValuationBand>[
      for (final ValuationBand band in quote.valuations)
        if (band.code != headline?.code) band,
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        // A `Wrap`, not a `Row`. At 360dp a full plate and a text control do not share a line, and
        // a `Row` there is an overflow stripe across the one screen the product is judged on.
        Wrap(
          alignment: WrapAlignment.spaceBetween,
          crossAxisAlignment: WrapCrossAlignment.center,
          spacing: AhSpace.md,
          runSpacing: AhSpace.sm,
          children: <Widget>[
            PlateBlock(
              registration: quote.vehicle.registration.canonical,
              display: quote.vehicle.registration.display,
              plateClass: plateClassFromWire(quote.vehicle.registration.plateClass),
            ),
            if (onReset != null)
              AhButton(
                label: t(CustomerText.heroReset),
                variant: AhButtonVariant.ghost,
                size: AhButtonSize.sm,
                onPressed: onReset,
              ),
          ],
        ),
        const SizedBox(height: AhSpace.lg),
        Text(
          quote.vehicle.title,
          style: AhText.displayLg.copyWith(color: surfaces.textPrimary),
        ),

        const SizedBox(height: AhSpace.xl),

        // The figure, between two hairlines. Structure comes from rules, not from a card inside a
        // card.
        Container(
          decoration: BoxDecoration(
            border: Border.symmetric(
              horizontal: BorderSide(color: surfaces.borderHairline),
            ),
          ),
          padding: const EdgeInsets.symmetric(vertical: AhSpace.xl),
          width: double.infinity,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Text(
                (headline?.label ?? t(CustomerText.valuationHeadlineFallback)).toUpperCase(),
                style: AhText.label.copyWith(color: surfaces.textSecondary),
              ),
              const SizedBox(height: AhSpace.sm),
              OdometerValue(
                minorUnits: headline?.amountMinor ?? 0,
                compact: true,
                size: OdometerSize.xl,
              ),
              const SizedBox(height: AhSpace.md),
              Wrap(
                crossAxisAlignment: WrapCrossAlignment.center,
                spacing: AhSpace.xs,
                children: <Widget>[
                  Text(
                    t(CustomerText.valuationRange).toUpperCase(),
                    style: AhText.label.copyWith(color: surfaces.textSecondary),
                  ),
                  MoneyValue(
                    minorUnits: quote.range.lowMinor,
                    compact: true,
                    size: MoneySize.sm,
                    tone: MoneyTone.secondary,
                  ),
                  Text('–', style: AhText.data.copyWith(color: surfaces.textSecondary)),
                  MoneyValue(
                    minorUnits: quote.range.highMinor,
                    compact: true,
                    size: MoneySize.sm,
                    tone: MoneyTone.secondary,
                  ),
                ],
              ),
            ],
          ),
        ),

        if (rest.isNotEmpty) ...<Widget>[
          const SizedBox(height: AhSpace.xl),
          // The other bands. Two columns at 360dp, because four mono figures on one line is four
          // truncated figures.
          Wrap(
            spacing: AhSpace.xl,
            runSpacing: AhSpace.lg,
            children: <Widget>[
              for (final ValuationBand band in rest)
                SizedBox(
                  width: 140,
                  child: FieldBlock(
                    label: band.label,
                    hint: band.helpText,
                    valueWidget: MoneyValue(
                      minorUnits: band.amountMinor,
                      compact: true,
                    ),
                  ),
                ),
            ],
          ),
        ],

        const SizedBox(height: AhSpace.xl),
        DecoratedBox(
          decoration: BoxDecoration(
            border: Border(top: BorderSide(color: surfaces.borderHairline)),
          ),
          child: Padding(
            padding: const EdgeInsets.only(top: AhSpace.lg),
            child: Wrap(
              spacing: AhSpace.xxl,
              runSpacing: AhSpace.lg,
              children: <Widget>[
                FieldBlock(
                  label: t(CustomerText.valuationConfidence),
                  // The band is a contract enum and the percentage is the model's own number. A
                  // figure without its confidence is a rumour with a rupee sign on it.
                  value: '${(quote.confidence.score * 100).round()}% · '
                      '${quote.confidence.band.name}',
                  hint: quote.confidence.basedOn,
                  mono: true,
                ),
                FieldBlock(
                  label: t(CustomerText.valuationAsOf),
                  value: quote.asOf,
                  hint: quote.modelVersion == null
                      ? null
                      : t(CustomerText.valuationModel, <String, Object?>{
                          'version': quote.modelVersion,
                        }),
                  mono: true,
                ),
                if (quote.vehicle.source != null)
                  FieldBlock(
                    label: t(CustomerText.valuationSource),
                    // `vahan` when the RTO record answered, `manual` when the user typed it in. A
                    // number derived from a guess has to say so.
                    value: quote.vehicle.source!.toUpperCase(),
                    mono: true,
                  ),
              ],
            ),
          ),
        ),

        if (onSell != null || onBrowse != null) ...<Widget>[
          const SizedBox(height: AhSpace.xl),
          Row(
            children: <Widget>[
              if (onSell != null)
                Expanded(
                  // The screen's one rationed yellow. The valuation is the moment the product earns
                  // a listing, and this is the act that takes it.
                  child: AhButton(
                    label: t(CustomerText.valuationSellCta),
                    variant: AhButtonVariant.action,
                    size: AhButtonSize.lg,
                    block: true,
                    onPressed: onSell,
                  ),
                ),
              if (onSell != null && onBrowse != null) const SizedBox(width: AhSpace.md),
              if (onBrowse != null)
                Expanded(
                  child: AhButton(
                    label: t(CustomerText.valuationBrowseCta),
                    variant: AhButtonVariant.outline,
                    size: AhButtonSize.lg,
                    block: true,
                    onPressed: onBrowse,
                  ),
                ),
            ],
          ),
        ],

        const SizedBox(height: AhSpace.lg),
        Text(
          t(CustomerText.valuationDisclaimer),
          style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
        ),
      ],
    );
  }
}
