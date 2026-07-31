import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/error_surface.dart';
import '../app/providers.dart';
import '../app/strings.dart';

/// One `AsyncValue`, rendered the same way on every screen in the console.
///
/// Three behaviours worth naming, because getting any of them wrong is what makes a data-heavy app
/// feel unreliable:
///
/// **Stale data outranks a spinner.** While a refetch is in flight, whatever the screen already had
/// stays on screen. A dealer pulling to refresh their inventory must not watch it blank and return
/// — the list they were reading is still true, and replacing it with skeletons for 400ms is a
/// flicker they will read as the app losing their stock.
///
/// **A failure with stale data is a banner, not a page.** If the refetch fails but the previous
/// answer is still here, the previous answer is still the best thing to show. Only a failure with
/// *nothing* behind it earns the whole surface.
///
/// **Loading is held layout, not motion.** [AhSkeleton] is a slot, not a shimmer, for the reason
/// its own doc gives: this product spends its entire motion budget on the odometer roll.
class AsyncSection<T> extends ConsumerWidget {
  const AsyncSection({
    required this.value,
    required this.builder,
    this.onRetry,
    this.loading,
    super.key,
  });

  final AsyncValue<T> value;
  final Widget Function(BuildContext context, T data) builder;

  /// Usually `() => ref.invalidate(theProvider)`.
  final VoidCallback? onRetry;

  /// The held layout. Give it the final content's dimensions — a skeleton that is the wrong size
  /// promises a shape and then moves the thing the dealer was reaching for.
  final Widget? loading;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final strings = ref.watch(stringsProvider);
    final data = value.valueOrNull;

    if (data != null) {
      final failure = value.hasError ? value.error : null;
      if (failure == null) return builder(context, data);

      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          _StaleBanner(failure: AhFailure.from(failure, strings), onRetry: onRetry),
          const SizedBox(height: AhSpace.md),
          builder(context, data),
        ],
      );
    }

    return value.when(
      data: (data) => builder(context, data),
      loading: () => loading ?? const _DefaultSkeleton(),
      error: (error, _) => FailurePanel(
        failure: AhFailure.from(error, strings),
        strings: strings,
        onRetry: onRetry,
      ),
    );
  }
}

/// A hairline strip over content that is still good but no longer confirmed.
class _StaleBanner extends ConsumerWidget {
  const _StaleBanner({required this.failure, this.onRetry});

  final AhFailure failure;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final strings = ref.watch(stringsProvider);

    return DecoratedBox(
      decoration: const BoxDecoration(
        // Amber fill with dark amber ink, fixed in both themes. A status colour is a sticker on a
        // file: it is chosen for AA against its own fill, not against the page.
        color: AhColors.cautionSoft,
        border: Border(left: BorderSide(color: AhColors.caution, width: 2)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AhSpace.sm, vertical: AhSpace.xs + 2),
        child: Row(
          children: <Widget>[
            Expanded(
              child: Text(
                failure.message,
                style: AhText.bodySm.copyWith(color: AhColors.caution),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ),
            if (onRetry != null) ...<Widget>[
              const SizedBox(width: AhSpace.sm),
              AhButton(
                label: strings(DealerText.retry),
                onPressed: onRetry,
                variant: AhButtonVariant.ghost,
                size: AhButtonSize.sm,
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _DefaultSkeleton extends StatelessWidget {
  const _DefaultSkeleton();

  @override
  Widget build(BuildContext context) {
    final gap = context.ahDensity.gap;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        for (var index = 0; index < 6; index += 1) ...<Widget>[
          const AhSkeleton(height: 40),
          SizedBox(height: gap),
        ],
      ],
    );
  }
}
