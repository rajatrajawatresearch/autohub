import 'dart:async';

import 'package:autohub_core/autohub_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../app/fault.dart';
import 'app_providers.dart';

/// Who is using the app, if anyone.
///
/// Signed out is a first-class state and not an error: browsing, searching, opening a listing and
/// taking a valuation all work without an account, deliberately — §10 of the contract makes the
/// first quote unauthenticated because asking for a phone number before showing a number is why
/// users bounce.
@immutable
sealed class SessionState {
  const SessionState();
}

/// Before the stored session has been read off the keychain.
final class SessionUnknown extends SessionState {
  const SessionUnknown();
}

final class SignedOut extends SessionState {
  const SignedOut();
}

final class SignedIn extends SessionState {
  const SignedIn(this.user);

  final AuthUser user;
}

final NotifierProvider<SessionController, SessionState> sessionProvider =
    NotifierProvider<SessionController, SessionState>(SessionController.new);

/// Holds the signed-in user and nothing else.
///
/// It does **not** hold the tokens. Those live in `SessionTokenStore`, which the transport reads
/// synchronously on every request; duplicating them here would create two answers to "am I signed
/// in" and guarantee that one of them is stale after a refresh rotation.
class SessionController extends Notifier<SessionState> {
  @override
  SessionState build() {
    // The token store is the authority. It emits on every write including the sign-out, so a
    // session revoked by the server during a background refresh lands here without polling.
    final StreamSubscription<AuthTokens?> subscription =
        ref.watch(sessionTokenStoreProvider).changes.listen((AuthTokens? tokens) {
      if (tokens == null) state = const SignedOut();
    });
    ref.onDispose(subscription.cancel);

    unawaited(Future<void>.microtask(restore));
    return const SessionUnknown();
  }

  /// Resolves who the stored tokens belong to.
  ///
  /// A failure here does **not** sign the user out. A cold launch in a lift cannot reach
  /// `/auth/me`, and treating that as "signed out" would throw away a perfectly good thirty-day
  /// refresh token because a lift has no signal.
  Future<void> restore() async {
    final SessionTokenStore tokens = ref.read(sessionTokenStoreProvider);
    if (tokens.tokens == null) {
      state = const SignedOut();
      return;
    }

    try {
      final AuthUser user = await ref.read(customerApiProvider).me();
      await tokens.attachUser(user.id);
      state = SignedIn(user);
    } on ApiException catch (error) {
      // Only an authentication failure means signed out. Anything else — a tunnel, a 500 — leaves
      // the session alone and the shell renders as unknown, which reads as "not yet" rather than
      // "you have been logged out".
      if (error.statusCode == 401 || error.statusCode == 403) {
        await signOut();
      }
    }
  }

  /// Called by the login screen once `POST /auth/otp/verify` has answered.
  ///
  /// Writes the pair through to the keychain *before* touching state, so the first request the next
  /// screen makes already carries the bearer. The other order produces a `401` on a session that is
  /// two milliseconds old.
  Future<void> adopt(AuthSession session) async {
    final SessionTokenStore store = ref.read(sessionTokenStoreProvider);
    await store.write(
      AuthTokens.fromExpiresIn(
        accessToken: session.accessToken,
        refreshToken: session.refreshToken,
        expiresInSeconds: session.expiresIn,
      ),
    );
    await store.attachUser(session.user.id);
    state = SignedIn(session.user);
  }

  /// Ends the session on this device.
  ///
  /// The local session ends whether or not the server acknowledged — a user who taps sign out on a
  /// train has signed out. The registry cache and the outbox go with it: features and i18n are
  /// evaluated per user, and one person's queued listing must never drain under the next person's
  /// token.
  Future<void> signOut() async {
    final SessionTokenStore store = ref.read(sessionTokenStoreProvider);
    final String? refreshToken = store.tokens?.refreshToken;

    if (refreshToken != null && refreshToken.isNotEmpty) {
      try {
        await ref.read(customerApiProvider).logout(refreshToken);
      } on ApiException {
        // Best effort. The server will expire the family on its own; what matters is that this
        // handset stops holding a live token.
      }
    }

    await store.write(null);
    await ref.read(outboxProvider).clear();
    state = const SignedOut();
  }
}

/* ── The OTP flow ──────────────────────────────────────────────────────────── */

/// Where the two-screen sign-in stands.
@immutable
sealed class LoginState {
  const LoginState();
}

/// Asking for the number.
final class LoginPhoneEntry extends LoginState {
  const LoginPhoneEntry({this.sending = false, this.fault});

  final bool sending;
  final AppFault? fault;
}

/// Asking for the code.
final class LoginCodeEntry extends LoginState {
  const LoginCodeEntry({
    required this.challenge,
    required this.phone,
    this.verifying = false,
    this.fault,
  });

  final OtpChallenge challenge;

  /// E.164, so the screen can show the user which number they are waiting on.
  final String phone;

  final bool verifying;
  final AppFault? fault;
}

/// Verified. The router redirects off this.
final class LoginComplete extends LoginState {
  const LoginComplete();
}

final NotifierProvider<LoginController, LoginState> loginProvider =
    NotifierProvider<LoginController, LoginState>(LoginController.new);

/// Phone, then a six-digit code. No password anywhere.
///
/// India is phone-first: a password is a support ticket waiting to happen and one more thing for a
/// user to lose. The OTP is the primary factor here, not a second one.
class LoginController extends Notifier<LoginState> {
  @override
  LoginState build() => const LoginPhoneEntry();

  /// The national significant number length. **Not** a business rule the registry owns — it is the
  /// shape of an Indian mobile number, fixed by TRAI's numbering plan, and the same constant the
  /// server validates against.
  static const int nationalNumberLength = 10;

  static const String dialCode = '+91';

  /// Requests a code.
  ///
  /// Idempotency-keyed at the gesture like every other write: a double-tap on a flaky connection
  /// must send one SMS, not two — the second one costs money and reads to the user as a scam.
  Future<void> requestCode(String nationalNumber) async {
    final String digits = nationalNumber.replaceAll(RegExp(r'\D'), '');
    if (digits.length != nationalNumberLength) return;
    if (state is LoginPhoneEntry && (state as LoginPhoneEntry).sending) return;

    final String phone = '$dialCode$digits';
    state = const LoginPhoneEntry(sending: true);

    final WriteIntent gesture = WriteIntent.immediateNow(
      endpoint: '/auth/otp/request',
      method: WriteMethod.post,
      body: <String, dynamic>{'phone': phone},
    );

    try {
      final OtpChallenge challenge = await ref.read(customerApiProvider).requestOtp(
            OtpRequest(phone: phone, purpose: OtpPurpose.login),
            idempotencyKey: gesture.idempotencyKey,
          );
      state = LoginCodeEntry(challenge: challenge, phone: phone);
    } on Object catch (error) {
      state = LoginPhoneEntry(
        fault: ref.read(diagnosticsLogProvider.notifier).capture(error),
      );
    }
  }

  /// Sends the code again against the same number, minting a fresh challenge.
  Future<void> resend() async {
    final LoginState current = state;
    if (current is! LoginCodeEntry) return;
    final String national =
        current.phone.startsWith(dialCode) ? current.phone.substring(dialCode.length) : current.phone;
    state = const LoginPhoneEntry(sending: true);
    await requestCode(national);
  }

  /// Verifies, and adopts the session.
  ///
  /// **Never queued.** An OTP challenge expires in five minutes; a verification delivered after
  /// that is a request the server is right to refuse, and queueing it would leave a user staring at
  /// a screen that says nothing while a row waits in SQLite for a network.
  Future<void> verify(String code) async {
    final LoginState current = state;
    if (current is! LoginCodeEntry || current.verifying) return;

    final String digits = code.replaceAll(RegExp(r'\D'), '');
    state = LoginCodeEntry(
      challenge: current.challenge,
      phone: current.phone,
      verifying: true,
    );

    final WriteIntent gesture = WriteIntent.immediateNow(
      endpoint: '/auth/otp/verify',
      method: WriteMethod.post,
      body: <String, dynamic>{'challengeId': current.challenge.challengeId, 'code': digits},
    );

    try {
      final AuthSession session = await ref.read(customerApiProvider).verifyOtp(
            OtpVerifyRequest(challengeId: current.challenge.challengeId, code: digits),
            idempotencyKey: gesture.idempotencyKey,
          );
      await ref.read(sessionProvider.notifier).adopt(session);
      state = const LoginComplete();
    } on Object catch (error) {
      state = LoginCodeEntry(
        challenge: current.challenge,
        phone: current.phone,
        fault: ref.read(diagnosticsLogProvider.notifier).capture(error),
      );
    }
  }

  /// Back to the number. Clears the challenge, because a code sent to the old number is worthless
  /// against a new one.
  void changeNumber() => state = const LoginPhoneEntry();
}
