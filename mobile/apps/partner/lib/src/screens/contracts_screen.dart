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

/// Every contract this partner is on, from either side.
///
/// The list is identical for an owner and an operator, because a contract *is* identical — one row,
/// two parties, one recurring settlement. Only the counterparty's name swaps ends. That is the
/// clearest evidence in this app that attachment is one module rather than two verticals sharing a
/// name.
class ContractsScreen extends ConsumerWidget {
  const ContractsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerText text = ref.watch(partnerTextProvider);

    return PartnerBoard(
      eyebrow: text(PartnerKeys.sideEyebrow),
      title: text(PartnerKeys.contractsTitle),
      onRefresh: () async => ref.invalidate(contractsProvider),
      children: <Widget>[
        AsyncSection<CursorPage<AttachmentContract>>(
          value: ref.watch(contractsProvider),
          onRetry: () => ref.invalidate(contractsProvider),
          builder: (BuildContext context, CursorPage<AttachmentContract> page) {
            if (page.isEmpty) {
              return EmptyState(
                title: text(PartnerKeys.contractsEmptyTitle),
                body: text(PartnerKeys.contractsEmptyBody),
              );
            }

            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                for (final AttachmentContract contract in page.items) ...<Widget>[
                  _ContractRow(contract: contract, text: text),
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

class _ContractRow extends StatelessWidget {
  const _ContractRow({required this.contract, required this.text});

  final AttachmentContract contract;
  final PartnerText text;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return PartnerCard(
      onTap: () => context.go('/contracts/${Uri.encodeComponent(contract.id)}'),
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
                      contract.title,
                      style: AhText.body.copyWith(color: surfaces.textPrimary),
                    ),
                    if (contract.publicRef != null)
                      // Mono, because it is the string a partner reads down the phone to support.
                      Text(
                        contract.publicRef!,
                        style: AhText.data.copyWith(
                          fontSize: 12,
                          color: surfaces.textSecondary,
                        ),
                      ),
                  ],
                ),
              ),
              const SizedBox(width: AhSpace.sm),
              StatusPill(
                category: contract.state.category,
                label: contract.state.name,
                size: StatusPillSize.sm,
              ),
            ],
          ),
          const SizedBox(height: AhSpace.md),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Expanded(
                child: FieldBlock(
                  label: text(PartnerKeys.contractsCounterparty),
                  value: contract.counterpartyName,
                  hint: contract.contractTypeLabel,
                ),
              ),
              if (contract.rateMinor != null)
                Expanded(
                  child: FieldBlock(
                    label: contract.rateLabel ?? text(PartnerKeys.contractsRate),
                    valueWidget: MoneyValue(minorUnits: contract.rateMinor!, size: MoneySize.sm),
                    hint: contract.cycleLabel,
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
