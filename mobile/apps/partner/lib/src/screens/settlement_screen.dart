import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/partner_text.dart';
import '../data/attachment_models.dart';
import '../data/partner_providers.dart';
import '../widgets/failure_view.dart';
import '../widgets/partner_page.dart';
import 'contract_screen.dart';

/// Every closed cycle across every contract, newest first.
///
/// This is where the module earns and where the partner checks whether it paid. One cycle is one
/// deal, one invoice set and one payout, and the two numbers that matter on a row are what is due
/// and when it lands — so those are the two numbers on it.
class SettlementScreen extends ConsumerWidget {
  const SettlementScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerText text = ref.watch(partnerTextProvider);

    return PartnerBoard(
      eyebrow: text(PartnerKeys.sideEyebrow),
      title: text(PartnerKeys.settlementTitle),
      onRefresh: () async => ref.invalidate(cyclesProvider(null)),
      children: <Widget>[
        AsyncSection<CursorPage<SettlementCycle>>(
          value: ref.watch(cyclesProvider(null)),
          onRetry: () => ref.invalidate(cyclesProvider(null)),
          builder: (BuildContext context, CursorPage<SettlementCycle> page) {
            if (page.isEmpty) {
              return EmptyState(
                title: text(PartnerKeys.settlementEmptyTitle),
                body: text(PartnerKeys.settlementEmptyBody),
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
