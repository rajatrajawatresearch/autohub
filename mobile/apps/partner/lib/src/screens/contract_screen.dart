import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../app/partner_failure.dart';
import '../app/partner_text.dart';
import '../data/attachment_api.dart';
import '../data/attachment_models.dart';
import '../data/attachment_writes.dart';
import '../data/partner_providers.dart';
import '../data/wire.dart';
import '../widgets/compliance_view.dart';
import '../widgets/contract_timeline.dart';
import '../widgets/failure_view.dart';
import '../widgets/partner_page.dart';

/// One contract, its lifecycle, its compliance and whatever this partner may do to it next.
///
/// proposal → trial → active → renewal → termination is nowhere in this file. The states arrive
/// from the workflow registry with their names and their categories, the history arrives from the
/// entity, and the buttons arrive from `availableActions` — evaluated server-side for this user,
/// against guards this app has never seen. A client that re-derived "may I terminate?" from a state
/// code and a permission list is a client an attacker can convince, and on a livelihood contract
/// that is not a theoretical objection.
class ContractScreen extends ConsumerWidget {
  const ContractScreen({required this.contractId, super.key});

  final String contractId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerText text = ref.watch(partnerTextProvider);

    // The page chrome is outside the `AsyncSection` on purpose. A partner who opens a contract from
    // a notification in a lift must land on a page with a back affordance and the app's own
    // surfaces, not on a bare failure block floating on a white rectangle. The contract's public
    // reference appears in the summary card once it arrives; the app bar does not wait for it.
    return PartnerDetailPage(
      title: text(PartnerKeys.contractsTitle),
      children: <Widget>[
        AsyncSection<AttachmentContract>(
          value: ref.watch(contractProvider(contractId)),
          onRetry: () => ref.invalidate(contractProvider(contractId)),
          builder: (BuildContext context, AttachmentContract contract) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              _Summary(contract: contract, text: text),
              const SizedBox(height: AhSpace.xl),
              _Lifecycle(contract: contract, text: text),
              const SizedBox(height: AhSpace.xl),
              _Compliance(contractId: contractId, text: text),
              const SizedBox(height: AhSpace.xl),
              _Cycles(contractId: contractId, text: text),
            ],
          ),
        ),
      ],
    );
  }
}

class _Summary extends StatelessWidget {
  const _Summary({required this.contract, required this.text});

  final AttachmentContract contract;
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
                child: Text(
                  contract.title,
                  style: AhText.subheading.copyWith(color: context.ahSurfaces.textPrimary),
                ),
              ),
              StatusPill(category: contract.state.category, label: contract.state.name),
            ],
          ),
          const SizedBox(height: AhSpace.md),
          Wrap(
            spacing: AhSpace.xl,
            runSpacing: AhSpace.md,
            children: <Widget>[
              FieldBlock(
                label: text(PartnerKeys.contractsCounterparty),
                value: contract.counterpartyName,
                hint: contract.contractTypeLabel,
              ),
              if (contract.rateMinor != null)
                FieldBlock(
                  label: contract.rateLabel ?? text(PartnerKeys.contractsRate),
                  // Not compact. This is the figure the partner is held to for the length of the
                  // contract, and `₹1.9K` is not a number anyone can sign.
                  valueWidget: MoneyValue(minorUnits: contract.rateMinor!),
                ),
              if (contract.cycleLabel != null)
                FieldBlock(label: text(PartnerKeys.contractsCycle), value: contract.cycleLabel),
              if (contract.startsOn != null)
                FieldBlock(
                  label: text(PartnerKeys.contractsStarts),
                  value: contract.startsOn,
                  mono: true,
                ),
              if (contract.endsOn != null)
                FieldBlock(
                  label: text(PartnerKeys.contractsEnds),
                  value: contract.endsOn,
                  mono: true,
                ),
            ],
          ),
        ],
      ),
    );
  }
}

/// The lifecycle panel: the registry's timeline, and the actions the server says are open.
class _Lifecycle extends ConsumerWidget {
  const _Lifecycle({required this.contract, required this.text});

  final AttachmentContract contract;
  final PartnerText text;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final String workflowKey = contract.workflowKey ?? AttachmentApi.contractWorkflowKey;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(
          eyebrow: text(PartnerKeys.contractsLifecycle),
          title: contract.state.name,
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        AsyncSection<WorkflowSnapshot>(
          value: ref.watch(contractWorkflowProvider(contract.id)),
          onRetry: () => ref.invalidate(contractWorkflowProvider(contract.id)),
          builder: (BuildContext context, WorkflowSnapshot snapshot) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              ContractTimeline(
                snapshot: snapshot,
                // Watched, not awaited: the timeline renders the history the moment it arrives and
                // picks up the state names when the registry document lands. A partner opening a
                // contract in a lift sees what happened, not a spinner.
                definition: ref.watch(workflowDefinitionProvider(workflowKey)).valueOrNull,
                emptyMessage: text(PartnerKeys.contractsHistoryEmpty),
              ),
              if (snapshot.availableActions.isNotEmpty) ...<Widget>[
                const SizedBox(height: AhSpace.lg),
                _Actions(contractId: contract.id, snapshot: snapshot, text: text),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _Actions extends ConsumerStatefulWidget {
  const _Actions({required this.contractId, required this.snapshot, required this.text});

  final String contractId;
  final WorkflowSnapshot snapshot;
  final PartnerText text;

  @override
  ConsumerState<_Actions> createState() => _ActionsState();
}

class _ActionsState extends ConsumerState<_Actions> {
  /// True while any action on this contract is in flight. All of them disable together: two
  /// transitions fired in the same second against one workflow instance is a `409` the partner did
  /// not need to see.
  bool _firing = false;
  PartnerFailure? _failure;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        for (final AvailableAction action in widget.snapshot.availableActions) ...<Widget>[
          AhButton(
            // The label is the server's. Nothing here maps `confirm` to "Confirm trial" — that
            // mapping is a `platform.workflow_transitions` row, and putting it in the binary is how
            // a renamed transition ships a stale verb to every handset.
            label: action.label,
            variant: _variantFor(action.emphasis),
            block: true,
            onPressed: _firing ? null : () => unawaited(_fire(action)),
          ),
          const SizedBox(height: AhSpace.sm),
        ],
        if (_failure != null) FailureBlock(failure: _failure!, text: widget.text),
      ],
    );
  }

  /// Emphasis is a closed contract vocabulary, so the switch is exhaustive by design: a fourth
  /// value would be a compile error here, which is the point of modelling it as an enum at all.
  /// `null` — an emphasis this build's decoder did not recognise — reads as the safe middle.
  AhButtonVariant _variantFor(ActionEmphasis? emphasis) => switch (emphasis) {
        ActionEmphasis.primary => AhButtonVariant.primary,
        ActionEmphasis.destructive => AhButtonVariant.danger,
        ActionEmphasis.standard => AhButtonVariant.outline,
        null => AhButtonVariant.outline,
      };

  Future<void> _fire(AvailableAction action) async {
    setState(() {
      _firing = true;
      _failure = null;
    });

    final WriteIntent intent = fireContractAction(
      contractId: widget.contractId,
      actionCode: action.actionCode,
      activeCity: ref.read(activeCityProvider).valueOrNull,
    );

    // Queued, not immediate. A trial confirmation or a renewal acceptance is measured in days by
    // the workflow's own SLA timers, so losing it to a tunnel matters and delivering it ninety
    // seconds late does not.
    final Outbox outbox = ref.read(outboxProvider);
    await outbox.enqueue(intent);
    await ref.read(outboxDrainerProvider).drain();
    final OutboxEntry? entry = await outbox.find(intent.idempotencyKey);

    if (!mounted) return;

    setState(() {
      _firing = false;
      _failure = entry == null || !entry.needsAttention
          ? null
          : PartnerFailure(
              message: entry.lastError ?? 'This contract moved while the action was queued.',
              code: entry.lastErrorCode ?? 'platform.workflow.transition_not_allowed',
            );
    });

    if (entry == null) {
      // Replace state from the server's answer rather than guessing what the transition did — the
      // action may have opened a cycle, released a commitment block or served notice.
      ref
        ..invalidate(contractWorkflowProvider(widget.contractId))
        ..invalidate(contractProvider(widget.contractId))
        ..invalidate(contractsProvider);
    }
  }
}

class _Compliance extends ConsumerWidget {
  const _Compliance({required this.contractId, required this.text});

  final String contractId;
  final PartnerText text;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(
          eyebrow: text(PartnerKeys.complianceTitle),
          title: text(PartnerKeys.complianceTitle),
          description: text(PartnerKeys.complianceBody),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        AsyncSection<List<ComplianceDocument>>(
          value: ref.watch(contractComplianceProvider(contractId)),
          onRetry: () => ref.invalidate(contractComplianceProvider(contractId)),
          builder: (BuildContext context, List<ComplianceDocument> documents) => ComplianceList(
            documents: documents,
            now: ref.watch(serverClockProvider).now(),
            text: text,
          ),
        ),
      ],
    );
  }
}

class _Cycles extends ConsumerWidget {
  const _Cycles({required this.contractId, required this.text});

  final String contractId;
  final PartnerText text;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(
          eyebrow: text(PartnerKeys.settlementTitle),
          title: text(PartnerKeys.settlementLines),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        AsyncSection<CursorPage<SettlementCycle>>(
          value: ref.watch(cyclesProvider(contractId)),
          onRetry: () => ref.invalidate(cyclesProvider(contractId)),
          builder: (BuildContext context, CursorPage<SettlementCycle> page) {
            if (page.isEmpty) {
              return EmptyState(
                title: text(PartnerKeys.settlementEmptyTitle),
                body: text(PartnerKeys.settlementEmptyBody),
                align: CrossAxisAlignment.start,
              );
            }
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                for (final SettlementCycle cycle in page.items) ...<Widget>[
                  CycleRow(cycle: cycle, text: text),
                  const SizedBox(height: AhSpace.sm),
                ],
              ],
            );
          },
        ),
      ],
    );
  }
}

/// One cycle as a list row. Shared by the contract page and the settlement board so the two cannot
/// disagree about what a cycle is worth.
class CycleRow extends StatelessWidget {
  const CycleRow({required this.cycle, required this.text, super.key});

  final SettlementCycle cycle;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return PartnerCard(
      onTap: () => context.go('/cycles/${Uri.encodeComponent(cycle.id)}'),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                Text(
                  cycle.publicRef ?? '${text(PartnerKeys.settlementPeriod)} ${cycle.cycleNo ?? ''}',
                  style: AhText.data.copyWith(fontSize: 13, color: surfaces.textPrimary),
                ),
                const SizedBox(height: 2),
                StatusPill(
                  category: cycle.state.category,
                  label: cycle.state.name,
                  size: StatusPillSize.sm,
                ),
              ],
            ),
          ),
          const SizedBox(width: AhSpace.md),
          FieldBlock(
            label: text(PartnerKeys.settlementNet),
            valueWidget: MoneyValue(
              minorUnits: cycle.netPayableMinor,
              currency: cycle.currency,
              size: MoneySize.sm,
            ),
            hint: cycle.payoutOn,
            align: CrossAxisAlignment.end,
          ),
        ],
      ),
    );
  }
}
