'use client';

import { Input } from '@autohub/ui';

import { asString } from './shared';
import type { FieldProps } from '../types';

/**
 * A date, in mono, stored as ISO `YYYY-MM-DD`.
 *
 * Native, on purpose. An inspector entering an insurance expiry outdoors on a phone wants the OS
 * date wheel they already know, and a custom calendar popover is a worse tool in a worse place.
 * The value is a plain date string, never a `Date` — no timezone gets to shift an RC expiry by a
 * day between the browser and the server.
 */
export function DateControl({
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
    <Input
      id={inputId}
      name={name}
      type="date"
      mono
      value={asString(value)}
      min={field.config?.minDate}
      max={field.config?.maxDate}
      readOnly={field.readOnly}
      disabled={disabled}
      invalid={invalid}
      aria-describedby={describedBy}
      onChange={(event) => onChange(event.target.value === '' ? undefined : event.target.value)}
      onBlur={onBlur}
      className="[&::-webkit-calendar-picker-indicator]:opacity-60"
    />
  );
}
