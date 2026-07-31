'use client';

import { Switch } from '@autohub/ui';

import { asBoolean } from './shared';
import type { FieldProps } from '../types';

/**
 * A setting, not an answer.
 *
 * The label sits on the left and the rocker on the right across a hairline row — the shape of a
 * settings list, which is what a toggle always belongs to. Owns its label for that reason.
 */
export function ToggleControl({
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
  return (
    <div className="flex items-center justify-between gap-4 border-y border-[var(--border-hairline)] py-3">
      <div className="grid gap-0.5">
        <label htmlFor={inputId} className="cursor-pointer text-body text-[color:var(--text-primary)]">
          {field.label}
        </label>
        {field.helpText ? (
          <span className="text-body-sm text-[color:var(--text-secondary)]">{field.helpText}</span>
        ) : null}
      </div>
      <Switch
        id={inputId}
        name={name}
        checked={asBoolean(value)}
        disabled={disabled || field.readOnly === true}
        aria-describedby={describedBy}
        aria-invalid={invalid || undefined}
        onCheckedChange={(next) => onChange(next)}
        onBlur={onBlur}
      />
    </div>
  );
}
