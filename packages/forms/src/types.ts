/**
 * The Form Schema Registry contract, as consumed by the renderer.
 *
 * These types mirror `docs/contracts/registry-api.md` §2 exactly. They describe JSON that arrives
 * over the wire — so every shape here is permissive where the registry is permissive. A control
 * name we do not recognise, a validation rule we have never seen, an extra key on `config`: none of
 * those may break a build or a page. The sell flow stays up even when a registry row is wrong.
 *
 * Nothing in this file encodes a business fact. There are no fuel types, no statuses, no fee
 * percentages, no option lists — those live in the registries and arrive as data.
 */


/* ── Values ────────────────────────────────────────────────────────────────── */

/** A form's live values. Keys are field keys from the registry; we never know them at build time. */
export type FormValues = Record<string, unknown>;

/* ── Visibility ────────────────────────────────────────────────────────────── */

/**
 * `visibleWhen` is JsonLogic evaluated against the form's own values. This is why adding EVs took
 * no code: `{"==":[{"var":"fuel_type"},"ev"]}` on `battery_health_pct` and the field appears.
 *
 * Typed structurally rather than as `json-logic-js`'s `RulesLogic` union. The rule arrives as
 * parsed JSON from a registry an operations user authored — its shape is whatever they saved, and
 * a nominal union would reject valid documents at the package boundary while proving nothing about
 * them. The narrowing happens once, at the single call site in `visibility.ts`, where an
 * unevaluable rule is caught and treated as "not visible".
 */
export type VisibilityRule = JsonLogicValue;

/** Any JSON document. The wire shape, before anything interprets it. */
export type JsonLogicValue =
  | string
  | number
  | boolean
  | null
  | JsonLogicValue[]
  | { [key: string]: JsonLogicValue };

/* ── Controls ──────────────────────────────────────────────────────────────── */

/** The control vocabulary in the contract. The registry may send others; see `control-registry`. */
export type ControlName =
  | 'plate'
  | 'text'
  | 'number'
  | 'select'
  | 'multiselect'
  | 'radio'
  | 'checkbox'
  | 'date'
  | 'money'
  | 'phone'
  | 'otp'
  | 'file'
  | 'photo_set'
  | 'slider'
  | 'toggle'
  | 'address'
  | 'vehicle_picker'
  | 'rich_text';

/* ── Validation ────────────────────────────────────────────────────────────── */

/**
 * `rule` values in the contract: required | min | max | minLength | maxLength | pattern | email |
 * phone | oneOf | custom. Typed as `string` because a registry may add one before this client
 * knows about it — an unrecognised rule is skipped on the client and still enforced by the server.
 */
export type ValidationRuleName =
  | 'required'
  | 'min'
  | 'max'
  | 'minLength'
  | 'maxLength'
  | 'pattern'
  | 'email'
  | 'phone'
  | 'oneOf'
  | 'custom';

export interface ValidationRule {
  rule: ValidationRuleName | (string & {});
  value?: unknown;
  /** Stable machine code. Travels into the DOM so support can read it off a screenshot. */
  code: string;
  /** The message the user sees. Authored in the registry, localised by the registry. */
  message: string;
}

/* ── Options ───────────────────────────────────────────────────────────────── */

export interface FieldOption {
  value: string;
  label: string;
  sortOrder?: number;
  disabled?: boolean;
  /** Secondary line under the label — a count, a hint, a unit. */
  hint?: string;
}

/** A cascading level for `vehicle_picker` — make → model → year → variant, defined by the registry. */
export interface PickerLevel {
  key: string;
  label: string;
  placeholder?: string;
  /** Level keys whose values must be chosen before this one can load. */
  dependsOn?: string[];
  optionsSource?: string;
  options?: FieldOption[];
  required?: boolean;
}

/** A sub-input inside `address`. The registry decides whether a market needs a landmark line. */
export interface AddressPart {
  key: string;
  label: string;
  placeholder?: string;
  control?: 'text' | 'select';
  optionsSource?: string;
  options?: FieldOption[];
  required?: boolean;
  /** Other part keys this one's option list is scoped by — a city list narrowed by state. */
  dependsOn?: string[];
  /** Render as mono — pincodes and plot numbers are transcribed, not read. */
  mono?: boolean;
}

/**
 * The per-control bag from the contract (`"config": { "min": 6, "prompts": [...] }`).
 *
 * `min`/`max` mean what the value's shape says they mean: a bound on a number, a count on a list.
 * That is decided at validation time from the value itself, never from a hardcoded control list.
 */
export interface FieldConfig {
  min?: number;
  max?: number;
  step?: number;
  /** Prompted capture slots for `photo_set` — "Front 3/4", "Odometer". */
  prompts?: string[];
  /** Accepted MIME types / extensions for `file` and `photo_set`. */
  accept?: string[];
  maxSizeMb?: number;
  multiple?: boolean;
  rows?: number;
  /** Digit count for `otp`. */
  length?: number;
  /** ISO-4217 for `money`. Amounts are always minor units. */
  currency?: string;
  /** Dial code for `phone`, e.g. `+91`. */
  dialCode?: string;
  /** National significant number length for `phone`. */
  nationalLength?: number;
  /** Where `select` / `multiselect` / `radio` fetch options when they are not inline. */
  optionsSource?: string;
  /** Extra params handed to the options loader — e.g. `{ "category": "suv" }`. */
  optionsParams?: Record<string, unknown>;
  levels?: PickerLevel[];
  parts?: AddressPart[];
  /** `date` bounds, ISO `YYYY-MM-DD`. */
  minDate?: string;
  maxDate?: string;
  /** Render a `text` control as a multi-line box. */
  multiline?: boolean;
  /** Marks on a `slider` track: `[{ value, label }]`. */
  marks?: Array<{ value: number; label: string }>;
}

/* ── Fields, steps, definitions ────────────────────────────────────────────── */

export interface FieldDefinition {
  key: string;
  label: string;
  control: ControlName | (string & {});
  required?: boolean;
  placeholder?: string | null;
  helpText?: string | null;
  /** `km`, `%`, `kg` — rendered beside the label and in the value readout. */
  unit?: string | null;
  defaultValue?: unknown;
  readOnly?: boolean;
  options?: FieldOption[];
  config?: FieldConfig;
  validation?: ValidationRule[];
  visibleWhen?: VisibilityRule;
}

export interface StepDefinition {
  key: string;
  title: string;
  description?: string | null;
  fields: FieldDefinition[];
  /** A whole step can be conditional — a commercial-permit step that only applies to taxis. */
  visibleWhen?: VisibilityRule;
}

export interface FormDefinition {
  key: string;
  version: number;
  title?: string | null;
  description?: string | null;
  submitLabel?: string | null;
  steps: StepDefinition[];
}

/* ── Faults ────────────────────────────────────────────────────────────────── */

/**
 * One thing wrong with one field. `code` is the registry's stable machine code and travels into
 * the DOM; `message` is the registry's authored sentence and is the only thing the user reads.
 */
export interface FieldFault {
  code: string;
  message: string;
}

/* ── The uniform control interface ─────────────────────────────────────────── */

/**
 * Every control receives exactly this. No control knows which form it is in, which step it is on,
 * or what the value means — which is what lets the sell flow, the 200-point inspection and the loan
 * application be the same component.
 */
export interface FieldProps {
  field: FieldDefinition;
  value: unknown;
  onChange: (next: unknown) => void;
  onBlur: () => void;
  /** Form-scoped field name — matches the registry field key. */
  name: string;
  /** DOM id of the primary input, so the label's `htmlFor` lands on something focusable. */
  inputId: string;
  /** Space-separated ids of help text and error text. */
  describedBy?: string;
  invalid: boolean;
  disabled: boolean;
  /**
   * The current fault, if any. Most controls ignore it — the field shell renders the message. It
   * is here for the few that draw their own error affordance (the plate, a photo slot).
   */
  error?: FieldFault;
}

export type ControlComponent = (props: FieldProps) => React.ReactElement | null;

/* ── Media ─────────────────────────────────────────────────────────────────── */

export type UploadStatus = 'pending' | 'uploading' | 'uploaded' | 'failed';

/** What a `file` / `photo_set` field stores. Never a raw `File` — values must stay serialisable. */
export interface MediaItem {
  /** Client-side id, stable across reorders. */
  id: string;
  fileName: string;
  sizeBytes: number;
  contentType: string;
  /** The prompt this shot satisfies, when it fills a prompted slot. */
  prompt?: string | null;
  /** Local object URL for preview before upload completes. */
  previewUrl?: string;
  /** Where the file now lives, once the uploader has finished. */
  url?: string;
  /** Server-side asset id, when the uploader returns one. */
  assetId?: string;
  status: UploadStatus;
  /** 0–100. */
  progress: number;
  /** Machine code from the failed upload, shown to support. */
  errorCode?: string;
  errorMessage?: string;
}

export interface UploadResult {
  url?: string;
  assetId?: string;
}

export interface UploadRequest {
  file: File;
  field: FieldDefinition;
  prompt?: string | null;
  onProgress: (percent: number) => void;
  signal: AbortSignal;
}

/**
 * The transport is injected, not implemented here. A package that renders forms has no business
 * knowing about S3, presigned URLs, or the media pipeline.
 */
export type FileUploader = (request: UploadRequest) => Promise<UploadResult>;

/* ── Options loading ───────────────────────────────────────────────────────── */

export interface OptionsRequest {
  field: FieldDefinition;
  /** Set for cascading controls — the `vehicle_picker` level or `address` part being filled. */
  level?: string;
  source?: string;
  /** Current form values plus the picker's own partial selection, so a loader can scope its query. */
  params: Record<string, unknown>;
  signal: AbortSignal;
}

/**
 * Options come from the taxonomy / geography registries. This package asks; the app fetches.
 * That is the whole no-hardcoding rule expressed as a function type.
 */
export type OptionsLoader = (request: OptionsRequest) => Promise<FieldOption[]>;

/* ── Chrome strings ────────────────────────────────────────────────────────── */

/**
 * Every string the renderer itself needs — the registry supplies field labels and error messages,
 * but somebody still has to say "Back". These keys are shaped like the localisation registry's
 * (`GET /platform/i18n/{locale}?namespace=web`) so an app can hand its resource bundle straight in.
 */
export type FormLabelKey =
  | 'form.action.back'
  | 'form.action.next'
  | 'form.action.submit'
  | 'form.action.working'
  | 'form.step.progress'
  | 'form.field.optional'
  | 'form.validation.required'
  | 'form.validation.invalid'
  | 'form.validation.min'
  | 'form.validation.max'
  | 'form.validation.min_items'
  | 'form.validation.max_items'
  | 'form.control.unsupported.title'
  | 'form.control.unsupported.body'
  | 'form.select.placeholder'
  | 'form.select.loading'
  | 'form.select.empty'
  | 'form.select.error'
  | 'form.select.locked'
  | 'form.multiselect.count'
  | 'form.money.symbol'
  | 'form.phone.dial_code'
  | 'form.otp.hint'
  | 'form.file.choose'
  | 'form.file.drop'
  | 'form.file.remove'
  | 'form.file.retry'
  | 'form.file.uploading'
  | 'form.file.failed'
  | 'form.file.too_large'
  | 'form.file.wrong_type'
  | 'form.photo.required_shots'
  | 'form.photo.more_shots'
  | 'form.photo.add'
  | 'form.photo.drop'
  | 'form.photo.count'
  | 'form.photo.cover'
  | 'form.photo.empty_slot'
  | 'form.photo.move_earlier'
  | 'form.photo.move_later'
  | 'form.photo.assign'
  | 'form.photo.unassigned'
  | 'form.address.line1'
  | 'form.address.line2'
  | 'form.address.city'
  | 'form.address.state'
  | 'form.address.pincode'
  | 'form.address.landmark'
  | 'form.richtext.count';

export type FormLabels = Partial<Record<FormLabelKey, string>>;

/**
 * Defaults in the product's voice: a knowledgeable dealer, not a salesperson. Errors state the
 * problem and the fix. Nothing apologises. Anything here can be replaced by the localisation
 * registry without touching a component.
 */
export const defaultFormLabels: Record<FormLabelKey, string> = {
  'form.action.back': 'Back',
  'form.action.next': 'Continue',
  'form.action.submit': 'Submit',
  'form.action.working': 'Working',
  'form.step.progress': 'Step {current} of {total}',
  'form.field.optional': 'Optional',
  'form.validation.required': 'Fill this in to continue.',
  'form.validation.invalid': 'Check this value.',
  'form.validation.min': 'Enter {value} or more.',
  'form.validation.max': 'Enter {value} or less.',
  'form.validation.min_items': 'Add at least {value}.',
  'form.validation.max_items': 'Keep it to {value} or fewer.',
  'form.control.unsupported.title': 'Field unavailable',
  'form.control.unsupported.body':
    'This version of the app cannot draw this field. Update the app, or finish this field from the website.',
  'form.select.placeholder': 'Select',
  'form.select.loading': 'Loading options',
  'form.select.empty': 'No options available',
  'form.select.error': 'Options did not load. Retry.',
  'form.select.locked': 'Choose {field} first',
  'form.multiselect.count': '{count} selected',
  'form.money.symbol': '₹',
  'form.phone.dial_code': '+91',
  'form.otp.hint': 'Enter the {length}-digit code.',
  'form.file.choose': 'Choose file',
  'form.file.drop': 'Drop a file here, or choose one',
  'form.file.remove': 'Remove',
  'form.file.retry': 'Retry upload',
  'form.file.uploading': 'Uploading',
  'form.file.failed': 'Upload failed. Retry, or pick a different file.',
  'form.file.too_large': 'File is over {value} MB. Pick a smaller one.',
  'form.file.wrong_type': 'That file type is not accepted here.',
  'form.photo.required_shots': 'Prompted shots',
  'form.photo.more_shots': 'More photos',
  'form.photo.add': 'Add photos',
  'form.photo.drop': 'Drop photos here, or choose files',
  'form.photo.count': '{count} of {min} minimum',
  'form.photo.cover': 'Cover',
  'form.photo.empty_slot': 'Not shot yet',
  'form.photo.move_earlier': 'Move earlier',
  'form.photo.move_later': 'Move later',
  'form.photo.assign': 'Use for',
  'form.photo.unassigned': 'Unassigned',
  'form.address.line1': 'Address',
  'form.address.line2': 'Area',
  'form.address.city': 'City',
  'form.address.state': 'State',
  'form.address.pincode': 'Pincode',
  'form.address.landmark': 'Landmark',
  'form.richtext.count': '{count} characters',
};

/** Resolves a chrome string, interpolating `{name}` placeholders. Never returns an empty string. */
export type LabelResolver = (key: FormLabelKey, vars?: Record<string, string | number>) => string;

export function interpolate(template: string, vars?: Record<string, string | number>): string {
  if (!vars) return template;
  return template.replace(/\{(\w+)\}/g, (match, name: string) => {
    const replacement = vars[name];
    return replacement === undefined ? match : String(replacement);
  });
}

/** Builds a resolver over an override bundle, falling back to defaults, then to the key itself. */
export function createLabelResolver(overrides?: FormLabels): LabelResolver {
  return (key, vars) => {
    const override = overrides?.[key];
    const template = override && override.length > 0 ? override : defaultFormLabels[key];
    return interpolate(template ?? key, vars);
  };
}
