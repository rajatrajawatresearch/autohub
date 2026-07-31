'use client';

/**
 * One component. Every form in the product.
 *
 * The sell flow, dealer onboarding, the 200-point inspection checklist and the loan application are
 * this file rendering different JSON from the Form Schema Registry. Nothing here knows what a car
 * is, what a fuel type is, or what a dealer has to prove — it knows steps, fields, controls and
 * rules, all of which arrive over the wire.
 *
 * The shape on screen is an RC card: an uppercase letter-spaced caption above every value, hairline
 * rules for structure, near-square corners, no elevation. The progress rail is a list of stamps
 * down the side, not a bar filling up — a form is a document being completed, not a game.
 *
 * Motion: none. Steps cut, they do not slide.
 */

import * as React from 'react';
import { Controller } from 'react-hook-form';
import { Button, cn } from '@autohub/ui';

import { resolveControl } from './control-registry';
import { SchemaFormServicesProvider, useFormLabels } from './form-context';
import { useSchemaForm, type SchemaFormApi } from './use-schema-form';
import {
  createLabelResolver,
  type FieldDefinition,
  type FileUploader,
  type FormDefinition,
  type FormLabels,
  type FormValues,
  type OptionsLoader,
  type StepDefinition,
} from './types';

export interface SchemaFormProps {
  /** Straight from `GET /platform/forms/{key}`. */
  definition: FormDefinition;
  /** Receives the payload with hidden fields already removed. */
  onSubmit: (values: FormValues) => void | Promise<void>;
  defaultValues?: FormValues;
  /** Localisation-registry strings for the renderer's own chrome. Field labels come from the schema. */
  labels?: FormLabels;
  /** Fetches option lists from the taxonomy / geography registries. */
  loadOptions?: OptionsLoader;
  /** Uploads files for `file` and `photo_set`. */
  uploader?: FileUploader;
  /** Mirrors `<html data-density="compact">` for dealer and admin. */
  compact?: boolean;
  onStepChange?: (step: StepDefinition, index: number) => void;
  /** A failure the form itself could not have caught — a 409, a gateway timeout, a server rule. */
  error?: string | null;
  /** Handle for the page: applying a 422's `metadata.fieldErrors`, resetting, reading values. */
  apiRef?: { current: SchemaFormApi | null };
  /** The page usually owns the heading; set false when it does. */
  showTitle?: boolean;
  className?: string;
  id?: string;
}

export function SchemaForm({
  definition,
  onSubmit,
  defaultValues,
  labels,
  loadOptions,
  uploader,
  compact,
  onStepChange,
  error,
  apiRef,
  showTitle = true,
  className,
  id,
}: SchemaFormProps) {
  const t = React.useMemo(() => createLabelResolver(labels), [labels]);

  return (
    <SchemaFormServicesProvider
      labels={labels}
      loadOptions={loadOptions}
      uploader={uploader}
      compact={compact}
    >
      <SchemaFormInner
        definition={definition}
        onSubmit={onSubmit}
        defaultValues={defaultValues}
        labelResolver={t}
        onStepChange={onStepChange}
        error={error}
        apiRef={apiRef}
        showTitle={showTitle}
        className={className}
        id={id}
      />
    </SchemaFormServicesProvider>
  );
}

interface InnerProps
  extends Pick<
    SchemaFormProps,
    'definition' | 'onSubmit' | 'defaultValues' | 'onStepChange' | 'error' | 'apiRef' | 'className' | 'id'
  > {
  labelResolver: ReturnType<typeof createLabelResolver>;
  showTitle: boolean;
}

function SchemaFormInner({
  definition,
  onSubmit,
  defaultValues,
  labelResolver,
  onStepChange,
  error,
  apiRef,
  showTitle,
  className,
  id,
}: InnerProps) {
  const t = labelResolver;
  const reactId = React.useId();
  const formId = id ?? `form-${definition.key}-${reactId}`;

  const api = useSchemaForm({
    definition,
    onSubmit,
    defaultValues,
    labels: t,
    onStepChange,
  });

  if (apiRef) apiRef.current = api;

  const { steps, step, stepIndex, fields, isFirst, isLast, submitting, submitError } = api;
  const multiStep = steps.length > 1;
  const submitLabel = definition.submitLabel ?? t('form.action.submit');
  const failure = error ?? submitError;

  return (
    <form
      id={formId}
      noValidate
      data-form-key={definition.key}
      data-form-version={definition.version}
      onSubmit={(event) => {
        event.preventDefault();
        if (!isLast && multiStep) {
          void api.goNext();
          return;
        }
        void api.submit(event);
      }}
      className={cn('grid gap-6', className)}
    >
      {showTitle && definition.title ? (
        <header className="grid gap-1 border-b border-[var(--border-hairline)] pb-4">
          <h2 className="font-display text-heading text-[color:var(--text-primary)]">{definition.title}</h2>
          {definition.description ? (
            <p className="text-body text-[color:var(--text-secondary)]">{definition.description}</p>
          ) : null}
        </header>
      ) : null}

      <div className={cn('grid gap-6', multiStep && 'md:grid-cols-[13rem_minmax(0,1fr)] md:gap-10')}>
        {multiStep ? (
          <ProgressRail steps={steps} current={stepIndex} furthest={api.furthest} onJump={api.goTo} />
        ) : null}

        <section className="grid gap-6">
          {/* Announces the move without reading the whole step back. A live region wrapped around
              the fields would recite every label each time a value changed. */}
          <p role="status" className="sr-only">
            {multiStep
              ? `${t('form.step.progress', { current: stepIndex + 1, total: steps.length })} — ${step?.title ?? ''}`
              : (step?.title ?? '')}
          </p>

          {step ? (
            <>
              <div className="grid gap-1">
                <h3 className="font-display text-subheading text-[color:var(--text-primary)]">
                  {step.title}
                </h3>
                {step.description ? (
                  <p className="text-body-sm text-[color:var(--text-secondary)]">{step.description}</p>
                ) : null}
              </div>

              <div className="grid gap-5 sm:grid-cols-2">
                {fields.map((field) => (
                  <FieldRow key={field.key} field={field} api={api} formId={formId} />
                ))}
              </div>
            </>
          ) : (
            <p className="text-body text-[color:var(--text-secondary)]">{t('form.select.empty')}</p>
          )}

          {failure ? (
            <p
              role="alert"
              className="border-l-2 border-flag pl-3 text-body-sm text-[color:var(--text-primary)]"
            >
              {failure}
            </p>
          ) : null}

          <footer className="flex flex-wrap items-center gap-3 border-t border-[var(--border-hairline)] pt-4">
            {multiStep && !isFirst ? (
              <Button variant="outline" onClick={api.goBack} disabled={submitting}>
                {t('form.action.back')}
              </Button>
            ) : null}

            {/* Green carries the flow; the plate yellow is spent once, on the act that ends it. */}
            {isLast ? (
              <Button type="submit" variant="action" disabled={submitting}>
                {submitting ? t('form.action.working') : submitLabel}
              </Button>
            ) : (
              <Button type="submit" variant="primary" disabled={submitting}>
                {t('form.action.next')}
              </Button>
            )}

            <span className="ml-auto font-mono text-label text-[color:var(--text-secondary)]">
              {definition.key} · v{definition.version}
            </span>
          </footer>
        </section>
      </div>
    </form>
  );
}

/* ── The progress rail ─────────────────────────────────────────────────────── */

interface ProgressRailProps {
  steps: StepDefinition[];
  current: number;
  furthest: number;
  onJump: (index: number) => void;
}

/**
 * A column of stamps, not a filling bar. Each step is numbered in mono and captioned in the label
 * atom; the current one carries a 2px green rule. Completed steps are reachable — a user who
 * remembers they typed the odometer wrong should not have to finish the form to fix it.
 */
function ProgressRail({ steps, current, furthest, onJump }: ProgressRailProps) {
  const t = useFormLabels();

  return (
    <nav aria-label={t('form.step.progress', { current: current + 1, total: steps.length })}>
      {/* Below md the rail collapses to a single line and a row of segments. */}
      <div className="grid gap-2 md:hidden">
        <p className="ah-label">{t('form.step.progress', { current: current + 1, total: steps.length })}</p>
        <ol className="flex gap-1">
          {steps.map((step, index) => (
            <li
              key={step.key}
              aria-current={index === current ? 'step' : undefined}
              className={cn(
                'h-1 flex-1',
                index <= current ? 'bg-[var(--accent-primary)]' : 'bg-[var(--surface-sunken)]',
              )}
            />
          ))}
        </ol>
        <p className="text-body-sm text-[color:var(--text-primary)]">{steps[current]?.title}</p>
      </div>

      <ol className="hidden md:grid">
        {steps.map((step, index) => {
          const done = index < current;
          const isCurrent = index === current;
          const reachable = index <= furthest;

          return (
            <li key={step.key}>
              <button
                type="button"
                disabled={!reachable}
                aria-current={isCurrent ? 'step' : undefined}
                onClick={() => onJump(index)}
                className={cn(
                  'flex w-full items-baseline gap-3 border-l-2 py-2 pl-3 text-left',
                  'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
                  'disabled:cursor-default',
                  isCurrent
                    ? 'border-[var(--accent-primary)]'
                    : 'border-[var(--border-hairline)] hover:border-[var(--border-strong)]',
                )}
              >
                <span
                  className={cn(
                    'font-mono text-label',
                    isCurrent || done
                      ? 'text-[color:var(--accent-primary)]'
                      : 'text-[color:var(--text-secondary)]',
                  )}
                >
                  {String(index + 1).padStart(2, '0')}
                </span>
                <span
                  className={cn(
                    'ah-label',
                    isCurrent ? 'text-[color:var(--text-primary)]' : '',
                  )}
                >
                  {step.title}
                </span>
              </button>
            </li>
          );
        })}
      </ol>
    </nav>
  );
}

/* ── The field shell ───────────────────────────────────────────────────────── */

interface FieldRowProps {
  field: FieldDefinition;
  api: SchemaFormApi;
  formId: string;
}

/**
 * Caption, control, help, fault — the RC-card atom, wrapped around whatever the registry named.
 *
 * The fault line carries `data-error-code` and prints the machine code in mono beside the message.
 * That code is the whole point of the registry's `code` field: a user sends a screenshot, support
 * reads `kms.too_high`, and nobody has to guess which of four validation rules fired.
 */
function FieldRow({ field, api, formId }: FieldRowProps) {
  const t = useFormLabels();
  const entry = resolveControl(String(field.control));
  const Control = entry.Component;

  const fault = api.faultFor(field.key);
  const inputId = `${formId}-${field.key}`;
  const helpId = field.helpText ? `${inputId}-help` : undefined;
  const faultId = fault ? `${inputId}-fault` : undefined;
  const describedBy = [helpId, faultId].filter(Boolean).join(' ') || undefined;

  const ownsLabel = entry.ownsLabel?.(field) ?? false;
  const ownsError = entry.ownsError?.(field) ?? false;

  return (
    <div
      data-field={field.key}
      data-control={String(field.control)}
      data-error-code={fault?.code}
      className={cn('grid content-start gap-2', entry.fullWidth && 'sm:col-span-2')}
    >
      {!ownsLabel ? (
        <label htmlFor={inputId} className="ah-label flex items-baseline gap-2">
          <span>{field.label}</span>
          {field.unit ? (
            <span className="font-mono normal-case tracking-normal opacity-70">{field.unit}</span>
          ) : null}
          {field.required !== true ? (
            <span className="ml-auto opacity-70">{t('form.field.optional')}</span>
          ) : null}
        </label>
      ) : null}

      <Controller
        name={field.key}
        control={api.form.control}
        render={({ field: bound }) => (
          <Control
            field={field}
            value={bound.value}
            onChange={bound.onChange}
            onBlur={bound.onBlur}
            name={bound.name}
            inputId={inputId}
            describedBy={describedBy}
            invalid={fault !== undefined}
            disabled={api.submitting}
            error={fault}
          />
        )}
      />

      {field.helpText ? (
        <p id={helpId} className="text-body-sm text-[color:var(--text-secondary)]">
          {field.helpText}
        </p>
      ) : null}

      {fault && !ownsError ? (
        <p
          id={faultId}
          role="alert"
          data-error-code={fault.code}
          className="flex flex-wrap items-baseline justify-between gap-x-3 border-l-2 border-flag pl-2 text-body-sm text-[color:var(--text-primary)]"
        >
          <span>{fault.message}</span>
          <span className="font-mono text-label text-[color:var(--text-secondary)]">{fault.code}</span>
        </p>
      ) : null}
    </div>
  );
}
