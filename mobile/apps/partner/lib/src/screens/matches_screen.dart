import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/partner_failure.dart';
import '../app/partner_text.dart';
import '../data/attachment_models.dart';
import '../data/attachment_writes.dart';
import '../data/partner_providers.dart';
import '../data/wire.dart';
import '../widgets/commission_view.dart';
import '../widgets/failure_view.dart';
import '../widgets/partner_page.dart';

/// The match board — and the one screen in this app with a hard rule attached to it.
///
/// **The commission is stated before acceptance, never after.** `AttachmentMatch.canAccept` is
/// false while `commission` is null, and the accept control is disabled with the reason on screen.
/// That is not defensive coding; it is the product. This market runs today on WhatsApp groups and a
/// broker's ₹5,000 "line charge", and the thing AutoHub sells is that the cut is on the table
/// before you sign. A match screen that shows a rate and defers the platform's share to the first
/// monthly statement is that broker with a nicer typeface — and the owner discovers it a month
/// later, having already turned down two other operators.
///
/// The second thing this screen does that no competitor's does: it shows the matches the owner
/// **lost**, with the reason. Every candidate the hard filters rejected is recorded rather than
/// discarded, so an owner sees "31 of 40 requirements rejected you because your permit expires in
/// 22 days" instead of an empty list. A matching engine that silently returns nothing teaches
/// nobody anything, and a permit renewal is a far cheaper retention lever than a discount.
class MatchesScreen extends ConsumerWidget {
  const MatchesScreen({this.postId, super.key});

  /// Narrows the board to one of this partner's posts. Deep-linkable, because a notification about
  /// a new match must open that post's board rather than the home screen.
  final String? postId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerText text = ref.watch(partnerTextProvider);

    return PartnerBoard(
      eyebrow: text(PartnerKeys.sideEyebrow),
      title: text(PartnerKeys.matchesTitle),
      onRefresh: () async => ref.invalidate(matchesProvider(postId)),
      children: <Widget>[
        AsyncSection<CursorPage<AttachmentMatch>>(
          value: ref.watch(matchesProvider(postId)),
          onRetry: () => ref.invalidate(matchesProvider(postId)),
          builder: (BuildContext context, CursorPage<AttachmentMatch> page) {
            final List<AttachmentMatch> eligible =
                page.items.where((AttachmentMatch match) => match.isEligible).toList();
            final List<AttachmentMatch> rejected =
                page.items.where((AttachmentMatch match) => !match.isEligible).toList();

            if (page.isEmpty) {
              return EmptyState(
                title: text(PartnerKeys.matchesEmptyTitle),
                body: text(PartnerKeys.matchesEmptyBody),
              );
            }

            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                for (final AttachmentMatch match in eligible) ...<Widget>[
                  _MatchCard(match: match, postId: postId, text: text),
                  const SizedBox(height: AhSpace.md),
                ],
                if (rejected.isNotEmpty) ...<Widget>[
                  const SizedBox(height: AhSpace.lg),
                  SectionHeading(
                    eyebrow: text(PartnerKeys.matchesIneligibleTitle),
                    // The count is the headline, because the count is what makes the reason worth
                    // acting on. One rejection is bad luck; thirty-one is a permit.
                    title: '${rejected.length}',
                    description: text(PartnerKeys.matchesIneligibleBody),
                    size: SectionHeadingSize.sm,
                    rule: true,
                  ),
                  const SizedBox(height: AhSpace.md),
                  for (final AttachmentMatch match in rejected) ...<Widget>[
                    _RejectedRow(match: match),
                    const SizedBox(height: AhSpace.sm),
                  ],
                ],
              ],
            );
          },
        ),
      ],
    );
  }
}

class _MatchCard extends ConsumerStatefulWidget {
  const _MatchCard({required this.match, required this.postId, required this.text});

  final AttachmentMatch match;
  final String? postId;
  final PartnerText text;

  @override
  ConsumerState<_MatchCard> createState() => _MatchCardState();
}

class _MatchCardState extends ConsumerState<_MatchCard> {
  /// Set while the acceptance is in flight, so the control cannot be tapped twice. The
  /// `Idempotency-Key` would make a second tap harmless on the server; this makes it harmless on
  /// the screen, which is where the partner is watching.
  bool _accepting = false;

  /// The server's answer to the last attempt. Rendered in place rather than in a snackbar — a
  /// `409` here names the contract that won the vehicle, and that is something the partner needs to
  /// read rather than watch slide away.
  PartnerFailure? _failure;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AttachmentMatch match = widget.match;
    final PartnerText text = widget.text;
    final PricingQuote? commission = match.commission;

    return PartnerCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: <Widget>[
                    Text(
                      match.title,
                      style: AhText.body.copyWith(color: surfaces.textPrimary),
                    ),
                    if (match.counterpartyName != null)
                      Text(
                        match.counterpartyName!,
                        style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                      ),
                  ],
                ),
              ),
              const SizedBox(width: AhSpace.sm),
              StatusPill(
                category: match.state.category,
                label: match.state.name,
                size: StatusPillSize.sm,
              ),
            ],
          ),
          if (match.registration != null) ...<Widget>[
            const SizedBox(height: AhSpace.sm),
            Align(
              alignment: Alignment.centerLeft,
              child: PlateBlock(
                registration: match.registration!.canonical,
                display: match.registration!.display,
                plateClass: plateClassFromWire(match.registration!.plateClass),
                size: PlateSize.xs,
              ),
            ),
          ],
          if (match.specStrip.isNotEmpty) ...<Widget>[
            const SizedBox(height: AhSpace.sm),
            SpecStrip(items: match.specStrip, size: SpecStripSize.sm),
          ],
          const SizedBox(height: AhSpace.md),
          Row(
            children: <Widget>[
              if (match.rateMinor != null)
                Expanded(
                  child: FieldBlock(
                    label: match.rateLabel ?? text(PartnerKeys.contractsRate),
                    valueWidget: MoneyValue(minorUnits: match.rateMinor!, size: MoneySize.sm),
                  ),
                ),
              if (match.score != null)
                Expanded(
                  child: FieldBlock(
                    label: text(PartnerKeys.matchesScore),
                    // The score is the model's output, rendered to the precision the server sent
                    // it at. Nothing here recomputes it — the weights are registry rows, and a
                    // second opinion computed on a handset is a second model.
                    value: match.score!.toStringAsFixed(0),
                    mono: true,
                    align: CrossAxisAlignment.end,
                  ),
                ),
            ],
          ),
          if (match.factors.isNotEmpty) ...<Widget>[
            const SizedBox(height: AhSpace.md),
            Text(
              text(PartnerKeys.matchesWhy).toUpperCase(),
              style: AhText.label.copyWith(color: surfaces.textSecondary),
            ),
            const SizedBox(height: AhSpace.xs),
            for (final MatchFactor factor in match.factors)
              Padding(
                padding: const EdgeInsets.only(top: 2),
                child: Text(
                  factor.label,
                  style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                ),
              ),
          ],
          const SizedBox(height: AhSpace.md),
          if (commission != null)
            CommissionBlock(quote: commission, text: text, dense: true)
          else
            // No quote, no acceptance. Saying why is the difference between a control that looks
            // broken and one that is honestly waiting.
            Text(
              text(PartnerKeys.matchesCommissionMissing),
              style: AhText.bodySm.copyWith(color: AhColors.caution),
            ),
          if (_failure != null) ...<Widget>[
            const SizedBox(height: AhSpace.md),
            FailureBlock(failure: _failure!, text: text),
          ],
          const SizedBox(height: AhSpace.md),
          AhButton(
            label: text(PartnerKeys.matchesAccept),
            // The rationed yellow, spent on the single most consequential action in view. Accepting
            // a match commits a vehicle for months and starts a commission that recurs every cycle.
            variant: AhButtonVariant.action,
            block: true,
            onPressed:
                (match.canAccept && !_accepting) ? () => unawaited(_accept(match)) : null,
          ),
        ],
      ),
    );
  }

  /// Sends the acceptance now, and never queues it.
  ///
  /// See `acceptProposal` in `attachment_writes.dart` for the argument. In short: two operators can
  /// accept the same exclusive vehicle in the same second, the database settles it, and the loser
  /// has to be told while they are still looking at the board.
  Future<void> _accept(AttachmentMatch match) async {
    final String? proposalId = match.proposalId;
    if (proposalId == null) return;

    setState(() {
      _accepting = true;
      _failure = null;
    });

    final WriteIntent intent = acceptProposal(
      proposalId: proposalId,
      activeCity: ref.read(activeCityProvider).valueOrNull,
    );

    // `sendStrict` rather than `send`: it refuses an intent that was captured as queueable, which
    // makes "somebody changed this to WriteIntent.now() and it still compiled" a runtime failure on
    // the developer's first tap rather than a silently queued acceptance in production.
    final WriteOutcome outcome = await ref.read(immediateWriterProvider).sendStrict(intent);
    if (!mounted) return;

    final PartnerFailure? failure = PartnerFailure.fromOutcome(outcome);
    setState(() {
      _accepting = false;
      _failure = failure;
    });

    if (failure == null) {
      // The board is the truth about what is still available, and this acceptance changed it —
      // for this match and, through the commitment block, for every other match on the same
      // vehicle. Refetch rather than mutate a row in place.
      ref.invalidate(matchesProvider(widget.postId));
      ref.invalidate(contractsProvider);
    }
  }
}

/// A candidate the hard filters rejected, with the reason and its machine code.
class _RejectedRow extends StatelessWidget {
  const _RejectedRow({required this.match});

  final AttachmentMatch match;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Expanded(
          child: FieldBlock(
            label: match.postTitle ?? match.title,
            // The server's sentence — "Permit expires within 45 days". The threshold in it is a
            // registry value, which is exactly why the sentence is not composed here.
            value: match.denyReasonLabel,
            hint: match.denyReasonCode,
          ),
        ),
        const SizedBox(width: AhSpace.sm),
        Icon(Icons.block_outlined, size: 16, color: surfaces.textSecondary),
      ],
    );
  }
}
