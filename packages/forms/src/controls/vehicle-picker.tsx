'use client';

import * as React from 'react';
import { Select } from '@autohub/ui';

import { useFieldOptions, useFormLabels } from '../form-context';
import { asRecord, asString } from './shared';
import type { FieldProps, LabelResolver, PickerLevel } from '../types';

/**
 * The cascade: make → model → year → variant.
 *
 * The levels are registry data, not a fixed four. A truck listing cascades make → model →
 * axle config → permit type; construction equipment will cascade something nobody has thought of
 * yet. Each level asks the injected loader for its options, scoped by the levels it depends on, so
 * `GET /platform/taxonomy/models?make=maruti-suzuki` is a query this file never has to know about.
 *
 * Choosing a level clears everything downstream — a Swift is not a valid model of a Mahindra, and
 * silently keeping the stale value is how a listing ends up describing a car that does not exist.
 */
export function VehiclePickerControl({
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
  const selection = asRecord(value);
  const levels = field.config?.levels ?? [];

  function choose(level: PickerLevel, next: string, index: number) {
    const merged: Record<string, unknown> = { ...selection, [level.key]: next };
    for (const downstream of levels.slice(index + 1)) delete merged[downstream.key];
    onChange(merged);
  }

  if (levels.length === 0) {
    return (
      <p className="text-body-sm text-[color:var(--text-secondary)]">{t('form.select.empty')}</p>
    );
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
      {levels.map((level, index) => (
        <PickerLevelField
          key={level.key}
          level={level}
          levels={levels}
          field={field}
          selection={selection}
          disabled={disabled || field.readOnly === true}
          invalid={invalid}
          t={t}
          onChoose={(next) => choose(level, next, index)}
        />
      ))}
    </div>
  );
}

interface PickerLevelFieldProps {
  level: PickerLevel;
  levels: PickerLevel[];
  field: FieldProps['field'];
  selection: Record<string, unknown>;
  disabled: boolean;
  invalid: boolean;
  t: LabelResolver;
  onChoose: (next: string) => void;
}

function PickerLevelField({
  level,
  levels,
  field,
  selection,
  disabled,
  invalid,
  t,
  onChoose,
}: PickerLevelFieldProps) {
  const id = React.useId();
  const dependencies = level.dependsOn ?? [];
  const missing = dependencies.find((key) => asString(selection[key]) === '');

  const scope = React.useMemo(() => {
    const picked: Record<string, unknown> = {};
    for (const key of dependencies) picked[key] = selection[key];
    return picked;
    // Keyed on the dependency values themselves, not the whole selection object.
  }, [dependencies, selection]);

  const { options, loading, failed } = useFieldOptions({
    field,
    inline: level.options,
    level: level.key,
    source: level.optionsSource,
    params: scope,
    enabled: missing === undefined,
  });

  const blockedBy = missing === undefined ? undefined : levels.find((entry) => entry.key === missing)?.label;
  const current = asString(selection[level.key]);

  const placeholder = blockedBy
    ? t('form.select.locked', { field: blockedBy })
    : loading
      ? t('form.select.loading')
      : failed
        ? t('form.select.error')
        : options.length === 0
          ? t('form.select.empty')
          : (level.placeholder ?? t('form.select.placeholder'));

  return (
    <div className="grid gap-2">
      <label htmlFor={id} className="ah-label">
        {level.label}
      </label>
      <Select
        id={id}
        options={options.map((option) => ({ value: option.value, label: option.label }))}
        value={current === '' ? undefined : current}
        onValueChange={onChoose}
        placeholder={placeholder}
        disabled={disabled || missing !== undefined || loading || options.length === 0}
        invalid={invalid && level.required === true}
      />
    </div>
  );
}
