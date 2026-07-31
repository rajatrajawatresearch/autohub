'use client';

import { Slider, formatIndianNumber } from '@autohub/ui';

import { asNumber } from './shared';
import type { FieldProps } from '../types';

/**
 * A bounded quantity — battery health, condition score, tread depth.
 *
 * Bounds and step come from `config`, which comes from the taxonomy attribute's `range`. The
 * reading sits above the track in mono with its unit, because a slider without a number is a mood
 * and this product does not deal in moods.
 */
export function SliderControl({
  field,
  value,
  onChange,
  onBlur,
  inputId,
  describedBy,
  invalid,
  disabled,
}: FieldProps) {
  const config = field.config;
  const min = config?.min ?? 0;
  const max = config?.max ?? 100;
  const step = config?.step ?? 1;
  const current = asNumber(value) ?? min;

  return (
    <div className="grid gap-2">
      <div className="flex items-baseline justify-between gap-4">
        <output
          htmlFor={inputId}
          className="font-mono text-data text-[color:var(--text-primary)]"
          aria-live="off"
        >
          {formatIndianNumber(current)}
          {field.unit ? <span className="ml-1 text-label">{field.unit}</span> : null}
        </output>
        <span className="ah-label">
          {formatIndianNumber(min)}–{formatIndianNumber(max)}
          {field.unit ? ` ${field.unit}` : ''}
        </span>
      </div>
      <Slider
        id={inputId}
        value={[current]}
        min={min}
        max={max}
        step={step}
        disabled={disabled || field.readOnly === true}
        aria-label={field.label}
        aria-describedby={describedBy}
        aria-invalid={invalid || undefined}
        onValueChange={(next) => onChange(next[0] ?? min)}
        onValueCommit={() => onBlur()}
      />
      {config?.marks && config.marks.length > 0 ? (
        <div className="flex justify-between">
          {config.marks.map((mark) => (
            <span key={mark.value} className="ah-label">
              {mark.label}
            </span>
          ))}
        </div>
      ) : null}
    </div>
  );
}
