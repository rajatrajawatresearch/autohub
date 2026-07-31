'use client';

import * as React from 'react';
import { Input, formatIndianNumber } from '@autohub/ui';

import { asNumber } from './shared';
import type { FieldProps } from '../types';

/**
 * Whole and decimal quantities — kms driven, seats, payload, battery health.
 *
 * A draft string sits between the keyboard and the value because a controlled `<input type=number>`
 * that reformats on every keystroke cannot be typed in: "1", "12", "12." all have to survive, and
 * `12.` is not a number yet. The value the form holds is always a real number or `undefined`.
 */
export function NumberControl({
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
  const numeric = asNumber(value);
  const [draft, setDraft] = React.useState(() => (numeric === undefined ? '' : String(numeric)));

  // Re-sync only when the outside world moved the value somewhere the draft cannot represent —
  // a saved draft loading, a reset, a VAHAN lookup filling the odometer in.
  React.useEffect(() => {
    const parsed = draft.trim() === '' ? undefined : Number(draft);
    const drafted = Number.isFinite(parsed as number) ? (parsed as number) : undefined;
    if (drafted !== numeric) setDraft(numeric === undefined ? '' : String(numeric));
    // Intentionally keyed on the incoming value only; the draft is this component's business.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [numeric]);

  function handleChange(next: string) {
    const cleaned = next.replace(/[^\d.-]/g, '');
    setDraft(cleaned);
    if (cleaned.trim() === '') {
      onChange(undefined);
      return;
    }
    const parsed = Number(cleaned);
    if (Number.isFinite(parsed)) onChange(parsed);
  }

  const readout = numeric === undefined ? undefined : formatIndianNumber(numeric);

  return (
    <div className="grid gap-1">
      <Input
        id={inputId}
        name={name}
        type="text"
        inputMode={field.config?.step !== undefined && field.config.step < 1 ? 'decimal' : 'numeric'}
        autoComplete="off"
        mono
        value={draft}
        placeholder={field.placeholder ?? undefined}
        readOnly={field.readOnly}
        disabled={disabled}
        invalid={invalid}
        unit={field.unit ?? undefined}
        aria-describedby={describedBy}
        onChange={(event) => handleChange(event.target.value)}
        onBlur={onBlur}
      />
      {/* 38400 is unreadable at a glance; 38,400 is the number the user meant to type. Shown only
          once grouping actually changes something, so short answers do not gain a second line. */}
      {readout !== undefined && Math.abs(numeric ?? 0) >= 10000 ? (
        <p aria-hidden="true" className="font-mono text-label text-[color:var(--text-secondary)]">
          {readout}
          {field.unit ? ` ${field.unit}` : ''}
        </p>
      ) : null}
    </div>
  );
}
