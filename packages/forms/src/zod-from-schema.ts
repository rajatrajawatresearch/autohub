/**
 * Registry validation rules → a Zod schema.
 *
 * The point is parity. `POST /platform/forms/{key}/validate` runs the same rules on the server and
 * answers with machine codes (`metadata.fieldErrors: { "kms_driven": ["kms.too_high"] }`). If the
 * client invented its own messages the two would drift on the first schema change, so every issue
 * this module raises carries the registry's own `code` and the registry's own `message`.
 *
 * The code travels as a Zod issue `param`, survives into the react-hook-form error's `type`, and
 * lands in a `data-error-code` attribute in the DOM — which is how support reads a fault off a
 * screenshot without asking the user to describe what they saw.
 *
 * No React here. The server can import this file and get identical answers.
 */

import { z } from 'zod';

import { visibleFieldsOf } from './visibility';
import {
  createLabelResolver,
  type FieldDefinition,
  type FieldFault,
  type FormDefinition,
  type FormValues,
  type LabelResolver,
  type ValidationRule,
} from './types';

export type { FieldFault };

/** Key under which a Zod custom issue carries the registry's machine code. */
export const ISSUE_CODE_PARAM = 'autohubCode';

/** Used only when an issue reaches us without a registry code — a technical fallback, not a rule. */
export const FALLBACK_ISSUE_CODE = 'field.invalid';

/* ── Value shape helpers ───────────────────────────────────────────────────── */

/**
 * "Empty" for validation purposes. `0` is a real odometer reading and a real price, so it is not
 * empty. `false` is: a required consent toggle that is off has not been answered.
 */
export function isEmptyValue(value: unknown): boolean {
  if (value === undefined || value === null) return true;
  if (typeof value === 'string') return value.trim().length === 0;
  if (typeof value === 'boolean') return value === false;
  if (Array.isArray(value)) return value.length === 0;
  if (value instanceof Date) return Number.isNaN(value.getTime());
  if (typeof value === 'object') {
    return Object.values(value as Record<string, unknown>).every((inner) => isEmptyValue(inner));
  }
  return false;
}

function countOf(value: unknown): number | undefined {
  if (Array.isArray(value)) return value.length;
  if (typeof value === 'string') return value.length;
  return undefined;
}

function numberOf(value: unknown): number | undefined {
  if (typeof value === 'number') return Number.isFinite(value) ? value : undefined;
  if (typeof value === 'string' && value.trim().length > 0) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : undefined;
  }
  if (value instanceof Date) return value.getTime();
  return undefined;
}

/* ── Rule compilation ──────────────────────────────────────────────────────── */

interface CompiledRule {
  rule: string;
  value: unknown;
  code: string;
  /** Resolved against the value, because "at least 6" reads differently for a list and a number. */
  message: (value: unknown) => string;
}

function fromRegistry(rule: ValidationRule): CompiledRule {
  return { rule: rule.rule, value: rule.value, code: rule.code, message: () => rule.message };
}

/**
 * Assembles the rules for one field: the registry's own rules first — they carry authored messages
 * and stable codes — then anything implied by `required` and `config` that the registry left to the
 * client. Registry rules always win; a synthesised rule never overrides an authored one.
 */
export function compileRules(field: FieldDefinition, t: LabelResolver): CompiledRule[] {
  const authored = field.validation ?? [];
  const declared = new Set(authored.map((rule) => rule.rule));
  const compiled: CompiledRule[] = [];

  if (field.required === true && !declared.has('required')) {
    compiled.push({
      rule: 'required',
      value: true,
      code: `${field.key}.required`,
      message: () => t('form.validation.required'),
    });
  }

  for (const rule of authored) compiled.push(fromRegistry(rule));

  const config = field.config;
  if (config) {
    if (typeof config.min === 'number' && !declared.has('min')) {
      const bound = config.min;
      compiled.push({
        rule: 'min',
        value: bound,
        code: `${field.key}.min`,
        message: (value) =>
          Array.isArray(value)
            ? t('form.validation.min_items', { value: bound })
            : t('form.validation.min', { value: bound }),
      });
    }
    if (typeof config.max === 'number' && !declared.has('max')) {
      const bound = config.max;
      compiled.push({
        rule: 'max',
        value: bound,
        code: `${field.key}.max`,
        message: (value) =>
          Array.isArray(value)
            ? t('form.validation.max_items', { value: bound })
            : t('form.validation.max', { value: bound }),
      });
    }
  }

  return compiled;
}

/* ── Rule evaluation ───────────────────────────────────────────────────────── */

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

function safeRegExp(pattern: unknown): RegExp | undefined {
  if (pattern instanceof RegExp) return pattern;
  if (typeof pattern !== 'string') return undefined;
  try {
    return new RegExp(pattern);
  } catch {
    return undefined;
  }
}

/**
 * `true` means the rule passes. An unrecognised rule passes here and is enforced by the server —
 * a registry that ships a rule this client has not learned yet must not lock users out of the flow.
 */
function passes(rule: CompiledRule, value: unknown, field: FieldDefinition): boolean {
  switch (rule.rule) {
    case 'required':
      return !isEmptyValue(value);

    case 'min': {
      const bound = numberOf(rule.value);
      if (bound === undefined) return true;
      const count = Array.isArray(value) ? countOf(value) : undefined;
      if (count !== undefined) return count >= bound;
      const numeric = numberOf(value);
      return numeric === undefined ? true : numeric >= bound;
    }

    case 'max': {
      const bound = numberOf(rule.value);
      if (bound === undefined) return true;
      const count = Array.isArray(value) ? countOf(value) : undefined;
      if (count !== undefined) return count <= bound;
      const numeric = numberOf(value);
      return numeric === undefined ? true : numeric <= bound;
    }

    case 'minLength': {
      const bound = numberOf(rule.value);
      const length = countOf(value);
      if (bound === undefined || length === undefined) return true;
      return length >= bound;
    }

    case 'maxLength': {
      const bound = numberOf(rule.value);
      const length = countOf(value);
      if (bound === undefined || length === undefined) return true;
      return length <= bound;
    }

    case 'pattern': {
      const expression = safeRegExp(rule.value);
      if (!expression) return true;
      return expression.test(String(value));
    }

    case 'email':
      return EMAIL_PATTERN.test(String(value).trim());

    case 'phone': {
      const digits = String(value).replace(/\D/g, '');
      const expected = field.config?.nationalLength ?? 10;
      // Accept the national number with or without its dial code.
      return digits.length === expected || (digits.length > expected && digits.length <= expected + 4);
    }

    case 'oneOf': {
      if (!Array.isArray(rule.value)) return true;
      const allowed = new Set(rule.value.map((entry) => String(entry)));
      if (Array.isArray(value)) return value.every((entry) => allowed.has(String(entry)));
      return allowed.has(String(value));
    }

    // Server-side only by contract — the client has neither the data nor the authority.
    case 'custom':
      return true;

    default:
      return true;
  }
}

/* ── Schema construction ───────────────────────────────────────────────────── */

/**
 * One field's schema.
 *
 * The base type is `unknown` and every check runs in a refinement, deliberately. If the base type
 * did the work, a number field handed a string would raise a Zod issue with a Zod message and no
 * registry code — an error the user cannot act on and support cannot trace. This way every issue
 * that can reach a user carries a code and a message somebody wrote on purpose.
 */
export function buildFieldSchema(field: FieldDefinition, t: LabelResolver): z.ZodTypeAny {
  const rules = compileRules(field, t);
  return z.unknown().superRefine((value, ctx) => {
    const empty = isEmptyValue(value);
    for (const rule of rules) {
      // An untouched optional field is not "wrong" — only `required` speaks about emptiness.
      if (empty && rule.rule !== 'required') continue;
      if (passes(rule, value, field)) continue;
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: rule.message(value),
        params: { [ISSUE_CODE_PARAM]: rule.code },
      });
      // One fault per field. Naming the next thing wrong before the first is fixed is noise.
      return;
    }
  });
}

export interface BuildFormSchemaOptions {
  /** Values to resolve `visibleWhen` against. Hidden fields are left out of the schema entirely. */
  values?: FormValues;
  /** Restrict the schema to these field keys — how a step validates without touching later steps. */
  fieldKeys?: readonly string[];
  labels?: LabelResolver;
}

/**
 * The whole form's schema, for the values as they currently stand.
 *
 * Rebuilt per validation run rather than memoised, because visibility is part of the schema: the
 * shape of a sell form for an EV is not the shape of a sell form for a diesel, and the difference
 * appears the instant the fuel type changes.
 */
export function buildFormSchema(
  definition: FormDefinition,
  options: BuildFormSchemaOptions = {},
): z.ZodTypeAny {
  const values = options.values ?? {};
  const t = options.labels ?? createLabelResolver();
  const allowed = options.fieldKeys ? new Set(options.fieldKeys) : undefined;

  const shape: Record<string, z.ZodTypeAny> = {};
  for (const field of visibleFieldsOf(definition, values)) {
    if (allowed && !allowed.has(field.key)) continue;
    shape[field.key] = buildFieldSchema(field, t);
  }

  // Passthrough: keys the schema does not own are the caller's business, not ours to delete here.
  // `stripHidden` is what decides the payload.
  return z.object(shape).passthrough();
}

/* ── Reading codes back out ────────────────────────────────────────────────── */

/** Pulls the registry machine code off an issue, falling back to a technical code. */
export function issueCode(issue: z.ZodIssue): string {
  if (issue.code === z.ZodIssueCode.custom) {
    const params = issue.params as Record<string, unknown> | undefined;
    const code = params?.[ISSUE_CODE_PARAM];
    if (typeof code === 'string' && code.length > 0) return code;
  }
  return FALLBACK_ISSUE_CODE;
}

/**
 * Validates without React. Returns the first fault per field, keyed by field key — deliberately the
 * same shape the server sends back in `metadata.fieldErrors`, so one function feeds both paths.
 */
export function validateAgainstSchema(
  definition: FormDefinition,
  values: FormValues,
  options: BuildFormSchemaOptions = {},
): { valid: boolean; fieldErrors: Record<string, FieldFault> } {
  const schema = buildFormSchema(definition, { ...options, values });
  const result = schema.safeParse(values);
  if (result.success) return { valid: true, fieldErrors: {} };

  const fieldErrors: Record<string, FieldFault> = {};
  for (const issue of result.error.issues) {
    const path = issue.path.map((segment) => String(segment)).join('.');
    if (path.length === 0 || fieldErrors[path] !== undefined) continue;
    fieldErrors[path] = { code: issueCode(issue), message: issue.message };
  }
  return { valid: false, fieldErrors };
}

/**
 * Turns the server's code-only answer into displayable faults by looking each code up in the
 * definition. The server is the authority on *what* is wrong; the registry is the authority on
 * *how we say it* — so a fault raised on the server reads exactly like one raised on the client.
 */
export function resolveServerFieldErrors(
  definition: FormDefinition,
  fieldErrors: Record<string, string[] | string>,
  labels?: LabelResolver,
): Record<string, FieldFault> {
  const t = labels ?? createLabelResolver();
  const byKey = new Map<string, FieldDefinition>();
  for (const step of definition.steps) {
    for (const field of step.fields) byKey.set(field.key, field);
  }

  const resolved: Record<string, FieldFault> = {};
  for (const [key, raw] of Object.entries(fieldErrors)) {
    const codes = Array.isArray(raw) ? raw : [raw];
    const code = codes[0];
    if (code === undefined) continue;
    const authored = byKey.get(key)?.validation?.find((rule) => rule.code === code);
    resolved[key] = { code, message: authored?.message ?? t('form.validation.invalid') };
  }
  return resolved;
}
