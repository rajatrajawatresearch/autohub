'use client';

import { Button, RadioGroup, RadioGroupItem } from '@autohub/ui';

import { useFieldOptions, useFormLabels } from '../form-context';
import { asString } from './shared';
import type { FieldProps } from '../types';

/**
 * One choice, all options visible.
 *
 * Used where the list is short and the answer matters enough to be read rather than opened —
 * owner count, condition grade, ownership type. Round, deliberately: round versus square is the
 * only grammar a user has for "pick one" versus "pick several".
 */
export function RadioControl({
  field,
  value,
  onChange,
  onBlur,
  name,
  inputId,
  describedBy,
  invalid,
  disabled,
}: FieldProps) {
  const t = useFormLabels();
  const { options, loading, failed, reload } = useFieldOptions({
    field,
    inline: field.options,
    source: field.config?.optionsSource,
    params: field.config?.optionsParams,
  });

  if (failed) {
    return (
      <div className="flex items-center gap-3">
        <p className="text-body-sm text-[color:var(--text-primary)]">{t('form.select.error')}</p>
        <Button size="sm" variant="outline" onClick={reload}>
          {t('form.file.retry')}
        </Button>
      </div>
    );
  }

  if (loading) {
    return <p className="text-body-sm text-[color:var(--text-secondary)]">{t('form.select.loading')}</p>;
  }

  if (options.length === 0) {
    return <p className="text-body-sm text-[color:var(--text-secondary)]">{t('form.select.empty')}</p>;
  }

  return (
    <RadioGroup
      id={inputId}
      name={name}
      value={asString(value) === '' ? undefined : asString(value)}
      onValueChange={(next) => onChange(next)}
      disabled={disabled || field.readOnly === true}
      aria-describedby={describedBy}
      aria-invalid={invalid || undefined}
      onBlur={onBlur}
      className="gap-3"
    >
      {options.map((option) => {
        const optionId = `${inputId}-${option.value}`;
        return (
          <div key={option.value} className="flex items-start gap-3">
            <RadioGroupItem
              id={optionId}
              value={option.value}
              disabled={option.disabled === true}
              className="mt-[0.2rem]"
            />
            <div className="grid gap-0.5">
              {/* A plain label, not the .ah-label atom: an option is prose the user reads, not a
                  caption above a value. */}
              <label htmlFor={optionId} className="cursor-pointer text-body text-[color:var(--text-primary)]">
                {option.label}
              </label>
              {option.hint ? (
                <span className="text-body-sm text-[color:var(--text-secondary)]">{option.hint}</span>
              ) : null}
            </div>
          </div>
        );
      })}
    </RadioGroup>
  );
}
