'use client';

import { Checkbox } from '@autohub/ui';

import { useFieldOptions } from '../form-context';
import { asBoolean, asStringArray } from './shared';
import type { FieldProps } from '../types';

/**
 * A tick.
 *
 * With options on the field definition it is a set of ticks and the value is a list of codes —
 * accessories fitted, documents on hand, defects found on an inspection line. Without options it is
 * a single yes/no and the value is a boolean; `required` then means "must be ticked", which is what
 * a consent or a declaration needs.
 */
export function CheckboxControl({
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
  const hasOptions = (field.options?.length ?? 0) > 0 || field.config?.optionsSource !== undefined;
  const { options } = useFieldOptions({
    field,
    inline: field.options,
    source: field.config?.optionsSource,
    params: field.config?.optionsParams,
    enabled: hasOptions,
  });

  if (!hasOptions) {
    return (
      <div className="flex items-start gap-3">
        <Checkbox
          id={inputId}
          name={name}
          checked={asBoolean(value)}
          disabled={disabled || field.readOnly === true}
          invalid={invalid}
          aria-describedby={describedBy}
          onCheckedChange={(next) => onChange(next === true)}
          onBlur={onBlur}
          className="mt-[0.2rem]"
        />
        <label htmlFor={inputId} className="cursor-pointer text-body text-[color:var(--text-primary)]">
          {field.label}
        </label>
      </div>
    );
  }

  const selected = asStringArray(value);

  function toggle(optionValue: string, checked: boolean) {
    const next = checked
      ? [...selected, optionValue]
      : selected.filter((entry) => entry !== optionValue);
    onChange(next.length === 0 ? undefined : next);
  }

  return (
    <div
      id={inputId}
      role="group"
      aria-describedby={describedBy}
      aria-invalid={invalid || undefined}
      onBlur={onBlur}
      className="grid gap-3"
    >
      {options.map((option) => {
        const optionId = `${inputId}-${option.value}`;
        return (
          <div key={option.value} className="flex items-start gap-3">
            <Checkbox
              id={optionId}
              name={name}
              checked={selected.includes(option.value)}
              disabled={disabled || option.disabled === true || field.readOnly === true}
              onCheckedChange={(next) => toggle(option.value, next === true)}
              className="mt-[0.2rem]"
            />
            <div className="grid gap-0.5">
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
    </div>
  );
}
