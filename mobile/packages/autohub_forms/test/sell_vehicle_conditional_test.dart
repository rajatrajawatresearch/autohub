import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fixtures/sell_vehicle.dart';

/// The test the whole workspace exists to pass.
///
/// `docs/10-mobile-architecture.md` §1 states the success criterion as a single sentence: an
/// operations user adds `battery_health_pct` to the EV category and the field appears in the
/// Flutter sell flow, with its validation and its conditional visibility, with nothing rebuilt.
///
/// So these assertions are against the **real** `sell_vehicle` document, parsed from the same JSON
/// the web renders and the server validates. Nothing here constructs a field in Dart. If any of it
/// fails, the mobile app is a second codebase pretending to be a client and every registry in the
/// platform is decoration.
void main() {
  final FormDefinition sellVehicle = FormDefinition.fromJson(sellVehicleJson);

  Future<void> noSubmit(FormValues _) async {}

  group('the EV-conditional field', () {
    test('is absent from the payload when the car is not an EV', () {
      final SchemaFormController controller = SchemaFormController(
        definition: sellVehicle,
        onSubmit: noSubmit,
        initialValues: <String, Object?>{
          'registration_number': 'MH12AB1234',
          'city': 'pune',
          'fuel_type': 'petrol',
          'kms_driven': 38400,
          'owners': '1',
          // Deliberately present in the values. A seller who answered "ev", filled the slider and
          // then corrected themselves to "petrol" leaves this behind, and it must not reach the
          // server describing a car that does not have a battery.
          'battery_health_pct': 88,
        },
      );
      addTearDown(controller.dispose);

      expect(controller.payload().containsKey('battery_health_pct'), isFalse);
      expect(controller.payload()['fuel_type'], 'petrol');
    });

    test('is in the payload when the car is an EV', () {
      final SchemaFormController controller = SchemaFormController(
        definition: sellVehicle,
        onSubmit: noSubmit,
        initialValues: <String, Object?>{
          'fuel_type': 'ev',
          'battery_health_pct': 88,
        },
      );
      addTearDown(controller.dispose);

      expect(controller.payload()['battery_health_pct'], 88);
    });

    test('is not validated for a diesel, and is required for an EV', () {
      // A required EV field must never block a diesel hatchback. This is the half of conditional
      // visibility that is easy to forget and produces a form the user cannot submit and cannot
      // see why.
      final FormValidation diesel = validateForm(
        sellVehicle,
        <String, Object?>{'fuel_type': 'diesel'},
      );
      expect(diesel.fieldErrors.containsKey('battery_health_pct'), isFalse);

      final FormValidation electric = validateForm(
        sellVehicle,
        <String, Object?>{'fuel_type': 'ev'},
      );
      expect(electric.fieldErrors['battery_health_pct']?.code, 'battery_health_pct.required');
    });

    test('keeps its value in the draft even while hidden', () {
      // The draft and the payload disagree on purpose. Switching to petrol and back to `ev` must
      // find the battery figure still there; the server must never see it for a petrol car.
      final FormValues values = <String, Object?>{
        'fuel_type': 'petrol',
        'battery_health_pct': 88,
      };
      expect(stripHidden(sellVehicle, values).containsKey('battery_health_pct'), isFalse);
      expect(values['battery_health_pct'], 88);
    });
  });

  group('the rendered sell flow', () {
    late SchemaFormController controller;

    Widget host(FormValues initialValues) => MaterialApp(
          theme: AhTheme.light(),
          home: Scaffold(
            body: SingleChildScrollView(
              child: SchemaForm(
                definition: sellVehicle,
                initialValues: initialValues,
                onSubmit: noSubmit,
                onControllerCreated: (SchemaFormController created) => controller = created,
              ),
            ),
          ),
        );

    /// The sell flow is a tall document and the default 800x600 test surface is not a phone. A
    /// realistic viewport keeps the footer inside the hit-testable area, so a tap in a test is a
    /// tap a user could have made.
    void useHandsetViewport(WidgetTester tester) {
      tester.view
        ..physicalSize = const Size(1080, 3200)
        ..devicePixelRatio = 3;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
    }

    testWidgets('draws the battery field the moment the seller says EV',
        (WidgetTester tester) async {
      useHandsetViewport(tester);
      await tester.pumpWidget(host(<String, Object?>{'fuel_type': 'petrol'}));
      expect(find.text('BATTERY HEALTH'), findsNothing);

      // Exactly what answering the fuel question does. No rebuild, no new definition, no release —
      // the field is in the document and the document decides.
      controller.setValue('fuel_type', 'ev');
      await tester.pump();

      // The caption atom uppercases; the registry authored "Battery health".
      expect(find.text('BATTERY HEALTH'), findsOneWidget);
      // Its unit came from the registry too, not from anything in this package.
      expect(find.text('%'), findsWidgets);
      // And the help text the operations user wrote came with it.
      expect(find.text('From the vehicle display, or from the last service report.'), findsOneWidget);

      controller.setValue('fuel_type', 'petrol');
      await tester.pump();
      expect(find.text('BATTERY HEALTH'), findsNothing);
    });

    testWidgets('renders the registry title, step and submit label', (WidgetTester tester) async {
      useHandsetViewport(tester);
      await tester.pumpWidget(host(<String, Object?>{}));

      expect(find.text('Tell us about your car'), findsOneWidget);
      expect(find.text('Your car'), findsOneWidget);
      // Three steps, so the first action is Continue and the authored submit label waits.
      expect(find.text('Continue'), findsOneWidget);
      expect(find.text('Get my price'), findsNothing);
      // The schema's identity is always on screen, so a bug report names a version.
      expect(find.text('sell_vehicle · v7'), findsOneWidget);
    });

    testWidgets('refuses to advance past an unanswered required step', (WidgetTester tester) async {
      useHandsetViewport(tester);
      await tester.pumpWidget(host(<String, Object?>{}));

      await tester.tap(find.text('Continue'));
      await tester.pump();

      expect(controller.stepIndex, 0);
      // The gate reveals the faults rather than moving. The message is the registry's sentence and
      // the code beside it is the registry's code.
      expect(find.text('Pick the city the car is in.'), findsOneWidget);
      expect(find.text('city.required'), findsOneWidget);
    });
  });
}
