'use client';

import { Textarea } from '@autohub/ui';

import { useFormLabels } from '../form-context';
import { asString } from './shared';
import type { FieldProps } from '../types';

/**
 * Long-form prose — a listing description, a rejection reason, an inspector's note.
 *
 * Deliberately a plain box with a live character count rather than a formatting toolbar. Nothing
 * downstream renders HTML: the description shows up in a search result, an SMS, a WhatsApp card and
 * a PDF, and every one of those wants plain text. A bold button here would be a promise the rest of
 * the product cannot keep.
 */
export function RichTextControl({
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
  const text = asString(value);
  const limit = field.validation?.find((rule) => rule.rule === 'maxLength')?.value;
  const max = typeof limit === 'number' ? limit : undefined;

  return (
    <div className="grid gap-1">
      <Textarea
        id={inputId}
        name={name}
        rows={field.config?.rows ?? 6}
        value={text}
        placeholder={field.placeholder ?? undefined}
        readOnly={field.readOnly}
        disabled={disabled}
        invalid={invalid}
        maxLength={max}
        aria-describedby={describedBy}
        onChange={(event) => onChange(event.target.value)}
        onBlur={onBlur}
      />
      <p className="ah-label text-right" aria-live="off">
        {max === undefined
          ? t('form.richtext.count', { count: text.length })
          : `${text.length}/${max}`}
      </p>
    </div>
  );
}
