import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_customer/src/data/sell.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures/registry.dart';
import 'harness.dart';

/// The write that creates supply, and the two ways it can go wrong that cost real money.
///
/// **Duplicate listings.** A seller on a train taps submit, the request leaves as the carriage
/// enters a tunnel, and the socket dies somewhere between the phone and the server. Neither the
/// client nor the user can tell "the server never saw it" from "the listing was created and the
/// response was lost". If the retry carries a fresh `Idempotency-Key`, the seller ends up with two
/// listings for one car — and on the payment paths, the same shape of bug is two debits. The whole
/// defence is one line: the key is minted on the gesture, so the replay is byte-identical.
///
/// **Lost work.** A `422` must come back to the *form*, with its field errors, while the seller is
/// still standing in front of it — not twenty minutes later as a poisoned row nobody is looking at,
/// with the codes discarded.
void main() {
  const FormValues payload = <String, Object?>{
    'registration_number': 'MH12AB1234',
    'kms_driven': 38400,
  };

  Future<TestRig> makeRig() async {
    final TestRig rig = await TestRig.create(
      extraOverrides: <Override>[
        // The real curve is fifteen seconds after an outage, which is right on a handset and
        // pointless in a test. Nothing else about the retry path is stubbed.
        backoffPolicyProvider.overrideWithValue(const BackoffPolicy(outageRetry: Duration.zero)),
      ],
    );
    addTearDown(rig.database.close);
    addTearDown(rig.container.dispose);
    return rig;
  }

  test('a submit that dies in a tunnel is queued, and drains as the same act', () async {
    final TestRig rig = await makeRig();
    // Nothing reaches a server: the first attempt and the drain that follows it both fail.
    rig.outbox.fallback = const WriteUnreachable(reason: ApiErrorCodes.unreachable);

    await rig.container.read(sellProvider.notifier).submit(payload);
    // Enqueuing nudges the drain loop rather than waiting for its sweep. That nudge is in flight
    // here, and `drain()` returns the running pass rather than starting a new one — so the test has
    // to let it finish failing before it can change what the network does.
    await settleEventLoop();

    expect(
      rig.container.read(sellProvider),
      isA<SellQueued>(),
      reason: 'the seller must be told it is on their phone, not that it is submitted',
    );

    final String gestureKey = rig.outbox.sent.first.idempotencyKey;
    final List<OutboxEntry> queued = await rig.container.read(outboxProvider).all();
    expect(queued, hasLength(1));
    expect(queued.single.idempotencyKey, gestureKey);

    // The tunnel ends.
    rig.outbox.fallback = const WriteAccepted(
      statusCode: 201,
      body: <String, dynamic>{'listingId': '019fab0c-listing'},
    );
    await rig.container.read(outboxDrainerProvider).drain();
    await settleEventLoop();

    // Every attempt — the one that died and every replay — carried the key the gesture was born
    // with. This is the assertion that stands between a seller and two listings for one car.
    expect(rig.outbox.sent.length, greaterThan(1));
    expect(
      rig.outbox.sent.map((WriteIntent intent) => intent.idempotencyKey).toSet(),
      <String>{gestureKey},
    );

    expect(rig.container.read(sellProvider), isA<SellSubmitted>());
    expect((rig.container.read(sellProvider) as SellSubmitted).listingId, '019fab0c-listing');
  });

  test('the draft outlives the queue and is discarded only once the server has it', () async {
    final TestRig rig = await makeRig();
    final DraftStore drafts = rig.container.read(draftStoreProvider);
    await drafts.record(formKey: sellFormKey, values: payload, schemaVersion: 7);

    rig.outbox.fallback = const WriteUnreachable(reason: ApiErrorCodes.unreachable);
    await rig.container.read(sellProvider.notifier).submit(payload);
    await settleEventLoop();

    // Until the server owns the data, the draft is the only copy of what the user typed. A
    // poisoned write with nothing behind it is the twenty minutes gone.
    expect(await drafts.recover(formKey: sellFormKey), isNotNull);

    rig.outbox.fallback = const WriteAccepted(statusCode: 201);
    await rig.container.read(outboxDrainerProvider).drain();
    await settleEventLoop();

    expect(await drafts.recover(formKey: sellFormKey), isNull);
  });

  test('a 422 goes back to the form, and is never queued', () async {
    final TestRig rig = await makeRig();
    rig.outbox.fallback = const WriteRejected(
      statusCode: 422,
      code: 'validation.failed',
      fieldErrors: <String, List<String>>{
        'kms_driven': <String>['kms.too_high'],
      },
    );

    Object? thrown;
    try {
      await rig.container.read(sellProvider.notifier).submit(payload);
    } on Object catch (error) {
      thrown = error;
    }

    expect(thrown, isA<FormSubmissionRejected>());

    // The renderer resolves the server's code against the field's own authored rule, so a fault the
    // server raised is indistinguishable from one the phone raised — same sentence, same code.
    final FormDefinition definition = FormDefinition.fromJson(sellFormJson);
    final Map<String, FieldFault> faults = faultsFromProblem(
      definition,
      (thrown! as FormSubmissionRejected).problem,
    );
    expect(faults['kms_driven']?.code, 'kms.too_high');
    expect(faults['kms_driven']?.message, 'Enter the odometer reading in kilometres.');

    // Retrying a `422` produces the same `422` at every backoff step, having spent a commuter's
    // battery to learn nothing.
    expect(await rig.container.read(outboxProvider).all(), isEmpty);
    expect(rig.container.read(sellProvider), isA<SellComposing>());
  });

  test('the write carries the city the seller was in when they acted', () async {
    final TestRig rig = await makeRig();
    await rig.container.read(devicePreferencesProvider).setActiveCity('pune');
    // Let the city stream reach the provider the controller reads.
    await rig.container.read(activeCityProvider.future);

    await rig.container.read(sellProvider.notifier).submit(payload);

    // Frozen at the gesture. A listing composed in Pune and drained after the user switched to
    // Mumbai belongs to Pune.
    expect(rig.outbox.sent.single.headers['X-Active-City'], 'pune');
  });
}
