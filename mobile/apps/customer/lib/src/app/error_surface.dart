import 'dart:async';

import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/app_providers.dart';
import '../i18n/customer_text.dart';
import 'fault.dart';

/// The one error surface in the app.
///
/// ## What the user sees, and what support gets
///
/// The resting screen carries **no machine code**. A customer standing in a car park cannot act on
/// `catalog.vahan.record_not_found`; showing it to them turns a recoverable moment into one that
/// reads as broken, and it is the fastest way to make a product feel like somebody else's admin
/// console. So the card is a sentence that names the problem and the fix, and a control that does
/// the thing again.
///
/// The code is not discarded — it is one tap away. *Copy reference* puts
/// `code · status · traceId` on the clipboard and echoes it in the confirmation, which is the
/// moment it is worth being on screen: a support agent has asked for it, and a screenshot of that
/// snackbar is a log query. Every fault also lands in [diagnosticsLogProvider] whether or not the
/// user copies anything.
///
/// ## Why one widget
///
/// Search, the valuation hero, the sell flow and the garage all fail in the same four ways, and a
/// screen that writes its own error card writes its own vocabulary with it. One widget means one
/// voice, one copy affordance, and one place to add a diagnostic when support asks for one.
class AppErrorView extends ConsumerWidget {
  const AppErrorView({
    required this.fault,
    this.onRetry,
    this.secondary,
    super.key,
  });

  final AppFault fault;

  /// `null` when there is nothing worth retrying — a `404`, a `422` the user must fix first.
  final Future<void> Function()? onRetry;

  /// A second control for the cases where "try again" is not the useful next move.
  final Widget? secondary;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final Future<void> Function()? retry = onRetry;

    return EmptyState(
      // An outage is not a rejection. Telling a user their input was wrong when the tunnel was
      // wrong is how a perfectly good registration number gets retyped four times.
      title: fault.isOffline ? t(CustomerText.errorOfflineTitle) : t(CustomerText.errorTitle),
      body: fault.message,
      action: Wrap(
        spacing: AhSpace.sm,
        runSpacing: AhSpace.sm,
        alignment: WrapAlignment.center,
        children: <Widget>[
          if (retry != null)
            AhButton(
              label: t(CustomerText.errorRetry),
              variant: AhButtonVariant.outline,
              onPressed: () => unawaited(retry()),
            ),
          if (secondary != null) secondary!,
          FaultReferenceButton(fault: fault),
        ],
      ),
    );
  }
}

/// Copies the diagnostic and confirms it, and is the only path by which a machine code reaches a
/// screen.
class FaultReferenceButton extends ConsumerWidget {
  const FaultReferenceButton({required this.fault, super.key});

  final AppFault fault;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);

    return AhButton(
      label: t(CustomerText.errorCopy),
      variant: AhButtonVariant.ghost,
      size: AhButtonSize.sm,
      onPressed: () => unawaited(_copy(context, t)),
    );
  }

  Future<void> _copy(BuildContext context, AppStrings t) async {
    await Clipboard.setData(ClipboardData(text: fault.reference));
    // The clipboard is a platform channel, so this is a real async gap and the user may have popped
    // the screen across it. Showing a snackbar on a dead context is an exception in release.
    if (!context.mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('${t(CustomerText.errorCopied)} · ${fault.reference}'),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }
}

/// The inline form of the same thing, for a failure that must not take the whole screen.
///
/// Used where the page has already rendered something worth keeping — a search that failed while
/// loading page four, an offer that did not send under a listing the user is still reading.
class AppFaultBanner extends ConsumerWidget {
  const AppFaultBanner({required this.fault, this.onRetry, super.key});

  final AppFault fault;
  final Future<void> Function()? onRetry;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final Future<void> Function()? retry = onRetry;

    return DecoratedBox(
      decoration: const BoxDecoration(
        color: AhColors.flagSoft,
        // A left rule rather than an icon: it survives being read in sunlight, and it costs no
        // width on a 360dp handset.
        border: Border(left: BorderSide(color: AhColors.flag, width: 2)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AhSpace.md, vertical: AhSpace.sm),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: <Widget>[
            Expanded(
              child: Semantics(
                liveRegion: true,
                child: Text(
                  fault.message,
                  style: AhText.bodySm.copyWith(color: AhColors.flag),
                ),
              ),
            ),
            if (retry != null)
              AhButton(
                label: t(CustomerText.errorRetry),
                variant: AhButtonVariant.ghost,
                size: AhButtonSize.sm,
                onPressed: () => unawaited(retry()),
              )
            else
              // No retry means the failure is permanent for this input; the only useful control
              // left is the one that hands support the code.
              FaultReferenceButton(fault: fault),
          ],
        ),
      ),
    );
  }
}
