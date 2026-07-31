import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../app/partner_text.dart';
import '../data/attachment_models.dart';
import '../data/partner_providers.dart';
import '../widgets/failure_view.dart';
import '../widgets/partner_page.dart';

/// The demand board: what this operator needs, and how much of it is filled.
///
/// Structurally the inverse of every other listing screen in AutoHub. A requirement is not one
/// vehicle — it is "forty sedans in Whitefield from 1 September, twelve months, ₹1,850 a duty" —
/// so the row's headline number is a *count*, and it moves as contracts activate rather than when
/// somebody buys something. One requirement becomes forty contracts with forty different owners,
/// which is why nothing here is a vehicle card.
class RequirementsScreen extends ConsumerWidget {
  const RequirementsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerText text = ref.watch(partnerTextProvider);

    return PartnerBoard(
      eyebrow: text(PartnerKeys.sideEyebrow),
      title: text(PartnerKeys.requirementsTitle),
      action: AhButton(
        label: text(PartnerKeys.requirementsAdd),
        size: AhButtonSize.sm,
        onPressed: () => context.go('/requirements/new'),
      ),
      onRefresh: () async => ref.invalidate(demandPostsProvider),
      children: <Widget>[
        AsyncSection<CursorPage<DemandPost>>(
          value: ref.watch(demandPostsProvider),
          onRetry: () => ref.invalidate(demandPostsProvider),
          builder: (BuildContext context, CursorPage<DemandPost> page) {
            if (page.isEmpty) {
              return EmptyState(
                title: text(PartnerKeys.requirementsEmptyTitle),
                body: text(PartnerKeys.requirementsEmptyBody),
                action: AhButton(
                  label: text(PartnerKeys.requirementsAdd),
                  variant: AhButtonVariant.action,
                  onPressed: () => context.go('/requirements/new'),
                ),
              );
            }

            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                for (final DemandPost post in page.items) ...<Widget>[
                  _RequirementRow(post: post, text: text),
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

class _RequirementRow extends StatelessWidget {
  const _RequirementRow({required this.post, required this.text});

  final DemandPost post;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final int? target = post.quantityRequired;
    final int filled = post.quantityFulfilled ?? 0;

    return PartnerCard(
      onTap: () => context.go('/matches?postId=${Uri.encodeQueryComponent(post.id)}'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Expanded(
                child: Text(
                  post.title,
                  style: AhText.body.copyWith(color: surfaces.textPrimary),
                ),
              ),
              const SizedBox(width: AhSpace.sm),
              StatusPill(
                category: post.state.category,
                label: post.state.name,
                size: StatusPillSize.sm,
              ),
            ],
          ),
          const SizedBox(height: AhSpace.md),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              if (target != null)
                Expanded(
                  child: FieldBlock(
                    label: text(PartnerKeys.requirementsFilled),
                    // Two integers and a separator. No sentence to translate, and the shape is the
                    // one a fleet manager already reads on a roster.
                    value: '$filled / $target',
                    mono: true,
                    hint: post.city,
                  ),
                ),
              if (post.rateMinor != null)
                Expanded(
                  child: FieldBlock(
                    label: post.rateLabel ?? text(PartnerKeys.contractsRate),
                    valueWidget: MoneyValue(minorUnits: post.rateMinor!, size: MoneySize.sm),
                    hint: post.tenureLabel,
                  ),
                ),
              if (post.matchCount != null)
                Expanded(
                  child: FieldBlock(
                    label: text(PartnerKeys.matchesTitle),
                    value: '${post.matchCount}',
                    mono: true,
                    align: CrossAxisAlignment.end,
                  ),
                ),
            ],
          ),
          if (post.startsOn != null) ...<Widget>[
            const SizedBox(height: AhSpace.sm),
            FieldBlock(
              label: text(PartnerKeys.contractsStarts),
              value: post.startsOn,
              mono: true,
            ),
          ],
        ],
      ),
    );
  }
}
