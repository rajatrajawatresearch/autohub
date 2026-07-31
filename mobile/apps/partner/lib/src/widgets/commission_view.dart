import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

import '../app/partner_text.dart';

/// What AutoHub will take, stated before the partner accepts — never after.
///
/// This module's whole commercial claim is that it replaces a broker's ₹5,000 "line charge" with a
/// number you can see. A match screen that shows a rate and defers the cut to the first monthly
/// statement *is* that broker, with a nicer typeface. So the commission is rendered as a
/// `PricingQuote` — every line named, every rule named, the total spelled in full — and the accept
/// control is unavailable until there is one to render. See `AttachmentMatch.canAccept`.
///
/// Nothing here computes a fee. The percentages, the slabs, the GST on the commission and the
/// regulatory cap are all `fee_components` rows resolved server-side; this widget adds up nothing
/// and rounds nothing. `totalFeesMinor` is the server's total and is displayed as sent, because a
/// client that re-derived it would eventually disagree by a paisa and the partner would be left
/// deciding which of two figures to believe.
class CommissionBlock extends StatelessWidget {
  const CommissionBlock({
    required this.quote,
    required this.text,
    this.dense = false,
    super.key,
  });

  /// `null` means the server has not quoted this match. Callers render
  /// [PartnerKeys.matchesCommissionMissing] instead of guessing, and disable acceptance.
  final PricingQuote quote;

  final PartnerText text;

  /// Set inside a list row, where the section heading is redundant.
  final bool dense;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.sunken,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: Padding(
        padding: EdgeInsets.all(dense ? AhSpace.md : AhSpace.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            if (!dense) ...<Widget>[
              SectionHeading(
                eyebrow: text(PartnerKeys.matchesCommissionTitle),
                title: formatIndianMoney(quote.totalFeesMinor, currency: quote.currency),
                description: text(PartnerKeys.matchesCommissionBody),
                size: SectionHeadingSize.sm,
              ),
              const SizedBox(height: AhSpace.md),
            ],
            for (final PricingLine line in quote.lines) ...<Widget>[
              _LineRow(line: line, currency: quote.currency),
              const SizedBox(height: AhSpace.sm),
            ],
            Divider(height: AhSpace.md, thickness: 1, color: surfaces.borderHairline),
            Row(
              children: <Widget>[
                Expanded(
                  child: Text(
                    text(PartnerKeys.matchesCommissionTitle).toUpperCase(),
                    style: AhText.label.copyWith(color: surfaces.textSecondary),
                  ),
                ),
                // Not `compact`. `₹3.2K` is not a figure anyone can be held to, and this one is
                // the number the partner is agreeing to pay every cycle for eighteen months.
                MoneyValue(
                  minorUnits: quote.totalFeesMinor,
                  currency: quote.currency,
                  size: MoneySize.md,
                ),
              ],
            ),
            if (quote.explanation != null) ...<Widget>[
              const SizedBox(height: AhSpace.sm),
              // The server's own sentence — "Gold dealers in Maharashtra pay 2% on auction wins".
              // A partner will ask why, and "the code says so" is not an answer support can give.
              Text(
                quote.explanation!,
                style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _LineRow extends StatelessWidget {
  const _LineRow({required this.line, required this.currency});

  final PricingLine line;
  final String currency;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              // The label already carries the rate where there is one — the contract's own example
              // is `"GST 18%"`. Rendering `line.rate` separately would mean this client deciding
              // how a percentage is spelled, and disagreeing with the invoice the moment a slab
              // makes the effective rate something other than the headline one.
              Text(line.label, style: AhText.body.copyWith(color: surfaces.textPrimary)),
              if (line.ruleName != null)
                Text(
                  line.ruleName!,
                  style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                ),
            ],
          ),
        ),
        const SizedBox(width: AhSpace.md),
        MoneyValue(
          minorUnits: line.amountMinor,
          currency: currency,
          size: MoneySize.sm,
          tone: MoneyTone.secondary,
        ),
      ],
    );
  }
}
