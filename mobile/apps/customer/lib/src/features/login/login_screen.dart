import 'dart:async';

import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/error_surface.dart';
import '../../app/router.dart';
import '../../data/app_providers.dart';
import '../../data/session.dart';
import '../../i18n/customer_text.dart';

/// Phone, then a six-digit code. Two screens, no password.
///
/// India is phone-first. A password is one more thing for a user to lose and a support ticket
/// waiting to happen, so the OTP is the primary factor here rather than a second one — and the
/// number is the account, which is why the whole flow is two fields and no profile step.
///
/// Both steps live on one route. `/login` is a single address a deep link can point at, and the
/// step is a state rather than a URL because a code screen with no challenge behind it — which is
/// what `/login/verify` would be on a cold link — is a dead end.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final TextEditingController _phone = TextEditingController();
  final TextEditingController _code = TextEditingController();

  /// Counts down to the moment a resend is allowed. The server tells us how long via
  /// `resendAfterSeconds`; a client that picked its own number would either annoy the user or
  /// invite the rate limiter.
  int _resendIn = 0;
  Timer? _ticker;

  @override
  void dispose() {
    _ticker?.cancel();
    _phone.dispose();
    _code.dispose();
    super.dispose();
  }

  void _startResendCountdown(int seconds) {
    _ticker?.cancel();
    setState(() => _resendIn = seconds);
    _ticker = Timer.periodic(const Duration(seconds: 1), (Timer timer) {
      if (!mounted || _resendIn <= 1) {
        timer.cancel();
        if (mounted) setState(() => _resendIn = 0);
        return;
      }
      setState(() => _resendIn -= 1);
    });
  }

  @override
  Widget build(BuildContext context) {
    final AppStrings t = ref.watch(appStringsProvider);
    final LoginState state = ref.watch(loginProvider);
    final LoginController controller = ref.read(loginProvider.notifier);

    ref.listen<LoginState>(loginProvider, (LoginState? previous, LoginState next) {
      // The listener runs outside the build phase, so by the time a state change lands this screen
      // may already be on its way out — a deep link that resolved, a back gesture, or the very
      // navigation this callback performed a moment ago. Both branches below reach for an ancestor
      // (`setState` and the router), and looking one up through a deactivated element throws.
      if (!mounted) return;

      if (next is LoginCodeEntry && previous is! LoginCodeEntry) {
        _startResendCountdown(next.challenge.resendAfterSeconds);
      }
      if (next is LoginComplete) {
        // Back to whatever asked for a session, or to the garage when the login was the deep link
        // that opened the app.
        if (context.canPop()) {
          context.pop();
        } else {
          context.go(Routes.garage);
        }
      }
    });

    return Scaffold(
      appBar: AppBar(
        backgroundColor: context.ahSurfaces.page,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        leading: BackButton(
          onPressed: () => context.canPop() ? context.pop() : context.go(Routes.home),
        ),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AhSpace.lg),
          children: <Widget>[
            switch (state) {
              LoginPhoneEntry() => _PhoneStep(
                  controller: controller,
                  field: _phone,
                  state: state,
                  t: t,
                ),
              LoginCodeEntry() => _CodeStep(
                  controller: controller,
                  field: _code,
                  state: state,
                  t: t,
                  resendIn: _resendIn,
                  onResend: () {
                    _code.clear();
                    unawaited(controller.resend());
                  },
                ),
              // The listener above has already navigated; this frame is the last one on screen.
              LoginComplete() => const SizedBox.shrink(),
            },
          ],
        ),
      ),
    );
  }
}

class _PhoneStep extends StatelessWidget {
  const _PhoneStep({
    required this.controller,
    required this.field,
    required this.state,
    required this.t,
  });

  final LoginController controller;
  final TextEditingController field;
  final LoginPhoneEntry state;
  final AppStrings t;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(
          eyebrow: t(CustomerText.loginStepPhone),
          title: t(CustomerText.loginTitlePhone),
          description: t(CustomerText.loginDescPhone),
        ),
        const SizedBox(height: AhSpace.xl),
        AhTextField(
          controller: field,
          label: t(CustomerText.loginPhoneLabel),
          placeholder: t(CustomerText.loginPhonePlaceholder),
          // The dial code is fixed for this market and shown rather than typed, so a user cannot
          // half-enter `+91` and produce an eleven-digit number the server will reject.
          unit: LoginController.dialCode,
          mono: true,
          autofocus: true,
          keyboardType: TextInputType.phone,
          textInputAction: TextInputAction.go,
          inputFormatters: <TextInputFormatter>[
            FilteringTextInputFormatter.digitsOnly,
            LengthLimitingTextInputFormatter(LoginController.nationalNumberLength),
          ],
          onSubmitted: (String value) => unawaited(controller.requestCode(value)),
        ),
        const SizedBox(height: AhSpace.lg),
        AhButton(
          label: state.sending ? t(CustomerText.loginSending) : t(CustomerText.loginSend),
          size: AhButtonSize.lg,
          block: true,
          onPressed: state.sending ? null : () => unawaited(controller.requestCode(field.text)),
        ),
        if (state.fault != null) ...<Widget>[
          const SizedBox(height: AhSpace.lg),
          AppFaultBanner(fault: state.fault!),
        ],
      ],
    );
  }
}

class _CodeStep extends StatelessWidget {
  const _CodeStep({
    required this.controller,
    required this.field,
    required this.state,
    required this.t,
    required this.resendIn,
    required this.onResend,
  });

  final LoginController controller;
  final TextEditingController field;
  final LoginCodeEntry state;
  final AppStrings t;
  final int resendIn;
  final VoidCallback onResend;

  /// Six, per the contract's example and the server's template. Not a registry value — the length
  /// of the code is part of the SMS the server sends, and the two must agree.
  static const int _codeLength = 6;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SectionHeading(
          eyebrow: t(CustomerText.loginStepCode),
          title: t(CustomerText.loginTitleCode),
          description: t(
            CustomerText.loginDescCode,
            <String, Object?>{'phone': state.challenge.maskedTo ?? state.phone},
          ),
        ),
        if (state.challenge.hint != null) ...<Widget>[
          const SizedBox(height: AhSpace.sm),
          Text(
            // Four characters echoed in the SMS body. If it does not match what is on screen, the
            // message did not come from us — which is worth showing in a market with this much
            // OTP fraud.
            state.challenge.hint!,
            style: AhText.data.copyWith(color: surfaces.textSecondary),
          ),
        ],
        const SizedBox(height: AhSpace.xl),
        AhTextField(
          controller: field,
          label: t(CustomerText.loginCodeLabel),
          placeholder: t(CustomerText.loginCodePlaceholder),
          mono: true,
          autofocus: true,
          keyboardType: TextInputType.number,
          textInputAction: TextInputAction.go,
          inputFormatters: <TextInputFormatter>[
            FilteringTextInputFormatter.digitsOnly,
            LengthLimitingTextInputFormatter(_codeLength),
          ],
          onChanged: (String value) {
            // Submitting on the last digit is the difference between two taps and one on a screen
            // whose whole job is six characters. The keyboard's Go key still works for anyone who
            // pastes.
            if (value.length == _codeLength) unawaited(controller.verify(value));
          },
          onSubmitted: (String value) => unawaited(controller.verify(value)),
        ),
        const SizedBox(height: AhSpace.lg),
        AhButton(
          label: state.verifying ? t(CustomerText.loginVerifying) : t(CustomerText.loginVerify),
          size: AhButtonSize.lg,
          block: true,
          onPressed: state.verifying ? null : () => unawaited(controller.verify(field.text)),
        ),
        if (state.fault != null) ...<Widget>[
          const SizedBox(height: AhSpace.lg),
          AppFaultBanner(fault: state.fault!),
        ],
        const SizedBox(height: AhSpace.lg),
        // Both `Flexible`, because both labels are registry strings. "Change number" is two words
        // in English and can be five in Marathi, and a fixed-width row of two intrinsically-sized
        // controls is an overflow stripe waiting for a translation.
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: <Widget>[
            Flexible(
              child: AhButton(
                label: t(CustomerText.loginChangeNumber),
                variant: AhButtonVariant.ghost,
                size: AhButtonSize.sm,
                onPressed: controller.changeNumber,
              ),
            ),
            const SizedBox(width: AhSpace.sm),
            Flexible(
              child: AhButton(
                label: resendIn > 0
                    ? t(CustomerText.loginResendIn, <String, Object?>{'seconds': resendIn})
                    : t(CustomerText.loginResend),
                variant: AhButtonVariant.ghost,
                size: AhButtonSize.sm,
                onPressed: resendIn > 0 ? null : onResend,
              ),
            ),
          ],
        ),
      ],
    );
  }
}
