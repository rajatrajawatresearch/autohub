/**
 * Conditional visibility.
 *
 * A field or a step carries `visibleWhen` — JsonLogic evaluated against the form's own values. A
 * hidden field is hidden in three places at once, and all three matter:
 *
 *   1. it is not rendered,
 *   2. it is not validated (a required EV battery field must not block a diesel hatchback),
 *   3. it is not submitted (the payload must describe the car, not the form).
 *
 * Pure and dependency-light on purpose: the same functions run on the server.
 */

// Default import, not `{ apply }`. json-logic-js is a UMD bundle whose exports Node's CJS lexer
// cannot see statically, so a named import works under a bundler and throws under plain Node — and
// this module has to run under plain Node, because server-side validation parity is the point.
import jsonLogic from 'json-logic-js';

import type { FieldDefinition, FormDefinition, FormValues, StepDefinition, VisibilityRule } from './types';

/**
 * Evaluates a `visibleWhen` rule. No rule means visible.
 *
 * A malformed rule from the registry resolves to *visible* rather than throwing: a bad row should
 * cost a user one confusing field, never the whole flow.
 */
export function isVisible(rule: VisibilityRule | undefined, values: FormValues): boolean {
  if (rule === undefined || rule === null) return true;
  try {
    // The single narrowing point. `rule` is registry-authored JSON, so its type is structural up to
    // here; json-logic-js validates it at runtime and throws on anything it cannot evaluate.
    return truthy(jsonLogic.apply(rule as Parameters<typeof jsonLogic.apply>[0], values));
  } catch {
    return true;
  }
}

/** JsonLogic truthiness — empty array and empty string are false, as in the spec. */
function truthy(result: unknown): boolean {
  if (Array.isArray(result)) return result.length > 0;
  return Boolean(result);
}

export function isFieldVisible(field: FieldDefinition, values: FormValues): boolean {
  return isVisible(field.visibleWhen, values);
}

export function isStepVisible(step: StepDefinition, values: FormValues): boolean {
  return isVisible(step.visibleWhen, values);
}

/** The fields of one step that apply to the current values, in registry order. */
export function visibleFields(step: StepDefinition, values: FormValues): FieldDefinition[] {
  return step.fields.filter((field) => isFieldVisible(field, values));
}

/**
 * The steps that apply to the current values. A step whose every field is hidden is itself hidden —
 * otherwise an EV-only step would show up empty for a petrol car and dead-end the user.
 */
export function visibleSteps(definition: FormDefinition, values: FormValues): StepDefinition[] {
  return definition.steps.filter(
    (step) => isStepVisible(step, values) && visibleFields(step, values).length > 0,
  );
}

/** Every field across every visible step, flattened. The set that validation and submission see. */
export function visibleFieldsOf(definition: FormDefinition, values: FormValues): FieldDefinition[] {
  const fields: FieldDefinition[] = [];
  for (const step of visibleSteps(definition, values)) {
    for (const field of visibleFields(step, values)) fields.push(field);
  }
  return fields;
}

export function visibleFieldKeys(definition: FormDefinition, values: FormValues): string[] {
  return visibleFieldsOf(definition, values).map((field) => field.key);
}

/**
 * The payload. Only keys the registry declared *and* the current values make visible, with
 * `undefined` dropped so a never-touched optional field is absent rather than null.
 *
 * Visibility is resolved once, against the values as submitted — not iteratively — so a rule that
 * depends on a hidden field's stale value cannot resurrect it.
 */
export function stripHidden(definition: FormDefinition, values: FormValues): FormValues {
  const payload: FormValues = {};
  for (const field of visibleFieldsOf(definition, values)) {
    const value = values[field.key];
    if (value === undefined) continue;
    payload[field.key] = value;
  }
  return payload;
}
