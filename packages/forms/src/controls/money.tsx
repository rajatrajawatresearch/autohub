'use client';

import * as React from 'react';
import { Input, formatIndianMoney } from '@autohub/ui';

import { useFormLabels } from '../form-context';
import { asNumber, minorToRupees, rupeesToMinor } from './shared';
import type { FieldProps } from '../types';

/**
 * Rupees in, paise out.
 *
 * The user types a rupee figure; the form holds an integer number of paise, which is the only
 * representation the API, the ledger and the commission engine will accept. The conversion is
 * string arithmetic — `8.4 * 100` is `840.0000000000001`, and a listing price that is one paise
 * off is a settlement that does not reconcile.
 *
 * The live `₹8.4L` readout underneath is how India *says* the number, and it is the fastest way
 * for a seller to notice they typed one zero too many.
 */
export function MoneyControl({
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
  const minor = asNumber(value);
  const [draft, setDraft] = React.useState(() => minorToRupees(minor));

  React.useEffect(() => {
    if (rupeesToMinor(draft) !== minor) setDraft(minorToRupees(minor));
    // Keyed on the incoming value; the draft belongs to the keyboard.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [minor]);

  function handleChange(next: string) {
    const cleaned = next.replace(/[^\d.]/g, '');
    setDraft(cleaned);
    onChange(cleaned.trim() === '' ? undefined : rupeesToMinor(cleaned));
  }

  const currency = field.config?.currency ?? 'INR';
  const spoken = minor === undefined ? undefined : formatIndianMoney(minor, { compact: true, currency });

  return (
    <div className="grid gap-1">
      <div className="relative">
        <span
          aria-hidden="true"
          className="pointer-events-none absolute top-1/2 left-[var(--density-pad-x)] -translate-y-1/2 font-mono text-data text-[color:var(--text-secondary)]"
        >
          {t('form.money.symbol')}
        </span>
        <Input
          id={inputId}
          name={name}
          type="text"
          inputMode="decimal"
          autoComplete="off"
          mono
          value={draft}
          placeholder={field.placeholder ?? undefined}
          readOnly={field.readOnly}
          disabled={disabled}
          invalid={invalid}
          aria-describedby={describedBy}
          onChange={(event) => handleChange(event.target.value)}
          onBlur={onBlur}
          className="pl-[calc(var(--density-pad-x)+0.9rem)]"
        />
      </div>
      {spoken !== undefined ? (
        <p aria-hidden="true" className="font-mono text-label text-[color:var(--text-secondary)]">
          {spoken}
        </p>
      ) : null}
    </div>
  );
}
