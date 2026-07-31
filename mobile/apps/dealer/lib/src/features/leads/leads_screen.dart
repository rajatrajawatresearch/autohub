import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/providers.dart';
import '../../app/strings.dart';
import '../../data/models/cursor_page.dart';
import '../../data/models/deal.dart';
import '../../widgets/async_section.dart';
import '../../widgets/console.dart';

final AutoDisposeFutureProvider<CursorPage<DealSummary>> leadsProvider =
    FutureProvider.autoDispose<CursorPage<DealSummary>>(
  (ref) => ref.watch(dealerApiProvider).deals(const DealListParams(limit: 60)),
);

/// The pipeline, with its stages taken from the deal workflow registry.
///
/// **The stages are not a list in this file.** They are the non-terminal states of
/// `GET /platform/workflows/deal`, in the order the registry declares them. Inserting "Awaiting
/// finance approval" between two stages is a row an operations user adds on a Tuesday, and it
/// appears here on handsets that shipped in March — correctly toned, because the chip derives its
/// colour from the state's `category` rather than from its name.
///
/// That is the difference between a CRM a dealer can shape to how they actually sell and one they
/// have to work around. A hardcoded `['new', 'contacted', 'negotiating', 'won']` would be the same
/// bug this whole architecture exists to prevent, wearing a kanban board.
///
/// Terminal states are excluded on purpose: a pipeline column of things that have finished is a
/// column nobody works, and `isTerminal` is the registry's own answer to which those are.
class LeadsScreen extends ConsumerWidget {
  const LeadsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = ref.watch(stringsProvider);
    final workflow = ref.watch(workflowDefinitionProvider('deal'));
    final deals = ref.watch(leadsProvider);

    return RefreshIndicator(
      onRefresh: () async {
        ref
          ..invalidate(leadsProvider)
          ..invalidate(workflowDefinitionProvider('deal'));
        try {
          await ref.read(leadsProvider.future);
        } on Object {
          // The board keeps whatever it has and shows a banner. Losing a pipeline because a refresh
          // failed is worse than showing one that is a minute old.
        }
      },
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: consolePagePadding(context),
        children: <Widget>[
          ConsolePageHeader(
            eyebrow: t(DealerText.leadsEyebrow),
            title: t(DealerText.leadsTitle),
            description: t(DealerText.leadsDescription),
          ),
          const SizedBox(height: kConsoleBlockGap),
          AsyncSection<WorkflowDefinition>(
            value: workflow,
            onRetry: () => ref.invalidate(workflowDefinitionProvider('deal')),
            builder: (context, definition) {
              final stages =
                  definition.states.where((state) => !state.isTerminal).toList();

              if (stages.isEmpty) {
                return EmptyState(
                  title: t(DealerText.noPipelineTitle),
                  body: t(DealerText.noPipelineBody),
                );
              }

              return AsyncSection<CursorPage<DealSummary>>(
                value: deals,
                onRetry: () => ref.invalidate(leadsProvider),
                builder: (context, page) => Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  mainAxisSize: MainAxisSize.min,
                  children: <Widget>[
                    for (final stage in stages)
                      Padding(
                        padding: const EdgeInsets.only(bottom: kConsoleBlockGap),
                        child: _Stage(
                          stage: stage,
                          // Matched on `code`, which is the registry's identity for a state. Never
                          // on `name` — that is display text a content person can reword, and the
                          // board would silently empty the day they did.
                          deals: page.items
                              .where((deal) => deal.state.code == stage.code)
                              .toList(),
                          strings: t,
                        ),
                      ),
                  ],
                ),
              );
            },
          ),
        ],
      ),
    );
  }
}

/// One stage, as a section rather than a column.
///
/// A horizontal kanban board is a desktop idiom. On a 360dp handset it means side-scrolling to
/// find a stage and then vertical-scrolling inside it, with the header gone — so the stages stack
/// and each carries its own count. The information is identical; the gesture is one a thumb can do.
class _Stage extends StatelessWidget {
  const _Stage({required this.stage, required this.deals, required this.strings});

  final WorkflowState stage;
  final List<DealSummary> deals;
  final DealerStrings strings;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        DecoratedBox(
          decoration: BoxDecoration(
            border: Border(bottom: BorderSide(color: surfaces.borderHairline)),
          ),
          child: Padding(
            padding: const EdgeInsets.only(bottom: AhSpace.sm),
            child: Row(
              children: <Widget>[
                StatusPill(
                  category: stage.category,
                  label: stage.name,
                  size: StatusPillSize.sm,
                ),
                const Spacer(),
                Text(
                  '${deals.length}',
                  style: AhText.data.copyWith(color: surfaces.textSecondary),
                ),
              ],
            ),
          ),
        ),
        if (deals.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: AhSpace.md),
            child: Text(
              strings(DealerText.stageEmpty),
              style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
            ),
          )
        else
          for (var index = 0; index < deals.length; index += 1)
            _DealRow(
              deal: deals[index],
              divider: index != deals.length - 1,
            ),
      ],
    );
  }
}

class _DealRow extends StatelessWidget {
  const _DealRow({required this.deal, required this.divider});

  final DealSummary deal;
  final bool divider;

  @override
  Widget build(BuildContext context) {
    final surfaces = context.ahSurfaces;

    return ConsoleRow(
      divider: divider,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(
                child: Text(
                  deal.listing.title,
                  style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              const SizedBox(width: AhSpace.sm),
              MoneyValue(minorUnits: deal.amountMinor, compact: true, size: MoneySize.sm),
            ],
          ),
          const SizedBox(height: 2),
          Row(
            children: <Widget>[
              Text(
                // The reference is what a dealer reads down the phone to support, so it is mono
                // with a slashed zero — see `AhText.data`.
                deal.reference,
                style: AhText.data.copyWith(fontSize: 12, color: surfaces.textSecondary),
              ),
              if (deal.counterparty != null) ...<Widget>[
                const SizedBox(width: AhSpace.sm),
                Expanded(
                  child: Text(
                    deal.counterparty!.displayName,
                    style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }
}
