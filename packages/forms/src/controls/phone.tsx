'use client';

import { Input } from '@autohub/ui';

import { useFormLabels } from '../form-context';
import { asString, digitsOnly } from './shared';
import type { FieldProps } from '../types';

/**
 * A mobile number, stored E.164 — `+919812345678` — because that is what `/auth/otp/request` takes.
 *
 * The dial code is a fixed block to the left of the field rather than part of the text, so a user
 * cannot delete it, cannot type it twice, and cannot paste a number that quietly ends up with two
 * country codes. India is phone-first; this field is the front door.
 */
export function PhoneControl({
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
  const dialCode = field.config?.dialCode ?? t('form.phone.dial_code');
  const nationalLength = field.config?.nationalLength ?? 10;

  const stored = asString(value);
  const national = stored.startsWith(dialCode) ? stored.slice(dialCode.length) : digitsOnly(stored);

  function handleChange(next: string) {
    const digits = digitsOnly(next).slice(0, nationalLength);
    onChange(digits === '' ? undefined : `${dialCode}${digits}`);
  }

  return (
    <div className="flex items-stretch">
      <span
        aria-hidden="true"
        className="inline-flex items-center rounded-l-control border border-r-0 border-[var(--border-strong)] bg-[var(--surface-sunken)] px-3 font-mono text-data text-[color:var(--text-secondary)]"
      >
        {dialCode}
      </span>
      <Input
        id={inputId}
        name={name}
        type="tel"
        inputMode="numeric"
        autoComplete="tel-national"
        mono
        value={national}
        placeholder={field.placeholder ?? undefined}
        readOnly={field.readOnly}
        disabled={disabled}
        invalid={invalid}
        aria-describedby={describedBy}
        onChange={(event) => handleChange(event.target.value)}
        onBlur={onBlur}
        className="rounded-l-none"
      />
    </div>
  );
}
