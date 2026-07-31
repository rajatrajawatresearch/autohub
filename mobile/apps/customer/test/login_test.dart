import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_customer/src/app/router.dart';
import 'package:autohub_customer/src/data/session.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'harness.dart';

/// Two screens, no password.
///
/// India is phone-first: a password is one more thing for a user to lose and a support ticket
/// waiting to happen, so the OTP is the primary factor rather than a second one. The assertions
/// worth having are the ones about the session, because getting them wrong means either a user who
/// cannot stay signed in or a token that outlives the sign-out.
void main() {
  const AuthUser user = AuthUser(id: 'user-1', displayName: 'Rajat', phone: '+919812345678');

  const AuthSession session = AuthSession(
    accessToken: 'access-1',
    refreshToken: 'refresh-1',
    user: user,
  );

  Future<void> openLogin(WidgetTester tester, TestRig rig) async {
    await pumpApp(tester, rig);
    rig.container.read(routerProvider).push(Routes.login);
    await tester.pumpAndSettle();
  }

  appTest('a number, then a code, and the session is on the keychain',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api
      ..challenge = const OtpChallenge(
        challengeId: 'chal-1',
        resendAfterSeconds: 30,
        maskedTo: '+91 98120 •••78',
      )
      ..session = session
      ..user = user;

    await openLogin(tester, rig);

    // Step one: the number, with the dial code shown rather than typed — a user who half-enters
    // `+91` produces eleven digits the server is right to reject.
    expect(find.text('Sign in'), findsWidgets);
    expect(find.text('+91'.toUpperCase()), findsOneWidget);

    await tester.enterText(find.byType(TextField).first, '9812345678');
    await tester.pump();
    await tester.tap(find.text('Send code'));
    await tester.pumpAndSettle();

    // Step two, on the same route: a code screen reached by deep link with no challenge behind it
    // would be a dead end.
    expect(find.text('Enter the code'), findsOneWidget);
    expect(find.textContaining('+91 98120 •••78'), findsOneWidget);

    // Six digits submits itself. The whole screen's job is six characters, and making the user
    // find a button afterwards is one tap too many.
    await tester.enterText(find.byType(TextField).first, '482915');
    await tester.pumpAndSettle();

    expect(rig.container.read(sessionProvider), isA<SignedIn>());
    // Written through to the store the transport reads synchronously on every request — not held
    // in a second copy that goes stale the first time a refresh rotates.
    expect(rig.tokens.tokens?.accessToken, 'access-1');
    expect(rig.tokens.tokens?.refreshToken, 'refresh-1');
  });

  appTest('a wrong code keeps the user on the code screen with the problem stated',
      (WidgetTester tester, TestRig rig) async {
    useHandsetViewport(tester);
    rig.api.challenge = const OtpChallenge(challengeId: 'chal-1');

    await openLogin(tester, rig);
    await tester.enterText(find.byType(TextField).first, '9812345678');
    await tester.pump();
    await tester.tap(find.text('Send code'));
    await tester.pumpAndSettle();

    rig.api.authError = const ApiException(
      statusCode: 422,
      code: 'identity.otp.mismatch',
      message: 'That code did not match. Check it, or ask for a new one.',
    );

    await tester.enterText(find.byType(TextField).first, '111111');
    await tester.pumpAndSettle();

    expect(find.text('Enter the code'), findsOneWidget);
    expect(find.text('That code did not match. Check it, or ask for a new one.'), findsOneWidget);
    expect(find.textContaining('identity.otp.mismatch'), findsNothing);
    expect(rig.container.read(sessionProvider), isNot(isA<SignedIn>()));
  });

  test('one gesture is one SMS', () async {
    final TestRig rig = await TestRig.create();
    addTearDown(rig.database.close);
    addTearDown(rig.container.dispose);

    final LoginController controller = rig.container.read(loginProvider.notifier);

    // A thumb that bounces on a slow connection, expressed without a widget in the way: two
    // requests would be two messages, and the second one costs money and reads to the user as a
    // scam. Asserted on the controller because a widget test cannot pump between two taps without
    // also giving the first request time to land, which is a different scenario.
    await Future.wait<void>(<Future<void>>[
      controller.requestCode('9812345678'),
      controller.requestCode('9812345678'),
    ]);

    expect(rig.api.idempotencyKeys, hasLength(1));
  });
}
