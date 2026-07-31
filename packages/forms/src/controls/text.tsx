'use client';

import { Input, Textarea } from '@autohub/ui';

import { asString } from './shared';
import type { FieldProps } from '../types';

/** Free text. `config.multiline` turns it into a box; `config.maxLength` is enforced by validation. */
export function TextControl({
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
  const config = field.config;

  if (config?.multiline === true) {
    return (
      <Textarea
        id={inputId}
        name={name}
        rows={config.rows ?? 4}
        value={asString(value)}
        placeholder={field.placeholder ?? undefined}
        readOnly={field.readOnly}
        disabled={disabled}
        invalid={invalid}
        aria-describedby={describedBy}
        onChange={(event) => onChange(event.target.value)}
        onBlur={onBlur}
      />
    );
  }

  return (
    <Input
      id={inputId}
      name={name}
      type="text"
      value={asString(value)}
      placeholder={field.placeholder ?? undefined}
      readOnly={field.readOnly}
      disabled={disabled}
      invalid={invalid}
      unit={field.unit ?? undefined}
      aria-describedby={describedBy}
      onChange={(event) => onChange(event.target.value)}
      onBlur={onBlur}
    />
  );
}
