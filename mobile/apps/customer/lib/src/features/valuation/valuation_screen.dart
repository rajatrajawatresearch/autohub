import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/router.dart';
import '../../data/app_providers.dart';
import '../../data/valuation.dart';
import '../../i18n/customer_text.dart';
import 'valuation_panel.dart';

/// The reveal, reached from a saved quote rather than from the hero.
///
/// Two ways in, and the difference matters:
///
/// * **From inside the app** — the garage, a share sheet, the hero's own history — the quote
///   travels in the route's `extra`. No network, no spinner, and it works in a lift.
/// * **Cold, from a deep link** — a notification, a link somebody sent — there is no `extra`, so the
///   quote comes off this phone's own store. A valuation is the user's unauthenticated question
///   about their own car; there is no endpoint to ask the server to hand it back, and inventing one
///   would be this client deciding a piece of the contract by itself.
///
/// When neither has it, the empty state is an instruction rather than an apology: the fastest route
/// to the number is to type the plate again, and that takes thirty seconds.
class ValuationScreen extends ConsumerWidget {
  const ValuationScreen({required this.quoteId, this.quote, super.key});

  final String quoteId;

  /// Supplied by the router when the reveal was reached in-app.
  final ValuationQuote? quote;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AppStrings t = ref.watch(appStringsProvider);
    final ValuationQuote? provided = quote;
    final AsyncValue<ValuationQuote?> saved = ref.watch(savedValuationProvider(quoteId));

    return Scaffold(
      appBar: AppBar(
        backgroundColor: context.ahSurfaces.page,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        title: Text(
          t(CustomerText.valuationTitle),
          style: AhText.subheading.copyWith(color: context.ahSurfaces.textPrimary),
        ),
        leading: BackButton(
          onPressed: () => context.canPop() ? context.pop() : context.go(Routes.home),
        ),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AhSpace.lg),
          child: switch (provided ?? saved.valueOrNull) {
            final ValuationQuote resolved => SingleChildScrollView(
                child: ValuationPanel(
                  quote: resolved,
                  onSell: () => context.go(Routes.sell),
                  onBrowse: () => context.go(Routes.buy),
                ),
              ),
            null when saved.isLoading => const _Skeleton(),
            null => EmptyState(
                title: t(CustomerText.valuationMissingTitle),
                body: t(CustomerText.valuationMissingBody),
                action: AhButton(
                  label: t(CustomerText.heroCta),
                  variant: AhButtonVariant.outline,
                  onPressed: () => context.go(Routes.home),
                ),
              ),
          },
        ),
      ),
    );
  }
}

class _Skeleton extends StatelessWidget {
  const _Skeleton();

  @override
  Widget build(BuildContext context) => const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          AhSkeleton(width: 180, height: 36),
          SizedBox(height: AhSpace.lg),
          AhSkeleton.line(width: 240),
          SizedBox(height: AhSpace.xl),
          AhSkeleton(height: 56, width: 200),
        ],
      );
}
