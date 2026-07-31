/// A JsonLogic evaluator, written here rather than taken from pub.dev.
///
/// The reason is behavioural parity, not pride. This is the single mechanism by which business
/// conditions enter the platform without entering the codebase — a form field's `visibleWhen`, a
/// pricing rule's applicability, a workflow guard, a notification's audience filter are all one
/// JsonLogic document evaluated against one context document. Three runtimes evaluate them:
/// `json-logic-js` on the web, `Json.Logic` in
/// `backend/src/AutoHub.Platform/Expressions/ConditionExpression.cs`, and this file on handsets.
///
/// A pub.dev package would be a fourth semantic, chosen by somebody who was not in the room, and
/// the disagreement would surface as: the EV battery-health field appears on the web and not on
/// Android. That is precisely the failure this architecture exists to prevent, and on a phone it
/// cannot be hot-fixed — a stale binary sits on handsets until users update.
///
/// Two semantics are therefore copied deliberately from `ConditionExpression`:
///
/// **JavaScript truthiness.** Empty string, `0`, empty array and `null` are false; an empty *map*
/// is true, because a JS object always is. See [isTruthy].
///
/// **Fail closed.** A rule that cannot be parsed or cannot be evaluated does **not** match. For a
/// fee, a permission and a state transition alike, the safe direction of a broken rule is "does not
/// apply". A rule that silently evaluated truthy would apply a commission to every transaction on
/// the platform.
///
/// The operator set is the set the registries actually use. It has no loops, no function
/// definitions and no I/O, which is why an operations user authoring a rule in the admin console
/// cannot hang a request or read a file. The expressiveness ceiling is the feature.
library;

/// Operators this evaluator implements. Anything outside it is unevaluable, and therefore false.
const Set<String> kJsonLogicOperators = <String>{
  '==',
  '===',
  '!=',
  '!==',
  '>',
  '>=',
  '<',
  '<=',
  'and',
  'or',
  '!',
  '!!',
  'var',
  'in',
  'if',
  'missing',
};

/// Raised internally the moment a rule stops being evaluable, so the failure cannot be mistaken for
/// a legitimate falsy result on the way back up the recursion.
final class _UnevaluableRule implements Exception {
  const _UnevaluableRule();

  @override
  String toString() => 'JsonLogic: the rule could not be evaluated.';
}

abstract final class JsonLogic {
  /// Guards against a registry row that nests itself into a stack overflow. A rule this deep is an
  /// authoring accident or an attack; either way the answer is "does not apply", not a crash on a
  /// screen the user cannot leave.
  static const int _maxDepth = 64;

  /// Evaluates a rule to a boolean, failing closed.
  ///
  /// `null` is **not** treated as "always". `ConditionExpression` models an absent condition as a
  /// separate, explicit `Always`, so that "no condition" and "a condition that failed to parse" can
  /// never be confused — one applies, the other must not. The same distinction is kept here: an
  /// absent rule is the caller's decision, and callers that want it must say so.
  static bool evaluate(Object? rule, Object? data) => tryEvaluate(rule, data) ?? false;

  /// The same evaluation, but `null` distinguishes "could not be evaluated" from "evaluated false".
  ///
  /// `autohub_forms` needs this distinction. `packages/forms/src/visibility.ts` deliberately fails
  /// *open* for `visibleWhen` — a malformed registry row costs a user one confusing field rather
  /// than the whole sell flow — and matching that behaviour is only possible if the caller can see
  /// which of the two happened. Everything with money or authorisation in it uses [evaluate].
  static bool? tryEvaluate(Object? rule, Object? data) {
    // A bare string or number as a top-level condition is almost always an authoring mistake that
    // would otherwise evaluate truthy and apply a rule to everything.
    if (rule is bool) return rule;
    if (rule is! Map<Object?, Object?>) return null;
    try {
      return isTruthy(_apply(rule, data, 0));
    } on _UnevaluableRule {
      return null;
    }
  }

  /// Evaluates a rule to its raw value rather than to a boolean — `var` lookups, `if` branches and
  /// `missing` lists all return something other than a bool. Throws nothing: unevaluable is `null`.
  static Object? apply(Object? rule, Object? data) {
    try {
      return _apply(rule, data, 0);
    } on _UnevaluableRule {
      return null;
    }
  }

  /// JsonLogic truthiness, which follows JavaScript.
  ///
  /// Implemented here rather than inferred at call sites so every registry agrees on what `0` and
  /// `""` mean. Note the last case: a map is truthy even when empty, because `{}` is truthy in JS
  /// and a rule authored against the web's behaviour must not invert on a phone.
  static bool isTruthy(Object? value) => switch (value) {
        null => false,
        final bool flag => flag,
        final num number => number != 0 && !number.isNaN,
        final String text => text.isNotEmpty,
        final List<Object?> items => items.isNotEmpty,
        _ => true,
      };

  static Object? _apply(Object? rule, Object? data, int depth) {
    if (depth > _maxDepth) throw const _UnevaluableRule();

    if (rule is List<Object?>) {
      return <Object?>[for (final item in rule) _apply(item, data, depth + 1)];
    }
    if (rule is! Map<Object?, Object?>) return rule;

    // json-logic-js treats a map as an operation only when it has exactly one key. A two-key map is
    // an authoring mistake whose meaning would otherwise depend on iteration order.
    if (rule.length != 1) throw const _UnevaluableRule();

    final entry = rule.entries.first;
    final operator = entry.key;
    if (operator is! String) throw const _UnevaluableRule();

    final raw = entry.value;
    final args = raw is List<Object?> ? raw : <Object?>[raw];

    return _applyOperator(operator, args, data, depth);
  }

  static Object? _applyOperator(
    String operator,
    List<Object?> args,
    Object? data,
    int depth,
  ) {
    // `and`, `or` and `if` short-circuit, so their arguments are evaluated lazily. Everything else
    // takes evaluated arguments, exactly as json-logic-js does.
    switch (operator) {
      case 'and':
        {
          Object? last;
          for (final arg in args) {
            last = _apply(arg, data, depth + 1);
            if (!isTruthy(last)) return last;
          }
          return last;
        }

      case 'or':
        {
          Object? last;
          for (final arg in args) {
            last = _apply(arg, data, depth + 1);
            if (isTruthy(last)) return last;
          }
          return last;
        }

      case 'if':
        return _ifThenElse(args, data, depth);
    }

    if (!kJsonLogicOperators.contains(operator)) throw const _UnevaluableRule();

    final values = <Object?>[for (final arg in args) _apply(arg, data, depth + 1)];

    return switch (operator) {
      'var' => _resolveVar(values, data),
      'missing' => _missing(values, data),
      '!' => !isTruthy(values.isEmpty ? null : values.first),
      '!!' => isTruthy(values.isEmpty ? null : values.first),
      '==' => _looseEquals(_at(values, 0), _at(values, 1)),
      '!=' => !_looseEquals(_at(values, 0), _at(values, 1)),
      '===' => _strictEquals(_at(values, 0), _at(values, 1)),
      '!==' => !_strictEquals(_at(values, 0), _at(values, 1)),
      '>' || '>=' || '<' || '<=' => _compare(operator, values),
      'in' => _contains(values),
      // `and`, `or` and `if` returned above; anything else was rejected above.
      _ => throw const _UnevaluableRule(),
    };
  }

  static Object? _at(List<Object?> values, int index) =>
      index < values.length ? values[index] : null;

  /// `if [cond, then, cond2, then2, …, else]`. An odd tail is the else branch.
  static Object? _ifThenElse(List<Object?> args, Object? data, int depth) {
    var index = 0;
    while (index + 1 < args.length) {
      if (isTruthy(_apply(args[index], data, depth + 1))) {
        return _apply(args[index + 1], data, depth + 1);
      }
      index += 2;
    }
    return index < args.length ? _apply(args[index], data, depth + 1) : null;
  }

  /// `{"var": "vehicle.category"}`, `{"var": ["fuel_type", "petrol"]}`, `{"var": ""}`.
  ///
  /// A path the context does not carry resolves to the supplied default, or to `null` — which is
  /// falsy, so a rule referencing a field the form has not reached yet does not match. That is the
  /// same "does not apply" the server produces, and it is why a conditional field stays hidden
  /// until its trigger is answered rather than flashing into view on first paint.
  static Object? _resolveVar(List<Object?> values, Object? data) {
    final rawPath = _at(values, 0);
    final fallback = _at(values, 1);

    if (rawPath == null || rawPath == '') return data;

    final path = switch (rawPath) {
      final String text => text,
      final int index => '$index',
      _ => null,
    };
    if (path == null) return fallback;

    var cursor = data;
    for (final segment in path.split('.')) {
      switch (cursor) {
        case final Map<Object?, Object?> map:
          if (!map.containsKey(segment)) return fallback;
          cursor = map[segment];
        case final List<Object?> list:
          final index = int.tryParse(segment);
          if (index == null || index < 0 || index >= list.length) return fallback;
          cursor = list[index];
        default:
          return fallback;
      }
    }
    return cursor;
  }

  /// The subset of the given keys that the context does not answer. Empty and `null` count as
  /// missing, matching json-logic-js — a field left blank is not a field that was filled in.
  static List<Object?> _missing(List<Object?> values, Object? data) {
    final only = values.length == 1 ? values.first : null;
    final keys = only is List<Object?> ? only : values;

    final absent = <Object?>[];
    for (final key in keys) {
      final resolved = _resolveVar(<Object?>[key], data);
      if (resolved == null || resolved == '') absent.add(key);
    }
    return absent;
  }

  /// JavaScript `==`. `"1" == 1` and `true == 1` are true; `null == 0` is not.
  static bool _looseEquals(Object? left, Object? right) {
    if (left == null || right == null) return left == null && right == null;
    if (left is String && right is String) return left == right;
    if (left is num && right is num) return left == right;
    if (left is bool && right is bool) return left == right;

    final leftNumber = _toNumber(left);
    final rightNumber = _toNumber(right);
    if (leftNumber == null || rightNumber == null) return false;
    return leftNumber == rightNumber;
  }

  /// JavaScript `===`. No coercion; `1 === 1.0` is still true because JS has one number type.
  static bool _strictEquals(Object? left, Object? right) {
    if (left == null || right == null) return left == null && right == null;
    if (left is num && right is num) return left == right;
    if (left is String && right is String) return left == right;
    if (left is bool && right is bool) return left == right;
    return identical(left, right);
  }

  /// `null` for a value JavaScript would coerce to `NaN`. Every comparison against `NaN` is false,
  /// which is how a rule comparing a text answer to a number quietly stops matching instead of
  /// throwing on a screen the user is in the middle of.
  static num? _toNumber(Object? value) => switch (value) {
        null => 0,
        final num number => number,
        final bool flag => flag ? 1 : 0,
        final String text => text.trim().isEmpty ? 0 : num.tryParse(text.trim()),
        _ => null,
      };

  static bool _compare(String operator, List<Object?> values) {
    // json-logic-js allows the three-argument between form for `<` and `<=`: {"<":[0, x, 100]}.
    if (values.length == 3 && (operator == '<' || operator == '<=')) {
      return _comparePair(operator, values[0], values[1]) &&
          _comparePair(operator, values[1], values[2]);
    }
    if (values.length < 2) throw const _UnevaluableRule();
    return _comparePair(operator, values[0], values[1]);
  }

  static bool _comparePair(String operator, Object? left, Object? right) {
    if (left is String && right is String) {
      return _order(operator, left.compareTo(right));
    }
    final leftNumber = _toNumber(left);
    final rightNumber = _toNumber(right);
    if (leftNumber == null || rightNumber == null) return false;
    return _order(operator, leftNumber.compareTo(rightNumber));
  }

  static bool _order(String operator, int comparison) => switch (operator) {
        '>' => comparison > 0,
        '>=' => comparison >= 0,
        '<' => comparison < 0,
        '<=' => comparison <= 0,
        _ => throw const _UnevaluableRule(),
      };

  /// `in` against a list is membership; against a string it is substring. Both use strict equality,
  /// as `Array.prototype.indexOf` and `String.prototype.indexOf` do.
  static bool _contains(List<Object?> values) {
    if (values.length < 2) throw const _UnevaluableRule();
    final needle = values[0];
    final haystack = values[1];

    if (haystack is String) {
      return switch (needle) {
        final String text => haystack.contains(text),
        final num number => haystack.contains('$number'),
        _ => false,
      };
    }
    if (haystack is List<Object?>) {
      return haystack.any((item) => _strictEquals(item, needle));
    }
    return false;
  }
}
