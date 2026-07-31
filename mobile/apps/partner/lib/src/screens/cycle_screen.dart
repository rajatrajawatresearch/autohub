import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/partner_failure.dart';
import '../app/partner_text.dart';
import '../data/attachment_models.dart';
import '../data/partner_providers.dart';
import '../widgets/failure_view.dart';
import '../widgets/partner_page.dart';

/// One settlement statement: the gross, every deduction in the order the contract pinned, the net,
/// the payout date and the invoice.
///
/// Three decisions in this file are about money, and each of them is a decision *not* to be clever.
///
/// **Nothing is computed.** The commission, its GST, the TDS, the SLA penalty and the recoveries
/// are `fee_components` rows resolved server-side against a snapshot pinned at signing. This screen
/// renders `netPayableMinor` as sent. A client that re-derived the total would agree with the
/// server until the first slab, the first rounding rule or the first registry edit, and then the
/// partner would be holding two figures and no way to choose.
///
/// **Nothing is rounded and no sign is flipped.** Integer paise throughout, and a deduction arrives
/// negative because the server said so. A client that negated what it was handed could turn a
/// credit note into a debit, and the owner would find out from their bank statement a month later.
///
/// **A statement that does not add up says so.** The server's pricing pipeline rounds each
/// component and then sums the rounded values *specifically* so the invoice foots. If the lines
/// here do not reconcile to the payable figure, something is missing or a rounding rule disagrees,
/// and both are conditions the partner must not be shown a confident number for. So the
/// disagreement is rendered, with the support reference, instead of quietly trusting one of the two
/// figures — because a settlement UI that displays lines contradicting its own total is how a
/// disputed ₹1,500 penalty becomes an argument nobody can reconstruct.
class CycleScreen extends ConsumerWidget {
  const CycleScreen({required this.cycleId, super.key});

  final String cycleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerText text = ref.watch(partnerTextProvider);

    // Chrome outside the `AsyncSection`, as on the contract page: a statement opened from a payout
    // notification must have a back affordance even while it is still arriving.
    return PartnerDetailPage(
      title: text(PartnerKeys.settlementTitle),
      children: <Widget>[
        AsyncSection<SettlementCycle>(
          value: ref.watch(cycleProvider(cycleId)),
          onRetry: () => ref.invalidate(cycleProvider(cycleId)),
          builder: (BuildContext context, SettlementCycle cycle) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              _Header(cycle: cycle, text: text),
              const SizedBox(height: AhSpace.xl),
              // Before anything else on the page. A partner must not read a payout figure and only
              // then be told the statement it came from does not add up.
              if (!cycle.foots) ...<Widget>[
                UnreconciledStatement(cycle: cycle, text: text),
                const SizedBox(height: AhSpace.xl),
              ],
              _Statement(cycle: cycle, text: text),
              const SizedBox(height: AhSpace.xl),
              _Payout(cycle: cycle, text: text),
            ],
          ),
        ),
      ],
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.cycle, required this.text});

  final SettlementCycle cycle;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    return PartnerCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(
                child: FieldBlock(
                  label: text(PartnerKeys.settlementPeriod),
                  // Both ends of the period, verbatim and in mono. A cycle is a date range and
                  // "November" is not one — the owner is checking it against duty slips.
                  value: <String?>[cycle.periodStart, cycle.periodEnd]
                      .whereType<String>()
                      .join(' → '),
                  mono: true,
                ),
              ),
              StatusPill(category: cycle.state.category, label: cycle.state.name),
            ],
          ),
          const SizedBox(height: AhSpace.md),
          Row(
            children: <Widget>[
              Expanded(
                child: FieldBlock(
                  label: text(PartnerKeys.settlementGross),
                  valueWidget: MoneyValue(
                    minorUnits: cycle.agreedGrossMinor,
                    currency: cycle.currency,
                  ),
                ),
              ),
              Expanded(
                child: FieldBlock(
                  label: text(PartnerKeys.settlementNet),
                  valueWidget: MoneyValue(
                    minorUnits: cycle.netPayableMinor,
                    currency: cycle.currency,
                    size: MoneySize.lg,
                    // Positive is not a decoration here. This is the figure the owner is waiting
                    // for, and it is the one number on the screen that is allowed to be loud.
                    tone: MoneyTone.positive,
                  ),
                  align: CrossAxisAlignment.end,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Statement extends StatelessWidget {
  const _Statement({required this.cycle, required this.text});

  final SettlementCycle cycle;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    if (cycle.lines.isEmpty) {
      return EmptyState(
        title: text(PartnerKeys.settlementLines),
        body: text(PartnerKeys.settlementEmptyBody),
        align: CrossAxisAlignment.start,
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(
          eyebrow: text(PartnerKeys.settlementLines),
          title: text(PartnerKeys.settlementTitle),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        for (final SettlementLine line in cycle.lines) ...<Widget>[
          Padding(
            padding: const EdgeInsets.symmetric(vertical: AhSpace.sm),
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
                        style: AhText.body.copyWith(color: surfaces.textPrimary),
                      ),
                      if (line.ruleName != null)
                        Text(
                          line.ruleName!,
                          style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                        ),
                      // The `fee_components` code, quiet and in mono. An owner disputing ₹1,500
                      // reads this to support; support reads the same row in the registry.
                      Text(
                        line.code,
                        style: AhText.label.copyWith(color: surfaces.textSecondary),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: AhSpace.md),
                MoneyValue(
                  minorUnits: line.amountMinor,
                  currency: cycle.currency,
                  size: MoneySize.sm,
                  // The sign the server sent decides the tone; this widget does not interpret a
                  // line's meaning from its code.
                  tone: line.amountMinor < 0 ? MoneyTone.negative : MoneyTone.normal,
                ),
              ],
            ),
          ),
          Divider(height: 1, thickness: 1, color: surfaces.borderHairline),
        ],
        const SizedBox(height: AhSpace.md),
        Row(
          children: <Widget>[
            Expanded(
              child: Text(
                text(PartnerKeys.settlementNet).toUpperCase(),
                style: AhText.label.copyWith(color: surfaces.textSecondary),
              ),
            ),
            MoneyValue(
              minorUnits: cycle.netPayableMinor,
              currency: cycle.currency,
              size: MoneySize.md,
            ),
          ],
        ),
      ],
    );
  }
}

/// Shown when the lines do not reconcile to the payable figure.
///
/// Public so a widget test can find it: this is the guard that decides whether a partner is shown a
/// number they can act on, and "being wrong costs money" is the whole reason it exists.
class UnreconciledStatement extends StatelessWidget {
  const UnreconciledStatement({required this.cycle, required this.text, super.key});

  final SettlementCycle cycle;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    return FailureBlock(
      failure: PartnerFailure(
        message: text(PartnerKeys.settlementUnreconciled),
        // A client-side code, and named as one. It says which side noticed, which is the first
        // thing support needs to know before they go looking at the deal lines.
        code: 'partner.settlement.statement_unreconciled',
        traceId: cycle.publicRef,
      ),
      text: text,
    );
  }
}

class _Payout extends StatelessWidget {
  const _Payout({required this.cycle, required this.text});

  final SettlementCycle cycle;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return PartnerCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          FieldBlock(
            label: text(PartnerKeys.settlementPayoutOn),
            // The date the **server** computed. The product says T+2 and that is
            // `attach.payout_delay_days`, a registry value tuned per market — a client that added
            // two days to the cycle end would print a date the treasury has never heard of the
            // first time somebody changes it, and an owner who plans a fuel purchase around a wrong
            // date stops trusting the app.
            value: cycle.payoutOn,
            mono: true,
            hint: cycle.payoutOn == null
                ? text(PartnerKeys.settlementPayoutPending)
                : cycle.payoutNote,
          ),
          if (cycle.invoiceUrl != null) ...<Widget>[
            const SizedBox(height: AhSpace.md),
            Divider(height: 1, thickness: 1, color: surfaces.borderHairline),
            const SizedBox(height: AhSpace.md),
            FieldBlock(
              label: text(PartnerKeys.settlementInvoice),
              value: cycle.invoiceLabel ?? cycle.invoiceUrl,
              mono: true,
            ),
          ],
        ],
      ),
    );
  }
}
