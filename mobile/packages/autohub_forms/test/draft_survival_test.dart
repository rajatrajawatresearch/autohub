import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures/sell_vehicle.dart';

/// Losing a half-finished sell form is losing an hour of somebody's day.
///
/// `docs/10-mobile-architecture.md` §4 is unambiguous about the requirement: every schema form
/// autosaves on each field change, keyed by form key plus entity id, and is recovered on relaunch
/// **without asking**. This file is that requirement, checked against `autohub_core`'s real
/// SQLite-backed store rather than a stub — the interesting failures here are about what is
/// persisted and what is not, and a fake would agree with whatever the code happened to do.
void main() {
  final FormDefinition sellVehicle = FormDefinition.fromJson(sellVehicleJson);

  late AhDatabase database;
  late DraftStore drafts;

  setUp(() {
    database = openInMemoryAhDatabase();
    drafts = DraftStore(database);
  });

  tearDown(() async {
    await database.close();
  });

  Future<void> noSubmit(FormValues _) async {}

  SchemaFormController controllerFor({FormValues? initialValues, String? entityId}) {
    final SchemaFormController controller = SchemaFormController(
      definition: sellVehicle,
      onSubmit: noSubmit,
      initialValues: initialValues,
      draftStore: drafts,
      draftEntityId: entityId,
      // The debounce is `autohub_core`'s and it is tested there. Here it only gets in the way, and
      // `saveDraft()` is the same path a backgrounded app takes.
      autosaveWindow: Duration.zero,
    );
    addTearDown(controller.dispose);
    return controller;
  }

  test('a form recovered on relaunch has what the seller typed', () async {
    final SchemaFormController first = controllerFor();
    first
      ..setValue('registration_number', 'MH12AB1234')
      ..setValue('fuel_type', 'ev')
      ..setValue('battery_health_pct', 88);
    await first.saveDraft();

    // A different process, a different controller, the same phone.
    final SchemaFormController second = controllerFor();
    expect(second.valueOf('registration_number'), isNull);
    await second.restoreDraft();

    expect(second.valueOf('registration_number'), 'MH12AB1234');
    expect(second.valueOf('battery_health_pct'), 88);
    // Recovered without asking. A dialogue offering to restore work the user did not know they had
    // lost is one they dismiss by reflex, and the twenty minutes goes with it.
    expect(second.draftRestored, isTrue);
  });

  test('a hidden field keeps its value in the draft and stays out of the payload', () async {
    final SchemaFormController controller = controllerFor();
    controller
      ..setValue('fuel_type', 'ev')
      ..setValue('battery_health_pct', 88)
      // The seller corrects themselves. The battery figure is now invisible — but not gone.
      ..setValue('fuel_type', 'petrol');
    await controller.saveDraft();

    final FormDraft? saved = await drafts.recover(formKey: 'sell_vehicle');
    // Switching back to `ev` must find the figure still there, so the draft keeps it…
    expect(saved!.values['battery_health_pct'], 88);
    // …and the server must never see it for a petrol car, so the payload does not.
    expect(controller.payload().containsKey('battery_health_pct'), isFalse);
  });

  test('two cars do not overwrite each other', () async {
    final SchemaFormController swift = controllerFor(entityId: 'listing-a');
    swift.setValue('registration_number', 'MH12AB1234');
    await swift.saveDraft();

    final SchemaFormController thar = controllerFor(entityId: 'listing-b');
    thar.setValue('registration_number', 'MH14CD5678');
    await thar.saveDraft();

    final SchemaFormController reopened = controllerFor(entityId: 'listing-a');
    await reopened.restoreDraft();
    expect(reopened.valueOf('registration_number'), 'MH12AB1234');
  });

  test('a draft from an older schema is kept, and its gaps are surfaced immediately', () async {
    // Twenty minutes of typing is worth more than a clean payload, so a schema publish between the
    // save and the restore does not discard the draft. What it does do is stop waiting for the user
    // to touch each field before showing what is now missing — otherwise a newly-required field
    // becomes a 422 at the end of the flow that the seller cannot explain.
    await drafts.record(
      formKey: 'sell_vehicle',
      values: <String, dynamic>{'registration_number': 'MH12AB1234'},
      schemaVersion: sellVehicle.version - 1,
    );

    final SchemaFormController controller = controllerFor();
    await controller.restoreDraft();

    expect(controller.valueOf('registration_number'), 'MH12AB1234');
    expect(controller.draftWasStale, isTrue);
    expect(controller.faultFor('city')?.code, 'city.required');
  });

  test('a key the definition no longer declares is not resurrected', () async {
    // A field removed from the schema must not reach the server, where an unknown property fails
    // the whole submission for a reason nobody on the screen can see.
    await drafts.record(
      formKey: 'sell_vehicle',
      values: <String, dynamic>{
        'registration_number': 'MH12AB1234',
        'insurance_provider': 'a field that used to exist',
      },
      schemaVersion: sellVehicle.version,
    );

    final SchemaFormController controller = controllerFor();
    await controller.restoreDraft();

    expect(controller.valueOf('registration_number'), 'MH12AB1234');
    expect(controller.valueOf('insurance_provider'), isNull);
    expect(controller.payload().containsKey('insurance_provider'), isFalse);
  });

  test('the draft outlives a submission that failed', () async {
    final SchemaFormController controller = SchemaFormController(
      definition: sellVehicle,
      onSubmit: (FormValues _) async =>
          throw const FormSubmissionRejected(message: 'The gateway timed out.'),
      initialValues: <String, Object?>{
        'registration_number': 'MH12AB1234',
        'city': 'pune',
        'fuel_type': 'petrol',
        'kms_driven': 38400,
        'owners': '1',
        'condition': 'good',
        'photos': <Object?>[1, 2, 3, 4, 5, 6],
        'phone': '+919812345678',
      },
      draftStore: drafts,
      autosaveWindow: Duration.zero,
    );
    addTearDown(controller.dispose);

    await controller.saveDraft();
    expect(await controller.submit(), isFalse);
    expect(controller.submitError, 'The gateway timed out.');

    // Until the caller knows the server accepted the write, the draft is the only copy of what the
    // user typed. Discarding it on `submit()` would mean a poisoned write loses the whole form.
    expect(await drafts.recover(formKey: 'sell_vehicle'), isNotNull);

    await controller.clearDraft();
    expect(await drafts.recover(formKey: 'sell_vehicle'), isNull);
  });
}
