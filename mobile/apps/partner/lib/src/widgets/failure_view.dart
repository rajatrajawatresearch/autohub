import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/partner_failure.dart';
import '../app/partner_text.dart';
import '../data/partner_providers.dart';

/// The app-wide error surface.
///
/// One shape for every failure, and the whole design is in the last two lines of it: the sentence
/// the partner reads comes from the server through the i18n registry, and underneath it — small,
/// mono, secondary — sits the machine code and the trace id.
///
/// That placement is the decision worth defending. The code is not *hidden* behind a "details"
/// disclosure and it is not *shown* as though it were the message. Hiding it loses it: the
/// realistic support path in this market is a WhatsApp screenshot, and a code behind a tap is never
/// in the screenshot, so the ticket reads "it said something went wrong" and support starts by
/// guessing. Promoting it is worse — an owner-driver who is shown
/// `attachment.settlement.profile_unverified` as the headline concludes the app is broken rather
/// than that their bank account needs verifying. Present and quiet costs the user nothing, and
/// saves the round trip.
class FailureBlock extends StatelessWidget {
  const FailureBlock({
    required this.failure,
    required this.text,
    this.onRetry,
    super.key,
  });

  final PartnerFailure failure;
  final PartnerText text;

  /// Offered for anything that might be true in a minute. A `422` gets none — retrying a rejection
  /// produces the same rejection and spends the partner's battery to learn nothing.
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    // The tones are fixed rather than read from `AhSurfaces`, exactly as `StatusPill` fixes its
    // own: a failure chip is a sticker on a file, and its colours are chosen for AA against their
    // own fill so the same red means the same thing in a basement and in sunlight.
    return DecoratedBox(
      decoration: BoxDecoration(
        color: AhColors.flagSoft,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: AhColors.flag),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AhSpace.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            Text(
              text(failure.isOffline ? PartnerKeys.errorOffline : PartnerKeys.errorTitle)
                  .toUpperCase(),
              style: AhText.label.copyWith(color: AhColors.flag),
            ),
            const SizedBox(height: AhSpace.sm),
            Text(
              failure.message,
              style: AhText.body.copyWith(color: AhColors.tarmac),
            ),
            const SizedBox(height: AhSpace.md),
            // Mono, because it is transcribed. Tabular figures and a slashed zero come with
            // `AhText.data`, and a trace id read down a phone line needs both.
            Text(
              '${text(PartnerKeys.errorSupportRef)}: ${failure.supportReference}',
              style: AhText.data.copyWith(fontSize: 12, color: AhColors.n600),
            ),
            if (onRetry != null) ...<Widget>[
              const SizedBox(height: AhSpace.lg),
              AhButton(
                label: text(PartnerKeys.actionRetry),
                variant: AhButtonVariant.outline,
                size: AhButtonSize.sm,
                onPressed: onRetry,
              ),
            ],
          ],
        ),
      ),
    );
  }

  /// A hairline note for a failure that is not the point of the screen — a compliance strip that
  /// would not load beside a vehicle that did.
  static Widget quiet(BuildContext context, PartnerFailure failure) {
    final AhSurfaces surfaces = context.ahSurfaces;
    return Text(
      '${failure.message} · ${failure.supportReference}',
      style: AhText.bodySm.copyWith(color: surfaces.textSecondary),
    );
  }
}

/// Renders one `AsyncValue` in the house style: a held slot, a failure block, or the data.
///
/// The loading state is a skeleton and not a spinner because the only animation in AutoHub is the
/// odometer roll on a valuation reveal — and because a skeleton the size of the final element means
/// the row a partner is reaching for does not move when the data lands.
class AsyncSection<T> extends ConsumerWidget {
  const AsyncSection({
    required this.value,
    required this.builder,
    required this.onRetry,
    this.loading,
    super.key,
  });

  final AsyncValue<T> value;
  final Widget Function(BuildContext context, T data) builder;

  /// Almost always `() => ref.invalidate(theProvider)`.
  final VoidCallback onRetry;

  /// The held slot. Defaults to three lines, which is the right size for a list and wrong for a
  /// statement — pass the real dimensions where they are known.
  final Widget? loading;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return value.when(
      skipLoadingOnRefresh: true,
      data: (T data) => builder(context, data),
      loading: () => loading ?? const _DefaultSkeleton(),
      error: (Object error, StackTrace _) {
        final PartnerFailure failure = PartnerFailure.from(error);
        return FailureBlock(
          failure: failure,
          text: ref.watch(partnerTextProvider),
          // Field-level rejections are the one class that will never come good on a retry.
          onRetry: failure.fieldErrors.isEmpty ? onRetry : null,
        );
      },
    );
  }
}

class _DefaultSkeleton extends StatelessWidget {
  const _DefaultSkeleton();

  @override
  Widget build(BuildContext context) => const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          AhSkeleton.line(width: 180),
          SizedBox(height: AhSpace.sm),
          AhSkeleton.line(),
          SizedBox(height: AhSpace.sm),
          AhSkeleton.line(width: 240),
        ],
      );
}
