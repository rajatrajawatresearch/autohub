'use client';

import * as React from 'react';
import { cn } from '@autohub/ui';

import { useFormLabels } from '../form-context';
import { asString, digitsOnly } from './shared';
import type { FieldProps } from '../types';

/**
 * The six-digit code.
 *
 * Separate boxes because that is what an SMS code looks like everywhere, but one string underneath:
 * paste fills the whole row, backspace walks left, and the OS autofill hint lands the code in one
 * move. Each box is a plate block — mono, hard-bordered, zero radius.
 *
 * The value is a compact left-aligned string of digits. There is no per-box slot state and no
 * padding character; a code is entered left to right, and modelling it as anything else invents
 * states ("box 4 filled, boxes 1–3 empty") that the verify endpoint cannot represent anyway.
 */
export function OtpControl({
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
  const length = field.config?.length ?? 6;
  const code = digitsOnly(asString(value)).slice(0, length);
  const refs = React.useRef<Array<HTMLInputElement | null>>([]);

  function commit(next: string) {
    onChange(next === '' ? undefined : next);
  }

  /** The code as one slot per box, so a write to box 3 does not disturb boxes 1, 2, 4. */
  function slots(): string[] {
    return Array.from({ length }, (_, index) => code[index] ?? '');
  }

  function commitSlots(next: string[]) {
    commit(next.join('').slice(0, length));
  }

  function handleInput(index: number, raw: string) {
    const digits = digitsOnly(raw);

    if (digits.length === 0) {
      const next = slots();
      next[index] = '';
      commitSlots(next);
      return;
    }

    if (digits.length > 1) {
      // A paste. Fill from this box onward and land the caret after what arrived.
      const next = slots();
      for (let offset = 0; index + offset < length && offset < digits.length; offset += 1) {
        next[index + offset] = digits[offset] ?? '';
      }
      commitSlots(next);
      refs.current[Math.min(index + digits.length, length - 1)]?.focus();
      return;
    }

    const next = slots();
    next[index] = digits;
    commitSlots(next);
    refs.current[Math.min(index + 1, length - 1)]?.focus();
  }

  function handleKeyDown(index: number, event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Backspace' && (code[index] ?? '') === '' && index > 0) {
      event.preventDefault();
      const next = slots();
      next[index - 1] = '';
      commitSlots(next);
      refs.current[index - 1]?.focus();
      return;
    }
    if (event.key === 'ArrowLeft' && index > 0) {
      event.preventDefault();
      refs.current[index - 1]?.focus();
    }
    if (event.key === 'ArrowRight' && index < length - 1) {
      event.preventDefault();
      refs.current[index + 1]?.focus();
    }
  }

  return (
    <div className="grid gap-2">
      <div
        role="group"
        aria-describedby={describedBy}
        aria-label={field.label}
        className="flex flex-wrap gap-2"
        onBlur={onBlur}
      >
        {Array.from({ length }, (_, index) => (
          <input
            key={index}
            ref={(node) => {
              refs.current[index] = node;
            }}
            id={index === 0 ? inputId : `${inputId}-${index}`}
            name={index === 0 ? name : undefined}
            type="text"
            inputMode="numeric"
            // Only the first box claims the autofill hint; six competing one-time-code fields make
            // the OS give up and fill none of them.
            autoComplete={index === 0 ? 'one-time-code' : 'off'}
            maxLength={length}
            value={code[index] ?? ''}
            disabled={disabled}
            aria-invalid={invalid || undefined}
            aria-label={`${field.label} ${index + 1}`}
            onChange={(event) => handleInput(index, event.target.value)}
            onKeyDown={(event) => handleKeyDown(index, event)}
            className={cn(
              // `text-[length:…]`, not `text-plate`: both --text-plate and --color-plate exist,
              // and the bare utility is ambiguous between a size and a colour.
              'size-11 rounded-plate border-2 bg-plate text-center font-mono text-[length:var(--text-plate)] font-bold',
              'text-[color:var(--color-tarmac)] caret-[var(--color-highway)]',
              'outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--focus-ring)]',
              'disabled:cursor-not-allowed disabled:opacity-60',
              invalid ? 'border-[color:var(--color-flag)]' : 'border-[color:var(--color-tarmac)]',
            )}
          />
        ))}
      </div>
      <p className="text-body-sm text-[color:var(--text-secondary)]">{t('form.otp.hint', { length })}</p>
    </div>
  );
}
