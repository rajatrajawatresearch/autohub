'use client';

import { Button, cn } from '@autohub/ui';

import { useFieldOptions, useFormLabels } from '../form-context';
import { asStringArray } from './shared';
import type { FieldProps } from '../types';

/**
 * Several choices from a registry list, as chips.
 *
 * Chips rather than a multi-select menu because that is how the search facets read
 * (`facetStyle: "chips"`), and a dealer filtering inventory and a seller describing a car should
 * not be operating two different grammars for the same act.
 */
export function MultiselectControl({
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
  const selected = asStringArray(value);
  const { options, loading, failed, reload } = useFieldOptions({
    field,
    inline: field.options,
    source: field.config?.optionsSource,
    params: field.config?.optionsParams,
  });

  function toggle(optionValue: string) {
    const next = selected.includes(optionValue)
      ? selected.filter((entry) => entry !== optionValue)
      : [...selected, optionValue];
    onChange(next.length === 0 ? undefined : next);
  }

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
    <div
      id={inputId}
      role="group"
      aria-describedby={describedBy}
      aria-invalid={invalid || undefined}
      onBlur={onBlur}
      className="grid gap-2"
    >
      <div className="flex flex-wrap gap-2">
        {options.map((option) => {
          const isOn = selected.includes(option.value);
          return (
            <button
              key={option.value}
              type="button"
              role="checkbox"
              aria-checked={isOn}
              name={name}
              disabled={disabled || option.disabled === true || field.readOnly === true}
              onClick={() => toggle(option.value)}
              className={cn(
                'inline-flex min-h-11 items-center gap-2 rounded-control border px-3 text-body-sm',
                'transition-colors duration-100 ease-instrument',
                'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
                'disabled:cursor-not-allowed disabled:opacity-45',
                isOn
                  ? 'border-transparent bg-[var(--accent-primary)] font-semibold text-[color:var(--text-on-primary)]'
                  : 'border-[var(--border-strong)] bg-[var(--surface-card)] text-[color:var(--text-primary)] hover:bg-[var(--surface-sunken)]',
              )}
            >
              <span>{option.label}</span>
              {option.hint ? (
                <span className="font-mono text-label opacity-70">{option.hint}</span>
              ) : null}
            </button>
          );
        })}
      </div>
      {selected.length > 0 ? (
        <p className="ah-label">{t('form.multiselect.count', { count: selected.length })}</p>
      ) : null}
    </div>
  );
}
