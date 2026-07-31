'use client';

import { PlateInput } from '@autohub/ui';

import { useFormLabels } from '../form-context';
import { asString } from './shared';
import type { FieldProps } from '../types';

/**
 * The registration number, rendered as the plate itself.
 *
 * This control owns its label and its error because `PlateInput` is the product's signature and
 * arrives with both built in — the plate block is not a field wearing a caption, it is the primary
 * key made visible, and the shell must not draw a second caption above it.
 */
export function PlateControl({
  field,
  value,
  onChange,
  name,
  inputId,
  disabled,
  error,
}: FieldProps) {
  const t = useFormLabels();

  return (
    <div data-error-code={error?.code}>
      <PlateInput
        id={inputId}
        name={name}
        value={asString(value)}
        onChange={(canonical) => onChange(canonical === '' ? undefined : canonical)}
        label={field.label}
        hint={field.helpText ?? undefined}
        error={error?.message}
        disabled={disabled}
        placeholder={field.placeholder ?? undefined}
        size="md"
      />
      {field.required !== true ? (
        <p className="ah-label mt-1">{t('form.field.optional')}</p>
      ) : null}
    </div>
  );
}
