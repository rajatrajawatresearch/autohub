import 'package:autohub_core/autohub_core.dart';
import 'package:autohub_forms/autohub_forms.dart';
import 'package:flutter_test/flutter_test.dart';

/// `visibleWhen`, at the edges.
///
/// The EV case is covered against the real document in `sell_vehicle_conditional_test.dart`. These
/// are the cases a registry will produce eventually and nobody will have looked at: a whole step
/// that turns out to be empty, a rule written with an operator, and a row that is simply wrong.
void main() {
  FormDefinition documentWith(List<Map<String, Object?>> steps) =>
      FormDefinition.fromJson(<String, Object?>{
        'key': 'loan_application',
        'version': 5,
        'title': 'Apply for finance',
        'steps': steps,
      });

  test('a step whose every field is hidden is itself hidden', () {
    // Otherwise an EV-only step shows up empty for a petrol car and dead-ends the user on a page
    // with a Continue button and nothing to fill in.
    final FormDefinition definition = documentWith(<Map<String, Object?>>[
      <String, Object?>{
        'key': 'applicant',
        'title': 'About you',
        'fields': <Object?>[
          <String, Object?>{'key': 'employment_type', 'label': 'Employment', 'control': 'radio'},
        ],
      },
      <String, Object?>{
        'key': 'business',
        'title': 'Your business',
        'fields': <Object?>[
          <String, Object?>{
            'key': 'business_vintage_years',
            'label': 'Years in business',
            'control': 'number',
            'visibleWhen': <String, Object?>{
              'in': <Object?>[
                <String, Object?>{'var': 'employment_type'},
                <Object?>['self_employed', 'business'],
              ],
            },
          },
        ],
      },
    ]);

    expect(visibleSteps(definition, <String, Object?>{'employment_type': 'salaried'}).length, 1);
    expect(
      visibleSteps(definition, <String, Object?>{'employment_type': 'self_employed'}).length,
      2,
    );
  });

  test('a step-level rule hides the whole step', () {
    final FormDefinition definition = documentWith(<Map<String, Object?>>[
      <String, Object?>{
        'key': 'permit',
        'title': 'Commercial permit',
        'visibleWhen': <String, Object?>{
          '==': <Object?>[
            <String, Object?>{'var': 'intent'},
            'commercial',
          ],
        },
        'fields': <Object?>[
          <String, Object?>{'key': 'permit_type', 'label': 'Permit', 'control': 'select'},
        ],
      },
    ]);

    expect(visibleSteps(definition, <String, Object?>{'intent': 'sale'}), isEmpty);
    expect(visibleSteps(definition, <String, Object?>{'intent': 'commercial'}).length, 1);
    expect(
      stripHidden(definition, <String, Object?>{'intent': 'sale', 'permit_type': 'contract'}),
      isEmpty,
    );
  });

  test('a rule the evaluator cannot make sense of leaves the field visible', () {
    // The one place in this workspace that fails open, and the reason is asymmetric: showing a
    // field that should have been conditional costs one confusing question and somebody notices;
    // hiding one silently deletes a question from the flow *and* a key from the payload, and
    // nobody ever sees an error.
    expect(
      isVisible(<String, Object?>{'wormhole': <Object?>[1, 2]}, <String, Object?>{}),
      isTrue,
    );
    expect(isVisible(<String, Object?>{}, <String, Object?>{}), isTrue);
    expect(isVisible(null, <String, Object?>{}), isTrue);
  });

  test('an unanswered question is not the same as a false one', () {
    const Map<String, Object?> rule = <String, Object?>{
      '==': <Object?>[
        <String, Object?>{'var': 'accident_history'},
        true,
      ],
    };
    expect(isVisible(rule, <String, Object?>{}), isFalse);
    expect(isVisible(rule, <String, Object?>{'accident_history': false}), isFalse);
    expect(isVisible(rule, <String, Object?>{'accident_history': true}), isTrue);
  });
}
