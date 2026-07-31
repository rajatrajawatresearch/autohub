'use client';

/**
 * react-hook-form wired to a registry definition.
 *
 * Three things make this different from a normal RHF setup, and all three come from the fact that
 * the schema is data:
 *
 *   1. The resolver is rebuilt on every validation run, not memoised, because visibility is part of
 *      the schema. The moment `fuel_type` becomes `ev`, `battery_health_pct` becomes required — and
 *      a resolver captured at mount would never know.
 *   2. Errors carry the registry's machine code in RHF's `type` slot, which is what `type` is for.
 *      The message stays the registry's authored sentence; the code reaches the DOM.
 *   3. Steps validate against their own field keys only, so a user is never blocked on step two by
 *      something they have not been shown yet.
 */

import * as React from 'react';
import { useForm, type FieldErrors, type Resolver, type UseFormReturn } from 'react-hook-form';

import { stripHidden, visibleFields, visibleSteps } from './visibility';
import { buildFormSchema, issueCode, resolveServerFieldErrors, validateAgainstSchema } from './zod-from-schema';
import {
  createLabelResolver,
  type FieldDefinition,
  type FieldFault,
  type FormDefinition,
  type FormValues,
  type LabelResolver,
  type StepDefinition,
} from './types';

/* ── Resolver ──────────────────────────────────────────────────────────────── */

/**
 * Validation for the values as they stand. Not a `zodResolver` call, because the schema depends on
 * the values being validated and `zodResolver` takes a fixed schema.
 */
export function createSchemaResolver(
  definition: FormDefinition,
  t: LabelResolver,
): Resolver<FormValues> {
  return async (values) => {
    const schema = buildFormSchema(definition, { values, labels: t });
    const result = schema.safeParse(values);
    if (result.success) return { values, errors: {} };

    const errors: Record<string, { type: string; message: string }> = {};
    for (const issue of result.error.issues) {
      const path = issue.path.map((segment) => String(segment)).join('.');
      // First fault per field wins — the user fixes one thing at a time.
      if (path.length === 0 || errors[path] !== undefined) continue;
      errors[path] = { type: issueCode(issue), message: issue.message };
    }

    return { values: {}, errors: errors as FieldErrors<FormValues> };
  };
}

/* ── Initial values ────────────────────────────────────────────────────────── */

/** Registry `defaultValue`s first, then whatever the caller loaded — a saved draft always wins. */
export function initialValues(definition: FormDefinition, provided?: FormValues): FormValues {
  const base: FormValues = {};
  for (const step of definition.steps) {
    for (const field of step.fields) {
      if (field.defaultValue !== undefined) base[field.key] = field.defaultValue;
    }
  }
  return { ...base, ...provided };
}

/* ── The hook ──────────────────────────────────────────────────────────────── */

export interface UseSchemaFormArgs {
  definition: FormDefinition;
  /** Receives the payload with hidden fields already stripped. */
  onSubmit: (values: FormValues) => void | Promise<void>;
  defaultValues?: FormValues;
  labels?: LabelResolver;
  onStepChange?: (step: StepDefinition, index: number) => void;
}

export interface SchemaFormApi {
  form: UseFormReturn<FormValues>;
  /** Live values. Visibility, step lists and field lists are all derived from these. */
  values: FormValues;
  /** Steps that apply right now. A step whose fields are all hidden is not in this list. */
  steps: StepDefinition[];
  step: StepDefinition | undefined;
  stepIndex: number;
  /** Visible fields of the current step, in registry order. */
  fields: FieldDefinition[];
  isFirst: boolean;
  isLast: boolean;
  /** The furthest step reached, so the progress rail knows what may be jumped back to. */
  furthest: number;
  goNext: () => Promise<boolean>;
  goBack: () => void;
  goTo: (index: number) => void;
  submit: (event?: React.BaseSyntheticEvent) => Promise<void>;
  submitting: boolean;
  /** A failure from `onSubmit` itself — a network fault, a 409, a server rule the client cannot see. */
  submitError: string | undefined;
  setSubmitError: (message: string | undefined) => void;
  /** Applies `metadata.fieldErrors` from a 422, resolving each code to its registry message. */
  applyServerFieldErrors: (fieldErrors: Record<string, string[] | string>) => void;
  faultFor: (key: string) => FieldFault | undefined;
}

export function useSchemaForm({
  definition,
  onSubmit,
  defaultValues,
  labels,
  onStepChange,
}: UseSchemaFormArgs): SchemaFormApi {
  const t = React.useMemo(() => labels ?? createLabelResolver(), [labels]);
  const resolver = React.useMemo(() => createSchemaResolver(definition, t), [definition, t]);

  const form = useForm<FormValues>({
    resolver,
    defaultValues: React.useMemo(() => initialValues(definition, defaultValues), [definition, defaultValues]),
    // Nothing shouts at a user who has not finished typing; everything corrects itself once it has.
    mode: 'onTouched',
    reValidateMode: 'onChange',
    shouldFocusError: true,
  });

  // Read through the formState proxy during render so RHF actually subscribes this component.
  const { errors, isSubmitting } = form.formState;
  const values = form.watch();

  const [stepIndex, setStepIndex] = React.useState(0);
  const [furthest, setFurthest] = React.useState(0);
  const [submitError, setSubmitError] = React.useState<string | undefined>(undefined);

  const steps = React.useMemo(() => visibleSteps(definition, values), [definition, values]);
  // A step can disappear under the user — answering "petrol" removes the EV step they were on.
  const safeIndex = Math.min(stepIndex, Math.max(0, steps.length - 1));
  const step = steps[safeIndex];
  const fields = React.useMemo(() => (step ? visibleFields(step, values) : []), [step, values]);

  const move = React.useCallback(
    (index: number) => {
      const target = steps[index];
      if (!target) return;
      setStepIndex(index);
      setFurthest((reached) => Math.max(reached, index));
      onStepChange?.(target, index);
    },
    [steps, onStepChange],
  );

  const goNext = React.useCallback(async () => {
    const current = steps[safeIndex];
    if (!current) return false;

    const snapshot = form.getValues();
    const keys = visibleFields(current, snapshot).map((field) => field.key);

    // `trigger` paints the messages; the explicit parse decides whether we advance. Two calls, but
    // the gate does not then depend on RHF's internal filtering semantics.
    await form.trigger(keys, { shouldFocus: true });
    const { valid } = validateAgainstSchema(definition, snapshot, { fieldKeys: keys, labels: t });
    if (!valid) return false;

    if (safeIndex < steps.length - 1) move(safeIndex + 1);
    return true;
  }, [steps, safeIndex, form, definition, t, move]);

  const goBack = React.useCallback(() => {
    if (safeIndex > 0) move(safeIndex - 1);
  }, [safeIndex, move]);

  const goTo = React.useCallback(
    (index: number) => {
      // Forward jumps are earned by validating; backward jumps are always allowed.
      if (index > furthest) return;
      move(index);
    },
    [furthest, move],
  );

  const submit = React.useCallback(
    async (event?: React.BaseSyntheticEvent) => {
      setSubmitError(undefined);
      await form.handleSubmit(
        async (raw) => {
          await onSubmit(stripHidden(definition, raw));
        },
        (invalid) => {
          // The fault may be on a step the user cannot see. Take them to it.
          const firstKey = Object.keys(invalid)[0];
          if (firstKey === undefined) return;
          const index = steps.findIndex((candidate) =>
            candidate.fields.some((field) => field.key === firstKey),
          );
          if (index >= 0) setStepIndex(index);
        },
      )(event);
    },
    [form, onSubmit, definition, steps],
  );

  const applyServerFieldErrors = React.useCallback(
    (fieldErrors: Record<string, string[] | string>) => {
      const resolved = resolveServerFieldErrors(definition, fieldErrors, t);
      for (const [key, fault] of Object.entries(resolved)) {
        form.setError(key, { type: fault.code, message: fault.message });
      }
      const firstKey = Object.keys(resolved)[0];
      if (firstKey === undefined) return;
      const index = steps.findIndex((candidate) =>
        candidate.fields.some((field) => field.key === firstKey),
      );
      if (index >= 0) setStepIndex(index);
    },
    [definition, t, form, steps],
  );

  const faultFor = React.useCallback(
    (key: string): FieldFault | undefined => {
      const error = errors[key];
      if (!error) return undefined;
      const message = typeof error.message === 'string' ? error.message : t('form.validation.invalid');
      const code = typeof error.type === 'string' ? error.type : 'field.invalid';
      return { code, message };
    },
    [errors, t],
  );

  return {
    form,
    values,
    steps,
    step,
    stepIndex: safeIndex,
    fields,
    isFirst: safeIndex === 0,
    isLast: safeIndex === steps.length - 1,
    furthest,
    goNext,
    goBack,
    goTo,
    submit,
    submitting: isSubmitting,
    submitError,
    setSubmitError,
    applyServerFieldErrors,
    faultFor,
  };
}
