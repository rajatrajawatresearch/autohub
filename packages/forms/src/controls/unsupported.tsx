'use client';

import { useFormLabels } from '../form-context';
import type { FieldProps } from '../types';

/**
 * What a registry row this client has never heard of looks like.
 *
 * The registries are versioned and edited by people who are not deploying this app. Sooner or later
 * a form schema will name a control that shipped after the phone in someone's hand — and when it
 * does, the sell flow must lose one field, not the whole page. So: a visible, labelled, boring
 * placeholder carrying the control name in mono, which is exactly the string support needs to hear.
 *
 * It renders in the caution tone rather than the flag tone deliberately. Nothing is broken; this
 * build is simply behind.
 */
export function UnsupportedControl({ field, inputId }: FieldProps) {
  const t = useFormLabels();

  return (
    <div
      id={inputId}
      role="note"
      data-unsupported-control={field.control}
      className="grid gap-1 border-l-2 border-caution bg-[var(--surface-sunken)] px-3 py-2"
    >
      <p className="ah-label">{t('form.control.unsupported.title')}</p>
      <p className="text-body-sm text-[color:var(--text-primary)]">
        {t('form.control.unsupported.body')}
      </p>
      <p className="font-mono text-label text-[color:var(--text-secondary)]">
        {field.key} · {String(field.control)}
      </p>
    </div>
  );
}
