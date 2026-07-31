/// Conditional visibility.
///
/// A field or a step carries `visibleWhen` — JsonLogic evaluated against the form's own values. A
/// hidden field is hidden in three places at once, and all three matter:
///
///   1. it is not rendered,
///   2. it is not validated — a required EV battery field must not block a diesel hatchback,
///   3. it is not submitted — the payload describes the car, not the form.
///
/// (2) and (3) are the ones that are easy to get wrong and expensive to get wrong. A renderer that
/// only skips the *drawing* produces either a form the user cannot submit and cannot see why, or a
/// payload carrying `battery_health_pct` for a petrol car that the server then rejects.
///
/// The evaluator is `autohub_core`'s, not one written here. Three runtimes evaluate these documents
/// — `json-logic-js` on the web, `ConditionExpression` on the server, and `JsonLogic` on handsets —
/// and a fourth semantic living in this package is exactly how the EV field ends up appearing on
/// the web and not on Android.
///
/// Pure functions over data, with no widget in sight, because the same answers have to hold in a
/// unit test, in the step gate and in the payload builder.
library;

import 'package:autohub_core/autohub_core.dart';

import 'types.dart';

/// Evaluates a `visibleWhen` rule. No rule means visible.
///
/// This is the one place in the workspace that **fails open**, and it is worth being explicit about
/// why, because everything else in `autohub_core` deliberately fails closed. A fee rule, a
/// permission guard and a state transition are all safest when a broken rule means "does not
/// apply". A form field is not: hiding it silently deletes a question from the flow *and* a key
/// from the payload, so a seller would submit a listing missing whatever the operations team had
/// just added, and nobody would ever see an error. Showing a field that should have been
/// conditional costs one confusing question, and somebody notices.
///
/// [JsonLogic.tryEvaluate] exists to make that distinction available: `null` is "could not be
/// evaluated", `false` is "evaluated, and the answer is no".
bool isVisible(Object? rule, FormValues values) {
  if (rule == null) return true;
  return JsonLogic.tryEvaluate(rule, values) ?? true;
}

bool isFieldVisible(FormField field, FormValues values) => isVisible(field.visibleWhen, values);

bool isStepVisible(FormStep step, FormValues values) => isVisible(step.visibleWhen, values);

/// The fields of one step that apply to the current values, in registry order.
List<FormField> visibleFields(FormStep step, FormValues values) => <FormField>[
      for (final FormField field in step.fields)
        if (isFieldVisible(field, values)) field,
    ];

/// The steps that apply to the current values.
///
/// A step whose every field is hidden is itself hidden — otherwise an EV-only step shows up empty
/// for a petrol car and dead-ends the user on a page with a Continue button and nothing to fill in.
List<FormStep> visibleSteps(FormDefinition definition, FormValues values) => <FormStep>[
      for (final FormStep step in definition.steps)
        if (isStepVisible(step, values) && visibleFields(step, values).isNotEmpty) step,
    ];

/// Every field across every visible step, flattened. The set validation and submission see.
List<FormField> visibleFieldsOf(FormDefinition definition, FormValues values) => <FormField>[
      for (final FormStep step in visibleSteps(definition, values)) ...visibleFields(step, values),
    ];

List<String> visibleFieldKeys(FormDefinition definition, FormValues values) => <String>[
      for (final FormField field in visibleFieldsOf(definition, values)) field.key,
    ];

/// The payload: only keys the registry declared *and* the current values make visible.
///
/// `null` entries are dropped, so a never-touched optional field is absent rather than explicitly
/// null — the server distinguishes "not answered" from "answered as nothing", and a photo set that
/// was emptied is not the same as one that was never opened.
///
/// Visibility is resolved once, against the values as submitted, rather than iteratively to a fixed
/// point. That is a decision and not an oversight: iterating would let a rule that reads a hidden
/// field's stale value resurrect it, and the resulting payload would depend on the order the user
/// happened to fill things in.
///
/// Note that the *draft* keeps hidden values and this does not. A seller who switches to petrol and
/// back to EV must find their battery figure still there; the server must never see it for a car
/// that is not an EV. Those are different jobs, and this function is only the second one.
FormValues stripHidden(FormDefinition definition, FormValues values) {
  final FormValues payload = <String, Object?>{};
  for (final FormField field in visibleFieldsOf(definition, values)) {
    final Object? value = values[field.key];
    if (value == null) continue;
    payload[field.key] = value;
  }
  return payload;
}
