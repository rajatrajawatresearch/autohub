/**
 * Registry #2 — Form Schema.
 *
 * The frontend has exactly one form renderer and it never knows what a sell form contains. Adding
 * a field to the sell flow is a registry publish, not a release. Every rule carries its own `code`
 * and `message` so web, Flutter and the server reject the same input for the same stated reason.
 */

import { z } from 'zod';
import { OptionSchema } from './common';

/**
 * Conditional visibility is JsonLogic over the form's own values:
 * `{ "==": [{ "var": "fuel_type" }, "ev"] }` is why adding EVs needed no code.
 *
 * Self-referential, so the type is written and the schema asserted against it — TypeScript cannot
 * infer a recursive zod schema.
 */
export type JsonLogicRule =
  | string
  | number
  | boolean
  | null
  | JsonLogicRule[]
  | { [operator: string]: JsonLogicRule };

export const JsonLogicRuleSchema: z.ZodType<JsonLogicRule, z.ZodTypeDef, unknown> = z.lazy(() =>
  z.union([
    z.string(),
    z.number(),
    z.boolean(),
    z.null(),
    z.array(JsonLogicRuleSchema),
    z.record(JsonLogicRuleSchema),
  ]),
);

/**
 * The control vocabulary the renderer implements. A control the client has not shipped yet
 * degrades to a text input rather than deleting the field from the form — a half-rendered sell
 * flow still collects a sale; a blank step does not.
 */
export const FORM_CONTROLS = [
  'plate',
  'text',
  'number',
  'select',
  'multiselect',
  'radio',
  'checkbox',
  'date',
  'money',
  'phone',
  'otp',
  'file',
  'photo_set',
  'slider',
  'toggle',
  'address',
  'vehicle_picker',
  'rich_text',
] as const;

export const FormControlSchema = z.enum(FORM_CONTROLS).catch('text');
export type FormControl = z.infer<typeof FormControlSchema>;

/** `custom` is server-side only — the client shows the returned message and does not re-derive it. */
export const VALIDATION_RULES = [
  'required',
  'min',
  'max',
  'minLength',
  'maxLength',
  'pattern',
  'email',
  'phone',
  'oneOf',
  'custom',
] as const;

const RuleMetaSchema = z.object({
  /** Stable machine code, e.g. `registration.malformed`. Also the i18n key suffix. */
  code: z.string(),
  /** Pre-resolved message in the caller's locale. Problem + fix, never an apology. */
  message: z.string(),
});

/**
 * `value` is a number for numeric bounds and an ISO date string for date bounds — the control's
 * `dataType` says which, and the renderer already knows the control.
 */
const BoundValueSchema = z.union([z.number(), z.string()]);

export const ValidationRuleSchema = z.discriminatedUnion('rule', [
  RuleMetaSchema.extend({ rule: z.literal('required') }),
  RuleMetaSchema.extend({ rule: z.literal('min'), value: BoundValueSchema }),
  RuleMetaSchema.extend({ rule: z.literal('max'), value: BoundValueSchema }),
  RuleMetaSchema.extend({ rule: z.literal('minLength'), value: z.number().int().nonnegative() }),
  RuleMetaSchema.extend({ rule: z.literal('maxLength'), value: z.number().int().nonnegative() }),
  RuleMetaSchema.extend({ rule: z.literal('pattern'), value: z.string() }),
  RuleMetaSchema.extend({ rule: z.literal('email') }),
  RuleMetaSchema.extend({ rule: z.literal('phone') }),
  RuleMetaSchema.extend({ rule: z.literal('oneOf'), value: z.array(z.string()) }),
  RuleMetaSchema.extend({ rule: z.literal('custom'), value: z.string().optional() }),
]);
export type ValidationRule = z.infer<typeof ValidationRuleSchema>;

export const FormFieldSchema = z.object({
  key: z.string(),
  label: z.string(),
  control: FormControlSchema,
  required: z.boolean().default(false),
  placeholder: z.string().nullish(),
  helpText: z.string().nullish(),
  /** `km`, `%`, `₹` — shown beside the input, never folded into the label. */
  unit: z.string().nullish(),
  defaultValue: z.unknown().optional(),
  readOnly: z.boolean().default(false),
  /** Present for `select` / `multiselect` / `radio`. Absent when options come from taxonomy. */
  options: z.array(OptionSchema).optional(),
  /** Attribute code to resolve options from the taxonomy registry instead of inlining them. */
  optionsFromAttribute: z.string().nullish(),
  validation: z.array(ValidationRuleSchema).default([]),
  visibleWhen: JsonLogicRuleSchema.nullish(),
  /** Control-specific settings. Parse with the matching config schema at the point of use. */
  config: z.record(z.unknown()).optional(),
});
export type FormField = z.infer<typeof FormFieldSchema>;

export const FormStepSchema = z.object({
  key: z.string(),
  title: z.string(),
  description: z.string().nullish(),
  fields: z.array(FormFieldSchema),
  visibleWhen: JsonLogicRuleSchema.nullish(),
});
export type FormStep = z.infer<typeof FormStepSchema>;

export const FormDefinitionSchema = z.object({
  key: z.string(),
  version: z.number().int(),
  title: z.string(),
  description: z.string().nullish(),
  /** The action keeps its name through the flow: `Get my price` → `Getting your price`. */
  submitLabel: z.string().default('Continue'),
  steps: z.array(FormStepSchema),
});
export type FormDefinition = z.infer<typeof FormDefinitionSchema>;

export const FormValidateResponseSchema = z.object({
  valid: z.boolean(),
});
export type FormValidateResponse = z.infer<typeof FormValidateResponseSchema>;

/* ── Typed `config` readers ───────────────────────────────────────────────────
   `config` is an open record on the wire so the registry can extend a control without a client
   release. These parse the slice a given control actually needs, at the point of use. */

export const PhotoSetConfigSchema = z.object({
  min: z.number().int().nonnegative().default(1),
  max: z.number().int().positive().default(20),
  /** "Front 3/4", "Odometer", "Tyres" — the shot list an inspector works through. */
  prompts: z.array(z.string()).default([]),
});
export type PhotoSetConfig = z.infer<typeof PhotoSetConfigSchema>;

export const SliderConfigSchema = z.object({
  min: z.number().default(0),
  max: z.number().default(100),
  step: z.number().positive().default(1),
});
export type SliderConfig = z.infer<typeof SliderConfigSchema>;

export const OtpConfigSchema = z.object({
  length: z.number().int().positive().default(6),
  resendAfterSeconds: z.number().int().nonnegative().default(30),
});
export type OtpConfig = z.infer<typeof OtpConfigSchema>;

export const FileConfigSchema = z.object({
  accept: z.array(z.string()).default([]),
  maxSizeMb: z.number().positive().default(10),
  multiple: z.boolean().default(false),
});
export type FileConfig = z.infer<typeof FileConfigSchema>;

export const MoneyConfigSchema = z.object({
  currency: z.string().default('INR'),
  minMinor: z.number().int().optional(),
  maxMinor: z.number().int().optional(),
});
export type MoneyConfig = z.infer<typeof MoneyConfigSchema>;

/** Reads a control's config with defaults applied. Never throws — a bad config yields defaults. */
export function readFieldConfig<TSchema extends z.ZodTypeAny>(
  field: FormField,
  schema: TSchema,
): z.infer<TSchema> {
  const parsed = schema.safeParse(field.config ?? {});
  if (parsed.success) return parsed.data;
  const empty = schema.safeParse({});
  return empty.success ? empty.data : ({} as z.infer<TSchema>);
}
