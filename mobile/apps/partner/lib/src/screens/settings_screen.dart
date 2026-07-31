import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/partner_side.dart';
import '../app/partner_text.dart';
import '../data/partner_providers.dart';
import '../widgets/failure_view.dart';
import '../widgets/partner_page.dart';

/// Settings: which side of the market you are on, how the app looks, and what is still waiting to
/// be sent.
///
/// The side switch lives here rather than behind a support call because the two sides of this
/// market are not exclusive in practice. A travel company with six of its own cars is supply on
/// Monday and demand on Friday, and an app that made that a re-registration would be an app they
/// stop using on Tuesday.
class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerText text = ref.watch(partnerTextProvider);

    return PartnerBoard(
      eyebrow: text(PartnerKeys.sideEyebrow),
      title: text(PartnerKeys.settingsTitle),
      children: <Widget>[
        _SideSection(text: text),
        const SizedBox(height: AhSpace.xl),
        _ThemeSection(text: text),
        const SizedBox(height: AhSpace.xl),
        _QueueSection(text: text),
      ],
    );
  }
}

class _SideSection extends ConsumerWidget {
  const _SideSection({required this.text});

  final PartnerText text;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final PartnerSide? side = ref.watch(partnerSideProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(
          eyebrow: text(PartnerKeys.settingsSide),
          title: text(
            side == PartnerSide.demand
                ? PartnerKeys.sideDemandTitle
                : PartnerKeys.sideSupplyTitle,
          ),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        AhButton(
          label: text(PartnerKeys.settingsSideSwitch),
          variant: AhButtonVariant.outline,
          block: true,
          onPressed: () => unawaited(
            ref.read(partnerSideProvider.notifier).choose(
                  side == PartnerSide.supply ? PartnerSide.demand : PartnerSide.supply,
                ),
          ),
        ),
      ],
    );
  }
}

class _ThemeSection extends ConsumerWidget {
  const _ThemeSection({required this.text});

  final PartnerText text;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(
          eyebrow: text(PartnerKeys.settingsTheme),
          title: text(PartnerKeys.settingsTheme),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        Align(
          alignment: Alignment.centerLeft,
          child: AhThemeToggle(controller: ref.watch(themeControllerProvider)),
        ),
        const SizedBox(height: AhSpace.md),
        // Diagnostics, quiet and in mono. Which locale bundle resolved is the first question when a
        // partner reports the app "speaking English again", and it is a question a screenshot can
        // answer.
        Text(
          'locale ${text.locale ?? AhStrings.fallbackLocale}',
          style: AhText.label.copyWith(color: context.ahSurfaces.textSecondary),
        ),
      ],
    );
  }
}

/// The write queue, with a row per stuck write and the two things a person can do about one.
class _QueueSection extends ConsumerWidget {
  const _QueueSection({required this.text});

  final PartnerText text;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(
          eyebrow: text(PartnerKeys.settingsQueue),
          title: text(PartnerKeys.settingsQueue),
          description: text(PartnerKeys.settingsQueueBody),
          size: SectionHeadingSize.sm,
          rule: true,
        ),
        const SizedBox(height: AhSpace.md),
        AsyncSection<List<OutboxEntry>>(
          value: ref.watch(outboxEntriesProvider),
          onRetry: () => ref.invalidate(outboxEntriesProvider),
          builder: (BuildContext context, List<OutboxEntry> entries) {
            if (entries.isEmpty) {
              return EmptyState(
                title: text(PartnerKeys.settingsQueue),
                body: text(PartnerKeys.settingsQueueBody),
                align: CrossAxisAlignment.start,
              );
            }
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                for (final OutboxEntry entry in entries) ...<Widget>[
                  _QueueRow(entry: entry, text: text),
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

class _QueueRow extends ConsumerWidget {
  const _QueueRow({required this.entry, required this.text});

  final OutboxEntry entry;
  final PartnerText text;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return PartnerCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          // Method and path, in mono. This is the only screen in the app that shows a partner an
          // endpoint, and it earns it: the row is a write of theirs that did not land, and the
          // endpoint is what support needs to find it.
          Text(
            '${entry.intent.method.wire} ${entry.intent.endpoint}',
            style: AhText.data.copyWith(fontSize: 12, color: surfaces.textPrimary),
          ),
          const SizedBox(height: AhSpace.xs),
          Text(
            entry.idempotencyKey,
            style: AhText.label.copyWith(color: surfaces.textSecondary),
          ),
          if (entry.lastError != null) ...<Widget>[
            const SizedBox(height: AhSpace.sm),
            Text(
              entry.lastError!,
              style: AhText.bodySm.copyWith(color: surfaces.textPrimary),
            ),
          ],
          if (entry.lastErrorCode != null) ...<Widget>[
            const SizedBox(height: AhSpace.xs),
            Text(
              entry.lastErrorCode!,
              style: AhText.label.copyWith(color: surfaces.textSecondary),
            ),
          ],
          if (entry.needsAttention) ...<Widget>[
            const SizedBox(height: AhSpace.md),
            Row(
              children: <Widget>[
                AhButton(
                  label: text(PartnerKeys.actionRetry),
                  variant: AhButtonVariant.outline,
                  size: AhButtonSize.sm,
                  onPressed: () => unawaited(_retry(ref)),
                ),
                const SizedBox(width: AhSpace.sm),
                AhButton(
                  label: text(PartnerKeys.actionDismiss),
                  variant: AhButtonVariant.ghost,
                  size: AhButtonSize.sm,
                  onPressed: () =>
                      unawaited(ref.read(outboxProvider).discard(entry.idempotencyKey)),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  /// Puts the write back in the queue and asks the drainer to run.
  ///
  /// `retryNow` resets the attempt count, which is correct here and only here: the partner has
  /// looked at the failure and decided to send it again, and carrying the old count forward would
  /// poison the corrected write after a single further hiccup.
  Future<void> _retry(WidgetRef ref) async {
    await ref.read(outboxProvider).retryNow(entry.idempotencyKey);
    await ref.read(outboxDrainerProvider).drain();
  }
}
