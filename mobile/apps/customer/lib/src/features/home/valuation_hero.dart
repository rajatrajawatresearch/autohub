import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/fault.dart';
import '../../app/router.dart';
import '../../data/app_providers.dart';
import '../../data/valuation.dart';
import '../../i18n/customer_text.dart';
import '../valuation/valuation_panel.dart';

/// The thesis, rendered as an interaction.
///
/// This screen does not open with a photograph of a car and a headline over it. It opens with an
/// **empty number plate waiting to be typed in** — the most characteristic artifact in Indian
/// automotive life, doing the most valuable job in the product. One input, one outcome, no funnel
/// and no phone number.
///
/// ## The answer never navigates
///
/// [ValuationRevealed] replaces this widget's own subtree. It does not push a route, and that is
/// the single most important structural decision on the screen: a navigation between the question
/// and the answer puts a page load, a transition and a back button in the middle of the one moment
/// this product is judged on. The user taps once and the number is *there*, rolling into place on
/// the plate they just typed.
///
/// ## What is deliberately absent
///
/// No phone field. No "sign in to see your price". No progress steps. §10 of the contract makes the
/// first quote unauthenticated precisely because asking for a number before showing a number is why
/// users bounce, and the reassurance line under the button says so in as many words.
class ValuationHero extends ConsumerStatefulWidget {
  const ValuationHero({super.key});

  @override
  ConsumerState<ValuationHero> createState() => _ValuationHeroState();
}

class _ValuationHeroState extends ConsumerState<ValuationHero> {
  /// The canonical plate — uppercase, no separators. `PlateInput` owns the grouped display.
  String _plate = '';

  final TextEditingController _kms = TextEditingController();

  /// Six characters is the shortest registration any Indian series produces. Whether the plate is
  /// *valid* is the server's answer and the form registry's rule; this only decides when the button
  /// stops being pointless to press.
  static const int _minimumPlateLength = 6;

  @override
  void dispose() {
    _kms.dispose();
    super.dispose();
  }

  bool get _canSubmit => _plate.length >= _minimumPlateLength;

  void _submit() {
    if (!_canSubmit) return;
    final String digits = _kms.text.replaceAll(RegExp(r'\D'), '');
    unawaited(
      ref.read(valuationHeroProvider.notifier).submit(
            registrationNumber: _plate,
            kmsDriven: digits.isEmpty ? null : int.tryParse(digits),
          ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final ValuationHeroState state = ref.watch(valuationHeroProvider);

    return switch (state) {
      ValuationRevealed(:final ValuationQuote quote) => _Reveal(quote: quote),
      ValuationFailed() || ValuationIdle() || ValuationWorking() => _Ask(
          plate: _plate,
          kms: _kms,
          working: state is ValuationWorking,
          fault: state is ValuationFailed ? state.fault : null,
          canSubmit: _canSubmit,
          onPlateChanged: (String value) => setState(() => _plate = value),
          onSubmit: _submit,
        ),
    };
  }
}

/// The question.
class _Ask extends ConsumerWidget {
  const _Ask({
    required this.plate,
    required this.kms,
    required this.working,
    required this.fault,
    required this.canSubmit,
    required this.onPlateChanged,
    required this.onSubmit,
  });

  final String plate;
  final TextEditingController kms;
  final bool working;

  /// Rendered *inside* the plate field rather than as a page-level card: the overwhelmingly common
  /// cause is a mistyped digit, and the fix is four characters away from where the user is looking.
  /// The machine code still reaches the diagnostics log — see `DiagnosticsLog.capture`.
  final AppFault? fault;

  final bool canSubmit;
  final ValueChanged<String> onPlateChanged;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final AhSurfaces surfaces = context.ahSurfaces;
    final AppStrings t = ref.watch(appStringsProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(
          t(CustomerText.heroEyebrow).toUpperCase(),
          textAlign: TextAlign.center,
          style: AhText.label.copyWith(color: surfaces.textSecondary),
        ),
        const SizedBox(height: AhSpace.md),
        Text(
          t(CustomerText.heroTitle),
          textAlign: TextAlign.center,
          style: AhText.displayXl.copyWith(color: surfaces.textPrimary),
        ),

        const SizedBox(height: AhSpace.xxl),

        // The input **is** the plate: same hard border, same mono face, same tracking, same zero
        // radius, and it does not flip in dark mode — the user is often standing next to the car,
        // and the screen and the windscreen have to agree.
        PlateInput(
          value: plate,
          size: PlateInputSize.lg,
          label: t(CustomerText.heroPlateLabel),
          error: fault?.message,
          onChanged: onPlateChanged,
          onSubmitted: (_) => onSubmit(),
        ),

        const SizedBox(height: AhSpace.lg),

        AhTextField(
          controller: kms,
          label: t(CustomerText.heroKmsLabel),
          placeholder: t(CustomerText.heroKmsPlaceholder),
          unit: 'km',
          mono: true,
          keyboardType: TextInputType.number,
          textInputAction: TextInputAction.go,
          inputFormatters: <TextInputFormatter>[
            FilteringTextInputFormatter.digitsOnly,
            LengthLimitingTextInputFormatter(7),
          ],
          onSubmitted: (_) => onSubmit(),
        ),

        const SizedBox(height: AhSpace.lg),

        // The one rationed yellow element on this screen. Nothing else here may wear it.
        AhButton(
          label: working ? t(CustomerText.heroWorking) : t(CustomerText.heroCta),
          variant: AhButtonVariant.action,
          size: AhButtonSize.lg,
          block: true,
          // There is no spinner. A button that is working is a button whose handler is null and
          // whose label changed — the motion budget is spent on the odometer and nowhere else.
          onPressed: (canSubmit && !working) ? onSubmit : null,
        ),

        const SizedBox(height: AhSpace.md),
        Text(
          t(CustomerText.heroReassurance),
          textAlign: TextAlign.center,
          style: AhText.data.copyWith(fontSize: 12, color: surfaces.textSecondary),
        ),
      ],
    );
  }
}

/// The answer, in place.
class _Reveal extends ConsumerWidget {
  const _Reveal({required this.quote});

  final ValuationQuote quote;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ValuationPanel(
      quote: quote,
      onReset: ref.read(valuationHeroProvider.notifier).reset,
      onSell: () => context.go(Routes.sell),
      onBrowse: () => context.go(Routes.buy),
    );
  }
}
