'use client';

import * as React from 'react';
import { Input, Select } from '@autohub/ui';

import { useFieldOptions, useFormLabels } from '../form-context';
import { asRecord, asString } from './shared';
import type { AddressPart, FieldProps, LabelResolver } from '../types';

/**
 * A composite value — the parts are whatever the registry says they are.
 *
 * A dealer's premises address, a pickup location and a loan applicant's permanent address are not
 * the same shape, and a market that needs a landmark line gets one by editing `config.parts`, not
 * by editing this file. The fallback below is the shape an Indian postal address takes when the
 * registry has not spoken.
 */
export function AddressControl({
  field,
  value,
  onChange,
  onBlur,
  inputId,
  describedBy,
  invalid,
  disabled,
}: FieldProps) {
  const t = useFormLabels();
  const record = asRecord(value);
  const configured = field.config?.parts;
  const parts = React.useMemo(() => configured ?? defaultParts(t), [configured, t]);

  function setPart(key: string, next: string) {
    const merged: Record<string, unknown> = { ...record };
    if (next === '') delete merged[key];
    else merged[key] = next;
    onChange(Object.keys(merged).length === 0 ? undefined : merged);
  }

  return (
    <div
      id={inputId}
      role="group"
      aria-describedby={describedBy}
      aria-invalid={invalid || undefined}
      onBlur={onBlur}
      className="grid gap-3 sm:grid-cols-2"
    >
      {parts.map((part) => (
        <AddressPartField
          key={part.key}
          part={part}
          field={field}
          value={asString(record[part.key])}
          values={record}
          disabled={disabled || field.readOnly === true}
          invalid={invalid}
          onChange={(next) => setPart(part.key, next)}
        />
      ))}
    </div>
  );
}

interface AddressPartFieldProps {
  part: AddressPart;
  field: FieldProps['field'];
  value: string;
  values: Record<string, unknown>;
  disabled: boolean;
  invalid: boolean;
  onChange: (next: string) => void;
}

/**
 * One sub-input. A component per part rather than a loop with hooks inside it — the parts list is
 * registry data and can change length between renders, and hooks cannot.
 */
function AddressPartField({ part, field, value, values, disabled, invalid, onChange }: AddressPartFieldProps) {
  const t = useFormLabels();
  const id = React.useId();
  const isSelect = part.control === 'select' || part.options !== undefined || part.optionsSource !== undefined;

  // Only the parts this one is scoped by. Passing the whole address would refetch the city list on
  // every keystroke in the street line.
  const scope = React.useMemo(() => {
    const picked: Record<string, unknown> = {};
    for (const key of part.dependsOn ?? []) picked[key] = values[key];
    return picked;
  }, [part.dependsOn, values]);

  const { options, loading } = useFieldOptions({
    field,
    inline: part.options,
    level: part.key,
    source: part.optionsSource,
    params: scope,
    enabled: isSelect,
  });

  return (
    <div className={part.key === 'line1' ? 'grid gap-2 sm:col-span-2' : 'grid gap-2'}>
      <label htmlFor={id} className="ah-label">
        {part.label}
      </label>
      {isSelect ? (
        <Select
          id={id}
          options={options.map((option) => ({ value: option.value, label: option.label }))}
          value={value === '' ? undefined : value}
          onValueChange={onChange}
          placeholder={loading ? t('form.select.loading') : (part.placeholder ?? t('form.select.placeholder'))}
          disabled={disabled || loading || options.length === 0}
          invalid={invalid && part.required === true}
        />
      ) : (
        <Input
          id={id}
          value={value}
          mono={part.mono}
          placeholder={part.placeholder}
          disabled={disabled}
          invalid={invalid && part.required === true}
          onChange={(event) => onChange(event.target.value)}
        />
      )}
    </div>
  );
}

/** Only used when the registry sends no `parts`. Labels still come from the localisation bundle. */
function defaultParts(t: LabelResolver): AddressPart[] {
  return [
    { key: 'line1', label: t('form.address.line1'), required: true },
    { key: 'line2', label: t('form.address.line2') },
    { key: 'city', label: t('form.address.city'), required: true },
    { key: 'state', label: t('form.address.state'), required: true },
    { key: 'pincode', label: t('form.address.pincode'), required: true, mono: true },
    { key: 'landmark', label: t('form.address.landmark') },
  ];
}
