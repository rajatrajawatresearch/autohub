import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// A registry row this build has never heard of must cost one field, not the screen.
///
/// This is the failure mode mobile has and the web does not. The registries are versioned and
/// edited by people who are not shipping this binary; sooner or later a form schema will name a
/// control that shipped after the phone in somebody's hand, and on Android "however long until the
/// user updates" is frequently "never". The sell flow is the one screen in the product that creates
/// supply, so it degrades rather than crashes.
void main() {
  /// A document from a registry that is one publish ahead of this build: a control nobody here has
  /// implemented, sitting between two that everybody has.
  final FormDefinition aheadOfUs = FormDefinition.fromJson(<String, Object?>{
    'key': 'sell_vehicle',
    'version': 9,
    'title': 'Tell us about your car',
    'submitLabel': 'Get my price',
    'steps': <Object?>[
      <String, Object?>{
        'key': 'identity',
        'title': 'Your car',
        'fields': <Object?>[
          <String, Object?>{
            'key': 'kms_driven',
            'label': 'Kms driven',
            'control': 'number',
            'unit': 'km',
            'required': true,
          },
          <String, Object?>{
            'key': 'battery_chemistry_scan',
            'label': 'Battery chemistry',
            'control': 'holographic_scan',
            'required': true,
            'helpText': 'Point the phone at the pack.',
          },
          <String, Object?>{
            'key': 'condition',
            'label': 'Overall condition',
            'control': 'radio',
            'required': true,
            'options': <Object?>[
              <String, Object?>{'value': 'good', 'label': 'Good'},
            ],
          },
        ],
      },
    ],
  });

  Future<void> noSubmit(FormValues _) async {}

  test('a control name this build has never heard of decodes to the placeholder', () {
    // `autohub_core` does the degradation at the decode boundary, so a registry running ahead of a
    // deployed binary never reaches the renderer as an unknown string.
    expect(aheadOfUs.fieldFor('battery_chemistry_scan')!.control, FormControl.unsupported);
    expect(isSupportedControl(FormControl.unsupported), isFalse);
    expect(isSupportedControl(FormControl.photoSet), isTrue);
    // The lookup is a function rather than a map subscript precisely so there is nowhere for a
    // caller to write `!`.
    expect(resolveControl(FormControl.unsupported), isNotNull);
  });

  test('every control in the contract vocabulary is implemented', () {
    // Straight from `docs/contracts/registry-api.md` §2. A control listed in the contract and
    // missing here would silently render as a placeholder in production instead of failing a build.
    final Set<FormControl> contract = FormControl.values.toSet()
      ..remove(FormControl.unsupported);
    expect(supportedControls.toSet(), contract);
  });

  testWidgets('an unknown control renders a placeholder and leaves the step standing',
      (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AhTheme.light(),
        home: Scaffold(
          body: SingleChildScrollView(
            child: SchemaForm(definition: aheadOfUs, onSubmit: noSubmit),
          ),
        ),
      ),
    );

    expect(tester.takeException(), isNull);

    // The placeholder is visible and names the field and the control, which is exactly the string
    // support needs read back off a screenshot.
    expect(find.text('FIELD UNAVAILABLE'), findsOneWidget);
    expect(find.text('battery_chemistry_scan · unsupported'), findsOneWidget);

    // And the fields on either side of it still rendered. Losing one question is the whole cost.
    expect(find.text('KMS DRIVEN'), findsOneWidget);
    expect(find.text('OVERALL CONDITION'), findsOneWidget);
    expect(find.text('Good'), findsOneWidget);
  });

  testWidgets('a required unknown control does not trap the user on the step',
      (WidgetTester tester) async {
    late SchemaFormController controller;
    await tester.pumpWidget(
      MaterialApp(
        theme: AhTheme.light(),
        home: Scaffold(
          body: SingleChildScrollView(
            child: SchemaForm(
              definition: aheadOfUs,
              onSubmit: noSubmit,
              initialValues: <String, Object?>{'kms_driven': 38400, 'condition': 'good'},
              onControllerCreated: (SchemaFormController created) => controller = created,
            ),
          ),
        ),
      ),
    );

    // The field is `required: true` and cannot be answered, so validation *does* stop the submit —
    // which is correct, because the server would reject the payload too. What must not happen is a
    // crash, or a silent submission missing a field the registry declared mandatory.
    expect(await controller.submit(), isFalse);
    expect(controller.faultFor('battery_chemistry_scan')?.code, 'battery_chemistry_scan.required');
    expect(tester.takeException(), isNull);
  });
}
