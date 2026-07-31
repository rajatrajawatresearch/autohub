import 'package:autohub_core/src/logic/json_logic.dart';
import 'package:flutter_test/flutter_test.dart';

/// The evaluator against the conditions the registries actually carry.
///
/// The rules below are lifted verbatim from three places that are already running: the `visibleWhen`
/// documents in `packages/api/src/mock/forms.ts`, the pricing conditions in
/// `backend/tests/AutoHub.Tests.Unit/Platform/PricingEngineTests.cs`, and the worked example in the
/// doc comment of `ConditionExpression.cs`. If any of them evaluates differently here, a field
/// appears on one client and not the other — which is the failure the whole registry architecture
/// exists to prevent.
void main() {
  group('the sell flow — why adding EVs needed no code', () {
    const rule = <String, Object?>{
      '==': <Object?>[
        <String, Object?>{'var': 'fuel_type'},
        'ev',
      ],
    };

    test('the battery field appears for an EV', () {
      expect(JsonLogic.evaluate(rule, <String, Object?>{'fuel_type': 'ev'}), isTrue);
    });

    test('and not for a petrol hatchback', () {
      expect(JsonLogic.evaluate(rule, <String, Object?>{'fuel_type': 'petrol'}), isFalse);
    });

    test('and not before the seller has answered the question at all', () {
      // A conditional field that flashes into view on first paint, before its trigger has a value,
      // reads as a glitch and gets a screenshot rather than a bug report.
      expect(JsonLogic.evaluate(rule, <String, Object?>{}), isFalse);
    });

    test('a boolean trigger compares as a boolean, not as a string', () {
      const accident = <String, Object?>{
        '==': <Object?>[
          <String, Object?>{'var': 'accident_history'},
          true,
        ],
      };

      expect(JsonLogic.evaluate(accident, <String, Object?>{'accident_history': true}), isTrue);
      expect(JsonLogic.evaluate(accident, <String, Object?>{'accident_history': false}), isFalse);
    });

    test('the loan application`s `in` against a list of employment types', () {
      const rule = <String, Object?>{
        'in': <Object?>[
          <String, Object?>{'var': 'employment_type'},
          <String>['self_employed', 'business'],
        ],
      };

      expect(
        JsonLogic.evaluate(rule, <String, Object?>{'employment_type': 'business'}),
        isTrue,
      );
      expect(
        JsonLogic.evaluate(rule, <String, Object?>{'employment_type': 'salaried'}),
        isFalse,
      );
    });
  });

  group('pricing — the worked example from ConditionExpression', () {
    // "auction commission applies to commercial vehicles in Maharashtra above ₹5,00,000"
    const rule = <String, Object?>{
      'and': <Object?>[
        <String, Object?>{
          '==': <Object?>[
            <String, Object?>{'var': 'vehicle.category'},
            'commercial',
          ],
        },
        <String, Object?>{
          '==': <Object?>[
            <String, Object?>{'var': 'location.state'},
            'MH',
          ],
        },
        <String, Object?>{
          '>': <Object?>[
            <String, Object?>{'var': 'deal.amount_minor'},
            50000000,
          ],
        },
      ],
    };

    Map<String, Object?> context({
      String category = 'commercial',
      String state = 'MH',
      int amountMinor = 84735000,
    }) =>
        <String, Object?>{
          'vehicle': <String, Object?>{'category': category},
          'location': <String, Object?>{'state': state},
          'deal': <String, Object?>{'amount_minor': amountMinor},
        };

    test('all three clauses true', () {
      expect(JsonLogic.evaluate(rule, context()), isTrue);
    });

    test('one clause false is the whole rule false', () {
      expect(JsonLogic.evaluate(rule, context(state: 'KA')), isFalse);
      expect(JsonLogic.evaluate(rule, context(category: 'car')), isFalse);
      expect(JsonLogic.evaluate(rule, context(amountMinor: 40000000)), isFalse);
    });

    test('a context missing the branch the rule reads does not apply the fee', () {
      // The safe direction for a fee, a permission and a state transition alike.
      expect(JsonLogic.evaluate(rule, <String, Object?>{}), isFalse);
    });

    test('the dealer tier condition reads a nested path', () {
      const tier = <String, Object?>{
        '==': <Object?>[
          <String, Object?>{'var': 'party.tier'},
          'gold',
        ],
      };

      expect(
        JsonLogic.evaluate(tier, <String, Object?>{
          'party': <String, Object?>{'tier': 'gold'},
        }),
        isTrue,
      );
      expect(
        JsonLogic.evaluate(tier, <String, Object?>{
          'party': <String, Object?>{'tier': 'silver'},
        }),
        isFalse,
      );
    });
  });

  group('truthiness follows JavaScript', () {
    test('empty string, zero, empty array and null are false', () {
      expect(JsonLogic.isTruthy(''), isFalse);
      expect(JsonLogic.isTruthy(0), isFalse);
      expect(JsonLogic.isTruthy(0.0), isFalse);
      expect(JsonLogic.isTruthy(<Object?>[]), isFalse);
      expect(JsonLogic.isTruthy(null), isFalse);
      expect(JsonLogic.isTruthy(false), isFalse);
    });

    test('an empty map is true, because a JS object always is', () {
      // Inverting this on mobile would make a rule authored against the web behave backwards.
      expect(JsonLogic.isTruthy(<String, Object?>{}), isTrue);
    });

    test('the string "0" is true — it is a non-empty string', () {
      expect(JsonLogic.isTruthy('0'), isTrue);
      expect(JsonLogic.isTruthy(<Object?>[0]), isTrue);
    });
  });

  group('equality', () {
    test('`==` coerces the way JavaScript does', () {
      expect(JsonLogic.evaluate(_op('==', <Object?>['1', 1]), null), isTrue);
      expect(JsonLogic.evaluate(_op('==', <Object?>[true, 1]), null), isTrue);
      expect(JsonLogic.evaluate(_op('==', <Object?>['', 0]), null), isTrue);
      expect(JsonLogic.evaluate(_op('==', <Object?>[null, 0]), null), isFalse);
      expect(JsonLogic.evaluate(_op('==', <Object?>[null, null]), null), isTrue);
    });

    test('`===` does not', () {
      expect(JsonLogic.evaluate(_op('===', <Object?>['1', 1]), null), isFalse);
      expect(JsonLogic.evaluate(_op('===', <Object?>[1, 1]), null), isTrue);
      expect(JsonLogic.evaluate(_op('!==', <Object?>['1', 1]), null), isTrue);
      expect(JsonLogic.evaluate(_op('!=', <Object?>['1', 1]), null), isFalse);
    });
  });

  group('comparison', () {
    test('numbers compare numerically, including numeric strings', () {
      expect(JsonLogic.evaluate(_op('>', <Object?>[40000, 1000]), null), isTrue);
      expect(JsonLogic.evaluate(_op('<=', <Object?>['40000', 40000]), null), isTrue);
      expect(JsonLogic.evaluate(_op('>=', <Object?>[2, 2]), null), isTrue);
    });

    test('a value that is not a number compares false in both directions', () {
      // JavaScript compares against NaN, and every NaN comparison is false. A rule that mistakenly
      // reads a text field must stop matching rather than throw on a screen mid-edit.
      expect(JsonLogic.evaluate(_op('>', <Object?>['diesel', 1000]), null), isFalse);
      expect(JsonLogic.evaluate(_op('<', <Object?>['diesel', 1000]), null), isFalse);
    });

    test('the three-argument between form', () {
      expect(JsonLogic.evaluate(_op('<', <Object?>[0, 38400, 500000]), null), isTrue);
      expect(JsonLogic.evaluate(_op('<', <Object?>[0, 600000, 500000]), null), isFalse);
    });
  });

  group('control flow', () {
    test('`if` picks a branch and falls through to the else', () {
      const rule = <String, Object?>{
        'if': <Object?>[
          <String, Object?>{'var': 'inspected'},
          'yes',
          'no',
        ],
      };

      expect(JsonLogic.apply(rule, <String, Object?>{'inspected': true}), 'yes');
      expect(JsonLogic.apply(rule, <String, Object?>{'inspected': false}), 'no');
    });

    test('`and` and `or` return the operand, JavaScript style', () {
      expect(JsonLogic.apply(_op('or', <Object?>['', 'fallback']), null), 'fallback');
      expect(JsonLogic.apply(_op('and', <Object?>['first', '']), null), '');
    });

    test('`!` and `!!` coerce to a boolean', () {
      expect(JsonLogic.evaluate(_op('!', <Object?>['']), null), isTrue);
      expect(JsonLogic.evaluate(_op('!!', <Object?>['petrol']), null), isTrue);
    });

    test('`missing` names the keys the payload has not answered', () {
      const rule = <String, Object?>{
        'missing': <Object?>['registration_number', 'kms_driven'],
      };

      expect(
        JsonLogic.apply(rule, <String, Object?>{'registration_number': 'MH12AB1234'}),
        <Object?>['kms_driven'],
      );
      expect(
        JsonLogic.apply(rule, <String, Object?>{
          'registration_number': 'MH12AB1234',
          'kms_driven': 38400,
        }),
        <Object?>[],
      );
    });

    test('`var` with a default, and the whole context for an empty path', () {
      expect(
        JsonLogic.apply(_op('var', <Object?>['city', 'pune']), <String, Object?>{}),
        'pune',
      );
      expect(
        JsonLogic.apply(_op('var', <Object?>['']), <String, Object?>{'a': 1}),
        <String, Object?>{'a': 1},
      );
    });

    test('`var` indexes into arrays', () {
      expect(
        JsonLogic.apply(
          _op('var', <Object?>['photos.1']),
          <String, Object?>{
            'photos': <String>['front', 'rear'],
          },
        ),
        'rear',
      );
    });
  });

  group('fail closed', () {
    test('an operator this build does not implement does not match', () {
      // A registry row authored against a newer client must cost one rule, not a fee applied to
      // every transaction on the platform.
      expect(JsonLogic.evaluate(_op('cat', <Object?>['a', 'b']), null), isFalse);
      expect(JsonLogic.tryEvaluate(_op('cat', <Object?>['a', 'b']), null), isNull);
    });

    test('a two-key object is an authoring mistake, not an operation', () {
      const ambiguous = <String, Object?>{
        '==': <Object?>[1, 1],
        '>': <Object?>[2, 1],
      };

      expect(JsonLogic.evaluate(ambiguous, null), isFalse);
      expect(JsonLogic.tryEvaluate(ambiguous, null), isNull);
    });

    test('a bare string or number as a condition does not match', () {
      // It would otherwise evaluate truthy and apply the rule to everything.
      expect(JsonLogic.evaluate('ev', null), isFalse);
      expect(JsonLogic.evaluate(1, null), isFalse);
      expect(JsonLogic.evaluate(null, null), isFalse);
    });

    test('a literal boolean is honoured, because it is unambiguous', () {
      expect(JsonLogic.evaluate(true, null), isTrue);
      expect(JsonLogic.evaluate(false, null), isFalse);
    });

    test('unevaluable and evaluated-false are distinguishable', () {
      // `autohub_forms` needs this: `visibleWhen` fails open on the web, and matching that
      // deliberately is only possible if the caller can tell the two apart.
      expect(JsonLogic.tryEvaluate(_op('==', <Object?>['a', 'b']), null), isFalse);
      expect(JsonLogic.tryEvaluate(_op('nope', <Object?>[]), null), isNull);
    });

    test('a rule nested past the depth limit does not match rather than overflowing', () {
      Object? rule = <String, Object?>{'!': <Object?>[true]};
      for (var i = 0; i < 200; i++) {
        rule = <String, Object?>{
          '!': <Object?>[rule],
        };
      }

      expect(JsonLogic.evaluate(rule, null), isFalse);
    });

    test('a rule decoded from real JSON, with its dynamic maps, still evaluates', () {
      // What comes back from `dart:convert` is Map<String, dynamic>, not Map<String, Object?>. If
      // the type test were written narrowly this would silently fail closed on every real document.
      final decoded = <String, dynamic>{
        '==': <dynamic>[
          <String, dynamic>{'var': 'fuel_type'},
          'ev',
        ],
      };

      expect(JsonLogic.evaluate(decoded, <String, dynamic>{'fuel_type': 'ev'}), isTrue);
    });
  });
}

Map<String, Object?> _op(String operator, List<Object?> args) =>
    <String, Object?>{operator: args};
