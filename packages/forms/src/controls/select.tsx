'use client';

import { Button, Select } from '@autohub/ui';

import { useFieldOptions, useFormLabels } from '../form-context';
import { asString } from './shared';
import type { FieldProps } from '../types';

/**
 * One choice from a registry list.
 *
 * The list is never in this file. It is inline on the field definition, or it is fetched by the
 * app through the injected loader from `GET /platform/taxonomy/attributes` — which is what makes a
 * new fuel type a database row rather than a release.
 */
export function SelectControl({
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

  const empty = !loading && options.length === 0;
  const placeholder = loading
    ? t('form.select.loading')
    : empty
      ? t('form.select.empty')
      : (field.placeholder ?? t('form.select.placeholder'));

  return (
    <div onBlur={onBlur}>
      <Select
        id={inputId}
        name={name}
        options={options.map((option) => ({
          value: option.value,
          label: option.label,
          disabled: option.disabled,
        }))}
        value={asString(value) === '' ? undefined : asString(value)}
        onValueChange={(next) => onChange(next)}
        placeholder={placeholder}
        disabled={disabled || field.readOnly === true || loading || empty}
        invalid={invalid}
        aria-describedby={describedBy}
      />
    </div>
  );
}
