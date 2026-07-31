'use client';

import { useState } from 'react';
import { useFormDefinition } from '@autohub/api';
import { SchemaForm } from '@autohub/forms';
import { Badge, Button, EmptyState, SectionHeading, Skeleton, Select } from '@autohub/ui';

const FORM_KEYS = [
  'sell_vehicle',
  'dealer_onboarding',
  'dealer_stock_intake',
  'inspection_checklist',
  'loan_application',
  'insurance_quote',
] as const;

export function FormBuilder() {
  const [key, setKey] = useState<string>(FORM_KEYS[0]);
  const definition = useFormDefinition(key);

  return (
    <div>
      <div className="flex flex-wrap items-end gap-3 pb-4">
        <label className="block">
          <span className="ah-label">Form</span>
          <Select
            value={key}
            onValueChange={setKey}
            triggerSize="sm"
            options={FORM_KEYS.map((k) => ({ value: k, label: k }))}
            aria-label="Form definition"
            className="mt-1 w-[16rem]"
          />
        </label>

        {definition.data ? (
          <Badge variant="primary" size="sm">
            v{definition.data.version}
          </Badge>
        ) : null}

        <div className="ml-auto flex gap-2">
          <Button variant="outline" size="sm">
            Add step
          </Button>
          <Button variant="outline" size="sm">
            Add field
          </Button>
        </div>
      </div>

      {definition.isLoading ? (
        <Skeleton className="h-[30rem] w-full" />
      ) : definition.isError || !definition.data ? (
        <EmptyState
          title={`No published '${key}' form`}
          body="Either the form registry is unreachable or this definition has never been published."
          action={
            <Button variant="outline" onClick={() => definition.refetch()}>
              Retry
            </Button>
          }
          align="center"
        />
      ) : (
        <div className="grid gap-8 xl:grid-cols-2">
          <section>
            <SectionHeading
              title="Structure"
              description="Field keys reference taxonomy attribute codes, so option lists are never duplicated here."
              level={2}
              size="sm"
              rule
            />

            <ol className="mt-4 space-y-5">
              {definition.data.steps.map((step, stepIndex) => (
                <li key={step.key}>
                  <div className="flex items-baseline gap-2">
                    <span className="ah-label">
                      Step {String(stepIndex + 1).padStart(2, '0')}
                    </span>
                    <span className="font-display text-subheading">{step.title}</span>
                    <span className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                      {step.key}
                    </span>
                  </div>

                  <ul className="mt-2 divide-y divide-[color:var(--border-hairline)] border-y border-[color:var(--border-hairline)]">
                    {step.fields.map((field) => (
                      <li key={field.key} className="flex flex-wrap items-center gap-2 py-2">
                        <span className="font-mono text-body-sm">{field.key}</span>
                        <Badge variant="neutral" size="sm">
                          {field.control}
                        </Badge>
                        {field.required ? (
                          <Badge variant="caution" size="sm">
                            required
                          </Badge>
                        ) : null}
                        {field.visibleWhen ? (
                          <Badge variant="info" size="sm">
                            conditional
                          </Badge>
                        ) : null}
                        {field.validation?.length ? (
                          <span className="font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                            {field.validation.length} rule{field.validation.length === 1 ? '' : 's'}
                          </span>
                        ) : null}
                        <span className="ml-auto truncate text-body-sm text-[color:var(--text-secondary)]">
                          {field.label}
                        </span>
                      </li>
                    ))}
                  </ul>
                </li>
              ))}
            </ol>
          </section>

          <section>
            <SectionHeading
              title="Live preview"
              description="Rendered by the same component the customer app uses. What you see is what ships."
              level={2}
              size="sm"
              rule
            />
            <div className="mt-4 rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-5">
              {/* compact=false: the preview must show the customer's density, not the console's,
                  or an operator signs off on spacing no customer will ever see. */}
              <SchemaForm
                definition={definition.data}
                onSubmit={() => {
                  /* Preview only — a publish is the action that changes anything. */
                }}
                showTitle={false}
                compact={false}
              />
            </div>
          </section>
        </div>
      )}
    </div>
  );
}
